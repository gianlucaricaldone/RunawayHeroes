using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Componente che definisce il comportamento di pattugliamento di un nemico.
    /// Gestisce il percorso di pattugliamento, le tempistiche e le configurazioni per
    /// i nemici che seguono percorsi predefiniti.
    /// </summary>
    [Serializable]
    public struct PatrolComponent : IComponentData
    {
        // Punti di pattugliamento
        public float3 StartPoint;         // Punto di partenza del pattugliamento
        public float3 EndPoint;           // Punto finale del pattugliamento
        public byte CurrentWaypointIndex; // Indice del waypoint corrente
        
        // Configurazione del pattugliamento
        public float WaypointReachedDistance; // Distanza per considerare un waypoint raggiunto
        public float PatrolSpeed;             // Velocità di pattugliamento
        public float WaitTimeAtWaypoint;      // Tempo di attesa a ciascun waypoint
        
        // Stato corrente
        public float WaitTimer;           // Timer di attesa nel waypoint corrente
        public bool IsWaiting;            // Se è in attesa ad un waypoint
        public bool IsCircular;           // Se il percorso è circolare (continua all'infinito)
        public bool IsReversing;          // Se sta percorrendo il percorso al contrario
        
        // Parametri avanzati
        public float LookAheadDistance;   // Distanza di "look ahead" per rotazione
        public float RotationSpeed;       // Velocità di rotazione
    }
}
