using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Rappresenta un checkpoint nel tutorial che può attivare step specifici, 
    /// salvare il progresso del giocatore e fornire feedback visivo/audio.
    /// </summary>
    public class TutorialCheckpoint : MonoBehaviour
    {
        [Header("Checkpoint Configuration")]
        [SerializeField] private int checkpointIndex = 0;
        [SerializeField] private string checkpointName = "Checkpoint";
        [SerializeField] private bool isMandatory = true;
        [SerializeField] private bool saveProgress = true;

        [Header("Tutorial Interaction")]
        [SerializeField] private TutorialCheckpointAction checkpointAction = TutorialCheckpointAction.TriggerEvent;
        [SerializeField] private int specificStepToActivate = -1;
        [SerializeField] private float activationDelay = 0.5f;

        [Header("Visual and Audio Feedback")]
        [SerializeField] private bool showCheckpointEffect = true;
        [SerializeField] private GameObject activationVFX;
        [SerializeField] private float vfxDuration = 2f;
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private string messageToDisplay = "Checkpoint Reached!";

        [Header("Events")]
        [SerializeField] private UnityEvent onCheckpointActivated;
        [SerializeField] private UnityEvent onCheckpointCompleted;

        // Private fields
        private bool hasBeenActivated = false;
        private bool hasBeenCompleted = false;
        private AudioSource audioSource;

        private void Awake()
        {
            // Get or add audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && activationSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }

            // Check if this checkpoint was previously activated
            if (saveProgress)
            {
                string key = $"Tutorial_Checkpoint_{checkpointIndex}";
                hasBeenActivated = PlayerPrefs.GetInt(key, 0) == 1;

                if (hasBeenActivated)
                {
                    // We could change appearance to indicate it was already activated
                    SetCheckpointAppearanceToCompleted();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !hasBeenCompleted)
            {
                ActivateCheckpoint();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !hasBeenCompleted)
            {
                ActivateCheckpoint();
            }
        }

        /// <summary>
        /// Attiva manualmente questo checkpoint
        /// </summary>
        public void ActivateCheckpoint()
        {
            if (hasBeenCompleted && !isMandatory)
                return;

            hasBeenActivated = true;
            
            // Save progress
            if (saveProgress)
            {
                string key = $"Tutorial_Checkpoint_{checkpointIndex}";
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }

            // Play activation effects
            if (showCheckpointEffect)
            {
                // Play sound
                if (audioSource != null && activationSound != null)
                {
                    audioSource.PlayOneShot(activationSound);
                }

                // Show VFX
                if (activationVFX != null)
                {
                    GameObject vfx = Instantiate(activationVFX, transform.position, Quaternion.identity);
                    Destroy(vfx, vfxDuration);
                }

                // Change appearance
                SetCheckpointAppearanceToActivated();
            }

            // Trigger events
            onCheckpointActivated?.Invoke();

            // Delayed tutorial action execution
            if (activationDelay > 0)
            {
                StartCoroutine(ExecuteCheckpointActionDelayed());
            }
            else
            {
                ExecuteCheckpointAction();
            }
        }

        private IEnumerator ExecuteCheckpointActionDelayed()
        {
            yield return new WaitForSeconds(activationDelay);
            ExecuteCheckpointAction();
        }

        private void ExecuteCheckpointAction()
        {
            TutorialManager tutorialManager = TutorialManager.Instance;
            if (tutorialManager == null)
            {
                Debug.LogWarning("TutorialCheckpoint could not find TutorialManager instance");
                return;
            }

            switch (checkpointAction)
            {
                case TutorialCheckpointAction.CompleteCurrentStep:
                    tutorialManager.ForceCompleteCurrentStep();
                    break;

                case TutorialCheckpointAction.TriggerEvent:
                    tutorialManager.TriggerCheckpointReached(checkpointIndex);
                    break;

                case TutorialCheckpointAction.ActivateSpecificStep:
                    // This functionality would depend on how TutorialManager is implemented
                    // For example: tutorialManager.StartTutorialStep(specificStepToActivate);
                    break;

                case TutorialCheckpointAction.DisplayMessage:
                    // This would require TutorialManager to have a method for displaying messages
                    // For example: tutorialManager.ShowTutorialMessage(messageToDisplay);
                    break;
            }

            // Mark as completed
            CompleteCheckpoint();
        }

        /// <summary>
        /// Segna questo checkpoint come completato e attiva gli eventi associati
        /// </summary>
        public void CompleteCheckpoint()
        {
            if (hasBeenCompleted)
                return;

            hasBeenCompleted = true;
            
            // Set visual state to completed
            SetCheckpointAppearanceToCompleted();
            
            // Trigger completion events
            onCheckpointCompleted?.Invoke();
        }

        /// <summary>
        /// Resetta lo stato di questo checkpoint
        /// </summary>
        public void ResetCheckpoint()
        {
            hasBeenActivated = false;
            hasBeenCompleted = false;

            // Reset visual appearance
            SetCheckpointAppearanceToDefault();

            // Clear saved data
            if (saveProgress)
            {
                string key = $"Tutorial_Checkpoint_{checkpointIndex}";
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }

        // Visual state management - would be implemented differently based on the actual visuals
        private void SetCheckpointAppearanceToDefault()
        {
            // Example implementation:
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Set material to default state
                Material defaultMaterial = Resources.Load<Material>("Materials/CheckpointDefault");
                if (defaultMaterial != null)
                {
                    renderer.material = defaultMaterial;
                }
            }
        }

        private void SetCheckpointAppearanceToActivated()
        {
            // Example implementation:
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Set material to activated state
                Material activatedMaterial = Resources.Load<Material>("Materials/CheckpointActivated");
                if (activatedMaterial != null)
                {
                    renderer.material = activatedMaterial;
                }
            }
        }

        private void SetCheckpointAppearanceToCompleted()
        {
            // Example implementation:
            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Set material to completed state
                Material completedMaterial = Resources.Load<Material>("Materials/CheckpointCompleted");
                if (completedMaterial != null)
                {
                    renderer.material = completedMaterial;
                }
            }
        }

        // Utility properties
        public int CheckpointIndex => checkpointIndex;
        public bool HasBeenActivated => hasBeenActivated;
        public bool HasBeenCompleted => hasBeenCompleted;
    }

    /// <summary>
    /// Definisce le possibili azioni che un checkpoint può eseguire quando attivato
    /// </summary>
    public enum TutorialCheckpointAction
    {
        CompleteCurrentStep,
        TriggerEvent,
        ActivateSpecificStep,
        DisplayMessage
    }
}