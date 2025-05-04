using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Systems.Core;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che implementa comportamenti di steering per evitare ostacoli.
    /// Rileva potenziali collisioni con ostacoli nel percorso delle entità mobili
    /// e modifica il loro movimento per evitarli in modo realistico.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystem))]
    [BurstCompile]
    public partial struct ObstacleAvoidanceSystem : ISystem
    {
        #region Fields
        
        // Query per entità che devono evitare ostacoli
        private EntityQuery _avoidersQuery;
        
        // Query per gli ostacoli
        private EntityQuery _obstaclesQuery;
        
        #endregion
        
        #region Lifecycle
        
        /// <summary>
        /// Inizializza il sistema di obstacle avoidance
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità che evitano ostacoli
            _avoidersQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleAvoiderComponent, TransformComponent>()
                .WithAllRW<PhysicsComponent>()
                .Build(ref state);
                
            // Configura la query per gli ostacoli
            _obstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità che evitano ostacoli e ostacoli per l'aggiornamento
            state.RequireForUpdate(_avoidersQuery);
            state.RequireForUpdate(_obstaclesQuery);
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna il comportamento di evitamento ostacoli per tutte le entità
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Raccogli tutti gli ostacoli
            var obstacles = _obstaclesQuery.ToEntityArray(Allocator.TempJob);
            var obstaclePositions = _obstaclesQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            var obstacleComponents = _obstaclesQuery.ToComponentDataArray<ObstacleComponent>(Allocator.TempJob);
            
            // Processa le entità che evitano ostacoli con un IJobEntity
            state.Dependency = new ObstacleAvoidanceJob
            {
                DeltaTime = deltaTime,
                Obstacles = obstacles,
                ObstaclePositions = obstaclePositions,
                ObstacleComponents = obstacleComponents
            }.ScheduleParallel(_avoidersQuery, state.Dependency);
            
            // Pulisci le risorse allocate
            state.Dependency = obstacles.Dispose(state.Dependency);
            state.Dependency = obstaclePositions.Dispose(state.Dependency);
            state.Dependency = obstacleComponents.Dispose(state.Dependency);
        }
        
        #endregion
    }
    
    #region Jobs
    
    /// <summary>
    /// Job che implementa il comportamento di obstacle avoidance
    /// </summary>
    [BurstCompile]
    public partial struct ObstacleAvoidanceJob : IJobEntity
    {
        public float DeltaTime;
        
        [ReadOnly] public NativeArray<Entity> Obstacles;
        [ReadOnly] public NativeArray<TransformComponent> ObstaclePositions;
        [ReadOnly] public NativeArray<ObstacleComponent> ObstacleComponents;
        
        [BurstCompile]
        public void Execute(Entity entity, 
                          ref PhysicsComponent physics,
                          in TransformComponent transform,
                          in ObstacleAvoiderComponent avoider)
        {
            // Se l'entità non si sta muovendo, non è necessario evitare ostacoli
            if (math.lengthsq(physics.Velocity) < 0.01f)
                return;
                
            // Calcola la direzione attuale del movimento
            float3 currentDirection = math.normalize(physics.Velocity);
            
            // Accumula le forze di steering per evitare gli ostacoli
            float3 steeringForce = float3.zero;
            int obstacleCount = 0;
            
            // Controlla ogni ostacolo
            for (int i = 0; i < Obstacles.Length; i++)
            {
                // Calcola la distanza dall'ostacolo
                float3 obstaclePos = ObstaclePositions[i].Position;
                float obstacleRadius = ObstacleComponents[i].CollisionRadius;
                
                float3 toObstacle = obstaclePos - transform.Position;
                float distance = math.length(toObstacle);
                
                // Considera solo ostacoli entro il raggio di rilevamento
                if (distance <= avoider.DetectionRadius + obstacleRadius)
                {
                    // Proiezione del vettore ostacolo sulla direzione di movimento
                    float projection = math.dot(toObstacle, currentDirection);
                    
                    // Considera solo ostacoli di fronte all'entità
                    if (projection > 0)
                    {
                        // Calcola la distanza perpendicolare alla direzione di movimento
                        float3 perpendicularVector = toObstacle - projection * currentDirection;
                        float perpendicularDistance = math.length(perpendicularVector);
                        
                        // Se l'ostacolo è nel percorso dell'entità (con un certo margine)
                        if (perpendicularDistance < (obstacleRadius + avoider.AvoidanceRadius))
                        {
                            // Calcola la forza di repulsione proporzionale alla vicinanza e direzione
                            float repulsionMagnitude = (avoider.AvoidanceRadius - perpendicularDistance) / avoider.AvoidanceRadius;
                            
                            // Calcola la direzione di evitamento (perpendicolare)
                            float3 avoidanceDirection;
                            
                            if (perpendicularDistance > 0.01f)
                            {
                                // Usa la direzione perpendicolare all'ostacolo
                                avoidanceDirection = math.normalize(perpendicularVector) * math.sign(perpendicularDistance);
                            }
                            else
                            {
                                // Se siamo troppo vicini, usa una direzione casuale ma coerente
                                avoidanceDirection = new float3(math.sin(transform.Position.x * 100), 0, math.cos(transform.Position.z * 100));
                            }
                            
                            // Forza di repulsione
                            float3 repulsionForce = avoidanceDirection * repulsionMagnitude * avoider.AvoidanceStrength;
                            
                            // Accumula la forza di steering
                            steeringForce += repulsionForce;
                            obstacleCount++;
                        }
                    }
                }
            }
            
            // Applica la forza di steering media se ci sono ostacoli da evitare
            if (obstacleCount > 0)
            {
                // Calcola la media delle forze
                steeringForce /= obstacleCount;
                
                // Limita la forza di steering
                float steeringMagnitude = math.length(steeringForce);
                if (steeringMagnitude > avoider.MaxSteeringForce)
                {
                    steeringForce = (steeringForce / steeringMagnitude) * avoider.MaxSteeringForce;
                }
                
                // Applica la forza di steering alla velocità
                physics.Velocity += steeringForce * DeltaTime;
                
                // Mantieni la stessa velocità (solo cambia direzione)
                float originalSpeed = math.length(physics.Velocity);
                if (originalSpeed > 0.01f)
                {
                    physics.Velocity = math.normalize(physics.Velocity) * avoider.MoveSpeed;
                }
            }
        }
    }
    
    #endregion
    
    #region Components
    
    /// <summary>
    /// Componente che definisce il comportamento di obstacle avoidance
    /// </summary>
    public struct ObstacleAvoiderComponent : IComponentData
    {
        public float DetectionRadius;    // Raggio di rilevamento ostacoli
        public float AvoidanceRadius;    // Raggio di evitamento
        public float AvoidanceStrength;  // Forza di evitamento
        public float MaxSteeringForce;   // Massima forza di steering
        public float MoveSpeed;          // Velocità di movimento desiderata
    }
    
    #endregion
}
