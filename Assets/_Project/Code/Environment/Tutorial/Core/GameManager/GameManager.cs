using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RunawayHeroes.Core.Tutorial;
using RunawayHeroes.Core.SaveSystem;
using RunawayHeroes.Characters;

namespace RunawayHeroes.Manager
{
    /// <summary>
    /// GameManager è il sistema centrale che gestisce lo stato di gioco, le transizioni
    /// tra livelli, e coordina i sottosistemi come il tutorial, il sistema di salvataggio,
    /// e il sistema audio.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize
            InitializeManager();
        }
        #endregion

        [Header("Game Settings")]
        [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool isTutorialCompleted = false;
        
        [Header("Level Flow")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string firstLevelScene = "World1_City/Level1_CentralPark";
        [SerializeField] private float levelTransitionDelay = 2f;

        [Header("Systems References")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private SaveSystem saveSystem;

        // Events
        public event Action OnGamePaused;
        public event Action OnGameResumed;
        public event Action<string> OnLevelStarted;
        public event Action<string, LevelResult> OnLevelCompleted;
        public event Action<int> OnScoreChanged;
        public event Action<int> OnCurrencyChanged;
        public event Action OnGameOver;

        // Properties
        public GameDifficulty CurrentDifficulty => currentDifficulty;
        public bool IsPaused => isPaused;
        public bool IsGameActive { get; private set; }
        public bool IsTutorialMode { get; private set; }
        public float GameTime { get; private set; }
        public int TotalScore { get; private set; }
        public int TotalCurrency { get; private set; }
        public string CurrentLevel { get; private set; }
        public string CurrentWorld => GetWorldFromLevelName(CurrentLevel);

        // Private variables
        private TutorialManager tutorialManager;
        private PlayerController activePlayer;
        private GameState gameState = GameState.MainMenu;
        private float gamePlayTime = 0f;
        private int currentScore = 0;
        private int currentCurrency = 0;
        private LevelResult lastLevelResult;
        private bool isLevelLoading = false;
        private AsyncOperation sceneLoadOperation;

        #region Initialization
        private void InitializeManager()
        {
            Debug.Log("GameManager initializing...");

            // Find tutorial manager if exists
            tutorialManager = FindObjectOfType<TutorialManager>();
            if (tutorialManager == null)
            {
                tutorialManager = GameObject.FindObjectOfType<TutorialManager>();
            }

            // Check if tutorial is completed from saved data
            isTutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

            // Initialize systems
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<AudioManager>();
            }

            if (saveSystem == null)
            {
                saveSystem = FindObjectOfType<SaveSystem>();
            }

            // Subscribe to scene load events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Load player data if save system exists
            if (saveSystem != null)
            {
                LoadPlayerData();
            }

            Debug.Log("GameManager initialized.");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CurrentLevel = scene.name;
            isLevelLoading = false;

            // Find the active player in the scene
            activePlayer = FindObjectOfType<PlayerController>();

            // Check if this is a tutorial level
            IsTutorialMode = IsTutorialLevel(scene.name);

            // Set starting game state based on scene
            if (scene.name == mainMenuScene)
            {
                gameState = GameState.MainMenu;
                IsGameActive = false;
            }
            else if (IsTutorialMode)
            {
                gameState = GameState.Tutorial;
                IsGameActive = true;
            }
            else
            {
                gameState = GameState.Playing;
                IsGameActive = true;
                StartLevel();
            }

            // Trigger event
            OnLevelStarted?.Invoke(scene.name);

            Debug.Log($"Scene loaded: {scene.name}, Game State: {gameState}");
        }

        private void Start()
        {
            // Additional initialization if needed
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        #endregion

        #region Update Loop
        private void Update()
        {
            // Update game time when active
            if (IsGameActive && !isPaused)
            {
                gamePlayTime += Time.deltaTime;
                GameTime = gamePlayTime;
            }

            // Handle global input like pause
            if (IsGameActive && Input.GetKeyDown(KeyCode.Escape) && gameState != GameState.GameOver)
            {
                TogglePause();
            }

            // Add other update logic as needed
        }
        #endregion

        #region Game Flow Methods
        /// <summary>
        /// Inizia la partita dal primo livello o dal tutorial se non è stato completato
        /// </summary>
        public void StartGame()
        {
            if (!isTutorialCompleted && tutorialManager != null)
            {
                StartTutorial();
            }
            else
            {
                // Load the first level
                LoadLevel(firstLevelScene);
            }
        }

        /// <summary>
        /// Inizia il tutorial
        /// </summary>
        public void StartTutorial()
        {
            IsTutorialMode = true;
            LoadLevel("Tutorial/Level1_FirstSteps");
        }

        /// <summary>
        /// Chiamato quando il tutorial è completato
        /// </summary>
        public void OnTutorialCompleted()
        {
            isTutorialCompleted = true;
            PlayerPrefs.SetInt("TutorialCompleted", 1);
            PlayerPrefs.Save();

            // After tutorial is completed, we load the first real level
            LoadLevel(firstLevelScene);
        }

        /// <summary>
        /// Mette in pausa o riprende il gioco
        /// </summary>
        public void TogglePause()
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// Mette in pausa il gioco
        /// </summary>
        public void PauseGame()
        {
            if (gameState == GameState.GameOver || isLevelLoading)
                return;

            isPaused = true;
            Time.timeScale = 0f;

            // Trigger event
            OnGamePaused?.Invoke();

            Debug.Log("Game paused");
        }

        /// <summary>
        /// Riprende il gioco dalla pausa
        /// </summary>
        public void ResumeGame()
        {
            isPaused = false;
            Time.timeScale = 1f;

            // Trigger event
            OnGameResumed?.Invoke();

            Debug.Log("Game resumed");
        }

        /// <summary>
        /// Termina il livello corrente con un risultato
        /// </summary>
        public void CompleteLevel(LevelResult result)
        {
            if (gameState == GameState.GameOver || isLevelLoading)
                return;

            lastLevelResult = result;
            
            // Set your game state
            gameState = result.playerWon ? GameState.LevelCompleted : GameState.GameOver;

            // Save game progress if player won
            if (result.playerWon && saveSystem != null)
            {
                SavePlayerData();
            }

            // Trigger event
            OnLevelCompleted?.Invoke(CurrentLevel, result);

            Debug.Log($"Level completed: {CurrentLevel}, Success: {result.playerWon}, Score: {result.score}");

            // Proceed to next level with delay if player won
            if (result.playerWon)
            {
                StartCoroutine(LoadNextLevelAfterDelay(levelTransitionDelay));
            }
            else
            {
                // Game over state
                OnGameOver?.Invoke();
            }
        }

        /// <summary>
        /// Carica un livello specifico
        /// </summary>
        public void LoadLevel(string levelName)
        {
            if (isLevelLoading)
                return;

            isLevelLoading = true;

            // Reset game state for new level
            ResetLevelState();

            // Start loading the scene
            StartCoroutine(LoadLevelAsync(levelName));
        }

        private IEnumerator LoadLevelAsync(string levelName)
        {
            // Make sure time scale is normal
            Time.timeScale = 1f;

            // Show loading screen if needed
            // ShowLoadingScreen();

            // Load the scene asynchronously
            sceneLoadOperation = SceneManager.LoadSceneAsync(levelName);
            
            while (!sceneLoadOperation.isDone)
            {
                float progress = Mathf.Clamp01(sceneLoadOperation.progress / 0.9f);
                // Update loading progress UI if needed
                // updateLoadingProgress(progress);
                
                yield return null;
            }

            isLevelLoading = false;

            // Hide loading screen
            // HideLoadingScreen();
        }

        private IEnumerator LoadNextLevelAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // Determine next level and load it
            string nextLevel = GetNextLevelName(CurrentLevel);
            if (!string.IsNullOrEmpty(nextLevel))
            {
                LoadLevel(nextLevel);
            }
            else
            {
                // If no next level (world completed), load world selection or main menu
                LoadLevel(mainMenuScene);
            }
        }

        /// <summary>
        /// Riavvia il livello corrente
        /// </summary>
        public void RestartLevel()
        {
            LoadLevel(CurrentLevel);
        }

        /// <summary>
        /// Torna al menu principale
        /// </summary>
        public void ReturnToMainMenu()
        {
            LoadLevel(mainMenuScene);
        }

        /// <summary>
        /// Termina immediatamente la partita
        /// </summary>
        public void EndGame()
        {
            gameState = GameState.GameOver;
            OnGameOver?.Invoke();

            // After a delay, return to main menu
            StartCoroutine(ReturnToMenuAfterDelay(3f));
        }

        private IEnumerator ReturnToMenuAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToMainMenu();
        }

        /// <summary>
        /// Inizializza il livello corrente
        /// </summary>
        private void StartLevel()
        {
            // Reset level-specific variables
            gamePlayTime = 0f;
            currentScore = 0;
            currentCurrency = 0;

            // Reset time scale just in case
            Time.timeScale = 1f;
            isPaused = false;

            // Set active state
            IsGameActive = true;
            gameState = GameState.Playing;

            Debug.Log($"Starting level: {CurrentLevel}");
        }

        /// <summary>
        /// Resetta lo stato per un nuovo livello
        /// </summary>
        private void ResetLevelState()
        {
            gamePlayTime = 0f;
            isPaused = false;
            Time.timeScale = 1f;
        }
        #endregion

        #region Score and Currency Methods
        /// <summary>
        /// Aggiunge punti al punteggio corrente
        /// </summary>
        public void AddScore(int points)
        {
            currentScore += points;
            TotalScore += points;
            
            // Trigger event
            OnScoreChanged?.Invoke(currentScore);
        }

        /// <summary>
        /// Aggiunge valuta di gioco al totale corrente
        /// </summary>
        public void AddCurrency(int amount)
        {
            currentCurrency += amount;
            TotalCurrency += amount;
            
            // Trigger event
            OnCurrencyChanged?.Invoke(currentCurrency);

            // Save updated currency data
            if (saveSystem != null)
            {
                saveSystem.SaveCurrency(TotalCurrency);
            }
            else
            {
                // Fallback to PlayerPrefs
                PlayerPrefs.SetInt("TotalCurrency", TotalCurrency);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Spende valuta di gioco se disponibile
        /// </summary>
        /// <returns>True se l'acquisto è andato a buon fine</returns>
        public bool SpendCurrency(int amount)
        {
            if (TotalCurrency >= amount)
            {
                TotalCurrency -= amount;
                
                // Trigger event
                OnCurrencyChanged?.Invoke(TotalCurrency);

                // Save updated currency data
                if (saveSystem != null)
                {
                    saveSystem.SaveCurrency(TotalCurrency);
                }
                else
                {
                    // Fallback to PlayerPrefs
                    PlayerPrefs.SetInt("TotalCurrency", TotalCurrency);
                    PlayerPrefs.Save();
                }
                
                return true;
            }
            
            return false;
        }
        #endregion

        #region Save/Load Methods
        /// <summary>
        /// Salva i dati del giocatore
        /// </summary>
        private void SavePlayerData()
        {
            if (saveSystem != null)
            {
                saveSystem.SavePlayerProgress(CurrentLevel, TotalScore, TotalCurrency);
            }
            else
            {
                // Fallback to PlayerPrefs
                PlayerPrefs.SetString("LastCompletedLevel", CurrentLevel);
                PlayerPrefs.SetInt("TotalScore", TotalScore);
                PlayerPrefs.SetInt("TotalCurrency", TotalCurrency);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Carica i dati salvati del giocatore
        /// </summary>
        private void LoadPlayerData()
        {
            if (saveSystem != null)
            {
                PlayerData data = saveSystem.LoadPlayerData();
                if (data != null)
                {
                    TotalScore = data.totalScore;
                    TotalCurrency = data.totalCurrency;
                }
            }
            else
            {
                // Fallback to PlayerPrefs
                TotalScore = PlayerPrefs.GetInt("TotalScore", 0);
                TotalCurrency = PlayerPrefs.GetInt("TotalCurrency", 0);
            }
        }

        /// <summary>
        /// Cancella tutti i dati salvati (da usare con cautela)
        /// </summary>
        public void ClearAllSavedData()
        {
            if (saveSystem != null)
            {
                saveSystem.ClearAllData();
            }

            // Also clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            
            // Reset runtime variables
            TotalScore = 0;
            TotalCurrency = 0;
            currentScore = 0;
            currentCurrency = 0;
            isTutorialCompleted = false;
            
            Debug.Log("All saved data cleared");
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Identifica a quale mondo appartiene un livello
        /// </summary>
        private string GetWorldFromLevelName(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
                return "";

            if (levelName.Contains("Tutorial"))
                return "Tutorial";
            else if (levelName.Contains("World1") || levelName.Contains("City"))
                return "World1_City";
            else if (levelName.Contains("World2") || levelName.Contains("Forest"))
                return "World2_Forest";
            else if (levelName.Contains("World3") || levelName.Contains("Tundra"))
                return "World3_Tundra";
            else if (levelName.Contains("World4") || levelName.Contains("Volcano"))
                return "World4_Volcano";
            else if (levelName.Contains("World5") || levelName.Contains("Abyss"))
                return "World5_Abyss";
            else if (levelName.Contains("World6") || levelName.Contains("Virtual"))
                return "World6_Virtual";
            
            return "";
        }

        /// <summary>
        /// Determina il nome del prossimo livello basandosi sul livello corrente
        /// </summary>
        private string GetNextLevelName(string currentLevel)
        {
            // Tutorial progression
            if (currentLevel.Contains("Tutorial"))
            {
                if (currentLevel.Contains("Level1"))
                    return "Tutorial/Level2_PerfectSlide";
                else if (currentLevel.Contains("Level2"))
                    return "Tutorial/Level3_ReadyReflexes";
                else if (currentLevel.Contains("Level3"))
                    return "Tutorial/Level4_ItemPower";
                else if (currentLevel.Contains("Level4"))
                    return "Tutorial/Level5_EscapeTrainer";
                else if (currentLevel.Contains("Level5"))
                    return firstLevelScene; // After tutorial, go to first real level
            }
            // World 1 progression
            else if (currentLevel.Contains("World1") || currentLevel.Contains("City"))
            {
                if (currentLevel.Contains("Level1") || currentLevel.Contains("CentralPark"))
                    return "World1_City/Level2_CommercialAvenues";
                else if (currentLevel.Contains("Level2") || currentLevel.Contains("CommercialAvenues"))
                    return "World1_City/Level3_ResidentialDistrict";
                else if (currentLevel.Contains("Level3") || currentLevel.Contains("ResidentialDistrict"))
                    return "World1_City/Level4_ConstructionArea";
                else if (currentLevel.Contains("Level4") || currentLevel.Contains("ConstructionArea"))
                    return "World1_City/Level5_IndustrialZone";
                else if (currentLevel.Contains("Level5") || currentLevel.Contains("IndustrialZone"))
                    return "World1_City/Level6_AbandonedSite";
                else if (currentLevel.Contains("Level6") || currentLevel.Contains("AbandonedSite"))
                    return "World1_City/Level7_RundownPeriphery";
                else if (currentLevel.Contains("Level7") || currentLevel.Contains("RundownPeriphery"))
                    return "World1_City/Level8_PollutedDistrict";
                else if (currentLevel.Contains("Level8") || currentLevel.Contains("PollutedDistrict"))
                    return "World1_City/Level9_TechCenter";
                else if (currentLevel.Contains("Level9") || currentLevel.Contains("TechCenter"))
                    return "World2_Forest/Level1_SunnyPath"; // Proceed to World 2
            }
            
            // Add similar logic for other worlds
            
            // If no match found or at the end of a world
            return "";
        }

        /// <summary>
        /// Controlla se un livello fa parte del tutorial
        /// </summary>
        private bool IsTutorialLevel(string levelName)
        {
            return levelName.Contains("Tutorial") || 
                   levelName.Contains("FirstSteps") || 
                   levelName.Contains("PerfectSlide") || 
                   levelName.Contains("ReadyReflexes") || 
                   levelName.Contains("ItemPower") || 
                   levelName.Contains("EscapeTrainer");
        }

        /// <summary>
        /// Imposta la difficoltà di gioco
        /// </summary>
        public void SetDifficulty(GameDifficulty difficulty)
        {
            currentDifficulty = difficulty;
            
            // Apply difficulty settings to the game
            ApplyDifficultySettings(difficulty);
        }

        /// <summary>
        /// Applica le impostazioni basate sulla difficoltà selezionata
        /// </summary>
        private void ApplyDifficultySettings(GameDifficulty difficulty)
        {
            switch (difficulty)
            {
                case GameDifficulty.Easy:
                    // Apply easy settings
                    break;
                case GameDifficulty.Normal:
                    // Apply normal settings
                    break;
                case GameDifficulty.Hard:
                    // Apply hard settings
                    break;
                case GameDifficulty.Extreme:
                    // Apply extreme settings
                    break;
            }
        }
        #endregion
    }

    /// <summary>
    /// Rappresenta lo stato corrente del gioco
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Tutorial,
        Playing,
        Paused,
        LevelCompleted,
        GameOver
    }

    /// <summary>
    /// Livelli di difficoltà disponibili
    /// </summary>
    public enum GameDifficulty
    {
        Easy,
        Normal,
        Hard,
        Extreme
    }

    /// <summary>
    /// Contiene i dati sul risultato di un livello
    /// </summary>
    [System.Serializable]
    public class LevelResult
    {
        public bool playerWon;
        public int score;
        public int coinsCollected;
        public int gemsCollected;
        public float completionTime;
        public float distanceTraveled;
        public int enemiesDefeated;
        public int obstaclesAvoided;
        public int itemsUsed;
        public int hitsTaken;
    }

    /// <summary>
    /// Struttura per i dati del giocatore salvati
    /// </summary>
    [System.Serializable]
    public class PlayerData
    {
        public string lastCompletedLevel;
        public int totalScore;
        public int totalCurrency;
        public bool tutorialCompleted;
        public string[] unlockedCharacters;
        public string[] unlockedLevels;
        public Dictionary<string, bool> achievements;
    }

    /// <summary>
    /// Scheletro della classe SaveSystem, implementata separatamente
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        public void SavePlayerProgress(string level, int score, int currency)
        {
            // Implementation for saving player progress
        }

        public void SaveCurrency(int currency)
        {
            // Implementation for saving currency
        }

        public PlayerData LoadPlayerData()
        {
            // Implementation for loading player data
            return null;
        }

        public void ClearAllData()
        {
            // Implementation for clearing all data
        }
    }

}