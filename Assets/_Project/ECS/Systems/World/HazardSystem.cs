using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce le zone pericolose (hazard) nell'ambiente di gioco.
    /// Si occupa del rilevamento di entità all'interno di aree pericolose,
    /// dell'applicazione di danni e effetti di stato, e dell'aggiornamento
    /// delle proprietà delle zone pericolose nel tempo.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct HazardSystem : ISystem
    {
        // Query per le zone pericolose
        private EntityQuery _hazardsQuery;
        
        // Query per le entità vulnerabili ai pericoli
        private EntityQuery _vulnerableEntitiesQuery;
        
        // Stati di effetti attivi
        private ComponentLookup<StatusEffectComponent> _statusEffectLookup;
        
        // Timer per effetti periodici
        private float _effectTimer;
        
        /// <summary>
        /// Inizializza il sistema di gestione delle zone pericolose
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le zone pericolose
            _hazardsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HazardComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per le entità vulnerabili
            _vulnerableEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TransformComponent, HealthComponent>()
                .WithNone<InvulnerableTag>() // Escludi entità invulnerabili
                .Build(ref state);
                
            // Configura l'accesso agli effetti di stato
            _statusEffectLookup = state.GetComponentLookup<StatusEffectComponent>();
            
            // Inizializza il timer
            _effectTimer = 0;
            
            // Richiedi che ci siano aree pericolose per l'aggiornamento
            state.RequireForUpdate(_hazardsQuery);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna gli effetti delle zone pericolose
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Aggiorna il timer di effetti
            _effectTimer += deltaTime;
            
            // Aggiorna la lookup degli effetti di stato
            _statusEffectLookup.Update(ref state);
            
            // Raccogli le posizioni e dati di tutte le entità vulnerabili
            NativeArray<float3> vulnerablePositions;
            NativeArray<Entity> vulnerableEntities;
            
            if (!_vulnerableEntitiesQuery.IsEmpty)
            {
                var vulnerableTransforms = _vulnerableEntitiesQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
                vulnerableEntities = _vulnerableEntitiesQuery.ToEntityArray(Allocator.TempJob);
                vulnerablePositions = new NativeArray<float3>(vulnerableTransforms.Length, Allocator.TempJob);
                
                for (int i = 0; i < vulnerableTransforms.Length; i++)
                {
                    vulnerablePositions[i] = vulnerableTransforms[i].Position;
                }
                
                vulnerableTransforms.Dispose();
            }
            else
            {
                vulnerablePositions = new NativeArray<float3>(0, Allocator.TempJob);
                vulnerableEntities = new NativeArray<Entity>(0, Allocator.TempJob);
            }
            
            // Gestisci gli effetti delle aree pericolose
            state.Dependency = new ProcessHazardZonesJob
            {
                DeltaTime = deltaTime,
                EffectTimer = _effectTimer,
                VulnerablePositions = vulnerablePositions,
                VulnerableEntities = vulnerableEntities,
                StatusEffects = _statusEffectLookup,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_hazardsQuery, state.Dependency);
            
            // Cleanup
            state.Dependency = vulnerablePositions.Dispose(state.Dependency);
            state.Dependency = vulnerableEntities.Dispose(state.Dependency);
            
            // Aggiornamenti specifici per tipi di hazard (temporizzati)
            if (_effectTimer >= 0.5f)
            {
                _effectTimer = 0;
                
                // Gestisci effetti visivi e animazioni delle zone pericolose
                state.Dependency = new UpdateHazardVisualsJob
                {
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_hazardsQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che gestisce gli effetti delle zone pericolose sulle entità
        /// </summary>
        [BurstCompile]
        private partial struct ProcessHazardZonesJob : IJobEntity
        {
            public float DeltaTime;
            public float EffectTimer;
            [ReadOnly] public NativeArray<float3> VulnerablePositions;
            [ReadOnly] public NativeArray<Entity> VulnerableEntities;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatusEffectComponent> StatusEffects;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                in TransformComponent transform, 
                in HazardComponent hazard)
            {
                // Posizione e raggio dell'area pericolosa
                float3 hazardPosition = transform.Position;
                float hazardRadius = hazard.Radius;
                
                // Processa tutte le entità vulnerabili
                for (int i = 0; i < VulnerablePositions.Length; i++)
                {
                    float3 entityPosition = VulnerablePositions[i];
                    Entity vulnerableEntity = VulnerableEntities[i];
                    
                    // Calcola la distanza dall'area pericolosa
                    float distance = math.distance(hazardPosition, entityPosition);
                    
                    // Se l'entità è all'interno dell'area pericolosa
                    if (distance <= hazardRadius)
                    {
                        // Fattore di danno basato sulla distanza dal centro (opzionale)
                        float damageFactor = 1.0f - math.saturate(distance / hazardRadius);
                        
                        // Applica danno se necessario
                        if (hazard.IsContinuousDamage || DeltaTime < 0.02f) // Piccolo tempo per trigger di contatto
                        {
                            float damageAmount = hazard.DamagePerSecond * DeltaTime * damageFactor;
                            
                            if (damageAmount > 0)
                            {
                                // Crea evento di danno
                                Entity damageEvent = ECB.CreateEntity(sortKey);
                                ECB.AddComponent(sortKey, damageEvent, new DamageEvent
                                {
                                    Target = vulnerableEntity,
                                    Amount = damageAmount,
                                    Source = entity,
                                    DamageType = (byte)hazard.Type
                                });
                            }
                        }
                        
                        // Applica effetti di stato
                        if (hazard.StatusEffect != StatusEffectType.None)
                        {
                            // Verifica se l'entità ha già un componente effetto di stato
                            if (StatusEffects.HasComponent(vulnerableEntity))
                            {
                                var effect = StatusEffects[vulnerableEntity];
                                
                                // Aggiorna solo se l'effetto è dello stesso tipo o è scaduto
                                if (effect.Type == (byte)hazard.StatusEffect || effect.RemainingDuration <= 0)
                                {
                                    effect.Type = (byte)hazard.StatusEffect;
                                    effect.Intensity = math.max(effect.Intensity, hazard.StatusEffectIntensity);
                                    effect.RemainingDuration = math.max(effect.RemainingDuration, hazard.StatusEffectDuration);
                                    StatusEffects[vulnerableEntity] = effect;
                                }
                            }
                            else
                            {
                                // Aggiungi nuovo componente di effetto stato
                                ECB.AddComponent(sortKey, vulnerableEntity, new StatusEffectComponent
                                {
                                    Type = (byte)hazard.StatusEffect,
                                    Intensity = hazard.StatusEffectIntensity,
                                    Duration = hazard.StatusEffectDuration,
                                    RemainingDuration = hazard.StatusEffectDuration,
                                    SourceEntity = entity
                                });
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna gli effetti visivi delle zone pericolose
        /// </summary>
        [BurstCompile]
        private partial struct UpdateHazardVisualsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                in HazardComponent hazard)
            {
                // Aggiorna gli effetti visivi in base al tipo di pericolo
                switch (hazard.Type)
                {
                    case HazardType.Lava:
                        // Crea evento per effetti di lava (particelle, suoni, ecc.)
                        Entity lavaEffectEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, lavaEffectEvent, new HazardVisualEffectEvent
                        {
                            HazardEntity = entity,
                            EffectType = (byte)HazardType.Lava,
                            Intensity = 1.0f
                        });
                        break;
                        
                    case HazardType.Toxic:
                        // Crea evento per effetti tossici (nebbie, ecc.)
                        Entity toxicEffectEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, toxicEffectEvent, new HazardVisualEffectEvent
                        {
                            HazardEntity = entity,
                            EffectType = (byte)HazardType.Toxic,
                            Intensity = 0.8f
                        });
                        break;
                        
                    case HazardType.Electric:
                        // Crea evento per effetti elettrici (scariche, ecc.)
                        Entity electricEffectEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, electricEffectEvent, new HazardVisualEffectEvent
                        {
                            HazardEntity = entity,
                            EffectType = (byte)HazardType.Electric,
                            Intensity = 0.9f
                        });
                        break;
                        
                    // Altri tipi di pericoli...
                }
            }
        }
    }
    
    #region Componenti per effetti di stato
    
    /// <summary>
    /// Componente che rappresenta un effetto di stato attivo su un'entità
    /// </summary>
    [System.Serializable]
    public struct StatusEffectComponent : IComponentData
    {
        public byte Type;                 // Tipo di effetto (riferimento a StatusEffectType)
        public float Intensity;           // Intensità dell'effetto (0-1)
        public float Duration;            // Durata totale dell'effetto
        public float RemainingDuration;   // Durata rimanente
        public Entity SourceEntity;       // Entità che ha causato l'effetto
    }
    
    /// <summary>
    /// Tag per entità invulnerabili ai pericoli
    /// </summary>
    [System.Serializable]
    public struct InvulnerableTag : IComponentData {}
    
    #endregion
    
    #region Eventi
    
    /// <summary>
    /// Evento generato quando un'entità subisce danno
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        public Entity Target;         // Entità bersaglio del danno
        public float Amount;          // Quantità di danno
        public Entity Source;         // Fonte del danno
        public byte DamageType;       // Tipo di danno
    }
    
    /// <summary>
    /// Evento generato per gli effetti visivi delle zone pericolose
    /// </summary>
    public struct HazardVisualEffectEvent : IComponentData
    {
        public Entity HazardEntity;   // Entità zona pericolosa
        public byte EffectType;       // Tipo di effetto
        public float Intensity;       // Intensità dell'effetto
    }
    
    #endregion
}
