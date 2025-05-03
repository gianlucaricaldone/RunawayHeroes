using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
    /// Sistema specializzato che gestisce la progressione del giocatore attraverso i tutorial.
    /// Si occupa dell'apprendimento delle meccaniche di base e dello sblocco sequenziale dei tutorial.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class TutorialProgressionSystem : SystemBase
    {
        // Query per varie entità
        private EntityQuery _tutorialLevelQuery;
        private EntityQuery _tutorialProgressQuery;
        private EntityQuery _playerQuery;
        private EntityQuery _playerProgressionQuery;
        
        // Buffer per i comandi
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Stato
        private bool _initializationComplete = false;
        
        // Riferimenti Tutorial
        private RunawayHeroes.Runtime.Levels.TutorialLevelInitializer _tutorialManager;
        private int _totalAvailableTutorials = 0;
        
        // Costanti
        private const float TUTORIAL_COMPLETION_THRESHOLD = 0.9f; // 90% del percorso per considerare completato
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        protected override void OnCreate()
        {
            // Inizializza query per tutorial
            _tutorialLevelQuery = GetEntityQuery(ComponentType.ReadWrite<TutorialLevelTag>());
            
            // Inizializza query per progressione
            _tutorialProgressQuery = GetEntityQuery(ComponentType.ReadWrite<TutorialProgressionComponent>());
            
            // Inizializza query per la progressione globale
            _playerProgressionQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            
            // Inizializza query per giocatore
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Ottieni riferimento al command buffer
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi aggiornamento solo se esiste un livello tutorial o progressione
            RequireAnyForUpdate(new EntityQuery[] { _tutorialLevelQuery, _tutorialProgressQuery });
        }
        
        /// <summary>
        /// Cerca e crea riferimenti a runtime
        /// </summary>
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            // Ottieni riferimento al gestore tutorial
            _tutorialManager = UnityEngine.Object.FindObjectOfType<RunawayHeroes.Runtime.Levels.TutorialLevelInitializer>();
            if (_tutorialManager != null && _tutorialManager.tutorialSequence != null)
            {
                _totalAvailableTutorials = _tutorialManager.tutorialSequence.Length;
            }
        }
        
        /// <summary>
        /// Aggiorna il sistema di progressione tutorial
        /// </summary>
        protected override void OnUpdate()
        {
            // Inizializza se necessario
            if (!_initializationComplete)
            {
                InitializeProgressionData();
                _initializationComplete = true;
            }
            
            // Ottieni il buffer per i comandi
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer();
            
            // Controlla il completamento del tutorial
            if (!_tutorialLevelQuery.IsEmpty)
            {
                // Ottieni l'entità livello tutorial
                var tutorialEntity = _tutorialLevelQuery.GetSingletonEntity();
                var tutorialLevel = GetComponent<TutorialLevelTag>(tutorialEntity);
                
                // Controlla se il tutorial è completato
                Entities
                    .WithoutBurst()
                    .WithStructuralChanges()
                    .WithAll<TutorialCompletedTag>()
                    .ForEach((Entity entity, in TutorialCompletedTag completedTag) =>
                    {
                        // Aggiorna lo stato di progressione tutorial
                        var progressEntity = GetOrCreateTutorialProgressionEntity();
                        var tutorialProgress = GetComponent<TutorialProgressionComponent>(progressEntity);
                        
                        // Incrementa contatore tutorial completati
                        tutorialProgress.CompletedTutorialCount++;
                        
                        // Aggiorna l'indice massimo sbloccato
                        if (tutorialLevel.TutorialIndex > tutorialProgress.HighestUnlockedTutorial)
                        {
                            tutorialProgress.HighestUnlockedTutorial = tutorialLevel.TutorialIndex;
                        }
                        
                        // Calcola tempo medio di completamento (per semplicità, usiamo un valore costante)
                        float completionTime = 120.0f; // 2 minuti per tutorial
                        tutorialProgress.AverageTutorialCompletionTime = 
                            (tutorialProgress.AverageTutorialCompletionTime * (tutorialProgress.CompletedTutorialCount - 1) + completionTime) 
                            / tutorialProgress.CompletedTutorialCount;
                        
                        // Aggiorna meccaniche apprese (ogni tutorial insegna almeno una meccanica)
                        tutorialProgress.MechanicsLearned++;
                        
                        // Controlla se tutti i tutorial sono stati completati
                        if (_tutorialManager != null && tutorialLevel.TutorialIndex >= _totalAvailableTutorials - 1)
                        {
                            tutorialProgress.AllTutorialsCompleted = true;
                            Debug.Log("Tutti i tutorial sono stati completati!");
                            
                            // Genera un messaggio di congratulazioni
                            CreateTutorialCompletionMessage(commandBuffer, true);
                        }
                        
                        // Aggiorna il componente di progressione tutorial
                        SetComponent(progressEntity, tutorialProgress);
                        
                        // Aggiorna anche la progressione globale
                        if (!_playerProgressionQuery.IsEmpty)
                        {
                            var playerProgressEntity = _playerProgressionQuery.GetSingletonEntity();
                            var playerProgress = GetComponent<PlayerProgressionComponent>(playerProgressEntity);
                            
                            playerProgress.CompletedTutorialCount = tutorialProgress.CompletedTutorialCount;
                            playerProgress.HighestUnlockedTutorial = tutorialProgress.HighestUnlockedTutorial;
                            playerProgress.TutorialsCompleted = tutorialProgress.AllTutorialsCompleted;
                            playerProgress.LastUpdatedTimestamp = DateTime.Now.Ticks;
                            
                            SetComponent(playerProgressEntity, playerProgress);
                        }
                        
                        // Genera evento di completamento tutorial
                        Entity completionEvent = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent(completionEvent, new TutorialCompletionEvent
                        {
                            CompletedTutorialIndex = tutorialLevel.TutorialIndex,
                            AllTutorialsCompleted = tutorialProgress.AllTutorialsCompleted,
                            NextTutorialToUnlock = tutorialProgress.HighestUnlockedTutorial + 1,
                            CompletionTime = completionTime
                        });
                        
                        // Genera evento di avanzamento progressione
                        GenerateProgressionAdvancementEvent(commandBuffer, 0, tutorialLevel.TutorialIndex, 0);
                        
                        // Genera messaggio UI per il completamento
                        CreateTutorialCompletionMessage(commandBuffer, false);
                        
                        // Rimuovi il tag di completamento
                        commandBuffer.DestroyEntity(entity);
                    }).Run();
                    
                // Verifica completamento tutorial basato su distanza percorsa
                CheckTutorialCompletionByDistance();
            }
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Verifica se il giocatore ha completato un tutorial percorrendo la distanza necessaria
        /// </summary>
        private void CheckTutorialCompletionByDistance()
        {
            if (_playerQuery.IsEmpty || _tutorialLevelQuery.IsEmpty)
                return;
                
            var playerEntity = _playerQuery.GetSingletonEntity();
            var playerPosition = GetComponent<TransformComponent>(playerEntity).Position;
            
            var tutorialEntity = _tutorialLevelQuery.GetSingletonEntity();
            var tutorialLevel = GetComponent<TutorialLevelTag>(tutorialEntity);
            
            // Se il tutorial è già stato completato, salta
            if (tutorialLevel.Completed)
                return;
                
            // Controlla se il giocatore ha raggiunto la fine del livello
            if (_tutorialManager != null && _tutorialManager.tutorialSequence.Length > tutorialLevel.TutorialIndex)
            {
                float levelLength = _tutorialManager.tutorialSequence[tutorialLevel.TutorialIndex].length;
                
                // Se il giocatore ha superato la soglia di completamento (default 90%)
                if (playerPosition.z >= levelLength * TUTORIAL_COMPLETION_THRESHOLD)
                {
                    // Marca il tutorial come completato
                    tutorialLevel.Completed = true;
                    EntityManager.SetComponentData(tutorialEntity, tutorialLevel);
                    
                    // Crea un'entità con tag di completamento
                    var commandBuffer = _commandBufferSystem.CreateCommandBuffer();
                    Entity completedEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(completedEntity, new TutorialCompletedTag 
                    { 
                        CompletedTutorialIndex = tutorialLevel.TutorialIndex 
                    });
                    
                    _commandBufferSystem.AddJobHandleForProducer(Dependency);
                    
                    Debug.Log($"Tutorial {tutorialLevel.TutorialIndex} completato!");
                }
            }
        }
        
        /// <summary>
        /// Inizializza i dati di progressione se non esistono già
        /// </summary>
        private void InitializeProgressionData()
        {
            // Crea un'entità per la progressione tutorial se non esiste già
            if (_tutorialProgressQuery.IsEmpty)
            {
                var entity = EntityManager.CreateEntity();
                EntityManager.AddComponentData(entity, new TutorialProgressionComponent
                {
                    CompletedTutorialCount = 0,
                    HighestUnlockedTutorial = 0,
                    AllTutorialsCompleted = false,
                    MechanicsLearned = 0,
                    TutorialRetryCount = 0,
                    AverageTutorialCompletionTime = 0
                });
                
                Debug.Log("Creata nuova entità per la progressione tutorial");
            }
            
            // Crea anche entità per progressione globale se non esiste
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
        }
        
        /// <summary>
        /// Ottiene o crea l'entità di progressione tutorial
        /// </summary>
        private Entity GetOrCreateTutorialProgressionEntity()
        {
            if (_tutorialProgressQuery.IsEmpty)
            {
                InitializeProgressionData();
            }
            
            return _tutorialProgressQuery.GetSingletonEntity();
        }
        
        /// <summary>
        /// Crea un messaggio UI per il completamento tutorial
        /// </summary>
        private void CreateTutorialCompletionMessage(EntityCommandBuffer commandBuffer, bool allCompleted)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string message = allCompleted ? 
                "Congratulazioni! Hai completato tutti i tutorial! Ora sei pronto per il vero viaggio!" : 
                "Tutorial completato! Hai appreso nuove meccaniche di gioco!";
                
            commandBuffer.AddComponent(messageEntity, new UIMessageComponent
            {
                Message = new FixedString128Bytes(message),
                Duration = 5.0f,
                RemainingTime = 5.0f,
                MessageType = 1, // Notifica (verde)
                IsPersistent = false,
                MessageId = 1000 + UnityEngine.Random.Range(0, 1000)
            });
            
            commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
            {
                QueuePosition = 0
            });
        }
        
        /// <summary>
        /// Genera un evento di avanzamento progressione
        /// </summary>
        private void GenerateProgressionAdvancementEvent(
            EntityCommandBuffer commandBuffer, 
            byte progressionType, 
            int primaryIndex, 
            int secondaryIndex)
        {
            Entity eventEntity = commandBuffer.CreateEntity();
            
            commandBuffer.AddComponent(eventEntity, new ProgressionAdvancementEvent
            {
                ProgressionType = progressionType,
                PrimaryIndex = primaryIndex,
                SecondaryIndex = secondaryIndex,
                ValueChanged = 1,
                IsSignificantAdvancement = true
            });
        }
    }
}