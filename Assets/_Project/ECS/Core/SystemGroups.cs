using Unity.Entities;

namespace RunawayHeroes.ECS.Core
{
    /// <summary>
    /// Gruppo di sistemi per la gestione delle trasformazioni e del movimento
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TransformSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Gruppo di sistemi per la gestione degli input
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class InputSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Gruppo di sistemi per la gestione della fisica e delle collisioni
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    public partial class PhysicsSystemGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Gruppo di sistemi per la gestione del movimento
    /// </summary>
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial class MovementSystemGroup : ComponentSystemGroup
    {
    }
}