using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce la configurazione di difficoltà per ciascun tema di mondo.
    /// Consente di scalare la difficoltà in modo appropriato per ogni ambiente di gioco.
    /// </summary>
    [Serializable]
    public struct WorldDifficultyConfigComponent : IComponentData
    {
        // Difficoltà di base per ciascun tema di mondo (1-10)
        public int TutorialBaseDifficulty;  // Difficoltà di base per il tutorial
        public int CityBaseDifficulty;      // Difficoltà di base per City
        public int ForestBaseDifficulty;    // Difficoltà di base per Forest
        public int TundraBaseDifficulty;    // Difficoltà di base per Tundra
        public int VolcanoBaseDifficulty;   // Difficoltà di base per Volcano
        public int AbyssBaseDifficulty;     // Difficoltà di base per Abyss
        public int VirtualBaseDifficulty;   // Difficoltà di base per Virtual
        
        // Fattori di scala per elementi di gioco per ciascun tema (0-2)
        // I valori inferiori a 1 riducono la difficoltà, i valori superiori la aumentano
        
        // Scala per densità degli ostacoli
        public float TutorialObstacleDensityScale;
        public float CityObstacleDensityScale;
        public float ForestObstacleDensityScale;
        public float TundraObstacleDensityScale;
        public float VolcanoObstacleDensityScale;
        public float AbyssObstacleDensityScale;
        public float VirtualObstacleDensityScale;
        
        // Scala per densità dei nemici
        public float TutorialEnemyDensityScale;
        public float CityEnemyDensityScale;
        public float ForestEnemyDensityScale;
        public float TundraEnemyDensityScale;
        public float VolcanoEnemyDensityScale;
        public float AbyssEnemyDensityScale;
        public float VirtualEnemyDensityScale;
        
        // Scala per velocità di incremento della difficoltà
        public float TutorialDifficultyRampScale;
        public float CityDifficultyRampScale;
        public float ForestDifficultyRampScale;
        public float TundraDifficultyRampScale;
        public float VolcanoDifficultyRampScale;
        public float AbyssDifficultyRampScale;
        public float VirtualDifficultyRampScale;
        
        // Proprietà speciali
        public bool EnableTutorialSafezones;  // Attiva zone sicure nel tutorial
        public float SpecialHazardReductionInTutorial; // Riduzione dei pericoli speciali nel tutorial (0-1)
        
        /// <summary>
        /// Restituisce la difficoltà di base per un tema specifico
        /// </summary>
        public int GetBaseDifficultyForTheme(WorldTheme theme)
        {
            return theme switch
            {
                WorldTheme.City => CityBaseDifficulty,
                WorldTheme.Forest => ForestBaseDifficulty,
                WorldTheme.Tundra => TundraBaseDifficulty,
                WorldTheme.Volcano => VolcanoBaseDifficulty,
                WorldTheme.Abyss => AbyssBaseDifficulty,
                WorldTheme.Virtual => VirtualBaseDifficulty,
                _ => 5 // Default medio
            };
        }
        
        /// <summary>
        /// Restituisce il fattore di scala della densità degli ostacoli per un tema specifico
        /// </summary>
        public float GetObstacleDensityScaleForTheme(WorldTheme theme, bool isTutorial = false)
        {
            if (isTutorial) return TutorialObstacleDensityScale;
            
            return theme switch
            {
                WorldTheme.City => CityObstacleDensityScale,
                WorldTheme.Forest => ForestObstacleDensityScale,
                WorldTheme.Tundra => TundraObstacleDensityScale,
                WorldTheme.Volcano => VolcanoObstacleDensityScale,
                WorldTheme.Abyss => AbyssObstacleDensityScale,
                WorldTheme.Virtual => VirtualObstacleDensityScale,
                _ => 1.0f // Default neutro
            };
        }
        
        /// <summary>
        /// Restituisce il fattore di scala della densità dei nemici per un tema specifico
        /// </summary>
        public float GetEnemyDensityScaleForTheme(WorldTheme theme, bool isTutorial = false)
        {
            if (isTutorial) return TutorialEnemyDensityScale;
            
            return theme switch
            {
                WorldTheme.City => CityEnemyDensityScale,
                WorldTheme.Forest => ForestEnemyDensityScale,
                WorldTheme.Tundra => TundraEnemyDensityScale,
                WorldTheme.Volcano => VolcanoEnemyDensityScale,
                WorldTheme.Abyss => AbyssEnemyDensityScale,
                WorldTheme.Virtual => VirtualEnemyDensityScale,
                _ => 1.0f // Default neutro
            };
        }
        
        /// <summary>
        /// Restituisce il fattore di scala del ramp (incremento) della difficoltà per un tema specifico
        /// </summary>
        public float GetDifficultyRampScaleForTheme(WorldTheme theme, bool isTutorial = false)
        {
            if (isTutorial) return TutorialDifficultyRampScale;
            
            return theme switch
            {
                WorldTheme.City => CityDifficultyRampScale,
                WorldTheme.Forest => ForestDifficultyRampScale,
                WorldTheme.Tundra => TundraDifficultyRampScale,
                WorldTheme.Volcano => VolcanoDifficultyRampScale,
                WorldTheme.Abyss => AbyssDifficultyRampScale,
                WorldTheme.Virtual => VirtualDifficultyRampScale,
                _ => 1.0f // Default neutro
            };
        }
        
        /// <summary>
        /// Crea una configurazione di difficoltà con valori predefiniti
        /// </summary>
        public static WorldDifficultyConfigComponent CreateDefault()
        {
            return new WorldDifficultyConfigComponent
            {
                // Difficoltà di base
                TutorialBaseDifficulty = 1,
                CityBaseDifficulty = 3,
                ForestBaseDifficulty = 4, 
                TundraBaseDifficulty = 5,
                VolcanoBaseDifficulty = 7,
                AbyssBaseDifficulty = 8,
                VirtualBaseDifficulty = 6,
                
                // Scala densità ostacoli
                TutorialObstacleDensityScale = 0.4f,
                CityObstacleDensityScale = 0.7f,
                ForestObstacleDensityScale = 0.9f,
                TundraObstacleDensityScale = 1.0f,
                VolcanoObstacleDensityScale = 1.2f,
                AbyssObstacleDensityScale = 1.3f,
                VirtualObstacleDensityScale = 1.1f,
                
                // Scala densità nemici
                TutorialEnemyDensityScale = 0.3f,
                CityEnemyDensityScale = 0.8f,
                ForestEnemyDensityScale = 0.9f,
                TundraEnemyDensityScale = 1.0f,
                VolcanoEnemyDensityScale = 1.3f,
                AbyssEnemyDensityScale = 1.2f,
                VirtualEnemyDensityScale = 1.1f,
                
                // Scala incremento difficoltà
                TutorialDifficultyRampScale = 0.3f,
                CityDifficultyRampScale = 0.7f,
                ForestDifficultyRampScale = 0.8f,
                TundraDifficultyRampScale = 1.0f,
                VolcanoDifficultyRampScale = 1.2f,
                AbyssDifficultyRampScale = 1.3f,
                VirtualDifficultyRampScale = 1.1f,
                
                // Proprietà speciali
                EnableTutorialSafezones = true,
                SpecialHazardReductionInTutorial = 0.8f
            };
        }
    }
}