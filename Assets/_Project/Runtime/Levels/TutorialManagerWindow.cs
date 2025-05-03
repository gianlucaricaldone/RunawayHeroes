using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;
using Unity.Entities;
using System.Collections;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Gestisce l'interfaccia utente per la selezione e avanzamento dei tutorial
    /// </summary>
    public class TutorialManagerWindow : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject tutorialSelectionPanel;
        [SerializeField] private GameObject tutorialButtonPrefab;
        [SerializeField] private Transform tutorialButtonsContainer;
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Tutorial Initializer")]
        [SerializeField] private TutorialLevelInitializer tutorialInitializer;
        
        private int _highestUnlockedTutorial = 0;
        
        private void Start()
        {
            // Carica il progresso del tutorial
            LoadTutorialProgress();
            
            // Inizializza l'UI
            InitializeUI();
            
            // Collega gli event handler
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }
        
        /// <summary>
        /// Carica il progresso del tutorial
        /// </summary>
        private void LoadTutorialProgress()
        {
            // In un gioco reale, qui useresti un sistema di salvataggio appropriato
            _highestUnlockedTutorial = PlayerPrefs.GetInt("HighestUnlockedTutorial", 0);
            
            Debug.Log($"Loaded tutorial progress: highest unlocked = {_highestUnlockedTutorial}");
        }
        
        /// <summary>
        /// Inizializza l'UI dei tutorial
        /// </summary>
        private void InitializeUI()
        {
            // Aggiorna il titolo
            if (titleText != null)
            {
                titleText.text = "Tutorial";
            }
            
            // Verifica che abbiamo il container e il prefab
            if (tutorialButtonsContainer == null || tutorialButtonPrefab == null || tutorialInitializer == null)
            {
                Debug.LogError("Missing references for tutorial UI setup");
                return;
            }
            
            // Pulisci i bottoni esistenti
            foreach (Transform child in tutorialButtonsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Aggiungi un bottone per ogni tutorial
            for (int i = 0; i < tutorialInitializer.tutorialSequence.Length; i++)
            {
                TutorialLevelData tutorial = tutorialInitializer.tutorialSequence[i];
                
                // Crea il bottone
                GameObject buttonObj = Instantiate(tutorialButtonPrefab, tutorialButtonsContainer);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                // Configura il testo
                if (buttonText != null)
                {
                    buttonText.text = tutorial.description;
                }
                
                // Interattività basata sul progresso
                bool isUnlocked = i <= _highestUnlockedTutorial;
                button.interactable = isUnlocked;
                
                // Applica stile visivo in base allo stato di sblocco
                if (!isUnlocked)
                {
                    // Aggiungi icona di lucchetto o opacità ridotta
                    var colors = button.colors;
                    colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                    button.colors = colors;
                    
                    if (buttonText != null)
                    {
                        buttonText.text = $"{tutorial.description} (Bloccato)";
                    }
                }
                
                // Indice del tutorial per la lambda
                int tutorialIndex = i;
                
                // Collega il callback
                button.onClick.AddListener(() => StartTutorial(tutorialIndex));
            }
        }
        
        /// <summary>
        /// Avvia un tutorial specifico
        /// </summary>
        public void StartTutorial(int tutorialIndex)
        {
            // Verifica che l'indice sia valido
            if (tutorialIndex < 0 || tutorialIndex >= tutorialInitializer.tutorialSequence.Length)
            {
                Debug.LogError($"Invalid tutorial index: {tutorialIndex}");
                return;
            }
            
            // Imposta l'indice corrente
            tutorialInitializer.tutorialLevelIndex = tutorialIndex;
            
            // Nascondi il pannello di selezione
            tutorialSelectionPanel.SetActive(false);
            
            // Avvia una coroutine per caricare il tutorial dopo un breve ritardo
            StartCoroutine(StartTutorialWithDelay(tutorialIndex));
        }
        
        /// <summary>
        /// Coroutine per avviare il tutorial dopo un breve ritardo
        /// </summary>
        private IEnumerator StartTutorialWithDelay(int tutorialIndex)
        {
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log($"Starting tutorial {tutorialIndex}: {tutorialInitializer.tutorialSequence[tutorialIndex].description}");
            
            // In un gioco reale, qui faresti una transizione appropriata
            
            // Avvia il tutorial
            if (tutorialInitializer != null)
            {
                // Chiama il metodo Start() per generare il tutorial
                tutorialInitializer.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
            }
        }
        
        /// <summary>
        /// Gestisce il click sul pulsante back
        /// </summary>
        private void OnBackButtonClicked()
        {
            // In un gioco reale, qui torneresti al menu principale
            Debug.Log("Back button clicked");
            
            // Esempio di navigazione al menu principale
            // SceneManager.LoadScene("MainMenu");
        }
        
        /// <summary>
        /// Sblocca il prossimo tutorial nella sequenza
        /// </summary>
        public void UnlockNextTutorial()
        {
            if (_highestUnlockedTutorial < tutorialInitializer.tutorialSequence.Length - 1)
            {
                _highestUnlockedTutorial++;
                PlayerPrefs.SetInt("HighestUnlockedTutorial", _highestUnlockedTutorial);
                PlayerPrefs.Save();
                
                // Aggiorna l'UI
                InitializeUI();
                
                Debug.Log($"Unlocked tutorial {_highestUnlockedTutorial}");
            }
        }
    }
}