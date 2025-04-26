using UnityEngine;
using System.Collections;
using RunawayHeroes.Characters;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Gestisce gli ostacoli del tutorial (barriere e gap)
    /// Rileva quando il giocatore supera un ostacolo e lo comunica al controller
    /// </summary>
    public class TutorialObstacle : MonoBehaviour
    {
        public enum ObstacleType
        {
            LowBarrier,
            Gap
        }

        [Header("Obstacle Configuration")]
        [SerializeField] private ObstacleType obstacleType;
        [SerializeField] private bool isActive = true;
        [SerializeField] private bool hasBeenPassed = false;

        [Header("Visuals")]
        [SerializeField] private GameObject obstacleModel;
        [SerializeField] private Renderer obstacleRenderer;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color highlightColor = Color.yellow;

        private Level1TutorialController tutorialController;
        private BoxCollider triggerCollider;
        private Coroutine highlightCoroutine;

        private void Awake()
        {
            // Trova il tutorial controller nella scena
            tutorialController = FindObjectOfType<Level1TutorialController>();
            
            // Assicurati che l'ostacolo abbia un collider trigger
            triggerCollider = GetComponent<BoxCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                
                // Imposta le dimensioni in base al tipo di ostacolo
                if (obstacleType == ObstacleType.LowBarrier)
                {
                    triggerCollider.size = new Vector3(1f, 0.5f, 0.5f);
                    triggerCollider.center = new Vector3(0, 0.25f, 0);
                }
                else if (obstacleType == ObstacleType.Gap)
                {
                    triggerCollider.size = new Vector3(1f, 0.5f, 1f);
                    triggerCollider.center = new Vector3(0, -0.25f, 0);
                }
            }
            
            // Ottieni il renderer se non è già assegnato
            if (obstacleRenderer == null && obstacleModel != null)
            {
                obstacleRenderer = obstacleModel.GetComponent<Renderer>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!isActive || hasBeenPassed) return;
            
            // Verifica se l'oggetto che ha colliso è il giocatore
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Controlla se il giocatore ha superato l'ostacolo correttamente
                bool obstacleCleared = false;
                
                if (obstacleType == ObstacleType.LowBarrier)
                {
                    // Per le barriere, il giocatore deve essere in salto
                    obstacleCleared = player.IsJumping();
                }
                else if (obstacleType == ObstacleType.Gap)
                {
                    // Per i gap, il giocatore deve essere in salto
                    obstacleCleared = player.IsJumping();
                }
                
                if (obstacleCleared)
                {
                    // Contrassegna l'ostacolo come superato
                    hasBeenPassed = true;
                    
                    // Notifica il controller del tutorial
                    if (tutorialController != null)
                    {
                        if (obstacleType == ObstacleType.LowBarrier)
                        {
                            tutorialController.OnObstaclePassed();
                        }
                        else if (obstacleType == ObstacleType.Gap)
                        {
                            tutorialController.OnGapPassed();
                        }
                    }
                    
                    // Effetto visivo di superamento ostacolo
                    StartCoroutine(ShowSuccessEffect());
                }
                else
                {
                    // Il giocatore ha fallito nel superare l'ostacolo
                    StartCoroutine(ShowFailureEffect());
                }
            }
        }

        // Metodo chiamato dal tutorial controller per evidenziare questo ostacolo
        public void Highlight(bool highlight)
        {
            if (highlightCoroutine != null)
            {
                StopCoroutine(highlightCoroutine);
            }
            
            if (highlight)
            {
                highlightCoroutine = StartCoroutine(PulseHighlight());
            }
            else
            {
                if (obstacleRenderer != null)
                {
                    obstacleRenderer.material.color = defaultColor;
                }
            }
        }

        private IEnumerator PulseHighlight()
        {
            if (obstacleRenderer == null) yield break;
            
            float duration = 1.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 4);
                
                obstacleRenderer.material.color = Color.Lerp(defaultColor, highlightColor, pulse);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            obstacleRenderer.material.color = defaultColor;
        }

        private IEnumerator ShowSuccessEffect()
        {
            // Implementa qui un effetto di successo
            // Ad esempio, un flash verde o un effetto particellare
            if (obstacleRenderer != null)
            {
                Color originalColor = obstacleRenderer.material.color;
                obstacleRenderer.material.color = Color.green;
                
                yield return new WaitForSeconds(0.3f);
                
                obstacleRenderer.material.color = originalColor;
            }
            else
            {
                yield return null;
            }
        }

        private IEnumerator ShowFailureEffect()
        {
            // Implementa qui un effetto di fallimento
            // Ad esempio, un flash rosso
            if (obstacleRenderer != null)
            {
                Color originalColor = obstacleRenderer.material.color;
                obstacleRenderer.material.color = Color.red;
                
                yield return new WaitForSeconds(0.3f);
                
                obstacleRenderer.material.color = originalColor;
            }
            else
            {
                yield return null;
            }
        }

        // Per il posizionamento nel level design
        private void OnDrawGizmos()
        {
            Gizmos.color = (obstacleType == ObstacleType.LowBarrier) ? Color.blue : Color.red;
            
            Vector3 size;
            Vector3 center = transform.position;
            
            if (obstacleType == ObstacleType.LowBarrier)
            {
                size = new Vector3(1f, 0.5f, 0.5f);
                center.y += 0.25f;
            }
            else // Gap
            {
                size = new Vector3(1f, 0.5f, 1f);
                center.y -= 0.25f;
            }
            
            Gizmos.DrawWireCube(center, size);
        }
    }
}