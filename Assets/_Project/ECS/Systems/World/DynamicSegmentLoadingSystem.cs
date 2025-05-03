using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Core;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce il caricamento dinamico dei segmenti del percorso
    /// man mano che il giocatore avanza nel livello
    /// </summary>
    public partial class DynamicSegmentLoadingSystem : SystemBase
    {
        private EntityCommandBufferSystem _commandBufferSystem;
        private const float ACTIVATION_DISTANCE = 50f; // Distanza per attivare i segmenti successivi
        private const float DEACTIVATION_DISTANCE = 70f; // Distanza per disattivare i segmenti precedenti
        
        protected override void OnCreate()
        {
            // Ottieni il sistema di command buffer
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi che il sistema venga eseguito solo quando c'è almeno un'entità giocatore
            RequireForUpdate<PlayerTag>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni la posizione del giocatore
            float3 playerPosition = float3.zero;
            Entity playerEntity = Entity.Null;
            
            Entities
                .WithAll<PlayerTag>()
                .ForEach((Entity entity, in LocalTransform transform) =>
                {
                    playerPosition = transform.Position;
                    playerEntity = entity;
                }).Run(); // Eseguiamo in modo sincrono perché abbiamo bisogno del risultato subito
                
            if (playerEntity == Entity.Null)
                return; // Nessun giocatore trovato
                
            // Cerca tutti i segmenti e attiva quelli vicini, disattiva quelli lontani
            Entities
                .WithName("ManageSegmentActivation")
                .ForEach((Entity segmentEntity, int entityInQueryIndex,
                         ref PathSegmentComponent segment) =>
                {
                    // Calcola la distanza tra il giocatore e l'inizio del segmento
                    float distanceToSegmentStart = math.length(segment.StartPosition - playerPosition);
                    
                    // Gestisci l'attivazione
                    if (!segment.IsActive && distanceToSegmentStart < ACTIVATION_DISTANCE)
                    {
                        // Attiva il segmento
                        segment.IsActive = true;
                        
                        // Se il contenuto non è ancora generato, richiedi la generazione
                        if (!segment.IsGenerated)
                        {
                            commandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new RequiresContentGenerationTag());
                        }
                        
                        // Se c'è un segmento successivo, pre-caricalo
                        if (segment.NextSegment != Entity.Null)
                        {
                            // Precarica il prossimo segmento (potremmo avere una logica più complessa qui)
                        }
                    }
                    
                    // Gestisci la disattivazione dei segmenti lontani
                    if (segment.IsActive && distanceToSegmentStart > DEACTIVATION_DISTANCE)
                    {
                        // Disattiva il segmento se è lontano
                        segment.IsActive = false;
                        
                        // Opzionalmente pulisci le risorse
                        // Se il segmento conteneva nemici attivi, potremmo voler aggiungere logica per pulirli
                    }
                    
                }).ScheduleParallel();
            
            // Assicurati che i comandi vengano eseguiti
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}