using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che gestisce i parametri di movimento di un'entità,
    /// inclusi velocità, salto, scivolata e altri attributi di movimento.
    /// </summary>
    [Serializable]
    public struct MovementComponent : IComponentData
    {
        // Velocità di movimento
        
        /// <summary>
        /// Velocità base del personaggio
        /// </summary>
        public float BaseSpeed;
        
        /// <summary>
        /// Velocità corrente, modificata da potenziamenti o penalità
        /// </summary>
        public float CurrentSpeed;
        
        /// <summary>
        /// Velocità massima raggiungibile
        /// </summary>
        public float MaxSpeed;
        
        /// <summary>
        /// Tasso di accelerazione
        /// </summary>
        public float Acceleration;
        
        // Parametri di salto
        
        /// <summary>
        /// Forza del salto
        /// </summary>
        public float JumpForce;
        
        /// <summary>
        /// Numero massimo di salti in aria consecutivi
        /// </summary>
        public int MaxJumps;
        
        /// <summary>
        /// Salti rimanenti prima di toccare terra
        /// </summary>
        public int RemainingJumps;
        
        /// <summary>
        /// Indica se l'entità sta attualmente saltando
        /// </summary>
        public bool IsJumping;
        
        // Parametri di scivolata
        
        /// <summary>
        /// Durata massima della scivolata in secondi
        /// </summary>
        public float SlideDuration;
        
        /// <summary>
        /// Tempo rimanente della scivolata corrente
        /// </summary>
        public float SlideTimeRemaining;
        
        /// <summary>
        /// Indica se l'entità sta attualmente scivolando
        /// </summary>
        public bool IsSliding;
        
        /// <summary>
        /// Moltiplicatore di velocità durante la scivolata
        /// </summary>
        public float SlideSpeedMultiplier;
        
        // Altri stati di movimento
        
        /// <summary>
        /// Indica se l'entità si sta muovendo
        /// </summary>
        public bool IsMoving;
        
        /// <summary>
        /// Direzione attuale del movimento
        /// </summary>
        public float3 MoveDirection;
        
        /// <summary>
        /// Genera un MovementComponent con valori di default per un personaggio giocabile
        /// </summary>
        public static MovementComponent DefaultPlayer()
        {
            return new MovementComponent
            {
                BaseSpeed = 5.0f,
                CurrentSpeed = 5.0f,
                MaxSpeed = 10.0f,
                Acceleration = 15.0f,
                
                JumpForce = 8.0f,
                MaxJumps = 1,
                RemainingJumps = 1,
                IsJumping = false,
                
                SlideDuration = 1.0f,
                SlideTimeRemaining = 0.0f,
                IsSliding = false,
                SlideSpeedMultiplier = 1.5f,
                
                IsMoving = false,
                MoveDirection = float3.zero
            };
        }
        
        /// <summary>
        /// Inizia un salto, se possibile
        /// </summary>
        /// <returns>True se il salto è stato avviato, false altrimenti</returns>
        public bool TryJump()
        {
            if (RemainingJumps > 0)
            {
                IsJumping = true;
                RemainingJumps--;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Inizia una scivolata, se possibile
        /// </summary>
        /// <returns>True se la scivolata è stata avviata, false altrimenti</returns>
        public bool TrySlide()
        {
            if (!IsSliding && IsMoving)
            {
                IsSliding = true;
                SlideTimeRemaining = SlideDuration;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Reimposta i salti disponibili quando il personaggio tocca terra
        /// </summary>
        public void ResetJumps()
        {
            RemainingJumps = MaxJumps;
            IsJumping = false;
        }
    }
}