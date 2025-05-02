using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli abissali specifici del tema Abyss
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo abissale per raggruppamento
    /// </summary>
    [Serializable]
    public struct AbyssObstacleTag : IComponentData { }
    
    /// <summary>
    /// A01: Sezione subacquea - Richiede nuoto e gestione dell'ossigeno
    /// </summary>
    [Serializable]
    public struct A01_UnderwaterSection : IComponentData
    {
        public float Length; // Lunghezza della sezione
        public float Depth; // Profondità
        public float CurrentStrength; // Forza della corrente (0-1)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.WaterObstacle,
                IsAbyssObstacle = true,
                Height = 3.0f, 
                Width = 4.0f,
                Depth = 5.0f,
                RequiresJump = false,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsUnderwater = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A02: Corrente abissale - Spinge in una direzione
    /// </summary>
    [Serializable]
    public struct A02_AbyssalCurrent : IComponentData
    {
        public float Strength; // Forza della corrente
        public float Direction; // Direzione della corrente in gradi
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.WaterObstacle,
                IsAbyssObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 6.0f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsUnderwater = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A03: Tentacoli oscillanti - Richiede movimento preciso
    /// </summary>
    [Serializable]
    public struct A03_SwingingTentacles : IComponentData
    {
        public float SwingFrequency; // Frequenza di oscillazione
        public float SwingAmplitude; // Ampiezza di oscillazione
        public int TentacleCount; // Numero di tentacoli
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.MovingObstacle,
                IsAbyssObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 1.0f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsUnderwater = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A04: Alga avviluppante - Rallenta il movimento
    /// </summary>
    [Serializable]
    public struct A04_TanglingSeaweed : IComponentData
    {
        public float SlowFactor; // Fattore di rallentamento
        public float BreakForce; // Forza necessaria per liberarsi
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.WaterObstacle,
                IsAbyssObstacle = true,
                Height = 2.5f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = true,
                IsUnderwater = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A05: Medusa abissale - Emette scariche elettriche
    /// </summary>
    [Serializable]
    public struct A05_AbyssalJellyfish : IComponentData
    {
        public float StingRadius; // Raggio della scarica
        public float DamageAmount; // Quantità di danno
        public float MoveSpeed; // Velocità di movimento
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.MovingObstacle,
                IsAbyssObstacle = true,
                Height = 1.5f, 
                Width = 1.5f,
                Depth = 1.5f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = true,
                IsUnderwater = true,
                IsToxic = true // Per le scariche elettriche
            };
            return data;
        }
    }
    
    /// <summary>
    /// A06: Bolla di gas tossico - Riduce l'ossigeno
    /// </summary>
    [Serializable]
    public struct A06_ToxicGasBubble : IComponentData
    {
        public float Size; // Dimensione della bolla
        public float ToxicityLevel; // Livello di tossicità
        public float ExpansionRate; // Velocità di espansione
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.WaterObstacle,
                IsAbyssObstacle = true,
                Height = 2.0f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresSideStep = true,
                DifficultyLevel = 5,
                IsBreakable = true,
                IsUnderwater = true,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A07: Voragine abissale - Richiede salto lungo
    /// </summary>
    [Serializable]
    public struct A07_AbyssalChasm : IComponentData
    {
        public float Width; // Larghezza della voragine
        public float Depth; // Profondità (visiva)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.Gap,
                IsAbyssObstacle = true,
                Height = 10.0f, // Profondità visiva
                Width = 3.0f,
                Depth = 2.0f,
                RequiresJump = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsUnderwater = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// A08: Spruzzo di inchiostro - Riduce la visibilità
    /// </summary>
    [Serializable]
    public struct A08_InkSpray : IComponentData
    {
        public float BlindnessDuration; // Durata della cecità
        public float BlindnessIntensity; // Intensità dell'oscuramento
        public float Radius; // Raggio dello spruzzo
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.WaterObstacle,
                IsAbyssObstacle = true,
                Height = 2.0f, 
                Width = 3.0f,
                Depth = 3.0f,
                RequiresSideStep = true,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsUnderwater = true
            };
            return data;
        }
    }
}