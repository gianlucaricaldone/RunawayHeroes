// Path: Assets/_Project/ECS/Components/Input/SlideInputComponent.cs
using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che gestisce l'input di scivolata per il personaggio.
    /// Viene aggiornato ogni frame dal sistema di input.
    /// </summary>
    [Serializable]
    public struct SlideInputComponent : IComponentData
    {
        /// <summary>
        /// Indica se il comando di scivolata è stato premuto in questo frame
        /// </summary>
        public bool SlidePressed;
        
        /// <summary>
        /// Moltiplicatore di forza per la scivolata
        /// </summary>
        public float SlideForceMultiplier;
        
        /// <summary>
        /// Durata personalizzata della scivolata, se diversa da zero sostituisce
        /// il valore predefinito nel MovementComponent
        /// </summary>
        public float CustomSlideDuration;
        
        /// <summary>
        /// Resetta lo stato di input per evitare input duplicati
        /// </summary>
        public void Reset()
        {
            SlidePressed = false;
        }
        
        /// <summary>
        /// Crea una nuova istanza con valori predefiniti
        /// </summary>
        /// <returns>Un nuovo SlideInputComponent con valori predefiniti</returns>
        public static SlideInputComponent Default()
        {
            return new SlideInputComponent
            {
                SlidePressed = false,
                SlideForceMultiplier = 1.0f,
                CustomSlideDuration = 0.0f
            };
        }
    }
}