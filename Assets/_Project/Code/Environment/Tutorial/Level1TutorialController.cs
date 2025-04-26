using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RunawayHeroes.Characters;
using TMPro;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Controller specifico per il primo livello tutorial "Primi Passi"
    /// Gestisce le istruzioni e la progressione specifiche di questo livello
    /// </summary>
    public class Level1TutorialController : MonoBehaviour
    {
        [Header("Tutorial References")]
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private TutorialUI tutorialUI;
        [SerializeField] private PlayerController playerController;

        [Header("Level Specific")]
        [SerializeField] private Transform startPosition;
        [SerializeField] private Transform endPosition;
        [SerializeField] private List<GameObject> barriers;
        [SerializeField] private List<GameObject> gaps;
        
        [Header("Tutorial Messages")]
        [SerializeField] private string welcomeMessage = "Benvenuto al Centro di Addestramento, Alex! Imparerai i movimenti di base per sopravvivere in Città in Caos.";
        [SerializeField] private string jumpInstructionMessage = "Tap sullo schermo per saltare gli ostacoli";
        [SerializeField] private string jumpTimingMessage = "Il timing è importante! Salta nel momento giusto per superare gli ostacoli";
        [SerializeField] private string successMessage = "Ottimo lavoro! Hai completato con successo il tutorial sui salti di base.";

        private bool tutorialStarted = false;
        private bool tutorialCompleted = false;
        private int currentBarrierIndex = 0;
        private int currentGapIndex = 0;

        private void Start()
        {
            // Disabilita inizialmente il controllo del giocatore
            if (playerController != null)
            {
                playerController.canJump = false;
                playerController.canSlide = false;
                playerController.canMoveHorizontal = false;
                playerController.isAutoRunning = false;
            }

            // Posiziona il giocatore all'inizio del livello
            if (playerController != null && startPosition != null)
            {
                playerController.transform.position = startPosition.position;
            }

            // Inizializza TutorialManager con i dati di questo livello
            if (tutorialManager != null)
            {
                tutorialManager.Initialize(this);
                StartCoroutine(StartTutorialAfterDelay(2f));
            }
        }

        private IEnumerator StartTutorialAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartTutorial();
        }

        public void StartTutorial()
        {
            if (tutorialStarted) return;
            
            tutorialStarted = true;
            tutorialUI.ShowMessage(welcomeMessage, 3f);
            StartCoroutine(IntroSequence());
        }

        private IEnumerator IntroSequence()
        {
            yield return new WaitForSeconds(3.5f);
            
            // Mostra istruzioni per il salto
            tutorialUI.ShowMessage(jumpInstructionMessage, 3f);
            
            yield return new WaitForSeconds(1f);
            
            // Attiva il controllo del salto e la corsa automatica
            playerController.canJump = true;
            playerController.isAutoRunning = true;
            
            // Evidenzia il primo ostacolo
            if (barriers.Count > 0)
            {
                HighlightObstacle(barriers[0]);
            }
            
            yield return new WaitForSeconds(4f);
            
            // Ricorda al giocatore l'importanza del timing
            tutorialUI.ShowMessage(jumpTimingMessage, 3f);
        }

        public void OnObstaclePassed()
        {
            // Incrementa il contatore degli ostacoli
            currentBarrierIndex++;
            
            // Se abbiamo superato tutti gli ostacoli, conclude il tutorial
            if (currentBarrierIndex >= barriers.Count && currentGapIndex >= gaps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            // Evidenzia il prossimo ostacolo se disponibile
            if (currentBarrierIndex < barriers.Count)
            {
                HighlightObstacle(barriers[currentBarrierIndex]);
            }
        }

        public void OnGapPassed()
        {
            // Incrementa il contatore dei gap
            currentGapIndex++;
            
            // Se abbiamo superato tutti gli ostacoli, conclude il tutorial
            if (currentBarrierIndex >= barriers.Count && currentGapIndex >= gaps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            // Evidenzia il prossimo gap se disponibile
            if (currentGapIndex < gaps.Count)
            {
                HighlightObstacle(gaps[currentGapIndex]);
            }
        }

        private void HighlightObstacle(GameObject obstacle)
        {
            // Implementa qui l'evidenziazione dell'ostacolo
            // Ad esempio, puoi usare un outline shader o un effetto particellare
            
            // Esempio di codice per un effetto di highlight visivo
            Renderer renderer = obstacle.GetComponent<Renderer>();
            if (renderer != null)
            {
                StartCoroutine(PulseObstacle(renderer));
            }
        }

        private IEnumerator PulseObstacle(Renderer renderer)
        {
            Material originalMaterial = renderer.material;
            Material highlightMaterial = new Material(originalMaterial);
            highlightMaterial.SetColor("_EmissionColor", Color.yellow);
            highlightMaterial.EnableKeyword("_EMISSION");
            
            float duration = 1.0f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float intensity = Mathf.PingPong(elapsed * 2, 1.0f);
                highlightMaterial.SetColor("_EmissionColor", Color.yellow * intensity);
                renderer.material = highlightMaterial;
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            renderer.material = originalMaterial;
        }

        private void CompleteTutorial()
        {
            if (tutorialCompleted) return;
            
            tutorialCompleted = true;
            
            // Mostra il messaggio di successo
            tutorialUI.ShowMessage(successMessage, 3f);
            
            // Informa il TutorialManager che il livello è completato
            if (tutorialManager != null)
            {
                tutorialManager.CompleteTutorial(1);
            }
            
            // Prepara il passaggio al livello successivo
            StartCoroutine(PrepareNextLevel());
        }

        private IEnumerator PrepareNextLevel()
        {
            yield return new WaitForSeconds(4f);
            
            // Salva il progresso
            PlayerPrefs.SetInt("Level1_Completed", 1);
            PlayerPrefs.Save();
            
            // Carica il livello successivo (Level2_PerfectSlide)
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level2_PerfectSlide");
        }

        // Metodo chiamato dai checkpoint o trigger nel livello
        public void OnTriggerCheckpoint(int checkpointID)
        {
            // Puoi implementare logica specifica per checkpoint intermedi
            Debug.Log($"Checkpoint {checkpointID} raggiunto");
            
            // Esempio: mostra un messaggio incoraggiante
            if (checkpointID == 1)
            {
                tutorialUI.ShowMessage("Stai andando alla grande! Continua così!", 2f);
            }
        }

        // Per debug o test dalla UI
        public void SkipTutorial()
        {
            CompleteTutorial();
        }
    }
}