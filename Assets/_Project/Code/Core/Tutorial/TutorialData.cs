using System.Collections.Generic;
using UnityEngine;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// ScriptableObject che contiene i dati per un livello tutorial completo,
    /// includendo tutti i passi, messaggi e impostazioni.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTutorialData", menuName = "RunawayHeroes/Tutorial/TutorialData")]
    public class TutorialData : ScriptableObject
    {
        [Header("Tutorial Info")]
        public string tutorialName = "New Tutorial";
        [TextArea(3, 5)]
        public string tutorialDescription = "Tutorial description";
        public Sprite tutorialIcon;
        public int tutorialLevel = 0; // 0-based index
        
        [Header("Completion")]
        [TextArea(3, 5)]
        public string completionMessage = "Congratulations! You've completed this tutorial.";
        public string nextLevelName = "";
        public bool isLastTutorialLevel = false;
        public float completionDelay = 2.0f;
        
        [Header("Tutorial Steps")]
        public List<TutorialStep> steps = new List<TutorialStep>();
        
        [Header("Advanced Settings")]
        public bool allowSkipping = true;
        public bool disableEnemiesUntilRelevant = true;
        public float initialDelay = 1.0f;
        public float stepTransitionDelay = 0.5f;
        
        [Header("Level Specific Settings")]
        public bool enableFocusTime = true;
        public bool enableSpecialAbility = true;
        public int initialPlayerHealth = 100;
        public float runSpeed = 5.0f;
        
        /// <summary>
        /// Validate this asset to ensure it has all required data
        /// </summary>
        public bool Validate()
        {
            if (string.IsNullOrEmpty(tutorialName))
            {
                Debug.LogError($"Tutorial data {name} is missing a name!");
                return false;
            }
            
            if (steps == null || steps.Count == 0)
            {
                Debug.LogError($"Tutorial data {name} has no steps!");
                return false;
            }
            
            // Validate each step
            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStep step = steps[i];
                
                if (string.IsNullOrEmpty(step.stepName))
                {
                    Debug.LogError($"Step {i} in tutorial {name} is missing a name!");
                    return false;
                }
                
                if (string.IsNullOrEmpty(step.instructionText))
                {
                    Debug.LogError($"Step {i} ({step.stepName}) in tutorial {name} is missing instruction text!");
                    return false;
                }
                
                // Additional validation based on step type
                switch (step.stepType)
                {
                    case TutorialStepType.KeyPress:
                        if (step.keyAction == TutorialKeyAction.None)
                        {
                            Debug.LogWarning($"Step {i} ({step.stepName}) in tutorial {name} is a KeyPress step but has no key action specified.");
                        }
                        break;
                        
                    case TutorialStepType.Trigger:
                        if (step.triggerAction == TutorialTriggerAction.None)
                        {
                            Debug.LogWarning($"Step {i} ({step.stepName}) in tutorial {name} is a Trigger step but has no trigger action specified.");
                        }
                        break;
                        
                    case TutorialStepType.TimeBased:
                        if (step.timeToComplete <= 0)
                        {
                            Debug.LogWarning($"Step {i} ({step.stepName}) in tutorial {name} is a TimeBased step but has an invalid time of {step.timeToComplete}.");
                        }
                        break;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Create tutorial data for Level 1 (Primi Passi) with default values
        /// </summary>
        public static TutorialData CreateLevel1Data()
        {
            TutorialData data = CreateInstance<TutorialData>();
            data.tutorialName = "First Steps";
            data.tutorialDescription = "Learn the basic movement controls.";
            data.tutorialLevel = 0;
            data.completionMessage = "Great job! You've learned how to jump over obstacles.";
            
            // Create first step
            TutorialStep step1 = new TutorialStep
            {
                stepName = "Welcome",
                instructionText = "Welcome to the Training Center! In this tutorial, you'll learn how to move and jump.",
                keyboardInstructions = "Press any key to continue.",
                touchInstructions = "Tap anywhere to continue.",
                stepType = TutorialStepType.KeyPress,
                keyAction = TutorialKeyAction.PressAny,
                timeToComplete = 5f
            };
            data.steps.Add(step1);
            
            // Create second step
            TutorialStep step2 = new TutorialStep
            {
                stepName = "Basic Jump",
                instructionText = "Press the jump button to jump over obstacles.",
                keyboardInstructions = "Press SPACE to jump.",
                touchInstructions = "Tap the screen to jump.",
                stepType = TutorialStepType.Trigger,
                triggerAction = TutorialTriggerAction.Jump,
                useHintArrow = true,
                timeToComplete = 10f
            };
            data.steps.Add(step2);
            
            // Create more steps as needed...
            
            return data;
        }
        
        /// <summary>
        /// Create tutorial data for Level 2 (Scivolata Perfetta) with default values
        /// </summary>
        public static TutorialData CreateLevel2Data()
        {
            TutorialData data = CreateInstance<TutorialData>();
            data.tutorialName = "Perfect Slide";
            data.tutorialDescription = "Learn how to slide under obstacles.";
            data.tutorialLevel = 1;
            data.completionMessage = "Excellent! You've mastered the sliding technique.";
            
            // Create first step
            TutorialStep step1 = new TutorialStep
            {
                stepName = "Basic Slide",
                instructionText = "Use the slide command to go under low obstacles.",
                keyboardInstructions = "Press S or DOWN ARROW to slide.",
                touchInstructions = "Swipe DOWN to slide.",
                stepType = TutorialStepType.Trigger,
                triggerAction = TutorialTriggerAction.Slide,
                useHintArrow = true,
                timeToComplete = 10f
            };
            data.steps.Add(step1);
            
            // Create more steps as needed...
            
            return data;
        }
        
        /// <summary>
        /// Create tutorial data for a specific level by index
        /// </summary>
        public static TutorialData CreateDataForLevel(int levelIndex)
        {
            switch (levelIndex)
            {
                case 0:
                    return CreateLevel1Data();
                case 1:
                    return CreateLevel2Data();
                // Add cases for other levels
                default:
                    Debug.LogError($"No default tutorial data available for level index {levelIndex}");
                    return null;
            }
        }
    }
}