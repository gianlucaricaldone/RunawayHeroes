// Path: Assets/_Project/ECS/Components/Gameplay/ObstacleComponent.cs
using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che rappresenta un ostacolo nel gioco.
    /// Contiene proprietà come dimensioni, resistenza e danno causato.
    /// </summary>
    [Serializable]
    public struct ObstacleComponent : IComponentData
    {
        /// <summary>
        /// Altezza dell'ostacolo, usata per determinare se può essere superato con scivolata
        /// </summary>
        public float Height;
        
        /// <summary>
        /// Larghezza dell'ostacolo
        /// </summary>
        public float Width;
        
        /// <summary>
        /// Raggio di collisione dell'ostacolo
        /// </summary>
        public float CollisionRadius;
        
        /// <summary>
        /// Resistenza dell'ostacolo, determina se può essere sfondato
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// Danno inflitto in caso di collisione (0 per calcolo automatico basato sulla velocità)
        /// </summary>
        public float DamageValue;
        
        /// <summary>
        /// Se true, l'ostacolo può essere completamente distrutto
        /// </summary>
        public bool IsDestructible;
        
        /// <summary>
        /// Crea un nuovo ObstacleComponent con valori predefiniti per un ostacolo piccolo
        /// </summary>
        public static ObstacleComponent CreateSmall()
        {
            return new ObstacleComponent
            {
                Height = 0.3f,
                Width = 0.5f,
                CollisionRadius = 0.4f,
                Strength = 50.0f,
                DamageValue = 10.0f,
                IsDestructible = true
            };
        }
        
        /// <summary>
        /// Crea un nuovo ObstacleComponent con valori predefiniti per un ostacolo medio
        /// </summary>
        public static ObstacleComponent CreateMedium()
        {
            return new ObstacleComponent
            {
                Height = 0.8f,
                Width = 1.0f,
                CollisionRadius = 0.6f,
                Strength = 100.0f,
                DamageValue = 25.0f,
                IsDestructible = true
            };
        }
        
        /// <summary>
        /// Crea un nuovo ObstacleComponent con valori predefiniti per un ostacolo grande
        /// </summary>
        public static ObstacleComponent CreateLarge()
        {
            return new ObstacleComponent
            {
                Height = 1.5f,
                Width = 2.0f,
                CollisionRadius = 1.0f,
                Strength = 200.0f,
                DamageValue = 50.0f,
                IsDestructible = false
            };
        }
    }
}