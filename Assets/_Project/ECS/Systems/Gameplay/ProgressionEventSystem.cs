using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
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
    public partial class ProgressionEventSystem : SystemBase
    {
        // Query per varie entità
        private EntityQuery _playerProgressionQuery;
        private EntityQuery _progressionEventsQuery;
        private EntityQuery _unlockEventsQuery;
        
        // Buffer per i comandi
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Audio manager
        private AudioManager _audioManager;
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        protected override void OnCreate()
        {
            // Inizializza query per la progressione globale
            _playerProgressionQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            
            // Inizializza query per eventi di avanzamento progressione
            _progressionEventsQuery = GetEntityQuery(ComponentType.ReadOnly<ProgressionAdvancementEvent>());
            
            // Inizializza query per eventi di sblocco
            _unlockEventsQuery = GetEntityQuery(ComponentType.ReadOnly<UnlockEvent>());
            
            // Ottieni riferimento al command buffer
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi aggiornamento solo se ci sono eventi di progressione
            RequireAnyForUpdate(new EntityQuery[] { _progressionEventsQuery, _unlockEventsQuery });
        }
        
        /// <summary>
        /// Cerca e ottiene riferimenti
        /// </summary>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            // Ottieni riferimento all'audio manager
            _audioManager = UnityEngine.Object.FindObjectOfType<AudioManager>();
        }
        
        /// <summary>
        /// Aggiorna il sistema di eventi progressione
        /// </summary>
        protected override void OnUpdate()
        {
            // Ottieni il buffer per i comandi
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer();
            
            // Processa eventi di avanzamento progressione
            ProcessProgressionAdvancementEvents(commandBuffer);
            
            // Processa eventi di sblocco
            ProcessUnlockEvents(commandBuffer);
            
            // Salva periodicamente lo stato di progressione (non implementato in questa versione)
            // SaveProgressionData();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Processa eventi di avanzamento progressione
        /// </summary>
        private void ProcessProgressionAdvancementEvents(EntityCommandBuffer commandBuffer)
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in ProgressionAdvancementEvent progressionEvent) =>
                {
                    // Propaga l'evento di avanzamento tra diversi sistemi
                    switch (progressionEvent.ProgressionType)
                    {
                        case 0: // Tutorial
                            // Avanzamento tutorial potrebbe influenzare mondo
                            if (progressionEvent.IsSignificantAdvancement)
                            {
                                NotifyWorldOfTutorialAdvancement(commandBuffer, progressionEvent.PrimaryIndex);
                            }
                            break;
                            
                        case 1: // Mondo
                            // Avanzamento mondo potrebbe influenzare livelli
                            if (progressionEvent.IsSignificantAdvancement)
                            {
                                NotifyLevelsOfWorldAdvancement(commandBuffer, progressionEvent.PrimaryIndex);
                            }
                            break;
                            
                        case 2: // Livello
                            // Avanzamento livello potrebbe influenzare mondo
                            if (progressionEvent.IsSignificantAdvancement)
                            {
                                NotifyWorldOfLevelAdvancement(commandBuffer, progressionEvent.PrimaryIndex, progressionEvent.SecondaryIndex);
                            }
                            break;
                    }
                    
                    // Aggiorna anche PlayerPrefs (sistema di salvataggio)
                    UpdatePlayerPrefs(progressionEvent);
                    
                    // Riproduce effetti audio appropriati per avanzamento
                    PlayProgressionAudio(progressionEvent);
                    
                    // Rimuovi l'evento dopo l'elaborazione
                    EntityManager.DestroyEntity(entity);
                }).Run();
        }
        
        /// <summary>
        /// Processa eventi di sblocco
        /// </summary>
        private void ProcessUnlockEvents(EntityCommandBuffer commandBuffer)
        {
            Entities
                .WithoutBurst()
                .WithStructuralChanges()
                .ForEach((Entity entity, in UnlockEvent unlockEvent) =>
                {
                    // Gestisci sblocchi in base al tipo
                    switch (unlockEvent.UnlockType)
                    {
                        case 0: // Tutorial
                            // Sblocco tutorial potrebbe influenzare mondo
                            if (unlockEvent.UnlockedItemIndex > 0)
                            {
                                // Seleziona audio appropriato
                                PlayUnlockAudio("unlock_tutorial");
                            }
                            break;
                            
                        case 1: // Mondo
                            // Sblocco mondo è un avanzamento significativo
                            if (unlockEvent.UnlockedItemIndex > 0)
                            {
                                // Seleziona audio appropriato
                                PlayUnlockAudio("unlock_world");
                                
                                // Registra nelle statistiche
                                UpdatePlayerPrefsForUnlock("World", unlockEvent.UnlockedItemIndex);
                            }
                            break;
                            
                        case 2: // Livello
                            // Sblocco livello potrebbe influenzare mondo
                            if (unlockEvent.UnlockedItemIndex > 0)
                            {
                                // Audio più semplice per livello
                                PlayUnlockAudio("unlock_level");
                                
                                // Registra nelle statistiche
                                int worldIndex = unlockEvent.UnlockedItemIndex / 100;
                                int levelIndex = unlockEvent.UnlockedItemIndex % 100;
                                UpdatePlayerPrefsForUnlock($"Level_{worldIndex}", levelIndex);
                            }
                            break;
                            
                        case 3: // Personaggio
                            // Sblocco personaggio è un avanzamento molto significativo
                            if (unlockEvent.UnlockedItemIndex > 0)
                            {
                                // Audio speciale per personaggio
                                PlayUnlockAudio("unlock_character");
                                
                                // Registra nelle statistiche
                                UpdatePlayerPrefsForUnlock("Character", unlockEvent.UnlockedItemIndex);
                            }
                            break;
                            
                        case 4: // Abilità
                            // Sblocco abilità
                            if (unlockEvent.UnlockedItemIndex > 0)
                            {
                                // Audio per abilità
                                PlayUnlockAudio("unlock_ability");
                                
                                // Registra nelle statistiche
                                UpdatePlayerPrefsForUnlock("Ability", unlockEvent.UnlockedItemIndex);
                            }
                            break;
                    }
                    
                    // Rimuovi l'evento dopo l'elaborazione
                    EntityManager.DestroyEntity(entity);
                }).Run();
        }
        
        /// <summary>
        /// Notifica avanzamento tutorial al sistema mondo
        /// </summary>
        private void NotifyWorldOfTutorialAdvancement(EntityCommandBuffer commandBuffer, int tutorialIndex)
        {
            // Se tutti i tutorial sono completati, sblocca il primo mondo vero
            if (!_playerProgressionQuery.IsEmpty)
            {
                var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                var playerProgress = EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                
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
        private void NotifyWorldOfLevelAdvancement(EntityCommandBuffer commandBuffer, int worldIndex, int levelIndex)
        {
            // Se l'ultimo livello di un mondo è stato completato, genera evento di completamento mondo
            if (levelIndex >= GetMaxLevelsForWorld(worldIndex) - 1) // -1 perché l'indice è 0-based
            {
                // Verifica se il mondo è già stato marcato come completato
                bool isWorldAlreadyCompleted = false;
                
                if (!_playerProgressionQuery.IsEmpty)
                {
                    var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                    var playerProgress = EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                    
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