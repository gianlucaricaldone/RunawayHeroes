using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Buffer per memorizzare i riferimenti agli elementi (ostacoli, nemici, ecc.) di un segmento
    /// </summary>
    [Serializable]
    public struct SegmentContentBuffer : IBufferElementData
    {
        public Entity ContentEntity;     // Riferimento all'entità contenuta nel segmento
        public SegmentContentType Type;  // Tipo di contenuto
    }
    
    /// <summary>
    /// Tipi di contenuto che possono essere presenti in un segmento
    /// </summary>
    public enum SegmentContentType
    {
        Obstacle,       // Ostacolo generico
        SmallObstacle,  // Ostacolo piccolo
        MediumObstacle, // Ostacolo medio
        LargeObstacle,  // Ostacolo grande
        Enemy,          // Nemico generico
        Drone,          // Nemico drone
        Patrol,         // Nemico pattuglia
        MidBoss,        // Mini-boss
        Boss,           // Boss
        Collectible,    // Oggetto collezionabile
        PowerUp,        // Power-up
        Checkpoint,     // Checkpoint
        EnvironmentHazard, // Pericolo ambientale (lava, ghiaccio, ecc.)
        Decoration      // Decorazione (non interagibile)
    }
}