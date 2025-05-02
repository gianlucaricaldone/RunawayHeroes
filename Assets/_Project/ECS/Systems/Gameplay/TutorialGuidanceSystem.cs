using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.World.Obstacles;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema responsabile della gestione delle meccaniche di tutorial,
    /// fornendo istruzioni al giocatore e assicurando un'esperienza guidata.
    /// </summary>
    [UpdateAfter(typeof(DifficultySystem))]
    public partial class TutorialGuidanceSystem : SystemBase
    {
        // Sezioni del tutorial
        private enum TutorialSection
        {
            Introduction,     // Introduzione ai controlli base
            BasicMovement,    // Movimento base (corsa)
            JumpingSection,   // Salti semplici
            SlidingSection,   // Scivolate sotto ostacoli
            SideStepSection,  // Passi laterali
            CombinedMoves,    // Combinazione di mosse
            Completion        // Completamento del tutorial
        }
        
        private TutorialSection _currentSection = TutorialSection.Introduction;
        private EntityQuery _playerQuery;
        private EntityQuery _tutorialQuery;
        private Entity _tutorialLevelEntity;
        private float _tutorialProgress = 0f;
        private float _timeInCurrentSection = 0f;
        private const float SECTION_TRANSITION_TIME = 5.0f; // Secondi tra sezioni
        
        // Riferimento per l'UI del tutorial (in un progetto reale questo sarebbe gestito da un sistema UI)
        private GameObject _tutorialUIObject;
        private UnityEngine.UI.Text _tutorialText;
        
        protected override void OnCreate()
        {
            // Crea query per il giocatore
            _playerQuery = GetEntityQuery(ComponentType.ReadOnly<PlayerTag>());
            
            // Crea query per il livello tutorial
            _tutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialLevelTag>());
            
            // Richiedi aggiornamento solo se c'è un livello tutorial attivo
            RequireForUpdate<TutorialLevelTag>();
        }

        protected override void OnStartRunning()
        {
            // Cerca l'entità del livello tutorial
            if (_tutorialQuery.HasAnyEntities())
            {
                _tutorialLevelEntity = _tutorialQuery.GetSingletonEntity();
                
                // Ottieni informazioni sul tutorial corrente
                var tutorialTag = GetComponent<TutorialLevelTag>(_tutorialLevelEntity);
                var tutorialInfo = GetComponent<TutorialLevelInfoComponent>(_tutorialLevelEntity);
                
                Debug.Log($"Starting Tutorial #{tutorialTag.TutorialIndex}: {tutorialInfo.Description}");
                
                // Inizializza l'UI del tutorial (in un progetto reale, questo sarebbe fatto attraverso un sistema UI)
                InitializeTutorialUI();
                
                // Avvia il tutorial
                StartTutorial(tutorialTag.TutorialIndex, tutorialInfo);
            }
        }
        
        protected override void OnUpdate()
        {
            // Aggiorna il tempo nella sezione corrente
            _timeInCurrentSection += Time.DeltaTime;
            
            // Ottieni la posizione del giocatore
            if (_playerQuery.HasAnyEntities())
            {
                Entity playerEntity = _playerQuery.GetSingletonEntity();
                LocalTransform playerTransform = GetComponent<LocalTransform>(playerEntity);
                
                // Aggiorna il progresso del tutorial in base alla posizione del giocatore
                _tutorialProgress = playerTransform.Position.z / 500f; // Assumiamo 500 metri di lunghezza tutorial
                
                // Gestisci la progressione del tutorial
                UpdateTutorialProgression(playerEntity, playerTransform);
            }
            
            // Aggiorna l'UI del tutorial (in un progetto reale, questo sarebbe fatto attraverso eventi o un sistema UI)
            UpdateTutorialUI();
        }
        
        /// <summary>
        /// Inizializza l'UI del tutorial
        /// </summary>
        private void InitializeTutorialUI()
        {
            // Questo è solo uno stub - in un progetto reale, questo dovrebbe cercare e configurare l'UI effettiva
            Debug.Log("Tutorial UI initialized");
            
            // Cerca l'oggetto UI tutorial (in un progetto reale sarebbe un riferimento)
            _tutorialUIObject = GameObject.Find("TutorialUI");
            
            if (_tutorialUIObject != null)
            {
                _tutorialText = _tutorialUIObject.GetComponentInChildren<UnityEngine.UI.Text>();
                _tutorialUIObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("Tutorial UI not found. Creating a temporary text display.");
                
                // Crea un oggetto UI temporaneo (solo per debug)
                _tutorialUIObject = new GameObject("TutorialUI");
                var canvas = _tutorialUIObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                var textObj = new GameObject("TutorialText");
                textObj.transform.SetParent(_tutorialUIObject.transform);
                _tutorialText = textObj.AddComponent<UnityEngine.UI.Text>();
                _tutorialText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _tutorialText.fontSize = 24;
                _tutorialText.alignment = TextAnchor.MiddleCenter;
                
                var rectTransform = textObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.8f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.9f);
                rectTransform.sizeDelta = new Vector2(600, 80);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        /// <summary>
        /// Avvia il tutorial in base all'indice
        /// </summary>
        private void StartTutorial(int tutorialIndex, TutorialLevelInfoComponent tutorialInfo)
        {
            _currentSection = TutorialSection.Introduction;
            _timeInCurrentSection = 0f;
            
            // Mostra il messaggio di avvio appropriato in base al tutorial
            switch (tutorialIndex)
            {
                case 0: // Tutorial 1: Comandi Base
                    ShowTutorialMessage("Tutorial 1: Comandi Base\nUsa le frecce per muoverti.");
                    break;
                    
                case 1: // Tutorial 2: Ostacoli Avanzati
                    ShowTutorialMessage("Tutorial 2: Ostacoli Avanzati\nImpariamo a superare ostacoli più complessi.");
                    break;
                    
                case 2: // Tutorial 3: Nemici
                    ShowTutorialMessage("Tutorial 3: Nemici\nImpariamo ad affrontare gli avversari.");
                    break;
                    
                case 3: // Tutorial 4: Abilità Speciali
                    ShowTutorialMessage("Tutorial 4: Abilità Speciali\nScopriamo i poteri unici del tuo personaggio.");
                    break;
                    
                default:
                    ShowTutorialMessage($"Tutorial {tutorialIndex + 1}: {tutorialInfo.Description}\nPreparati a imparare nuove abilità!");
                    break;
            }
            
            // Altre inizializzazioni...
            Debug.Log($"Tutorial {tutorialIndex} started with difficulty {tutorialInfo.Difficulty}");
            
            // Verifichiamo se ci sono scenari di insegnamento specifici
            CheckForTeachingScenarios();
        }
        
        /// <summary>
        /// Controlla se ci sono scenari di insegnamento per questo tutorial
        /// </summary>
        private void CheckForTeachingScenarios()
        {
            if (!EntityManager.HasBuffer<TutorialScenarioBuffer>(_tutorialLevelEntity))
                return;
                
            var scenarios = EntityManager.GetBuffer<TutorialScenarioBuffer>(_tutorialLevelEntity);
            Debug.Log($"Found {scenarios.Length} teaching scenarios for this tutorial");
        }
        
        /// <summary>
        /// Aggiorna l'UI del tutorial
        /// </summary>
        private void UpdateTutorialUI()
        {
            if (_tutorialText != null)
            {
                // Aggiungi un indicatore di progresso
                _tutorialText.text += $"\n\nProgresso: {Mathf.Floor(_tutorialProgress * 100)}%";
            }
        }
        
        /// <summary>
        /// Gestisce la progressione del tutorial in base alla posizione e alla sezione
        /// </summary>
        private void UpdateTutorialProgression(Entity playerEntity, LocalTransform playerTransform)
        {
            // Logica di progressione basata sul tempo e sulla posizione
            switch (_currentSection)
            {
                case TutorialSection.Introduction:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.1f)
                    {
                        AdvanceToSection(TutorialSection.BasicMovement);
                    }
                    break;
                    
                case TutorialSection.BasicMovement:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.2f)
                    {
                        AdvanceToSection(TutorialSection.JumpingSection);
                        
                        // Crea configurazioni per barriere basse da saltare
                        ObstacleSetup[] jumpObstacles = new ObstacleSetup[]
                        {
                            // Barriera bassa standard
                            new ObstacleSetup
                            {
                                Type = "U01",
                                Placement = PlacementStrategy.Center,
                                HeightOffset = 0f,
                                Scale = 1.0f,
                                RandomizeHeight = false,
                                RandomizeScale = false
                            }
                        };
                        
                        // Spawna gli ostacoli da saltare
                        SpawnTutorialObstaclesAdvanced(jumpObstacles, playerTransform.Position.z + 20f, 3);
                    }
                    break;
                    
                case TutorialSection.JumpingSection:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.4f)
                    {
                        AdvanceToSection(TutorialSection.SlidingSection);
                        
                        // Crea configurazioni per più tipi di ostacoli da scivolare sotto
                        ObstacleSetup[] slidingObstacles = new ObstacleSetup[]
                        {
                            // Barriera alta standard al centro
                            new ObstacleSetup
                            {
                                Type = "U02",
                                Placement = PlacementStrategy.Center,
                                HeightOffset = 0f,
                                Scale = 1.0f,
                                RandomizeHeight = false,
                                RandomizeScale = false
                            },
                            
                            // Barriera più alta a sinistra o destra con altezza variabile
                            new ObstacleSetup
                            {
                                Type = "U02",
                                Placement = PlacementStrategy.Random,
                                HeightOffset = 0.2f,
                                Scale = 1.2f,
                                RandomizeHeight = true,
                                MinHeightOffset = 0.1f,
                                MaxHeightOffset = 0.3f,
                                RandomizeScale = true,
                                MinScale = 1.1f,
                                MaxScale = 1.3f
                            }
                        };
                        
                        // Spawna gli ostacoli con configurazioni multiple
                        SpawnTutorialObstaclesAdvanced(slidingObstacles, playerTransform.Position.z + 20f, 3);
                    }
                    break;
                    
                case TutorialSection.SlidingSection:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.6f)
                    {
                        AdvanceToSection(TutorialSection.SideStepSection);
                        
                        // Crea configurazioni per ostacoli laterali
                        ObstacleSetup[] lateralObstacles = new ObstacleSetup[]
                        {
                            // Ostacolo sul lato sinistro
                            new ObstacleSetup
                            {
                                Type = "U04",
                                Placement = PlacementStrategy.Left,
                                HeightOffset = 0f,
                                Scale = 1.0f,
                                RandomizeHeight = false,
                                RandomizeScale = false
                            },
                            
                            // Ostacolo sul lato destro
                            new ObstacleSetup
                            {
                                Type = "U04",
                                Placement = PlacementStrategy.Right,
                                HeightOffset = 0f,
                                Scale = 1.0f,
                                RandomizeHeight = false,
                                RandomizeScale = false
                            }
                        };
                        
                        // Spawna gli ostacoli laterali alternati
                        SpawnTutorialObstaclesAdvanced(lateralObstacles, playerTransform.Position.z + 20f, 3);
                    }
                    break;
                    
                case TutorialSection.SideStepSection:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.8f)
                    {
                        AdvanceToSection(TutorialSection.CombinedMoves);
                        
                        // Sequenza di ostacoli con pattern
                        
                        // 1. Pattern di barriere basse distribuite
                        var lowBarriers = new ObstacleSetup[]
                        {
                            new ObstacleSetup
                            {
                                Type = "U01",
                                Placement = PlacementStrategy.Pattern, // Distribuite sulla larghezza
                                HeightOffset = 0f,
                                Scale = 0.8f,
                                RandomizeHeight = false,
                                RandomizeScale = true,
                                MinScale = 0.7f,
                                MaxScale = 0.9f
                            }
                        };
                        SpawnTutorialObstaclesAdvanced(lowBarriers, playerTransform.Position.z + 20f, 3);
                        
                        // 2. Barriera alta al centro con scala maggiore
                        var highBarrier = new ObstacleSetup[]
                        {
                            new ObstacleSetup
                            {
                                Type = "U02",
                                Placement = PlacementStrategy.Center,
                                HeightOffset = 0.1f,
                                Scale = 1.3f,
                                RandomizeHeight = false,
                                RandomizeScale = false
                            }
                        };
                        SpawnTutorialObstaclesAdvanced(highBarrier, playerTransform.Position.z + 30f, 1);
                        
                        // 3. Combinazione di ostacoli laterali con diversi tipi
                        var mixedObstacles = new ObstacleSetup[]
                        {
                            new ObstacleSetup
                            {
                                Type = "U01", // Barriera bassa
                                Placement = PlacementStrategy.Left,
                                HeightOffset = 0f,
                                Scale = 1.0f
                            },
                            new ObstacleSetup
                            {
                                Type = "U02", // Barriera alta
                                Placement = PlacementStrategy.Right,
                                HeightOffset = 0.1f,
                                Scale = 0.9f
                            },
                            new ObstacleSetup
                            {
                                Type = "U04", // Ostacolo laterale
                                Placement = PlacementStrategy.Center,
                                HeightOffset = 0f,
                                Scale = 1.0f,
                                RandomizeHeight = true,
                                MinHeightOffset = 0f,
                                MaxHeightOffset = 0.2f
                            }
                        };
                        SpawnTutorialObstaclesAdvanced(mixedObstacles, playerTransform.Position.z + 40f, 3);
                    }
                    break;
                    
                case TutorialSection.CombinedMoves:
                    if (_timeInCurrentSection > SECTION_TRANSITION_TIME || _tutorialProgress > 0.95f)
                    {
                        AdvanceToSection(TutorialSection.Completion);
                    }
                    break;
                    
                case TutorialSection.Completion:
                    // Gestisci il completamento del tutorial
                    if (_tutorialProgress >= 1.0f)
                    {
                        CompleteTutorial();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Avanza alla sezione specificata del tutorial
        /// </summary>
        private void AdvanceToSection(TutorialSection newSection)
        {
            _currentSection = newSection;
            _timeInCurrentSection = 0f;
            
            // Mostra il messaggio appropriato per la nuova sezione
            switch (newSection)
            {
                case TutorialSection.BasicMovement:
                    ShowTutorialMessage("Ottimo! Ora continua a correre verso la fine del percorso.");
                    break;
                    
                case TutorialSection.JumpingSection:
                    ShowTutorialMessage("Premi SPAZIO per saltare sopra le barriere basse.");
                    break;
                    
                case TutorialSection.SlidingSection:
                    ShowTutorialMessage("Premi GIÙ per scivolare sotto le barriere alte.");
                    break;
                    
                case TutorialSection.SideStepSection:
                    ShowTutorialMessage("Premi SINISTRA o DESTRA per evitare gli ostacoli laterali.");
                    break;
                    
                case TutorialSection.CombinedMoves:
                    ShowTutorialMessage("Ora prova a combinare i movimenti per superare ostacoli in sequenza.");
                    break;
                    
                case TutorialSection.Completion:
                    ShowTutorialMessage("Ottimo lavoro! Hai quasi completato il tutorial.");
                    break;
            }
            
            Debug.Log($"Advanced to tutorial section: {newSection}");
        }
        
        /// <summary>
        /// Mostra un messaggio nell'interfaccia del tutorial
        /// </summary>
        private void ShowTutorialMessage(string message)
        {
            if (_tutorialText != null)
            {
                _tutorialText.text = message;
            }
            
            Debug.Log($"Tutorial message: {message}");
        }
        
        /// <summary>
        /// Completa il tutorial
        /// </summary>
        private void CompleteTutorial()
        {
            ShowTutorialMessage("Congratulazioni! Hai completato il tutorial.\nOra sei pronto per l'avventura!");
            
            // Logica aggiuntiva per il completamento del tutorial
            Debug.Log("Tutorial completed!");
            
            // In un gioco reale, qui si potrebbe sbloccare il primo livello, mostrare una sequenza narrativa, ecc.
        }
        
        /// <summary>
        /// Definizione di configurazione per un singolo ostacolo
        /// </summary>
        private struct ObstacleSetup
        {
            // Tipo di ostacolo da generare
            public string Type;
            
            // Strategia di posizionamento
            public PlacementStrategy Placement;
            
            // Offset verticale (altezza)
            public float HeightOffset;
            
            // Scala dell'ostacolo (1.0f = dimensione normale)
            public float Scale;
            
            // Se l'altezza deve essere randomizzata in un range
            public bool RandomizeHeight;
            
            // Range min per randomizzazione altezza
            public float MinHeightOffset;
            
            // Range max per randomizzazione altezza
            public float MaxHeightOffset;
            
            // Se la scala deve essere randomizzata in un range
            public bool RandomizeScale;
            
            // Range min per randomizzazione scala
            public float MinScale;
            
            // Range max per randomizzazione scala
            public float MaxScale;
            
            /// <summary>
            /// Crea un setup standard per un tipo di ostacolo
            /// </summary>
            public static ObstacleSetup CreateDefault(string type)
            {
                return new ObstacleSetup
                {
                    Type = type,
                    Placement = PlacementStrategy.Center,
                    HeightOffset = 0f,
                    Scale = 1.0f,
                    RandomizeHeight = false,
                    MinHeightOffset = 0f,
                    MaxHeightOffset = 0f,
                    RandomizeScale = false,
                    MinScale = 1.0f,
                    MaxScale = 1.0f
                };
            }
        }
        
        /// <summary>
        /// Strategie di posizionamento degli ostacoli
        /// </summary>
        private enum PlacementStrategy
        {
            Center,     // Al centro della corsia
            Left,       // A sinistra
            Right,      // A destra
            Random,     // Posizione casuale tra sinistra e destra
            Pattern     // Pattern specifico (per più ostacoli)
        }
        
        /// <summary>
        /// Spawna ostacoli specifici per il tutorial con supporto per più tipi e strategie di posizionamento
        /// </summary>
        private void SpawnTutorialObstacles(string obstacleCode, float zPosition, int count)
        {
            // Crea una configurazione di default per l'ostacolo richiesto
            var setup = ObstacleSetup.CreateDefault(obstacleCode);
            
            // Genera gli ostacoli con la configurazione di default
            SpawnTutorialObstaclesAdvanced(new ObstacleSetup[] { setup }, zPosition, count);
        }
        
        /// <summary>
        /// Spawna ostacoli complessi per il tutorial con supporto per configurazioni multiple
        /// </summary>
        private void SpawnTutorialObstaclesAdvanced(ObstacleSetup[] setups, float zPosition, int count)
        {
            // Questa funzione dovrebbe essere gestita da un sistema dedicato al posizionamento di ostacoli
            // Ma per semplicità, qui implementiamo direttamente
            
            var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            var random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
            
            // Larghezza totale della corsia del giocatore
            const float LANE_WIDTH = 6.0f;
            
            // Per ogni ostacolo da generare
            for (int i = 0; i < count; i++)
            {
                // Per configurazioni multiple, seleziona una configurazione casuale
                ObstacleSetup setup = setups[random.NextInt(0, setups.Length)];
                
                // Calcola la posizione in base alla strategia di posizionamento
                float xPosition = 0f;
                
                switch (setup.Placement)
                {
                    case PlacementStrategy.Center:
                        xPosition = 0f;
                        break;
                        
                    case PlacementStrategy.Left:
                        xPosition = -LANE_WIDTH / 3f;
                        break;
                        
                    case PlacementStrategy.Right:
                        xPosition = LANE_WIDTH / 3f;
                        break;
                        
                    case PlacementStrategy.Random:
                        xPosition = random.NextFloat(-LANE_WIDTH / 2f, LANE_WIDTH / 2f);
                        break;
                        
                    case PlacementStrategy.Pattern:
                        // Per pattern distribuiti, posiziona in base all'indice
                        if (count > 1)
                        {
                            // Distribuisce in modo uniforme su tutta la larghezza
                            xPosition = math.lerp(-LANE_WIDTH / 2f, LANE_WIDTH / 2f, (float)i / (count - 1));
                        }
                        else
                        {
                            xPosition = 0f;
                        }
                        break;
                }
                
                // Determina l'altezza dell'ostacolo (randomizzata o fissa)
                float heightOffset = setup.HeightOffset;
                if (setup.RandomizeHeight)
                {
                    heightOffset = random.NextFloat(setup.MinHeightOffset, setup.MaxHeightOffset);
                }
                
                // Determina la scala dell'ostacolo (randomizzata o fissa)
                float scale = setup.Scale;
                if (setup.RandomizeScale)
                {
                    scale = random.NextFloat(setup.MinScale, setup.MaxScale);
                }
                
                // Posizione finale dell'ostacolo
                float3 position = new float3(
                    xPosition,
                    heightOffset,
                    zPosition + i * 5.0f  // Distanziati 5 metri
                );
                
                // Crea l'ostacolo usando la factory
                ObstacleFactory.CreateObstacle(
                    commandBuffer,
                    setup.Type,
                    position,
                    quaternion.identity,
                    scale
                );
            }
            
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
            
            Debug.Log($"Spawned {count} tutorial obstacles at z={zPosition} with {setups.Length} configurations");
        }
        
        protected override void OnDestroy()
        {
            // Pulizia delle risorse
            if (_tutorialUIObject != null)
            {
                GameObject.Destroy(_tutorialUIObject);
            }
        }
    }
}