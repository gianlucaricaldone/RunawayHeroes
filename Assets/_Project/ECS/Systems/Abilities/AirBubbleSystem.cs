// Path: Assets/_Project/ECS/Systems/Abilities/AirBubbleSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct AirBubbleSystem : ISystem
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _underwaterEnemyQuery;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            _abilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AirBubbleAbilityComponent, AbilityInputComponent, MarinaComponent>()
                .Build(ref state);
            
            // Query per nemici acquatici
            _underwaterEnemyQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyComponent, UnderwaterTag>()
                .Build(ref state);
            
            state.RequireForUpdate(_abilityQuery);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni la lista di nemici subacquei prima di entrare nel job
            var underwaterEnemies = _underwaterEnemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyPositions = new NativeArray<float3>(underwaterEnemies.Length, Allocator.TempJob);
            var enemyPhysics = new NativeArray<PhysicsComponent>(underwaterEnemies.Length, Allocator.TempJob);
            
            // Popola le posizioni e i dati fisici dei nemici
            for (int i = 0; i < underwaterEnemies.Length; i++)
            {
                if (state.EntityManager.HasComponent<TransformComponent>(underwaterEnemies[i]))
                {
                    enemyPositions[i] = state.EntityManager.GetComponentData<TransformComponent>(underwaterEnemies[i]).Position;
                }
                
                if (state.EntityManager.HasComponent<PhysicsComponent>(underwaterEnemies[i]))
                {
                    enemyPhysics[i] = state.EntityManager.GetComponentData<PhysicsComponent>(underwaterEnemies[i]);
                }
            }
            
            // Aggiornamento dell'abilità e interazione con i nemici
            state.Dependency = new AirBubbleUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                UnderwaterEnemies = underwaterEnemies,
                EnemyPositions = enemyPositions,
                EnemyPhysics = enemyPhysics
            }.ScheduleParallel(state.Dependency);
            
            // Aggiornamento degli effetti visivi - non è possibile usare Burst a causa di EntityManager
            state.Dependency = new AirBubbleVisualUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                EntityManager = state.EntityManager
            }.Schedule(state.Dependency);
            
            // Cleanup delle risorse native
            state.Dependency = underwaterEnemies.Dispose(state.Dependency);
            state.Dependency = enemyPositions.Dispose(state.Dependency);
            state.Dependency = enemyPhysics.Dispose(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct AirBubbleUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [ReadOnly] public NativeArray<Entity> UnderwaterEnemies;
            [ReadOnly] public NativeArray<float3> EnemyPositions;
            [ReadOnly] public NativeArray<PhysicsComponent> EnemyPhysics;
            
            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                ref AirBubbleAbilityComponent airBubble,
                in AbilityInputComponent abilityInput,
                in TransformComponent transform,
                in MarinaComponent marinaComponent)
            {
                bool stateChanged = false;
                
                if (airBubble.IsActive)
                {
                    airBubble.RemainingTime -= DeltaTime;
                    
                    if (airBubble.RemainingTime <= 0)
                    {
                        airBubble.IsActive = false;
                        airBubble.RemainingTime = 0;
                        stateChanged = true;
                        
                        var endAbilityEvent = ECB.CreateEntity(chunkIndexInQuery);
                        ECB.AddComponent(chunkIndexInQuery, endAbilityEvent, new AbilityEndedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.AirBubble
                        });
                    }
                    else
                    {
                        // Usiamo i dati precalcolati
                        for (int i = 0; i < UnderwaterEnemies.Length; i++)
                        {
                            float3 enemyPosition = EnemyPositions[i];
                            float distance = math.distance(transform.Position, enemyPosition);
                            
                            if (distance <= airBubble.BubbleRadius)
                            {
                                float3 repulsionDir = math.normalize(enemyPosition - transform.Position);
                                float repulsionStrength = airBubble.RepelForce * (1.0f - distance / airBubble.BubbleRadius);
                                
                                var physics = EnemyPhysics[i];
                                physics.Velocity += repulsionDir * repulsionStrength;
                                ECB.SetComponent(chunkIndexInQuery, UnderwaterEnemies[i], physics);
                                
                                var repulsionEvent = ECB.CreateEntity(chunkIndexInQuery);
                                ECB.AddComponent(chunkIndexInQuery, repulsionEvent, new EnemyRepulsionEvent
                                {
                                    EnemyEntity = UnderwaterEnemies[i],
                                    SourceEntity = entity,
                                    RepulsionForce = repulsionStrength,
                                    Direction = repulsionDir
                                });
                            }
                        }
                    }
                }
                
                if (airBubble.CooldownRemaining > 0)
                {
                    airBubble.CooldownRemaining -= DeltaTime;
                    
                    if (airBubble.CooldownRemaining <= 0)
                    {
                        airBubble.CooldownRemaining = 0;
                        stateChanged = true;
                        
                        var readyEvent = ECB.CreateEntity(chunkIndexInQuery);
                        ECB.AddComponent(chunkIndexInQuery, readyEvent, new AbilityReadyEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.AirBubble
                        });
                    }
                }
                
                if (abilityInput.ActivateAbility && airBubble.IsAvailable && !airBubble.IsActive)
                {
                    airBubble.IsActive = true;
                    airBubble.RemainingTime = airBubble.Duration;
                    airBubble.CooldownRemaining = airBubble.Cooldown;
                    
                    // Applicazione dei bonus di Marina
                    airBubble.BubbleRadius *= (1.0f + marinaComponent.WaterBreathing);
                    airBubble.RepelForce *= (1.0f + marinaComponent.ElectricResistance * 0.5f);
                    
                    var activateEvent = ECB.CreateEntity(chunkIndexInQuery);
                    ECB.AddComponent(chunkIndexInQuery, activateEvent, new AbilityActivatedEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.AirBubble,
                        Position = transform.Position,
                        Duration = airBubble.Duration
                    });
                    
                    var bubbleEntity = ECB.CreateEntity(chunkIndexInQuery);
                    ECB.AddComponent(chunkIndexInQuery, bubbleEntity, new AirBubbleVisualComponent
                    {
                        OwnerEntity = entity,
                        Radius = airBubble.BubbleRadius,
                        Duration = airBubble.Duration,
                        RemainingTime = airBubble.Duration
                    });
                }
            }
        }
        
        private partial struct AirBubbleVisualUpdateJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
            public EntityManager EntityManager;
            
            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                ref AirBubbleVisualComponent bubbleVisual,
                ref TransformComponent transform)
            {
                bubbleVisual.RemainingTime -= DeltaTime;
                
                if (bubbleVisual.RemainingTime <= 0)
                {
                    ECB.DestroyEntity(chunkIndexInQuery, entity);
                }
                else
                {
                    if (EntityManager.Exists(bubbleVisual.OwnerEntity) &&
                        EntityManager.HasComponent<TransformComponent>(bubbleVisual.OwnerEntity))
                    {
                        transform.Position = EntityManager.GetComponentData<TransformComponent>(bubbleVisual.OwnerEntity).Position;
                    }
                }
            }
        }
    }
    
    public struct AirBubbleVisualComponent : IComponentData
    {
        public Entity OwnerEntity;
        public float Radius;
        public float Duration;
        public float RemainingTime;
    }
    
    public struct UnderwaterTag : IComponentData { }
}