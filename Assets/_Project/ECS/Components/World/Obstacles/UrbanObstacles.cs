using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli urbani specifici del tema City
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo urbano per raggruppamento
    /// </summary>
    [Serializable]
    public struct UrbanObstacleTag : IComponentData { }
    
    /// <summary>
    /// C01: Auto danneggiata - Richiede salto o scivolata
    /// </summary>
    [Serializable]
    public struct C01_Car : IComponentData
    {
        public byte CarType;  // Tipo di auto (0-3)
        public bool IsCrashed;  // Se l'auto è danneggiata
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.Vehicle,
                IsUrbanObstacle = true,
                Height = 1.5f, 
                Width = 2.0f,
                Depth = 4.5f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// C02: Furgone danneggiato - Richiede salto alto o aggiramento
    /// </summary>
    [Serializable]
    public struct C02_Van : IComponentData
    {
        public bool IsTipped;  // Se il furgone è ribaltato
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.Vehicle,
                IsUrbanObstacle = true,
                Height = 2.2f, 
                Width = 2.5f,
                Depth = 6.0f,
                RequiresJump = true,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// C03: Drone di sorveglianza - Si muove in pattern prevedibili
    /// </summary>
    [Serializable]
    public struct C03_SurveillanceDrone : IComponentData
    {
        public float MovementSpeed;
        public float DetectionRadius;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.MovingObstacle,
                IsUrbanObstacle = true,
                Height = 0.8f, 
                Width = 0.8f,
                Depth = 0.8f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// C04: Barricata di polizia - Richiede salto o scivolata
    /// </summary>
    [Serializable]
    public struct C04_PoliceBarricade : IComponentData
    {
        public bool HasFlashingLights;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.LargeBarrier,
                IsUrbanObstacle = true,
                Height = 1.2f, 
                Width = 3.0f,
                Depth = 1.0f,
                RequiresJump = true,
                DifficultyLevel = 2,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// C05: Idrante rotto - Crea getto d'acqua che spinge lateralmente
    /// </summary>
    [Serializable]
    public struct C05_FireHydrant : IComponentData
    {
        public float WaterForce;
        public bool IsSpraying;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.GroundHazard,
                IsUrbanObstacle = true,
                Height = 1.0f, 
                Width = 0.7f,
                Depth = 0.7f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// C06: Veicolo fuori controllo - Si muove attraverso il percorso
    /// </summary>
    [Serializable]
    public struct C06_RunawayCar : IComponentData
    {
        public float Speed;
        public byte VehicleType;  // 0=auto, 1=camion, 2=bus
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.Vehicle,
                IsUrbanObstacle = true,
                Height = 1.5f, 
                Width = 2.0f,
                Depth = 4.8f,
                RequiresJump = true,
                RequiresSideStep = true,
                DifficultyLevel = 5,
                IsBreakable = false
            };
            return data;
        }
    }
    
    /// <summary>
    /// C07: Cantiere stradale - Combinazione di coni e barriere
    /// </summary>
    [Serializable]
    public struct C07_RoadConstruction : IComponentData
    {
        public byte ConstructionType;  // Layout del cantiere
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.SmallBarrier,
                IsUrbanObstacle = true,
                Height = 1.2f, 
                Width = 3.5f,
                Depth = 3.0f,
                RequiresJump = true,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// C08: Insegna al neon caduta - Può essere elettrificata
    /// </summary>
    [Serializable]
    public struct C08_FallenNeonSign : IComponentData
    {
        public bool IsElectrified;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.GroundHazard,
                IsUrbanObstacle = true,
                Height = 0.8f, 
                Width = 3.0f,
                Depth = 1.0f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsToxic = true  // Elettrificata
            };
            return data;
        }
    }
}