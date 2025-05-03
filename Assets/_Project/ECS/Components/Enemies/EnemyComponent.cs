using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Componente principale per tutte le entità nemiche.
    /// Contiene proprietà base come velocità, danno e stato.
    /// </summary>
    [Serializable]
    public struct EnemyComponent : IComponentData
    {
        // Proprietà di base
        public EnemyType Type;        // Tipo di nemico
        public int Tier;              // Livello del nemico (1-3)
        public bool IsElite;          // Se è un nemico élite (più forte)
        
        // Movimento e comportamento
        public float BaseSpeed;       // Velocità base di movimento
        public float DetectionRange;  // Raggio in cui può rilevare il giocatore
        
        // Combattimento
        public int AttackDamage;      // Danno base degli attacchi
        public float AttackRange;     // Raggio d'attacco
        public float AttackSpeed;     // Velocità di attacco
        
        // Stato attuale
        public bool IsAggressive;     // Se è attualmente aggressivo
        public bool IsStunned;        // Se è stordito
    }
    
    /// <summary>
    /// Tipi di nemici disponibili nel gioco
    /// </summary>
    public enum EnemyType : byte
    {
        // Standard enemies
        Drone = 0,
        SecurityGuard = 1,
        Turret = 2,
        
        // Specializzati
        Sniper = 10,
        Bruiser = 11,
        Speeder = 12,
        
        // Elite
        EliteDrone = 20,
        EliteGuard = 21,
        CommandUnit = 22
    }
}
