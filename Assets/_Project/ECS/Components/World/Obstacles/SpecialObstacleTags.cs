using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Tag per indicare un ostacolo di tipo lava
    /// </summary>
    [Serializable]
    public struct LavaTag : IComponentData
    {
        // Tag per ostacoli di lava
        public float DamagePerSecond;
    }

    /// <summary>
    /// Tag per indicare un ostacolo di tipo ghiaccio
    /// </summary>
    [Serializable]
    public struct IceObstacleTag : IComponentData
    {
        // Tag per ostacoli di ghiaccio
        public float SlipperyFactor;
    }

    /// <summary>
    /// Componente per tracciare l'integrità degli ostacoli di ghiaccio
    /// </summary>
    [Serializable]
    public struct IceIntegrityComponent : IComponentData
    {
        public float MaxIntegrity;
        public float CurrentIntegrity;
    }

    /// <summary>
    /// Tag per indicare un ostacolo di tipo barriera digitale
    /// </summary>
    [Serializable]
    public struct DigitalBarrierTag : IComponentData
    {
        // Tag per barriere digitali
        public byte SecurityLevel;
    }

    /// <summary>
    /// Tag per indicare un ostacolo subacqueo
    /// </summary>
    [Serializable]
    public struct UnderwaterTag : IComponentData
    {
        // Tag per ostacoli subacquei
        public float DepthPressure;
    }

    /// <summary>
    /// Tag per indicare terreno tossico o dannoso
    /// </summary>
    [Serializable]
    public struct ToxicGroundTag : IComponentData
    {
        public byte ToxicType; // 0=Veleno, 1=Fuoco, 2=Acido, 3=Radioattivo
        public float DamagePerSecond;
    }
}