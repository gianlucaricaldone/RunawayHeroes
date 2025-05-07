using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un'entità subisce danno.
    /// Questo evento viene processato da DamageEventHandler per applicare
    /// il danno effettivo alle entità, considerando difese e resistenze.
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        // Entità coinvolte
        public Entity TargetEntity;       // Entità bersaglio del danno
        public Entity SourceEntity;       // Entità che ha generato il danno
        
        // Dati del danno
        public float DamageAmount;        // Quantità di danno base
        public RunawayHeroes.ECS.Systems.Movement.DamageType DamageType; // Tipo di danno (fisico, elementale, ecc.)
        public bool IsCritical;           // Se è un colpo critico
        
        // Dati di impatto
        public float3 ImpactPosition;     // Punto di impatto del danno
        
        // Dati per effetti di stato
        public byte StatusEffectType;     // Tipo di effetto di stato che può essere applicato
        public float StatusEffectDuration; // Durata dell'effetto di stato
    }
}
