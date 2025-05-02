using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che rappresenta una stanza generata all'interno di un livello
    /// </summary>
    [Serializable]
    public struct RoomComponent : IComponentData
    {
        // Dati di posizionamento
        public float3 Position;           // Posizione della stanza nel mondo
        public quaternion Rotation;       // Rotazione della stanza
        public int2 GridPosition;         // Posizione nella griglia di generazione
        public int2 Size;                 // Dimensione in X,Z
        
        // Riferimenti
        public int TemplateID;            // ID del template utilizzato
        public Entity LevelEntity;        // Riferimento all'entità livello
        
        // Stato
        public RoomState State;           // Stato attuale della stanza
        public bool IsVisited;            // La stanza è stata visitata?
        public bool IsMapped;             // La stanza è stata mappata?
        public bool ContainsCollectibles; // Contiene oggetti collezionabili?
        public bool ContainsEnemies;      // Contiene nemici?
        
        // Buffer di doorway
        // Nota: in ECS i buffer vengono tipicamente implementati 
        // come IBufferElementData separati, ma qui li rappresentiamo concettualmente
    }
    
    /// <summary>
    /// Enumerazione che definisce i possibili stati di una stanza
    /// </summary>
    public enum RoomState
    {
        Inactive,       // Non ancora attiva
        Active,         // Attualmente attiva
        Cleared,        // Completata (nemici sconfitti)
        Locked,         // Bloccata
        Secret          // Segreta (non visibile sulla mappa)
    }
}