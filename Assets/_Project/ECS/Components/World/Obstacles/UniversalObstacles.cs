using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli universali che appaiono in tutti i temi
    /// </summary>
    
    /// <summary>
    /// U01: Barriera bassa - Richiede salto
    /// </summary>
    [Serializable]
    public struct U01_LowBarrier : IComponentData
    {
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(1, ObstacleCategory.SmallBarrier, 1.0f, 3.0f, 0.5f);
            data.RequiresJump = true;
            data.DifficultyLevel = 1;
            return data;
        }
    }
    
    /// <summary>
    /// U02: Barriera alta - Richiede scivolata
    /// </summary>
    [Serializable]
    public struct U02_HighBarrier : IComponentData
    {
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(2, ObstacleCategory.MediumBarrier, 2.5f, 3.0f, 0.5f);
            data.RequiresSlide = true;
            data.DifficultyLevel = 2;
            return data;
        }
    }
    
    /// <summary>
    /// U03: Gap/Buco - Richiede salto lungo
    /// </summary>
    [Serializable]
    public struct U03_Gap : IComponentData
    {
        public float Width;  // Larghezza del vuoto
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(3, ObstacleCategory.Gap, 0.0f, 2.0f, 2.0f);
            data.RequiresJump = true;
            data.DifficultyLevel = 3;
            return data;
        }
    }
    
    /// <summary>
    /// U04: Ostacolo laterale - Richiede passo laterale
    /// </summary>
    [Serializable]
    public struct U04_SideObstacle : IComponentData
    {
        public bool IsLeftSide;  // Indica se l'ostacolo è sul lato sinistro
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(4, ObstacleCategory.MovingObstacle, 2.0f, 1.5f, 2.0f);
            data.RequiresSideStep = true;
            data.DifficultyLevel = 2;
            return data;
        }
    }
    
    /// <summary>
    /// U05: Oggetto sospeso - Richiede scivolata
    /// </summary>
    [Serializable]
    public struct U05_HangingObject : IComponentData
    {
        public float Height;  // Altezza dal suolo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(5, ObstacleCategory.HangingObject, 1.0f, 3.0f, 0.5f);
            data.RequiresSlide = true;
            data.DifficultyLevel = 2;
            return data;
        }
    }
    
    /// <summary>
    /// U06: Barriera doppia - Richiede salto e scivolata in sequenza
    /// </summary>
    [Serializable]
    public struct U06_DoubleBarrier : IComponentData
    {
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(6, ObstacleCategory.SmallBarrier, 1.0f, 3.0f, 2.0f);
            data.RequiresJump = true;
            data.RequiresSlide = true;
            data.DifficultyLevel = 4;
            return data;
        }
    }
    
    /// <summary>
    /// U07: Spuntoni a terra - Richiede salto
    /// </summary>
    [Serializable]
    public struct U07_FloorSpikes : IComponentData
    {
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(7, ObstacleCategory.GroundHazard, 0.5f, 2.0f, 2.0f);
            data.RequiresJump = true;
            data.DifficultyLevel = 3;
            data.IsToxic = true;
            return data;
        }
    }
    
    /// <summary>
    /// U08: Contenitore di energia - Può essere distrutto
    /// </summary>
    [Serializable]
    public struct U08_PowerContainer : IComponentData
    {
        public float Energy;  // Quantità di energia contenuta
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = ObstacleTypeComponent.CreateUniversal(8, ObstacleCategory.SmallBarrier, 1.2f, 1.2f, 1.2f);
            data.IsBreakable = true;
            data.DifficultyLevel = 2;
            return data;
        }
    }
}