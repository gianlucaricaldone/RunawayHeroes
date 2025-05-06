using System;
using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente principale per tracciare la progressione globale del giocatore
    /// </summary>
    [Serializable]
    public struct PlayerProgressionComponent : IComponentData
    {
        // Progressione tutorial
        public int CompletedTutorialCount;        // Numero di tutorial completati
        public int HighestUnlockedTutorial;       // Indice più alto di tutorial sbloccato
        public bool TutorialsCompleted;           // Se tutti i tutorial sono stati completati
        
        // Progressione mondo
        public int HighestUnlockedWorld;          // Indice più alto di mondo sbloccato (0-5)
        public int CurrentActiveWorld;            // Mondo attualmente attivo
        public byte WorldsCompleted;              // Bitmap dei mondi completati (bit 0-5)
        
        // Progressione frammenti
        public int TotalFragmentsCollected;       // Numero totale di frammenti raccolti
        public byte FragmentsCollectedMask;       // Bitmap dei frammenti raccolti
        
        // Progressione livelli
        public int TotalStarsEarned;              // Stelle totali guadagnate
        public int TotalLevelsCompleted;          // Livelli totali completati
        public int TotalBonusObjectivesCompleted; // Obiettivi bonus totali completati
        
        // Progressione personaggi
        public byte UnlockedCharactersMask;       // Bitmap dei personaggi sbloccati
        public byte CurrentActiveCharacter;       // Personaggio attualmente attivo (0-5)
        
        // Timestamp
        public long LastUpdatedTimestamp;         // Timestamp dell'ultimo aggiornamento
    }
    
    #region Componenti Tutorial
    
    /// <summary>
    /// Componente specifico per tracciare la progressione dei tutorial
    /// </summary>
    [Serializable]
    public struct TutorialProgressionComponent : IComponentData
    {
        public int CompletedTutorialCount;        // Numero di tutorial completati
        public int HighestUnlockedTutorial;       // Indice più alto di tutorial sbloccato
        public bool AllTutorialsCompleted;        // Se tutti i tutorial sono stati completati
        
        // Metriche di apprendimento
        public int MechanicsLearned;              // Numero di meccaniche apprese
        public int TutorialRetryCount;            // Quante volte i tutorial sono stati ripetuti
        public float AverageTutorialCompletionTime; // Tempo medio di completamento tutorial
    }
    
    /// <summary>
    /// Tag che identifica un livello tutorial
    /// </summary>
    [Serializable]
    public struct TutorialLevelTag : IComponentData
    {
        public int TutorialIndex;             // Indice del tutorial nella sequenza
        public int CurrentSequence;           // Sequenza corrente del tutorial
        public bool Completed;                // Se il tutorial è stato completato
    }
    
    /// <summary>
    /// Tag per identificare il completamento di un tutorial
    /// </summary>
    [Serializable]
    public struct TutorialCompletedTag : IComponentData
    {
        public int CompletedTutorialIndex;    // Indice del tutorial completato
    }
    
    /// <summary>
    /// Evento generato quando viene completato un tutorial
    /// </summary>
    [Serializable]
    public struct TutorialCompletionEvent : IComponentData
    {
        public int CompletedTutorialIndex;    // Indice del tutorial completato
        public bool AllTutorialsCompleted;    // Flag che indica se tutti i tutorial sono stati completati
        public int NextTutorialToUnlock;      // Indice del prossimo tutorial da sbloccare
        public float CompletionTime;          // Tempo impiegato per completare il tutorial
    }
    
    #endregion
    
    #region Componenti Mondo
    
    /// <summary>
    /// Componente per tracciare la progressione all'interno di un mondo
    /// </summary>
    [Serializable]
    public struct WorldProgressionComponent : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo (0-5)
        public FixedString32Bytes WorldName;      // Nome del mondo
        public byte CompletedLevelsBitmap;        // Bitmap dei livelli completati
        public byte FullyCompletedLevelsBitmap;   // Bitmap dei livelli completati al 100%
        public byte UnlockedLevelsBitmap;         // Bitmap dei livelli sbloccati
        public bool IsFragmentCollected;          // Se il frammento principale è stato raccolto
        public bool IsBossDefeated;               // Se il boss principale è stato sconfitto
        public byte CollectiblesMask;             // Bitmap dei collezionabili trovati
        
        // Meta-progressione
        public int TotalStarsInWorld;             // Stelle totali disponibili in questo mondo
        public int StarsCollected;                // Stelle raccolte in questo mondo
        public byte DifficultyLevel;              // Livello di difficoltà attuale (1-5)
    }
    
    /// <summary>
    /// Tag che identifica un mondo di gioco
    /// </summary>
    [Serializable]
    public struct WorldTag : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo (0-5)
        public bool IsActive;                     // Se il mondo è attualmente attivo
    }
    
    /// <summary>
    /// Evento generato quando viene completato un mondo intero
    /// </summary>
    [Serializable]
    public struct WorldCompletionEvent : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo completato
        public bool IsFullyCompleted;             // Se il mondo è stato completato al 100%
        public int NextWorldToUnlock;             // Indice del prossimo mondo da sbloccare
        public int FragmentIndex;                 // Indice del frammento raccolto
        public int CharacterUnlocked;             // Indice del personaggio sbloccato (-1 se nessuno)
    }
    
    /// <summary>
    /// Tag per identificare lo sblocco di un nuovo mondo
    /// </summary>
    [Serializable]
    public struct WorldUnlockedTag : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo sbloccato
    }
    
    #endregion
    
    #region Componenti Livello
    
    /// <summary>
    /// Componente per tracciare la progressione all'interno di un livello
    /// </summary>
    [Serializable]
    public struct LevelProgressionComponent : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo di appartenenza
        public int LevelIndex;                    // Indice del livello nel mondo
        public byte StarCount;                    // Numero di stelle ottenute (0-3)
        public bool IsCompleted;                  // Se il livello è stato completato
        public bool IsBonusObjectiveCompleted;    // Se l'obiettivo bonus è stato completato
        public bool AreAllCollectiblesFound;      // Se tutti i collezionabili sono stati trovati
        public float BestCompletionTime;          // Miglior tempo di completamento
        public int AttemptCount;                  // Numero di tentativi
        public byte TreasuresFound;               // Bitmap dei tesori trovati
        public long LastPlayedTimestamp;          // Timestamp dell'ultimo tentativo
    }
    
    /// <summary>
    /// Tag che identifica un livello di gioco
    /// </summary>
    [Serializable]
    public struct LevelTag : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo di appartenenza
        public int LevelIndex;                    // Indice del livello nel mondo
        public FixedString64Bytes LevelName;      // Nome del livello
        public bool IsActive;                     // Se il livello è attualmente attivo
    }
    
    /// <summary>
    /// Evento generato quando viene completato un livello
    /// </summary>
    [Serializable]
    public struct LevelCompletionEvent : IComponentData
    {
        public int WorldIndex;                    // Indice del mondo
        public int LevelIndex;                    // Indice del livello
        public byte StarsEarned;                  // Stelle guadagnate in questo tentativo
        public bool BonusObjectiveCompleted;      // Se l'obiettivo bonus è stato completato
        public float CompletionTime;              // Tempo di completamento
        public int CollectiblesFound;             // Numero di collezionabili trovati
        public int TreasuresFound;                // Numero di tesori trovati
        public bool IsNewHighScore;               // Se è un nuovo record
    }
    
    #endregion
    
    #region Eventi di Comunicazione
    
    /// <summary>
    /// Evento che notifica avanzamento nella progressione generale
    /// </summary>
    [Serializable]
    public struct ProgressionAdvancementEvent : IComponentData
    {
        public byte ProgressionType;              // 0=Tutorial, 1=World, 2=Level
        public int PrimaryIndex;                  // Indice principale (tutorial/world/level)
        public int SecondaryIndex;                // Indice secondario (usato solo per livelli)
        public int ValueChanged;                  // Valore cambiato (stelle, frammenti, ecc.)
        public bool IsSignificantAdvancement;     // Se è un avanzamento significativo
    }
    
    /// <summary>
    /// Evento che notifica lo sblocco di un nuovo elemento
    /// </summary>
    [Serializable]
    public struct UnlockEvent : IComponentData
    {
        public byte UnlockType;                   // 0=Tutorial, 1=World, 2=Level, 3=Character, 4=Ability
        public int UnlockedItemIndex;             // Indice dell'elemento sbloccato
        public FixedString64Bytes UnlockedItemName; // Nome dell'elemento sbloccato
    }
    
    /// <summary>
    /// Evento che notifica il salvataggio dei dati di progressione
    /// </summary>
    [Serializable]
    public struct ProgressionSaveEvent : IComponentData
    {
        public bool IsAutosave;                   // Se è un salvataggio automatico
        public long Timestamp;                    // Timestamp del salvataggio
    }
    
    #endregion
}