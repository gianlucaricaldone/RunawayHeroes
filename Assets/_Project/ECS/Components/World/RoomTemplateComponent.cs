using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce un template di stanza utilizzabile nella generazione procedurale
    /// </summary>
    [Serializable]
    public struct RoomTemplateComponent : IComponentData
    {
        // Dati del template
        public int TemplateID;           // ID univoco del template
        public int2 Size;                // Dimensione in X,Z
        public int MinDoorways;          // Numero minimo di doorway
        public int MaxDoorways;          // Numero massimo di doorway
        
        // Proprietà speciali
        public RoomType Type;            // Tipo di stanza (standard, tesoro, boss, ecc)
        public int Difficulty;           // Livello di difficoltà (1-10)
        
        // Flags
        public bool IsStartRoom;         // È una stanza iniziale?
        public bool IsEndRoom;           // È una stanza finale?
        public bool RequiresSpecificAbility; // Richiede un'abilità specifica?
        public int RequiredAbilityType;  // Tipo di abilità richiesta, se necessaria
    }
    
    /// <summary>
    /// Enumerazione che definisce i possibili tipi di stanza
    /// </summary>
    public enum RoomType
    {
        Standard,       // Stanza standard
        Treasure,       // Stanza del tesoro
        Challenge,      // Stanza sfida
        Boss,           // Stanza boss
        Secret,         // Stanza segreta
        Checkpoint,     // Checkpoint
        Puzzle,         // Puzzle
        Hub             // Hub centrale
    }
}