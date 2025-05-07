using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Systems.Movement;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un'entità riceve danno
    /// </summary>
    public struct DamageReceivedEvent : IComponentData
    {
        public Entity TargetEntity;         // Entità che ha ricevuto danno
        public Entity SourceEntity;         // Entità che ha causato danno
        public float DamageAmount;          // Quantità di danno applicata
        public DamageType DamageType;       // Tipo di danno
        public float3 ImpactPosition;       // Posizione dell'impatto
        public float RemainingHealth;       // Salute rimanente
        public float RemainingShield;       // Scudo rimanente
    }
}