// Path: Assets/_Project/ECS/Systems/Abilities/HeatAuraSystem.cs
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
    /// Sistema che gestisce l'abilità "Aura di Calore" di Kai.
    /// Si occupa della generazione di un campo di calore che scioglie
    /// il ghiaccio e protegge dal freddo.
    /// </summary>
    public partial class HeatAuraSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _iceObstacleQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<HeatAuraAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<KaiComponent>()
            );
            
            // Query per ostacoli di ghiaccio
            _iceObstacleQuery = GetEntityQuery(
                ComponentType.ReadOnly<ObstacleComponent>(),
                ComponentType.ReadOnly<IceObstacleTag>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            Entities
                .WithName("HeatAuraSystem")
                .WithReadOnly(_iceObstacleQuery)
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref HeatAuraAbilityComponent heatAura,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in KaiComponent kaiComponent) =>
                {
                    // Aggiorna timer e stato
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (heatAura.IsActive)
                    {
                        heatAura.RemainingTime -= deltaTime;
                        
                        if (heatAura.RemainingTime <= 0)
                        {
                            // Termina l'abilità
                            heatAura.IsActive = false;
                            heatAura.RemainingTime = 0;
                            stateChanged = true;
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.HeatAura
                            });
                        }
                        else
                        {
                            // Continua gli effetti dell'aura di calore
                            
                            // 1. Effetto di scioglimento del ghiaccio
                            foreach (var obstacle in _iceObstacleQuery.ToEntityArray(Allocator.Temp))
                            {
                                if (EntityManager.HasComponent<TransformComponent>(obstacle))
                                {
                                    var obstacleTransform = EntityManager.GetComponentData<TransformComponent>(obstacle);
                                    float distance = math.distance(transform.Position, obstacleTransform.Position);
                                    
                                    // Se l'ostacolo è nel raggio dell'aura
                                    if (distance <= heatAura.AuraRadius)
                                    {
                                        // Calcola la velocità di scioglimento basata sulla distanza
                                        float meltFactor = 1.0f - (distance / heatAura.AuraRadius);
                                        float meltRate = heatAura.MeltIceRate * meltFactor;
                                        
                                        // Ottieni il componente di ghiaccio e riduci la sua integrità
                                        if (EntityManager.HasComponent<IceIntegrityComponent>(obstacle))
                                        {
                                            var iceIntegrity = EntityManager.GetComponentData<IceIntegrityComponent>(obstacle);
                                            iceIntegrity.CurrentIntegrity -= meltRate * deltaTime;
                                            
                                            // Se il ghiaccio è completamente sciolto
                                            if (iceIntegrity.CurrentIntegrity <= 0)
                                            {
                                                commandBuffer.DestroyEntity(entityInQueryIndex, obstacle);
                                                
                                                // Crea effetto di scioglimento
                                                var meltEffect = commandBuffer.CreateEntity(entityInQueryIndex);
                                                commandBuffer.AddComponent(entityInQueryIndex, meltEffect, new IceMeltEffectComponent
                                                {
                                                    Position = obstacleTransform.Position,
                                                    Size = obstacleTransform.Scale
                                                });
                                            }
                                            else
                                            {
                                                // Aggiorna l'integrità del ghiaccio
                                                commandBuffer.SetComponent(entityInQueryIndex, obstacle, iceIntegrity);
                                            }
                                        }
                                    }
                                }
                            }
                            
                            // 2. Protezione dal freddo - già gestita dall'attributo IsActive
                        }
                    }
                    
                    // Aggiorna il cooldown
                    if (heatAura.CooldownRemaining > 0)
                    {
                        heatAura.CooldownRemaining -= deltaTime;
                        
                        if (heatAura.CooldownRemaining <= 0)
                        {
                            heatAura.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.HeatAura
                            });
                        }
                    }
                    
                    // Controlla input per attivazione abilità
                    if (abilityInput.ActivateAbility && heatAura.IsAvailable && !heatAura.IsActive)
                    {
                        // Attiva l'abilità
                        heatAura.IsActive = true;
                        heatAura.RemainingTime = heatAura.Duration;
                        heatAura.CooldownRemaining = heatAura.Cooldown;
                        
                        // Potenzia l'aura in base alle capacità di Kai
                        heatAura.AuraRadius *= (1.0f + kaiComponent.HeatRetention * 0.5f);
                        
                        // Crea evento di attivazione abilità
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.HeatAura,
                            Position = transform.Position,
                            Duration = heatAura.Duration
                        });
                        
                        // Crea entità visiva per l'aura
                        var auraEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, auraEntity, new HeatAuraVisualComponent
                        {
                            OwnerEntity = entity,
                            Radius = heatAura.AuraRadius,
                            Duration = heatAura.Duration,
                            RemainingTime = heatAura.Duration
                        });
                    }
                    
                }).ScheduleParallel();
            
            // Aggiorna i visualizzatori dell'aura
            Entities
                .WithName("HeatAuraVisualUpdater")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref HeatAuraVisualComponent auraVisual,
                          ref TransformComponent transform) =>
                {
                    // Aggiorna il tempo rimanente
                    auraVisual.RemainingTime -= deltaTime;
                    
                    // Se il tempo è scaduto, distruggi l'entità visiva
                    if (auraVisual.RemainingTime <= 0)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                    else
                    {
                        // Aggiorna la posizione in base all'entità proprietaria
                        if (EntityManager.Exists(auraVisual.OwnerEntity) &&
                            EntityManager.HasComponent<TransformComponent>(auraVisual.OwnerEntity))
                        {
                            transform.Position = EntityManager.GetComponentData<TransformComponent>(auraVisual.OwnerEntity).Position;
                        }
                    }
                    
                }).ScheduleParallel();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    /// <summary>
    /// Componente per l'effetto visivo dell'aura di calore
    /// </summary>
    public struct HeatAuraVisualComponent : IComponentData
    {
        public Entity OwnerEntity;   // Entità proprietaria (Kai)
        public float Radius;         // Raggio dell'aura
        public float Duration;       // Durata totale
        public float RemainingTime;  // Tempo rimanente
    }
    
    /// <summary>
    /// Tag per identificare gli ostacoli di ghiaccio
    /// </summary>
    public struct IceObstacleTag : IComponentData { }
    
    /// <summary>
    /// Componente per tracciare l'integrità del ghiaccio
    /// </summary>
    public struct IceIntegrityComponent : IComponentData
    {
        public float MaxIntegrity;     // Integrità massima
        public float CurrentIntegrity; // Integrità attuale
    }
    
    /// <summary>
    /// Componente per l'effetto visivo di scioglimento del ghiaccio
    /// </summary>
    public struct IceMeltEffectComponent : IComponentData
    {
        public float3 Position; // Posizione dell'effetto
        public float Size;      // Dimensione dell'effetto
    }
}