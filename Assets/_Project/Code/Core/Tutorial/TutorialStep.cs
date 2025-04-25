using System;
using UnityEngine;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Definisce un singolo passo del tutorial con tutte le informazioni necessarie 
    /// per guidare il giocatore attraverso una specifica meccanica o azione.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        [Header("Step Info")]
        public string stepName = "New Step";
        [TextArea(3, 5)]
        public string instructionText = "Premi il pulsante per continuare";
        [TextArea(3, 5)]
        public string keyboardInstructions = ""; // Istruzioni specifiche per tastiera
        [TextArea(3, 5)]
        public string touchInstructions = ""; // Istruzioni specifiche per touchscreen
        public Sprite instructionIcon;
        
        [Header("Step Type")]
        public TutorialStepType stepType = TutorialStepType.KeyPress;
        
        [Header("Key Press Settings")]
        [Tooltip("Usato quando stepType è KeyPress")]
        public TutorialKeyAction keyAction = TutorialKeyAction.None;
        
        [Header("Trigger Settings")]
        [Tooltip("Usato quando stepType è Trigger")]
        public TutorialTriggerAction triggerAction = TutorialTriggerAction.None;
        public string objectiveId = ""; // ID dell'obiettivo da completare
        public int checkpointIndex = -1; // Indice del checkpoint da raggiungere
        public string collectibleType = ""; // Tipo di collezionabile da raccogliere
        
        [Header("Time Settings")]
        [Tooltip("Usato quando stepType è TimeBased")]
        public float timeToComplete = 5f;
        
        [Header("Visual Hints")]
        public bool useHintArrow = false;
        public Vector3 hintArrowPosition = Vector3.zero;
        public bool useWorldSpaceArrow = false;
        public Vector3 hintArrowWorldPosition = Vector3.zero;
        public string targetTag = ""; // Tag dell'oggetto da evidenziare
        public bool highlightTargetUI = false;
        public string targetUIElementName = "";
        
        [Header("Gameplay")]
        public bool activateEnemies = false; // Se attivare i nemici durante questo step
        public bool allowPlayerDamage = true; // Se il giocatore può essere danneggiato
        public bool allowPlayerFailure = true; // Se il giocatore può fallire lo step
        
        [Header("Completion")]
        public AudioClip completionSound;
        public GameObject completionVFX;
        public string completionMessage = "";
    }
    
    /// <summary>
    /// Enum per i tipi di step del tutorial
    /// </summary>
    public enum TutorialStepType
    {
        KeyPress,   // Richiede la pressione di un tasto specifico
        Trigger,    // Richiede il verificarsi di un evento specifico (salto, scivolata, ecc.)
        TimeBased   // Lo step si completa dopo un certo tempo
    }
    
    /// <summary>
    /// Enum per le azioni che possono triggerare il completamento di uno step
    /// </summary>
    public enum TutorialTriggerAction
    {
        None,
        Jump,
        Slide,
        UseAbility,
        ActivateFocusTime,
        ExitFocusTime,
        CollectItem,
        UseItem,
        SelectItem,
        TakeDamage,
        AvoidObstacle,
        ReachCheckpoint,
        DefeatEnemy,
        CompleteObjective
    }
    
    /// <summary>
    /// Enum per le azioni da tastiera che possono completare uno step
    /// </summary>
    public enum TutorialKeyAction
    {
        None,
        PressJump,
        PressSlide,
        PressFocusTime,
        PressAbility,
        PressLeftRight,
        PressPause,
        PressAny
    }
}