using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.World
{
    /// <summary>
    /// Componente che definisce la configurazione per la generazione di livelli randomizzati
    /// </summary>
    [Serializable]
    public struct RandomLevelConfigComponent : IComponentData
    {
        // Parametri di configurazione
        public int MinRooms;        // Numero minimo di stanze
        public int MaxRooms;        // Numero massimo di stanze
        public int MinRoomSize;     // Dimensione minima di una stanza
        public int MaxRoomSize;     // Dimensione massima di una stanza
        public int CorridorWidth;   // Larghezza dei corridoi
        public float BranchingProbability; // Probabilità di ramificazione (0-1)
        public int Seed;            // Seed per la generazione casuale
        
        // Proprietà tema-specifiche
        public int WorldTheme;      // ID del tema del mondo (Città, Foresta, ecc.)
        
        // Flags
        public bool UseLoops;       // Consenti percorsi ad anello
        public bool EnsureConnectivity; // Garantisci che tutte le stanze siano raggiungibili
    }
}