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
    /// Sistema che gestisce l'inseguimento di bersagli da parte dei nemici.
    /// Implementa il comportamento di inseguimento degli agenti nemici,
    /// calcolando percorsi, evitando ostacoli e adattando le velocità.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAISystem))]
    [BurstCompile]
    public partial struct PursuitSystem : ISystem
    {
        #region Fields
        // Query per le entità in inseguimento
        private EntityQuery _pursuitQuery;
        
        // Query per i giocatori (target)
        private EntityQuery _targetQuery;
        #endregion
        
        #region Lifecycle
        /// <summary>
        /// Inizializza il sistema di inseguimento
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per gli agenti in modalità inseguimento
            _pursuitQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TransformComponent, PhysicsComponent>()
                .WithNone<StunnedTag>() // Escludi nemici storditi
                .Build(ref state);
                
            // Configura la query per i possibili target
            _targetQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità con inseguimento e target per l'aggiornamento
            state.RequireForUpdate(_pursuitQuery);
            state.RequireForUpdate(_targetQuery);
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
        /// Aggiorna il comportamento di inseguimento di tutti i nemici
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Raccogli tutte le posizioni dei possibili target (tipicamente giocatori)
            var targetTransforms = _targetQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            var targetPositions = new NativeArray<float3>(targetTransforms.Length, Allocator.TempJob);
            
            for (int i = 0; i < targetTransforms.Length; i++)
            {
                targetPositions[i] = targetTransforms[i].Position;
            }
            
            // Schedule il job che gestisce l'inseguimento
            state.Dependency = new PursuitJob
            {
                DeltaTime = deltaTime,
                TargetPositions = targetPositions,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_pursuitQuery, state.Dependency);
            
            // Cleanup
            state.Dependency = targetTransforms.Dispose(state.Dependency);
            state.Dependency = targetPositions.Dispose(state.Dependency);
        }
        #endregion
        
        #region Jobs
        /// <summary>
        /// Job che gestisce il comportamento di inseguimento
        /// </summary>
        [BurstCompile]
        private partial struct PursuitJob : IJobEntity
        {
            // Dati di input
            public float DeltaTime;
            [ReadOnly] public NativeArray<float3> TargetPositions;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            // Logica di inseguimento
            void Execute(Entity entity, 
                         [EntityIndexInQuery] int sortKey, 
                         ref TransformComponent transform, 
                         ref PhysicsComponent physics,
                         in EnemyAIComponent ai)
            {
                // Verifica se l'entità è effettivamente in stato di inseguimento
                if (ai.CurrentState != AIState.Pursuing)
                {
                    return;
                }
                
                // Trova il target più vicino
                float3 nearestTargetPosition = FindNearestTarget(transform.Position);
                
                // Calcola la direzione verso il target
                float3 direction = nearestTargetPosition - transform.Position;
                float distance = math.length(direction);
                
                // Se il target è abbastanza vicino, inseguilo
                if (distance > 0.01f)
                {
                    // Normalizza la direzione
                    direction = math.normalize(direction);
                    
                    // Adatta la velocità in base alla distanza
                    float speedFactor = math.min(1.0f, distance / ai.AttackRange);
                    float adjustedSpeed = math.lerp(ai.MovementSpeed * 0.7f, ai.MovementSpeed, speedFactor);
                    
                    // Applica la velocità
                    physics.Velocity = direction * adjustedSpeed;
                    
                    // Applica anche la rotazione per far guardare l'entità nella direzione di movimento
                    if (physics.Velocity.x != 0 || physics.Velocity.z != 0)
                    {
                        float3 forwardDir = math.normalize(physics.Velocity);
                        
                        if (math.any(forwardDir))
                        {
                            quaternion targetRotation = quaternion.LookRotation(forwardDir, new float3(0, 1, 0));
                            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 5.0f * DeltaTime);
                        }
                    }
                }
                else
                {
                    // Se siamo molto vicini, ferma il movimento
                    physics.Velocity = float3.zero;
                }
            }
            
            /// <summary>
            /// Trova la posizione del target più vicino
            /// </summary>
            private float3 FindNearestTarget(float3 currentPosition)
            {
                float minDistance = float.MaxValue;
                float3 nearestPosition = currentPosition; // Default: se non ci sono target, rimani fermo
                
                for (int i = 0; i < TargetPositions.Length; i++)
                {
                    float distance = math.distancesq(currentPosition, TargetPositions[i]);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestPosition = TargetPositions[i];
                    }
                }
                
                return nearestPosition;
            }
        }
        #endregion
    }
}
