using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Componente che definisce le proprietà e i parametri di attacco
    /// per un'entità nemica o per oggetti che possono infliggere danni.
    /// </summary>
    [Serializable]
    public struct AttackComponent : IComponentData
    {
        // Proprietà di base
        public float BaseDamage;         // Danno base inflitto dall'attacco
        public float AttackRange;        // Raggio entro cui l'attacco è efficace
        public float AttackCooldown;     // Tempo tra un attacco e il successivo
        public float CurrentCooldown;    // Tempo rimanente prima del prossimo attacco
        
        // Proprietà di pattern
        public AttackPatternType PatternType;  // Tipo di pattern di attacco
        public byte PatternVariant;      // Variante del pattern (0-255)
        public float PatternSpeed;       // Velocità di esecuzione del pattern
        
        // Proprietà avanzate
        public float CriticalChance;     // Probabilità di colpo critico (0-1)
        public float CriticalMultiplier; // Moltiplicatore di danno per colpi critici
        public ElementType ElementType;  // Tipo di elemento dell'attacco
        
        // Proprietà di effetti
        public StatusEffectType StatusEffect; // Effetto di stato che può essere applicato
        public float StatusEffectChance; // Probabilità di applicare l'effetto di stato (0-1)
        public float StatusEffectDuration; // Durata dell'effetto di stato
        
        // Proprietà di area
        public bool IsAreaEffect;        // Se l'attacco ha effetto su un'area
        public float AreaRadius;         // Raggio dell'area di effetto
        public float AreaFalloff;        // Fattore di riduzione del danno al bordo dell'area
    }
    
    /// <summary>
    /// Tipi di pattern di attacco disponibili
    /// </summary>
    public enum AttackPatternType : byte
    {
        Direct = 0,        // Attacco diretto semplice
        Sweep = 1,         // Attacco a spazzata
        Burst = 2,         // Sequenza rapida di attacchi
        Charge = 3,        // Attacco con carica
        Projectile = 4,    // Attacco a distanza con proiettile
        AOE = 5,           // Attacco ad area
        Summon = 6,        // Evoca entità alleate
        DoT = 7,           // Danno nel tempo
        Teleport = 8,      // Teletrasporto con attacco
        Special = 9        // Pattern speciali specifici per boss
    }
    
    /// <summary>
    /// Tipi di elementi per gli attacchi
    /// </summary>
    public enum ElementType : byte
    {
        None = 0,         // Nessun elemento (fisico)
        Fire = 1,         // Fuoco
        Ice = 2,          // Ghiaccio
        Electric = 3,     // Elettrico
        Poison = 4,       // Veleno
        Digital = 5,      // Digitale (mondo virtuale)
        Water = 6,        // Acqua
        Earth = 7,        // Terra
        Wind = 8          // Vento
    }
    
    /// <summary>
    /// Tipi di effetti di stato che possono essere applicati da un attacco
    /// </summary>
    public enum StatusEffectType : byte
    {
        None = 0,        // Nessun effetto
        Burn = 1,        // Bruciatura (danno nel tempo)
        Freeze = 2,      // Congelamento (movimento rallentato)
        Paralyze = 3,    // Paralisi (attacchi e movimento rallentati)
        Poison = 4,      // Avvelenamento (danno nel tempo e potenza ridotta)
        Corrupt = 5,     // Corruzione (danno nel tempo e movimento casuale)
        Drown = 6,       // Annegamento (movimento limitato)
        Stun = 7,        // Stordimento (impossibilità di azione)
        Confuse = 8      // Confusione (movimento invertito)
    }
}
