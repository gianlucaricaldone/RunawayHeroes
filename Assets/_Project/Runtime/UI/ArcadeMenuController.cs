// Path: Assets/_Project/Runtime/UI/ArcadeMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.Runtime.UI
{
    /// <summary>
    /// Controller per il menu della modalità Arcade, che permette
    /// di rigiocare livelli già completati.
    /// </summary>
    public class ArcadeMenuController : MonoBehaviour
    {
        [System.Serializable]
        public class WorldCategory
        {
            public string worldName;
            public Sprite worldIcon;
            public GameObject levelSelectionPanel;
            public List<LevelButton> levels = new List<LevelButton>();
        }
        
        [System.Serializable]
        public class LevelButton
        {
            public string levelName;
            public string scenePath;
            public Button button;
            public Image completionStar;
            public TextMeshProUGUI highScoreText;
        }
        
        [Header("World Selection")]
        [SerializeField] private List<WorldCategory> worlds = new List<WorldCategory>();
        [SerializeField] private Transform worldButtonContainer;
        [SerializeField] private GameObject worldButtonPrefab;
        
        [Header("UI Controls")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI titleText;
        
        private GameObject currentPanel;
        
        private void Start()
        {
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // Configura i bottoni per i mondi
            InitializeWorldButtons();
            
            // Configura il pulsante back
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
            
            // Inizialmente mostra selezione mondi
            ShowWorldSelection();
        }
        
        private void InitializeWorldButtons()
        {
            if (worldButtonContainer == null || worldButtonPrefab == null) return;
            
            // Pulisci container esistente
            foreach (Transform child in worldButtonContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Crea bottoni per ogni mondo
            for (int i = 0; i < worlds.Count; i++)
            {
                WorldCategory world = worlds[i];
                
                // Crea bottone
                GameObject buttonObj = Instantiate(worldButtonPrefab, worldButtonContainer);
                Button button = buttonObj.GetComponent<Button>();
                
                // Configura immagine e testo
                Image buttonImage = buttonObj.GetComponentInChildren<Image>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonImage != null && world.worldIcon != null)
                {
                    buttonImage.sprite = world.worldIcon;
                }
                
                if (buttonText != null)
                {
                    buttonText.text = world.worldName;
                }
                
                // Configura onclick
                int worldIndex = i;
                button.onClick.AddListener(() => OnWorldSelected(worldIndex));
                
                // Configura disponibilità (basata sul progresso del giocatore)
                bool isWorldUnlocked = IsWorldUnlocked(i);
                button.interactable = isWorldUnlocked;
                
                // Feedback visivo se bloccato
                if (!isWorldUnlocked)
                {
                    // Aggiunge icona lucchetto o opacità ridotta
                    buttonImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
            }
            
            // Inizializza i livelli di ciascun mondo
            InitializeLevelButtons();
        }
        
        private void InitializeLevelButtons()
        {
            foreach (WorldCategory world in worlds)
            {
                foreach (LevelButton levelButton in world.levels)
                {
                    if (levelButton.button != null)
                    {
                        // Configura listener per il bottone
                        string scenePath = levelButton.scenePath;
                        levelButton.button.onClick.AddListener(() => OnLevelSelected(scenePath));
                        
                        // Configura stato completamento
                        bool isCompleted = IsLevelCompleted(levelButton.scenePath);
                        if (levelButton.completionStar != null)
                        {
                            levelButton.completionStar.gameObject.SetActive(isCompleted);
                        }
                        
                        // Configura highscore
                        int highScore = GetLevelHighScore(levelButton.scenePath);
                        if (levelButton.highScoreText != null && highScore > 0)
                        {
                            levelButton.highScoreText.text = highScore.ToString();
                        }
                    }
                }
            }
        }
        
        private void ShowWorldSelection()
        {
            // Nascondi tutti i pannelli dei livelli
            foreach (WorldCategory world in worlds)
            {
                if (world.levelSelectionPanel != null)
                {
                    world.levelSelectionPanel.SetActive(false);
                }
            }
            
            // Aggiorna titolo
            if (titleText != null)
            {
                titleText.text = "SELEZIONA MONDO";
            }
            
            // Disattiva il pannello corrente
            currentPanel = null;
        }
        
        private void OnWorldSelected(int worldIndex)
        {
            if (worldIndex < 0 || worldIndex >= worlds.Count) return;
            
            // Nascondi tutti i pannelli
            foreach (WorldCategory world in worlds)
            {
                if (world.levelSelectionPanel != null)
                {
                    world.levelSelectionPanel.SetActive(false);
                }
            }
            
            // Mostra pannello del mondo selezionato
            WorldCategory selectedWorld = worlds[worldIndex];
            if (selectedWorld.levelSelectionPanel != null)
            {
                selectedWorld.levelSelectionPanel.SetActive(true);
                currentPanel = selectedWorld.levelSelectionPanel;
            }
            
            // Aggiorna titolo
            if (titleText != null)
            {
                titleText.text = selectedWorld.worldName.ToUpper();
            }
        }
        
        private void OnLevelSelected(string scenePath)
        {
            // Carica il livello selezionato
            UIManager.Instance.LoadSceneWithTransition(scenePath);
        }
        
        private void OnBackButtonClicked()
        {
            if (currentPanel != null)
            {
                // Torna alla selezione mondi
                ShowWorldSelection();
            }
            else
            {
                // Torna al menu principale
                UIManager.Instance.BackToPreviousPanel();
            }
        }
        
        #region Progress Tracking Helpers
        
        private bool IsWorldUnlocked(int worldIndex)
        {
            // Implementazione basata sui progressi di gioco
            // Per ora, consideriamo sbloccati i primi 3 mondi come specificato nel GDD
            return worldIndex < 3 || PlayerPrefs.GetInt("WorldUnlocked_" + worldIndex, 0) == 1;
        }
        
        private bool IsLevelCompleted(string levelPath)
        {
            // Implementazione basata sui progressi di gioco
            return PlayerPrefs.GetInt("LevelCompleted_" + levelPath, 0) == 1;
        }
        
        private int GetLevelHighScore(string levelPath)
        {
            // Implementazione basata sui progressi di gioco
            return PlayerPrefs.GetInt("HighScore_" + levelPath, 0);
        }
        
        #endregion
    }
}