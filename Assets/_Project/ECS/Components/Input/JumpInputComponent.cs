// Path: Assets/_Project/ECS/Components/Input/JumpInputComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che gestisce l'input di salto per il personaggio.
    /// Viene aggiornato ogni frame dal sistema di input.
    /// </summary>
    [Serializable]
    public struct JumpInputComponent : IComponentData
    {
        /// <summary>
        /// Indica se il pulsante di salto è stato premuto in questo frame
        /// </summary>
        public bool JumpPressed;
        
        /// <summary>
        /// Forza extra da applicare al salto (moltiplicatore)
        /// </summary>
        public float JumpForceMultiplier;
    }
}