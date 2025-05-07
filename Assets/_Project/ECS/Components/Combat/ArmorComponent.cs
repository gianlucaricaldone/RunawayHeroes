using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Combat
{
    /// <summary>
    /// Componente che rappresenta l'armatura e le resistenze di un'entità
    /// </summary>
    [Serializable]
    public struct ArmorComponent : IComponentData
    {
        public float PhysicalDamageReduction;    // Riduzione danni fisici (0-1)
        public float EnemyDamageReduction;       // Riduzione danni da nemici (0-1)
        public float HazardDamageReduction;      // Riduzione danni da pericoli ambientali (0-1)
        public float FallDamageReduction;        // Riduzione danni da caduta (0-1)
        public float StatusEffectResistance;     // Resistenza agli effetti di stato (0-1)
    }
}