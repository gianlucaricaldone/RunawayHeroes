using UnityEngine;
using UnityEditor;
using RunawayHeroes.Runtime.Levels;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.Gameplay; // Contiene la definizione corretta di TutorialScenario

namespace RunawayHeroes.Editor
{
    /// <summary>
    /// Editor personalizzato per la configurazione della sequenza di tutorial
    /// </summary>
    [CustomEditor(typeof(TutorialLevelInitializer))]
    public class TutorialSequenceEditor : UnityEditor.Editor
    {
        private bool _showTutorialSequence = true;
        private bool[] _showTutorialDetails;
        private bool _showScenarios = true;
        
        public override void OnInspectorGUI()
        {
            var initializer = (TutorialLevelInitializer)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Tutorial Sequence Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Tutorial corrente
            initializer.tutorialLevelIndex = EditorGUILayout.IntSlider(
                new GUIContent("Current Tutorial", "Index of the current tutorial to generate"),
                initializer.tutorialLevelIndex, 0, 
                initializer.tutorialSequence != null ? initializer.tutorialSequence.Length - 1 : 0);
            
            EditorGUILayout.Space();
            
            // Tutorial Sequence
            _showTutorialSequence = EditorGUILayout.Foldout(_showTutorialSequence, "Tutorial Sequence", true);
            if (_showTutorialSequence)
            {
                EditorGUI.indentLevel++;
                
                // Create arrays for the tutorial details if needed
                if (_showTutorialDetails == null || _showTutorialDetails.Length != initializer.tutorialSequence.Length)
                {
                    _showTutorialDetails = new bool[initializer.tutorialSequence.Length];
                }
                
                // Sequence size
                int oldSize = initializer.tutorialSequence.Length;
                int newSize = EditorGUILayout.IntField("Sequence Size", oldSize);
                if (newSize != oldSize)
                {
                    // Resize the array
                    System.Array.Resize(ref initializer.tutorialSequence, newSize);
                    System.Array.Resize(ref _showTutorialDetails, newSize);
                    
                    // Initialize new elements
                    for (int i = oldSize; i < newSize; i++)
                    {
                        initializer.tutorialSequence[i] = new TutorialLevelData
                        {
                            description = $"Tutorial {i + 1}",
                            theme = WorldTheme.City,
                            length = 300 + i * 100,
                            difficulty = i + 1,
                            obstacleDensity = 0.5f,
                            enemyDensity = 0.3f,
                            scenarios = new TutorialScenario[0]
                        };
                    }
                }
                
                EditorGUILayout.Space();
                
                // Display each tutorial in the sequence
                for (int i = 0; i < initializer.tutorialSequence.Length; i++)
                {
                    TutorialLevelData tutorial = initializer.tutorialSequence[i];
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Header with expand/collapse
                    EditorGUILayout.BeginHorizontal();
                    _showTutorialDetails[i] = EditorGUILayout.Foldout(_showTutorialDetails[i], $"Tutorial {i + 1}: {tutorial.description}", true);
                    
                    // Quick edit buttons
                    GUI.enabled = i > 0;
                    if (GUILayout.Button("↑", GUILayout.Width(25)))
                    {
                        // Move up
                        TutorialLevelData temp = initializer.tutorialSequence[i - 1];
                        initializer.tutorialSequence[i - 1] = initializer.tutorialSequence[i];
                        initializer.tutorialSequence[i] = temp;
                        
                        bool tempShow = _showTutorialDetails[i - 1];
                        _showTutorialDetails[i - 1] = _showTutorialDetails[i];
                        _showTutorialDetails[i] = tempShow;
                        
                        // Update current index if needed
                        if (initializer.tutorialLevelIndex == i)
                            initializer.tutorialLevelIndex = i - 1;
                        else if (initializer.tutorialLevelIndex == i - 1)
                            initializer.tutorialLevelIndex = i;
                    }
                    GUI.enabled = i < initializer.tutorialSequence.Length - 1;
                    if (GUILayout.Button("↓", GUILayout.Width(25)))
                    {
                        // Move down
                        TutorialLevelData temp = initializer.tutorialSequence[i + 1];
                        initializer.tutorialSequence[i + 1] = initializer.tutorialSequence[i];
                        initializer.tutorialSequence[i] = temp;
                        
                        bool tempShow = _showTutorialDetails[i + 1];
                        _showTutorialDetails[i + 1] = _showTutorialDetails[i];
                        _showTutorialDetails[i] = tempShow;
                        
                        // Update current index if needed
                        if (initializer.tutorialLevelIndex == i)
                            initializer.tutorialLevelIndex = i + 1;
                        else if (initializer.tutorialLevelIndex == i + 1)
                            initializer.tutorialLevelIndex = i;
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Tutorial?", $"Are you sure you want to remove Tutorial {i + 1}?", "Remove", "Cancel"))
                        {
                            // Remove tutorial
                            var newSequence = new TutorialLevelData[initializer.tutorialSequence.Length - 1];
                            var newShowDetails = new bool[initializer.tutorialSequence.Length - 1];
                            
                            for (int j = 0; j < newSequence.Length; j++)
                            {
                                if (j < i)
                                {
                                    newSequence[j] = initializer.tutorialSequence[j];
                                    newShowDetails[j] = _showTutorialDetails[j];
                                }
                                else
                                {
                                    newSequence[j] = initializer.tutorialSequence[j + 1];
                                    newShowDetails[j] = _showTutorialDetails[j + 1];
                                }
                            }
                            
                            initializer.tutorialSequence = newSequence;
                            _showTutorialDetails = newShowDetails;
                            
                            // Update current index if needed
                            if (initializer.tutorialLevelIndex >= i && initializer.tutorialLevelIndex > 0)
                                initializer.tutorialLevelIndex--;
                            
                            return;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Details if expanded
                    if (_showTutorialDetails[i])
                    {
                        EditorGUI.indentLevel++;
                        
                        tutorial.description = EditorGUILayout.TextField("Description", tutorial.description);
                        tutorial.theme = (WorldTheme)EditorGUILayout.EnumPopup("Theme", tutorial.theme);
                        tutorial.length = EditorGUILayout.IntSlider("Length (m)", tutorial.length, 300, 1000);
                        tutorial.difficulty = EditorGUILayout.IntSlider("Difficulty", tutorial.difficulty, 1, 10);
                        tutorial.obstacleDensity = EditorGUILayout.Slider("Obstacle Density", tutorial.obstacleDensity, 0f, 1f);
                        tutorial.enemyDensity = EditorGUILayout.Slider("Enemy Density", tutorial.enemyDensity, 0f, 1f);
                        
                        // Teaching scenarios
                        EditorGUILayout.Space();
                        _showScenarios = EditorGUILayout.Foldout(_showScenarios, "Teaching Scenarios", true);
                        if (_showScenarios)
                        {
                            EditorGUI.indentLevel++;
                            
                            // Scenarios count
                            int oldScenarioCount = tutorial.scenarios != null ? tutorial.scenarios.Length : 0;
                            int newScenarioCount = EditorGUILayout.IntField("Scenario Count", oldScenarioCount);
                            if (newScenarioCount != oldScenarioCount)
                            {
                                // Resize the array
                                System.Array.Resize(ref tutorial.scenarios, newScenarioCount);
                                
                                // Initialize new elements
                                for (int j = oldScenarioCount; j < newScenarioCount; j++)
                                {
                                    tutorial.scenarios[j] = new TutorialScenario
                                    {
                                        name = $"Scenario {j + 1}",
                                        distanceFromStart = j * 50f,
                                        // Inizializza l'array obstacles invece di usare obstacleCode e obstacleCount
                                        obstacles = new ObstacleSetup[] 
                                        {
                                            new ObstacleSetup
                                            {
                                                obstacleCode = "U01",
                                                count = 3,
                                                placement = ObstaclePlacement.Center,
                                                randomizeHeight = false,
                                                randomizeScale = false
                                            }
                                        },
                                        instructionMessage = "New instruction",
                                        messageDuration = 5.0f,
                                        randomPlacement = false,
                                        obstacleSpacing = 8f
                                    };
                                }
                            }
                            
                            // Display scenarios
                            if (tutorial.scenarios != null)
                            {
                                for (int j = 0; j < tutorial.scenarios.Length; j++)
                                {
                                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                    
                                    TutorialScenario scenario = tutorial.scenarios[j];
                                    
                                    EditorGUILayout.BeginHorizontal();
                                    EditorGUILayout.LabelField($"Scenario {j + 1}: {scenario.name}");
                                    
                                    if (GUILayout.Button("×", GUILayout.Width(25)))
                                    {
                                        if (EditorUtility.DisplayDialog("Remove Scenario?", $"Are you sure you want to remove this scenario?", "Remove", "Cancel"))
                                        {
                                            // Remove scenario
                                            var newScenarios = new TutorialScenario[tutorial.scenarios.Length - 1];
                                            
                                            for (int k = 0; k < newScenarios.Length; k++)
                                            {
                                                if (k < j)
                                                    newScenarios[k] = tutorial.scenarios[k];
                                                else
                                                    newScenarios[k] = tutorial.scenarios[k + 1];
                                            }
                                            
                                            tutorial.scenarios = newScenarios;
                                            break;
                                        }
                                    }
                                    EditorGUILayout.EndHorizontal();
                                    
                                    scenario.name = EditorGUILayout.TextField("Name", scenario.name);
                                    scenario.distanceFromStart = EditorGUILayout.FloatField("Distance (m)", scenario.distanceFromStart);
                                    scenario.instructionMessage = EditorGUILayout.TextField("Instruction", scenario.instructionMessage);
                                    scenario.messageDuration = EditorGUILayout.FloatField("Duration (s)", scenario.messageDuration);
                                    scenario.randomPlacement = EditorGUILayout.Toggle("Random Placement", scenario.randomPlacement);
                                    scenario.obstacleSpacing = EditorGUILayout.FloatField("Obstacle Spacing", scenario.obstacleSpacing);
                                    
                                    // Obstacles configuration
                                    EditorGUILayout.Space();
                                    GUILayout.Label("Obstacles", EditorStyles.boldLabel);
                                    
                                    int oldObstacleCount = scenario.obstacles != null ? scenario.obstacles.Length : 0;
                                    int newObstacleCount = EditorGUILayout.IntField("Obstacle Types", oldObstacleCount);
                                    
                                    if (newObstacleCount != oldObstacleCount)
                                    {
                                        System.Array.Resize(ref scenario.obstacles, newObstacleCount);
                                        
                                        // Initialize new elements
                                        for (int k = oldObstacleCount; k < newObstacleCount; k++)
                                        {
                                            scenario.obstacles[k] = new ObstacleSetup
                                            {
                                                obstacleCode = "U01",
                                                count = 1,
                                                placement = ObstaclePlacement.Center,
                                                randomizeHeight = false,
                                                randomizeScale = false
                                            };
                                        }
                                    }
                                    
                                    // Display obstacle configurations
                                    if (scenario.obstacles != null)
                                    {
                                        for (int k = 0; k < scenario.obstacles.Length; k++)
                                        {
                                            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                            
                                            GUILayout.Label($"Obstacle Type {k + 1}", EditorStyles.boldLabel);
                                            
                                            ObstacleSetup obstacleSetup = scenario.obstacles[k];
                                            
                                            obstacleSetup.obstacleCode = EditorGUILayout.TextField("Obstacle Code", obstacleSetup.obstacleCode);
                                            obstacleSetup.count = EditorGUILayout.IntField("Count", obstacleSetup.count);
                                            obstacleSetup.placement = (ObstaclePlacement)EditorGUILayout.EnumPopup("Placement", obstacleSetup.placement);
                                            obstacleSetup.randomizeHeight = EditorGUILayout.Toggle("Randomize Height", obstacleSetup.randomizeHeight);
                                            obstacleSetup.randomizeScale = EditorGUILayout.Toggle("Randomize Scale", obstacleSetup.randomizeScale);
                                            
                                            scenario.obstacles[k] = obstacleSetup;
                                            
                                            EditorGUILayout.EndVertical();
                                        }
                                    }
                                    
                                    tutorial.scenarios[j] = scenario;
                                    
                                    EditorGUILayout.EndVertical();
                                    EditorGUILayout.Space();
                                }
                            }
                            
                            EditorGUI.indentLevel--;
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                    
                    // Update the tutorial in the sequence
                    initializer.tutorialSequence[i] = tutorial;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Draw the rest of the fields
            DrawPropertiesExcluding(serializedObject, new string[] { "tutorialLevelIndex", "tutorialSequence" });
            
            // Set dirty flag if needed
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                
                if (Application.isPlaying)
                {
                    Debug.Log("Tutorial configuration changed during play mode");
                }
            }
        }
    }
}