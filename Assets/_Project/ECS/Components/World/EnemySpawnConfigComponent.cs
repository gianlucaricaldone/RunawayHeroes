using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce le configurazioni per lo spawn di nemici in un segmento
    /// </summary>
    [Serializable]
    public struct EnemySpawnConfigComponent : IComponentData
    {
        // Configurazione generale
        public float DensityFactor;      // Fattore di densità dei nemici (0-1)
        public int MinEnemies;           // Numero minimo di nemici nel segmento
        public int MaxEnemies;           // Numero massimo di nemici nel segmento
        
        // Configurazione per tipo
        public float DroneProbability;   // Probabilità di droni
        public float PatrolProbability;  // Probabilità di nemici pattuglia
        public float AmbushProbability;  // Probabilità di nemici in agguato
        
        // Configurazione per ogni tema
        public float CityEnemyWeight;      // Peso per nemici urbani
        public float ForestEnemyWeight;    // Peso per nemici forestali
        public float TundraEnemyWeight;    // Peso per nemici ghiacciati
        public float VolcanoEnemyWeight;   // Peso per nemici vulcanici
        public float AbyssEnemyWeight;     // Peso per nemici subacquei
        public float VirtualEnemyWeight;   // Peso per nemici digitali
        
        // Configurazione boss e mid-boss
        public float MidBossProbability;  // Probabilità di mid-boss
        public float BossProbability;     // Probabilità di boss (tipicamente vicino a 0)
        
        // Configurazione gruppi
        public float GroupSpawnProbability; // Probabilità di spawn in gruppo
        public int MinGroupSize;          // Dimensione minima dei gruppi
        public int MaxGroupSize;          // Dimensione massima dei gruppi
        
        // Configurazione avanzata
        public float EliteEnemyProbability; // Probabilità di nemici élite (più forti)
    }
}