using System;
using UnityEngine;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.Gameplay;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Definisce una sequenza di livelli tutorial con progressione di difficoltà
    /// </summary>
    [Serializable]
    public struct TutorialLevelData
    {
        [Tooltip("Descrizione del livello tutorial")]
        public string description;
        
        [Tooltip("Tema del livello tutorial")]
        public WorldTheme theme;
        
        [Tooltip("Lunghezza del livello in metri")]
        [Range(300, 1000)]
        public int length;
        
        [Tooltip("Livello di difficoltà (1-10)")]
        [Range(1, 10)]
        public int difficulty;
        
        [Tooltip("Densità di ostacoli (0-1)")]
        [Range(0, 1)]
        public float obstacleDensity;
        
        [Tooltip("Densità di nemici (0-1)")]
        [Range(0, 1)]
        public float enemyDensity;
        
        [Tooltip("Scenari di insegnamento specifici")]
        public TutorialScenario[] scenarios;
    }
    
    /// <summary>
    /// Definisce uno scenario di insegnamento specifico in un livello tutorial
    /// </summary>
    [Serializable]
    public struct TutorialScenario
    {
        [Tooltip("Nome dello scenario")]
        public string name;
        
        [Tooltip("Distanza dall'inizio in metri")]
        public float distanceFromStart;
        
        [Tooltip("Tipi di ostacoli da usare per l'insegnamento")]
        public ObstacleSetup[] obstacles;
        
        [Tooltip("Messaggio di istruzione da mostrare")]
        public string instructionMessage;
        
        [Tooltip("Durata del messaggio in secondi")]
        public float messageDuration;
        
        [Tooltip("Posizionamento in sequenza o casuale")]
        public bool randomPlacement;
        
        [Tooltip("Distanza tra gli ostacoli")]
        public float obstacleSpacing;
    }
    
}