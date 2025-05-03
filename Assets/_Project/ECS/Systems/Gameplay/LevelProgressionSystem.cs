using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.UI;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema specializzato che gestisce la progressione del giocatore all'interno dei singoli livelli.
    /// Si occupa del tracking delle stelle, collezionabili e obiettivi bonus.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(WorldProgressionSystem))]
    public partial class LevelProgressionSystem : SystemBase
    {
        // Query per varie entità
        private EntityQuery _playerProgressionQuery;
        private EntityQuery _worldProgressionQuery;
        private EntityQuery _levelProgressionQuery;
        private EntityQuery _activeLevelQuery;
        
        // Buffer per i comandi
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Stato
        private bool _initializationComplete = false;
        
        // Contatori per la sessione corrente
        private int _currentSessionScore = 0;
        private int _currentSessionCollectibles = 0;
        private int _currentSessionTreasures = 0;
        private float _currentSessionStartTime = 0;
        private bool _bonusObjectiveCompleted = false;
        
        // Livello attivo
        private int _activeWorldIndex = -1;
        private int _activeLevelIndex = -1;
        
        // Costanti
        private const float TIME_FOR_THREE_STARS = 90.0f;  // 1:30
        private const float TIME_FOR_TWO_STARS = 120.0f;   // 2:00
        private const float TIME_FOR_ONE_STAR = 180.0f;    // 3:00
        private const int COLLECTIBLES_PER_LEVEL = 5;      // Numero di collezionabili in ogni livello
        private const int TREASURES_PER_LEVEL = 3;         // Numero di tesori nascosti in ogni livello
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        protected override void OnCreate()
        {
            // Inizializza query per la progressione globale
            _playerProgressionQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            
            // Inizializza query per la progressione dei mondi
            _worldProgressionQuery = GetEntityQuery(ComponentType.ReadWrite<WorldProgressionComponent>());
            
            // Inizializza query per la progressione dei livelli
            _levelProgressionQuery = GetEntityQuery(ComponentType.ReadWrite<LevelProgressionComponent>());
            
            // Inizializza query per il livello attivo
            _activeLevelQuery = GetEntityQuery(
                ComponentType.ReadOnly<LevelTag>()
            );
            
            // Ottieni riferimento al command buffer
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi aggiornamento solo se esiste progressione o livello attivo
            RequireAnyForUpdate(new EntityQuery[] { _playerProgressionQuery, _activeLevelQuery });
        }
        
        /// <summary>
        /// Inizializza la sessione corrente
        /// </summary>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            // Reimposta contatori per la sessione
            ResetSessionCounters();
            
            // Identifica il livello attivo
            IdentifyActiveLevel();
        }
        
        /// <summary>
        /// Aggiorna il sistema di progressione dei livelli
        /// </summary>
        protected override void OnUpdate()
        {
            // Inizializza se necessario
            if (!_initializationComplete)
            {
                InitializeLevelProgressionData();
                _initializationComplete = true;
            }
            
            // Ottieni il buffer per i comandi
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer();
            
            // Processa eventi di completamento livello
            ProcessLevelCompletionEvents(commandBuffer);
            
            // Processa eventi di raccolta oggetti
            ProcessCollectionEvents(commandBuffer);
            
            // Processa eventi di completamento obiettivi
            ProcessObjectiveCompletionEvents(commandBuffer);
            
            // Aggiorna progresso globale
            UpdateGlobalProgress();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Reimposta i contatori per una nuova sessione di gioco
        /// </summary>
        private void ResetSessionCounters()
        {
            _currentSessionScore = 0;
            _currentSessionCollectibles = 0;
            _currentSessionTreasures = 0;
            _currentSessionStartTime = Time.time;
            _bonusObjectiveCompleted = false;
            _activeWorldIndex = -1;
            _activeLevelIndex = -1;
        }
        
        /// <summary>
        /// Identifica il livello attualmente attivo
        /// </summary>
        private void IdentifyActiveLevel()
        {
            Entities
                .WithoutBurst()
                .ForEach((in LevelTag levelTag) =>
                {
                    if (levelTag.IsActive)
                    {
                        _activeWorldIndex = levelTag.WorldIndex;
                        _activeLevelIndex = levelTag.LevelIndex;
                        
                        Debug.Log($"Livello attivo identificato: Mondo {_activeWorldIndex}, Livello {_activeLevelIndex}");
                    }
                }).Run();
        }
        
        /// <summary>
        /// Processa eventi di completamento livello
        /// </summary>
        private void ProcessLevelCompletionEvents(EntityCommandBuffer commandBuffer)
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in LevelCompletionEvent completionEvent) =>
                {
                    int worldIndex = completionEvent.WorldIndex;
                    int levelIndex = completionEvent.LevelIndex;
                    
                    Debug.Log($"Livello {levelIndex} del Mondo {worldIndex} completato!");
                    
                    // Calcola tempo di completamento
                    float completionTime = completionEvent.CompletionTime;
                    if (completionTime <= 0)
                    {
                        completionTime = Time.time - _currentSessionStartTime;
                    }
                    
                    // Determina quante stelle guadagnate
                    byte starsEarned = calculateStars(completionTime, completionEvent.StarsEarned);
                    
                    // Se il livello è nuovo o i dati sono migliori, aggiorna la progressione
                    Entity levelProgressEntity = GetOrCreateLevelProgressionEntity(worldIndex, levelIndex);
                    var levelProgress = EntityManager.GetComponentData<LevelProgressionComponent>(levelProgressEntity);
                    
                    bool isNewBest = false;
                    bool isFirstCompletion = !levelProgress.IsCompleted;
                    
                    // Aggiorna solo se è la prima volta o se è meglio
                    if (isFirstCompletion || starsEarned > levelProgress.StarCount || completionTime < levelProgress.BestCompletionTime)
                    {
                        isNewBest = true;
                        
                        // Aggiorna progressione livello
                        levelProgress.StarCount = Math.Max(levelProgress.StarCount, starsEarned);
                        levelProgress.IsCompleted = true;
                        levelProgress.BestCompletionTime = isFirstCompletion ? completionTime : Math.Min(levelProgress.BestCompletionTime, completionTime);
                        levelProgress.AttemptCount++;
                        levelProgress.LastPlayedTimestamp = DateTime.Now.Ticks;
                        
                        // Aggiorna con dati di collezionabili e obiettivi bonus
                        if (completionEvent.BonusObjectiveCompleted || _bonusObjectiveCompleted)
                        {
                            levelProgress.IsBonusObjectiveCompleted = true;
                        }
                        
                        // Aggiorna bitmap tesori se ci sono nuovi tesori
                        if (completionEvent.TreasuresFound > 0 || _currentSessionTreasures > 0)
                        {
                            int treasuresFound = completionEvent.TreasuresFound > 0 ? 
                                completionEvent.TreasuresFound : _currentSessionTreasures;
                                
                            // Ogni bit nel byte rappresenta un tesoro
                            for (int i = 0; i < treasuresFound; i++)
                            {
                                levelProgress.TreasuresFound |= (byte)(1 << i);
                            }
                        }
                        
                        // Aggiorna entità progressione livello
                        EntityManager.SetComponentData(levelProgressEntity, levelProgress);
                        
                        // Aggiorna anche la progressione del mondo
                        UpdateWorldProgressionForLevel(worldIndex, levelIndex, starsEarned, levelProgress.IsBonusObjectiveCompleted);
                        
                        // Sblocca il prossimo livello se è la prima volta
                        if (isFirstCompletion)
                        {
                            UnlockNextLevel(commandBuffer, worldIndex, levelIndex);
                        }
                    }
                    
                    // Crea messaggio UI appropriato
                    CreateLevelCompletionMessage(commandBuffer, worldIndex, levelIndex, starsEarned, isNewBest);
                    
                    // Reimposta contatori sessione
                    ResetSessionCounters();
                    
                    // Rimuovi l'evento dopo l'elaborazione
                    EntityManager.DestroyEntity(entity);
                }).Run();
        }
        
        /// <summary>
        /// Processa eventi di raccolta oggetti
        /// </summary>
        private void ProcessCollectionEvents(EntityCommandBuffer commandBuffer)
        {
            // In questa versione non implementiamo la raccolta real-time durante il gameplay
            // In un'implementazione completa, qui gestiremmo eventi come:
            // - CollectibleCollectedEvent
            // - TreasureFoundEvent
        }
        
        /// <summary>
        /// Processa eventi di completamento obiettivi
        /// </summary>
        private void ProcessObjectiveCompletionEvents(EntityCommandBuffer commandBuffer)
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in ObjectiveCompletedEvent completionEvent) =>
                {
                    // Se è un'obiettivo bonus, marcalo come completato
                    if (completionEvent.ObjectiveType == 2 || 
                        completionEvent.ObjectiveType == 3 || 
                        completionEvent.ObjectiveType == 5)
                    {
                        _bonusObjectiveCompleted = true;
                        
                        // Se siamo in un livello attivo, aggiorniamo subito i dati
                        if (_activeWorldIndex >= 0 && _activeLevelIndex >= 0)
                        {
                            Entity levelProgressEntity = GetOrCreateLevelProgressionEntity(_activeWorldIndex, _activeLevelIndex);
                            var levelProgress = EntityManager.GetComponentData<LevelProgressionComponent>(levelProgressEntity);
                            
                            if (!levelProgress.IsBonusObjectiveCompleted)
                            {
                                levelProgress.IsBonusObjectiveCompleted = true;
                                EntityManager.SetComponentData(levelProgressEntity, levelProgress);
                                
                                // Aggiorna anche progressione mondo e globale
                                UpdateWorldProgressionForBonusObjective(_activeWorldIndex);
                            }
                        }
                    }
                    
                    // Rimuovi l'evento dopo l'elaborazione
                    EntityManager.DestroyEntity(entity);
                }).Run();
        }
        
        /// <summary>
        /// Calcola il numero di stelle guadagnate in base al tempo di completamento
        /// </summary>
        private byte calculateStars(float completionTime, byte existingStars)
        {
            // Rispetta le stelle già assegnate dall'evento
            if (existingStars > 0)
                return existingStars;
                
            if (completionTime <= TIME_FOR_THREE_STARS)
                return 3;
            else if (completionTime <= TIME_FOR_TWO_STARS)
                return 2;
            else if (completionTime <= TIME_FOR_ONE_STAR || completionTime > 0)
                return 1;
                
            return 0;
        }
        
        /// <summary>
        /// Aggiorna la progressione del mondo per un livello completato
        /// </summary>
        private void UpdateWorldProgressionForLevel(int worldIndex, int levelIndex, byte starsEarned, bool bonusObjectiveCompleted)
        {
            // Trova l'entità di progressione mondo
            Entity worldProgressEntity = Entity.Null;
            
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref WorldProgressionComponent worldProgress) =>
                {
                    if (worldProgress.WorldIndex == worldIndex)
                    {
                        worldProgressEntity = entity;
                        
                        // Aggiorna bitmap di completamento
                        if (levelIndex < 8) // Massimo 8 bit in un byte
                        {
                            // Marca questo livello come completato
                            worldProgress.CompletedLevelsBitmap |= (byte)(1 << levelIndex);
                            
                            // Se è stato completato al 100% (3 stelle + bonus)
                            if (starsEarned == 3 && bonusObjectiveCompleted)
                            {
                                worldProgress.FullyCompletedLevelsBitmap |= (byte)(1 << levelIndex);
                            }
                        }
                        
                        // Aggiorna conteggio stelle
                        int previousStars = worldProgress.StarsCollected;
                        
                        // Calcola quante stelle sono già associate a questo livello
                        int levelBitPosition = levelIndex % 8;
                        bool isLevelCompleted = (worldProgress.CompletedLevelsBitmap & (1 << levelBitPosition)) != 0;
                        
                        // Ottieni stelle precedenti con getStarsForLevel (metodo fittizio)
                        int previousLevelStars = GetStarsForLevel(worldIndex, levelIndex);
                        
                        // Calcola incremento (solo stelle aggiuntive)
                        int starDifference = starsEarned - previousLevelStars;
                        if (starDifference > 0)
                        {
                            worldProgress.StarsCollected += starDifference;
                        }
                    }
                }).Run();
                
            // Se l'entità è stata trovata e modificata, aggiorna anche la progressione globale
            if (worldProgressEntity != Entity.Null && !_playerProgressionQuery.IsEmpty)
            {
                var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                var playerProgress = EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                
                // Ricalcola il totale stelle e livelli completati
                UpdateGlobalProgress();
            }
        }
        
        /// <summary>
        /// Aggiorna la progressione del mondo per un obiettivo bonus completato
        /// </summary>
        private void UpdateWorldProgressionForBonusObjective(int worldIndex)
        {
            if (!_playerProgressionQuery.IsEmpty)
            {
                var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                var playerProgress = EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                
                // Incrementa counter obiettivi bonus
                playerProgress.TotalBonusObjectivesCompleted++;
                playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                
                EntityManager.SetComponentData(playerProgressEntity, playerProgress);
            }
        }
        
        /// <summary>
        /// Sblocca il livello successivo se esiste
        /// </summary>
        private void UnlockNextLevel(EntityCommandBuffer commandBuffer, int worldIndex, int levelIndex)
        {
            // Calcola indice del prossimo livello
            int nextLevelIndex = levelIndex + 1;
            
            // Trova l'entità di progressione mondo
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref WorldProgressionComponent worldProgress) =>
                {
                    if (worldProgress.WorldIndex == worldIndex)
                    {
                        // Se il prossimo livello è valido, sbloccalo
                        if (nextLevelIndex < 8) // Massimo 8 bit in un byte
                        {
                            // Verifica che non sia già sbloccato
                            bool isAlreadyUnlocked = (worldProgress.UnlockedLevelsBitmap & (1 << nextLevelIndex)) != 0;
                            
                            if (!isAlreadyUnlocked)
                            {
                                // Sblocca il prossimo livello
                                worldProgress.UnlockedLevelsBitmap |= (byte)(1 << nextLevelIndex);
                                
                                // Crea evento di sblocco
                                Entity unlockEvent = commandBuffer.CreateEntity();
                                commandBuffer.AddComponent(unlockEvent, new UnlockEvent
                                {
                                    UnlockType = 2, // Livello
                                    UnlockedItemIndex = (worldIndex * 100) + nextLevelIndex, // Codice composito
                                    UnlockedItemName = new FixedString64Bytes($"Livello {nextLevelIndex + 1} - {GetWorldName(worldIndex)}")
                                });
                                
                                // Crea messaggio UI per lo sblocco
                                CreateLevelUnlockMessage(commandBuffer, worldIndex, nextLevelIndex);
                                
                                // Salva nelle PlayerPrefs
                                PlayerPrefs.SetInt($"Level_{worldIndex}_{nextLevelIndex}_Unlocked", 1);
                                PlayerPrefs.Save();
                            }
                        }
                        // Se abbiamo completato l'ultimo livello, considera il mondo completato
                        else if (nextLevelIndex >= GetMaxLevelsForWorld(worldIndex) &&
                                 !worldProgress.IsBossDefeated)
                        {
                            // Crea evento di completamento mondo
                            Entity worldCompletionEvent = commandBuffer.CreateEntity();
                            commandBuffer.AddComponent(worldCompletionEvent, new WorldCompletionEvent
                            {
                                WorldIndex = worldIndex,
                                IsFullyCompleted = IsWorldFullyCompleted(worldProgress),
                                NextWorldToUnlock = worldIndex + 1,
                                FragmentIndex = worldIndex,
                                CharacterUnlocked = worldIndex // Mappa indice mondo a indice personaggio
                            });
                        }
                    }
                }).Run();
        }
        
        /// <summary>
        /// Aggiorna il progresso globale basato sui livelli e mondi
        /// </summary>
        private void UpdateGlobalProgress()
        {
            if (_playerProgressionQuery.IsEmpty)
                return;
                
            var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
            var playerProgress = EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
            
            // Conta totale stelle e livelli
            int totalStars = 0;
            int totalLevels = 0;
            int totalBonusObjectives = 0;
            
            // Scorri mondi
            Entities
                .WithoutBurst()
                .ForEach((in WorldProgressionComponent worldProgress) =>
                {
                    totalStars += worldProgress.StarsCollected;
                    
                    // Conta livelli completati
                    byte completedLevels = worldProgress.CompletedLevelsBitmap;
                    while (completedLevels > 0)
                    {
                        totalLevels += (completedLevels & 1);
                        completedLevels >>= 1;
                    }
                }).Run();
                
            // Scorri livelli per obiettivi bonus
            Entities
                .WithoutBurst()
                .ForEach((in LevelProgressionComponent levelProgress) =>
                {
                    if (levelProgress.IsBonusObjectiveCompleted)
                    {
                        totalBonusObjectives++;
                    }
                }).Run();
                
            // Aggiorna solo se necessario
            bool needsUpdate = false;
            
            if (playerProgress.TotalStarsEarned != totalStars)
            {
                playerProgress.TotalStarsEarned = totalStars;
                needsUpdate = true;
            }
            
            if (playerProgress.TotalLevelsCompleted != totalLevels)
            {
                playerProgress.TotalLevelsCompleted = totalLevels;
                needsUpdate = true;
            }
            
            if (playerProgress.TotalBonusObjectivesCompleted != totalBonusObjectives)
            {
                playerProgress.TotalBonusObjectivesCompleted = totalBonusObjectives;
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                EntityManager.SetComponentData(playerProgressEntity, playerProgress);
            }
        }
        
        /// <summary>
        /// Inizializza i dati di progressione
        /// </summary>
        private void InitializeLevelProgressionData()
        {
            // Se non esiste già, crea l'entità per la progressione globale
            if (_playerProgressionQuery.IsEmpty)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new PlayerProgressionComponent
                {
                    CompletedTutorialCount = 0,
                    HighestUnlockedTutorial = 0,
                    TutorialsCompleted = false,
                    HighestUnlockedWorld = 0,
                    CurrentActiveWorld = 0,
                    WorldsCompleted = 0,
                    TotalFragmentsCollected = 0,
                    FragmentsCollectedMask = 0,
                    TotalStarsEarned = 0,
                    TotalLevelsCompleted = 0,
                    TotalBonusObjectivesCompleted = 0,
                    UnlockedCharactersMask = 1, // Solo Alex sbloccato
                    CurrentActiveCharacter = 0, // Alex attivo
                    LastUpdatedTimestamp = DateTime.Now.Ticks
                });
                
                Debug.Log("Creata nuova entità per la progressione globale del giocatore");
            }
            
            // Il resto dell'inizializzazione viene gestito dinamicamente quando necessario
        }
        
        /// <summary>
        /// Ottiene o crea un'entità di progressione livello
        /// </summary>
        private Entity GetOrCreateLevelProgressionEntity(int worldIndex, int levelIndex)
        {
            // Cerca l'entità esistente
            Entity levelEntity = Entity.Null;
            
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, in LevelProgressionComponent levelProgress) =>
                {
                    if (levelProgress.WorldIndex == worldIndex && levelProgress.LevelIndex == levelIndex)
                    {
                        levelEntity = entity;
                    }
                }).Run();
                
            // Se non esiste, crea una nuova entità
            if (levelEntity == Entity.Null)
            {
                levelEntity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(levelEntity, new LevelProgressionComponent
                {
                    WorldIndex = worldIndex,
                    LevelIndex = levelIndex,
                    StarCount = 0,
                    IsCompleted = false,
                    IsBonusObjectiveCompleted = false,
                    AreAllCollectiblesFound = false,
                    BestCompletionTime = float.MaxValue,
                    AttemptCount = 0,
                    TreasuresFound = 0,
                    LastPlayedTimestamp = DateTime.Now.Ticks
                });
                
                Debug.Log($"Creata nuova entità per la progressione del livello: Mondo {worldIndex}, Livello {levelIndex}");
            }
            
            return levelEntity;
        }
        
        /// <summary>
        /// Crea un messaggio UI per il completamento di un livello
        /// </summary>
        private void CreateLevelCompletionMessage(EntityCommandBuffer commandBuffer, int worldIndex, int levelIndex, byte starsEarned, bool isNewBest)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string starText = new string('★', starsEarned) + new string('☆', 3 - starsEarned);
            string message = isNewBest ?
                $"Livello Completato! {starText} Nuovo Record!" :
                $"Livello Completato! {starText}";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 5.0f,
                RemainingTime = 5.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 5000 + (worldIndex * 100) + levelIndex
            });
            
            commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
            {
                QueuePosition = 0
            });
        }
        
        /// <summary>
        /// Crea un messaggio UI per lo sblocco di un livello
        /// </summary>
        private void CreateLevelUnlockMessage(EntityCommandBuffer commandBuffer, int worldIndex, int levelIndex)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string message = $"Nuovo Livello Sbloccato: Livello {levelIndex + 1}!";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 4.0f,
                RemainingTime = 4.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 6000 + (worldIndex * 100) + levelIndex
            });
            
            commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
            {
                QueuePosition = 1
            });
        }
        
        /// <summary>
        /// Verifica se un mondo è stato completato al 100%
        /// </summary>
        private bool IsWorldFullyCompleted(WorldProgressionComponent worldProgress)
        {
            int requiredBits = GetMaxLevelsForWorld(worldProgress.WorldIndex);
            byte completedMask = (byte)((1 << requiredBits) - 1);
            
            return (worldProgress.FullyCompletedLevelsBitmap & completedMask) == completedMask;
        }
        
        /// <summary>
        /// Ottiene il numero massimo di livelli per un mondo
        /// </summary>
        private int GetMaxLevelsForWorld(int worldIndex)
        {
            return worldIndex == 0 ? 5 : 9; // 5 livelli tutorial, 9 livelli per mondo normale
        }
        
        /// <summary>
        /// Ottiene il nome di un mondo in base all'indice
        /// </summary>
        private string GetWorldName(int worldIndex)
        {
            string[] worldNames = new string[]
            {
                "Tutorial",
                "Città in Caos",
                "Foresta Primordiale",
                "Tundra Eterna",
                "Inferno di Lava",
                "Abissi Inesplorati",
                "Realtà Virtuale"
            };
            
            if (worldIndex >= 0 && worldIndex < worldNames.Length)
                return worldNames[worldIndex];
                
            return "Mondo Sconosciuto";
        }
        
        /// <summary>
        /// Ottiene le stelle già assegnate a un livello
        /// </summary>
        private int GetStarsForLevel(int worldIndex, int levelIndex)
        {
            int stars = 0;
            
            Entities
                .WithoutBurst()
                .ForEach((in LevelProgressionComponent levelProgress) =>
                {
                    if (levelProgress.WorldIndex == worldIndex && levelProgress.LevelIndex == levelIndex)
                    {
                        stars = levelProgress.StarCount;
                    }
                }).Run();
                
            return stars;
        }
    }
}