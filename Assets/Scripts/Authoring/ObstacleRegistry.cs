using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Registro che associa i codici ostacolo ai prefab
/// </summary>
public class ObstacleRegistry : MonoBehaviour
{
    // Associazione tra codice ostacolo e prefab
    [System.Serializable]
    public class ObstacleMapping
    {
        public string obstacleCode;
        public GameObject obstaclePrefab;
    }
    
    // Lista delle associazioni
    public List<ObstacleMapping> obstacleMappings = new List<ObstacleMapping>();
    
    // Singleton per accesso facile
    public static ObstacleRegistry Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Ottiene il prefab per un dato codice ostacolo
    public GameObject GetPrefabForCode(string code)
    {
        foreach (var mapping in obstacleMappings)
        {
            if (mapping.obstacleCode == code)
                return mapping.obstaclePrefab;
        }
        
        Debug.LogWarning($"No prefab found for obstacle code: {code}");
        return null;
    }
}