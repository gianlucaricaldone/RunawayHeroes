using Unity.Entities;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un giocatore raccoglie un frammento del Nucleo dell'Equilibrio
    /// </summary>
    public struct FragmentCollectedEvent : IComponentData
    {
        // Dati evento
        public int FragmentID;
        public byte FragmentType;
        public Entity CollectorEntity;
    }
}
