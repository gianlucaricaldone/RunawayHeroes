using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Systems.World;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Componente responsabile per l'inizializzazione del livello tutorial
    /// </summary>
    public class TutorialLevelInitializer : MonoBehaviour
    {
        [Header("Configurazione Livelli Tutorial")]
        [Tooltip("Indice del livello tutorial da generare (0-3)")]
        [Range(0, 3)]
        public int tutorialLevelIndex = 0;
        
        [Tooltip("Sequenza completa dei tutorial")]
        public TutorialLevelSequence[] tutorialSequence = new TutorialLevelSequence[] 
        {
            new TutorialLevelSequence { 
                description = "Tutorial 1: Comandi Base", 
                theme = WorldTheme.City, 
                length = 300, 
                difficulty = 1 
            },
            new TutorialLevelSequence { 
                description = "Tutorial 2: Ostacoli Avanzati", 
                theme = WorldTheme.City, 
                length = 400, 
                difficulty = 2 
            },
            new TutorialLevelSequence { 
                description = "Tutorial 3: Nemici", 
                theme = WorldTheme.City, 
                length = 500, 
                difficulty = 3 
            },
            new TutorialLevelSequence { 
                description = "Tutorial 4: Abilità Speciali", 
                theme = WorldTheme.Virtual, 
                length = 600, 
                difficulty = 4 
            }
        };
        
        [Tooltip("Seed per la generazione casuale (0 = random)")]
        public int seed = 0;
        
        [Header("Configurazione Tutorial")]
        [Tooltip("Posizione di partenza del giocatore")]
        public Vector3 playerStartPosition = new Vector3(0, 0, 0);
        
        [Tooltip("Distanza iniziale tra checkpoints")]
        [Range(50, 200)]
        public int checkpointDistance = 100;
        
        private EntityManager _entityManager;
        private LevelGenerationSystem _levelGenerationSystem;
        private Entity _tutorialLevelEntity;
        
        private void Awake()
        {
            // Ottieni accesso al mondo ECS
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;
            
            // Ottieni il sistema di generazione livelli
            _levelGenerationSystem = world.GetOrCreateSystemManaged<LevelGenerationSystem>();
            
            // Inizializza la configurazione di difficoltà se non esiste già
            InitializeDifficultyConfig();
        }
        
        private void Start()
        {
            // Genera il livello tutorial
            GenerateTutorialLevel();
        }
        
        /// <summary>
        /// Inizializza la configurazione di difficoltà globale
        /// </summary>
        private void InitializeDifficultyConfig()
        {
            // Controlla se esiste già una configurazione di difficoltà
            var difficultyQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
            if (!difficultyQuery.HasAnyEntities())
            {
                // Crea una nuova entità per la configurazione
                var configEntity = _entityManager.CreateEntity(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
                
                // Imposta la configurazione predefinita
                _entityManager.SetComponentData(configEntity, WorldDifficultyConfigComponent.CreateDefault());
            }
        }
        
        /// <summary>
        /// Genera il livello tutorial in base all'indice selezionato
        /// </summary>
        private void GenerateTutorialLevel()
        {
            // Assicurati che l'indice sia valido
            if (tutorialLevelIndex < 0 || tutorialLevelIndex >= tutorialSequence.Length)
            {
                Debug.LogError($"Invalid tutorial level index: {tutorialLevelIndex}. Must be between 0 and {tutorialSequence.Length - 1}");
                tutorialLevelIndex = 0;
            }
            
            // Ottieni la configurazione del tutorial corrente
            TutorialLevelSequence currentTutorial = tutorialSequence[tutorialLevelIndex];
            
            // Usa un seed casuale se non specificato
            int actualSeed = seed == 0 ? UnityEngine.Random.Range(1, 99999) : seed;
            
            // Crea la richiesta di generazione livello runner con flag tutorial = true
            _tutorialLevelEntity = _levelGenerationSystem.CreateRunnerLevelRequest(
                currentTutorial.theme, 
                currentTutorial.length, 
                actualSeed, 
                true);
                
            Debug.Log($"Tutorial level {tutorialLevelIndex}: \"{currentTutorial.description}\" generated with seed {actualSeed}");
            
            // Aggiungi un tag tutorial all'entità del livello per poterla identificare
            _entityManager.AddComponentData(_tutorialLevelEntity, new TutorialLevelTag { 
                TutorialIndex = tutorialLevelIndex 
            });
            
            // Aggiungi informazioni sul livello tutorial corrente
            _entityManager.AddComponentData(_tutorialLevelEntity, new TutorialLevelInfoComponent {
                Description = currentTutorial.description,
                Difficulty = currentTutorial.difficulty,
                IsLastTutorial = (tutorialLevelIndex == tutorialSequence.Length - 1)
            });
            
            // Posiziona il giocatore all'inizio del tutorial
            SetupPlayer();
            
            // Configura gli scenari di insegnamento specifici per questo livello tutorial
            SetupTutorialScenarios(currentTutorial);
        }
        
        /// <summary>
        /// Configura gli scenari di insegnamento specifici per questo livello tutorial
        /// </summary>
        private void SetupTutorialScenarios(TutorialLevelSequence tutorial)
        {
            // Se non ci sono scenari configurati, esci
            if (tutorial.scenarios == null || tutorial.scenarios.Length == 0)
                return;
                
            // Crea un buffer per gli scenari
            var scenarioBuffer = _entityManager.AddBuffer<TutorialScenarioBuffer>(_tutorialLevelEntity);
            
            // Aggiungi tutti gli scenari al buffer
            foreach (var scenario in tutorial.scenarios)
            {
                // Crea il buffer principale per lo scenario
                var scenarioEntity = _entityManager.CreateEntity();
                
                // Aggiungi le informazioni di base dello scenario
                var scenarioData = new TutorialScenarioBuffer {
                    Name = scenario.name,
                    DistanceFromStart = scenario.distanceFromStart,
                    InstructionMessage = scenario.instructionMessage,
                    MessageDuration = scenario.messageDuration,
                    RandomPlacement = scenario.randomPlacement,
                    ObstacleSpacing = scenario.obstacleSpacing > 0 ? scenario.obstacleSpacing : 5.0f, // Default 5 metri
                    Triggered = false
                };
                
                // Aggiungi il buffer al livello tutorial
                scenarioBuffer.Add(scenarioData);
                
                // Aggiungi un buffer separato per gli ostacoli
                if (scenario.obstacles != null && scenario.obstacles.Length > 0)
                {
                    var obstacleBuffer = _entityManager.AddBuffer<TutorialObstacleBuffer>(scenarioEntity);
                    
                    // Calcola l'offset iniziale per ogni tipo di ostacolo
                    float currentOffset = 0;
                    
                    // Aggiungi tutti i tipi di ostacoli
                    foreach (var obstacle in scenario.obstacles)
                    {
                        obstacleBuffer.Add(new TutorialObstacleBuffer {
                            ObstacleCode = obstacle.obstacleCode,
                            Count = obstacle.count,
                            Placement = (byte)obstacle.placement,
                            RandomizeHeight = obstacle.randomizeHeight,
                            RandomizeScale = obstacle.randomizeScale,
                            StartOffset = currentOffset
                        });
                        
                        // Incrementa l'offset se gli ostacoli non sono posizionati casualmente
                        if (!scenario.randomPlacement)
                        {
                            currentOffset += obstacle.count * scenario.obstacleSpacing;
                        }
                    }
                    
                    // Collega lo scenario entity al livello tutorial
                    _entityManager.AddComponentData(scenarioEntity, new ScenarioReference { 
                        TutorialLevelEntity = _tutorialLevelEntity,
                        ScenarioIndex = scenarioBuffer.Length - 1 // L'ultimo aggiunto
                    });
                }
            }
            
            Debug.Log($"Added {tutorial.scenarios.Length} teaching scenarios to tutorial level {tutorialLevelIndex}");
        }
        
        /// <summary>
        /// Configura il giocatore all'inizio del tutorial
        /// </summary>
        private void SetupPlayer()
        {
            // Trova il giocatore nella scena (in una implementazione reale, avrai un sistema più robusto)
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Posiziona il giocatore all'inizio del tutorial
                player.transform.position = playerStartPosition;
                
                // Potrebbe essere necessario configurare altri aspetti del giocatore
                // come velocità iniziale ridotta, invulnerabilità temporanea, ecc.
                
                Debug.Log($"Player positioned at {playerStartPosition}");
            }
            else
            {
                Debug.LogWarning("Player not found in scene. Cannot set up tutorial starting position.");
            }
        }
        
        private void OnDestroy()
        {
            // Pulizia quando si esce dal tutorial (se necessario)
            if (_entityManager.Exists(_tutorialLevelEntity))
            {
                _entityManager.DestroyEntity(_tutorialLevelEntity);
            }
        }
    }
    
    /// <summary>
    /// Tag per identificare i livelli tutorial
    /// </summary>
    public struct TutorialLevelTag : IComponentData { }
}