using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Systems.Abilities;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un'abilità viene attivata
    /// </summary>
    [System.Serializable]
    public struct AbilityActivatedEvent : IComponentData
    {
        public Entity EntityID;       // Entità che attiva l'abilità
        public AbilityType AbilityType; // Tipo di abilità
        public float3 Position;       // Posizione di attivazione
        public float Duration;        // Durata dell'abilità
    }
}
