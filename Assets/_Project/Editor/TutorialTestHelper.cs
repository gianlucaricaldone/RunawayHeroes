using UnityEngine;
using UnityEditor;
using RunawayHeroes.Runtime.Levels;
using RunawayHeroes.ECS.Components.World;
using Unity.Entities;
using RunawayHeroes.Utilities.ECSCompatibility;
using NUnit.Framework.Constraints;
using RunawayHeroes.ECS.Components.Gameplay;

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
            if (!difficultyQuery.HasAnyEntities())
            {
                var configEntity = entityManager.CreateEntity(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
                entityManager.SetComponentData(configEntity, WorldDifficultyConfigComponent.CreateDefault());
            }
            
            // Ottieni il sistema di generazione livelli
            var levelGenSystem = world.GetOrCreateSystemManaged<RunawayHeroes.ECS.Systems.World.LevelGenerationSystem>();
            if (levelGenSystem == null)
            {
                Debug.LogError("LevelGenerationSystem not found!");
                return;
            }
            
            // Genera il livello tutorial
            int actualSeed = _tutorialInitializer.seed == 0 ? 
                             Random.Range(1, 99999) : _tutorialInitializer.seed;
                             
            var tutorialLevelEntity = levelGenSystem.CreateRunnerLevelRequest(
                _tutorialInitializer.tutorialTheme, 
                _tutorialInitializer.tutorialLength,
                actualSeed,
                true // tutorial flag
            );
            
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
            if (tutorialQuery.HasAnyEntities())
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