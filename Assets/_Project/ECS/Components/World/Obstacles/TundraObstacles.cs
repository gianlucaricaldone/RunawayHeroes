using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli della tundra specifici del tema Tundra
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo della tundra per raggruppamento
    /// </summary>
    [Serializable]
    public struct TundraObstacleTag : IComponentData { }
    
    /// <summary>
    /// T01: Muro di ghiaccio - Richiede salto o scivolata
    /// </summary>
    [Serializable]
    public struct T01_IceWall : IComponentData
    {
        public float Thickness;
        public float Integrity; // 0-100, determina quanto è facile rompere il ghiaccio
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.IceObstacle,
                IsTundraObstacle = true,
                Height = 2.0f, 
                Width = 3.0f,
                Depth = 0.5f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = true,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T02: Blocco di ghiaccio - Richiede salto
    /// </summary>
    [Serializable]
    public struct T02_IceBlock : IComponentData
    {
        public bool IsBreakable;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.IceObstacle,
                IsTundraObstacle = true,
                Height = 1.2f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresJump = true,
                DifficultyLevel = 2,
                IsBreakable = true,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T03: Superficie ghiacciata - Causa scivolamento
    /// </summary>
    [Serializable]
    public struct T03_IcySurface : IComponentData
    {
        public float SlipperinessFactor; // 0-1, quanto è scivolosa
        public float Length; // Lunghezza della superficie
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.GroundHazard,
                IsTundraObstacle = true,
                Height = 0.1f, 
                Width = 3.0f,
                Depth = 5.0f,
                RequiresSideStep = false,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsIce = true,
                IsSlippery = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T04: Stalattiti di ghiaccio - Cadono quando ci si avvicina
    /// </summary>
    [Serializable]
    public struct T04_IceStalactite : IComponentData
    {
        public float FallDelay; // Ritardo prima della caduta
        public float TriggerDistance; // Distanza di attivazione
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.HangingObject,
                IsTundraObstacle = true,
                Height = 2.0f, 
                Width = 0.7f,
                Depth = 0.7f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = true,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T05: Geyser gelato - Erutta periodicamente causando danno da gelo
    /// </summary>
    [Serializable]
    public struct T05_FrostGeyser : IComponentData
    {
        public float EruptionInterval; // Intervallo tra le eruzioni
        public float EruptionDuration; // Durata dell'eruzione
        public float DamagePerSecond; // Danno da gelo al secondo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.GroundHazard,
                IsTundraObstacle = true,
                Height = 0.5f, 
                Width = 1.5f,
                Depth = 1.5f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T06: Valanga - Richiede reazione rapida
    /// </summary>
    [Serializable]
    public struct T06_Avalanche : IComponentData
    {
        public float Speed; // Velocità della valanga
        public float Width; // Larghezza della valanga
        public float WarningTime; // Tempo di preavviso (secondi)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.MovingObstacle,
                IsTundraObstacle = true,
                Height = 3.0f, 
                Width = 6.0f,
                Depth = 4.0f,
                RequiresSideStep = true,
                RequiresSlide = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T07: Ponte di ghiaccio - Si rompe dopo un po'
    /// </summary>
    [Serializable]
    public struct T07_IceBridge : IComponentData
    {
        public float BreakTime; // Tempo prima che si rompa
        public float Integrity; // Integrità strutturale
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.GroundHazard,
                IsTundraObstacle = true,
                Height = 0.2f, 
                Width = 2.0f,
                Depth = 4.0f,
                RequiresJump = false,
                DifficultyLevel = 3,
                IsBreakable = true,
                IsIce = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// T08: Vento gelido - Spinge il giocatore e causa danno da freddo
    /// </summary>
    [Serializable]
    public struct T08_FreezingWind : IComponentData
    {
        public float Force; // Forza del vento
        public float Direction; // Direzione in gradi
        public float DamagePerSecond; // Danno da freddo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.GroundHazard,
                IsTundraObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 8.0f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsIce = true
            };
            return data;
        }
    }
}