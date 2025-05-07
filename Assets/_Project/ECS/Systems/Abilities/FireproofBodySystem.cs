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
using RunawayHeroes.ECS.Components.World.Obstacles;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Corpo Ignifugo" di Ember.
    /// Si occupa della trasformazione in forma ignea che permette
    /// di attraversare la lava e resistere al calore estremo.
    /// </summary>
    public partial struct FireproofBodySystem : ISystem
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _lavaQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisci la query per l'abilità usando EntityQueryBuilder
            _abilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<FireproofBodyAbilityComponent, AbilityInputComponent, EmberComponent>()
                .Build(ref state);
            
            // Query per zone di lava
            _lavaQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HazardComponent, LavaTag>()
                .Build(ref state);
            
            // Richiedi entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_abilityQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni le zone di lava prima di entrare nel job
            var lavaHazards = _lavaQuery.ToEntityArray(Allocator.TempJob);
            var lavaPositions = new NativeArray<float3>(lavaHazards.Length, Allocator.TempJob);
            var lavaRadii = new NativeArray<float>(lavaHazards.Length, Allocator.TempJob);
            var hasRadiusData = new NativeArray<bool>(lavaHazards.Length, Allocator.TempJob);
            
            // Popola i dati delle zone di lava
            for (int i = 0; i < lavaHazards.Length; i++)
            {
                if (state.EntityManager.HasComponent<TransformComponent>(lavaHazards[i]))
                {
                    lavaPositions[i] = state.EntityManager.GetComponentData<TransformComponent>(lavaHazards[i]).Position;
                }
                
                if (state.EntityManager.HasComponent<HazardComponent>(lavaHazards[i]))
                {
                    var hazard = state.EntityManager.GetComponentData<HazardComponent>(lavaHazards[i]);
                    lavaRadii[i] = hazard.Radius;
                    hasRadiusData[i] = true;
                }
                else
                {
                    hasRadiusData[i] = false;
                }
            }
            
            // Usa un IJobEntity invece di Entities.ForEach
            new FireproofBodyJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                LavaHazards = lavaHazards,
                LavaPositions = lavaPositions,
                LavaRadii = lavaRadii,
                HasRadiusData = hasRadiusData
            }.ScheduleParallel(_abilityQuery, state.Dependency).Complete();
            
            
            // Ottieni la query per le visualizzazioni del corpo ignifugo
            var fireBodyVisualQuery = SystemAPI.QueryBuilder()
                .WithAll<FireBodyVisualComponent, TransformComponent>()
                .Build();
                
            // Aggiorna i visualizzatori dell'effetto igneo con un IJobEntity
            state.Dependency = new FireBodyVisualUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                TransformLookup = state.GetComponentLookup<TransformComponent>(true)
            }.ScheduleParallel(fireBodyVisualQuery, state.Dependency);
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
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
    
    // Nota: LavaTag è ora definito in RunawayHeroes.ECS.Components.World.Obstacles.LavaTag
    
    /// <summary>
    /// Evento per l'attraversamento della lava
    /// </summary>
    public struct LavaWalkingEvent : IComponentData
    {
        public Entity EntityID;  // Entità che cammina sulla lava
        public float3 Position;  // Posizione dell'attraversamento
    }
    
    /// <summary>
    /// Job per elaborare l'abilità Fireproof Body
    /// </summary>
    [BurstCompile]
    public partial struct FireproofBodyJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public NativeArray<Entity> LavaHazards;
        [ReadOnly] public NativeArray<float3> LavaPositions;
        [ReadOnly] public NativeArray<float> LavaRadii;
        [ReadOnly] public NativeArray<bool> HasRadiusData;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                          ref FireproofBodyAbilityComponent fireproofBody,
                          ref HealthComponent health,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in EmberComponent emberComponent)
        {
            // Aggiorna timer e stato
            bool stateChanged = false;
            
            // Se l'abilità è attiva, gestisci la durata
            if (fireproofBody.IsActive)
            {
                fireproofBody.RemainingTime -= DeltaTime;
                
                if (fireproofBody.RemainingTime <= 0)
                {
                    // Termina l'abilità
                    fireproofBody.IsActive = false;
                    fireproofBody.RemainingTime = 0;
                    stateChanged = true;
                    
                    // Disattiva immunità alla lava
                    fireproofBody.LavaWalkingActive = false;
                    
                    // Crea evento di fine abilità
                    var endAbilityEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
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
                    for (int i = 0; i < LavaHazards.Length; i++)
                    {
                        if (!HasRadiusData[i])
                            continue;
                        
                        float distance = math.distance(transform.Position, LavaPositions[i]);
                        
                        // Se il giocatore è dentro la zona di lava
                        if (distance <= LavaRadii[i])
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
                        var lavaWalkEvent = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, lavaWalkEvent, new LavaWalkingEvent
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
                fireproofBody.CooldownRemaining -= DeltaTime;
                
                if (fireproofBody.CooldownRemaining <= 0)
                {
                    fireproofBody.CooldownRemaining = 0;
                    stateChanged = true;
                    
                    // Crea evento di abilità pronta
                    var readyEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
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
                var activateEvent = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                {
                    EntityID = entity,
                    AbilityType = AbilityType.FireproofBody,
                    Position = transform.Position,
                    Duration = fireproofBody.Duration
                });
                
                // Crea entità visiva per la trasformazione ignea
                var visualEntity = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, visualEntity, new FireBodyVisualComponent
                {
                    OwnerEntity = entity,
                    Duration = fireproofBody.Duration,
                    RemainingTime = fireproofBody.Duration
                });
            }
        }
    }
    
    /// <summary>
    /// Job per aggiornare le visualizzazioni del corpo ignifugo
    /// </summary>
    [BurstCompile]
    public partial struct FireBodyVisualUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        // Lookup per il componente TransformComponent delle entità proprietarie
        [ReadOnly] public ComponentLookup<TransformComponent> TransformLookup;
        
        // Metodo Execute chiamato per ogni entità nel query
        public void Execute([ChunkIndexInQuery] int chunkIndexInQuery, 
                           Entity entity,
                           ref FireBodyVisualComponent fireVisual,
                           ref TransformComponent transform)
        {
            // Aggiorna il tempo rimanente
            fireVisual.RemainingTime -= DeltaTime;
            
            // Se il tempo è scaduto, distruggi l'entità visiva
            if (fireVisual.RemainingTime <= 0)
            {
                ECB.DestroyEntity(chunkIndexInQuery, entity);
            }
            else
            {
                // Aggiorna la posizione in base all'entità proprietaria
                if (fireVisual.OwnerEntity != Entity.Null && 
                    TransformLookup.HasComponent(fireVisual.OwnerEntity))
                {
                    transform.Position = TransformLookup[fireVisual.OwnerEntity].Position;
                }
            }
        }
    }
}