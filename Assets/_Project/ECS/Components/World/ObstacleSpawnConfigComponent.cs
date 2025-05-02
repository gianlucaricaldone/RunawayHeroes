using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce le configurazioni per lo spawn di ostacoli in un segmento
    /// </summary>
    [Serializable]
    public struct ObstacleSpawnConfigComponent : IComponentData
    {
        // Configurazione generale
        public float DensityFactor;       // Fattore di densità degli ostacoli (0-1)
        public int MinObstacles;          // Numero minimo di ostacoli nel segmento
        public int MaxObstacles;          // Numero massimo di ostacoli nel segmento
        
        // Configurazione per tipo
        public float SmallObstacleProbability;  // Probabilità di ostacoli piccoli
        public float MediumObstacleProbability; // Probabilità di ostacoli medi
        public float LargeObstacleProbability;  // Probabilità di ostacoli grandi
        
        // Configurazione per ogni tema
        public float CityObstacleWeight;        // Peso per ostacoli urbani
        public float ForestObstacleWeight;      // Peso per ostacoli forestali
        public float TundraObstacleWeight;      // Peso per ostacoli ghiacciati
        public float VolcanoObstacleWeight;     // Peso per ostacoli lavici
        public float AbyssObstacleWeight;       // Peso per ostacoli subacquei
        public float VirtualObstacleWeight;     // Peso per ostacoli digitali
        
        // Configurazione specifica per ostacoli tematici
        public float LavaObstacleProbability;   // Probabilità di ostacoli di lava
        public float IceObstacleProbability;    // Probabilità di ostacoli di ghiaccio
        public float DigitalBarrierProbability; // Probabilità di barriere digitali
        public float UnderwaterProbability;     // Probabilità di zone sottomarine
        public float SlipperyProbability;       // Probabilità di superfici scivolose
        public float ToxicGroundProbability;    // Probabilità di terreni tossici
        public float CurrentProbability;        // Probabilità di correnti d'aria/acqua
        
        // Densità di pericoli speciali
        public float SpecialHazardDensity;      // Densità di pericoli speciali (0-1)
    }
}