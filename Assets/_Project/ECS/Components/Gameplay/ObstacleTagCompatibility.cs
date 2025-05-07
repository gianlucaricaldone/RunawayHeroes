using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// NOTA DI COMPATIBILITÀ: Tutti i tag degli ostacoli sono stati spostati nel namespace 
    /// RunawayHeroes.ECS.Components.World.Obstacles.
    /// Questo file contiene solo alias per compatibilità con il codice esistente e dovrebbe
    /// essere rimosso in futuro. Usare le definizioni nel namespace corretto.
    /// </summary>
    
    // // Tipi obsoleti che ereditano dai nuovi tipi consolidati
    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.LavaTag invece")]
    // public struct LavaTag : IComponentData
    // {
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.IceObstacleTag invece")]
    // public struct IceObstacleTag : IComponentData
    // {
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.IceIntegrityComponent invece")]
    // public struct IceIntegrityComponent : IComponentData
    // {
    //     public float MaxIntegrity;
    //     public float CurrentIntegrity;
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.DigitalBarrierTag invece")]
    // public struct DigitalBarrierTag : IComponentData
    // {
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.UnderwaterTag invece")]
    // public struct UnderwaterTag : IComponentData
    // {
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.SlipperyTag invece")]
    // public struct SlipperyTag : IComponentData
    // {
    //     public float SlipFactor;
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.ToxicGroundTag invece")]
    // public struct ToxicGroundTag : IComponentData
    // {
    //     public byte ToxicType;
    //     public float DamagePerSecond;
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.CurrentTag invece")]
    // public struct CurrentTag : IComponentData
    // {
    //     public Unity.Mathematics.float3 Direction;
    //     public float Strength;
    //     public byte CurrentType;
    // }

    // [Obsolete("Usa RunawayHeroes.ECS.Components.World.Obstacles.IceMeltEffectComponent invece")]
    // public struct IceMeltEffectComponent : IComponentData
    // {
    //     public Unity.Mathematics.float3 Position;
    //     public float Size;
    // }
}