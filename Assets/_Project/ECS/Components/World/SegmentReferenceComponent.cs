using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che fornisce un riferimento a un segmento di percorso.
    /// Utilizzato per collegare entità (nemici, ostacoli, ecc.) ai segmenti di livello a cui appartengono.
    /// </summary>
    public struct SegmentReferenceComponent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del segmento di percorso
        /// </summary>
        public Entity SegmentEntity;
        
        /// <summary>
        /// ID del segmento di percorso
        /// </summary>
        public int SegmentID;
    }
}