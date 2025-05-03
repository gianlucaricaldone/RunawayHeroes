// Path: Assets/_Project/ECS/Systems/Abilities/NatureCallSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using System;
using Random = Unity.Mathematics.Random;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Richiamo della Natura" di Maya.
    /// Si occupa dell'evocazione di animali alleati temporanei che
    /// distraggono i nemici.
    /// </summary>
    public partial struct NatureCallSystem : ISystem
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _enemyQuery;
        private EntityQuery _allyQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisci le queries usando EntityQueryBuilder
            _abilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NatureCallAbilityComponent, AbilityInputComponent, MayaComponent>()
                .Build(ref state);
            
            _enemyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyComponent>()
                .Build(ref state);
            
            _allyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NatureAllyComponent, TransformComponent>()
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
            
            // Per la prima parte del sistema che richiede accesso all'EntityManager,
            // utilizziamo una nuova architettura che divide la logica in parti
            
            // Questa è la parte che gestisce gli aggiornamenti di base dell'abilità (non richiede EntityManager)
            new NatureCallAbilityJob {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                EnemyQuery = _enemyQuery,
                // Queste operazioni devono essere eseguite nel main thread
                EntityManagerPtr = state.EntityManager.GetUnsafeEntityDataAccess(),
                EntityTypeHandle = SystemAPI.GetEntityTypeHandle()
            }.Run(state.EntityManager, _abilityQuery);
            
            // Memorizza il tempo corrente per generare movimento basato sul tempo
            float gameTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Aggiorna il comportamento degli alleati naturali
            // Qui utilizziamo Burst Compiler per massimizzare le performance
            // Questo può essere convertito in un IJobEntity perché non usa EntityManager
            new NatureAllyBehaviorJob 
            {
                DeltaTime = deltaTime,
                GameTime = gameTime,
                ECB = commandBuffer
            }.ScheduleParallel(_allyQuery, state.Dependency).Complete();
            
            // Per il secondo passaggio che richiede EntityManager, utilizziamo un approccio diverso
            // Con un job personalizzato che elabora gli alleati
            new NatureAllyTargetInteractionJob
            {
                DeltaTime = deltaTime,
                GameTime = (float)SystemAPI.Time.ElapsedTime,
                EntityManagerPtr = state.EntityManager.GetUnsafeEntityDataAccess()
            }.Run(state.EntityManager, _allyQuery);
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
        }
    }
    
    /// <summary>
    /// Componente che definisce un alleato naturale evocato da Maya
    /// </summary>
    public struct NatureAllyComponent : IComponentData
    {
        public Entity TargetEnemy;    // Il nemico che questo alleato distrae
        public Entity OwnerEntity;    // Maya, che ha evocato l'alleato
        public float Duration;        // Durata della distrazione
        public float RemainingTime;   // Tempo rimanente
    }
    
    /// <summary>
    /// Tag per identificare le entità che devono essere distrutte
    /// </summary>
    public struct DisableEntityTag : IComponentData { }
    
    /// <summary>
    /// Job per processare l'abilità NatureCall
    /// </summary>
    [BurstCompile]
    public struct NatureCallAbilityJob
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public EntityQuery EnemyQuery;
        public IntPtr EntityManagerPtr;
        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        
        public void Run(EntityManager entityManager, EntityQuery query)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            
            foreach (var entity in entities)
            {
                // Ottieni i componenti rilevanti
                var natureCall = entityManager.GetComponentData<NatureCallAbilityComponent>(entity);
                var abilityInput = entityManager.GetComponentData<AbilityInputComponent>(entity);
                var transform = entityManager.GetComponentData<TransformComponent>(entity);
                var mayaComponent = entityManager.GetComponentData<MayaComponent>(entity);
                
                int entityInQueryIndex = entity.Index; // Usato per parallellismo
                bool stateChanged = false;
                
                // Gestisci durata dell'abilità
                if (natureCall.IsActive)
                {
                    natureCall.RemainingTime -= DeltaTime;
                    
                    if (natureCall.RemainingTime <= 0)
                    {
                        // Termina l'abilità
                        natureCall.IsActive = false;
                        natureCall.RemainingTime = 0;
                        stateChanged = true;
                        
                        // Rimuovi tutti gli alleati
                        for (int i = 0; i < natureCall.CurrentAllies.Length; i++)
                        {
                            if (natureCall.CurrentAllies[i] != Entity.Null)
                            {
                                ECB.DestroyEntity(entityInQueryIndex, natureCall.CurrentAllies[i]);
                            }
                        }
                        
                        natureCall.CurrentAllies.Clear();
                        
                        // Crea evento di fine abilità
                        var endAbilityEvent = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.NatureCall
                        });
                    }
                }
                
                // Gestisci cooldown
                if (natureCall.CooldownRemaining > 0)
                {
                    natureCall.CooldownRemaining -= DeltaTime;
                    
                    if (natureCall.CooldownRemaining <= 0)
                    {
                        natureCall.CooldownRemaining = 0;
                        stateChanged = true;
                        
                        // Crea evento di abilità pronta
                        var readyEvent = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.NatureCall
                        });
                    }
                }
                
                // Attivazione dell'abilità
                if (abilityInput.ActivateAbility && natureCall.IsAvailable && !natureCall.IsActive)
                {
                    // Attiva l'abilità
                    natureCall.IsActive = true;
                    natureCall.RemainingTime = natureCall.Duration;
                    natureCall.CooldownRemaining = natureCall.Cooldown;
                    
                    // Crea evento di attivazione abilità
                    var activateEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.NatureCall,
                        Position = transform.Position,
                        Duration = natureCall.Duration
                    });
                    
                    // Determina quanti alleati evocare
                    int allyCount = math.min(natureCall.MaxAllies, (int)(mayaComponent.WildlifeAffinity * 5));
                    
                    // Cerca nemici nelle vicinanze
                    using var enemyArray = EnemyQuery.ToEntityArray(Allocator.Temp);
                    NativeList<Entity> nearbyEnemies = new NativeList<Entity>(Allocator.Temp);
                    
                    foreach (var enemy in enemyArray)
                    {
                        // Ottieni posizione del nemico
                        if (entityManager.HasComponent<TransformComponent>(enemy))
                        {
                            var enemyTransform = entityManager.GetComponentData<TransformComponent>(enemy);
                            float distance = math.distance(transform.Position, enemyTransform.Position);
                            
                            // Se il nemico è nel raggio di azione
                            if (distance <= natureCall.AllySummonRadius)
                            {
                                nearbyEnemies.Add(enemy);
                            }
                        }
                    }
                    
                    // Evoca gli alleati
                    Unity.Mathematics.Random random = Random.CreateFromIndex((uint)entityInQueryIndex);
                    for (int i = 0; i < allyCount && i < nearbyEnemies.Length; i++)
                    {
                        // Crea un'entità alleato temporanea
                        Entity allyEntity = ECB.CreateEntity(entityInQueryIndex);
                        
                        // Aggiungi componenti base
                        ECB.AddComponent(entityInQueryIndex, allyEntity, new TransformComponent
                        {
                            Position = transform.Position + new float3(
                                random.NextFloat(-3f, 3f),
                                0,
                                random.NextFloat(-3f, 3f)
                            ),
                            Rotation = quaternion.identity,
                            Scale = 1.0f
                        });
                        
                        // Aggiunge componenti specifici per l'alleato
                        ECB.AddComponent(entityInQueryIndex, allyEntity, new NatureAllyComponent
                        {
                            TargetEnemy = nearbyEnemies[i],
                            Duration = natureCall.AllyDistractDuration,
                            RemainingTime = natureCall.AllyDistractDuration,
                            OwnerEntity = entity
                        });
                        
                        // Aggiunge tagComponent
                        ECB.AddComponent(entityInQueryIndex, allyEntity, new TagComponent
                        {
                            Tag = "NatureAlly"
                        });
                        
                        // Memorizzo l'alleato nell'elenco degli alleati attivi
                        natureCall.CurrentAllies.Add(allyEntity);
                    }
                    
                    nearbyEnemies.Dispose();
                }
                
                // Salva lo stato aggiornato del componente
                entityManager.SetComponentData(entity, natureCall);
            }
            
            entities.Dispose();
        }
    }
    
    /// <summary>
    /// Job per il comportamento base degli alleati naturali
    /// </summary>
    [BurstCompile]
    public partial struct NatureAllyBehaviorJob : IJobEntity
    {
        public float DeltaTime;
        public float GameTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                    ref NatureAllyComponent ally,
                    ref TransformComponent transform)
        {
            // Aggiorna il tempo rimanente
            ally.RemainingTime -= DeltaTime;
            
            // Se il tempo è scaduto, distruggi l'alleato
            if (ally.RemainingTime <= 0)
            {
                ECB.DestroyEntity(entityInQueryIndex, entity);
                return;
            }
            
            // Creiamo un generatore di numeri casuali deterministico basato sull'entità
            uint randomSeed = (uint)(entity.Index + (uint)(GameTime * 1000));
            var random = Random.CreateFromIndex(randomSeed);
            
            // Movimento base deterministico per l'alleato basato sul tempo
            float angle = GameTime * 0.5f + entity.Index * 0.7f;
            float3 baseMovement = new float3(
                math.sin(angle) * 0.5f,
                0,
                math.cos(angle) * 0.5f
            );
            
            // Aggiungiamo un piccolo movimento casuale ma deterministico
            float3 randomMovement = new float3(
                random.NextFloat(-0.3f, 0.3f),
                0,
                random.NextFloat(-0.3f, 0.3f)
            );
            
            // Applichiamo il movimento base se non abbiamo altre informazioni
            transform.Position += (baseMovement + randomMovement) * DeltaTime;
        }
    }
    
    /// <summary>
    /// Job per l'interazione degli alleati con i target nemici
    /// </summary>
    public struct NatureAllyTargetInteractionJob
    {
        public float DeltaTime;
        public float GameTime;
        public IntPtr EntityManagerPtr;
        
        public void Run(EntityManager entityManager, EntityQuery query)
        {
            var entities = query.ToEntityArray(Allocator.Temp);
            
            foreach (var entity in entities)
            {
                // Ottieni i componenti rilevanti
                var ally = entityManager.GetComponentData<NatureAllyComponent>(entity);
                var transform = entityManager.GetComponentData<TransformComponent>(entity);
                
                // Se il nemico target è valido, l'alleato si muove verso di esso
                if (entityManager.Exists(ally.TargetEnemy) && 
                    entityManager.HasComponent<TransformComponent>(ally.TargetEnemy))
                {
                    var enemyTransform = entityManager.GetComponentData<TransformComponent>(ally.TargetEnemy);
                    float3 direction = enemyTransform.Position - transform.Position;
                    float distance = math.length(direction);
                    
                    // Se già abbastanza vicino, mantieni la distanza
                    if (distance < 1.5f)
                    {
                        // Mantieni la distanza ma continua a muoversi intorno al nemico
                        float angle = GameTime * 2.0f + entity.Index;
                        float3 orbitOffset = new float3(
                            math.sin(angle) * 1.5f,
                            0,
                            math.cos(angle) * 1.5f
                        );
                        
                        transform.Position = enemyTransform.Position + orbitOffset;
                        
                        // Se il nemico ha un componente AI, attiva lo stato di distrazione
                        if (entityManager.HasComponent<AIStateComponent>(ally.TargetEnemy))
                        {
                            var aiState = entityManager.GetComponentData<AIStateComponent>(ally.TargetEnemy);
                            UnityEngine.Debug.Log($"Nemico {ally.TargetEnemy.Index} distratto da alleato naturale!");
                        }
                    }
                    else
                    {
                        // Muovi verso il nemico
                        direction = math.normalize(direction);
                        transform.Position += direction * 3.0f * DeltaTime; // velocità alleato
                    }
                }
                // Se il target non è più valido, resta vicino all'owner (Maya)
                else if (entityManager.Exists(ally.OwnerEntity) &&
                        entityManager.HasComponent<TransformComponent>(ally.OwnerEntity))
                {
                    var ownerPos = entityManager.GetComponentData<TransformComponent>(ally.OwnerEntity).Position;
                    float ownerDist = math.distance(transform.Position, ownerPos);
                    
                    if (ownerDist > 10f)
                    {
                        // Muovi verso il proprietario se troppo lontano
                        float3 toOwner = math.normalize(ownerPos - transform.Position);
                        transform.Position += toOwner * 2.0f * DeltaTime;
                    }
                }
                
                // Salva il transform aggiornato
                entityManager.SetComponentData(entity, transform);
            }
            
            entities.Dispose();
        }
    }
}