using Unity.Entities;
using RunawayHeroes.ECS.Systems.Movement;
using RunawayHeroes.ECS.Systems.Combat;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un danno viene bloccato
    /// </summary>
    public struct DamageBlockedEvent : IComponentData
    {
        public Entity TargetEntity;         // Entità che ha bloccato il danno
        public Entity SourceEntity;         // Entità che ha tentato di causare danno
        public float DamageAmount;          // Quantità di danno bloccata
        public DamageType DamageType;       // Tipo di danno
        public BlockReason BlockReason;     // Motivo del blocco
    }
}