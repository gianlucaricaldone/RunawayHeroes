using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.World.Obstacles
{
    /// <summary>
    /// Componente di base che definisce il tipo di ostacolo
    /// </summary>
    [Serializable]
    public struct ObstacleTypeComponent : IComponentData
    {
        // Identificatore univoco dell'ostacolo
        public ushort ObstacleID;
        
        // Categoria dell'ostacolo
        public ObstacleCategory Category;
        
        // Flag che indicano se l'ostacolo corrisponde a un tema specifico
        public bool IsUniversal;        // Ostacolo universale (appare in tutti i temi)
        public bool IsUrbanObstacle;    // Ostacolo urbano (città)
        public bool IsForestObstacle;   // Ostacolo forestale
        public bool IsTundraObstacle;   // Ostacolo della tundra
        public bool IsVolcanoObstacle;  // Ostacolo vulcanico
        public bool IsAbyssObstacle;    // Ostacolo abissale
        public bool IsVirtualObstacle;  // Ostacolo virtuale
        
        // Attributi fisici dell'ostacolo
        public float Height;            // Altezza dell'ostacolo in unità
        public float Width;             // Larghezza dell'ostacolo in unità
        public float Depth;             // Profondità dell'ostacolo in unità
        
        // Attributi di gameplay
        public bool RequiresJump;       // L'ostacolo richiede un salto per essere superato
        public bool RequiresSlide;      // L'ostacolo richiede una scivolata per essere superato
        public bool RequiresSideStep;   // L'ostacolo richiede un passo laterale
        
        // Attributi di difficoltà
        public byte DifficultyLevel;    // Livello di difficoltà dell'ostacolo (1-10)
        
        // Attributi speciali
        public bool IsBreakable;        // L'ostacolo può essere distrutto
        public bool IsSlippery;         // L'ostacolo è scivoloso
        public bool IsToxic;            // L'ostacolo è tossico
        public bool IsLava;             // L'ostacolo è costituito da lava
        public bool IsIce;              // L'ostacolo è costituito da ghiaccio
        public bool IsDigital;          // L'ostacolo è costituito da barriere digitali
        public bool IsUnderwater;       // L'ostacolo è subacqueo
        
        // Funzione helper per ottenere il prefisso dell'ID in base al tema
        public string GetIDPrefix()
        {
            if (IsUniversal) return "U";
            if (IsUrbanObstacle) return "C";
            if (IsForestObstacle) return "F";
            if (IsTundraObstacle) return "T";
            if (IsVolcanoObstacle) return "V";
            if (IsAbyssObstacle) return "A";
            if (IsVirtualObstacle) return "D";
            return "O"; // Generico
        }
        
        // Funzione helper per creare un ostacolo universale di base
        public static ObstacleTypeComponent CreateUniversal(ushort id, ObstacleCategory category, float height, float width, float depth)
        {
            return new ObstacleTypeComponent
            {
                ObstacleID = id,
                Category = category,
                IsUniversal = true,
                Height = height,
                Width = width,
                Depth = depth,
                DifficultyLevel = 1
            };
        }
    }
    
    /// <summary>
    /// Enumerazione delle categorie di ostacoli
    /// </summary>
    public enum ObstacleCategory : byte
    {
        None = 0,
        SmallBarrier = 1,     // Barriera bassa
        MediumBarrier = 2,    // Barriera media
        LargeBarrier = 3,     // Barriera alta
        Gap = 4,              // Buco/Vuoto
        HangingObject = 5,    // Oggetto sospeso
        MovingObstacle = 6,   // Ostacolo in movimento
        GroundHazard = 7,     // Pericolo al suolo
        SpecialBarrier = 8,   // Barriera che richiede abilità speciali
        Vehicle = 9,          // Veicolo (auto, camion, ecc.)
        NaturalObstacle = 10, // Ostacolo naturale (rocce, alberi, ecc.)
        ElectronicObstacle = 11, // Ostacolo elettronico
        WaterObstacle = 12,   // Ostacolo acquatico
        FireObstacle = 13,    // Ostacolo di fuoco
        IceObstacle = 14,     // Ostacolo di ghiaccio
        DigitalObstacle = 15, // Ostacolo digitale
        AreaEffect = 16,      // Effetto ad area (gas, campi elettrici, ecc.)
        SpecialEffect = 17    // Effetti speciali (glitch, distorsioni, ecc.)
    }
    
    /// <summary>
    /// Componente tag per ostacoli universali
    /// </summary>
    [Serializable]
    public struct UniversalObstacleTag : IComponentData
    {
        // Identificatore numerico dell'ostacolo
        public ushort ObstacleID;
    }
}