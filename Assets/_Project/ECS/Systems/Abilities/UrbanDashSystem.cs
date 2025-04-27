using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Scatto Urbano" di Alex.
    /// Si occupa dell'attivazione, della durata e degli effetti dell'abilità,
    /// inclusi velocità aumentata, invulnerabilità e capacità di sfondare ostacoli.
    /// </summary>
    public partial class UrbanDashSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            // Riferimento al command buffer system per generare eventi
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Query per entità con l'abilità Scatto Urbano
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<UrbanDashAbilityComponent>(),
                ComponentType.ReadWrite<MovementComponent>(),
                ComponentType.ReadWrite<PhysicsComponent>(),
                ComponentType.ReadWrite<HealthComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>()
            );
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora lo stato e le richieste dell'abilità
            Entities
                .WithName("UrbanDashProcessor")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref UrbanDashAbilityComponent ability,
                          ref MovementComponent movement,
                          ref PhysicsComponent physics,
                          ref HealthComponent health,
                          in AbilityInputComponent abilityInput) => 
                {
                    // Se è stata richiesta l'attivazione dell'abilità
                    if (abilityInput.ActivateAbility && ability.IsAvailable)
                    {
                        // Attiva l'abilità
                        bool activated = ability.Activate();
                        
                        if (activated)
                        {
                            // Applica effetti immediati
                            
                            // 1. Impulso di velocità nella direzione attuale
                            float3 dashDirection = movement.IsMoving 
                                ? movement.MoveDirection 
                                : new float3(0, 0, 1); // Default avanti
                                
                            physics.Velocity += dashDirection * ability.InitialBoost;
                            
                            // 2. Attiva invulnerabilità temporanea
                            health.SetInvulnerable(ability.Duration);
                            
                            // Genera un evento di attivazione abilità
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new AbilityActivatedEvent
                            {
                                EntityActivated = entity,
                                AbilityType = AbilityType.UrbanDash,
                                Duration = ability.Duration
                            });
                        }
                    }
                    
                    // Aggiorna lo stato dell'abilità (durata, cooldown)
                    bool stateChanged = ability.Update(deltaTime);
                    
                    // Se l'abilità è attiva, applica gli effetti continui
                    if (ability.IsActive)
                    {
                        // 1. Velocità aumentata
                        movement.CurrentSpeed = movement.BaseSpeed * ability.SpeedMultiplier;
                        
                        // 2. Effetti visivi (gestiti tramite eventi e sistema VFX separato)
                        
                        // 3. Controlla collisioni con ostacoli (in un sistema reale, questo
                        //    verrebbe probabilmente gestito nel sistema di collisione)
                    }
                    // Se l'abilità è appena terminata, ripristina gli stati modificati
                    else if (stateChanged)
                    {
                        // Ripristina la velocità normale
                        movement.CurrentSpeed = movement.BaseSpeed;
                        
                        // Genera un evento di fine abilità
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new AbilityDeactivatedEvent
                        {
                            EntityDeactivated = entity,
                            AbilityType = AbilityType.UrbanDash,
                            CooldownRemaining = ability.CooldownRemaining
                        });
                    }
                    
                    // Se lo stato del cooldown è cambiato (disponibile per l'uso), notifica
                    if (stateChanged && ability.IsAvailable)
                    {
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new AbilityReadyEvent
                        {
                            EntityReady = entity,
                            AbilityType = AbilityType.UrbanDash
                        });
                    }
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}