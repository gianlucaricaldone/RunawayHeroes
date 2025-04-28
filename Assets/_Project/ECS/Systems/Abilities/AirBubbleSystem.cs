// Path: Assets/_Project/ECS/Systems/Abilities/AirBubbleSystem.cs
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
    /// Sistema che gestisce l'abilità "Bolla d'Aria" di Marina.
    /// Si occupa della creazione di una bolla protettiva che fornisce
    /// ossigeno extra e respinge nemici acquatici.
    /// </summary>
    public partial class AirBubbleSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _underwaterEnemyQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
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
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            Entities
                .WithName("AirBubbleSystem")
                .WithReadOnly(_underwaterEnemyQuery)
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref AirBubbleAbilityComponent airBubble,
                          in AbilityInputComponent abilityInput,
                          in TransformComponent transform,
                          in MarinaComponent marinaComponent) =>
                {
                    // Aggiorna timer e stato
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (airBubble.IsActive)
                    {
                        airBubble.RemainingTime -= deltaTime;
                        
                        if (airBubble.RemainingTime <= 0)
                        {
                            // Termina l'abilità
                            airBubble.IsActive = false;
                            airBubble.RemainingTime = 0;
                            stateChanged = true;
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.AirBubble
                            });
                        }
                        else
                        {
                            // L'abilità è attiva - respingi i nemici acquatici nelle vicinanze
                            foreach (var enemy in _underwaterEnemyQuery.ToEntityArray(Allocator.Temp))
                            {
                                if (EntityManager.HasComponent<TransformComponent>(enemy) &&
                                    EntityManager.HasComponent<PhysicsComponent>(enemy))
                                {
                                    var enemyTransform = EntityManager.GetComponentData<TransformComponent>(enemy);
                                    float distance = math.distance(transform.Position, enemyTransform.Position);
                                    
                                    // Se il nemico è nel raggio della bolla
                                    if (distance <= airBubble.BubbleRadius)
                                    {
                                        // Calcola la direzione di repulsione
                                        float3 repulsionDir = math.normalize(enemyTransform.Position - transform.Position);
                                        
                                        // Calcola la forza di repulsione (più forte quando più vicino)
                                        float repulsionStrength = airBubble.RepelForce * (1.0f - distance / airBubble.BubbleRadius);
                                        
                                        // Applica la forza di repulsione
                                        var physics = EntityManager.GetComponentData<PhysicsComponent>(enemy);
                                        physics.Velocity += repulsionDir * repulsionStrength;
                                        commandBuffer.SetComponent(entityInQueryIndex, enemy, physics);
                                        
                                        // Crea evento di repulsione
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
                    
                    // Aggiorna il cooldown
                    if (airBubble.CooldownRemaining > 0)
                    {
                        airBubble.CooldownRemaining -= deltaTime;
                        
                        if (airBubble.CooldownRemaining <= 0)
                        {
                            airBubble.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                            {
                                EntityID = entity,
                                AbilityType = AbilityType.AirBubble
                            });
                        }
                    }
                    
                    // Controlla input per attivazione abilità
                    if (abilityInput.ActivateAbility && airBubble.IsAvailable && !airBubble.IsActive)
                    {
                        // Attiva l'abilità
                        airBubble.IsActive = true;
                        airBubble.RemainingTime = airBubble.Duration;
                        airBubble.CooldownRemaining = airBubble.Cooldown;
                        
                        // Potenzia la bolla in base alle capacità di Marina
                        airBubble.BubbleRadius *= (1.0f + marinaComponent.WaterBreathing);
                        airBubble.RepelForce *= (1.0f + marinaComponent.ElectricResistance * 0.5f);
                        
                        // Crea evento di attivazione abilità
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.AirBubble,
                            Position = transform.Position,
                            Duration = airBubble.Duration
                        });
                        
                        // Crea entità visiva per la bolla d'aria
                        var bubbleEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, bubbleEntity, new AirBubbleVisualComponent
                        {
                            OwnerEntity = entity,
                            Radius = airBubble.BubbleRadius,
                            Duration = airBubble.Duration,
                            RemainingTime = airBubble.Duration
                        });
                    }
                    
                }).ScheduleParallel();
            
            // Aggiorna i visualizzatori della bolla
            Entities
                .WithName("AirBubbleVisualUpdater")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref AirBubbleVisualComponent bubbleVisual,
                          ref TransformComponent transform) =>
                {
                    // Aggiorna il tempo rimanente
                    bubbleVisual.RemainingTime -= deltaTime;
                    
                    // Se il tempo è scaduto, distruggi l'entità visiva
                    if (bubbleVisual.RemainingTime <= 0)
                    {
                        commandBuffer.DestroyEntity(entityInQueryIndex, entity);
                    }
                    else
                    {
                        // Aggiorna la posizione in base all'entità proprietaria
                        if (EntityManager.Exists(bubbleVisual.OwnerEntity) &&
                            EntityManager.HasComponent<TransformComponent>(bubbleVisual.OwnerEntity))
                        {
                            transform.Position = EntityManager.GetComponentData<TransformComponent>(bubbleVisual.OwnerEntity).Position;
                        }
                    }
                    
                }).ScheduleParallel();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    /// <summary>
    /// Componente per l'effetto visivo della bolla d'aria
    /// </summary>
    public struct AirBubbleVisualComponent : IComponentData
    {
        public Entity OwnerEntity;   // Entità proprietaria (Marina)
        public float Radius;         // Raggio della bolla
        public float Duration;       // Durata totale
        public float RemainingTime;  // Tempo rimanente
    }
    
    /// <summary>
    /// Tag per identificare i nemici acquatici
    /// </summary>
    public struct UnderwaterTag : IComponentData { }
    
    /// <summary>
    /// Evento per la repulsione di un nemico
    /// </summary>
    public struct EnemyRepulsionEvent : IComponentData
    {
        public Entity EnemyEntity;     // Entità nemica respinta
        public Entity SourceEntity;    // Entità che causa la repulsione
        public float RepulsionForce;   // Forza di repulsione
        public float3 Direction;       // Direzione di repulsione
    }
}