// Path: Assets/_Project/ECS/Components/Abilities/AirBubbleAbilityComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Bolla d'Aria" di Marina.
    /// Crea una bolla protettiva che fornisce ossigeno e respinge nemici acquatici.
    /// </summary>
    [Serializable]
    public struct AirBubbleAbilityComponent : IComponentData
    {
        /// <summary>
        /// Durata dell'abilità in secondi
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Tempo rimanente dell'abilità
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// Tempo di ricarica dell'abilità
        /// </summary>
        public float Cooldown;
        
        /// <summary>
        /// Tempo di ricarica rimanente
        /// </summary>
        public float CooldownRemaining;
        
        /// <summary>
        /// Raggio della bolla d'aria
        /// </summary>
        public float BubbleRadius;
        
        /// <summary>
        /// Forza di repulsione contro i nemici
        /// </summary>
        public float RepelForce;
        
        /// <summary>
        /// Indica se l'abilità è attualmente attiva
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Indica se l'abilità è disponibile per l'uso
        /// </summary>
        public bool IsAvailable => CooldownRemaining <= 0 && !IsActive;
    }
}