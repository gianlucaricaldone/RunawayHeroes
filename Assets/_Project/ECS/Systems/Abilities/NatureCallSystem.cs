// Path: Assets/_Project/ECS/Systems/Abilities/NatureCallSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
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
    [BurstCompile]
    public partial class NatureCallSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _enemyQuery;
        private EntityQuery _allyQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<NatureCallAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<MayaComponent>()
            );
            
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyComponent>()
            );
            
            _allyQuery = GetEntityQuery(
                ComponentType.ReadWrite<NatureAllyComponent>(),
                ComponentType.ReadWrite<TransformComponent>()
            );
            
            RequireForUpdate(_abilityQuery);
        }

        [BurstCompile]        
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Per questo sistema, è più semplice usare WithoutBurst e Run,
            // perché deve creare nuove entità con UnityEngine.Random e lavora con dati dinamici
            // Quando si creano entità in base all'input, la semplicità supera l'ottimizzazione
            Entities
                .WithName("NatureCallAbilityProcessor")
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref NatureCallAbilityComponent natureCall,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in MayaComponent mayaComponent) =>
                {
                    // Aggiorna timer e stato
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (natureCall.IsActive)
                    {
                        natureCall.RemainingTime -= deltaTime;
                        
                        if (natureCall.RemainingTime <= 0)
                        {
                            // Termina l'abilità
                            natureCall.IsActive = false;
                            natureCall.RemainingTime = 0;
                            stateChanged = true;
                            
                            // Rimuovi tutti gli alleati (sono entità temporanee)
                            for (int i = 0; i < natureCall.CurrentAllies.Length; i++)
                            {
                                if (natureCall.CurrentAllies[i] != Entity.Null)
                                {
                                    commandBuffer.DestroyEntity(entityInQueryIndex, natureCall.CurrentAllies[i]);
                                }
                            }
                            
                            natureCall.CurrentAllies.Clear();
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.NatureCall
                            });
                        }
                    }
                    
                    // Aggiorna il cooldown
                    if (natureCall.CooldownRemaining > 0)
                    {
                        natureCall.CooldownRemaining -= deltaTime;
                        
                        if (natureCall.CooldownRemaining <= 0)
                        {
                            natureCall.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.NatureCall
                            });
                        }
                    }
                    
                    // Controlla input per attivazione abilità
                    if (abilityInput.ActivateAbility && natureCall.IsAvailable && !natureCall.IsActive)
                    {
                        // Attiva l'abilità
                        natureCall.IsActive = true;
                        natureCall.RemainingTime = natureCall.Duration;
                        natureCall.CooldownRemaining = natureCall.Cooldown;
                        
                        // Crea evento di attivazione abilità
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.NatureCall,
                            Position = transform.Position,
                            Duration = natureCall.Duration
                        });
                        
                        // Determina quanti alleati evocare (in base alla forza dell'abilità)
                        int allyCount = math.min(natureCall.MaxAllies, (int)(mayaComponent.WildlifeAffinity * 5));
                        
                        // Cerca nemici nelle vicinanze
                        using var enemyArray = _enemyQuery.ToEntityArray(Allocator.Temp);
                        NativeList<Entity> nearbyEnemies = new NativeList<Entity>(Allocator.Temp);
                        
                        foreach (var enemy in enemyArray)
                        {
                            // Ottieni posizione del nemico
                            if (EntityManager.HasComponent<TransformComponent>(enemy))
                            {
                                var enemyTransform = EntityManager.GetComponentData<TransformComponent>(enemy);
                                float distance = math.distance(transform.Position, enemyTransform.Position);
                                
                                // Se il nemico è nel raggio di azione
                                if (distance <= natureCall.AllySummonRadius)
                                {
                                    nearbyEnemies.Add(enemy);
                                }
                            }
                        }
                        
                        // Evoca gli alleati
                        Random random = Random.CreateFromIndex((uint)entityInQueryIndex);
                        for (int i = 0; i < allyCount && i < nearbyEnemies.Length; i++)
                        {
                            // Crea un'entità alleato temporanea
                            Entity allyEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            
                            // Aggiungi componenti base
                            commandBuffer.AddComponent(entityInQueryIndex, allyEntity, new TransformComponent
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
                            commandBuffer.AddComponent(entityInQueryIndex, allyEntity, new NatureAllyComponent
                            {
                                TargetEnemy = nearbyEnemies[i],
                                Duration = natureCall.AllyDistractDuration,
                                RemainingTime = natureCall.AllyDistractDuration,
                                OwnerEntity = entity
                            });
                            
                            // Aggiunge tagComponent
                            commandBuffer.AddComponent(entityInQueryIndex, allyEntity, new TagComponent
                            {
                                Tag = "NatureAlly"
                            });
                            
                            // Memorizzo l'alleato nell'elenco degli alleati attivi
                            natureCall.CurrentAllies.Add(allyEntity);
                        }
                        
                        nearbyEnemies.Dispose();
                    }
                    
                }).Run();
            
            // Memorizza il tempo corrente per generare movimento basato sul tempo
            float gameTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Aggiorna il comportamento degli alleati naturali
            // Qui utilizziamo Burst Compiler per massimizzare le performance
            Entities
                .WithName("NatureAllyBaseBehavior")
                .WithAll<NatureAllyComponent, TransformComponent>()
                .ForEach((Entity entity, int entityInQueryIndex,
                         ref NatureAllyComponent ally,
                         ref TransformComponent transform) =>
                {
                    // Aggiorna il tempo rimanente
                    ally.RemainingTime -= deltaTime;
                    
                    // Se il tempo è scaduto, distruggi l'alleato
                    if (ally.RemainingTime <= 0)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                        return;
                    }
                    
                    // Creiamo un generatore di numeri casuali deterministico basato sull'entità
                    uint randomSeed = (uint)(entity.Index + (uint)(gameTime * 1000));
                    var random = Random.CreateFromIndex(randomSeed);
                    
                    // Le operazioni qui sono limitate a calcoli matematici compatibili con Burst
                    
                    // Movimento base deterministico per l'alleato basato sul tempo
                    float angle = gameTime * 0.5f + entity.Index * 0.7f;
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
                    transform.Position += (baseMovement + randomMovement) * deltaTime;
                    
                }).ScheduleParallel();
            
            // Un secondo passaggio non-Burst per l'interazione con altri sistemi EntityManager-based
            // Questo mantiene la compatibilità con Burst del loop precedente
            Entities
                .WithName("NatureAllyTargetInteraction")
                .WithoutBurst()
                .WithNone<DisableEntityTag>() // Usiamo questo tag per contrassegnare alleati già distrutti
                .ForEach((Entity entity, int entityInQueryIndex,
                         ref NatureAllyComponent ally,
                         ref TransformComponent transform) =>
                {
                    // Se il nemico target è valido, l'alleato si muove verso di esso
                    if (EntityManager.Exists(ally.TargetEnemy) && 
                        EntityManager.HasComponent<TransformComponent>(ally.TargetEnemy))
                    {
                        var enemyTransform = EntityManager.GetComponentData<TransformComponent>(ally.TargetEnemy);
                        float3 direction = enemyTransform.Position - transform.Position;
                        float distance = math.length(direction);
                        
                        // Se già abbastanza vicino, mantieni la distanza
                        if (distance < 1.5f)
                        {
                            // Mantieni la distanza ma continua a muoversi intorno al nemico
                            float angle = (float)SystemAPI.Time.ElapsedTime * 2.0f + entity.Index; // ogni alleato orbita in modo diverso
                            float3 orbitOffset = new float3(
                                math.sin(angle) * 1.5f,
                                0,
                                math.cos(angle) * 1.5f
                            );
                            
                            transform.Position = enemyTransform.Position + orbitOffset;
                            
                            // Se il nemico ha un componente AI, attiva lo stato di distrazione
                            if (EntityManager.HasComponent<AIStateComponent>(ally.TargetEnemy))
                            {
                                var aiState = EntityManager.GetComponentData<AIStateComponent>(ally.TargetEnemy);
                                
                                // Qui potresti impostare uno stato "distracted" nell'AI del nemico
                                // Per ora lo simuliamo semplicemente con debug
                                UnityEngine.Debug.Log($"Nemico {ally.TargetEnemy.Index} distratto da alleato naturale!");
                                
                                // In un'implementazione completa, modificheresti lo stato AI qui
                                // Per ora, non facciamo nulla poiché AIStateComponent non è completamente implementato
                            }
                        }
                        else
                        {
                            // Muovi verso il nemico
                            direction = math.normalize(direction);
                            transform.Position += direction * 3.0f * deltaTime; // velocità alleato
                        }
                    }
                    // Se il target non è più valido, resta vicino all'owner (Maya)
                    else if (EntityManager.Exists(ally.OwnerEntity) &&
                             EntityManager.HasComponent<TransformComponent>(ally.OwnerEntity))
                    {
                        var ownerPos = EntityManager.GetComponentData<TransformComponent>(ally.OwnerEntity).Position;
                        float ownerDist = math.distance(transform.Position, ownerPos);
                        
                        if (ownerDist > 10f)
                        {
                            // Muovi verso il proprietario se troppo lontano
                            float3 toOwner = math.normalize(ownerPos - transform.Position);
                            transform.Position += toOwner * 2.0f * deltaTime;
                        }
                    }
                }).Run();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
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
}