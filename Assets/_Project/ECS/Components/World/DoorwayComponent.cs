using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce un punto di connessione tra stanze
    /// </summary>
    [Serializable]
    public struct DoorwayComponent : IComponentData
    {
        // Dati di posizionamento
        public float3 Position;          // Posizione del collegamento
        public quaternion Rotation;      // Rotazione
        public DoorwayDirection Direction; // Direzione del collegamento
        
        // Riferimenti
        public Entity SourceRoom;        // Entità stanza di origine
        public Entity TargetRoom;        // Entità stanza di destinazione (se già connessa)
        
        // Proprietà
        public DoorwayType Type;         // Tipo di collegamento
        public bool IsConnected;         // È già connessa ad un'altra stanza?
        public bool IsLocked;            // È bloccata?
        public int KeyID;                // ID della chiave necessaria (se bloccata)
    }
    
    /// <summary>
    /// Enumerazione che definisce i possibili tipi di doorway
    /// </summary>
    public enum DoorwayType
    {
        Standard,        // Porta standard
        OneWay,          // Solo ingresso
        Hidden,          // Nascosta
        Boss,            // Ingresso stanza boss
        Challenge,       // Ingresso stanza sfida
        AbilityGated,    // Richiede un'abilità specifica
        KeyGated         // Richiede una chiave
    }
    
    /// <summary>
    /// Enumerazione che definisce le possibili direzioni di una doorway
    /// </summary>
    public enum DoorwayDirection
    {
        North,
        East,
        South,
        West,
        Up,
        Down
    }
}