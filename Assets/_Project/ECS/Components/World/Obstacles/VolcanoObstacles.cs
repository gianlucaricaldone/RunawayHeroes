using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli vulcanici specifici del tema Volcano
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo vulcanico per raggruppamento
    /// </summary>
    [Serializable]
    public struct VolcanoObstacleTag : IComponentData { }
    
    /// <summary>
    /// V01: Pozza di lava - Causa danno continuo
    /// </summary>
    [Serializable]
    public struct V01_LavaPool : IComponentData
    {
        public float Temperature; // Influenza il danno
        public float DamagePerSecond;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.FireObstacle,
                IsVolcanoObstacle = true,
                Height = 0.3f, 
                Width = 3.0f,
                Depth = 3.0f,
                RequiresJump = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsLava = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V02: Geyser di lava - Erutta periodicamente
    /// </summary>
    [Serializable]
    public struct V02_LavaGeyser : IComponentData
    {
        public float EruptionInterval; // Tempo tra eruzioni
        public float EruptionDuration; // Durata dell'eruzione
        public float Height; // Altezza del getto
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.FireObstacle,
                IsVolcanoObstacle = true,
                Height = 0.2f, 
                Width = 1.5f,
                Depth = 1.5f,
                RequiresSideStep = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsLava = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V03: Roccia vulcanica - Richiede salto o scivolata
    /// </summary>
    [Serializable]
    public struct V03_VolcanicRock : IComponentData
    {
        public bool IsHot; // Se è incandescente (causa danno)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.NaturalObstacle,
                IsVolcanoObstacle = true,
                Height = 1.5f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V04: Parete di fiamme - Richiede salto speciale
    /// </summary>
    [Serializable]
    public struct V04_FireWall : IComponentData
    {
        public float Width;
        public float Height;
        public float DamagePerSecond;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.FireObstacle,
                IsVolcanoObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 0.5f,
                RequiresJump = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsLava = true // In realtà è fuoco, ma usiamo lo stesso flag
            };
            return data;
        }
    }
    
    /// <summary>
    /// V05: Bomba vulcanica - Esplode dopo un po'
    /// </summary>
    [Serializable]
    public struct V05_VolcanicBomb : IComponentData
    {
        public float ExplosionDelay; // Tempo prima dell'esplosione
        public float ExplosionRadius; // Raggio dell'esplosione
        public float DamageAmount; // Danno dell'esplosione
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.FireObstacle,
                IsVolcanoObstacle = true,
                Height = 0.8f, 
                Width = 0.8f,
                Depth = 0.8f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = true,
                IsLava = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V06: Cenere vulcanica - Riduce la visibilità
    /// </summary>
    [Serializable]
    public struct V06_VolcanicAsh : IComponentData
    {
        public float Density; // Densità della cenere (influenza visibilità)
        public float Length; // Lunghezza della zona con cenere
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.GroundHazard,
                IsVolcanoObstacle = true,
                Height = 3.0f, 
                Width = 5.0f,
                Depth = 5.0f,
                RequiresSideStep = false,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V07: Flusso piroclastico - Avanza rapidamente
    /// </summary>
    [Serializable]
    public struct V07_PyroclasticFlow : IComponentData
    {
        public float Speed; // Velocità di avanzamento
        public float Width; // Larghezza del flusso
        public float DamagePerSecond; // Danno al secondo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.FireObstacle,
                IsVolcanoObstacle = true,
                Height = 2.5f, 
                Width = 5.0f,
                Depth = 3.0f,
                RequiresSideStep = true,
                RequiresSlide = true,
                DifficultyLevel = 6,
                IsBreakable = false,
                IsLava = true,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// V08: Terreno instabile - Si sgretola dopo un po'
    /// </summary>
    [Serializable]
    public struct V08_UnstableGround : IComponentData
    {
        public float BreakTime; // Tempo prima che si sgretoli
        public float WarningTime; // Tempo di avviso prima di sgretolarsi
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.GroundHazard,
                IsVolcanoObstacle = true,
                Height = 0.2f, 
                Width = 3.0f,
                Depth = 3.0f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = true
            };
            return data;
        }
    }
}