using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RunawayHeroes.Manager;

namespace RunawayHeroes.Core.SaveSystem
{
    /// <summary>
    /// Gestisce il salvataggio e il caricamento dei dati di gioco,
    /// utilizzando sia PlayerPrefs per salvataggi semplici che JSON
    /// per dati più complessi.
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        [Header("Save Settings")]
        [SerializeField] private string saveFileName = "runaway_heroes_save.json";
        [SerializeField] private bool useEncryption = false;
        [SerializeField] private string encryptionKey = "R4n@w@yH3r03s";
        
        [Header("Auto Save")]
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        
        // Private variables
        private float lastAutoSaveTime = 0f;
        private bool hasSaveDataLoaded = false;
        
        // Singleton pattern
        public static SaveSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            // Initial load of save data
            LoadPlayerData();
            hasSaveDataLoaded = true;
        }
        
        private void Update()
        {
            // Handle auto-save
            if (enableAutoSave && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                AutoSave();
            }
        }
        
        /// <summary>
        /// Salva automaticamente i dati di gioco a intervalli regolari
        /// </summary>
        private void AutoSave()
        {
            if (GameManager.Instance != null)
            {
                SavePlayerProgress(
                    GameManager.Instance.CurrentLevel,
                    GameManager.Instance.TotalScore,
                    GameManager.Instance.TotalCurrency
                );
                
                lastAutoSaveTime = Time.time;
                Debug.Log("Auto save completed");
            }
        }
        
        /// <summary>
        /// Salva i dati di progresso del giocatore
        /// </summary>
        public void SavePlayerProgress(string level, int score, int currency)
        {
            // Create player data object
            PlayerData data = new PlayerData
            {
                lastCompletedLevel = level,
                totalScore = score,
                totalCurrency = currency,
                tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1
            };
            
            // Load existing data first to preserve other fields
            PlayerData existingData = LoadPlayerData();
            if (existingData != null)
            {
                data.unlockedCharacters = existingData.unlockedCharacters;
                data.unlockedLevels = existingData.unlockedLevels;
                data.achievements = existingData.achievements;
            }
            
            // Save to file
            SaveToFile(data);
            
            // Also save critical data to PlayerPrefs as backup
            PlayerPrefs.SetString("LastCompletedLevel", level);
            PlayerPrefs.SetInt("TotalScore", score);
            PlayerPrefs.SetInt("TotalCurrency", currency);
            PlayerPrefs.Save();
            
            Debug.Log($"Saved player progress: Level={level}, Score={score}, Currency={currency}");
        }
        
        /// <summary>
        /// Salva solo la valuta del giocatore
        /// </summary>
        public void SaveCurrency(int currency)
        {
            PlayerPrefs.SetInt("TotalCurrency", currency);
            PlayerPrefs.Save();
            
            // Update in data file too
            PlayerData data = LoadPlayerData();
            if (data != null)
            {
                data.totalCurrency = currency;
                SaveToFile(data);
            }
            
            Debug.Log($"Saved currency: {currency}");
        }
        
        /// <summary>
        /// Salva lo stato di completamento di un livello
        /// </summary>
        public void SaveLevelCompletion(string levelName, bool completed)
        {
            PlayerPrefs.SetInt($"Level_{levelName}_Completed", completed ? 1 : 0);
            PlayerPrefs.Save();
            
            // Update unlocked levels in data file
            PlayerData data = LoadPlayerData();
            if (data != null)
            {
                // Add to unlocked levels if not already present
                if (completed && (data.unlockedLevels == null || !ArrayContains(data.unlockedLevels, levelName)))
                {
                    List<string> levels = new List<string>();
                    if (data.unlockedLevels != null)
                    {
                        levels.AddRange(data.unlockedLevels);
                    }
                    levels.Add(levelName);
                    data.unlockedLevels = levels.ToArray();
                    
                    SaveToFile(data);
                }
            }
            
            Debug.Log($"Saved level completion: {levelName} = {completed}");
        }
        
        /// <summary>
        /// Salva lo sblocco di un personaggio
        /// </summary>
        public void SaveCharacterUnlock(string characterName, bool unlocked)
        {
            PlayerPrefs.SetInt($"Character_{characterName}_Unlocked", unlocked ? 1 : 0);
            PlayerPrefs.Save();
            
            // Update unlocked characters in data file
            PlayerData data = LoadPlayerData();
            if (data != null)
            {
                // Add to unlocked characters if not already present
                if (unlocked && (data.unlockedCharacters == null || !ArrayContains(data.unlockedCharacters, characterName)))
                {
                    List<string> characters = new List<string>();
                    if (data.unlockedCharacters != null)
                    {
                        characters.AddRange(data.unlockedCharacters);
                    }
                    characters.Add(characterName);
                    data.unlockedCharacters = characters.ToArray();
                    
                    SaveToFile(data);
                }
            }
            
            Debug.Log($"Saved character unlock: {characterName} = {unlocked}");
        }
        
        /// <summary>
        /// Carica i dati salvati del giocatore
        /// </summary>
        public PlayerData LoadPlayerData()
        {
            // First try to load from file
            PlayerData data = LoadFromFile();
            
            // If no file data, create new data object with values from PlayerPrefs
            if (data == null)
            {
                data = new PlayerData
                {
                    lastCompletedLevel = PlayerPrefs.GetString("LastCompletedLevel", ""),
                    totalScore = PlayerPrefs.GetInt("TotalScore", 0),
                    totalCurrency = PlayerPrefs.GetInt("TotalCurrency", 0),
                    tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1,
                    unlockedCharacters = new string[] { "Alex" }, // Always unlock the first character
                    unlockedLevels = new string[] { "World1_City/Level1_CentralPark" }, // Always unlock first level
                    achievements = new Dictionary<string, bool>()
                };
            }
            
            return data;
        }
        
        /// <summary>
        /// Verifica se un livello è stato completato
        /// </summary>
        public bool IsLevelCompleted(string levelName)
        {
            return PlayerPrefs.GetInt($"Level_{levelName}_Completed", 0) == 1;
        }
        
        /// <summary>
        /// Verifica se un personaggio è stato sbloccato
        /// </summary>
        public bool IsCharacterUnlocked(string characterName)
        {
            // Alex è sempre sbloccato
            if (characterName.Equals("Alex"))
                return true;
                
            return PlayerPrefs.GetInt($"Character_{characterName}_Unlocked", 0) == 1;
        }
        
        /// <summary>
        /// Cancella tutti i dati salvati
        /// </summary>
        public void ClearAllData()
        {
            // Clear PlayerPrefs
            PlayerPrefs.DeleteAll();
            
            // Delete save file if it exists
            string filePath = GetSavePath();
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            Debug.Log("All save data cleared");
        }
        
        #region File Operations
        /// <summary>
        /// Salva i dati del giocatore su file
        /// </summary>
        private void SaveToFile(PlayerData data)
        {
            // Convert data to JSON
            string jsonData = JsonUtility.ToJson(data, true);
            
            // Encrypt if needed
            if (useEncryption)
            {
                jsonData = EncryptDecrypt(jsonData);
            }
            
            // Write to file
            string filePath = GetSavePath();
            
            try
            {
                File.WriteAllText(filePath, jsonData);
                Debug.Log($"Data saved to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error saving data: {e.Message}");
            }
        }
        
        /// <summary>
        /// Carica i dati del giocatore da file
        /// </summary>
        private PlayerData LoadFromFile()
        {
            string filePath = GetSavePath();
            
            // Check if file exists
            if (!File.Exists(filePath))
            {
                Debug.Log("Save file not found");
                return null;
            }
            
            try
            {
                // Read file content
                string jsonData = File.ReadAllText(filePath);
                
                // Decrypt if needed
                if (useEncryption)
                {
                    jsonData = EncryptDecrypt(jsonData);
                }
                
                // Convert JSON to object
                PlayerData data = JsonUtility.FromJson<PlayerData>(jsonData);
                Debug.Log($"Data loaded from: {filePath}");
                
                return data;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading data: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Ottiene il percorso del file di salvataggio
        /// </summary>
        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }
        
        /// <summary>
        /// Semplice cifratura/decifratura XOR
        /// </summary>
        private string EncryptDecrypt(string data)
        {
            char[] result = new char[data.Length];
            
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (char)(data[i] ^ encryptionKey[i % encryptionKey.Length]);
            }
            
            return new string(result);
        }
        #endregion
        
        /// <summary>
        /// Verifica se un array contiene un determinato valore
        /// </summary>
        private bool ArrayContains(string[] array, string value)
        {
            if (array == null)
                return false;
                
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] == value)
                    return true;
            }
            
            return false;
        }
    }
}