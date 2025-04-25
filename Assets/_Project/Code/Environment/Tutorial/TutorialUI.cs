using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Gestisce l'interfaccia utente del tutorial, inclusi i pannelli d'istruzione,
    /// frecce indicatrici e feedback visivo per il giocatore.
    /// </summary>
    public class TutorialUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private GameObject hintArrow;

        [Header("Instruction Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText;
        [SerializeField] private Image instructionIcon;
        [SerializeField] private Transform controlIconsContainer;
        [SerializeField] private GameObject keyboardControlsGroup;
        [SerializeField] private GameObject touchControlsGroup;

        [Header("Completion Elements")]
        [SerializeField] private TextMeshProUGUI completionText;
        [SerializeField] private TextMeshProUGUI nextStepText;
        [SerializeField] private Button continueButton;

        [Header("Progress Indicators")]
        [SerializeField] private Image progressFill;
        [SerializeField] private TextMeshProUGUI stepCounter;

        [Header("Animation Settings")]
        [SerializeField] private float panelFadeInDuration = 0.3f;
        [SerializeField] private float panelFadeOutDuration = 0.2f;
        [SerializeField] private float textTypewriterSpeed = 0.03f;
        [SerializeField] private float arrowBobAmplitude = 0.3f;
        [SerializeField] private float arrowBobFrequency = 2f;
        [SerializeField] private float arrowRotationSpeed = 80f;

        [Header("Audio")]
        [SerializeField] private AudioClip instructionAppearSound;
        [SerializeField] private AudioClip typingSound;
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private AudioClip buttonClickSound;

        // Private variables
        private AudioSource audioSource;
        private CanvasGroup instructionCanvasGroup;
        private CanvasGroup completionCanvasGroup;
        private Sequence typingSequence;
        private Coroutine arrowAnimationCoroutine;
        private Coroutine typingCoroutine;

        private void Awake()
        {
            // Get component references
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Get canvas groups or add them if missing
            if (instructionPanel != null)
            {
                instructionCanvasGroup = instructionPanel.GetComponent<CanvasGroup>();
                if (instructionCanvasGroup == null)
                {
                    instructionCanvasGroup = instructionPanel.AddComponent<CanvasGroup>();
                }
            }

            if (completionPanel != null)
            {
                completionCanvasGroup = completionPanel.GetComponent<CanvasGroup>();
                if (completionCanvasGroup == null)
                {
                    completionCanvasGroup = completionPanel.AddComponent<CanvasGroup>();
                }
            }

            // Add button listener
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
            }

            // Initialize UI state
            HideAllPanels();
        }

        private void OnDestroy()
        {
            // Clean up
            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(OnContinueButtonClicked);
            }

            if (typingSequence != null)
            {
                typingSequence.Kill();
            }

            if (arrowAnimationCoroutine != null)
            {
                StopCoroutine(arrowAnimationCoroutine);
            }

            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
        }

        /// <summary>
        /// Mostra le istruzioni per un passo specifico del tutorial
        /// </summary>
        /// <param name="step">Il passo del tutorial da mostrare</param>
        public void ShowInstruction(TutorialStep step)
        {
            if (step == null)
                return;

            // Play sound effect
            if (audioSource != null && instructionAppearSound != null)
            {
                audioSource.PlayOneShot(instructionAppearSound);
            }

            // Set up the UI elements
            if (titleText != null)
            {
                titleText.text = step.stepName;
            }

            if (instructionText != null)
            {
                // Stop any existing typing animation
                if (typingCoroutine != null)
                {
                    StopCoroutine(typingCoroutine);
                }

                // Start a new typing animation
                string textToShow = GetInstructionText(step);
                typingCoroutine = StartCoroutine(TypeText(instructionText, textToShow));
            }

            // Set up the icon if provided
            if (instructionIcon != null)
            {
                if (step.instructionIcon != null)
                {
                    instructionIcon.sprite = step.instructionIcon;
                    instructionIcon.gameObject.SetActive(true);
                }
                else
                {
                    instructionIcon.gameObject.SetActive(false);
                }
            }

            // Show the appropriate control hints
            UpdateControlTypeVisibility(TutorialManager.Instance?.DetectedControlType ?? ControlType.KeyboardMouse);

            // Animate the panel in
            ShowPanel(instructionPanel, instructionCanvasGroup);
        }

        private string GetInstructionText(TutorialStep step)
        {
            var controlType = TutorialManager.Instance?.DetectedControlType ?? ControlType.KeyboardMouse;

            if (controlType == ControlType.KeyboardMouse && !string.IsNullOrEmpty(step.keyboardInstructions))
            {
                return step.keyboardInstructions;
            }
            else if (controlType == ControlType.Touch && !string.IsNullOrEmpty(step.touchInstructions))
            {
                return step.touchInstructions;
            }

            return step.instructionText;
        }

        /// <summary>
        /// Mostra un messaggio di completamento per un passo del tutorial
        /// </summary>
        /// <param name="message">Messaggio da mostrare</param>
        /// <param name="nextStepInfo">Informazioni sul prossimo passo (opzionale)</param>
        public void ShowCompletionMessage(string message, string nextStepInfo = "")
        {
            // Play completion sound
            if (audioSource != null && completionSound != null)
            {
                audioSource.PlayOneShot(completionSound);
            }

            // Set up text
            if (completionText != null)
            {
                completionText.text = message;
            }

            if (nextStepText != null)
            {
                nextStepText.text = nextStepInfo;
                nextStepText.gameObject.SetActive(!string.IsNullOrEmpty(nextStepInfo));
            }

            // Animate the panel in
            ShowPanel(completionPanel, completionCanvasGroup);
        }

        /// <summary>
        /// Nasconde il messaggio di completamento
        /// </summary>
        public void HideCompletionMessage()
        {
            HidePanel(completionPanel, completionCanvasGroup);
        }

        /// <summary>
        /// Aggiorna l'indicatore di progresso del tutorial
        /// </summary>
        /// <param name="currentStep">Passo corrente</param>
        /// <param name="totalSteps">Numero totale di passi</param>
        public void UpdateProgress(int currentStep, int totalSteps)
        {
            if (progressFill != null && totalSteps > 0)
            {
                float progress = (float)(currentStep + 1) / totalSteps;
                progressFill.DOFillAmount(progress, 0.3f).SetEase(Ease.OutQuad);
            }

            if (stepCounter != null)
            {
                stepCounter.text = $"{currentStep + 1}/{totalSteps}";
            }
        }

        /// <summary>
        /// Aggiorna la posizione della freccia indicatrice
        /// </summary>
        /// <param name="targetPosition">Posizione target nel mondo</param>
        /// <param name="useWorldSpace">Se true, posiziona la freccia nello spazio mondo; altrimenti usa lo spazio schermo</param>
        public void UpdateHintArrow(Vector3 targetPosition, bool useWorldSpace = true)
        {
            if (hintArrow == null)
                return;

            // Stop any existing animation
            if (arrowAnimationCoroutine != null)
            {
                StopCoroutine(arrowAnimationCoroutine);
            }

            // Show and position the arrow
            hintArrow.SetActive(true);

            if (useWorldSpace)
            {
                // Calculate screen position from world position
                Vector3 screenPos = Camera.main.WorldToScreenPoint(targetPosition);
                hintArrow.transform.position = screenPos;
            }
            else
            {
                // Use direct screen position
                hintArrow.transform.position = targetPosition;
            }

            // Start arrow animation
            arrowAnimationCoroutine = StartCoroutine(AnimateArrow(targetPosition, useWorldSpace));
        }

        /// <summary>
        /// Nasconde la freccia indicatrice
        /// </summary>
        public void HideHintArrow()
        {
            if (hintArrow != null)
            {
                hintArrow.SetActive(false);
            }

            if (arrowAnimationCoroutine != null)
            {
                StopCoroutine(arrowAnimationCoroutine);
                arrowAnimationCoroutine = null;
            }
        }

        /// <summary>
        /// Evidenzia un elemento UI specifico come parte del tutorial
        /// </summary>
        /// <param name="targetElement">Elemento UI da evidenziare</param>
        public void HighlightUIElement(GameObject targetElement)
        {
            if (targetElement == null)
                return;

            // In un'implementazione reale, qui si applicherebbe un effetto di evidenziazione
            // Come un contorno luminoso, un'animazione pulsante, ecc.
            
            // Per questo esempio, cambiamo semplicemente la scala
            targetElement.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// Rimuove l'evidenziazione da un elemento UI
        /// </summary>
        /// <param name="targetElement">Elemento UI da cui rimuovere l'evidenziazione</param>
        public void RemoveUIHighlight(GameObject targetElement)
        {
            if (targetElement == null)
                return;

            // Ferma l'animazione di evidenziazione
            DOTween.Kill(targetElement.transform);
            
            // Ripristina la scala originale
            targetElement.transform.DOScale(1f, 0.3f);
        }

        /// <summary>
        /// Nasconde tutti i pannelli UI del tutorial
        /// </summary>
        public void HideAllPanels()
        {
            if (instructionPanel != null)
            {
                instructionPanel.SetActive(false);
            }

            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }

            if (hintArrow != null)
            {
                hintArrow.SetActive(false);
            }
        }

        /// <summary>
        /// Aggiorna la visibilità dei controlli in base al tipo di input rilevato
        /// </summary>
        /// <param name="controlType">Tipo di controllo rilevato</param>
        public void UpdateControlTypeVisibility(ControlType controlType)
        {
            if (keyboardControlsGroup != null)
            {
                keyboardControlsGroup.SetActive(controlType == ControlType.KeyboardMouse || controlType == ControlType.Unknown);
            }

            if (touchControlsGroup != null)
            {
                touchControlsGroup.SetActive(controlType == ControlType.Touch);
            }
        }

        // Private methods
        private void ShowPanel(GameObject panel, CanvasGroup canvasGroup)
        {
            if (panel == null)
                return;

            panel.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, panelFadeInDuration).SetEase(Ease.OutQuad);
            }
        }

        private void HidePanel(GameObject panel, CanvasGroup canvasGroup)
        {
            if (panel == null)
                return;

            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, panelFadeOutDuration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    panel.SetActive(false);
                });
            }
            else
            {
                panel.SetActive(false);
            }
        }

        private IEnumerator TypeText(TextMeshProUGUI textComponent, string text)
        {
            textComponent.text = "";
            
            for (int i = 0; i < text.Length; i++)
            {
                textComponent.text += text[i];
                
                // Play typing sound on specific intervals
                if (i % 3 == 0 && audioSource != null && typingSound != null)
                {
                    audioSource.PlayOneShot(typingSound, 0.5f);
                }
                
                yield return new WaitForSeconds(textTypewriterSpeed);
            }
        }

        private IEnumerator AnimateArrow(Vector3 basePosition, bool useWorldSpace)
        {
            while (hintArrow != null && hintArrow.activeInHierarchy)
            {
                float time = Time.time;
                
                // Position bobbing
                Vector3 bobOffset = Vector3.up * Mathf.Sin(time * arrowBobFrequency) * arrowBobAmplitude;
                
                if (useWorldSpace)
                {
                    // Update screen position in case camera or target moves
                    Vector3 currentScreenPos = Camera.main.WorldToScreenPoint(basePosition);
                    hintArrow.transform.position = currentScreenPos + bobOffset;
                }
                else
                {
                    hintArrow.transform.position = basePosition + bobOffset;
                }
                
                // Rotation
                hintArrow.transform.Rotate(Vector3.forward, arrowRotationSpeed * Time.deltaTime);
                
                yield return null;
            }
        }

        private void OnContinueButtonClicked()
        {
            // Play button sound
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }

            // Hide completion panel
            HideCompletionMessage();

            // Forward to tutorial manager to continue
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.ForceCompleteCurrentStep();
            }
        }
    }
}