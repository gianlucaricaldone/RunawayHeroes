using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema responsabile della sincronizzazione tra TransformComponent personalizzati e 
    /// il sistema di trasformazione nativo di Unity DOTS. Gestisce posizione, rotazione e scala
    /// delle entità nel mondo di gioco.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(RenderSystem))]
    public partial class TransformSystem : SystemBase
    {
        /// <summary>
        /// Inizializza il sistema di trasformazione, configurando eventuali query di entità
        /// o dipendenze con altri sistemi.
        /// </summary>
        protected override void OnCreate()
        {
            // TODO: Inizializzare query di entità e altre risorse
        }

        /// <summary>
        /// Aggiorna la trasformazione di tutte le entità che hanno sia un TransformComponent
        /// che le necessarie componenti di trasformazione native di Unity DOTS (come Translation,
        /// Rotation, e NonUniformScale).
        /// </summary>
        protected override void OnUpdate()
        {
            // TODO: Implementare la sincronizzazione tra TransformComponent e 
            // componenti di trasformazione native di Unity DOTS
        }
    }
}
