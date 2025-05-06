// Path: TutorialLevelInitializer.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Systems.Gameplay;
using RunawayHeroes.Gameplay;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Inizializzatore per i livelli tutorial, gestisce la configurazione degli scenari
    /// </summary>
    public class TutorialLevelInitializer : MonoBehaviour
    {
        [Header("Configurazione Tutorial")]
        [Tooltip("Livelli tutorial in sequenza")]
        public TutorialLevelData[] tutorialSequence;
        
        [Header("Debug")]
        [Tooltip("Se true, mostra gizmo per visualizzare gli scenari nel editor")]
        public bool showDebugGizmos = true;
        
        [Header("Stato Tutorial")]
        [Tooltip("Indice del livello tutorial attuale nella sequenza")]
        public int tutorialLevelIndex = 0;
        
        [Tooltip("Posizione iniziale del giocatore")]
        public Vector3 playerStartPosition = Vector3.zero;
        
        private EntityManager _entityManager;
        private float _currentLevelLength;
        private Vector3 _startPosition;
        
        private void Awake()
        {
            // Ottieni riferimento all'EntityManager
            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            _startPosition = transform.position;
        }
        
        private void Start()
        {
            InitializeTutorialLevel();
        }
        
        /// <summary>
        /// Inizializza un livello tutorial con la sequenza definita
        /// </summary>
        public void InitializeTutorialLevel()
        {
            if (tutorialSequence == null || tutorialSequence.Length == 0)
            {
                Debug.LogWarning("Nessuna sequenza tutorial definita!");
                return;
            }
            
            // Verifica che l'indice del livello tutorial sia valido
            if (tutorialLevelIndex < 0 || tutorialLevelIndex >= tutorialSequence.Length)
            {
                Debug.LogWarning($"Indice tutorial non valido: {tutorialLevelIndex}. Impostato a 0.");
                tutorialLevelIndex = 0;
            }
            
            // Imposta la posizione iniziale su _startPosition se non specificata
            if (playerStartPosition == Vector3.zero)
            {
                playerStartPosition = _startPosition;
            }
            
            // Crea un'entità singleton per marcare questo come livello tutorial
            Entity tutorialLevelEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(tutorialLevelEntity, new TutorialLevelTag 
            {
                CurrentSequence = tutorialLevelIndex,
                Completed = false
            });
            
            // Configura il tutorial della sequenza corrente
            SetupTutorialScenarios(tutorialSequence[tutorialLevelIndex]);
            
            Debug.Log($"Tutorial inizializzato: {tutorialSequence[tutorialLevelIndex].description}");
        }
        
        /// <summary>
        /// Avanza alla prossima sequenza tutorial
        /// </summary>
        public void AdvanceToNextTutorialSequence()
        {
            // Trova l'entità tutorial
            var tutorialQuery = _entityManager.CreateEntityQuery(ComponentType.ReadWrite<TutorialLevelTag>());
            
            if (tutorialQuery.CalculateEntityCount() == 0)
            {
                Debug.LogWarning("Nessun tutorial attivo trovato!");
                return;
            }
            
            Entity tutorialEntity = tutorialQuery.GetSingletonEntity();
            var tutorialTag = _entityManager.GetComponentData<TutorialLevelTag>(tutorialEntity);
            
            int nextSequence = tutorialTag.CurrentSequence + 1;
            tutorialLevelIndex = nextSequence; // Aggiorna anche l'indice del livello tutorial
            
            if (nextSequence >= tutorialSequence.Length)
            {
                // Completato tutti i tutorial
                tutorialTag.Completed = true;
                _entityManager.SetComponentData(tutorialEntity, tutorialTag);
                Debug.Log("Tutte le sequenze tutorial completate!");
                return;
            }
            
            // Avanza alla prossima sequenza
            tutorialTag.CurrentSequence = nextSequence;
            _entityManager.SetComponentData(tutorialEntity, tutorialTag);
            
            // Pulisci scenari precedenti
            CleanupPreviousScenarios();
            
            // Configura la nuova sequenza
            SetupTutorialScenarios(tutorialSequence[nextSequence]);
            
            Debug.Log($"Avanzato alla sequenza tutorial: {tutorialSequence[nextSequence].description}");
        }
        
        /// <summary>
        /// Configura gli scenari tutorial per una sequenza
        /// </summary>
        private void SetupTutorialScenarios(TutorialLevelData tutorial)
        {
            if (tutorial.scenarios == null || tutorial.scenarios.Length == 0)
            {
                Debug.LogWarning("Nessuno scenario definito per questo tutorial!");
                return;
            }
            
            _currentLevelLength = tutorial.length;
            
            // Crea entità e buffer per ogni scenario
            for (int i = 0; i < tutorial.scenarios.Length; i++)
            {
                var scenario = tutorial.scenarios[i];
                
                // Crea l'entità per lo scenario
                Entity scenarioEntity = _entityManager.CreateEntity();
                
                // Aggiungi il componente principale dello scenario
                _entityManager.AddComponentData(scenarioEntity, new TutorialScenarioComponent
                {
                    Name = new FixedString64Bytes(scenario.name),
                    DistanceFromStart = scenario.distanceFromStart,
                    InstructionMessage = new FixedString128Bytes(scenario.instructionMessage),
                    MessageDuration = scenario.messageDuration,
                    RandomPlacement = scenario.randomPlacement,
                    ObstacleSpacing = scenario.obstacleSpacing
                });
                
                // Crea il buffer per gli ostacoli
                var obstacleBuffer = _entityManager.AddBuffer<TutorialObstacleBuffer>(scenarioEntity);
                
                // Aggiungi tutti i tipi di ostacoli al buffer
                if (scenario.obstacles != null && scenario.obstacles.Length > 0)
                {
                    foreach (var obstacle in scenario.obstacles)
                    {
                        obstacleBuffer.Add(new TutorialObstacleBuffer
                        {
                            ObstacleCode = new FixedString32Bytes(obstacle.obstacleCode),
                            Count = obstacle.count,
                            Placement = (byte)obstacle.placement,
                            RandomizeHeight = obstacle.randomizeHeight,
                            HeightRange = new float2(obstacle.randomizeHeight ? 
                                                    new Vector2(0.5f, 1.5f).x : 1.0f, 
                                                    obstacle.randomizeHeight ? 
                                                    new Vector2(0.5f, 1.5f).y : 1.0f),
                            RandomizeScale = obstacle.randomizeScale,
                            ScaleRange = new float2(obstacle.randomizeScale ? 
                                                   new Vector2(0.8f, 1.2f).x : 1.0f,
                                                   obstacle.randomizeScale ? 
                                                   new Vector2(0.8f, 1.2f).y : 1.0f),
                            StartOffset = 0f // Using default value as field is not present in TutorialLevelData
                        });
                    }
                }
                else
                {
                    // Aggiungi un ostacolo predefinito se non specificato
                    var defaultSetup = ObstacleSetup.CreateDefault();
                    obstacleBuffer.Add(new TutorialObstacleBuffer
                    {
                        ObstacleCode = new FixedString32Bytes(defaultSetup.obstacleCode),
                        Count = defaultSetup.count,
                        Placement = (byte)defaultSetup.placement,
                        RandomizeHeight = defaultSetup.randomizeHeight,
                        HeightRange = new float2(defaultSetup.heightRange.x, defaultSetup.heightRange.y),
                        RandomizeScale = defaultSetup.randomizeScale,
                        ScaleRange = new float2(defaultSetup.scaleRange.x, defaultSetup.scaleRange.y),
                        StartOffset = defaultSetup.startOffset
                    });
                }
            }
        }
        
        /// <summary>
        /// Pulisce gli scenari tutorial precedenti
        /// </summary>
        private void CleanupPreviousScenarios()
        {
            // Trova tutte le entità scenario
            var scenarioQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<TutorialScenarioComponent>()
            );
            
            var scenarios = scenarioQuery.ToEntityArray(Allocator.Temp);
            
            // Rimuovi tutte le entità scenario
            foreach (var entity in scenarios)
            {
                _entityManager.DestroyEntity(entity);
            }
            
            scenarios.Dispose();
            
            // Trova tutti gli ostacoli del tutorial e rimuovili
            var obstacleQuery = _entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ObstacleTag>(),
                ComponentType.ReadOnly<ObstacleTypeComponent>()
            );
            
            _entityManager.DestroyEntity(obstacleQuery);
        }
        
        /// <summary>
        /// Visualizza gizmo per debug nel mondo
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || tutorialSequence == null)
                return;
                
            Vector3 startPos = transform.position;
            
            // Per ogni tutorial nella sequenza
            for (int t = 0; t < tutorialSequence.Length; t++)
            {
                var tutorial = tutorialSequence[t];
                
                if (tutorial.scenarios == null)
                    continue;
                    
                // Per ogni scenario nel tutorial
                for (int i = 0; i < tutorial.scenarios.Length; i++)
                {
                    var scenario = tutorial.scenarios[i];
                    
                    // Colore diverso per ogni scenario
                    Color scenarioColor = new Color(
                        0.2f + (i % 3) * 0.25f,
                        0.2f + ((i + 1) % 3) * 0.25f,
                        0.2f + ((i + 2) % 3) * 0.25f,
                        0.7f
                    );
                    
                    Gizmos.color = scenarioColor;
                    
                    // Disegna una linea orizzontale all'inizio dello scenario
                    Vector3 scenarioStart = startPos + new Vector3(0, 0, scenario.distanceFromStart);
                    Gizmos.DrawLine(
                        scenarioStart + new Vector3(-5, 0, 0),
                        scenarioStart + new Vector3(5, 0, 0)
                    );
                    
                    // Disegna un punto per ogni ostacolo
                    if (scenario.obstacles != null)
                    {
                        foreach (var obstacleSetup in scenario.obstacles)
                        {
                            // Usa un colore leggermente diverso per ogni tipo di ostacolo
                            Color obstacleColor = scenarioColor;
                            obstacleColor.r = Mathf.Clamp01(obstacleColor.r + 0.2f);
                            obstacleColor.g = Mathf.Clamp01(obstacleColor.g - 0.1f);
                            Gizmos.color = obstacleColor;
                            
                            float startZ = scenario.distanceFromStart + obstacleSetup.startOffset;
                            
                            // Posizionamento in base al tipo
                            for (int j = 0; j < obstacleSetup.count; j++)
                            {
                                float xPos = 0;
                                switch (obstacleSetup.placement)
                                {
                                    case ObstaclePlacement.Center:
                                        xPos = 0;
                                        break;
                                    case ObstaclePlacement.Left:
                                        xPos = -3;
                                        break;
                                    case ObstaclePlacement.Right:
                                        xPos = 3;
                                        break;
                                    case ObstaclePlacement.Pattern:
                                        // Distribuzione uniforme degli ostacoli in un pattern attraverso la corsia
                                        float pattern = (float)j / Mathf.Max(1, (float)(obstacleSetup.count - 1));
                                        xPos = Mathf.Lerp(-4.5f, 4.5f, pattern);
                                        break;
                                    case ObstaclePlacement.Random:
                                        // Per debug, mostra in posizioni equidistanti
                                        xPos = Mathf.Lerp(-4.5f, 4.5f, (float)j / Mathf.Max(1, (float)obstacleSetup.count));
                                        break;
                                }
                                
                                float zPos;
                                if (scenario.randomPlacement)
                                {
                                    // Per debug, mostra in posizioni equidistanti
                                    zPos = startZ + (j * (scenario.obstacleSpacing / 2));
                                }
                                else
                                {
                                    zPos = startZ + (j * scenario.obstacleSpacing);
                                }
                                
                                Vector3 obstaclePos = startPos + new Vector3(xPos, 0, zPos);
                                float radius = 0.5f;
                                
                                // Scala il raggio se randomizeScale è attivo
                                if (obstacleSetup.randomizeScale)
                                {
                                    radius = 0.5f * (obstacleSetup.scaleRange.x + obstacleSetup.scaleRange.y) / 2;
                                }
                                
                                Gizmos.DrawSphere(obstaclePos, radius);
                            }
                        }
                    }
                }
            }
        }
    }
    
    // Note: TutorialLevelSequence is now replaced by TutorialLevelData from TutorialLevelSequence.cs
    // This class is kept as a comment for reference, but should not be used.
    /* 
    /// <summary>
    /// Definisce una sequenza di livelli tutorial
    /// </summary>
    [Serializable]
    public class TutorialLevelSequence
    {
        [Tooltip("Descrizione della sequenza tutorial")]
        public string description;
        
        [Tooltip("Tema del mondo per questo tutorial")]
        public WorldTheme theme;
        
        [Tooltip("Lunghezza del livello in metri")]
        public float length = 500f;
        
        [Tooltip("Livello di difficoltà (1-10)")]
        [Range(1, 10)]
        public int difficulty = 1;
        
        [Tooltip("Scenari di insegnamento per questo tutorial")]
        public TutorialScenario[] scenarios;
    }
    */
    
    /// <summary>
    /// Temi di mondo disponibili
    /// </summary>
    public enum WorldTheme
    {
        Tutorial,
        City,
        Forest,
        Tundra,
        Volcano,
        Abyss,
        Virtual
    }
}