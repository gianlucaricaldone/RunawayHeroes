using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using System.Collections.Generic;

public class DOTSVersionChecker : EditorWindow
{
    [MenuItem("Tools/DOTS Version Checker")]
    public static void ShowWindow()
    {
        GetWindow<DOTSVersionChecker>("DOTS Version Checker");
    }

    private void OnGUI()
    {
        GUILayout.Label("DOTS Packages Versions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Check Versions"))
        {
            CheckDOTSVersions();
        }
    }

    private void CheckDOTSVersions()
    {
        var packageRequest = Client.List(true);
        while (!packageRequest.IsCompleted)
            System.Threading.Thread.Sleep(100);
            
        if (packageRequest.Status == StatusCode.Success)
        {
            Dictionary<string, string> dotsPackages = new Dictionary<string, string>();
            
            foreach (var package in packageRequest.Result)
            {
                if (IsDOTSPackage(package.name))
                {
                    dotsPackages.Add(package.name, package.version);
                    Debug.Log($"DOTS Package: {package.name}, Version: {package.version}");
                }
            }
            
            // Controlla compatibilità tra versioni
            CheckVersionCompatibility(dotsPackages);
        }
        else
        {
            Debug.LogError("Failed to get package list: " + packageRequest.Error.message);
        }
    }
    
    private bool IsDOTSPackage(string packageName)
    {
        string[] dotsPackageNames = new string[]
        {
            "com.unity.entities",
            "com.unity.collections",
            "com.unity.jobs",
            "com.unity.burst",
            "com.unity.mathematics",
            "com.unity.rendering.hybrid",
            "com.unity.entities.graphics",
            "com.unity.scenes",
            "com.unity.dots.editor"
        };
        
        foreach (var name in dotsPackageNames)
        {
            if (packageName.Contains(name))
                return true;
        }
        
        return false;
    }
    
    private void CheckVersionCompatibility(Dictionary<string, string> packages)
    {
        // Verifica compatibilità delle versioni
        if (packages.ContainsKey("com.unity.entities"))
        {
            string entitiesVersion = packages["com.unity.entities"];
            Debug.Log($"Core Entities version: {entitiesVersion}");
            
            // Controlla se altre versioni sono compatibili con Entities
            foreach (var package in packages)
            {
                if (package.Key != "com.unity.entities")
                {
                    // Qui potresti implementare una logica più sofisticata per controllare 
                    // la compatibilità delle versioni
                    Debug.Log($"Checking compatibility: {package.Key} (v{package.Value}) with Entities (v{entitiesVersion})");
                }
            }
        }
    }
}