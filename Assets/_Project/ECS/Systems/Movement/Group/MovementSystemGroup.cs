using Unity.Entities;

namespace RunawayHeroes.ECS.Systems.Movement.Group
{
    /// <summary>
    /// Gruppo di sistemi che gestisce tutti gli aspetti del movimento.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class MovementSystemGroup : ComponentSystemGroup
    {
    }
}