using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Buffer element component che contiene i riferimenti alle doorway di una stanza
    /// </summary>
    [Serializable]
    public struct RoomDoorwayBuffer : IBufferElementData
    {
        public Entity DoorwayEntity;  // Riferimento all'entità doorway
    }
}