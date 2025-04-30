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
    public partial class AirBubbleSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _underwaterEnemyQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = SystemAPI.QueryBuilder()
                .WithAll<AirBubbleAbilityComponent, AbilityInputComponent, MarinaComponent>()
                .Build();
            
            // Query per nemici acquatici
            _underwaterEnemyQuery = SystemAPI.QueryBuilder()
                .WithAll<EnemyComponent, UnderwaterTag>()
                .Build();
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni la lista di nemici subacquei prima di entrare nel job
            var underwaterEnemies = _underwaterEnemyQuery.ToEntityArray(Allocator.TempJob);
            var enemyPositions = new NativeArray<float3>(underwaterEnemies.Length, Allocator.TempJob);
            var enemyPhysics = new NativeArray<PhysicsComponent>(underwaterEnemies.Length, Allocator.TempJob);
            
            // Popola le posizioni e i dati fisici dei nemici
            for (int i = 0; i < underwaterEnemies.Length; i++)
            {
                if (EntityManager.HasComponent<TransformComponent>(underwaterEnemies[i]))
                {
                    enemyPositions[i] = EntityManager.GetComponentData<TransformComponent>(underwaterEnemies[i]).Position;
                }
                
                if (EntityManager.HasComponent<PhysicsComponent>(underwaterEnemies[i]))
                {
                    enemyPhysics[i] = EntityManager.GetComponentData<PhysicsComponent>(underwaterEnemies[i]);
                }
            }
            
            // Aggiornamento dell'abilità e interazione con i nemici
            new AirBubbleUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                UnderwaterEnemies = underwaterEnemies,
                EnemyPositions = enemyPositions,
                EnemyPhysics = enemyPhysics
            }.ScheduleParallel();
            
            // Aggiornamento degli effetti visivi - non è possibile usare Burst a causa di EntityManager
            new AirBubbleVisualUpdateJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                EntityManager = EntityManager
            }.Run();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
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
                [EntityIndexInQuery] int entityIndexInQuery,
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
                        
                        var endAbilityEvent = ECB.CreateEntity(entityIndexInQuery);
                        ECB.AddComponent(entityIndexInQuery, endAbilityEvent, new AbilityEndedEvent
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
                                ECB.SetComponent(entityIndexInQuery, UnderwaterEnemies[i], physics);
                                
                                var repulsionEvent = ECB.CreateEntity(entityIndexInQuery);
                                ECB.AddComponent(entityIndexInQuery, repulsionEvent, new EnemyRepulsionEvent
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
                        
                        var readyEvent = ECB.CreateEntity(entityIndexInQuery);
                        ECB.AddComponent(entityIndexInQuery, readyEvent, new AbilityReadyEvent
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
                    
                    var activateEvent = ECB.CreateEntity(entityIndexInQuery);
                    ECB.AddComponent(entityIndexInQuery, activateEvent, new AbilityActivatedEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.AirBubble,
                        Position = transform.Position,
                        Duration = airBubble.Duration
                    });
                    
                    var bubbleEntity = ECB.CreateEntity(entityIndexInQuery);
                    ECB.AddComponent(entityIndexInQuery, bubbleEntity, new AirBubbleVisualComponent
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
                [EntityIndexInQuery] int entityIndexInQuery,
                ref AirBubbleVisualComponent bubbleVisual,
                ref TransformComponent transform)
            {
                bubbleVisual.RemainingTime -= DeltaTime;
                
                if (bubbleVisual.RemainingTime <= 0)
                {
                    ECB.DestroyEntity(entityIndexInQuery, entity);
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