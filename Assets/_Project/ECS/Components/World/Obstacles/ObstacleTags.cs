using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Componente tag per identificare un'entità come ostacolo
    /// </summary>
    [Serializable]
    public struct ObstacleTag : IComponentData
    {
        // Tag per ostacoli senza dati aggiuntivi
    }

    /// <summary>
    /// Tag per identificare le superfici di lava, utilizzato per l'abilità Corpo Ignifugo di Ember
    /// </summary>
    [Serializable]
    public struct LavaTag : IComponentData
    {
        /// <summary>
        /// Danno per secondo inflitto dalla lava
        /// </summary>
        public float DamagePerSecond;
    }

    /// <summary>
    /// Tag per identificare le zone di ghiaccio, utilizzato per l'abilità Aura di Calore di Kai
    /// </summary>
    [Serializable]
    public struct IceObstacleTag : IComponentData
    {
        /// <summary>
        /// Fattore di scivolosità del ghiaccio
        /// </summary>
        public float SlipperyFactor;
    }

    /// <summary>
    /// Componente per tracciare l'integrità di un ostacolo di ghiaccio, utilizzato per l'effetto di scioglimento
    /// </summary>
    [Serializable]
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
    [Serializable]
    public struct DigitalBarrierTag : IComponentData
    {
        /// <summary>
        /// Livello di sicurezza della barriera (difficoltà di attraversamento)
        /// </summary>
        public byte SecurityLevel;
    }

    /// <summary>
    /// Tag per identificare le zone sottomarine, utilizzato per l'abilità Bolla d'Aria di Marina
    /// </summary>
    [Serializable]
    public struct UnderwaterTag : IComponentData
    {
        /// <summary>
        /// Pressione dell'acqua in base alla profondità
        /// </summary>
        public float DepthPressure;
    }

    /// <summary>
    /// Tag per identificare superfici scivolose o ghiacciate, utilizzato per l'abilità Aura di Calore di Kai
    /// </summary>
    [Serializable]
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
    [Serializable]
    public struct ToxicGroundTag : IComponentData
    {
        /// <summary>
        /// Tipo di tossicità: 0=Veleno, 1=Fuoco, 2=Acido, 3=Radioattivo
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
    [Serializable]
    public struct CurrentTag : IComponentData
    {
        /// <summary>
        /// Direzione della corrente
        /// </summary>
        public float3 Direction;
        
        /// <summary>
        /// Forza della corrente
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// Tipo di corrente: 0=Generica, 1=Aria, 2=Acqua
        /// </summary>
        public byte CurrentType;
    }

    /// <summary>
    /// Componente per l'effetto visivo di scioglimento del ghiaccio
    /// </summary>
    [Serializable]
    public struct IceMeltEffectComponent : IComponentData
    {
        /// <summary>
        /// Posizione dell'effetto
        /// </summary>
        public float3 Position;
        
        /// <summary>
        /// Dimensione dell'effetto
        /// </summary>
        public float Size;
    }
}