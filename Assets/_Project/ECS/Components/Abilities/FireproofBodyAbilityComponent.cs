// Path: Assets/_Project/ECS/Components/Abilities/FireproofBodyAbilityComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Corpo Ignifugo" di Ember.
    /// Trasformazione in forma ignea che permette di attraversare la lava.
    /// </summary>
    [Serializable]
    public struct FireproofBodyAbilityComponent : IComponentData
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
        /// Potenza dell'aura di calore che danneggia i nemici vicini
        /// </summary>
        public float HeatAura;
        
        /// <summary>
        /// Indica se la camminata sulla lava è attualmente attiva
        /// </summary>
        public bool LavaWalkingActive;
        
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