using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli forestali specifici del tema Forest
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo forestale per raggruppamento
    /// </summary>
    [Serializable]
    public struct ForestObstacleTag : IComponentData { }
    
    /// <summary>
    /// F01: Tronco caduto - Richiede salto
    /// </summary>
    [Serializable]
    public struct F01_FallenLog : IComponentData
    {
        public float Length;
        public byte RottenLevel; // 0-3, influenza la difficoltà per romperlo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.NaturalObstacle,
                IsForestObstacle = true,
                Height = 1.0f, 
                Width = 3.5f,
                Depth = 1.0f,
                RequiresJump = true,
                DifficultyLevel = 2,
                IsBreakable = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// F02: Albero caduto - Richiede salto grande
    /// </summary>
    [Serializable]
    public struct F02_FallenTree : IComponentData
    {
        public bool HasBranches; // Rami che ne aumentano la difficoltà
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.NaturalObstacle,
                IsForestObstacle = true,
                Height = 1.5f, 
                Width = 4.0f,
                Depth = 1.5f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// F03: Radici sporgenti - Richiede salto
    /// </summary>
    [Serializable]
    public struct F03_ProtrudingRoots : IComponentData
    {
        public float Height;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.GroundHazard,
                IsForestObstacle = true,
                Height = 0.7f, 
                Width = 2.5f,
                Depth = 1.5f,
                RequiresJump = true,
                DifficultyLevel = 2,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// F04: Pianta velenosa - Causa danno continuo se toccata
    /// </summary>
    [Serializable]
    public struct F04_PoisonousPlant : IComponentData
    {
        public float ToxicityLevel; // Livello di danno
        public float EffectRadius;  // Raggio d'effetto
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.GroundHazard,
                IsForestObstacle = true,
                Height = 1.2f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = true,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// F05: Liane pendenti - Richiede scivolata
    /// </summary>
    [Serializable]
    public struct F05_HangingVines : IComponentData
    {
        public float Length;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.HangingObject,
                IsForestObstacle = true,
                Height = 2.5f, 
                Width = 3.0f,
                Depth = 1.0f,
                RequiresSlide = true,
                DifficultyLevel = 2,
                IsBreakable = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// F06: Nido di vespe - Attiva sciame se avvicinato
    /// </summary>
    [Serializable]
    public struct F06_WaspNest : IComponentData
    {
        public float ActivationDistance; // Distanza alla quale si attiva
        public int WaspCount; // Numero di vespe che vengono rilasciate
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.HangingObject,
                IsForestObstacle = true,
                Height = 0.8f, 
                Width = 0.8f,
                Depth = 0.8f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = true,
                IsToxic = true // Danno continuo dalle vespe
            };
            return data;
        }
    }
    
    /// <summary>
    /// F07: Fossa di fango - Rallenta il movimento
    /// </summary>
    [Serializable]
    public struct F07_MudPit : IComponentData
    {
        public float SlowFactor; // Fattore di rallentamento (0-1)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.GroundHazard,
                IsForestObstacle = true,
                Height = 0.2f, 
                Width = 4.0f,
                Depth = 2.5f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsSlippery = true // Effetto rallentamento
            };
            return data;
        }
    }
    
    /// <summary>
    /// F08: Tronco oscillante - Si muove da lato a lato
    /// </summary>
    [Serializable]
    public struct F08_SwingingLog : IComponentData
    {
        public float SwingSpeed; // Velocità dell'oscillazione
        public float SwingRange; // Ampiezza dell'oscillazione
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.MovingObstacle,
                IsForestObstacle = true,
                Height = 1.0f, 
                Width = 3.0f,
                Depth = 1.0f,
                RequiresSideStep = true,
                RequiresJump = true,
                DifficultyLevel = 5,
                IsBreakable = false
            };
            return data;
        }
    }
}