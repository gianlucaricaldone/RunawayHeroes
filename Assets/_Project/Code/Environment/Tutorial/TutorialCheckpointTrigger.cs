using UnityEngine;
using RunawayHeroes.Characters;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Trigger di checkpoint per il tutorial, attiva eventi specifici quando il giocatore passa
    /// </summary>
    public class TutorialCheckpointTrigger : MonoBehaviour
    {
        [Header("Checkpoint Configuration")]
        [SerializeField] private int checkpointID;
        [SerializeField] private bool triggerOnce = true;
        [SerializeField] private bool hasBeenTriggered = false;
        
        [Header("Optional Trigger Actions")]
        [SerializeField] private bool showMessage = false;
        [SerializeField] private string checkpointMessage = "";
        [SerializeField] private float messageDuration = 2f;
        
        [SerializeField] private bool activateGameObjects = false;
        [SerializeField] private GameObject[] objectsToActivate;
        
        [SerializeField] private bool deactivateGameObjects = false;
        [SerializeField] private GameObject[] objectsToDeactivate;

        private Level1TutorialController tutorialController;
        private TutorialUI tutorialUI;

        private void Awake()
        {
            // Trova i riferimenti necessari nella scena
            tutorialController = FindObjectOfType<Level1TutorialController>();
            tutorialUI = FindObjectOfType<TutorialUI>();
            
            // Assicurati che ci sia un collider trigger
            Collider collider = GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
            }
            else if (collider == null)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(3f, 2f, 0.5f); // Dimensione predefinita
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnce && hasBeenTriggered) return;
            
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                hasBeenTriggered = true;
                
                // Notifica il controller tutorial
                if (tutorialController != null)
                {
                    tutorialController.OnTriggerCheckpoint(checkpointID);
                }
                
                // Esegui le azioni opzionali
                if (showMessage && tutorialUI != null && !string.IsNullOrEmpty(checkpointMessage))
                {
                    tutorialUI.ShowMessage(checkpointMessage, messageDuration);
                }
                
                if (activateGameObjects && objectsToActivate != null)
                {
                    foreach (GameObject obj in objectsToActivate)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(true);
                        }
                    }
                }
                
                if (deactivateGameObjects && objectsToDeactivate != null)
                {
                    foreach (GameObject obj in objectsToDeactivate)
                    {
                        if (obj != null)
                        {
                            obj.SetActive(false);
                        }
                    }
                }
            }
        }

        // Per facilitare la visualizzazione nell'editor
        private void OnDrawGizmos()
        {
            // Colore del gizmo basato sull'ID del checkpoint
            float hue = (checkpointID * 0.1f) % 1f;
            Gizmos.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
            
            // Disegna un piano verticale per indicare il checkpoint
            Vector3 size = new Vector3(3f, 2f, 0.1f);
            Gizmos.DrawCube(transform.position, size);
            
            // Disegna il numero del checkpoint
            Gizmos.color = Color.white;
            Vector3 textPosition = transform.position;
            textPosition.y += 1.5f;
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(textPosition, $"Checkpoint {checkpointID}");
#endif
        }
    }
}