// File: Assets/_Project/ECS/Components/Input/FocusTimeInputComponent.cs

using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che gestisce l'input relativo al sistema Focus Time,
    /// includendo l'attivazione/disattivazione e la selezione di oggetti.
    /// </summary>
    [Serializable]
    public struct FocusTimeInputComponent : IComponentData
    {
        /// <summary>
        /// Flag che indica se è stata richiesta l'attivazione del Focus Time
        /// </summary>
        public bool ActivateFocusTime;
        
        /// <summary>
        /// Flag che indica se è stata richiesta la disattivazione del Focus Time
        /// </summary>
        public bool DeactivateFocusTime;
        
        /// <summary>
        /// Indice dell'oggetto selezionato nell'interfaccia radiale (-1 se nessuno)
        /// </summary>
        public int SelectedItemIndex;
        
        /// <summary>
        /// Flag che indica se è stato rilevato un nuovo oggetto nel raggio d'azione
        /// </summary>
        public bool NewItemDetected;
        
        /// <summary>
        /// Entità del nuovo oggetto rilevato (Entity.Null se nessuno)
        /// </summary>
        public Entity NewItemEntity;
        
        /// <summary>
        /// Posizione del tocco/cursore durante il Focus Time
        /// </summary>
        public float2 FocusPointerPosition;
        
        /// <summary>
        /// Durata della pressione del pulsante Focus Time
        /// </summary>
        public float ActivationHoldTime;
        
        /// <summary>
        /// Crea un nuovo FocusTimeInputComponent con valori predefiniti
        /// </summary>
        /// <returns>Componente inizializzato con valori predefiniti</returns>
        public static FocusTimeInputComponent Default()
        {
            return new FocusTimeInputComponent
            {
                ActivateFocusTime = false,
                DeactivateFocusTime = false,
                SelectedItemIndex = -1,
                NewItemDetected = false,
                NewItemEntity = Entity.Null,
                FocusPointerPosition = float2.zero,
                ActivationHoldTime = 0.0f
            };
        }
        
        /// <summary>
        /// Reimposta gli input a singolo frame per evitare input ripetuti
        /// </summary>
        public void Reset()
        {
            ActivateFocusTime = false;
            DeactivateFocusTime = false;
            SelectedItemIndex = -1;
            NewItemDetected = false;
            NewItemEntity = Entity.Null;
            ActivationHoldTime = 0.0f;
        }
    }
}