using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce la configurazione per la generazione di livelli runner
    /// </summary>
    [Serializable]
    public struct RunnerLevelConfigComponent : IComponentData
    {
        // Configurazione del livello
        public int LevelLength;         // Lunghezza approssimativa del livello in metri
        public int MinSegments;         // Numero minimo di segmenti
        public int MaxSegments;         // Numero massimo di segmenti
        public int Seed;                // Seed per la generazione casuale
        
        // Configurazione di difficoltà
        public int StartDifficulty;     // Difficoltà iniziale (1-10)
        public int EndDifficulty;       // Difficoltà finale (1-10)
        public float DifficultyRamp;    // Velocità di incremento della difficoltà (0-1)
        
        // Configurazione degli ostacoli
        public float ObstacleDensity;   // Densità degli ostacoli (0-1)
        public float EnemyDensity;      // Densità dei nemici (0-1)
        public float CollectibleDensity; // Densità degli oggetti collezionabili (0-1)
        
        // Configurazione del tema
        public WorldTheme PrimaryTheme;  // Tema principale del livello
        public WorldTheme SecondaryTheme; // Tema secondario (per transizioni)
        public float ThemeBlendFactor;  // Fattore di mescolamento dei temi (0-1)
        
        // Configurazione avanzata
        public bool GenerateCheckpoints; // Genera checkpoint automaticamente?
        public bool DynamicDifficulty;   // Adatta la difficoltà durante il gioco?
        public float SegmentVarietyFactor; // Fattore di varietà dei segmenti (0-1)
    }
}