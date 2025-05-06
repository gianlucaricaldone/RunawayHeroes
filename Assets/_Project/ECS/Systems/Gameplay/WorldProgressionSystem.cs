using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using System;
using UnityEngine;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.UI;
using RunawayHeroes.ECS.Systems.Gameplay.Group;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema specializzato che gestisce la progressione del giocatore attraverso i vari mondi di gioco.
    /// Si occupa dello sblocco dei mondi, della raccolta dei frammenti e della progressione narrativa.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(TutorialProgressionSystem))]
    [BurstCompile]
    public partial struct WorldProgressionSystem : ISystem
    {
        // Query per varie entità
        private EntityQuery _playerProgressionQuery;
        private EntityQuery _worldProgressionQuery;
        private EntityQuery _activeWorldQuery;
        
        // Stato
        private bool _initializationComplete;
        
        // Reference to World
        private Unity.Entities.World _world;
        
        // Mapping dei personaggi sbloccabili per ogni mondo
        private int[] _worldCharacterMapping;
        
        // Nomi dei mondi
        private string[] _worldNames;
        
        // Costanti
        private const int TOTAL_WORLDS = 6;
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Stato
            _initializationComplete = false;
            _world = state.World;
            
            // Inizializza array di mapping dei personaggi
            _worldCharacterMapping = new int[]
            {
                -1, // Tutorial - Nessun personaggio
                1,  // City - Maya
                2,  // Forest - Kai
                3,  // Tundra - Ember
                4,  // Volcano - Marina
                5   // Abyss - Neo
            };
            
            // Inizializza array dei nomi dei mondi
            _worldNames = new string[]
            {
                "Tutorial",
                "Città in Caos",
                "Foresta Primordiale",
                "Tundra Eterna",
                "Inferno di Lava",
                "Abissi Inesplorati",
                "Realtà Virtuale"
            };
            
            // Inizializza query per la progressione globale
            _playerProgressionQuery = state.GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            
            // Inizializza query per la progressione dei mondi
            _worldProgressionQuery = state.GetEntityQuery(ComponentType.ReadWrite<WorldProgressionComponent>());
            
            // Inizializza query per il mondo attivo
            _activeWorldQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<WorldTag>(),
                ComponentType.ReadOnly<LevelTag>()
            );
            
            // Richiedi aggiornamento solo se esiste progressione o mondo attivo
            state.RequireForUpdate(_playerProgressionQuery);
            state.RequireForUpdate(_worldProgressionQuery);
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup se necessario
        }
        
        /// <summary>
        /// Aggiorna il sistema di progressione dei mondi
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Inizializza se necessario
            if (!_initializationComplete)
            {
                InitializeWorldProgressionData(ref state);
                _initializationComplete = true;
            }
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Processa eventi di completamento tutorial per sbloccare il primo mondo
            ProcessTutorialCompletionEvents(commandBuffer, ref state);
            
            // Processa eventi di completamento mondo
            ProcessWorldCompletionEvents(commandBuffer, ref state);
            
            // Processa eventi di raccolta frammenti
            ProcessFragmentCollectionEvents(commandBuffer, ref state);
            
            // Aggiorna stato generale progressione
            UpdateGlobalProgressionState(ref state);
        }
        
        /// <summary>
        /// Processa eventi di completamento tutorial per sbloccare il primo mondo
        /// </summary>
        private void ProcessTutorialCompletionEvents(EntityCommandBuffer commandBuffer, ref SystemState state)
        {
            // Poiché questo sistema ha bisogno di accedere a EntityManager e altre variabili di sistema,
            // e stiamo usando WithStructuralChanges, manteniamo questa logica nel main thread
            // invece di convertirla in IJobEntity
            // Process all entities with TutorialCompletionEvent
            var completionEvents = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TutorialCompletionEvent>());
            using var entities = completionEvents.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in entities)
            {
                var completionEvent = state.EntityManager.GetComponentData<TutorialCompletionEvent>(entity);
                // Se tutti i tutorial sono completati, sblocca il primo mondo
                if (completionEvent.AllTutorialsCompleted)
                {
                    Debug.Log("Tutti i tutorial completati! Sblocco del Mondo 1: Città in Caos");
                    
                    // Sblocca il primo mondo nella progressione globale
                    if (!_playerProgressionQuery.IsEmpty)
                    {
                        var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                        var playerProgress = state.EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                        
                        // Aggiorna solo se necessario
                        if (playerProgress.HighestUnlockedWorld < 1)
                        {
                            playerProgress.HighestUnlockedWorld = 1;
                            playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                            
                            state.EntityManager.SetComponentData(playerProgressEntity, playerProgress);
                            
                            // Crea evento di sblocco mondo
                            Entity unlockEvent = commandBuffer.CreateEntity();
                            commandBuffer.AddComponent(unlockEvent, new UnlockEvent
                            {
                                UnlockType = 1, // Mondo
                                UnlockedItemIndex = 1, // Città in Caos
                                UnlockedItemName = new FixedString64Bytes(_worldNames[1])
                            });
                            
                            // Crea un messaggio UI per lo sblocco
                            CreateWorldUnlockMessage(commandBuffer, 1, ref state);
                            
                            // Salva nelle PlayerPrefs
                            PlayerPrefs.SetInt("World1_Unlocked", 1);
                            PlayerPrefs.Save();
                        }
                    }
                }
                
                // Non rimuoviamo l'evento qui, lasciamo che TutorialProgressionSystem lo gestisca
            }
        }
        
        /// <summary>
        /// Processa eventi di completamento mondo
        /// </summary>
        private void ProcessWorldCompletionEvents(EntityCommandBuffer commandBuffer, ref SystemState state)
        {
            // Anche qui manteniamo l'implementazione nel main thread per l'accesso a EntityManager e altre variabili
            // Process all entities with WorldCompletionEvent
            var worldCompletionQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldCompletionEvent>());
            using var worldCompletionEntities = worldCompletionQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in worldCompletionEntities)
            {
                var completionEvent = state.EntityManager.GetComponentData<WorldCompletionEvent>(entity);
                int worldIndex = completionEvent.WorldIndex;
                int nextWorldIndex = worldIndex + 1;
                
                Debug.Log($"Mondo {worldIndex} ({_worldNames[worldIndex]}) completato!");
                
                // Aggiorna la progressione specifica del mondo
                Entity worldProgressEntity = GetOrCreateWorldProgressionEntity(worldIndex, ref state);
                var worldProgress = state.EntityManager.GetComponentData<WorldProgressionComponent>(worldProgressEntity);
                
                // Marca il mondo come completato
                worldProgress.IsBossDefeated = true;
                
                // Aggiorna la progressione globale
                if (!_playerProgressionQuery.IsEmpty)
                {
                    var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                    var playerProgress = state.EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
                    
                    // Aggiorna il bitmap dei mondi completati
                    playerProgress.WorldsCompleted |= (byte)(1 << worldIndex);
                    
                    // Aggiorna i frammenti raccolti se necessario
                    if (completionEvent.FragmentIndex >= 0 && !worldProgress.IsFragmentCollected)
                    {
                        worldProgress.IsFragmentCollected = true;
                        playerProgress.TotalFragmentsCollected++;
                        playerProgress.FragmentsCollectedMask |= (byte)(1 << completionEvent.FragmentIndex);
                        
                        // Crea messaggio per la raccolta del frammento
                        CreateFragmentCollectionMessage(commandBuffer, completionEvent.FragmentIndex, ref state);
                    }
                    
                    // Sblocca il prossimo mondo se esiste e non è già sbloccato
                    if (nextWorldIndex < TOTAL_WORLDS && playerProgress.HighestUnlockedWorld < nextWorldIndex)
                    {
                        playerProgress.HighestUnlockedWorld = nextWorldIndex;
                        
                        // Crea evento di sblocco mondo
                        Entity unlockEvent = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent(unlockEvent, new UnlockEvent
                        {
                            UnlockType = 1, // Mondo
                            UnlockedItemIndex = nextWorldIndex,
                            UnlockedItemName = new FixedString64Bytes(_worldNames[nextWorldIndex])
                        });
                        
                        // Crea un messaggio UI per lo sblocco
                        CreateWorldUnlockMessage(commandBuffer, nextWorldIndex, ref state);
                        
                        // Salva nelle PlayerPrefs
                        PlayerPrefs.SetInt($"World{nextWorldIndex}_Unlocked", 1);
                    }
                    
                    // Sblocca personaggio se previsto per questo mondo
                    int characterToUnlock = _worldCharacterMapping[worldIndex];
                    if (characterToUnlock >= 0)
                    {
                        // Controlla se il personaggio non è già sbloccato
                        bool isAlreadyUnlocked = (playerProgress.UnlockedCharactersMask & (1 << characterToUnlock)) != 0;
                        
                        if (!isAlreadyUnlocked)
                        {
                            // Sblocca il personaggio
                            playerProgress.UnlockedCharactersMask |= (byte)(1 << characterToUnlock);
                            
                            // Crea evento di sblocco personaggio
                            Entity characterUnlockEvent = commandBuffer.CreateEntity();
                            commandBuffer.AddComponent(characterUnlockEvent, new UnlockEvent
                            {
                                UnlockType = 3, // Personaggio
                                UnlockedItemIndex = characterToUnlock,
                                UnlockedItemName = new FixedString64Bytes(GetCharacterName(characterToUnlock))
                            });
                            
                            // Crea messaggio UI per lo sblocco personaggio
                            CreateCharacterUnlockMessage(commandBuffer, characterToUnlock, ref state);
                            
                            // Salva nelle PlayerPrefs
                            PlayerPrefs.SetInt($"Character{characterToUnlock}_Unlocked", 1);
                        }
                    }
                    
                    // Aggiorna timestamp
                    playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                    
                    // Salva progressione globale
                    state.EntityManager.SetComponentData(playerProgressEntity, playerProgress);
                }
                
                // Salva progressione mondo
                state.EntityManager.SetComponentData(worldProgressEntity, worldProgress);
                
                // Rimuovi l'evento dopo l'elaborazione
                state.EntityManager.DestroyEntity(entity);
                
                // Salva preferenze
                PlayerPrefs.Save();
            }
        }
        
        /// <summary>
        /// Processa eventi di raccolta frammenti
        /// </summary>
        private void ProcessFragmentCollectionEvents(EntityCommandBuffer commandBuffer, ref SystemState state)
        {
            // Non implementato in questa versione
            // Si tratterebbe di gestire eventi di raccolta frammenti durante il gameplay
        }
        
        /// <summary>
        /// Aggiorna lo stato globale di progressione
        /// </summary>
        private void UpdateGlobalProgressionState(ref SystemState state)
        {
            if (_playerProgressionQuery.IsEmpty || _worldProgressionQuery.IsEmpty)
                return;
                
            var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
            var playerProgress = state.EntityManager.GetComponentData<PlayerProgressionComponent>(playerProgressEntity);
            
            // Calcola numeri totali basati sui dati disponibili
            int totalFragments = 0;
            int totalStars = 0;
            int totalLevelsCompleted = 0;
            
            // Utilizziamo EntityManager per iterare sui componenti
            var wpQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldProgressionComponent>());
            using var wpEntities = wpQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in wpEntities)
            {
                var worldProgress = state.EntityManager.GetComponentData<WorldProgressionComponent>(entity);
                if (worldProgress.IsFragmentCollected)
                    totalFragments++;
                    
                totalStars += worldProgress.StarsCollected;
                
                // Conta livelli completati (conta bit a 1 nel bitmap)
                byte completedLevels = worldProgress.CompletedLevelsBitmap;
                while (completedLevels > 0)
                {
                    if ((completedLevels & 1) == 1)
                        totalLevelsCompleted++;
                        
                    completedLevels >>= 1;
                }
            }
                
            // Aggiorna il componente globale solo se necessario
            bool needsUpdate = false;
            
            if (playerProgress.TotalFragmentsCollected != totalFragments)
            {
                playerProgress.TotalFragmentsCollected = totalFragments;
                needsUpdate = true;
            }
            
            if (playerProgress.TotalStarsEarned != totalStars)
            {
                playerProgress.TotalStarsEarned = totalStars;
                needsUpdate = true;
            }
            
            if (playerProgress.TotalLevelsCompleted != totalLevelsCompleted)
            {
                playerProgress.TotalLevelsCompleted = totalLevelsCompleted;
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                state.EntityManager.SetComponentData(playerProgressEntity, playerProgress);
            }
        }
        
        /// <summary>
        /// Inizializza i dati di progressione dei mondi se non esistono già
        /// </summary>
        private void InitializeWorldProgressionData(ref SystemState state)
        {
            // Assicurati che l'entità di progressione globale esista
            if (_playerProgressionQuery.IsEmpty)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new PlayerProgressionComponent
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
            
            // Crea le entità di progressione mondo
            for (int i = 0; i < TOTAL_WORLDS; i++)
            {
                // Verifica se esiste già una progressione per questo mondo
                bool worldProgressionExists = false;
                
                // Usa EntityManager per verificare l'esistenza di un mondo
                var worldExistsQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldProgressionComponent>());
                using var worldExistsEntities = worldExistsQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in worldExistsEntities)
                {
                    var worldProgress = state.EntityManager.GetComponentData<WorldProgressionComponent>(entity);
                    if (worldProgress.WorldIndex == i)
                    {
                        worldProgressionExists = true;
                        break;
                    }
                }
                    
                if (!worldProgressionExists)
                {
                    // Crea una nuova entità progressione per questo mondo
                    Entity worldEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(worldEntity, new WorldProgressionComponent
                    {
                        WorldIndex = i,
                        WorldName = new FixedString32Bytes(_worldNames[i]),
                        CompletedLevelsBitmap = 0,
                        FullyCompletedLevelsBitmap = 0,
                        UnlockedLevelsBitmap = 1, // Primo livello sempre sbloccato se il mondo è disponibile
                        IsFragmentCollected = false,
                        IsBossDefeated = false,
                        CollectiblesMask = 0,
                        TotalStarsInWorld = GetTotalStarsForWorld(i),
                        StarsCollected = 0,
                        DifficultyLevel = 1
                    });
                    
                    Debug.Log($"Creata nuova entità per la progressione del mondo {i} ({_worldNames[i]})");
                }
            }
        }
        
        /// <summary>
        /// Ottiene o crea un'entità di progressione per un mondo specifico
        /// </summary>
        private Entity GetOrCreateWorldProgressionEntity(int worldIndex, ref SystemState state)
        {
            if (worldIndex < 0 || worldIndex >= TOTAL_WORLDS)
                throw new ArgumentOutOfRangeException(nameof(worldIndex));
                
            // Cerca l'entità esistente
            Entity worldEntity = Entity.Null;
            
            // Utilizza EntityManager per cercare il mondo
            var worldProgressQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldProgressionComponent>());
            using var worldProgressEntities = worldProgressQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in worldProgressEntities)
            {
                var worldProgress = state.EntityManager.GetComponentData<WorldProgressionComponent>(entity);
                if (worldProgress.WorldIndex == worldIndex)
                {
                    worldEntity = entity;
                    break;
                }
            }
                
            // Se non esiste, crea una nuova entità
            if (worldEntity == Entity.Null)
            {
                var systemState = _world.Unmanaged.GetExistingSystemState<WorldProgressionSystem>();
                InitializeWorldProgressionData(ref systemState);
                
                // Cerca di nuovo dopo l'inizializzazione
                var worldProgressQuery2 = systemState.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldProgressionComponent>());
                using var worldProgressEntities2 = worldProgressQuery2.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in worldProgressEntities2)
                {
                    var worldProgress = systemState.EntityManager.GetComponentData<WorldProgressionComponent>(entity);
                    if (worldProgress.WorldIndex == worldIndex)
                    {
                        worldEntity = entity;
                        break;
                    }
                }
            }
            
            return worldEntity;
        }
        
        /// <summary>
        /// Crea un messaggio UI per lo sblocco di un mondo
        /// </summary>
        private void CreateWorldUnlockMessage(EntityCommandBuffer commandBuffer, int worldIndex, ref SystemState state)
        {
            if (worldIndex < 0 || worldIndex >= _worldNames.Length)
                return;
                
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string message = $"Nuovo Mondo Sbloccato: {_worldNames[worldIndex]}!";
                
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
        /// Crea un messaggio UI per la raccolta di un frammento
        /// </summary>
        private void CreateFragmentCollectionMessage(EntityCommandBuffer commandBuffer, int fragmentIndex, ref SystemState state)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string fragmentName = GetFragmentName(fragmentIndex);
            string message = $"Frammento dell'Equilibrio Raccolto: {fragmentName}!";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 5.0f,
                RemainingTime = 5.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 3000 + fragmentIndex
            });
            
            commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
            {
                QueuePosition = 1 // Coda dopo eventuale messaggio di sblocco mondo
            });
        }
        
        /// <summary>
        /// Crea un messaggio UI per lo sblocco di un personaggio
        /// </summary>
        private void CreateCharacterUnlockMessage(EntityCommandBuffer commandBuffer, int characterIndex, ref SystemState state)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string characterName = GetCharacterName(characterIndex);
            string message = $"Nuovo Eroe Sbloccato: {characterName}!";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 5.0f,
                RemainingTime = 5.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 4000 + characterIndex
            });
            
            commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
            {
                QueuePosition = 2 // Coda dopo altri messaggi
            });
        }
        
        /// <summary>
        /// Ottiene il numero totale di stelle disponibili in un mondo
        /// </summary>
        private int GetTotalStarsForWorld(int worldIndex)
        {
            // Numero fisso di livelli per mondo * 3 stelle per livello
            return worldIndex == 0 ? 15 : 27; // 5 livelli tutorial, 9 livelli per mondo normale
        }
        
        /// <summary>
        /// Ottiene il nome di un frammento in base all'indice
        /// </summary>
        private string GetFragmentName(int fragmentIndex)
        {
            string[] fragmentNames = new string[]
            {
                "Frammento Urbano",
                "Frammento della Natura",
                "Frammento Glaciale",
                "Frammento Igneo",
                "Frammento Abissale",
                "Frammento Digitale"
            };
            
            if (fragmentIndex >= 0 && fragmentIndex < fragmentNames.Length)
                return fragmentNames[fragmentIndex];
                
            return "Frammento Sconosciuto";
        }
        
        /// <summary>
        /// Ottiene il nome di un personaggio in base all'indice
        /// </summary>
        private string GetCharacterName(int characterIndex)
        {
            string[] characterNames = new string[]
            {
                "Alex",
                "Maya",
                "Kai",
                "Ember",
                "Marina",
                "Neo"
            };
            
            if (characterIndex >= 0 && characterIndex < characterNames.Length)
                return characterNames[characterIndex];
                
            return "Eroe Sconosciuto";
        }
    }
}