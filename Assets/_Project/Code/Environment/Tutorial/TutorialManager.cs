using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using RunawayHeroes.Core;
using RunawayHeroes.Characters;
using RunawayHeroes.Items;
using RunawayHeroes.Manager;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Gestisce il sistema di tutorial, guidando il giocatore attraverso i controlli e le meccaniche di base.
    /// Permette di creare una sequenza di passi interattivi che insegnano al giocatore come giocare.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        #region Singleton
        public static TutorialManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Inizializzazione
            InitializeTutorialManager();
        }
        #endregion

        [Header("Tutorial Data")]
        [SerializeField] private TutorialData currentTutorialData;
        [SerializeField] private int currentStepIndex = -1;
        [SerializeField] private string[] tutorialLevelNames = new string[5] 
        { 
            "Level1_FirstSteps", 
            "Level2_PerfectSlide", 
            "Level3_ReadyReflexes", 
            "Level4_ItemPower", 
            "Level5_EscapeTrainer" 
        };
        [SerializeField] private int currentTutorialLevel = 0;

        [Header("UI References")]
        [SerializeField] private GameObject tutorialCanvas;
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image instructionIcon;
        [SerializeField] private GameObject hintArrow;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TextMeshProUGUI completionText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Image progressFill;
        [SerializeField] private Image keyboardHint;
        [SerializeField] private Image touchscreenHint;

        [Header("Tutorial Settings")]
        [SerializeField] private float initialDelay = 1f;
        [SerializeField] private float stepCompletionDelay = 1.5f;
        [SerializeField] private bool canSkipTutorial = true;
        [SerializeField] private bool useAutoProgress = true;
        [SerializeField] private bool disableEnemiesUntilRelevant = true;
        [SerializeField] private float hintArrowOffset = 1.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip stepCompletedSound;
        [SerializeField] private AudioClip tutorialCompletedSound;
        [SerializeField] private AudioClip buttonClickSound;

        // References to game systems
        private PlayerController playerController;
        private FocusTimeManager focusTimeManager;
        private AudioSource audioSource;
        private GameManager gameManager;

        // State variables
        private bool tutorialActive = false;
        private bool currentStepCompleted = false;
        private TutorialStep currentStep;
        private List<GameObject> tutorialObjects = new List<GameObject>();
        private Dictionary<string, GameObject> targetObjects = new Dictionary<string, GameObject>();
        private ControlType detectedControlType = ControlType.Unknown;
        private bool skipConfirmationDisplayed = false;
        private float stepStartTime;
        private GameObject currentTargetObject;
        private Vector3 hintArrowTargetPosition;
        private bool waitingForInput = false;
        private Coroutine hintArrowCoroutine;
        private Coroutine autoProgressCoroutine;
        private Coroutine inputCheckCoroutine;

        // Events
        public event Action<int> OnTutorialStepStarted;
        public event Action<int> OnTutorialStepCompleted;
        public event Action OnTutorialCompleted;
        public event Action<int> OnTutorialLevelStarted;
        public event Action<int> OnTutorialLevelCompleted;

        // Properties
        public bool IsTutorialActive => tutorialActive;
        public int CurrentStepIndex => currentStepIndex;
        public int CurrentTutorialLevel => currentTutorialLevel;
        public TutorialData CurrentTutorialData => currentTutorialData;
        public bool IsTutorialCompleted => PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
        public ControlType DetectedControlType => detectedControlType;

        #region Initialization
        private void InitializeTutorialManager()
        {
            // Get component reference
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Get external references
            gameManager = GameManager.Instance;

            // Hide UI initially
            if (tutorialCanvas)
                tutorialCanvas.SetActive(false);

            // Initialize events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Set up buttons
            if (nextButton)
                nextButton.onClick.AddListener(OnNextButtonClicked);

            if (skipButton)
                skipButton.onClick.AddListener(OnSkipButtonClicked);

            // Check if tutorial was completed before
            if (IsTutorialCompleted)
            {
                Debug.Log("Tutorial was previously completed. Set tutorialActive to false.");
                tutorialActive = false;
            }
            else
            {
                // Tutorial should be started when entering the first tutorial level
                Debug.Log("Tutorial not yet completed. Waiting for the first tutorial level to load.");
            }

            // Attempt to detect control type
            DetectControlType();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsTutorialLevel(scene.name))
            {
                // This is a tutorial level, get the level index
                int levelIndex = GetTutorialLevelIndex(scene.name);
                
                if (levelIndex >= 0)
                {
                    currentTutorialLevel = levelIndex;
                    StartCoroutine(InitializeTutorialLevel(initialDelay));
                }
            }
            else
            {
                // Not a tutorial level, hide tutorial UI
                if (tutorialCanvas)
                    tutorialCanvas.SetActive(false);
            }
        }

        private IEnumerator InitializeTutorialLevel(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Load the tutorial data for this level
            LoadTutorialDataForCurrentLevel();

            // Find necessary game components after scene load
            playerController = FindObjectOfType<PlayerController>();
            focusTimeManager = FindObjectOfType<FocusTimeManager>();

            // If we have valid tutorial data, start the tutorial
            if (currentTutorialData != null)
            {
                tutorialActive = true;
                StartTutorial();
                OnTutorialLevelStarted?.Invoke(currentTutorialLevel);
            }
            else
            {
                Debug.LogError($"Tutorial data not found for level {currentTutorialLevel}");
            }

            // Handle enemy activation based on settings
            if (disableEnemiesUntilRelevant)
            {
                SetEnemiesActive(false);
            }
        }

        private void LoadTutorialDataForCurrentLevel()
        {
            string resourcePath = $"Tutorial/TutorialData/Level{currentTutorialLevel + 1}Data";
            TutorialData loadedData = Resources.Load<TutorialData>(resourcePath);

            if (loadedData != null)
            {
                currentTutorialData = loadedData;
                Debug.Log($"Loaded tutorial data for level {currentTutorialLevel + 1}: {loadedData.tutorialName}");
            }
            else
            {
                Debug.LogError($"Could not load tutorial data from path: {resourcePath}");
                // Fallback to current data if it exists
                if (currentTutorialData == null)
                {
                    Debug.LogError("No fallback tutorial data available!");
                }
            }
        }

        private void Start()
        {
            // Set up skip button visibility based on setting
            if (skipButton)
                skipButton.gameObject.SetActive(canSkipTutorial);
        }

        private void OnDestroy()
        {
            // Clean up event subscriptions
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Clean up button listeners
            if (nextButton)
                nextButton.onClick.RemoveListener(OnNextButtonClicked);

            if (skipButton)
                skipButton.onClick.RemoveListener(OnSkipButtonClicked);
        }
        #endregion

        #region Tutorial Flow Management
        public void StartTutorial()
        {
            if (currentTutorialData == null || currentTutorialData.steps.Count == 0)
            {
                Debug.LogError("Tutorial data is missing or empty!");
                return;
            }

            tutorialActive = true;
            currentStepIndex = -1;

            // Show tutorial UI
            if (tutorialCanvas)
                tutorialCanvas.SetActive(true);

            // Subscribe to necessary events
            SubscribeToEvents();

            // Start first step
            NextStep();

            Debug.Log($"Tutorial started: {currentTutorialData.tutorialName}");
        }

        public void NextStep()
        {
            currentStepIndex++;
            
            if (currentStepIndex >= currentTutorialData.steps.Count)
            {
                // We've reached the end of steps, complete the tutorial
                CompleteTutorialLevel();
                return;
            }

            StartTutorialStep(currentStepIndex);
        }

        public void StartTutorialStep(int stepIndex)
        {
            if (!tutorialActive)
                return;

            if (stepIndex < 0 || stepIndex >= currentTutorialData.steps.Count)
            {
                Debug.LogWarning($"Invalid tutorial step index: {stepIndex}");
                return;
            }

            // Stop any existing coroutines
            if (hintArrowCoroutine != null)
                StopCoroutine(hintArrowCoroutine);

            if (autoProgressCoroutine != null)
                StopCoroutine(autoProgressCoroutine);

            if (inputCheckCoroutine != null)
                StopCoroutine(inputCheckCoroutine);

            // Set up new step
            currentStepIndex = stepIndex;
            currentStep = currentTutorialData.steps[currentStepIndex];
            currentStepCompleted = false;
            stepStartTime = Time.time;
            
            // Keep track of progress
            UpdateProgressUI();

            // Show step instructions
            ShowTutorialInstruction(currentStep);

            // Position hint arrow if needed
            UpdateHintArrow();

            // Check if we need to highlight UI elements
            if (currentStep.highlightTargetUI)
            {
                HighlightUIElement(currentStep.targetUIElementName);
            }

            // If this step requires a target object, find it
            if (!string.IsNullOrEmpty(currentStep.targetTag))
            {
                FindTargetObject(currentStep.targetTag);
            }

            // If this step activates enemies, do it now
            if (currentStep.activateEnemies && disableEnemiesUntilRelevant)
            {
                SetEnemiesActive(true);
            }

            // Start auto progress if it's a time-based step and auto progress is enabled
            if (currentStep.stepType == TutorialStepType.TimeBased && useAutoProgress)
            {
                autoProgressCoroutine = StartCoroutine(AutoProgressAfterDelay(currentStep.timeToComplete));
            }

            // Start input check if it's an input-based step
            if (currentStep.stepType == TutorialStepType.KeyPress)
            {
                inputCheckCoroutine = StartCoroutine(CheckForInput());
                waitingForInput = true;
            }

            // Trigger event
            OnTutorialStepStarted?.Invoke(currentStepIndex);

            Debug.Log($"Tutorial step started: {currentStep.stepName}");
        }

        private IEnumerator AutoProgressAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (!currentStepCompleted)
            {
                CompleteCurrentStep();
            }
        }

        private IEnumerator CheckForInput()
        {
            while (!currentStepCompleted)
            {
                // Check if the expected input is pressed
                bool inputDetected = false;

                switch (currentStep.keyAction)
                {
                    case TutorialKeyAction.PressJump:
                        inputDetected = Input.GetButtonDown("Jump");
                        break;
                    case TutorialKeyAction.PressSlide:
                        inputDetected = Input.GetButtonDown("Slide") || CheckSwipeDown();
                        break;
                    case TutorialKeyAction.PressFocusTime:
                        inputDetected = Input.GetButtonDown("FocusTime");
                        break;
                    case TutorialKeyAction.PressAbility:
                        inputDetected = Input.GetButtonDown("Ability");
                        break;
                    case TutorialKeyAction.PressLeftRight:
                        inputDetected = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.5f;
                        break;
                    case TutorialKeyAction.PressPause:
                        inputDetected = Input.GetButtonDown("Pause");
                        break;
                    case TutorialKeyAction.PressAny:
                        inputDetected = Input.anyKeyDown;
                        break;
                }

                if (inputDetected)
                {
                    CompleteCurrentStep();
                    break;
                }

                // Update control type detection
                if (Input.touchCount > 0)
                {
                    detectedControlType = ControlType.Touch;
                    UpdateControlTypeUI();
                }
                else if (Input.anyKeyDown)
                {
                    detectedControlType = ControlType.KeyboardMouse;
                    UpdateControlTypeUI();
                }

                yield return null;
            }

            waitingForInput = false;
        }

        // Simplified swipe down detection - in a real implementation you'd want a more robust touch handling system
        private bool CheckSwipeDown()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Moved)
                {
                    // Check for downward swipe
                    if (touch.deltaPosition.y < -50f) // Threshold for swipe
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void CompleteCurrentStep()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            currentStepCompleted = true;

            // Stop any running coroutines for this step
            if (autoProgressCoroutine != null)
                StopCoroutine(autoProgressCoroutine);

            if (inputCheckCoroutine != null)
                StopCoroutine(inputCheckCoroutine);

            waitingForInput = false;

            // Hide hint arrow
            if (hintArrow != null)
                hintArrow.SetActive(false);

            // Remove UI highlights
            ClearUIHighlights();

            // Play completion sound
            if (audioSource && stepCompletedSound)
                audioSource.PlayOneShot(stepCompletedSound);

            // Play completion VFX if specified
            if (currentStep.completionVFX != null)
            {
                GameObject vfx = Instantiate(currentStep.completionVFX, GetStepCompletionPosition(), Quaternion.identity);
                Destroy(vfx, 2f); // Destroy after 2 seconds
            }

            // Show completion message if provided
            if (!string.IsNullOrEmpty(currentStep.completionMessage))
            {
                ShowCompletionMessage(currentStep.completionMessage);
            }

            // Trigger event
            OnTutorialStepCompleted?.Invoke(currentStepIndex);

            Debug.Log($"Tutorial step completed: {currentStep.stepName}");

            // Proceed to next step after delay
            StartCoroutine(ProceedToNextStepAfterDelay());
        }

        private Vector3 GetStepCompletionPosition()
        {
            // If we have a target object, use its position
            if (currentTargetObject != null)
            {
                return currentTargetObject.transform.position;
            }
            
            // Otherwise use the hint arrow position if available
            if (hintArrow != null && hintArrow.activeSelf)
            {
                return hintArrow.transform.position;
            }
            
            // Otherwise use player position if available
            if (playerController != null)
            {
                return playerController.transform.position + Vector3.up * 2f;
            }
            
            // Fallback to center of screen in world space
            return Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, 10f));
        }

        private IEnumerator ProceedToNextStepAfterDelay()
        {
            yield return new WaitForSeconds(stepCompletionDelay);

            // Go to next step
            NextStep();
        }

        private void CompleteTutorialLevel()
        {
            if (!tutorialActive)
                return;

            Debug.Log($"Tutorial level {currentTutorialLevel + 1} completed");

            // Play completion sound
            if (audioSource && tutorialCompletedSound)
                audioSource.PlayOneShot(tutorialCompletedSound);

            // Show completion panel
            if (completionPanel && completionText)
            {
                completionPanel.SetActive(true);
                completionText.text = currentTutorialData.completionMessage;
                
                // Auto-hide after delay
                StartCoroutine(HideCompletionPanelAfterDelay(5f));
            }

            // Save progress for this level
            SaveTutorialProgress();

            // Trigger event
            OnTutorialLevelCompleted?.Invoke(currentTutorialLevel);

            // If this is the last tutorial level, mark tutorial as completed
            if (currentTutorialLevel >= tutorialLevelNames.Length - 1)
            {
                CompleteTutorialFully();
            }
            else
            {
                // Otherwise, prepare to load next level
                StartCoroutine(LoadNextTutorialLevel(5f));
            }
        }

        private IEnumerator LoadNextTutorialLevel(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Load next tutorial level
            int nextLevel = currentTutorialLevel + 1;
            if (nextLevel < tutorialLevelNames.Length)
            {
                string nextLevelName = tutorialLevelNames[nextLevel];
                SceneManager.LoadScene("Tutorial/" + nextLevelName);
            }
        }

        private void CompleteTutorialFully()
        {
            tutorialActive = false;

            // Mark tutorial as completed
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();

            // Unsubscribe from events
            UnsubscribeFromEvents();

            // Trigger event
            OnTutorialCompleted?.Invoke();

            Debug.Log("Complete tutorial fully!");

            // After a delay, load the main game
            StartCoroutine(LoadMainGameAfterDelay(5f));
        }

        private IEnumerator LoadMainGameAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Hide tutorial UI
            if (tutorialCanvas)
                tutorialCanvas.SetActive(false);

            // If we have a game manager, tell it we're done with tutorial
            if (gameManager != null)
            {
                gameManager.OnTutorialCompleted();
            }
            else
            {
                // Fallback if no game manager
                SceneManager.LoadScene("MainMenu");
            }
        }

        public void SkipTutorial()
        {
            if (!canSkipTutorial)
                return;
                
            Debug.Log("Tutorial skipped by user");

            // Mark all tutorial levels as completed
            for (int i = 0; i < tutorialLevelNames.Length; i++)
            {
                string key = $"TutorialLevel_{i}_Completed";
                PlayerPrefs.SetInt(key, 1);
            }

            // Mark entire tutorial as completed
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();

            // Hide UI
            if (tutorialCanvas)
                tutorialCanvas.SetActive(false);

            // Clean up
            tutorialActive = false;
            UnsubscribeFromEvents();

            // Tell Game Manager that tutorial is completed
            if (gameManager != null)
            {
                gameManager.OnTutorialCompleted();
            }
            else
            {
                SceneManager.LoadScene("MainMenu");
            }
        }

        private IEnumerator HideCompletionPanelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (completionPanel)
                completionPanel.SetActive(false);
        }
        #endregion

        #region UI Management
        private void ShowTutorialInstruction(TutorialStep step)
        {
            // Ensure the tutorial panel is visible
            if (tutorialCanvas)
                tutorialCanvas.SetActive(true);
                
            if (instructionPanel)
                instructionPanel.SetActive(true);

            // Set title and instruction text
            if (titleText)
                titleText.text = step.stepName;
                
            if (instructionText)
                instructionText.text = GetControlSpecificInstructions(step);

            // Set icon if provided
            if (instructionIcon && step.instructionIcon != null)
            {
                instructionIcon.sprite = step.instructionIcon;
                instructionIcon.gameObject.SetActive(true);
            }
            else if (instructionIcon)
            {
                instructionIcon.gameObject.SetActive(false);
            }

            // Update controls display based on detected input method
            UpdateControlTypeUI();
        }

        private string GetControlSpecificInstructions(TutorialStep step)
        {
            // If the step has different instructions for keyboard vs touch, return the appropriate one
            if (detectedControlType == ControlType.KeyboardMouse && !string.IsNullOrEmpty(step.keyboardInstructions))
            {
                return step.keyboardInstructions;
            }
            else if (detectedControlType == ControlType.Touch && !string.IsNullOrEmpty(step.touchInstructions))
            {
                return step.touchInstructions;
            }
            
            // Otherwise return the default instructions
            return step.instructionText;
        }

        private void UpdateControlTypeUI()
        {
            // Show the appropriate control hints based on detected input type
            if (keyboardHint != null)
            {
                keyboardHint.gameObject.SetActive(detectedControlType == ControlType.KeyboardMouse || detectedControlType == ControlType.Unknown);
            }
            
            if (touchscreenHint != null)
            {
                touchscreenHint.gameObject.SetActive(detectedControlType == ControlType.Touch);
            }
        }

        private void ShowCompletionMessage(string message)
        {
            // This could be implemented as a temporary pop-up message
            // For simplicity, we'll just update the instruction text
            if (instructionText)
            {
                string originalText = instructionText.text;
                instructionText.text = message;
                
                // Restore original text after delay
                StartCoroutine(RestoreTextAfterDelay(originalText, 2f));
            }
        }

        private IEnumerator RestoreTextAfterDelay(string originalText, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (instructionText && !currentStepCompleted)
            {
                instructionText.text = originalText;
            }
        }

        private void UpdateHintArrow()
        {
            if (hintArrow == null)
                return;

            // If this step doesn't use the hint arrow, hide it
            if (!currentStep.useHintArrow)
            {
                hintArrow.SetActive(false);
                return;
            }

            // Otherwise, position and show the hint arrow
            hintArrow.SetActive(true);

            // Determine the target position based on step settings
            if (currentStep.useWorldSpaceArrow)
            {
                // World space position directly
                hintArrowTargetPosition = currentStep.hintArrowWorldPosition;
            }
            else if (!string.IsNullOrEmpty(currentStep.targetTag))
            {
                // Target object with tag
                GameObject targetObj = GameObject.FindGameObjectWithTag(currentStep.targetTag);
                if (targetObj != null)
                {
                    currentTargetObject = targetObj;
                    hintArrowTargetPosition = targetObj.transform.position + Vector3.up * hintArrowOffset;
                }
                else
                {
                    Debug.LogWarning($"Could not find target object with tag: {currentStep.targetTag}");
                    hintArrow.SetActive(false);
                    return;
                }
            }
            else
            {
                // Fixed position
                hintArrowTargetPosition = currentStep.hintArrowPosition;
            }

            // Position the arrow
            hintArrow.transform.position = hintArrowTargetPosition;

            // Start arrow animation
            hintArrowCoroutine = StartCoroutine(AnimateHintArrow());
        }

        private IEnumerator AnimateHintArrow()
        {
            Vector3 basePosition = hintArrowTargetPosition;
            float amplitude = 0.3f;
            float frequency = 2f;
            
            while (hintArrow.activeSelf && !currentStepCompleted)
            {
                float time = Time.time;
                Vector3 offset = Vector3.up * Mathf.Sin(time * frequency) * amplitude;
                
                hintArrow.transform.position = basePosition + offset;
                hintArrow.transform.Rotate(Vector3.up, 80f * Time.deltaTime); // Rotate around Y axis
                
                yield return null;
            }
        }

        private void UpdateProgressUI()
        {
            if (progressFill != null && currentTutorialData != null && currentTutorialData.steps.Count > 0)
            {
                float progress = (float)(currentStepIndex + 1) / currentTutorialData.steps.Count;
                progressFill.fillAmount = progress;
            }
        }

        private void OnNextButtonClicked()
        {
            if (audioSource && buttonClickSound)
                audioSource.PlayOneShot(buttonClickSound);

            if (currentStepCompleted)
            {
                // Already completed, go to next step
                NextStep();
            }
            else
            {
                // Force complete current step
                CompleteCurrentStep();
            }
        }

        private void OnSkipButtonClicked()
        {
            if (audioSource && buttonClickSound)
                audioSource.PlayOneShot(buttonClickSound);

            if (!skipConfirmationDisplayed)
            {
                // Show confirmation dialog
                ShowSkipConfirmation();
                skipConfirmationDisplayed = true;
            }
            else
            {
                // User confirmed, skip tutorial
                skipConfirmationDisplayed = false;
                SkipTutorial();
            }
        }

        private void ShowSkipConfirmation()
        {
            // In a real implementation, you would show a proper dialog
            // For simplicity, we'll just change the skip button text
            if (skipButton && skipButton.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                skipButton.GetComponentInChildren<TextMeshProUGUI>().text = "Confirm Skip";
                
                // Reset after delay if not clicked again
                StartCoroutine(ResetSkipButtonAfterDelay(3f));
            }
        }

        private IEnumerator ResetSkipButtonAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (skipConfirmationDisplayed)
            {
                skipConfirmationDisplayed = false;
                
                if (skipButton && skipButton.GetComponentInChildren<TextMeshProUGUI>() != null)
                {
                    skipButton.GetComponentInChildren<TextMeshProUGUI>().text = "Skip Tutorial";
                }
            }
        }

        private void HighlightUIElement(string elementName)
        {
            if (string.IsNullOrEmpty(elementName))
                return;

            GameObject uiElement = null;
            
            // Check if we've already found this element before
            if (targetObjects.ContainsKey(elementName))
            {
                uiElement = targetObjects[elementName];
            }
            else
            {
                // Try to find the UI element by name
                Transform[] allTransforms = FindObjectsOfType<Transform>();
                foreach (Transform t in allTransforms)
                {
                    if (t.name == elementName)
                    {
                        uiElement = t.gameObject;
                        targetObjects[elementName] = uiElement;
                        break;
                    }
                }
            }

            if (uiElement != null)
            {
                // Apply highlight (in a real implementation, you'd add a glow or outline)
                Image image = uiElement.GetComponent<Image>();
                if (image != null)
                {
                    // Store original color and make it glow
                    Color originalColor = image.color;
                    image.color = Color.white;
                    
                    // Restore after tutorial step
                    StartCoroutine(RestoreUIColorAfterStepCompletion(image, originalColor));
                }
            }
            else
            {
                Debug.LogWarning($"Could not find UI element with name: {elementName}");
            }
        }

        private IEnumerator RestoreUIColorAfterStepCompletion(Image image, Color originalColor)
        {
            while (!currentStepCompleted)
            {
                yield return null;
            }
            
            if (image != null)
            {
                image.color = originalColor;
            }
        }

        private void ClearUIHighlights()
        {
            // In a real implementation, this would remove all UI highlights
        }

        private void FindTargetObject(string tag)
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag(tag);
            if (targetObj != null)
            {
                currentTargetObject = targetObj;
                
                // Store for future reference
                if (!targetObjects.ContainsKey(tag))
                {
                    targetObjects[tag] = targetObj;
                }
            }
            else
            {
                Debug.LogWarning($"Could not find target object with tag: {tag}");
            }
        }
        #endregion

        #region Event Handling
        private void SubscribeToEvents()
        {
            if (playerController != null)
            {
                playerController.OnJump += HandleJumpEvent;
                playerController.OnSlide += HandleSlideEvent;
                playerController.OnSpecialAbility += HandleSpecialAbilityEvent;
                playerController.OnDamaged += HandlePlayerDamaged;
            }

            if (focusTimeManager != null)
            {
                focusTimeManager.OnFocusTimeActivated += HandleFocusTimeActivated;
                focusTimeManager.OnFocusTimeDeactivated += HandleFocusTimeDeactivated;
                focusTimeManager.OnItemSelected += HandleItemSelected;
                focusTimeManager.OnItemUsed += HandleItemUsed;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (playerController != null)
            {
                playerController.OnJump -= HandleJumpEvent;
                playerController.OnSlide -= HandleSlideEvent;
                playerController.OnSpecialAbility -= HandleSpecialAbilityEvent;
                playerController.OnDamaged -= HandlePlayerDamaged;
            }

            if (focusTimeManager != null)
            {
                focusTimeManager.OnFocusTimeActivated -= HandleFocusTimeActivated;
                focusTimeManager.OnFocusTimeDeactivated -= HandleFocusTimeDeactivated;
                focusTimeManager.OnItemSelected -= HandleItemSelected;
                focusTimeManager.OnItemUsed -= HandleItemUsed;
            }
        }

        private void HandleJumpEvent()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.Jump)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleSlideEvent()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.Slide)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleSpecialAbilityEvent()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.UseAbility)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleFocusTimeActivated()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.ActivateFocusTime)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleFocusTimeDeactivated()
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.ExitFocusTime)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleItemSelected(IUsableItem item)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.SelectItem)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleItemUsed(IUsableItem item)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.UseItem)
            {
                CompleteCurrentStep();
            }
        }

        private void HandlePlayerDamaged(float damage, Vector3 damageSource)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.TakeDamage)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleObjectiveCompleted(string objectiveId)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.CompleteObjective && 
                currentStep.objectiveId == objectiveId)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleCheckpointReached(int checkpointIndex)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.ReachCheckpoint && 
                currentStep.checkpointIndex == checkpointIndex)
            {
                CompleteCurrentStep();
            }
        }

        private void HandleCollectibleCollected(string collectibleType)
        {
            if (!tutorialActive || currentStepCompleted)
                return;

            if (currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.CollectItem && 
                currentStep.collectibleType == collectibleType)
            {
                CompleteCurrentStep();
            }
        }
        #endregion

        #region Utility Methods
        private void DetectControlType()
        {
            // Check if device has a touchscreen
            if (SystemInfo.deviceType == DeviceType.Handheld || Input.touchSupported)
            {
                detectedControlType = ControlType.Touch;
            }
            else
            {
                detectedControlType = ControlType.KeyboardMouse;
            }

            // Initial update of UI based on detected control type
            UpdateControlTypeUI();
            
            Debug.Log($"Detected control type: {detectedControlType}");
        }

        private bool IsTutorialLevel(string sceneName)
        {
            foreach (string tutorialLevelName in tutorialLevelNames)
            {
                if (sceneName.Contains(tutorialLevelName))
                    return true;
            }
            return false;
        }

        private int GetTutorialLevelIndex(string sceneName)
        {
            for (int i = 0; i < tutorialLevelNames.Length; i++)
            {
                if (sceneName.Contains(tutorialLevelNames[i]))
                    return i;
            }
            return -1;
        }

        private void SaveTutorialProgress()
        {
            // Save that this level was completed
            string key = $"TutorialLevel_{currentTutorialLevel}_Completed";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            
            Debug.Log($"Saved tutorial progress: {key} = 1");
        }

        private void SetEnemiesActive(bool active)
        {
            // Find all enemies in the scene and activate/deactivate them
            // This assumes they are tagged as "Enemy"
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                enemy.SetActive(active);
            }
            
            Debug.Log($"Set {enemies.Length} enemies active: {active}");
        }
        #endregion

        #region Public Interface Methods
        public void ForceCompleteCurrentStep()
        {
            if (!tutorialActive || currentStepCompleted)
                return;
                
            CompleteCurrentStep();
        }

        public void SetTutorialActive(bool active)
        {
            tutorialActive = active;
            
            if (tutorialCanvas)
                tutorialCanvas.SetActive(active);
                
            if (!active)
            {
                // Clean up any ongoing processes
                StopAllCoroutines();
                UnsubscribeFromEvents();
            }
        }

        public bool IsTutorialLevelCompleted(int levelIndex)
        {
            string key = $"TutorialLevel_{levelIndex}_Completed";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        public void ResetTutorialProgress()
        {
            // Reset all level completion flags
            for (int i = 0; i < tutorialLevelNames.Length; i++)
            {
                string key = $"TutorialLevel_{i}_Completed";
                PlayerPrefs.DeleteKey(key);
            }
            
            // Reset overall tutorial completion flag
            PlayerPrefs.DeleteKey("TutorialCompleted");
            PlayerPrefs.Save();
            
            Debug.Log("Tutorial progress reset");
        }

        public void JumpToTutorialLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= tutorialLevelNames.Length)
            {
                Debug.LogError($"Invalid tutorial level index: {levelIndex}");
                return;
            }
            
            // Load the specified tutorial level
            string levelName = tutorialLevelNames[levelIndex];
            SceneManager.LoadScene("Tutorial/" + levelName);
        }

        public void TriggerObjectiveCompletion(string objectiveId)
        {
            HandleObjectiveCompleted(objectiveId);
        }

        public void TriggerCheckpointReached(int checkpointIndex)
        {
            HandleCheckpointReached(checkpointIndex);
        }

        public void TriggerCollectibleCollected(string collectibleType)
        {
            HandleCollectibleCollected(collectibleType);
        }

        /// <summary>
        /// Gestisce il completamento di un obiettivo del tutorial.
        /// Viene chiamato dalla classe TutorialObjective quando un obiettivo è stato completato.
        /// </summary>
        /// <param name="objective">L'obiettivo completato</param>
        public void OnObjectiveCompleted(TutorialObjective objective)
        {
            if (!tutorialActive || objective == null)
                return;

            Debug.Log($"Objective completed: {objective.ObjectiveName} ({objective.ObjectiveId})");

            // Controlla se questo obiettivo è rilevante per lo step corrente
            if (currentStep != null && 
                currentStep.stepType == TutorialStepType.Trigger && 
                currentStep.triggerAction == TutorialTriggerAction.CompleteObjective && 
                currentStep.objectiveId == objective.ObjectiveId)
            {
                // Questo obiettivo è quello che stavamo aspettando per completare lo step corrente
                CompleteCurrentStep();
            }

            // Puoi aggiungere qui altre logiche per obiettivi opzionali, ricompense, ecc.
            
            // Riproduci suono di completamento se l'obiettivo ha il suo suono personalizzato
            AudioClip objectiveSound = objective.GetComponent<AudioSource>()?.clip;
            if (audioSource != null && objectiveSound != null)
            {
                audioSource.PlayOneShot(objectiveSound);
            }
            
            // Aggiorna UI se necessario
            UpdateProgressUI();
        }

        /// <summary>
        /// Registra un obiettivo nel tutorial manager.
        /// Viene chiamato dagli obiettivi quando vengono inizializzati.
        /// </summary>
        /// <param name="objective">L'obiettivo da registrare</param>
        public void RegisterObjective(TutorialObjective objective)
        {
            if (objective == null)
                return;
                
            Debug.Log($"Registering objective: {objective.ObjectiveName} ({objective.ObjectiveId})");
            
            // Qui potresti mantenere una lista di obiettivi attivi se necessario
            // objectives.Add(objective);
            
            // Traccia anche l'oggetto GameObject per facilitare la pulizia
            tutorialObjects.Add(objective.gameObject);
        }

        /// <summary>
        /// Rimuove un obiettivo dal tutorial manager.
        /// Viene chiamato dagli obiettivi quando vengono distrutti.
        /// </summary>
        /// <param name="objective">L'obiettivo da rimuovere</param>
        public void UnregisterObjective(TutorialObjective objective)
        {
            if (objective == null)
                return;
                
            Debug.Log($"Unregistering objective: {objective.ObjectiveName} ({objective.ObjectiveId})");
            
            // Qui potresti rimuovere l'obiettivo da una lista se ne tieni traccia
            // objectives.Remove(objective);
            
            // Rimuovi anche il GameObject dall'elenco degli oggetti di tutorial
            if (tutorialObjects.Contains(objective.gameObject))
            {
                tutorialObjects.Remove(objective.gameObject);
            }
        }


        #endregion
    }

    /// <summary>
    /// Tipi di controlli rilevati
    /// </summary>
    public enum ControlType
    {
        Unknown,
        KeyboardMouse,
        Touch,
        Controller
    }
}