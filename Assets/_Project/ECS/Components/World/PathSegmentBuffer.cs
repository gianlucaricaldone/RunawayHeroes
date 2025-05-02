using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Buffer per memorizzare i riferimenti ai segmenti di percorso in un livello
    /// </summary>
    [Serializable]
    public struct PathSegmentBuffer : IBufferElementData
    {
        public Entity SegmentEntity;  // Riferimento all'entità del segmento
    }
}