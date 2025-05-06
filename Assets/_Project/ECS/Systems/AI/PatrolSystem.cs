using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Systems.AI;

namespace RunawayHeroes.ECS.Systems.AI
{
    /// <summary>
    /// Sistema che gestisce il movimento di pattugliamento dei nemici.
    /// Si occupa di far muovere i nemici lungo percorsi predefiniti o generati
    /// dinamicamente, gestendo le tempistiche di attesa e le transizioni tra waypoint.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAISystem))]
    [BurstCompile]
    public partial struct PatrolSystem : ISystem
    {
        #region Fields
        // Query per le entità con comportamento di pattugliamento
        private EntityQuery _patrolQuery;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Inizializza il sistema di pattugliamento
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità che pattugliano
            _patrolQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PatrolComponent, TransformComponent, PhysicsComponent>()
                .WithNone<StunnedTag>() // Escludi entità stordite
                .Build(ref state);
                
            // Richiedi che ci siano entità con pattuglia per l'aggiornamento
            state.RequireForUpdate(_patrolQuery);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        #endregion
        
        #region Update
        /// <summary>
        /// Aggiorna il movimento di pattugliamento di tutti i nemici
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Carica i dati necessari per il patrolling (non serve schedularli in un job)
            state.Dependency = new PatrolMovementJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_patrolQuery, state.Dependency);
        }
        #endregion
        
        #region Jobs
        /// <summary>
        /// Job che gestisce il movimento di pattugliamento
        /// </summary>
        [BurstCompile]
        private partial struct PatrolMovementJob : IJobEntity
        {
            // Dati di input
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            // Logica di pattugliamento
            void Execute(Entity entity, 
                         [EntityIndexInQuery] int sortKey, 
                         ref PatrolComponent patrol, 
                         ref TransformComponent transform, 
                         ref PhysicsComponent physics)
            {
                // Se l'entità è in attesa, gestisci il timer
                if (patrol.IsWaiting)
                {
                    patrol.WaitTimer -= DeltaTime;
                    
                    // Se l'attesa è finita, continua il movimento
                    if (patrol.WaitTimer <= 0f)
                    {
                        patrol.IsWaiting = false;
                        
                        // Se siamo all'ultimo waypoint e il percorso non è circolare, inverti direzione
                        if ((patrol.IsReversing && patrol.CurrentWaypointIndex == 0) ||
                            (!patrol.IsReversing && patrol.CurrentWaypointIndex == 1))
                        {
                            if (patrol.IsCircular)
                            {
                                // Se circolare, riparti dall'inizio
                                patrol.CurrentWaypointIndex = (byte)(patrol.IsReversing ? 1 : 0);
                            }
                            else
                            {
                                // Altrimenti inverti direzione
                                patrol.IsReversing = !patrol.IsReversing;
                            }
                        }
                        else
                        {
                            // Passa al waypoint successivo/precedente
                            patrol.CurrentWaypointIndex = (byte)(patrol.IsReversing ? 
                                patrol.CurrentWaypointIndex - 1 : patrol.CurrentWaypointIndex + 1);
                        }
                    }
                    else
                    {
                        // Ancora in attesa, nessun movimento
                        physics.Velocity = float3.zero;
                        return;
                    }
                }
                
                // Determina il waypoint di destinazione
                float3 targetWaypoint = patrol.CurrentWaypointIndex == 0 ? patrol.StartPoint : patrol.EndPoint;
                
                // Calcola la direzione verso il waypoint
                float3 direction = targetWaypoint - transform.Position;
                float distance = math.length(direction);
                
                // Controlla se il waypoint è stato raggiunto
                if (distance <= patrol.WaypointReachedDistance)
                {
                    // Waypoint raggiunto, inizia il timer di attesa
                    patrol.IsWaiting = true;
                    patrol.WaitTimer = patrol.WaitTimeAtWaypoint;
                    physics.Velocity = float3.zero;
                }
                else
                {
                    // Normalizza la direzione e muoviti verso il waypoint
                    direction = math.normalize(direction);
                    physics.Velocity = direction * patrol.PatrolSpeed;
                    
                    // Imposta anche la rotazione per guardare nella direzione del movimento
                    // (Nota: questo è semplificato, idealmente vorremmo una rotazione graduale)
                    if (physics.Velocity.x != 0 || physics.Velocity.z != 0)
                    {
                        float3 lookAheadPoint = transform.Position + physics.Velocity * math.min(patrol.LookAheadDistance, distance);
                        float3 forwardDir = math.normalize(lookAheadPoint - transform.Position);
                        
                        // Calcola rotazione usando slerp (approssimazione)
                        if (math.any(forwardDir))
                        {
                            quaternion targetRotation = quaternion.LookRotation(forwardDir, new float3(0, 1, 0));
                            transform.Rotation = math.slerp(transform.Rotation, targetRotation, patrol.RotationSpeed * DeltaTime);
                        }
                    }
                }
            }
        }
        #endregion
    }
}
