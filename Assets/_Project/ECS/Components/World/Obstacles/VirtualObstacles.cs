using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Definizioni dei componenti per ostacoli virtuali specifici del tema Virtual
    /// </summary>
    
    /// <summary>
    /// Tag di ostacolo virtuale per raggruppamento
    /// </summary>
    [Serializable]
    public struct VirtualObstacleTag : IComponentData { }
    
    /// <summary>
    /// D01: Barriera di dati - Richiede salto o glitch
    /// </summary>
    [Serializable]
    public struct D01_DataBarrier : IComponentData
    {
        public float Height;
        public float Integrity; // Livello di integrità (per la glitch ability)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 1,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 2.5f, 
                Width = 3.0f,
                Depth = 0.5f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = true,
                IsDigital = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D02: Firewall - Causa danno continuo
    /// </summary>
    [Serializable]
    public struct D02_Firewall : IComponentData
    {
        public float DamagePerSecond;
        public float Width;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 2,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 0.5f,
                RequiresJump = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsDigital = true,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D03: Blocco di corruzione - Cambia forma periodicamente
    /// </summary>
    [Serializable]
    public struct D03_CorruptionBlock : IComponentData
    {
        public float MorphInterval; // Intervallo di cambio forma
        public byte MorphState; // Stato attuale (0-3)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 3,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 2.0f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresSideStep = true,
                RequiresJump = true,
                DifficultyLevel = 4,
                IsBreakable = false,
                IsDigital = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D04: Glitch spaziale - Teletrasporta il giocatore
    /// </summary>
    [Serializable]
    public struct D04_SpatialGlitch : IComponentData
    {
        public float TeleportDistance; // Distanza di teletrasporto
        public byte TeleportDirection; // Direzione (0=avanti, 1=indietro, 2=lato)
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 4,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 2.0f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresSideStep = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsDigital = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D05: Picco di dati - Richiede salto
    /// </summary>
    [Serializable]
    public struct D05_DataSpike : IComponentData
    {
        public float Height;
        public float Damage;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 5,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 1.0f, 
                Width = 2.0f,
                Depth = 2.0f,
                RequiresJump = true,
                DifficultyLevel = 3,
                IsBreakable = false,
                IsDigital = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D06: Virus attivo - Si muove verso il giocatore
    /// </summary>
    [Serializable]
    public struct D06_ActiveVirus : IComponentData
    {
        public float MoveSpeed;
        public float DetectionRadius;
        public float DamageAmount;
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 6,
                Category = ObstacleCategory.MovingObstacle,
                IsVirtualObstacle = true,
                Height = 1.5f, 
                Width = 1.5f,
                Depth = 1.5f,
                RequiresSideStep = true,
                DifficultyLevel = 4,
                IsBreakable = true,
                IsDigital = true,
                IsToxic = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D07: Campo di deformazione - Inverte i controlli
    /// </summary>
    [Serializable]
    public struct D07_DistortionField : IComponentData
    {
        public float Duration; // Durata dell'effetto
        public float Radius; // Raggio d'azione
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 7,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 2.0f, 
                Width = 4.0f,
                Depth = 4.0f,
                RequiresSideStep = true,
                DifficultyLevel = 5,
                IsBreakable = false,
                IsDigital = true
            };
            return data;
        }
    }
    
    /// <summary>
    /// D08: Matrice di codice - Si ricompone in diversi pattern
    /// </summary>
    [Serializable]
    public struct D08_CodeMatrix : IComponentData
    {
        public byte PatternIndex; // Indice del pattern attuale
        public float ShiftInterval; // Intervallo di cambio pattern
        
        public static ObstacleTypeComponent GetTypeData()
        {
            var data = new ObstacleTypeComponent
            {
                ObstacleID = 8,
                Category = ObstacleCategory.DigitalObstacle,
                IsVirtualObstacle = true,
                Height = 3.0f, 
                Width = 3.0f,
                Depth = 3.0f,
                RequiresSideStep = true,
                RequiresJump = true,
                DifficultyLevel = 6,
                IsBreakable = false,
                IsDigital = true
            };
            return data;
        }
    }
}