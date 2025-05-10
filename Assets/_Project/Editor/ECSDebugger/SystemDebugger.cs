using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using RunawayHeroes.Utilities.ECSCompatibility;

public class SystemDebugger : MonoBehaviour
{
    void Start()
    {
        // Otteniamo il world di default
        var world = RunawayWorldExtensions.DefaultGameObjectInjectionWorld;
        if (world != null)
        {
            Debug.Log("=== SISTEMI REGISTRATI ===");
            
            // Otteniamo tutti i sistemi registrati
            var systems = world.Systems;
            foreach (var system in systems)
            {
                Debug.Log($"Sistema: {system.GetType().Name}");
                
                // Per i ComponentSystemGroup, elenchiamo anche i loro sottosistemi
                if (system is ComponentSystemGroup group)
                {
                    var fieldInfo = typeof(ComponentSystemGroup).GetField("m_systemsToUpdate", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (fieldInfo != null)
                    {
                        var subsystems = fieldInfo.GetValue(group) as List<ComponentSystemBase>;
                        if (subsystems != null)
                        {
                            foreach (var subsystem in subsystems)
                            {
                                Debug.Log($"  - Sottosistema: {subsystem.GetType().Name}");
                            }
                        }
                    }
                }
            }
        }
    }
}