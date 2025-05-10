using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Systems.Gameplay.Group;
using RunawayHeroes.ECS.Components.UI;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema centralizzato che gestisce gli eventi di progressione per la comunicazione tra sistemi.
    /// Si occupa di propagare eventi tra i diversi livelli di progressione (tutorial, mondo, livello).
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(LevelProgressionSystem))]
    public partial struct ProgressionEventSystem : ISystem
    {
        // Query per varie entità
        private EntityQuery _playerProgressionQuery;
        private EntityQuery _progressionEventsQuery;
        private EntityQuery _unlockEventsQuery;
        
        // Audio manager
        private AudioManager _audioManager;
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Inizializza query per la progressione globale
            _playerProgressionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerProgressionComponent>()
                .Build(ref state);
            
            // Inizializza query per eventi di avanzamento progressione
            _progressionEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ProgressionAdvancementEvent>()
                .Build(ref state);
            
            // Inizializza query per eventi di sblocco
            _unlockEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<UnlockEvent>()
                .Build(ref state);
            
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi aggiornamento solo se ci sono eventi di progressione
            state.RequireForUpdate(_progressionEventsQuery);
            state.RequireForUpdate(_unlockEventsQuery);
        }
        
        /// <summary>
        /// Cerca e ottiene riferimenti
        /// </summary>
        public void OnStartRunning(ref SystemState state)
        {
            // Ottieni riferimento all'audio manager
            _audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna pulizia speciale richiesta
        }
        
        /// <summary>
        /// Aggiorna il sistema di eventi progressione
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Processa eventi di avanzamento progressione
            ProcessProgressionAdvancementEvents(ref state, commandBuffer);
            
            // Processa eventi di sblocco
            ProcessUnlockEvents(ref state, commandBuffer);
            
            // Salva periodicamente lo stato di progressione (non implementato in questa versione)
            // SaveProgressionData();
        }
        
        /// <summary>
        /// Processa eventi di avanzamento progressione
        /// </summary>
        private void ProcessProgressionAdvancementEvents(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            // Query entities with ProgressionAdvancementEvent
            var progressionEventQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<ProgressionAdvancementEvent>());
            using var entities = progressionEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var eventData = state.EntityManager.GetComponentData<ProgressionAdvancementEvent>(entity);
                
                // Propaga l'evento di avanzamento tra diversi sistemi
                switch (eventData.ProgressionType)
                {
                    case 0: // Tutorial
                        // Avanzamento tutorial potrebbe influenzare mondo
                        if (eventData.IsSignificantAdvancement)
                        {
                            NotifyWorldOfTutorialAdvancement(commandBuffer, eventData.PrimaryIndex, ref state);
                        }
                        break;
                        
                    case 1: // Mondo
                        // Avanzamento mondo potrebbe influenzare livelli
                        if (eventData.IsSignificantAdvancement)
                        {
                            NotifyLevelsOfWorldAdvancement(commandBuffer, eventData.PrimaryIndex);
                        }
                        break;
                        
                    case 2: // Livello
                        // Avanzamento livello potrebbe influenzare mondo
                        if (eventData.IsSignificantAdvancement)
                        {
                            NotifyWorldOfLevelAdvancement(commandBuffer, eventData.PrimaryIndex, eventData.SecondaryIndex, ref state);
                        }
                        break;
                }
                
                // Aggiorna anche PlayerPrefs (sistema di salvataggio)
                UpdatePlayerPrefs(eventData);
                
                // Riproduce effetti audio appropriati per avanzamento
                PlayProgressionAudio(eventData);
                
                // Rimuovi l'evento dopo l'elaborazione
                commandBuffer.DestroyEntity(entity);
            }
        }
        
        /// <summary>
        /// Processa eventi di sblocco
        /// </summary>
        private void ProcessUnlockEvents(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            // Query entities with UnlockEvent
            var unlockEventQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<UnlockEvent>());
            using var entities = unlockEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var unlockData = state.EntityManager.GetComponentData<UnlockEvent>(entity);
                
                // Gestisci sblocchi in base al tipo
                switch (unlockData.UnlockType)
                {
                    case 0: // Tutorial
                        // Sblocco tutorial potrebbe influenzare mondo
                        if (unlockData.UnlockedItemIndex > 0)
                        {
                            // Seleziona audio appropriato
                            PlayUnlockAudio("unlock_tutorial");
                        }
                        break;
                        
                    case 1: // Mondo
                        // Sblocco mondo è un avanzamento significativo
                        if (unlockData.UnlockedItemIndex > 0)
                        {
                            // Seleziona audio appropriato
                            PlayUnlockAudio("unlock_world");
                            
                            // Registra nelle statistiche
                            UpdatePlayerPrefsForUnlock("World", unlockData.UnlockedItemIndex);
                        }
                        break;
                        
                    case 2: // Livello
                        // Sblocco livello potrebbe influenzare mondo
                        if (unlockData.UnlockedItemIndex > 0)
                        {
                            // Audio più semplice per livello
                            PlayUnlockAudio("unlock_level");
                            
                            // Registra nelle statistiche
                            int worldIndex = unlockData.UnlockedItemIndex / 100;
                            int levelIndex = unlockData.UnlockedItemIndex % 100;
                            UpdatePlayerPrefsForUnlock($"Level_{worldIndex}", levelIndex);
                        }
                        break;
                        
                    case 3: // Personaggio
                        // Sblocco personaggio è un avanzamento molto significativo
                        if (unlockData.UnlockedItemIndex > 0)
                        {
                            // Audio speciale per personaggio
                            PlayUnlockAudio("unlock_character");
                            
                            // Registra nelle statistiche
                            UpdatePlayerPrefsForUnlock("Character", unlockData.UnlockedItemIndex);
                        }
                        break;
                        
                    case 4: // Abilità
                        // Sblocco abilità
                        if (unlockData.UnlockedItemIndex > 0)
                        {
                            // Audio per abilità
                            PlayUnlockAudio("unlock_ability");
                            
                            // Registra nelle statistiche
                            UpdatePlayerPrefsForUnlock("Ability", unlockData.UnlockedItemIndex);
                        }
                        break;
                }
                
                // Rimuovi l'evento dopo l'elaborazione
                commandBuffer.DestroyEntity(entity);
            }
        }
        
        /// <summary>
        /// Notifica avanzamento tutorial al sistema mondo
        /// </summary>
        private void NotifyWorldOfTutorialAdvancement(EntityCommandBuffer commandBuffer, int tutorialIndex, ref SystemState state)
        {
            // Se tutti i tutorial sono completati, sblocca il primo mondo vero
            if (!_playerProgressionQuery.IsEmpty)
            {
                var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                var playerProgress = SystemAPI.GetComponent<PlayerProgressionComponent>(playerProgressEntity);
                
                if (playerProgress.TutorialsCompleted && playerProgress.HighestUnlockedWorld < 1)
                {
                    // Crea evento di sblocco mondo
                    Entity unlockEvent = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(unlockEvent, new UnlockEvent
                    {
                        UnlockType = 1, // Mondo
                        UnlockedItemIndex = 1, // Mondo 1 (Città)
                        UnlockedItemName = new FixedString64Bytes("Città in Caos")
                    });
                    
                    // Mostra un messaggio UI
                    CreateWorldUnlockMessage(commandBuffer, 1);
                }
            }
        }
        
        /// <summary>
        /// Notifica avanzamento mondo ai livelli
        /// </summary>
        private void NotifyLevelsOfWorldAdvancement(EntityCommandBuffer commandBuffer, int worldIndex)
        {
            // Se un mondo è stato sbloccato, sblocca il suo primo livello
            if (worldIndex > 0)
            {
                Entity unlockEvent = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(unlockEvent, new UnlockEvent
                {
                    UnlockType = 2, // Livello
                    UnlockedItemIndex = (worldIndex * 100), // Livello 0 del mondo specificato
                    UnlockedItemName = new FixedString64Bytes($"Livello 1 - {GetWorldName(worldIndex)}")
                });
                
                // Mostra un messaggio UI
                CreateLevelUnlockMessage(commandBuffer, worldIndex, 0);
            }
        }
        
        /// <summary>
        /// Notifica avanzamento livello al sistema mondo
        /// </summary>
        private void NotifyWorldOfLevelAdvancement(EntityCommandBuffer commandBuffer, int worldIndex, int levelIndex, ref SystemState state)
        {
            // Se l'ultimo livello di un mondo è stato completato, genera evento di completamento mondo
            if (levelIndex >= GetMaxLevelsForWorld(worldIndex) - 1) // -1 perché l'indice è 0-based
            {
                // Verifica se il mondo è già stato marcato come completato
                bool isWorldAlreadyCompleted = false;
                
                if (!_playerProgressionQuery.IsEmpty)
                {
                    var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                    var playerProgress = SystemAPI.GetComponent<PlayerProgressionComponent>(playerProgressEntity);
                    
                    isWorldAlreadyCompleted = (playerProgress.WorldsCompleted & (1 << worldIndex)) != 0;
                }
                
                if (!isWorldAlreadyCompleted)
                {
                    // Crea evento di completamento mondo
                    Entity worldCompletionEvent = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(worldCompletionEvent, new WorldCompletionEvent
                    {
                        WorldIndex = worldIndex,
                        IsFullyCompleted = false, // Valutato da WorldProgressionSystem
                        NextWorldToUnlock = worldIndex + 1,
                        FragmentIndex = worldIndex, // Stesso indice
                        CharacterUnlocked = GetCharacterIndexForWorld(worldIndex)
                    });
                }
            }
        }
        
        /// <summary>
        /// Aggiorna PlayerPrefs per un evento di avanzamento
        /// </summary>
        private void UpdatePlayerPrefs(ProgressionAdvancementEvent progressionEvent)
        {
            // In una implementazione reale, salveremmo i dati di progressione in modo più strutturato
            // Per semplicità, qui usiamo solo PlayerPrefs di base
            
            switch (progressionEvent.ProgressionType)
            {
                case 0: // Tutorial
                    PlayerPrefs.SetInt($"Tutorial_{progressionEvent.PrimaryIndex}_Completed", 1);
                    break;
                    
                case 1: // Mondo
                    PlayerPrefs.SetInt($"World_{progressionEvent.PrimaryIndex}_Progress", 
                                       PlayerPrefs.GetInt($"World_{progressionEvent.PrimaryIndex}_Progress", 0) + progressionEvent.ValueChanged);
                    break;
                    
                case 2: // Livello
                    PlayerPrefs.SetInt($"Level_{progressionEvent.PrimaryIndex}_{progressionEvent.SecondaryIndex}_Progress", 
                                       PlayerPrefs.GetInt($"Level_{progressionEvent.PrimaryIndex}_{progressionEvent.SecondaryIndex}_Progress", 0) + progressionEvent.ValueChanged);
                    break;
            }
            
            // Salva modifiche
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Aggiorna PlayerPrefs per un evento di sblocco
        /// </summary>
        private void UpdatePlayerPrefsForUnlock(string prefix, int index)
        {
            PlayerPrefs.SetInt($"{prefix}_{index}_Unlocked", 1);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Riproduce audio appropriato per eventi di progressione
        /// </summary>
        private void PlayProgressionAudio(ProgressionAdvancementEvent progressionEvent)
        {
            if (_audioManager == null)
                return;
                
            string audioName = "progress_generic";
            
            // Seleziona audio in base al tipo e significatività
            if (progressionEvent.IsSignificantAdvancement)
            {
                switch (progressionEvent.ProgressionType)
                {
                    case 0: // Tutorial
                        audioName = "progress_tutorial";
                        break;
                        
                    case 1: // Mondo
                        audioName = "progress_world";
                        break;
                        
                    case 2: // Livello
                        audioName = "progress_level";
                        break;
                }
            }
            
            // Riproduci l'audio
            _audioManager.PlaySFX(audioName);
        }
        
        /// <summary>
        /// Riproduce audio appropriato per eventi di sblocco
        /// </summary>
        private void PlayUnlockAudio(string unlockType)
        {
            if (_audioManager == null)
                return;
                
            _audioManager.PlaySFX(unlockType);
        }
        
        /// <summary>
        /// Crea un messaggio UI per lo sblocco di un mondo
        /// </summary>
        private void CreateWorldUnlockMessage(EntityCommandBuffer commandBuffer, int worldIndex)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string message = $"Nuovo Mondo Sbloccato: {GetWorldName(worldIndex)}!";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 6.0f,
                RemainingTime = 6.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 2000 + worldIndex
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
            
            string message = $"Nuovo Livello Sbloccato: Livello {levelIndex + 1} in {GetWorldName(worldIndex)}!";
                
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
        /// Ottiene l'indice personaggio da sbloccare per un mondo
        /// </summary>
        private int GetCharacterIndexForWorld(int worldIndex)
        {
            // Mappa di quale personaggio viene sbloccato in quale mondo
            int[] characterMap = new int[]
            {
                -1, // Tutorial - nessuno
                1,  // City - Maya
                2,  // Forest - Kai
                3,  // Tundra - Ember
                4,  // Volcano - Marina
                5   // Abyss - Neo
            };
            
            if (worldIndex >= 0 && worldIndex < characterMap.Length)
                return characterMap[worldIndex];
                
            return -1; // Nessun personaggio
        }
    }
}