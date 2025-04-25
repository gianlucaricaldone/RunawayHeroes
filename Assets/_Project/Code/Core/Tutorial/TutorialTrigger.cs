using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Componente che rileva quando il giocatore entra in un'area predefinita e attiva un passo specifico del tutorial.
    /// Può essere usato per trigger basati su collisione, oppure per attivare manualmente step del tutorial basati su eventi di gameplay.
    /// </summary>
    public class TutorialTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private TriggerType triggerType = TriggerType.Collision;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private float triggerDelay = 0f;
        [SerializeField] private bool destroyAfterTrigger = false;

        [Header("Tutorial Action")]
        [SerializeField] private TutorialAction action = TutorialAction.CompleteCurrentStep;
        [SerializeField] private int specificStepIndex = -1;
        [SerializeField] private string objectiveId = "";
        [SerializeField] private int checkpointIndex = -1;
        [SerializeField] private string collectibleType = "";

        [Header("Feedback")]
        [SerializeField] private AudioClip triggerSound;
        [SerializeField] private GameObject triggerVFX;
        [SerializeField] private bool deactivateTriggerVisuals = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onTriggerActivated;

        // Private fields
        private bool hasBeenTriggered = false;
        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && triggerSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType != TriggerType.Collision)
                return;

            if (other.CompareTag(playerTag))
            {
                ActivateTrigger();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerType != TriggerType.Collision)
                return;

            if (other.CompareTag(playerTag))
            {
                ActivateTrigger();
            }
        }

        /// <summary>
        /// Attiva manualmente questo trigger. Utile per attivazioni basate su eventi invece che su collisioni.
        /// </summary>
        public void ActivateTrigger()
        {
            if (triggerOnce && hasBeenTriggered)
                return;

            if (triggerDelay > 0f)
            {
                StartCoroutine(DelayedTrigger());
            }
            else
            {
                ExecuteTriggerAction();
            }
        }

        private IEnumerator DelayedTrigger()
        {
            yield return new WaitForSeconds(triggerDelay);
            ExecuteTriggerAction();
        }

        private void ExecuteTriggerAction()
        {
            hasBeenTriggered = true;

            // Play feedback
            if (audioSource != null && triggerSound != null)
            {
                audioSource.PlayOneShot(triggerSound);
            }

            if (triggerVFX != null)
            {
                Instantiate(triggerVFX, transform.position, Quaternion.identity);
            }

            // Deactivate visuals if needed
            if (deactivateTriggerVisuals)
            {
                // Deactivate renderers but keep colliders active
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }

            // Execute the tutorial action
            TutorialManager tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null)
            {
                switch (action)
                {
                    case TutorialAction.CompleteCurrentStep:
                        tutorialManager.ForceCompleteCurrentStep();
                        break;
                    case TutorialAction.AdvanceToSpecificStep:
                        // Implementation depends on TutorialManager's public methods
                        // This would need to be expanded based on what's available
                        break;
                    case TutorialAction.TriggerObjectiveCompletion:
                        tutorialManager.TriggerObjectiveCompletion(objectiveId);
                        break;
                    case TutorialAction.TriggerCheckpointReached:
                        tutorialManager.TriggerCheckpointReached(checkpointIndex);
                        break;
                    case TutorialAction.TriggerCollectibleCollected:
                        tutorialManager.TriggerCollectibleCollected(collectibleType);
                        break;
                }
            }
            else
            {
                Debug.LogWarning("TutorialTrigger could not find TutorialManager instance");
            }

            // Invoke UnityEvent
            onTriggerActivated?.Invoke();

            // Self-destruct if needed
            if (destroyAfterTrigger)
            {
                Destroy(gameObject, 0.1f);
            }
        }

        // Reset the trigger (useful for testing or in-game resets)
        public void ResetTrigger()
        {
            hasBeenTriggered = false;

            // Reactivate visuals if they were deactivated
            if (deactivateTriggerVisuals)
            {
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.enabled = true;
                }
            }
        }
    }

    /// <summary>
    /// Tipo di attivazione per il trigger del tutorial
    /// </summary>
    public enum TriggerType
    {
        Collision,  // Attivato quando il giocatore entra nel collider
        Manual      // Attivato manualmente tramite codice
    }

    /// <summary>
    /// Azione da eseguire quando il trigger viene attivato
    /// </summary>
    public enum TutorialAction
    {
        CompleteCurrentStep,
        AdvanceToSpecificStep,
        TriggerObjectiveCompletion,
        TriggerCheckpointReached,
        TriggerCollectibleCollected
    }
}