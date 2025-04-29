// Path: Assets/_Project/ECS/Components/Input/AbilityInputComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Systems.Abilities;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che gestisce l'input relativo all'attivazione di abilità speciali.
    /// Tiene traccia dei comandi per attivare le abilità uniche di ogni personaggio.
    /// </summary>
    [Serializable]
    public struct AbilityInputComponent : IComponentData
    {
        /// <summary>
        /// Indica se il comando di attivazione abilità è stato premuto in questo frame
        /// </summary>
        public bool ActivateAbility;
        
        /// <summary>
        /// Posizione target per abilità che richiedono un bersaglio
        /// </summary>
        public float2 TargetPosition;
        
        /// <summary>
        /// Tipo di abilità attualmente associata al personaggio
        /// </summary>
        public AbilityType CurrentAbilityType;
        
        /// <summary>
        /// Resetta lo stato di input per evitare input duplicati
        /// </summary>
        public void Reset()
        {
            ActivateAbility = false;
        }
        
        /// <summary>
        /// Crea una nuova istanza con valori predefiniti
        /// </summary>
        /// <returns>Un nuovo AbilityInputComponent con valori predefiniti</returns>
        public static AbilityInputComponent Default()
        {
            return new AbilityInputComponent
            {
                ActivateAbility = false,
                TargetPosition = float2.zero,
                CurrentAbilityType = AbilityType.None
            };
        }
    }
}