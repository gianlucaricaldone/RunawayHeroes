// Path: Assets/_Project/ECS/Components/Core/CommonTags.cs
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Core
{
    /// <summary>
    /// Tag per identificare le entità del giocatore
    /// </summary>
    public struct PlayerTag : IComponentData { }

    /// <summary>
    /// Tag per identificare le entità attive
    /// </summary>
    public struct ActiveTag : IComponentData { }
}