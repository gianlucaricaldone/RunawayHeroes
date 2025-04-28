// Path: Assets/_Project/ECS/Systems/Abilities/NatureCallSystem.cs
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
    /// Sistema che gestisce l'abilità "Richiamo della Natura" di Maya.
    /// Si occupa dell'evocazione di animali alleati temporanei che
    /// distraggono i nemici.
    /// </summary>
    public partial class NatureCallSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _enemyQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<NatureCallAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<MayaComponent>()
            );
            
            _enemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyComponent>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            Entities
                .WithName("NatureCallSystem")
                .WithReadOnly(_enemyQuery)
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
                        NativeList<Entity> nearbyEnemies = new NativeList<Entity>(Allocator.Temp);
                        foreach (var enemy in _enemyQuery.ToEntityArray(Allocator.Temp))
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
                        for (int i = 0; i < allyCount && i < nearbyEnemies.Length; i++)
                        {
                            // Crea un'entità alleato temporanea
                            Entity allyEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            
                            // Aggiungi componenti base
                            commandBuffer.AddComponent(entityInQueryIndex, allyEntity, new TransformComponent
                            {
                                Position = transform.Position + new float3(
                                    UnityEngine.Random.Range(-3f, 3f),
                                    0,
                                    UnityEngine.Random.Range(-3f, 3f)
                                ),
                                Rotation = quaternion.identity,
                                Scale = 1.0f
                            });
                            
                            // Aggiunge componenti specifici per l'alleato
                            commandBuffer.AddComponent(entityInQueryIndex, allyEntity, new NatureAllyComponent
                            {
                                TargetEnemy = nearbyEnemies[i],
                                Duration = natureCall.AllyDistractDuration,
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
                    
                }).ScheduleParallel();
            
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
}