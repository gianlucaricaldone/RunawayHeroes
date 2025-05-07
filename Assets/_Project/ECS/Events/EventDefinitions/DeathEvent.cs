using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Systems.Movement;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un'entità muore
    /// </summary>
    public struct DeathEvent : IComponentData
    {
        public Entity DeadEntity;           // Entità che è morta
        public Entity KillerEntity;         // Entità che ha causato la morte
        public DamageType DamageType;       // Tipo di danno che ha causato la morte
        public float3 DeathPosition;        // Posizione della morte
    }
}