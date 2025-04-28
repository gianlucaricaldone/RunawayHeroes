using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce le collisioni tra il giocatore e gli ostacoli.
    /// Rileva collisioni, genera eventi appropriati e applica gli effetti delle collisioni.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial class ObstacleCollisionSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EntityQuery _obstacleQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Costanti di configurazione
        private const float OBSTACLE_DAMAGE_MULTIPLIER = 10.0f;  // Moltiplicatore del danno basato sulla velocità
        private const float MIN_IMPACT_VELOCITY = 2.0f;          // Velocità minima per considerare un impatto
        private const float SMALL_OBSTACLE_THRESHOLD = 0.5f;     // Soglia per definire un ostacolo piccolo
        
        protected override void OnCreate()
        {
            // Ottiene il sistema di command buffer per le modifiche strutturali
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Definisce la query per identificare le entità giocatore
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<TagComponent>(),
                ComponentType.ReadOnly<TransformComponent>(),
                ComponentType.ReadOnly<PhysicsComponent>(),
                ComponentType.ReadWrite<HealthComponent>()
            );
            
            // Definisce la query per identificare gli ostacoli
            _obstacleQuery = GetEntityQuery(
                ComponentType.ReadOnly<ObstacleComponent>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Richiede che ci siano sia giocatori che ostacoli per l'esecuzione
            RequireForUpdate(_playerQuery);
            RequireForUpdate(_obstacleQuery);
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            // Prepara il command buffer per le modifiche strutturali
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottiene le entità di ostacoli per questo frame
            NativeArray<Entity> obstacles = _obstacleQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<ObstacleComponent> obstacleComponents = _obstacleQuery.ToComponentDataArray<ObstacleComponent>(Allocator.TempJob);
            NativeArray<TransformComponent> obstacleTransforms = _obstacleQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Elabora le collisioni per ogni giocatore
            Entities
                .WithName("ProcessPlayerObstacleCollisions")
                .WithReadOnly(obstacles)
                .WithReadOnly(obstacleComponents)
                .WithReadOnly(obstacleTransforms)
                .WithAll<TagComponent>()
                .ForEach((Entity playerEntity, int entityInQueryIndex,
                          ref HealthComponent health,
                          in TransformComponent transform,
                          in PhysicsComponent physics,
                          in MovementComponent movement) =>
                {
                    // Salta se il giocatore è invulnerabile
                    if (health.IsInvulnerable)
                        return;
                    
                    // Verifica collisioni con tutti gli ostacoli
                    for (int i = 0; i < obstacles.Length; i++)
                    {
                        Entity obstacleEntity = obstacles[i];
                        ObstacleComponent obstacle = obstacleComponents[i];
                        TransformComponent obstacleTransform = obstacleTransforms[i];
                        
                        // Controllo collisione semplificato (box vs box)
                        if (CheckCollision(transform, obstacleTransform, obstacle.CollisionRadius))
                        {
                            // Calcola i dettagli della collisione
                            float3 impactPosition = CalculateImpactPosition(transform.Position, obstacleTransform.Position);
                            float3 impactNormal = math.normalize(transform.Position - obstacleTransform.Position);
                            float impactVelocity = math.dot(physics.Velocity, -impactNormal);
                            
                            // Ignora impatti a bassa velocità (sfioramenti)
                            if (impactVelocity < MIN_IMPACT_VELOCITY)
                                continue;
                            
                            // Determina il danno basato sulla velocità e tipo di ostacolo
                            float baseDamage = obstacle.DamageValue;
                            if (baseDamage <= 0)
                            {
                                // Se il danno non è specificato, calcolalo dalla velocità
                                baseDamage = impactVelocity * OBSTACLE_DAMAGE_MULTIPLIER;
                            }
                            
                            // Gestione della scivolata e dell'abilità Scatto Urbano
                            bool canBreakThrough = false;
                            
                            // Verifica se il giocatore può sfondare l'ostacolo
                            // (ad esempio con Scatto Urbano o se l'ostacolo è piccolo)
                            if (movement.IsSliding && obstacle.Height < SMALL_OBSTACLE_THRESHOLD)
                            {
                                canBreakThrough = true;
                            }
                            // Altri controlli per abilità speciali possono essere aggiunti qui
                            
                            // Se non può sfondare, applica danno e genera evento
                            if (!canBreakThrough)
                            {
                                // Applica il danno
                                float actualDamage = health.ApplyDamage(baseDamage);
                                
                                // Genera evento di collisione con ostacolo
                                var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new ObstacleHitEvent
                                {
                                    PlayerEntity = playerEntity,
                                    ObstacleEntity = obstacleEntity,
                                    ImpactPosition = impactPosition,
                                    ImpactNormal = impactNormal,
                                    ImpactVelocity = impactVelocity,
                                    DamageAmount = actualDamage
                                });
                                
                                // Applica invulnerabilità temporanea per evitare danni multipli dallo stesso ostacolo
                                health.SetInvulnerable(1.0f);
                                
                                // Genera anche un evento di danno generico
                                var damageEventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, damageEventEntity, new DamageEvent
                                {
                                    TargetEntity = playerEntity,
                                    SourceEntity = obstacleEntity,
                                    DamageAmount = actualDamage,
                                    DamageType = DamageType.Obstacle,
                                    ImpactPosition = impactPosition
                                });
                            }
                            else
                            {
                                // Genera evento di attraversamento ostacolo
                                var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new ObstacleBreakThroughEvent
                                {
                                    PlayerEntity = playerEntity,
                                    ObstacleEntity = obstacleEntity,
                                    BreakThroughPosition = impactPosition
                                });
                            }
                            
                            // Interrompe il controllo degli altri ostacoli se ha già colpito uno
                            break;
                        }
                    }
                }).ScheduleParallel();
            
            // Aggiungi il job handle per la produzione del command buffer
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
            
            // Rilascia le risorse temporanee
            Dependency = obstacles.Dispose(Dependency);
            Dependency = obstacleComponents.Dispose(Dependency);
            Dependency = obstacleTransforms.Dispose(Dependency);
        }
        
        /// <summary>
        /// Verifica se c'è una collisione tra giocatore e ostacolo
        /// </summary>
        private bool CheckCollision(TransformComponent playerTransform, TransformComponent obstacleTransform, float obstacleRadius)
        {
            // Implementazione semplificata della collisione sferica
            const float PLAYER_RADIUS = 0.5f; // Raggio di collisione del giocatore
            
            float3 playerPos = playerTransform.Position;
            float3 obstaclePos = obstacleTransform.Position;
            
            // Calcola la distanza al quadrato (più efficiente del calcolo della distanza)
            float distanceSq = math.distancesq(playerPos, obstaclePos);
            
            // Somma dei raggi
            float radiusSum = PLAYER_RADIUS + obstacleRadius;
            
            // Collisione se la distanza è minore della somma dei raggi
            return distanceSq < (radiusSum * radiusSum);
        }
        
        /// <summary>
        /// Calcola la posizione di impatto tra giocatore e ostacolo
        /// </summary>
        private float3 CalculateImpactPosition(float3 playerPos, float3 obstaclePos)
        {
            // Posizione a metà strada tra i due oggetti
            return (playerPos + obstaclePos) * 0.5f;
        }
    }
    
    /// <summary>
    /// Evento generato quando il giocatore sfonda un ostacolo
    /// </summary>
    public struct ObstacleBreakThroughEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità dell'ostacolo
        /// </summary>
        public Entity ObstacleEntity;
        
        /// <summary>
        /// Posizione dove è avvenuto lo sfondamento
        /// </summary>
        public float3 BreakThroughPosition;
    }
    
    /// <summary>
    /// Tipi di danno che possono essere applicati
    /// </summary>
    public enum DamageType : byte
    {
        /// <summary>
        /// Danno da collisione con ostacoli
        /// </summary>
        Obstacle = 0,
        
        /// <summary>
        /// Danno da caduta
        /// </summary>
        Fall = 1,
        
        /// <summary>
        /// Danno da nemici
        /// </summary>
        Enemy = 2,
        
        /// <summary>
        /// Danno da trappole ambientali
        /// </summary>
        Hazard = 3,
        
        /// <summary>
        /// Danno da effetti di stato (es. veleno, fuoco)
        /// </summary>
        StatusEffect = 4
    }
    
    /// <summary>
    /// Evento generico di danno
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        /// <summary>
        /// Entità che riceve il danno
        /// </summary>
        public Entity TargetEntity;
        
        /// <summary>
        /// Entità che causa il danno (può essere Entity.Null)
        /// </summary>
        public Entity SourceEntity;
        
        /// <summary>
        /// Quantità di danno
        /// </summary>
        public float DamageAmount;
        
        /// <summary>
        /// Tipo di danno
        /// </summary>
        public DamageType DamageType;
        
        /// <summary>
        /// Posizione dell'impatto
        /// </summary>
        public float3 ImpactPosition;
    }
}