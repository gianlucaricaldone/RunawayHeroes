// Path: Assets/_Project/ECS/Components/World/HazardComponent.cs
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che rappresenta una zona pericolosa nell'ambiente di gioco.
    /// Può essere lava, tossine, radiazioni o altri elementi dannosi per il giocatore.
    /// </summary>
    public struct HazardComponent : IComponentData
    {
        /// <summary>
        /// Tipo di pericolo
        /// </summary>
        public HazardType Type;
        
        /// <summary>
        /// Danno causato per secondo all'interno dell'area
        /// </summary>
        public float DamagePerSecond;
        
        /// <summary>
        /// Raggio dell'area pericolosa
        /// </summary>
        public float Radius;
        
        /// <summary>
        /// Se il danno è continuo o applicato solo al contatto
        /// </summary>
        public bool IsContinuousDamage;
        
        /// <summary>
        /// Effetto di status applicato (rallentamento, confusione, ecc.)
        /// </summary>
        public StatusEffectType StatusEffect;
        
        /// <summary>
        /// Durata dell'effetto di status
        /// </summary>
        public float StatusEffectDuration;
        
        /// <summary>
        /// Intensità dell'effetto di status
        /// </summary>
        public float StatusEffectIntensity;
        
        /// <summary>
        /// Crea un HazardComponent con valori predefiniti per una zona di lava
        /// </summary>
        public static HazardComponent CreateLavaHazard(float radius, float damagePerSecond)
        {
            return new HazardComponent
            {
                Type = HazardType.Lava,
                DamagePerSecond = damagePerSecond,
                Radius = radius,
                IsContinuousDamage = true,
                StatusEffect = StatusEffectType.Burning,
                StatusEffectDuration = 3.0f,
                StatusEffectIntensity = 1.0f
            };
        }
        
        /// <summary>
        /// Crea un HazardComponent con valori predefiniti per una zona tossica
        /// </summary>
        public static HazardComponent CreateToxicHazard(float radius, float damagePerSecond)
        {
            return new HazardComponent
            {
                Type = HazardType.Toxic,
                DamagePerSecond = damagePerSecond,
                Radius = radius,
                IsContinuousDamage = true,
                StatusEffect = StatusEffectType.Poisoned,
                StatusEffectDuration = 5.0f,
                StatusEffectIntensity = 0.8f
            };
        }
        
        /// <summary>
        /// Crea un HazardComponent con valori predefiniti per una zona elettrificata
        /// </summary>
        public static HazardComponent CreateElectrifiedHazard(float radius, float damagePerSecond)
        {
            return new HazardComponent
            {
                Type = HazardType.Electric,
                DamagePerSecond = damagePerSecond,
                Radius = radius,
                IsContinuousDamage = false,
                StatusEffect = StatusEffectType.Stunned,
                StatusEffectDuration = 1.5f,
                StatusEffectIntensity = 1.0f
            };
        }
        
        /// <summary>
        /// Crea un HazardComponent con valori predefiniti per una zona fredda
        /// </summary>
        public static HazardComponent CreateColdHazard(float radius, float damagePerSecond)
        {
            return new HazardComponent
            {
                Type = HazardType.Cold,
                DamagePerSecond = damagePerSecond,
                Radius = radius,
                IsContinuousDamage = true,
                StatusEffect = StatusEffectType.Slowed,
                StatusEffectDuration = 4.0f,
                StatusEffectIntensity = 0.5f
            };
        }
    }
    
    /// <summary>
    /// Tipi di pericoli ambientali
    /// </summary>
    public enum HazardType : byte
    {
        None = 0,
        Lava = 1,
        Toxic = 2,
        Electric = 3,
        Cold = 4,
        Void = 5,
        Digital = 6,
        Crushing = 7
    }
    
    /// <summary>
    /// Tipi di effetti di status che possono essere applicati da pericoli
    /// </summary>
    public enum StatusEffectType : byte
    {
        None = 0,
        Burning = 1,
        Poisoned = 2,
        Slowed = 3,
        Stunned = 4,
        Frozen = 5,
        Confused = 6,
        Weakened = 7
    }
}