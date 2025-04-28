// Path: Assets/_Project/ECS/Systems/Abilities/AirBubbleSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Components.Enemies;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    [BurstCompile]
    public partial class AirBubbleSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _underwaterEnemyQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<AirBubbleAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<MarinaComponent>()
            );
            
            // Query per nemici acquatici
            _underwaterEnemyQuery = GetEntityQuery(
                ComponentType.ReadOnly<EnemyComponent>(),
                ComponentType.ReadOnly<UnderwaterTag>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Rimuoviamo completamente WithReadOnly poiché non possiamo usarlo con EntityQuery
            // Useremo WithoutBurst e Run, che è la soluzione più semplice
            Entities
                .WithName("AirBubbleSystem")
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref AirBubbleAbilityComponent airBubble,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in MarinaComponent marinaComponent) => 
                {
                    bool stateChanged = false;
                    
                    if (airBubble.IsActive)
                    {
                        airBubble.RemainingTime -= deltaTime;
                        
                        if (airBubble.RemainingTime <= 0)
                        {
                            airBubble.IsActive = false;
                            airBubble.RemainingTime = 0;
                            stateChanged = true;
                            
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.AirBubble
                            });
                        }
                        else
                        {
                            // Usa ToEntityArray su _underwaterEnemyQuery direttamente
                            // Non è un problema in una lambda con WithoutBurst
                            foreach (var enemy in _underwaterEnemyQuery.ToEntityArray(Allocator.Temp))
                            {
                                if (EntityManager.HasComponent<TransformComponent>(enemy) &&
                                    EntityManager.HasComponent<PhysicsComponent>(enemy))
                                {
                                    var enemyTransform = EntityManager.GetComponentData<TransformComponent>(enemy);
                                    float distance = math.distance(transform.Position, enemyTransform.Position);
                                    
                                    if (distance <= airBubble.BubbleRadius)
                                    {
                                        float3 repulsionDir = math.normalize(enemyTransform.Position - transform.Position);
                                        float repulsionStrength = airBubble.RepelForce * (1.0f - distance / airBubble.BubbleRadius);
                                        
                                        var physics = EntityManager.GetComponentData<PhysicsComponent>(enemy);
                                        physics.Velocity += repulsionDir * repulsionStrength;
                                        commandBuffer.SetComponent(entityInQueryIndex, enemy, physics);
                                        
                                        var repulsionEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                        commandBuffer.AddComponent(entityInQueryIndex, repulsionEvent, new EnemyRepulsionEvent
                                        {
                                            EnemyEntity = enemy,
                                            SourceEntity = entity,
                                            RepulsionForce = repulsionStrength,
                                            Direction = repulsionDir
                                        });
                                    }
                                }
                            }
                        }
                    }
                    
                    if (airBubble.CooldownRemaining > 0)
                    {
                        airBubble.CooldownRemaining -= deltaTime;
                        
                        if (airBubble.CooldownRemaining <= 0)
                        {
                            airBubble.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
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
                        
                        airBubble.BubbleRadius *= (1.0f + marinaComponent.WaterBreathing);
                        airBubble.RepelForce *= (1.0f + marinaComponent.ElectricResistance * 0.5f);
                        
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.AirBubble,
                            Position = transform.Position,
                            Duration = airBubble.Duration
                        });
                        
                        var bubbleEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, bubbleEntity, new AirBubbleVisualComponent
                        {
                            OwnerEntity = entity,
                            Radius = airBubble.BubbleRadius,
                            Duration = airBubble.Duration,
                            RemainingTime = airBubble.Duration
                        });
                    }
                }).Run();
            
            Entities
                .WithName("AirBubbleVisualUpdater")
                .WithoutBurst()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref AirBubbleVisualComponent bubbleVisual,
                          ref TransformComponent transform) => 
                {
                    bubbleVisual.RemainingTime -= deltaTime;
                    
                    if (bubbleVisual.RemainingTime <= 0)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                    else
                    {
                        if (EntityManager.Exists(bubbleVisual.OwnerEntity) &&
                            EntityManager.HasComponent<TransformComponent>(bubbleVisual.OwnerEntity))
                        {
                            transform.Position = EntityManager.GetComponentData<TransformComponent>(bubbleVisual.OwnerEntity).Position;
                        }
                    }
                }).Run();
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
    
    public struct EnemyRepulsionEvent : IComponentData
    {
        public Entity EnemyEntity;
        public Entity SourceEntity;
        public float RepulsionForce;
        public float3 Direction;
    }
}