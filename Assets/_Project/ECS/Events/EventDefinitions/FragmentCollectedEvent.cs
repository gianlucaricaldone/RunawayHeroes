using Unity.Entities;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// 
    /// </summary>
    public struct FragmentCollectedEvent : IComponentData
    {
        // Dati evento
        public int FragmentID;
        public int FragmentType; // Cambiato da byte a int per coerenza con gli altri tipi di eventi
        public Entity CollectorEntity;

    }
}
