// Path: Assets/_Project/ECS/Systems/Abilities/HeatAuraSystem.cs
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
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Aura di Calore" di Kai.
    /// Si occupa della generazione di un campo di calore che scioglie
    /// il ghiaccio e protegge dal freddo.
    /// </summary>
    public partial struct HeatAuraSystem : ISystem
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _iceObstacleQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisci le query usando EntityQueryBuilder
            _abilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HeatAuraAbilityComponent, AbilityInputComponent, KaiComponent>()
                .Build(ref state);
            
            // Query per ostacoli di ghiaccio
            _iceObstacleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, IceObstacleTag>()
                .Build(ref state);
            
            // Richiedi entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_abilityQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni gli ostacoli di ghiaccio prima di entrare nel job
            var iceObstacles = _iceObstacleQuery.ToEntityArray(Allocator.TempJob);
            var obstaclePositions = new NativeArray<float3>(iceObstacles.Length, Allocator.TempJob);
            var obstacleScales = new NativeArray<float>(iceObstacles.Length, Allocator.TempJob);
            var iceIntegrities = new NativeArray<IceIntegrityComponent>(iceObstacles.Length, Allocator.TempJob);
            var hasIntegrity = new NativeArray<bool>(iceObstacles.Length, Allocator.TempJob);
            
            // Popola i dati degli ostacoli
            for (int i = 0; i < iceObstacles.Length; i++)
            {
                if (state.EntityManager.HasComponent<TransformComponent>(iceObstacles[i]))
                {
                    var transform = state.EntityManager.GetComponentData<TransformComponent>(iceObstacles[i]);
                    obstaclePositions[i] = transform.Position;
                    obstacleScales[i] = transform.Scale;
                }
                
                if (state.EntityManager.HasComponent<IceIntegrityComponent>(iceObstacles[i]))
                {
                    iceIntegrities[i] = state.EntityManager.GetComponentData<IceIntegrityComponent>(iceObstacles[i]);
                    hasIntegrity[i] = true;
                }
                else
                {
                    hasIntegrity[i] = false;
                }
            }
            
            // Usiamo un job IJobEntity personalizzato invece di Entities.ForEach
            new HeatAuraProcessingJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                IceObstacles = iceObstacles,
                ObstaclePositions = obstaclePositions,
                ObstacleScales = obstacleScales,
                IceIntegrities = iceIntegrities,
                HasIntegrity = hasIntegrity
            }.ScheduleParallel(_abilityQuery, state.Dependency).Complete();
            
            // Il job si prenderà cura della disposal dei NativeArray
            
            // Aggiorna i visualizzatori dell'aura con un IJobEntity
            var visualQuery = SystemAPI.QueryBuilder()
                .WithAll<HeatAuraVisualComponent, TransformComponent>()
                .Build(ref state);
                
            state.Dependency = new HeatAuraVisualUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                TransformLookup = state.GetComponentLookup<TransformComponent>(true)
            }.ScheduleParallel(visualQuery, state.Dependency);
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
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
    
    /// <summary>
    /// Job per elaborare l'abilità Heat Aura
    /// </summary>
    [BurstCompile]
    public partial struct HeatAuraProcessingJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public NativeArray<Entity> IceObstacles;
        [ReadOnly] public NativeArray<float3> ObstaclePositions;
        [ReadOnly] public NativeArray<float> ObstacleScales;
        [ReadOnly] public NativeArray<IceIntegrityComponent> IceIntegrities;
        [ReadOnly] public NativeArray<bool> HasIntegrity;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                  ref HeatAuraAbilityComponent heatAura,
                  in AbilityInputComponent abilityInput,
                  in TransformComponent transform,
                  in KaiComponent kaiComponent)
        {
            // Aggiorna timer e stato
            bool stateChanged = false;
            
            // Se l'abilità è attiva, gestisci la durata
            if (heatAura.IsActive)
            {
                heatAura.RemainingTime -= DeltaTime;
                
                if (heatAura.RemainingTime <= 0)
                {
                    // Termina l'abilità
                    heatAura.IsActive = false;
                    heatAura.RemainingTime = 0;
                    stateChanged = true;
                    
                    // Crea evento di fine abilità
                    var endAbilityEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.HeatAura
                    });
                }
                else
                {
                    // Continua gli effetti dell'aura di calore
                    
                    // 1. Effetto di scioglimento del ghiaccio
                    for (int i = 0; i < IceObstacles.Length; i++)
                    {
                        if (!HasIntegrity[i])
                            continue;
                        
                        float distance = math.distance(transform.Position, ObstaclePositions[i]);
                        
                        // Se l'ostacolo è nel raggio dell'aura
                        if (distance <= heatAura.AuraRadius)
                        {
                            // Calcola la velocità di scioglimento basata sulla distanza
                            float meltFactor = 1.0f - (distance / heatAura.AuraRadius);
                            float meltRate = heatAura.MeltIceRate * meltFactor;
                            
                            // Ottieni il componente di ghiaccio e riduci la sua integrità
                            var iceIntegrity = IceIntegrities[i];
                            iceIntegrity.CurrentIntegrity -= meltRate * DeltaTime;
                            
                            // Se il ghiaccio è completamente sciolto
                            if (iceIntegrity.CurrentIntegrity <= 0)
                            {
                                ECB.DestroyEntity(entityInQueryIndex, IceObstacles[i]);
                                
                                // Crea effetto di scioglimento
                                var meltEffect = ECB.CreateEntity(entityInQueryIndex);
                                ECB.AddComponent(entityInQueryIndex, meltEffect, new IceMeltEffectComponent
                                {
                                    Position = ObstaclePositions[i],
                                    Size = ObstacleScales[i]
                                });
                            }
                            else
                            {
                                // Aggiorna l'integrità del ghiaccio
                                ECB.SetComponent(entityInQueryIndex, IceObstacles[i], iceIntegrity);
                            }
                        }
                    }
                    
                    // 2. Protezione dal freddo - già gestita dall'attributo IsActive
                }
            }
            
            // Aggiorna il cooldown
            if (heatAura.CooldownRemaining > 0)
            {
                heatAura.CooldownRemaining -= DeltaTime;
                
                if (heatAura.CooldownRemaining <= 0)
                {
                    heatAura.CooldownRemaining = 0;
                    stateChanged = true;
                    
                    // Crea evento di abilità pronta
                    var readyEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
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
                var activateEvent = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                {
                    EntityID = entity,
                    AbilityType = AbilityType.HeatAura,
                    Position = transform.Position,
                    Duration = heatAura.Duration
                });
                
                // Crea entità visiva per l'aura
                var auraEntity = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, auraEntity, new HeatAuraVisualComponent
                {
                    OwnerEntity = entity,
                    Radius = heatAura.AuraRadius,
                    Duration = heatAura.Duration,
                    RemainingTime = heatAura.Duration
                });
            }
        }
        
        public void OnDestroy()
        {
            // Distrutti automaticamente grazie a WithDisposeOnCompletion
            if (IceObstacles.IsCreated) IceObstacles.Dispose();
            if (ObstaclePositions.IsCreated) ObstaclePositions.Dispose();
            if (ObstacleScales.IsCreated) ObstacleScales.Dispose();
            if (IceIntegrities.IsCreated) IceIntegrities.Dispose();
            if (HasIntegrity.IsCreated) HasIntegrity.Dispose();
        }
    }
    
    /// <summary>
    /// Job per aggiornare le visualizzazioni dell'aura di calore
    /// </summary>
    [BurstCompile]
    public partial struct HeatAuraVisualUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public ComponentLookup<TransformComponent> TransformLookup;
        
        // Metodo Execute chiamato per ogni entità nel query
        public void Execute([ChunkIndexInQuery] int chunkIndexInQuery, 
                          Entity entity,
                          ref HeatAuraVisualComponent auraVisual,
                          ref TransformComponent transform)
        {
            // Aggiorna il tempo rimanente
            auraVisual.RemainingTime -= DeltaTime;
            
            // Se il tempo è scaduto, distruggi l'entità visiva
            if (auraVisual.RemainingTime <= 0)
            {
                ECB.DestroyEntity(chunkIndexInQuery, entity);
            }
            else
            {
                // Aggiorna la posizione in base all'entità proprietaria
                if (auraVisual.OwnerEntity != Entity.Null && 
                    TransformLookup.HasComponent(auraVisual.OwnerEntity))
                {
                    transform.Position = TransformLookup[auraVisual.OwnerEntity].Position;
                }
            }
        }
    }
}