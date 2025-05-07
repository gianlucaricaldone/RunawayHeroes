using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Componente tag per identificare un'entità come ostacolo
    /// </summary>
    [Serializable]
    public struct ObstacleTag : IComponentData
    {
        // Tag per ostacoli senza dati aggiuntivi
    }
}