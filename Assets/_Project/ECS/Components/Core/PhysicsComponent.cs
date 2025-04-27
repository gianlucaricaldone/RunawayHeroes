using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Core
{
    /// <summary>
    /// Componente che memorizza le proprietà fisiche di un'entità, come velocità, massa e accelerazione.
    /// Utilizzato per simulare movimenti e interazioni fisiche nel gioco.
    /// </summary>
    [Serializable]
    public struct PhysicsComponent : IComponentData
    {
        /// <summary>
        /// Velocità corrente dell'entità
        /// </summary>
        public float3 Velocity;
        
        /// <summary>
        /// Accelerazione corrente dell'entità
        /// </summary>
        public float3 Acceleration;
        
        /// <summary>
        /// Massa dell'entità, influisce su forze e collisioni
        /// </summary>
        public float Mass;
        
        /// <summary>
        /// Forza di gravità applicata all'entità (default: 9.81 in direzione Y negativa)
        /// </summary>
        public float Gravity;
        
        /// <summary>
        /// Coefficiente di attrito, influisce sulla decelerazione
        /// </summary>
        public float Friction;
        
        /// <summary>
        /// Se l'entità è soggetta alla gravità
        /// </summary>
        public bool UseGravity;
        
        /// <summary>
        /// Se l'entità è considerata a terra (non in salto o caduta)
        /// </summary>
        public bool IsGrounded;
        
        /// <summary>
        /// Crea un nuovo PhysicsComponent con valori predefiniti
        /// </summary>
        /// <returns>PhysicsComponent inizializzato con valori predefiniti</returns>
        public static PhysicsComponent Default()
        {
            return new PhysicsComponent
            {
                Velocity = float3.zero,
                Acceleration = float3.zero,
                Mass = 1.0f,
                Gravity = 9.81f,
                Friction = 0.1f,
                UseGravity = true,
                IsGrounded = true
            };
        }
        
        /// <summary>
        /// Applica una forza all'entità, modificando la sua accelerazione in base alla massa
        /// </summary>
        /// <param name="force">Forza da applicare</param>
        /// <returns>Nuova accelerazione risultante</returns>
        public float3 ApplyForce(float3 force)
        {
            // F = ma, quindi a = F/m
            return force / Mass;
        }
    }
}