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
            tutorialManager = FindAnyObjectByType<TutorialManager>();
            if (tutorialManager == null)
            {
                tutorialManager = GameObject.FindAnyObjectByType<TutorialManager>();
            }

            // Check if tutorial is completed from saved data
            isTutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

            // Initialize systems
            if (audioManager == null)
            {
                audioManager = FindAnyObjectByType<AudioManager>();
            }

            if (saveSystem == null)
            {
                saveSystem = FindAnyObjectByType<SaveSystem>();
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
            activePlayer = FindAnyObjectByType<PlayerController>();

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
        /// Chiamato quando un livello specifico del tutorial è stato completato.
        /// </summary>
        /// <param name="levelIndex">Indice del livello tutorial completato</param>
        public void TutorialLevelCompleted(int levelIndex)
        {
            Debug.Log($"Tutorial level {levelIndex} completed");

            // Salva il progresso di questo specifico livello di tutorial
            string key = $"TutorialLevel_{levelIndex}_Completed";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            // Se questo è l'ultimo livello del tutorial, marca l'intero tutorial come completato
            if (tutorialManager != null && levelIndex >= GetTutorialLevelsCount() - 1)
            {
                // Chiama il metodo che gestisce il completamento dell'intero tutorial
                OnTutorialCompleted();
            }
            
            // Sblocca funzionalità o fornisci ricompense basate sul livello di tutorial completato
            ProcessTutorialLevelRewards(levelIndex);
        }

        /// <summary>
        /// Gestisce il completamento di un livello in uno specifico mondo.
        /// </summary>
        /// <param name="worldIndex">Indice del mondo (1-6)</param>
        /// <param name="levelIndex">Indice del livello nel mondo (1-9)</param>
        public void WorldLevelCompleted(int worldIndex, int levelIndex)
        {
            Debug.Log($"World {worldIndex} Level {levelIndex} completed");

            // Salva il progresso di questo specifico livello nel mondo
            string levelKey = $"World{worldIndex}_Level{levelIndex}_Completed";
            PlayerPrefs.SetInt(levelKey, 1);
            
            // Tiene traccia del livello più alto completato in questo mondo
            int highestCompletedLevel = PlayerPrefs.GetInt($"World{worldIndex}_HighestLevel", 0);
            if (levelIndex > highestCompletedLevel)
            {
                PlayerPrefs.SetInt($"World{worldIndex}_HighestLevel", levelIndex);
            }
            
            PlayerPrefs.Save();

            // Aggiorna sblocchi basati sul completamento
            UpdateWorldUnlocks(worldIndex, levelIndex);
            
            // Processa ricompense specifiche per questo livello
            ProcessWorldLevelRewards(worldIndex, levelIndex);
            
            // Controlla se è stato completato l'intero mondo
            CheckWorldCompletion(worldIndex, levelIndex);
        }

        /// <summary>
        /// Aggiorna gli sblocchi basati sul completamento di un livello in un mondo.
        /// </summary>
        private void UpdateWorldUnlocks(int worldIndex, int levelIndex)
        {
            // Sblocca il livello successivo se non è l'ultimo
            int totalLevelsInWorld = GetTotalLevelsInWorld(worldIndex);
            if (levelIndex < totalLevelsInWorld)
            {
                string nextLevelKey = $"World{worldIndex}_Level{levelIndex + 1}_Unlocked";
                PlayerPrefs.SetInt(nextLevelKey, 1);
            }
            
            // Se è l'ultimo livello del mondo, sblocca il prossimo mondo
            else if (levelIndex == totalLevelsInWorld && worldIndex < 6)
            {
                string nextWorldKey = $"World{worldIndex + 1}_Unlocked";
                PlayerPrefs.SetInt(nextWorldKey, 1);
                
                // Sblocca anche il primo livello del mondo successivo
                string nextWorldFirstLevelKey = $"World{worldIndex + 1}_Level1_Unlocked";
                PlayerPrefs.SetInt(nextWorldFirstLevelKey, 1);
            }
            
            // Sblocca il personaggio del mondo se è il boss finale (livello 9)
            if (levelIndex == totalLevelsInWorld)
            {
                UnlockWorldCharacter(worldIndex);
            }
            
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Controlla se un intero mondo è stato completato e processa le ricompense per il completamento.
        /// </summary>
        private void CheckWorldCompletion(int worldIndex, int levelIndex)
        {
            int totalLevelsInWorld = GetTotalLevelsInWorld(worldIndex);
            
            // Verifica se questo è l'ultimo livello del mondo
            if (levelIndex == totalLevelsInWorld)
            {
                Debug.Log($"World {worldIndex} completed!");
                
                // Salva che il mondo è stato completato
                string worldCompletedKey = $"World{worldIndex}_Completed";
                PlayerPrefs.SetInt(worldCompletedKey, 1);
                PlayerPrefs.Save();
                
                // Sblocca contenuti speciali per aver completato il mondo
                ProcessWorldCompletionRewards(worldIndex);
                
                // Controlla se tutti i mondi sono stati completati
                CheckAllWorldsCompletion();
            }
        }

        /// <summary>
        /// Verifica se tutti i mondi sono stati completati e sblocca contenuti finali.
        /// </summary>
        private void CheckAllWorldsCompletion()
        {
            bool allWorldsCompleted = true;
            
            // Controlla il completamento di tutti e 6 i mondi
            for (int i = 1; i <= 6; i++)
            {
                if (PlayerPrefs.GetInt($"World{i}_Completed", 0) != 1)
                {
                    allWorldsCompleted = false;
                    break;
                }
            }
            
            if (allWorldsCompleted)
            {
                Debug.Log("All worlds completed! Unlocking final content!");
                
                // Salva che tutti i mondi sono stati completati
                PlayerPrefs.SetInt("AllWorldsCompleted", 1);
                PlayerPrefs.Save();
                
                // Sblocca contenuti finali del gioco
                UnlockFinalContent();
            }
        }

        /// <summary>
        /// Sblocca il personaggio associato a un mondo specifico.
        /// </summary>
        private void UnlockWorldCharacter(int worldIndex)
        {
            string characterKey = "";
            
            switch (worldIndex)
            {
                case 1:
                    characterKey = "Character_Alex_Unlocked";
                    break;
                case 2:
                    characterKey = "Character_Maya_Unlocked";
                    break;
                case 3:
                    characterKey = "Character_Kai_Unlocked";
                    break;
                case 4:
                    characterKey = "Character_Ember_Unlocked";
                    break;
                case 5:
                    characterKey = "Character_Marina_Unlocked";
                    break;
                case 6:
                    characterKey = "Character_Neo_Unlocked";
                    break;
            }
            
            if (!string.IsNullOrEmpty(characterKey))
            {
                PlayerPrefs.SetInt(characterKey, 1);
                PlayerPrefs.Save();
                Debug.Log($"Character for World {worldIndex} unlocked!");
            }
        }

        /// <summary>
        /// Elabora ricompense specifiche per il completamento dei livelli del tutorial.
        /// </summary>
        private void ProcessTutorialLevelRewards(int levelIndex)
        {
            switch (levelIndex)
            {
                case 0: // Primi Passi
                    // Sblocca funzionalità di salto o fornisci ricompensa
                    AddCurrency(50); // Ricompensa piccola per il primo completamento
                    break;
                case 1: // Scivolata Perfetta
                    // Sblocca scivolate o fornisci ricompensa
                    AddCurrency(75);
                    break;
                case 2: // Riflessi Pronti
                    // Sblocca movimento laterale o fornisci ricompensa
                    AddCurrency(100);
                    break;
                case 3: // Potere degli Oggetti
                    // Sblocca Focus Time o fornisci ricompensa
                    AddCurrency(150);
                    break;
                case 4: // Fuga dal Trainer
                    // Sblocca ricompensa finale del tutorial
                    AddCurrency(300);
                    UnlockStarterEquipment();
                    break;
            }
        }

        /// <summary>
        /// Elabora ricompense specifiche per il completamento dei livelli nei mondi principali.
        /// </summary>
        private void ProcessWorldLevelRewards(int worldIndex, int levelIndex)
        {
            // Ricompense di base aumentano con la progressione
            int baseCurrency = 100 * worldIndex + 50 * levelIndex;
            
            // Bonus speciali per livelli significativi (mid-boss, boss)
            if (levelIndex == 3 || levelIndex == 6) // Mid-boss ai livelli 3 e 6
            {
                baseCurrency += 200 * worldIndex;
                UnlockMidBossReward(worldIndex, levelIndex);
            }
            else if (levelIndex == 9) // Boss finale al livello 9
            {
                baseCurrency += 500 * worldIndex;
                UnlockBossReward(worldIndex);
            }
            
            AddCurrency(baseCurrency);
        }

        /// <summary>
        /// Sblocca ricompense specifiche per aver sconfitto un mid-boss.
        /// </summary>
        private void UnlockMidBossReward(int worldIndex, int levelIndex)
        {
            // Sblocca item speciali, abilità o skin basate sul mid-boss sconfitto
            string rewardKey = $"MidBossReward_World{worldIndex}_Level{levelIndex}";
            PlayerPrefs.SetInt(rewardKey, 1);
            
            Debug.Log($"Mid-boss reward unlocked for World {worldIndex}, Level {levelIndex}");
            
            // Qui potresti aggiungere codice specifico per diversi mid-boss
        }

        /// <summary>
        /// Sblocca ricompense specifiche per aver sconfitto il boss di un mondo.
        /// </summary>
        private void UnlockBossReward(int worldIndex)
        {
            // Sblocca ricompense speciali per aver sconfitto il boss del mondo
            string rewardKey = $"BossReward_World{worldIndex}";
            PlayerPrefs.SetInt(rewardKey, 1);
            
            Debug.Log($"Boss reward unlocked for World {worldIndex}");
            
            // Qui potresti aggiungere codice specifico per diversi boss
        }

        /// <summary>
        /// Processa ricompense per aver completato un intero mondo.
        /// </summary>
        private void ProcessWorldCompletionRewards(int worldIndex)
        {
            // Ricompensa principale per aver completato il mondo
            int completionBonus = 1000 * worldIndex;
            AddCurrency(completionBonus);
            
            // Sblocca skin speciali o potenziamenti
            string worldCompletionRewardKey = $"WorldCompletionReward_{worldIndex}";
            PlayerPrefs.SetInt(worldCompletionRewardKey, 1);
            
            Debug.Log($"World {worldIndex} completion rewards processed");
            
            // Qui potresti aggiungere codice specifico per diversi mondi
        }

        /// <summary>
        /// Sblocca contenuti finali per aver completato tutti i mondi.
        /// </summary>
        private void UnlockFinalContent()
        {
            // Sblocca mondi bonus premium
            PlayerPrefs.SetInt("PremiumWorld_Celestial_Unlocked", 1);
            PlayerPrefs.SetInt("PremiumWorld_Mythical_Unlocked", 1);
            
            // Sblocca modalità di gioco aggiuntive
            PlayerPrefs.SetInt("Mode_BossRush_Unlocked", 1);
            PlayerPrefs.SetInt("Mode_Speedrun_Unlocked", 1);
            
            // Sblocca ultimate skin per tutti i personaggi
            for (int i = 1; i <= 6; i++)
            {
                PlayerPrefs.SetInt($"UltimateSkin_Character{i}", 1);
            }
            
            // Ricompensa sostanziosa di valuta
            AddCurrency(10000);
            
            Debug.Log("Final game content unlocked!");
        }

        /// <summary>
        /// Sblocca equipaggiamento iniziale dopo aver completato il tutorial.
        /// </summary>
        private void UnlockStarterEquipment()
        {
            PlayerPrefs.SetInt("Equipment_StarterBoots_Unlocked", 1);
            PlayerPrefs.SetInt("Equipment_StarterGloves_Unlocked", 1);
            
            Debug.Log("Starter equipment unlocked");
        }

        /// <summary>
        /// Restituisce il numero totale di livelli in un mondo specifico.
        /// </summary>
        private int GetTotalLevelsInWorld(int worldIndex)
        {
            // La maggior parte dei mondi ha 9 livelli
            return 9;
        }

        /// <summary>
        /// Restituisce il numero totale di livelli nel tutorial.
        /// </summary>
        private int GetTutorialLevelsCount()
        {
            // Se il TutorialManager è disponibile, usa la sua proprietà
            if (tutorialManager != null && tutorialManager is TutorialManager manager)
            {
                // Assumiamo che TutorialManager abbia una proprietà per il numero di livelli
                // Se non ce l'ha, possiamo implementarla o usare un valore fisso
                return 5; // tutorialManager.TutorialLevelsCount
            }
            
            // Fallback a un valore fisso (5 livelli tutorial: Level1_FirstSteps fino a Level5_EscapeTrainer)
            return 5;
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

        /// <summary>
        /// Applica un effetto visivo allo schermo del giocatore per una durata specificata
        /// </summary>
        /// <param name="effectName">Nome dell'effetto da applicare</param>
        /// <param name="duration">Durata dell'effetto in secondi</param>
        public void ApplyScreenEffect(string effectName, float duration)
        {
            // Implementazione di base per gestire diversi tipi di effetti schermo
            Debug.Log($"Applying screen effect: {effectName} for {duration} seconds");

            // Qui potremmo avere logica differente basata sul tipo di effetto
            switch (effectName.ToLower())
            {
                case "blinded":
                    StartCoroutine(ApplyBlindedEffect(duration));
                    break;
                case "damaged":
                    StartCoroutine(ApplyDamagedEffect(duration));
                    break;
                case "slowmotion":
                    StartCoroutine(ApplySlowMotionEffect(duration));
                    break;
                default:
                    Debug.LogWarning($"Unknown screen effect: {effectName}");
                    break;
            }
        }

        private IEnumerator ApplyBlindedEffect(float duration)
        {
            // Qui potremmo attivare un canvas bianco che copre lo schermo
            // o modificare un effetto post-processing per aumentare la luminosità
            Debug.Log("Blinded effect started");
            
            // Trova il canvas dell'effetto o crealo se necessario
            // Questo è solo un esempio di implementazione
            /* 
            CanvasGroup blindEffect = GetBlindEffectCanvas();
            if (blindEffect != null)
            {
                // Fade in
                float startTime = Time.time;
                while (Time.time < startTime + 0.2f)
                {
                    float t = (Time.time - startTime) / 0.2f;
                    blindEffect.alpha = Mathf.Lerp(0, 1, t);
                    yield return null;
                }
                blindEffect.alpha = 1;
                
                // Mantieni l'effetto
                yield return new WaitForSeconds(duration - 0.4f);
                
                // Fade out
                startTime = Time.time;
                while (Time.time < startTime + 0.2f)
                {
                    float t = (Time.time - startTime) / 0.2f;
                    blindEffect.alpha = Mathf.Lerp(1, 0, t);
                    yield return null;
                }
                blindEffect.alpha = 0;
            }
            */
            
            // Versione semplificata per il debugging
            yield return new WaitForSeconds(duration);
            Debug.Log("Blinded effect ended");
        }

        private IEnumerator ApplyDamagedEffect(float duration)
        {
            // Esempio: attiva un effetto vignetta rossa per indicare il danno
            Debug.Log("Damaged effect started");
            
            // Implementazione semplificata per il debugging
            yield return new WaitForSeconds(duration);
            Debug.Log("Damaged effect ended");
        }

        private IEnumerator ApplySlowMotionEffect(float duration)
        {
            // Esempio: rallenta temporaneamente il tempo di gioco
            Debug.Log("Slow motion effect started");
            
            // Salva il valore originale di timeScale
            float originalTimeScale = Time.timeScale;
            
            // Applica slow motion
            Time.timeScale = 0.3f;
            
            // Attendi la durata (tenendo conto del timeScale modificato)
            yield return new WaitForSecondsRealtime(duration);
            
            // Ripristina il timeScale originale
            Time.timeScale = originalTimeScale;
            
            Debug.Log("Slow motion effect ended");
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