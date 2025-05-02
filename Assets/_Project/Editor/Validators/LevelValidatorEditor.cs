#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RunawayHeroes.Runtime.Levels;

namespace RunawayHeroes.Editor
{
    /// <summary>
    /// Editor custom per LevelValidator
    /// </summary>
    [CustomEditor(typeof(LevelValidator))]
    public class LevelValidatorEditor : UnityEditor.Editor
    {
        private ValidationResult _lastValidationResult;
        private Vector2 _issuesScrollPosition;
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            LevelValidator validator = (LevelValidator)target;
            
            // Sezione configurazione
            EditorGUILayout.LabelField("Configurazione Validazione", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterRadius"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("characterSlideHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxJumpHeight"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxConsecutiveJumps"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSideStepDistance"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showDebugVisualization"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("generateDetailedReport"));
            
            // Riferimento all'inizializzatore del livello
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Riferimenti", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("tutorialInitializer"));
            
            // Pulsante validate
            EditorGUILayout.Space(15);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Valida Livello", GUILayout.Height(30), GUILayout.Width(150)))
            {
                _lastValidationResult = validator.ValidateLevel();
                Repaint(); // Aggiorna l'editor
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Mostra risultati validazione
            if (_lastValidationResult != null)
            {
                DisplayValidationResults(_lastValidationResult);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Visualizza i risultati della validazione
        /// </summary>
        private void DisplayValidationResults(ValidationResult result)
        {
            EditorGUILayout.Space(15);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Intestazione
            EditorGUILayout.BeginHorizontal();
            
            string validationText = result.IsValid ? 
                "✅ Livello Completabile!" : 
                "❌ Livello NON Completabile!";
            
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            if (result.IsValid)
                headerStyle.normal.textColor = new Color(0.0f, 0.6f, 0.0f);
            else
                headerStyle.normal.textColor = Color.red;
                
            EditorGUILayout.LabelField(validationText, headerStyle, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();
            
            // Contatore problemi
            if (result.Issues.Count > 0)
            {
                int criticalCount = 0;
                int warningCount = 0;
                
                foreach (var issue in result.Issues)
                {
                    if (issue.Type == IssueType.Critical)
                        criticalCount++;
                    else
                        warningCount++;
                }
                
                GUIStyle countStyle = new GUIStyle(EditorStyles.label);
                countStyle.alignment = TextAnchor.MiddleCenter;
                
                string issueText = $"Trovati {criticalCount} problemi critici e {warningCount} avvisi";
                EditorGUILayout.LabelField(issueText, countStyle);
                
                EditorGUILayout.Space(5);
                
                // Lista problemi
                _issuesScrollPosition = EditorGUILayout.BeginScrollView(_issuesScrollPosition, 
                    GUILayout.Height(Mathf.Min(400, result.Issues.Count * 40)));
                
                // Prima i problemi critici
                if (criticalCount > 0)
                {
                    DisplayIssueGroup(result.Issues, IssueType.Critical, "Problemi Critici");
                }
                
                // Poi gli avvisi
                if (warningCount > 0)
                {
                    DisplayIssueGroup(result.Issues, IssueType.Warning, "Avvisi");
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUIStyle perfectStyle = new GUIStyle(EditorStyles.label);
                perfectStyle.alignment = TextAnchor.MiddleCenter;
                perfectStyle.normal.textColor = new Color(0.0f, 0.6f, 0.0f);
                
                EditorGUILayout.LabelField("Nessun problema rilevato! Il livello è perfetto.", perfectStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// Visualizza un gruppo di problemi dello stesso tipo
        /// </summary>
        private void DisplayIssueGroup(List<ValidationIssue> issues, IssueType type, string groupTitle)
        {
            string key = "issues_" + type.ToString();
            
            if (!_foldoutStates.ContainsKey(key))
            {
                _foldoutStates[key] = true; // Default espanso
            }
            
            // Stile intestazione
            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldout);
            headerStyle.fontStyle = FontStyle.Bold;
            
            if (type == IssueType.Critical)
                headerStyle.normal.textColor = Color.red;
            else
                headerStyle.normal.textColor = new Color(0.9f, 0.6f, 0.1f);
            
            // Conta quanti problemi di questo tipo ci sono
            int count = 0;
            foreach (var issue in issues)
            {
                if (issue.Type == type)
                    count++;
            }
            
            // Foldout per questo gruppo
            _foldoutStates[key] = EditorGUILayout.Foldout(_foldoutStates[key], 
                $"{groupTitle} ({count})", true, headerStyle);
            
            if (_foldoutStates[key])
            {
                EditorGUI.indentLevel++;
                
                foreach (var issue in issues)
                {
                    if (issue.Type == type)
                    {
                        DisplayIssue(issue);
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// Visualizza un singolo problema
        /// </summary>
        private void DisplayIssue(ValidationIssue issue)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Stile messaggio
            GUIStyle messageStyle = new GUIStyle(EditorStyles.label);
            messageStyle.wordWrap = true;
            
            if (issue.Type == IssueType.Critical)
                messageStyle.normal.textColor = Color.red;
            else
                messageStyle.normal.textColor = new Color(0.9f, 0.6f, 0.1f);
            
            // Messaggio problema
            EditorGUILayout.LabelField(issue.Message, messageStyle);
            
            // Info scenario
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scenario:", GUILayout.Width(70));
            EditorGUILayout.LabelField(issue.ScenarioName);
            EditorGUILayout.EndHorizontal();
            
            // Posizione
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Posizione:", GUILayout.Width(70));
            EditorGUILayout.Vector3Field("", issue.Position);
            
            // Pulsante per selezionare nel mondo
            if (GUILayout.Button("Focus", GUILayout.Width(60)))
            {
                // Trova il transform dell'inizializzatore
                LevelValidator validator = (LevelValidator)target;
                if (validator.tutorialInitializer != null)
                {
                    Vector3 worldPos = validator.tutorialInitializer.transform.position + issue.Position;
                    
                    // Seleziona nel mondo
                    SceneView sceneView = SceneView.lastActiveSceneView;
                    if (sceneView != null)
                    {
                        sceneView.pivot = worldPos;
                        sceneView.Repaint();
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }
}
#endif