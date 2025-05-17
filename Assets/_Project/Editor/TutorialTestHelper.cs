using UnityEngine;
using UnityEditor;
using RunawayHeroes.Runtime.Levels;
using RunawayHeroes.ECS.Components.World;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.Utilities.ECSCompatibility;
using NUnit.Framework.Constraints;
using RunawayHeroes.ECS.Components.Gameplay;
using System; // Necessario per Exception

namespace RunawayHeroes.Editor
{
    /// <summary>
    /// Helper classe per testare il livello tutorial dall'editor
    /// </summary>
    public class TutorialTestHelper : EditorWindow
    {
        private TutorialLevelInitializer _tutorialInitializer;
        private bool _defaultSettings = true;
        private WorldTheme _selectedTheme = WorldTheme.City;
        private int _tutorialLength = 500;
        private int _seed = 0;
        private Vector3 _playerStartPos = Vector3.zero;
        
        [MenuItem("Runaway Heroes/Tutorial Test Tool")]
        public static void ShowWindow()
        {
            GetWindow<TutorialTestHelper>("Tutorial Tester");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Tutorial Test Helper", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Trova l'inizializzatore del tutorial nella scena
            if (_tutorialInitializer == null)
            {
                _tutorialInitializer = FindFirstObjectByType<TutorialLevelInitializer>();
            }
            
            if (_tutorialInitializer == null)
            {
                EditorGUILayout.HelpBox("No TutorialLevelInitializer found in the scene. Create one first.", MessageType.Warning);
                
                if (GUILayout.Button("Create Tutorial Manager"))
                {
                    CreateTutorialManager();
                }
                return;
            }
            
            // Impostazioni per il test
            _defaultSettings = EditorGUILayout.Toggle("Use Default Settings", _defaultSettings);
            
            if (!_defaultSettings)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Tutorial Settings", EditorStyles.boldLabel);
                
                _selectedTheme = (WorldTheme)EditorGUILayout.EnumPopup("Tutorial Theme", _selectedTheme);
                _tutorialLength = EditorGUILayout.IntSlider("Tutorial Length", _tutorialLength, 300, 1000);
                _seed = EditorGUILayout.IntField("Seed (0 = random)", _seed);
                _playerStartPos = EditorGUILayout.Vector3Field("Player Start Position", _playerStartPos);
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Generate Tutorial Level"))
            {
                if (_defaultSettings)
                {
                    // Usa le impostazioni predefinite dall'inizializzatore
                    Debug.Log("Generating tutorial level with default settings...");
                }
                else
                {
                    // Applica le impostazioni personalizzate
                    _tutorialInitializer.tutorialTheme = _selectedTheme;
                    _tutorialInitializer.tutorialLength = _tutorialLength;
                    _tutorialInitializer.seed = _seed;
                    _tutorialInitializer.playerStartPosition = _playerStartPos;
                    
                    Debug.Log($"Generating tutorial level with custom settings:\n" +
                              $"Theme: {_selectedTheme}\n" +
                              $"Length: {_tutorialLength}m\n" +
                              $"Seed: {_seed}\n" +
                              $"Player Pos: {_playerStartPos}");
                }
                
                // Avvia il tutorial (simulando l'Awake/Start)
                GenerateTutorialLevel();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Clear Tutorial Level"))
            {
                ClearTutorialLevel();
            }
        }
        
        /// <summary>
        /// Crea un GameObject con l'inizializzatore del tutorial
        /// </summary>
        private void CreateTutorialManager()
        {
            // Trova il tutorial manager esistente o ne crea uno nuovo
            GameObject tutorialManager = GameObject.Find("TutorialManager");
            
            if (tutorialManager == null)
            {
                tutorialManager = new GameObject("TutorialManager");
                Undo.RegisterCreatedObjectUndo(tutorialManager, "Create Tutorial Manager");
            }
            
            // Aggiungi i componenti necessari
            _tutorialInitializer = Undo.AddComponent<TutorialLevelInitializer>(tutorialManager);
            Undo.AddComponent<TutorialSceneSetup>(tutorialManager);
            
            // Imposta i valori predefiniti
            _tutorialInitializer.tutorialTheme = WorldTheme.City;
            _tutorialInitializer.tutorialLength = 500;
            _tutorialInitializer.seed = 0;
            _tutorialInitializer.playerStartPosition = Vector3.zero;
            
            Selection.activeGameObject = tutorialManager;
            
            Debug.Log("Tutorial Manager created!");
        }
        
        /// <summary>
        /// Genera un livello tutorial per il test
        /// </summary>
        private void GenerateTutorialLevel()
        {
            if (_tutorialInitializer == null)
                return;
                
            // Ottieni accesso al mondo ECS
            var world = RunawayWorldExtensions.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("ECS World not found!");
                return;
            }
            
            var entityManager = world.EntityManager;
            
            // Trova o crea la configurazione di difficoltà
            var difficultyQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
            if (difficultyQuery.IsEmpty) // Usa IsEmpty invece di HasAnyEntities() per DOTS 1.3.14
            {
                var configEntity = entityManager.CreateEntity(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
                entityManager.SetComponentData(configEntity, WorldDifficultyConfigComponent.CreateDefault());
            }
            
            // Ottieni il sistema di generazione livelli (per DOTS 1.3.14)
            // Nota: LevelGenerationSystem è una struct che implementa ISystem, quindi non possiamo
            // usare GetOrCreateSystemManaged che funziona solo con ComponentSystemBase
            var levelGenSystemHandle = world.GetExistingSystem<RunawayHeroes.ECS.Systems.World.LevelGenerationSystem>();
            if (levelGenSystemHandle == SystemHandle.Null)
            {
                levelGenSystemHandle = world.CreateSystem<RunawayHeroes.ECS.Systems.World.LevelGenerationSystem>();
            }
            
            if (levelGenSystemHandle == SystemHandle.Null)
            {
                Debug.LogError("LevelGenerationSystem not found and could not be created!");
                return;
            }
            
            // Genera il livello tutorial
            int actualSeed = _tutorialInitializer.seed == 0 ? 
                             UnityEngine.Random.Range(1, 99999) : _tutorialInitializer.seed;
                             
            // In DOTS 1.3.14, per i sistemi ISystem, dobbiamo usare un approccio alternativo
            // che non richiede codice unsafe
            var tutorialLevelEntity = default(Entity);
            
            try
            {
                // Invece di usare direttamente il sistema, creiamo manualmente l'entità di richiesta
                // con i componenti necessari per la generazione del livello
                tutorialLevelEntity = entityManager.CreateEntity();
                
                // Aggiungi il componente RunnerLevelConfigComponent con i parametri richiesti
                entityManager.AddComponentData(tutorialLevelEntity, new RunnerLevelConfigComponent
                {
                    LevelLength = _tutorialInitializer.tutorialLength,
                    MinSegments = _tutorialInitializer.tutorialLength / 50,  // Un segmento ogni 50 metri circa
                    MaxSegments = _tutorialInitializer.tutorialLength / 20,  // Un segmento ogni 20 metri circa
                    Seed = actualSeed,
                    StartDifficulty = 1,  // Bassa difficoltà per tutorial
                    EndDifficulty = 3,    // Difficoltà moderata alla fine
                    DifficultyRamp = 0.3f,
                    ObstacleDensity = 0.5f,
                    EnemyDensity = 0.3f,
                    CollectibleDensity = 0.8f, // Più collezionabili nel tutorial
                    PrimaryTheme = _tutorialInitializer.tutorialTheme,
                    ThemeBlendFactor = 0.1f, // Minore mescolamento nel tutorial
                    GenerateCheckpoints = true,
                    DynamicDifficulty = false, // Disabilita difficoltà dinamica nel tutorial
                    SegmentVarietyFactor = 0.4f // Minore varietà nel tutorial
                });
                
                // Aggiungi il componente transform
                entityManager.AddComponentData(tutorialLevelEntity, new LocalTransform
                {
                    Position = float3.zero,
                    Rotation = quaternion.identity,
                    Scale = 1.0f
                });
                
                Debug.Log($"Created tutorial level request entity with seed {actualSeed}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error creating tutorial level: {ex.Message}\n{ex.StackTrace}");
                return;
            }
            
            // Aggiungi un tag tutorial
            entityManager.AddComponentData(tutorialLevelEntity, new TutorialLevelTag());
            
            Debug.Log($"Tutorial level generated with seed {actualSeed}");
            
            // Avvia la simulazione se non è in corso
            EditorApplication.ExecuteMenuItem("Edit/Play");
        }
        
        /// <summary>
        /// Cancella il livello tutorial generato
        /// </summary>
        private void ClearTutorialLevel()
        {
            // Ottieni accesso al mondo ECS
            var world = RunawayWorldExtensions.DefaultGameObjectInjectionWorld;
            if (world == null)
            {
                Debug.LogError("ECS World not found!");
                return;
            }
            
            var entityManager = world.EntityManager;
            
            // Cerca le entità con TutorialLevelTag
            var tutorialQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<TutorialLevelTag>());
            if (!tutorialQuery.IsEmpty) // Usa !IsEmpty invece di HasAnyEntities() per DOTS 1.3.14
            {
                entityManager.DestroyEntity(tutorialQuery);
                Debug.Log("Tutorial level cleared!");
            }
            else
            {
                Debug.Log("No tutorial level found to clear.");
            }
        }
    }
}