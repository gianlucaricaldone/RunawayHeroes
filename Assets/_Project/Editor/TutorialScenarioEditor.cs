// Path: Editor/TutorialScenarioEditor.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RunawayHeroes.Gameplay;
using RunawayHeroes.Runtime.Levels;

namespace RunawayHeroes.Editor
{
    /// <summary>
    /// Editor custom per TutorialLevelInitializer
    /// </summary>
    [CustomEditor(typeof(TutorialLevelInitializer))]
    public class TutorialScenarioEditor : UnityEditor.Editor
    {
        private SerializedProperty tutorialSequenceProp;
        private SerializedProperty showDebugGizmosProp;
        
        // Dizionario per tenere traccia degli scenari espansi
        private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();
        
        private void OnEnable()
        {
            tutorialSequenceProp = serializedObject.FindProperty("tutorialSequence");
            showDebugGizmosProp = serializedObject.FindProperty("showDebugGizmos");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(showDebugGizmosProp);
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Tutorial Sequences", EditorStyles.boldLabel);
            
            if (tutorialSequenceProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("Nessuna sequenza tutorial definita.", MessageType.Info);
            }
            
            for (int i = 0; i < tutorialSequenceProp.arraySize; i++)
            {
                DrawTutorialSequence(tutorialSequenceProp.GetArrayElementAtIndex(i), i);
            }
            
            GUILayout.Space(10);
            if (GUILayout.Button("Add Tutorial Sequence"))
            {
                tutorialSequenceProp.arraySize++;
                var newSequence = tutorialSequenceProp.GetArrayElementAtIndex(tutorialSequenceProp.arraySize - 1);
                var descProp = newSequence.FindPropertyRelative("description");
                descProp.stringValue = "New Tutorial";
                var difficultyProp = newSequence.FindPropertyRelative("difficulty");
                difficultyProp.intValue = 1;
                var lengthProp = newSequence.FindPropertyRelative("length");
                lengthProp.floatValue = 500f;
                var themeType = newSequence.FindPropertyRelative("theme");
                themeType.enumValueIndex = 0; // Tutorial theme
                
                var scenariosProp = newSequence.FindPropertyRelative("scenarios");
                scenariosProp.arraySize = 0;
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawTutorialSequence(SerializedProperty tutorialSequence, int index)
        {
            string key = $"tutorial_{index}";
            if (!foldoutStates.ContainsKey(key))
            {
                foldoutStates[key] = false;
            }
            
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header con nome, eliminazione e toggle
            EditorGUILayout.BeginHorizontal();
            
            var descProp = tutorialSequence.FindPropertyRelative("description");
            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], descProp.stringValue, true);
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Conferma eliminazione", 
                    $"Sei sicuro di voler eliminare il tutorial '{descProp.stringValue}'?", "Elimina", "Annulla"))
                {
                    tutorialSequenceProp.DeleteArrayElementAtIndex(index);
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(descProp);
                
                var themeType = tutorialSequence.FindPropertyRelative("theme");
                EditorGUILayout.PropertyField(themeType);
                
                var lengthProp = tutorialSequence.FindPropertyRelative("length");
                EditorGUILayout.PropertyField(lengthProp);
                
                var difficultyProp = tutorialSequence.FindPropertyRelative("difficulty");
                EditorGUILayout.PropertyField(difficultyProp);
                
                // Scenari di questo tutorial
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Scenarios", EditorStyles.boldLabel);
                
                var scenariosProp = tutorialSequence.FindPropertyRelative("scenarios");
                
                if (scenariosProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("Nessuno scenario definito.", MessageType.Info);
                }
                
                for (int j = 0; j < scenariosProp.arraySize; j++)
                {
                    DrawScenario(scenariosProp.GetArrayElementAtIndex(j), index, j);
                }
                
                GUILayout.Space(5);
                if (GUILayout.Button("Add Scenario"))
                {
                    scenariosProp.arraySize++;
                    var newScenario = scenariosProp.GetArrayElementAtIndex(scenariosProp.arraySize - 1);
                    newScenario.FindPropertyRelative("name").stringValue = $"Scenario_{scenariosProp.arraySize}";
                    newScenario.FindPropertyRelative("distanceFromStart").floatValue = (scenariosProp.arraySize - 1) * 50f;
                    newScenario.FindPropertyRelative("instructionMessage").stringValue = "New instruction";
                    newScenario.FindPropertyRelative("messageDuration").floatValue = 5f;
                    newScenario.FindPropertyRelative("randomPlacement").boolValue = false;
                    newScenario.FindPropertyRelative("obstacleSpacing").floatValue = 10f;
                    
                    // Inizializza la lista di ostacoli con un ostacolo predefinito
                    var obstaclesProp = newScenario.FindPropertyRelative("obstacles");
                    obstaclesProp.arraySize = 1;
                    InitializeObstacleSetup(obstaclesProp.GetArrayElementAtIndex(0));
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawScenario(SerializedProperty scenario, int tutorialIndex, int scenarioIndex)
        {
            string key = $"tutorial_{tutorialIndex}_scenario_{scenarioIndex}";
            if (!foldoutStates.ContainsKey(key))
            {
                foldoutStates[key] = false;
            }
            
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header con nome, eliminazione e toggle
            EditorGUILayout.BeginHorizontal();
            
            var nameProp = scenario.FindPropertyRelative("name");
            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], nameProp.stringValue, true);
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                var scenariosProp = tutorialSequenceProp.GetArrayElementAtIndex(tutorialIndex).FindPropertyRelative("scenarios");
                scenariosProp.DeleteArrayElementAtIndex(scenarioIndex);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(nameProp);
                
                var distanceFromStartProp = scenario.FindPropertyRelative("distanceFromStart");
                EditorGUILayout.PropertyField(distanceFromStartProp);
                
                var instructionMessageProp = scenario.FindPropertyRelative("instructionMessage");
                EditorGUILayout.PropertyField(instructionMessageProp);
                
                var messageDurationProp = scenario.FindPropertyRelative("messageDuration");
                EditorGUILayout.PropertyField(messageDurationProp);
                
                var randomPlacementProp = scenario.FindPropertyRelative("randomPlacement");
                EditorGUILayout.PropertyField(randomPlacementProp);
                
                var obstacleSpacingProp = scenario.FindPropertyRelative("obstacleSpacing");
                EditorGUILayout.PropertyField(obstacleSpacingProp);
                
                // Ostacoli per questo scenario
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Obstacle Types", EditorStyles.boldLabel);
                
                var obstaclesProp = scenario.FindPropertyRelative("obstacles");
                
                if (obstaclesProp.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("Nessun tipo di ostacolo definito.", MessageType.Info);
                }
                
                for (int k = 0; k < obstaclesProp.arraySize; k++)
                {
                    DrawObstacleSetup(obstaclesProp.GetArrayElementAtIndex(k), tutorialIndex, scenarioIndex, k);
                }
                
                GUILayout.Space(5);
                if (GUILayout.Button("Add Obstacle Type"))
                {
                    obstaclesProp.arraySize++;
                    InitializeObstacleSetup(obstaclesProp.GetArrayElementAtIndex(obstaclesProp.arraySize - 1));
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawObstacleSetup(SerializedProperty obstacleSetup, int tutorialIndex, int scenarioIndex, int obstacleIndex)
        {
            string key = $"tutorial_{tutorialIndex}_scenario_{scenarioIndex}_obstacle_{obstacleIndex}";
            if (!foldoutStates.ContainsKey(key))
            {
                foldoutStates[key] = false;
            }
            
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Header con codice ostacolo, eliminazione e toggle
            EditorGUILayout.BeginHorizontal();
            
            var obstacleProp = obstacleSetup.FindPropertyRelative("obstacleCode");
            foldoutStates[key] = EditorGUILayout.Foldout(foldoutStates[key], $"Obstacle Type: {obstacleProp.stringValue}", true);
            
            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                var obstaclesProp = tutorialSequenceProp.GetArrayElementAtIndex(tutorialIndex)
                    .FindPropertyRelative("scenarios").GetArrayElementAtIndex(scenarioIndex)
                    .FindPropertyRelative("obstacles");
                obstaclesProp.DeleteArrayElementAtIndex(obstacleIndex);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                
                // Selettore rapido di preset
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Quick Preset", GUILayout.Width(120));
                
                if (GUILayout.Button("Jump Obstacles"))
                {
                    obstacleProp.stringValue = "U02";
                    obstacleSetup.FindPropertyRelative("count").intValue = 3;
                    obstacleSetup.FindPropertyRelative("placement").enumValueIndex = (int)ObstaclePlacement.Center;
                    obstacleSetup.FindPropertyRelative("randomizeHeight").boolValue = false;
                    obstacleSetup.FindPropertyRelative("randomizeScale").boolValue = false;
                    obstacleSetup.FindPropertyRelative("heightRange").vector2Value = new Vector2(0.3f, 0.5f);
                    obstacleSetup.FindPropertyRelative("scaleRange").vector2Value = new Vector2(1f, 1f);
                    obstacleSetup.FindPropertyRelative("startOffset").floatValue = 5f;
                }
                
                if (GUILayout.Button("Slide Obstacles"))
                {
                    obstacleProp.stringValue = "U03";
                    obstacleSetup.FindPropertyRelative("count").intValue = 3;
                    obstacleSetup.FindPropertyRelative("placement").enumValueIndex = (int)ObstaclePlacement.Center;
                    obstacleSetup.FindPropertyRelative("randomizeHeight").boolValue = false;
                    obstacleSetup.FindPropertyRelative("randomizeScale").boolValue = false;
                    obstacleSetup.FindPropertyRelative("heightRange").vector2Value = new Vector2(1.5f, 2f);
                    obstacleSetup.FindPropertyRelative("scaleRange").vector2Value = new Vector2(1f, 1f);
                    obstacleSetup.FindPropertyRelative("startOffset").floatValue = 5f;
                }
                
                if (GUILayout.Button("Side Step"))
                {
                    obstacleProp.stringValue = "U01";
                    obstacleSetup.FindPropertyRelative("count").intValue = 5;
                    obstacleSetup.FindPropertyRelative("placement").enumValueIndex = (int)ObstaclePlacement.Pattern;
                    obstacleSetup.FindPropertyRelative("randomizeHeight").boolValue = false;
                    obstacleSetup.FindPropertyRelative("randomizeScale").boolValue = true;
                    obstacleSetup.FindPropertyRelative("heightRange").vector2Value = new Vector2(0.8f, 1.2f);
                    obstacleSetup.FindPropertyRelative("scaleRange").vector2Value = new Vector2(0.8f, 1.2f);
                    obstacleSetup.FindPropertyRelative("startOffset").floatValue = 5f;
                }
                
                if (GUILayout.Button("Random"))
                {
                    obstacleProp.stringValue = "U04";
                    obstacleSetup.FindPropertyRelative("count").intValue = 8;
                    obstacleSetup.FindPropertyRelative("placement").enumValueIndex = (int)ObstaclePlacement.Random;
                    obstacleSetup.FindPropertyRelative("randomizeHeight").boolValue = true;
                    obstacleSetup.FindPropertyRelative("randomizeScale").boolValue = true;
                    obstacleSetup.FindPropertyRelative("heightRange").vector2Value = new Vector2(0.3f, 1.8f);
                    obstacleSetup.FindPropertyRelative("scaleRange").vector2Value = new Vector2(0.7f, 1.3f);
                    obstacleSetup.FindPropertyRelative("startOffset").floatValue = 5f;
                }
                
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);
                
                // Proprietà dell'ostacolo
                EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("obstacleCode"));
                EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("count"));
                EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("placement"));
                EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("startOffset"));
                
                // Randomizzazione altezza
                var randomizeHeightProp = obstacleSetup.FindPropertyRelative("randomizeHeight");
                EditorGUILayout.PropertyField(randomizeHeightProp);
                
                if (randomizeHeightProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("heightRange"));
                    EditorGUI.indentLevel--;
                }
                
                // Randomizzazione scala
                var randomizeScaleProp = obstacleSetup.FindPropertyRelative("randomizeScale");
                EditorGUILayout.PropertyField(randomizeScaleProp);
                
                if (randomizeScaleProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(obstacleSetup.FindPropertyRelative("scaleRange"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void InitializeObstacleSetup(SerializedProperty obstacleSetup)
        {
            obstacleSetup.FindPropertyRelative("obstacleCode").stringValue = "U01";
            obstacleSetup.FindPropertyRelative("count").intValue = 1;
            obstacleSetup.FindPropertyRelative("placement").enumValueIndex = (int)ObstaclePlacement.Center;
            obstacleSetup.FindPropertyRelative("randomizeHeight").boolValue = false;
            obstacleSetup.FindPropertyRelative("heightRange").vector2Value = new Vector2(0.5f, 1.5f);
            obstacleSetup.FindPropertyRelative("randomizeScale").boolValue = false;
            obstacleSetup.FindPropertyRelative("scaleRange").vector2Value = new Vector2(0.8f, 1.2f);
            obstacleSetup.FindPropertyRelative("startOffset").floatValue = 0f;
        }
    }
}
#endif