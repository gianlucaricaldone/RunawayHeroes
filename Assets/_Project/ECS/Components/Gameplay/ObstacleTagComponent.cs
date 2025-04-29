using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Tag per identificare le superfici di lava, utilizzato per l'abilità Corpo Ignifugo di Ember
    /// </summary>
    public struct LavaTag : IComponentData
    {
    }

    /// <summary>
    /// Tag per identificare le zone di ghiaccio, utilizzato per l'abilità Aura di Calore di Kai
    /// </summary>
    public struct IceObstacleTag : IComponentData
    {
    }

    /// <summary>
    /// Componente per tracciare l'integrità di un ostacolo di ghiaccio, utilizzato per l'effetto di scioglimento
    /// </summary>
    public struct IceIntegrityComponent : IComponentData
    {
        /// <summary>
        /// Valore massimo di integrità del ghiaccio
        /// </summary>
        public float MaxIntegrity;
        
        /// <summary>
        /// Valore attuale di integrità del ghiaccio
        /// </summary>
        public float CurrentIntegrity;
    }

    /// <summary>
    /// Tag per identificare barriere digitali, utilizzato per l'abilità Glitch Controllato di Neo
    /// </summary>
    public struct DigitalBarrierTag : IComponentData
    {
    }

    /// <summary>
    /// Tag per identificare le zone sottomarine, utilizzato per l'abilità Bolla d'Aria di Marina
    /// </summary>
    public struct UnderwaterTag : IComponentData
    {
    }

    /// <summary>
    /// Tag per identificare superfici scivolose o ghiacciate, utilizzato per l'abilità Aura di Calore di Kai
    /// </summary>
    public struct SlipperyTag : IComponentData
    {
        /// <summary>
        /// Fattore di scivolosità (0-1, dove 1 è il massimo)
        /// </summary>
        public float SlipFactor;
    }

    /// <summary>
    /// Tag per identificare terreni tossici, utilizzato per varie abilità di resistenza
    /// </summary>
    public struct ToxicGroundTag : IComponentData
    {
        /// <summary>
        /// Tipo di tossicità (gas, liquido, ecc.)
        /// </summary>
        public byte ToxicType;
        
        /// <summary>
        /// Danno per secondo
        /// </summary>
        public float DamagePerSecond;
    }

    /// <summary>
    /// Tag per identificare correnti d'aria o d'acqua, utilizzato per varie abilità
    /// </summary>
    public struct CurrentTag : IComponentData
    {
        /// <summary>
        /// Direzione della corrente
        /// </summary>
        public Unity.Mathematics.float3 Direction;
        
        /// <summary>
        /// Forza della corrente
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// Tipo di corrente (aria, acqua)
        /// </summary>
        public byte CurrentType;
    }

    /// <summary>
    /// Componente per l'effetto visivo di scioglimento del ghiaccio
    /// </summary>
    public struct IceMeltEffectComponent : IComponentData
    {
        /// <summary>
        /// Posizione dell'effetto
        /// </summary>
        public Unity.Mathematics.float3 Position;
        
        /// <summary>
        /// Dimensione dell'effetto
        /// </summary>
        public float Size;
    }
}