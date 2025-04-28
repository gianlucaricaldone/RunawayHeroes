// Path: Assets/_Project/ECS/Components/Abilities/NatureCallAbilityComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Richiamo della Natura" di Maya.
    /// Evoca animali alleati temporanei che distraggono i nemici.
    /// </summary>
    [Serializable]
    public struct NatureCallAbilityComponent : IComponentData
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
        /// Numero massimo di alleati evocabili
        /// </summary>
        public int MaxAllies;
        
        /// <summary>
        /// Raggio di evocazione degli alleati
        /// </summary>
        public float AllySummonRadius;
        
        /// <summary>
        /// Durata della distrazione causata dagli alleati
        /// </summary>
        public float AllyDistractDuration;
        
        /// <summary>
        /// Indica se l'abilità è attualmente attiva
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Elenco degli alleati attualmente attivi
        /// </summary>
        public FixedList32Bytes<Entity> CurrentAllies;
        
        /// <summary>
        /// Indica se l'abilità è disponibile per l'uso
        /// </summary>
        public bool IsAvailable => CooldownRemaining <= 0 && !IsActive;
    }
}