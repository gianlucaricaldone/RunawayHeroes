using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Core;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce il caricamento dinamico dei segmenti del percorso
    /// man mano che il giocatore avanza nel livello
    /// </summary>
    public partial struct DynamicSegmentLoadingSystem : ISystem
    {
        private const float ACTIVATION_DISTANCE = 50f; // Distanza per attivare i segmenti successivi
        private const float DEACTIVATION_DISTANCE = 70f; // Distanza per disattivare i segmenti precedenti
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi che il sistema venga eseguito solo quando c'è almeno un'entità giocatore
            state.RequireForUpdate<PlayerTag>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni la posizione del giocatore
            float3 playerPosition = float3.zero;
            Entity playerEntity = Entity.Null;
            
            foreach (var (transform, entity) in 
                     SystemAPI.Query<RefRO<LocalTransform>>()
                     .WithAll<PlayerTag>()
                     .WithEntityAccess())
            {
                playerPosition = transform.ValueRO.Position;
                playerEntity = entity;
                break; // Assumiamo che ci sia un solo giocatore principale
            }
                
            if (playerEntity == Entity.Null)
                return; // Nessun giocatore trovato
            
            // Memorizza la posizione del giocatore per l'uso nel job
            var playerPos = playerPosition;
            
            // Cerca tutti i segmenti e attiva quelli vicini, disattiva quelli lontani
            state.Dependency = new ManageSegmentActivationJob
            {
                PlayerPosition = playerPos,
                ActivationDistance = ACTIVATION_DISTANCE,
                DeactivationDistance = DEACTIVATION_DISTANCE,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct ManageSegmentActivationJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float ActivationDistance;
            public float DeactivationDistance;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            
            void Execute(Entity segmentEntity, 
                         [ChunkIndexInQuery] int chunkIndexInQuery,
                         ref PathSegmentComponent segment)
            {
                // Calcola la distanza tra il giocatore e l'inizio del segmento
                float distanceToSegmentStart = math.length(segment.StartPosition - PlayerPosition);
                
                // Gestisci l'attivazione
                if (!segment.IsActive && distanceToSegmentStart < ActivationDistance)
                {
                    // Attiva il segmento
                    segment.IsActive = true;
                    
                    // Se il contenuto non è ancora generato, richiedi la generazione
                    if (!segment.IsGenerated)
                    {
                        CommandBuffer.AddComponent(chunkIndexInQuery, segmentEntity, new RequiresContentGenerationTag());
                    }
                    
                    // Se c'è un segmento successivo, pre-caricalo
                    if (segment.NextSegment != Entity.Null)
                    {
                        // Precarica il prossimo segmento (potremmo avere una logica più complessa qui)
                    }
                }
                
                // Gestisci la disattivazione dei segmenti lontani
                if (segment.IsActive && distanceToSegmentStart > DeactivationDistance)
                {
                    // Disattiva il segmento se è lontano
                    segment.IsActive = false;
                    
                    // Opzionalmente pulisci le risorse
                    // Se il segmento conteneva nemici attivi, potremmo voler aggiungere logica per pulirli
                }
            }
        }
    }
}