using Unity.Entities;

namespace RunawayHeroes.ECS.Systems.Gameplay.Group
{
    /// <summary>
    /// Gruppo di sistemi che gestisce tutti gli aspetti del gameplay.
    /// Include progressione, tutorial, meccaniche di gioco principali.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class GameplaySystemGroup : ComponentSystemGroup
    {
    }
}