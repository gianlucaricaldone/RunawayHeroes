// Path: Assets/_Project/ECS/Components/Abilities/ControlledGlitchAbilityComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Glitch Controllato" di Neo.
    /// Deforma brevemente la realtà, permettendo di attraversare barriere digitali.
    /// </summary>
    [Serializable]
    public struct ControlledGlitchAbilityComponent : IComponentData
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
        /// Distanza massima del glitch
        /// </summary>
        public float GlitchDistance;
        
        /// <summary>
        /// Capacità di attraversare barriere
        /// </summary>
        public bool BarrierPenetration;
        
        /// <summary>
        /// Indica se l'abilità è attualmente attiva
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Indica se il processo di teletrasporto è iniziato
        /// </summary>
        public bool TeleportStarted;
        
        /// <summary>
        /// Indica se il glitch è stato completato
        /// </summary>
        public bool GlitchCompleted;
        
        /// <summary>
        /// Posizione iniziale del glitch
        /// </summary>
        public float3 StartPosition;
        
        /// <summary>
        /// Posizione target del glitch
        /// </summary>
        public float3 TargetPosition;
        
        /// <summary>
        /// Indica se l'abilità è disponibile per l'uso
        /// </summary>
        public bool IsAvailable => CooldownRemaining <= 0 && !IsActive;
    }
}