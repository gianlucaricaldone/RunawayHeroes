using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema responsabile della sincronizzazione tra TransformComponent personalizzati e 
    /// il sistema di trasformazione nativo di Unity DOTS. Gestisce posizione, rotazione e scala
    /// delle entità nel mondo di gioco.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct TransformSystem : ISystem
    {
        private EntityQuery _transformQuery;
        
        /// <summary>
        /// Inizializza il sistema di trasformazione, configurando le query di entità
        /// necessarie per la sincronizzazione.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Crea la query per entità con TransformComponent e componenti di trasformazione native
            _transformQuery = new EntityQueryBuilder(Unity.Collections.Allocator.Temp)
                .WithAll<RunawayHeroes.ECS.Components.Core.TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità che corrispondono alla query per l'aggiornamento
            state.RequireForUpdate(_transformQuery);
        }
        
        /// <summary>
        /// Cleanup del sistema quando viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da eliminare
        }

        /// <summary>
        /// Aggiorna la trasformazione di tutte le entità che hanno sia un TransformComponent
        /// che le necessarie componenti di trasformazione native di Unity DOTS (come Translation,
        /// Rotation, e NonUniformScale).
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Utilizza IJobEntity per sincronizzare TransformComponent con le componenti native
            state.Dependency = new SyncTransformsJob().ScheduleParallel(_transformQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per sincronizzare TransformComponent con componenti di trasformazione native
    /// </summary>
    [BurstCompile]
    public partial struct SyncTransformsJob : IJobEntity
    {
        public void Execute(
            ref LocalTransform localTransform,
            in RunawayHeroes.ECS.Components.Core.TransformComponent transformComponent)
        {
            // Sincronizza posizione
            localTransform.Position = transformComponent.Position;
            
            // Sincronizza rotazione
            localTransform.Rotation = transformComponent.Rotation;
            
            // Sincronizza scala (notare che LocalTransform gestisce solo scala uniforme)
            localTransform.Scale = transformComponent.Scale;
        }
    }
}
