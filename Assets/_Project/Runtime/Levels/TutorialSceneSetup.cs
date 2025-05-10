using UnityEngine;
using Unity.Entities;
using UnityEngine.SceneManagement;
using RunawayHeroes.Runtime.Characters;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Componente che gestisce il setup iniziale della scena del tutorial
    /// </summary>
    public class TutorialSceneSetup : MonoBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Prefab del personaggio giocatore")]
        public GameObject playerPrefab;
        
        [Tooltip("Prefab della telecamera di gioco")]
        public GameObject cameraPrefab;
        
        [Header("Configurazione")]
        [Tooltip("Posizione iniziale del giocatore")]
        public Vector3 playerStartPosition = new Vector3(0, 0, 0);
        
        [Tooltip("Personaggio selezionato per default (0=Alex, 1=Maya, ecc.)")]
        public int defaultCharacterId = 0;
        
        [Header("Tutorial")]
        [Tooltip("Inizializzatore del tutorial")]
        public TutorialLevelInitializer tutorialInitializer;
        
        [Tooltip("Carica automaticamente il main menu al completamento del tutorial")]
        public bool loadMainMenuOnCompletion = true;
        
        private GameObject _player;
        private GameObject _camera;
        
        private void Awake()
        {
            // Verifica se l'inizializzatore del tutorial è stato assegnato
            if (tutorialInitializer == null)
            {
                tutorialInitializer = GetComponent<TutorialLevelInitializer>();
                
                // Se ancora non lo troviamo, lo cerchiamo nella scena
                if (tutorialInitializer == null)
                {
                    tutorialInitializer = FindFirstObjectByType<TutorialLevelInitializer>();
                }
                
                // Se ancora non lo troviamo, lo creiamo
                if (tutorialInitializer == null)
                {
                    Debug.LogWarning("TutorialLevelInitializer not found. Creating one.");
                    tutorialInitializer = gameObject.AddComponent<TutorialLevelInitializer>();
                }
            }
        }
        
        private void Start()
        {
            // Setup iniziale della scena
            SpawnPlayer();
            SetupCamera();
            
            // Configura l'inizializzatore del tutorial
            tutorialInitializer.playerStartPosition = playerStartPosition;
            
            // Registra la callback per il completamento del tutorial
            if (loadMainMenuOnCompletion)
            {
                RegisterTutorialCompletionCallback();
            }
        }
        
        /// <summary>
        /// Spawna il giocatore nella scena
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab != null)
            {
                // Spawna il player
                _player = Instantiate(playerPrefab, playerStartPosition, Quaternion.identity);
                _player.tag = "Player"; // Assicurati che il tag sia corretto
                
                // Configura il personaggio (in un gioco reale, questo sarebbe basato sul personaggio selezionato)
                SetupCharacter(defaultCharacterId);
                
                Debug.Log($"Player spawned at {playerStartPosition}");
            }
            else
            {
                Debug.LogError("Player prefab not assigned to TutorialSceneSetup!");
            }
        }
        
        /// <summary>
        /// Configura la telecamera per seguire il giocatore
        /// </summary>
        private void SetupCamera()
        {
            if (cameraPrefab != null)
            {
                // Spawna la camera
                _camera = Instantiate(cameraPrefab);
                
                // Configura la camera per seguire il player (in un gioco reale, questo sarebbe gestito da un sistema dedicato)
                var cameraFollow = _camera.GetComponent<CameraFollow>();
                if (cameraFollow != null && _player != null)
                {
                    cameraFollow.target = _player.transform;
                }
                
                Debug.Log("Camera setup completed");
            }
            else
            {
                Debug.LogError("Camera prefab not assigned to TutorialSceneSetup!");
            }
        }
        
        /// <summary>
        /// Configura il personaggio selezionato
        /// </summary>
        private void SetupCharacter(int characterId)
        {
            // In un gioco reale, questo sarebbe una configurazione più complessa
            // basata sul personaggio selezionato dal giocatore
            
            Debug.Log($"Character {characterId} configured");
            
            // Esempio di configurazione del personaggio
            var playerController = _player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // playerController.SetCharacterId(characterId);
                Debug.Log($"Character {characterId} configured");
            }
        }
        
        /// <summary>
        /// Registra la callback per il completamento del tutorial
        /// </summary>
        private void RegisterTutorialCompletionCallback()
        {
            // In un gioco reale, questo usarebbe eventi o un sistema di callback appropriato
            // Per questo esempio, controlliamo semplicemente lo stato del tutorial in Update
            Debug.Log("Tutorial completion callback registered");
        }
        
        private void Update()
        {
            // Esempio di controllo per il completamento del tutorial
            if (loadMainMenuOnCompletion)
            {
                // Controlla se il tutorial è completato (in un gioco reale, useremmo eventi)
                var progressTracker = FindFirstObjectByType<ProgressTracker>();
                if (progressTracker != null && progressTracker.IsTutorialCompleted())
                {
                    LoadMainMenu();
                }
                
                // Scorciatoia per testare (premendo Escape)
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    LoadMainMenu();
                }
            }
        }
        
        /// <summary>
        /// Carica il prossimo tutorial o torna al menu principale
        /// </summary>
        private void LoadMainMenu()
        {
            // Se c'è un tutorial inizializzato, controlliamo se è l'ultimo
            if (tutorialInitializer != null)
            {
                // Verifica se ci sono altri tutorial da completare
                if (tutorialInitializer.tutorialLevelIndex < tutorialInitializer.tutorialSequence.Length - 1)
                {
                    // Passa al prossimo tutorial
                    LoadNextTutorial();
                    return;
                }
            }
            
            // Se è l'ultimo tutorial o non c'è un tutorial inizializzato, torna al menu principale
            Debug.Log("Tutorial sequence completed. Loading main menu...");
            SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// Carica il prossimo tutorial nella sequenza
        /// </summary>
        private void LoadNextTutorial()
        {
            int nextTutorialIndex = tutorialInitializer.tutorialLevelIndex + 1;
            
            // Verifica se l'indice è valido
            if (nextTutorialIndex < 0 || nextTutorialIndex >= tutorialInitializer.tutorialSequence.Length)
            {
                Debug.LogError($"Invalid next tutorial index: {nextTutorialIndex}");
                LoadMainMenu();
                return;
            }
            
            Debug.Log($"Loading next tutorial ({nextTutorialIndex})...");
            
            // Salva i dati del tutorial completato (in un gioco reale si userebbe un sistema di salvataggio)
            PlayerPrefs.SetInt("LastCompletedTutorial", tutorialInitializer.tutorialLevelIndex);
            PlayerPrefs.Save();
            
            // Ricarica la scena con il nuovo indice
            // In un'implementazione reale potresti voler usare una transizione più fluida
            // o mantenere la scena e cambiare solo la configurazione
            
            // Ricarichiamo la scena tutorial (in un approccio semplice)
            // Un'alternativa sarebbe usare SceneManager.LoadScene e passare i parametri 
            // tramite un sistema globale come PlayerPrefs o un singleton
            var activeScene = SceneManager.GetActiveScene();
            tutorialInitializer.tutorialLevelIndex = nextTutorialIndex;
            SceneManager.LoadScene(activeScene.name);
        }
    }
    
    /// <summary>
    /// Classe segnaposto per la telecamera che segue il giocatore
    /// In un gioco reale, questa sarebbe una implementazione completa
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 3, -5);
        public float smoothSpeed = 0.125f;
        
        private void LateUpdate()
        {
            if (target == null)
                return;
                
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
            
            transform.LookAt(target);
        }
    }
    
    /// <summary>
    /// Classe segnaposto per il tracciamento del progresso del tutorial
    /// In un gioco reale, questa sarebbe gestita da un sistema più complesso
    /// </summary>
    public class ProgressTracker : MonoBehaviour
    {
        private bool _tutorialCompleted = false;
        private float _progressPercentage = 0f;
        
        public void SetProgress(float percentage)
        {
            _progressPercentage = Mathf.Clamp01(percentage);
            
            if (_progressPercentage >= 1.0f)
            {
                _tutorialCompleted = true;
            }
        }
        
        public bool IsTutorialCompleted()
        {
            return _tutorialCompleted;
        }
    }
}