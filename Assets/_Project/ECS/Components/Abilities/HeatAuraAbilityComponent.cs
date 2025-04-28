// Path: Assets/_Project/ECS/Components/Abilities/HeatAuraAbilityComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Aura di Calore" di Kai.
    /// Crea un campo che scioglie il ghiaccio e protegge dal freddo.
    /// </summary>
    [Serializable]
    public struct HeatAuraAbilityComponent : IComponentData
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
        /// Raggio dell'aura di calore
        /// </summary>
        public float AuraRadius;
        
        /// <summary>
        /// Velocità di scioglimento del ghiaccio
        /// </summary>
        public float MeltIceRate;
        
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