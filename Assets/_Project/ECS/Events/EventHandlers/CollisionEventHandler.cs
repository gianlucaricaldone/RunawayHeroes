using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RunawayHeroes.ECS.Events.Handlers
{
    /// <summary>
    /// Sistema che gestisce gli eventi di collisione tra entità
    /// </summary>
    [BurstCompile]
    public partial struct CollisionEventHandler : ISystem
    {
        private EntityQuery _collisionEventsQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query necessarie
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query per le entità con CollisionEvent
            _collisionEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CollisionEvent>()
                .Build(ref state);
                
            state.RequireForUpdate(_collisionEventsQuery);
        }

        /// <summary>
        /// Esegue la pulizia delle risorse allocate
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup di eventuali risorse
        }

        /// <summary>
        /// Elabora tutti gli eventi di collisione presenti nel frame corrente
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottiene il buffer di comandi per eventuali modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Esegue il job di elaborazione eventi
            state.Dependency = new ProcessCollisionEventsJob
            {
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }
        
        /// <summary>
        /// Job che elabora gli eventi di collisione
        /// </summary>
        [BurstCompile]
        private partial struct ProcessCollisionEventsJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            
            // Definisce il metodo che verrà eseguito per ogni entità che soddisfa la query
            private void Execute(Entity entity, in CollisionEvent collisionEvent)
            {
                // Implementa la logica di risposta agli eventi di collisione
                // Esempi di azioni possibili:
                // - Applicare danni in caso di collisione con nemici o ostacoli
                // - Attivare effetti visivi o sonori
                // - Modificare lo stato di gioco
                // - Aggiornare componenti di gioco in risposta alla collisione
                
                // Elimina l'evento dopo l'elaborazione
                ECB.DestroyEntity(entity);
            }
        }
    }
}
