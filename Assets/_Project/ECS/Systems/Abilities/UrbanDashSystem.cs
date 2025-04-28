// Path: Assets/_Project/ECS/Systems/Abilities/UrbanDashSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
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
            // Prendi riferimento al command buffer system per creare eventi
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Definisci query per entità con UrbanDashAbilityComponent
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<UrbanDashAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadWrite<MovementComponent>(),
                ComponentType.ReadWrite<HealthComponent>(),
                ComponentType.ReadWrite<PhysicsComponent>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Aggiorna lo stato dell'abilità e gestisci input
            Entities
                .WithName("UrbanDashSystem")
                .ForEach((Entity entity, int entityInQueryIndex, 
                          ref UrbanDashAbilityComponent urbanDash,
                          ref MovementComponent movement,
                          ref HealthComponent health,
                          ref PhysicsComponent physics,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in AlexComponent alexComponent) => 
                {
                    // Aggiorna i timer dell'abilità
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (urbanDash.IsActive)
                    {
                        urbanDash.RemainingTime -= deltaTime;
                        
                        if (urbanDash.RemainingTime <= 0)
                        {
                            // Termina l'abilità
                            urbanDash.IsActive = false;
                            urbanDash.RemainingTime = 0;
                            stateChanged = true;
                            
                            // Ripristina la velocità normale
                            movement.CurrentSpeed /= urbanDash.SpeedMultiplier;
                            
                            // Termina l'invulnerabilità
                            health.IsInvulnerable = false;
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.UrbanDash
                            });
                        }
                    }
                    
                    // Aggiorna il cooldown se necessario
                    if (urbanDash.CooldownRemaining > 0)
                    {
                        // Applica bonus di riduzione cooldown da Alex
                        float reduction = 1.0f - alexComponent.UrbanDashCooldownReduction;
                        urbanDash.CooldownRemaining -= deltaTime * reduction;
                        
                        if (urbanDash.CooldownRemaining <= 0)
                        {
                            urbanDash.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.UrbanDash
                            });
                        }
                    }
                    
                    // Controlla input per attivazione abilità
                    if (abilityInput.ActivateAbility && urbanDash.IsAvailable && !urbanDash.IsActive)
                    {
                        // Attiva l'abilità
                        urbanDash.IsActive = true;
                        urbanDash.RemainingTime = urbanDash.Duration;
                        urbanDash.CooldownRemaining = urbanDash.Cooldown;
                        
                        // Applica aumento di velocità
                        movement.CurrentSpeed *= urbanDash.SpeedMultiplier;
                        
                        // Applica boost iniziale di velocità
                        physics.Velocity.z += urbanDash.InitialBoost;
                        
                        // Attiva invulnerabilità temporanea
                        health.SetInvulnerable(urbanDash.Duration);
                        
                        // Crea evento di attivazione abilità
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.UrbanDash,
                            Position = transform.Position,
                            Duration = urbanDash.Duration
                        });
                    }
                    
                    // Se attivo, gestisci sfondamento di ostacoli
                    if (urbanDash.IsActive)
                    {
                        // La logica per sfondare ostacoli è gestita da ObstacleCollisionSystem
                        // che controlla se l'abilità è attiva e applica il bonus di sfondamento
                    }
                    
                }).ScheduleParallel();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}