// Path: Assets/_Project/ECS/Systems/Abilities/FireproofBodySystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Corpo Ignifugo" di Ember.
    /// Si occupa della trasformazione in forma ignea che permette
    /// di attraversare la lava e resistere al calore estremo.
    /// </summary>
    public partial class FireproofBodySystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _lavaQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<FireproofBodyAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<EmberComponent>()
            );
            
            // Query per zone di lava
            _lavaQuery = GetEntityQuery(
                ComponentType.ReadOnly<HazardComponent>(),
                ComponentType.ReadOnly<LavaTag>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni le zone di lava prima di entrare nel job
            var lavaHazards = _lavaQuery.ToEntityArray(Allocator.TempJob);
            var lavaPositions = new NativeArray<float3>(lavaHazards.Length, Allocator.TempJob);
            var lavaRadii = new NativeArray<float>(lavaHazards.Length, Allocator.TempJob);
            var hasRadiusData = new NativeArray<bool>(lavaHazards.Length, Allocator.TempJob);
            
            // Popola i dati delle zone di lava
            for (int i = 0; i < lavaHazards.Length; i++)
            {
                if (EntityManager.HasComponent<TransformComponent>(lavaHazards[i]))
                {
                    lavaPositions[i] = EntityManager.GetComponentData<TransformComponent>(lavaHazards[i]).Position;
                }
                
                if (EntityManager.HasComponent<HazardComponent>(lavaHazards[i]))
                {
                    var hazard = EntityManager.GetComponentData<HazardComponent>(lavaHazards[i]);
                    lavaRadii[i] = hazard.Radius;
                    hasRadiusData[i] = true;
                }
                else
                {
                    hasRadiusData[i] = false;
                }
            }
            
            Entities
                .WithName("FireproofBodySystem")
                .WithReadOnly(lavaHazards)
                .WithReadOnly(lavaPositions)
                .WithReadOnly(lavaRadii)
                .WithReadOnly(hasRadiusData)
                .WithDisposeOnCompletion(lavaHazards)
                .WithDisposeOnCompletion(lavaPositions)
                .WithDisposeOnCompletion(lavaRadii)
                .WithDisposeOnCompletion(hasRadiusData)
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref FireproofBodyAbilityComponent fireproofBody,
                          ref HealthComponent health,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in EmberComponent emberComponent) =>
                {
                    // Aggiorna timer e stato
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (fireproofBody.IsActive)
                    {
                        fireproofBody.RemainingTime -= deltaTime;
                        
                        if (fireproofBody.RemainingTime <= 0)
                        {
                            // Termina l'abilità
                            fireproofBody.IsActive = false;
                            fireproofBody.RemainingTime = 0;
                            stateChanged = true;
                            
                            // Disattiva immunità alla lava
                            fireproofBody.LavaWalkingActive = false;
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.FireproofBody
                            });
                        }
                        else
                        {
                            // L'abilità è attiva
                            
                            // Applica l'aura di calore che danneggia i nemici nelle vicinanze
                            // (implementazione semplificata - il danno effettivo può essere gestito da un altro sistema)
                            
                            // Controlla se il giocatore è in una zona di lava
                            bool inLava = false;
                            for (int i = 0; i < lavaHazards.Length; i++)
                            {
                                if (!hasRadiusData[i])
                                    continue;
                                
                                float distance = math.distance(transform.Position, lavaPositions[i]);
                                
                                // Se il giocatore è dentro la zona di lava
                                if (distance <= lavaRadii[i])
                                {
                                    inLava = true;
                                    break;
                                }
                            }
                            
                            // Se il giocatore è in una zona di lava, attiva l'effetto di camminata sulla lava
                            if (inLava && !fireproofBody.LavaWalkingActive)
                            {
                                fireproofBody.LavaWalkingActive = true;
                                
                                // Crea evento di attraversamento lava
                                var lavaWalkEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, lavaWalkEvent, new LavaWalkingEvent
                                {
                                    EntityID = entity,
                                    Position = transform.Position
                                });
                            }
                            else if (!inLava && fireproofBody.LavaWalkingActive)
                            {
                                fireproofBody.LavaWalkingActive = false;
                            }
                        }
                    }
                    
                    // Aggiorna il cooldown
                    if (fireproofBody.CooldownRemaining > 0)
                    {
                        fireproofBody.CooldownRemaining -= deltaTime;
                        
                        if (fireproofBody.CooldownRemaining <= 0)
                        {
                            fireproofBody.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.FireproofBody
                            });
                        }
                    }
                    
                    // Controlla input per attivazione abilità
                    if (abilityInput.ActivateAbility && fireproofBody.IsAvailable && !fireproofBody.IsActive)
                    {
                        // Attiva l'abilità
                        fireproofBody.IsActive = true;
                        fireproofBody.RemainingTime = fireproofBody.Duration;
                        fireproofBody.CooldownRemaining = fireproofBody.Cooldown;
                        
                        // Imposta invulnerabilità al fuoco (l'invulnerabilità totale potrebbe essere eccessiva)
                        health.IsInvulnerable = true;
                        
                        // Potenzia l'aura di calore in base alle capacità di Ember
                        fireproofBody.HeatAura *= (1.0f + emberComponent.HeatResistance * 0.5f);
                        
                        // Crea evento di attivazione abilità
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.FireproofBody,
                            Position = transform.Position,
                            Duration = fireproofBody.Duration
                        });
                        
                        // Crea entità visiva per la trasformazione ignea
                        var visualEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, visualEntity, new FireBodyVisualComponent
                        {
                            OwnerEntity = entity,
                            Duration = fireproofBody.Duration,
                            RemainingTime = fireproofBody.Duration
                        });
                    }
                    
                }).ScheduleParallel();
            
            // Aggiorna i visualizzatori dell'effetto igneo
            // Usiamo WithoutBurst e Run() qui perché accediamo a EntityManager
            Entities
                .WithName("FireBodyVisualUpdater")
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref FireBodyVisualComponent fireVisual,
                          ref TransformComponent transform) =>
                {
                    // Aggiorna il tempo rimanente
                    fireVisual.RemainingTime -= deltaTime;
                    
                    // Se il tempo è scaduto, distruggi l'entità visiva
                    if (fireVisual.RemainingTime <= 0)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                    else
                    {
                        // Aggiorna la posizione in base all'entità proprietaria
                        if (EntityManager.Exists(fireVisual.OwnerEntity) &&
                            EntityManager.HasComponent<TransformComponent>(fireVisual.OwnerEntity))
                        {
                            transform.Position = EntityManager.GetComponentData<TransformComponent>(fireVisual.OwnerEntity).Position;
                        }
                    }
                    
                }).Run();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    /// <summary>
    /// Componente per l'effetto visivo del corpo ignifugo
    /// </summary>
    public struct FireBodyVisualComponent : IComponentData
    {
        public Entity OwnerEntity;   // Entità proprietaria (Ember)
        public float Duration;       // Durata totale
        public float RemainingTime;  // Tempo rimanente
    }
    
    /// <summary>
    /// Tag per identificare le zone di lava
    /// </summary>
    public struct LavaTag : IComponentData { }
    
    /// <summary>
    /// Evento per l'attraversamento della lava
    /// </summary>
    public struct LavaWalkingEvent : IComponentData
    {
        public Entity EntityID;  // Entità che cammina sulla lava
        public float3 Position;  // Posizione dell'attraversamento
    }
}