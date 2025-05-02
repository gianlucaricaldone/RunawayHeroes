using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce un segmento di percorso in un livello runner
    /// </summary>
    [Serializable]
    public struct PathSegmentComponent : IComponentData
    {
        // Dati di posizionamento
        public float3 StartPosition;      // Posizione di inizio del segmento
        public float3 EndPosition;        // Posizione di fine del segmento
        public quaternion Rotation;       // Rotazione del segmento
        public float Length;              // Lunghezza del segmento
        public float Width;               // Larghezza del percorso
        
        // Configurazione del segmento
        public SegmentType Type;          // Tipo di segmento (piano, salita, discesa, ecc.)
        public int SegmentIndex;          // Indice progressivo del segmento nel percorso
        public int DifficultyLevel;       // Livello di difficoltà (1-10)
        
        // Tema e ambiente
        public WorldTheme Theme;          // Tema del segmento (città, foresta, ecc.)
        
        // Stato del segmento
        public bool IsActive;             // Il segmento è attualmente attivo?
        public bool IsGenerated;          // Il contenuto del segmento è già stato generato?
        public Entity NextSegment;        // Riferimento al segmento successivo
    }
    
    /// <summary>
    /// Enumerazione dei tipi di segmenti di percorso
    /// </summary>
    public enum SegmentType
    {
        Straight,       // Segmento piano dritto
        Uphill,         // Salita
        Downhill,       // Discesa
        Curve,          // Curva
        Jump,           // Salto (con gap)
        Narrow,         // Corridoio stretto
        Wide,           // Area ampia
        Hazard,         // Area con pericoli (lava, acqua, ecc.)
        Checkpoint,     // Checkpoint
        Challenge       // Segmento sfida
    }
    
    /// <summary>
    /// Enumerazione dei temi di mondo disponibili
    /// </summary>
    public enum WorldTheme
    {
        City,           // Città in caos
        Forest,         // Foresta primordiale
        Tundra,         // Tundra eterna
        Volcano,        // Inferno di lava
        Abyss,          // Abissi inesplorati
        Virtual         // Realtà virtuale
    }
}