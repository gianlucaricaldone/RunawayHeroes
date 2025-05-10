using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
    /// Sistema che gestisce la progressione del giocatore attraverso i tutorial e i livelli.
    /// Monitora il completamento degli obiettivi e trigger eventi di progressione.
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [BurstCompile]
    public partial struct ProgressionSystem : ISystem
    {
        // Query per varie entità
        private EntityQuery _tutorialLevelQuery;
        private EntityQuery _tutorialProgressQuery;
        private EntityQuery _playerQuery;
        
        // Stato
        private bool _initializationComplete;
        
        /// <summary>
        /// Inizializza il sistema
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Inizializza variabili di stato
            _initializationComplete = false;
            
            // Inizializza query per tutorial
            _tutorialLevelQuery = state.GetEntityQuery(ComponentType.ReadWrite<TutorialLevelTag>());
            
            // Inizializza query per progressione
            _tutorialProgressQuery = state.GetEntityQuery(ComponentType.ReadWrite<TutorialProgressComponent>());
            
            // Inizializza query per giocatore
            _playerQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Richiedi singleton di EndSimulationEntityCommandBufferSystem per il CommandBuffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi aggiornamento solo se esiste un livello tutorial o progressione
            state.RequireAnyForUpdate(new EntityQuery[] { _tutorialLevelQuery, _tutorialProgressQuery });
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup se necessario
        }
        
        /// <summary>
        /// Aggiorna il sistema di progressione
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Inizializza se necessario
            if (!_initializationComplete)
            {
                InitializeProgressionData(ref state);
                _initializationComplete = true;
            }
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Controlla il completamento del tutorial
            if (!_tutorialLevelQuery.IsEmpty)
            {
                // Ottieni l'entità livello tutorial
                var tutorialEntity = _tutorialLevelQuery.GetSingletonEntity();
                var tutorialLevel = SystemAPI.GetComponent<TutorialLevelTag>(tutorialEntity);
                
                // Controlla se il tutorial è completato
                // Query entities with TutorialCompletedTag
                var completedTagQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RunawayHeroes.ECS.Components.Gameplay.TutorialCompletedTag>());
                using var completedEntities = completedTagQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in completedEntities)
                {
                    // Aggiorna lo stato di progressione
                    var progressEntity = GetOrCreateProgressionEntity(ref state);
                    var progress = SystemAPI.GetComponent<TutorialProgressComponent>(progressEntity);
                    
                    // Incrementa contatore tutorial completati
                    progress.CompletedTutorialCount++;
                    
                    // Aggiorna l'indice massimo sbloccato
                    if (tutorialLevel.CurrentSequence > progress.HighestUnlockedTutorial)
                    {
                        progress.HighestUnlockedTutorial = tutorialLevel.CurrentSequence;
                    }
                    
                    // Controlla se tutti i tutorial sono completati
                    var tutorialManager = UnityEngine.Object.FindFirstObjectByType<RunawayHeroes.Runtime.Levels.TutorialLevelInitializer>();
                    if (tutorialManager != null && tutorialLevel.CurrentSequence >= tutorialManager.tutorialSequence.Length - 1)
                    {
                        progress.TutorialsCompleted = true;
                        Debug.Log("Tutti i tutorial sono stati completati!");
                        
                        // Genera un messaggio di congratulazioni
                        CreateCompletionMessage(commandBuffer, true);
                    }
                    
                    // Aggiorna il componente di progressione
                    SystemAPI.SetComponent(progressEntity, progress);
                    
                    // Genera evento di completamento tutorial
                    Entity completionEvent = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(completionEvent, new RunawayHeroes.ECS.Components.Gameplay.TutorialCompletionEvent
                    {
                        CompletedTutorialIndex = tutorialLevel.CurrentSequence,
                        AllTutorialsCompleted = progress.TutorialsCompleted,
                        NextTutorialToUnlock = progress.HighestUnlockedTutorial + 1,
                        CompletionTime = 0f // Valore di default
                    });
                    
                    // Genera messaggio UI per il completamento
                    CreateCompletionMessage(commandBuffer, false);
                    
                    // Rimuovi il tag di completamento
                    commandBuffer.DestroyEntity(entity);
                }
            }
            
            // Aggiorna i timer di scenario
            UpdateScenarioTimers(ref state);
            
            // Verifica il raggiungimento degli obiettivi tutorial
            CheckTutorialObjectives(ref state);
        }
        
        /// <summary>
        /// Verifica il raggiungimento degli obiettivi tutorial
        /// </summary>
        private void CheckTutorialObjectives(ref SystemState state)
        {
            if (_playerQuery.IsEmpty || _tutorialLevelQuery.IsEmpty)
                return;
                
            var playerEntity = _playerQuery.GetSingletonEntity();
            var playerPosition = SystemAPI.GetComponent<TransformComponent>(playerEntity).Position;
            
            var tutorialEntity = _tutorialLevelQuery.GetSingletonEntity();
            var tutorialLevel = SystemAPI.GetComponent<TutorialLevelTag>(tutorialEntity);
            
            // Controlla se il giocatore ha raggiunto la fine del livello
            var tutorialManager = UnityEngine.Object.FindFirstObjectByType<RunawayHeroes.Runtime.Levels.TutorialLevelInitializer>();
            if (tutorialManager != null && tutorialManager.tutorialSequence.Length > tutorialLevel.CurrentSequence)
            {
                float levelLength = tutorialManager.tutorialSequence[tutorialLevel.CurrentSequence].length;
                
                // Se il giocatore ha superato la fine del livello
                if (playerPosition.z >= levelLength - 10)
                {
                    if (!tutorialLevel.Completed)
                    {
                        // Marca il tutorial come completato
                        tutorialLevel.Completed = true;
                        SystemAPI.SetComponent(tutorialEntity, tutorialLevel);
                        
                        // Crea un'entità con tag di completamento
                        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                        var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                        Entity completedEntity = commandBuffer.CreateEntity();
                        commandBuffer.AddComponent(completedEntity, new RunawayHeroes.ECS.Components.Gameplay.TutorialCompletedTag 
                        { 
                            CompletedTutorialIndex = tutorialLevel.CurrentSequence 
                        });
                        
                        Debug.Log($"Tutorial {tutorialLevel.CurrentSequence} completato!");
                    }
                }
            }
        }
        
        /// <summary>
        /// Aggiorna i timer per gli scenari attivati
        /// </summary>
        private void UpdateScenarioTimers(ref SystemState state)
        {
            // Query per entità con ScenarioActivationTag
            var scenarioQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<ScenarioActivationTag>());
            using var scenarioEntities = scenarioQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in scenarioEntities)
            {
                // Decrementa il timer
                var tag = state.EntityManager.GetComponentData<ScenarioActivationTag>(entity);
                tag.ActivationTime -= SystemAPI.Time.DeltaTime;
                state.EntityManager.SetComponentData(entity, tag);
                
                // Se il timer è scaduto, rimuovi il tag
                if (tag.ActivationTime <= 0)
                {
                    state.EntityManager.RemoveComponent<ScenarioActivationTag>(entity);
                }
            }
        }
        
        /// <summary>
        /// Inizializza i dati di progressione se non esistono già
        /// </summary>
        private void InitializeProgressionData(ref SystemState state)
        {
            // Crea un'entità per la progressione tutorial se non esiste già
            if (_tutorialProgressQuery.IsEmpty)
            {
                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, new TutorialProgressComponent
                {
                    CompletedTutorialCount = 0,
                    HighestUnlockedTutorial = 0,
                    TutorialsCompleted = false
                });
                
                Debug.Log("Creata nuova entità per la progressione tutorial");
            }
        }
        
        /// <summary>
        /// Ottiene o crea l'entità di progressione
        /// </summary>
        private Entity GetOrCreateProgressionEntity(ref SystemState state)
        {
            if (_tutorialProgressQuery.IsEmpty)
            {
                InitializeProgressionData(ref state);
            }
            
            return _tutorialProgressQuery.GetSingletonEntity();
        }
        
        /// <summary>
        /// Crea un messaggio UI per il completamento
        /// </summary>
        private void CreateCompletionMessage(EntityCommandBuffer commandBuffer, bool allCompleted)
        {
            Entity messageEntity = commandBuffer.CreateEntity();
            
            string message = allCompleted ? 
                "Congratulazioni! Hai completato tutti i tutorial!" : 
                "Tutorial completato! Avanza al prossimo!";
                
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
    }
    
    /// <summary>
    /// Sistema per la gestione degli obiettivi specifici
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    [UpdateAfter(typeof(ProgressionSystem))]
    [BurstCompile]
    public partial struct ObjectiveSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton di EndSimulationEntityCommandBufferSystem per il CommandBuffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup se necessario
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Processa eventi di completamento tutorial
            var completionEventQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<RunawayHeroes.ECS.Components.Gameplay.TutorialCompletionEvent>());
            using var completionEntities = completionEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var entity in completionEntities)
            {
                // Aggiorna lo stato del gioco in base al completamento
                var completionEvent = state.EntityManager.GetComponentData<RunawayHeroes.ECS.Components.Gameplay.TutorialCompletionEvent>(entity);
                if (completionEvent.AllTutorialsCompleted)
                {
                    Debug.Log("Tutti i tutorial completati! Sblocca il World 1...");
                    
                    // Qui aggiungere logica per sbloccare il primo mondo
                    // Ad esempio, salvare in PlayerPrefs o nel sistema di salvataggio
                    PlayerPrefs.SetInt("World1_Unlocked", 1);
                    PlayerPrefs.Save();
                }
                else
                {
                    // Sblocca il prossimo tutorial
                    int nextTutorial = completionEvent.CompletedTutorialIndex + 1;
                    Debug.Log($"Tutorial {completionEvent.CompletedTutorialIndex} completato! Sbloccato tutorial {nextTutorial}");
                    
                    // Salva lo stato di sblocco
                    PlayerPrefs.SetInt($"Tutorial_{nextTutorial}_Unlocked", 1);
                    PlayerPrefs.Save();
                    
                    // Avanza al prossimo tutorial automaticamente o mostra UI per procedere
                    var tutorialManager = UnityEngine.Object.FindFirstObjectByType<RunawayHeroes.Runtime.Levels.TutorialLevelInitializer>();
                    if (tutorialManager != null)
                    {
                        // Mostra un messaggio prima di passare al prossimo tutorial
                        UnityEngine.Object.FindFirstObjectByType<MonoBehaviour>().StartCoroutine(
                            DelayedTutorialAdvance(tutorialManager, 3.0f)
                        );
                    }
                }
                
                // Rimuovi l'evento dopo l'elaborazione
                state.EntityManager.DestroyEntity(entity);
            }
        }
        
        /// <summary>
        /// Coroutine per avanzare al prossimo tutorial dopo un ritardo
        /// </summary>
        private System.Collections.IEnumerator DelayedTutorialAdvance(
            RunawayHeroes.Runtime.Levels.TutorialLevelInitializer tutorialManager, float delay)
        {
            yield return new WaitForSeconds(delay);
            tutorialManager.AdvanceToNextTutorialSequence();
        }
    }
}