using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che memorizza tutti gli input del giocatore in un dato frame.
    /// Centralizza gli input provenienti da touch, tasti, o altri controlli,
    /// convertendoli in uno stato coerente per i sistemi di movimento e abilità.
    /// </summary>
    public struct InputComponent : IComponentData
    {
        /// <summary>
        /// Movimento laterale del giocatore, range da -1 (sinistra) a 1 (destra)
        /// </summary>
        public float LateralMovement;
        
        /// <summary>
        /// Direzione di movimento normalizzata (x: laterale, z: avanti)
        /// </summary>
        public float2 MoveDirection;
        
        /// <summary>
        /// Flag che indica se il movimento è abilitato
        /// </summary>
        public bool IsMovementEnabled;
        
        /// <summary>
        /// Flag che indica se il salto è stato premuto in questo frame
        /// </summary>
        public bool JumpPressed;
        
        /// <summary>
        /// Flag che indica se la scivolata è stata attivata in questo frame
        /// </summary>
        public bool SlidePressed;
        
        /// <summary>
        /// Flag che indica se il Focus Time è stato attivato in questo frame
        /// </summary>
        public bool FocusTimePressed;
        
        /// <summary>
        /// Flag che indica se l'abilità speciale è stata attivata in questo frame
        /// </summary>
        public bool AbilityPressed;
        
        /// <summary>
        /// Flag che indica se il cambio personaggio è stato attivato in questo frame
        /// </summary>
        public bool CharacterSwitchPressed;
        
        /// <summary>
        /// Posizione del tocco sullo schermo (per selezione oggetti in Focus Time)
        /// </summary>
        public float2 TouchPosition;
        
        /// <summary>
        /// Flag che indica se lo schermo è stato toccato
        /// </summary>
        public bool TouchActive;
        
        /// <summary>
        /// Durata del tocco corrente in secondi
        /// </summary>
        public float TouchDuration;
        
        /// <summary>
        /// Crea un nuovo InputComponent con valori predefiniti
        /// </summary>
        /// <returns>InputComponent inizializzato con tutti gli input a zero/false</returns>
        public static InputComponent Default()
        {
            return new InputComponent
            {
                LateralMovement = 0f,
                MoveDirection = new float2(0, 1), // Default: avanti
                IsMovementEnabled = true, // Per default il movimento è abilitato
                JumpPressed = false,
                SlidePressed = false,
                FocusTimePressed = false,
                AbilityPressed = false,
                CharacterSwitchPressed = false,
                TouchPosition = float2.zero,
                TouchActive = false,
                TouchDuration = 0f
            };
        }
        
        /// <summary>
        /// Resetta tutti gli input "premuti" a false, mantenendo solo gli stati analogici
        /// </summary>
        public void ResetPressedInputs()
        {
            JumpPressed = false;
            SlidePressed = false;
            FocusTimePressed = false;
            AbilityPressed = false;
            CharacterSwitchPressed = false;
        }
    }
}