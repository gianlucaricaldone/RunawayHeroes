// Path: ObstacleSetup.cs
using System;
using Unity.Mathematics;
using UnityEngine;

namespace RunawayHeroes.Gameplay
{
    /// <summary>
    /// Definisce i possibili posizionamenti per gli ostacoli
    /// </summary>
    public enum ObstaclePlacement : byte
    {
        /// <summary>
        /// Posiziona l'ostacolo al centro della corsia
        /// </summary>
        Center = 0,
        
        /// <summary>
        /// Posiziona l'ostacolo sul lato sinistro della corsia
        /// </summary>
        Left = 1,
        
        /// <summary>
        /// Posiziona l'ostacolo sul lato destro della corsia
        /// </summary>
        Right = 2,
        
        /// <summary>
        /// Posiziona gli ostacoli in posizioni casuali nella corsia
        /// </summary>
        Random = 3,
        
        /// <summary>
        /// Distribuisce gli ostacoli in un pattern attraverso la corsia
        /// </summary>
        Pattern = 4
    }
    
    /// <summary>
    /// Configurazione per un tipo di ostacolo in uno scenario
    /// </summary>
    [Serializable]
    public struct ObstacleSetup
    {
        /// <summary>
        /// Codice identificativo dell'ostacolo da spawnnare
        /// U## = Universal, C## = City, F## = Forest, ecc.
        /// </summary>
        [Tooltip("Codice identificativo dell'ostacolo (es. U01, C02, F03)")]
        public string obstacleCode;
        
        /// <summary>
        /// Numero di ostacoli di questo tipo da spawnnare
        /// </summary>
        [Tooltip("Numero di ostacoli di questo tipo da spawnnare")]
        public int count;
        
        /// <summary>
        /// Tipo di posizionamento per gli ostacoli
        /// </summary>
        [Tooltip("Come posizionare gli ostacoli (centro, lati, casuale, pattern)")]
        public ObstaclePlacement placement;
        
        /// <summary>
        /// Se true, l'altezza degli ostacoli sarà randomizzata entro un range
        /// </summary>
        [Tooltip("Altezze casuali per gli ostacoli")]
        public bool randomizeHeight;
        
        /// <summary>
        /// Range per la randomizzazione dell'altezza
        /// </summary>
        [Tooltip("Range per l'altezza casuale (min, max)")]
        public Vector2 heightRange;
        
        /// <summary>
        /// Se true, la scala degli ostacoli sarà randomizzata entro un range
        /// </summary>
        [Tooltip("Scale casuali per gli ostacoli")]
        public bool randomizeScale;
        
        /// <summary>
        /// Range per la randomizzazione della scala
        /// </summary>
        [Tooltip("Range per la scala casuale (min, max)")]
        public Vector2 scaleRange;
        
        /// <summary>
        /// Offset iniziale lungo l'asse Z rispetto all'inizio dello scenario
        /// </summary>
        [Tooltip("Distanza iniziale dal punto di inizio scenario")]
        public float startOffset;

        /// <summary>
        /// Creazione di un'istanza base di ObstacleSetup
        /// </summary>
        public static ObstacleSetup CreateDefault()
        {
            return new ObstacleSetup
            {
                obstacleCode = "U01", // Ostacolo universale base
                count = 1,
                placement = ObstaclePlacement.Center,
                randomizeHeight = false,
                heightRange = new Vector2(0.5f, 1.5f),
                randomizeScale = false,
                scaleRange = new Vector2(0.8f, 1.2f),
                startOffset = 0f
            };
        }
        
        /// <summary>
        /// Creazione di un'istanza per ostacoli da saltare
        /// </summary>
        public static ObstacleSetup CreateJumpObstacles(string code = "U02", int count = 3)
        {
            return new ObstacleSetup
            {
                obstacleCode = code,
                count = count,
                placement = ObstaclePlacement.Center,
                randomizeHeight = false,
                heightRange = new Vector2(0.3f, 0.5f),
                randomizeScale = false,
                scaleRange = new Vector2(1f, 1f),
                startOffset = 5f
            };
        }
        
        /// <summary>
        /// Creazione di un'istanza per ostacoli da scivolare sotto
        /// </summary>
        public static ObstacleSetup CreateSlideObstacles(string code = "U03", int count = 3)
        {
            return new ObstacleSetup
            {
                obstacleCode = code,
                count = count,
                placement = ObstaclePlacement.Center,
                randomizeHeight = false,
                heightRange = new Vector2(1.5f, 2f),
                randomizeScale = false,
                scaleRange = new Vector2(1f, 1f),
                startOffset = 5f
            };
        }
        
        /// <summary>
        /// Creazione di un'istanza per ostacoli da evitare con spostamenti laterali
        /// </summary>
        public static ObstacleSetup CreateSideStepObstacles(string code = "U01", int count = 5)
        {
            return new ObstacleSetup
            {
                obstacleCode = code,
                count = count,
                placement = ObstaclePlacement.Pattern,
                randomizeHeight = false,
                heightRange = new Vector2(0.8f, 1.2f),
                randomizeScale = true,
                scaleRange = new Vector2(0.8f, 1.2f),
                startOffset = 5f
            };
        }
        
        /// <summary>
        /// Creazione di un'istanza per ostacoli casuali (livelli avanzati)
        /// </summary>
        public static ObstacleSetup CreateRandomObstacles(string code = "U04", int count = 8)
        {
            return new ObstacleSetup
            {
                obstacleCode = code,
                count = count,
                placement = ObstaclePlacement.Random,
                randomizeHeight = true,
                heightRange = new Vector2(0.3f, 1.8f),
                randomizeScale = true,
                scaleRange = new Vector2(0.7f, 1.3f),
                startOffset = 5f
            };
        }
    }
}