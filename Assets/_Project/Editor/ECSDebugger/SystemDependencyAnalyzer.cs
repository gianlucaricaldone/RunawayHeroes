using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace RunawayHeroes.Editor.ECSDebugger
{
    public class SystemDependencyAnalyzer : EditorWindow
    {
        [MenuItem("Runaway Heroes/Tools/System Dependency Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<SystemDependencyAnalyzer>("System Dependency Analyzer");
        }

        private Vector2 scrollPosition;
        private bool showAllSystems = false;
        private bool showOnlyProblematic = true;
        private List<SystemInfo> systemInfos = new List<SystemInfo>();
        private List<CyclicDependency> cyclicDependencies = new List<CyclicDependency>();

        private void OnEnable()
        {
            AnalyzeSystems();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Analysis", GUILayout.Width(150)))
            {
                AnalyzeSystems();
            }
            
            showAllSystems = EditorGUILayout.Toggle("Show All Systems", showAllSystems);
            showOnlyProblematic = EditorGUILayout.Toggle("Show Only Problematic", showOnlyProblematic);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (cyclicDependencies.Count > 0)
            {
                EditorGUILayout.LabelField("Cyclic Dependencies Detected:", EditorStyles.boldLabel);
                
                foreach (var cycle in cyclicDependencies)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"Cycle found between {cycle.Systems.Count} systems:", EditorStyles.boldLabel);
                    
                    for (int i = 0; i < cycle.Systems.Count; i++)
                    {
                        var from = cycle.Systems[i];
                        var to = cycle.Systems[(i + 1) % cycle.Systems.Count];
                        EditorGUILayout.LabelField($"  • {from.Name} → {to.Name}");
                    }
                    
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            else if (systemInfos.Count > 0)
            {
                EditorGUILayout.LabelField("No cyclic dependencies detected.", EditorStyles.boldLabel);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("System Dependencies:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            foreach (var systemInfo in systemInfos)
            {
                if (!showAllSystems && !systemInfo.HasDependencies && !systemInfo.HasDependents)
                    continue;
                
                if (showOnlyProblematic && !systemInfo.IsProblematic)
                    continue;
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                string systemLabel = systemInfo.Name;
                if (systemInfo.IsProblematic)
                    systemLabel += " (PROBLEMATIC)";
                
                EditorGUILayout.LabelField(systemLabel, EditorStyles.boldLabel);
                
                if (systemInfo.Group != null)
                    EditorGUILayout.LabelField($"Group: {systemInfo.Group}");
                
                if (systemInfo.UpdateBefore.Count > 0)
                {
                    EditorGUILayout.LabelField("Updates Before:", EditorStyles.boldLabel);
                    foreach (var before in systemInfo.UpdateBefore)
                    {
                        EditorGUILayout.LabelField($"  • {before}");
                    }
                }
                
                if (systemInfo.UpdateAfter.Count > 0)
                {
                    EditorGUILayout.LabelField("Updates After:", EditorStyles.boldLabel);
                    foreach (var after in systemInfo.UpdateAfter)
                    {
                        EditorGUILayout.LabelField($"  • {after}");
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void AnalyzeSystems()
        {
            systemInfos.Clear();
            cyclicDependencies.Clear();
            
            // Get all system types
            var systemTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(SystemBase)) && !t.IsAbstract)
                .ToList();
            
            // Create system info objects
            foreach (var systemType in systemTypes)
            {
                var systemInfo = new SystemInfo
                {
                    Type = systemType,
                    Name = systemType.Name
                };
                
                // Check UpdateInGroup attribute
                var updateInGroupAttr = systemType.GetCustomAttribute<UpdateInGroupAttribute>();
                if (updateInGroupAttr != null)
                {
                    systemInfo.Group = updateInGroupAttr.GroupType.Name;
                }
                
                // Check UpdateBefore attributes
                var updateBeforeAttrs = systemType.GetCustomAttributes<UpdateBeforeAttribute>();
                foreach (var attr in updateBeforeAttrs)
                {
                    systemInfo.UpdateBefore.Add(attr.SystemType.Name);
                }
                
                // Check UpdateAfter attributes
                var updateAfterAttrs = systemType.GetCustomAttributes<UpdateAfterAttribute>();
                foreach (var attr in updateAfterAttrs)
                {
                    systemInfo.UpdateAfter.Add(attr.SystemType.Name);
                }
                
                systemInfos.Add(systemInfo);
            }
            
            // Connect dependencies
            foreach (var systemInfo in systemInfos)
            {
                foreach (var beforeSystem in systemInfo.UpdateBefore)
                {
                    var targetSystem = systemInfos.FirstOrDefault(s => s.Name == beforeSystem);
                    if (targetSystem != null)
                    {
                        systemInfo.DependenciesTo.Add(targetSystem);
                        targetSystem.DependentsFrom.Add(systemInfo);
                    }
                    else
                    {
                        // Missing system reference
                        systemInfo.IsProblematic = true;
                        Debug.LogWarning($"System {systemInfo.Name} has UpdateBefore dependency on non-existent system {beforeSystem}");
                    }
                }
                
                foreach (var afterSystem in systemInfo.UpdateAfter)
                {
                    var targetSystem = systemInfos.FirstOrDefault(s => s.Name == afterSystem);
                    if (targetSystem != null)
                    {
                        systemInfo.DependenciesFrom.Add(targetSystem);
                        targetSystem.DependentsTo.Add(systemInfo);
                    }
                    else
                    {
                        // Missing system reference
                        systemInfo.IsProblematic = true;
                        Debug.LogWarning($"System {systemInfo.Name} has UpdateAfter dependency on non-existent system {afterSystem}");
                    }
                }
            }
            
            // Find cyclic dependencies
            FindCyclicDependencies();
        }

        private void FindCyclicDependencies()
        {
            foreach (var systemInfo in systemInfos)
            {
                var visited = new HashSet<SystemInfo>();
                var path = new List<SystemInfo>();
                
                FindCycles(systemInfo, systemInfo, visited, path);
            }
        }

        private void FindCycles(SystemInfo start, SystemInfo current, HashSet<SystemInfo> visited, List<SystemInfo> path)
        {
            visited.Add(current);
            path.Add(current);
            
            foreach (var dependency in current.DependenciesTo.Concat(current.DependentsFrom))
            {
                if (dependency == start && path.Count > 1)
                {
                    // Cycle detected
                    var cycle = new CyclicDependency();
                    foreach (var system in path)
                    {
                        cycle.Systems.Add(system);
                        system.IsProblematic = true;
                    }
                    
                    cyclicDependencies.Add(cycle);
                    return;
                }
                
                if (!visited.Contains(dependency))
                {
                    FindCycles(start, dependency, new HashSet<SystemInfo>(visited), new List<SystemInfo>(path));
                }
            }
        }

        private class SystemInfo
        {
            public Type Type;
            public string Name;
            public string Group;
            public List<string> UpdateBefore = new List<string>();
            public List<string> UpdateAfter = new List<string>();
            
            public List<SystemInfo> DependenciesTo = new List<SystemInfo>(); // Systems that should update after this one
            public List<SystemInfo> DependenciesFrom = new List<SystemInfo>(); // Systems that should update before this one
            public List<SystemInfo> DependentsTo = new List<SystemInfo>(); // Systems that this one should update after
            public List<SystemInfo> DependentsFrom = new List<SystemInfo>(); // Systems that this one should update before
            
            public bool IsProblematic = false;
            
            public bool HasDependencies => DependenciesTo.Count > 0 || DependenciesFrom.Count > 0;
            public bool HasDependents => DependentsTo.Count > 0 || DependentsFrom.Count > 0;
        }

        private class CyclicDependency
        {
            public List<SystemInfo> Systems = new List<SystemInfo>();
        }
    }
}