using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Componente che definisce le caratteristiche di un mid-boss.
    /// I mid-boss sono nemici più potenti dei normali ma meno complessi dei boss finali,
    /// che possono apparire a metà livello o alla fine di segmenti specifici.
    /// </summary>
    [Serializable]
    public struct MidBossComponent : IComponentData
    {
        // Proprietà di base
        public MidBossType Type;           // Tipo di mid-boss
        public int Tier;                   // Livello di potenza (1-3)
        
        // Comportamento
        public bool HasEnragedState;       // Se ha uno stato infuriato
        public float EnrageThreshold;      // Soglia di salute per lo stato infuriato (es. 0.3f)
        public float EnragedDamageMultiplier; // Moltiplicatore di danno quando infuriato
        public float EnragedSpeedMultiplier;  // Moltiplicatore di velocità quando infuriato
        
        // Abilità speciali
        public SpecialAbilityType SpecialAbility; // Abilità speciale
        public float SpecialAbilityCooldown;      // Cooldown dell'abilità speciale
        public float CurrentSpecialCooldown;      // Cooldown attuale
        
        // Stato corrente
        public bool IsEnraged;            // Se è attualmente infuriato
        public bool IsActivated;          // Se è stato attivato (combattimento iniziato)
    }
    
    /// <summary>
    /// Tipi di mid-boss disponibili nel gioco
    /// </summary>
    public enum MidBossType : byte
    {
        // Mid-boss mondo 1: Centro Tecnologico
        SecurityCaptain = 0,    // Capitano della Sicurezza
        ArmoredDrone = 1,       // Drone Corazzato
        TurretCommander = 2,    // Comandante Torretta
        
        // Mid-boss mondo 2: Foresta Primordiale
        ElderShaman = 10,       // Sciamano Anziano
        CorruptedTreent = 11,   // Treant Corrotto
        PrimalHunter = 12,      // Cacciatore Primordiale
        
        // Mid-boss mondo 3: Tundra Eterna
        FrostGiant = 20,        // Gigante del Gelo
        IceGolem = 21,          // Golem di Ghiaccio
        StormCaller = 22,       // Evocatore di Tempeste
        
        // Mid-boss mondo 4: Inferno di Lava
        LavaConstruct = 30,     // Costrutto di Lava
        AshWraith = 31,         // Spettro di Cenere
        MagmaSerpent = 32,      // Serpente di Magma
        
        // Mid-boss mondo 5: Abissi Inesplorati
        DeepStalker = 40,       // Cacciatore Abissale
        CoralGuardian = 41,     // Guardiano di Corallo
        VoidSiren = 42,         // Sirena del Vuoto
        
        // Mid-boss mondo 6: Realtà Virtuale
        BinaryReaper = 50,      // Mietitore Binario
        FirewallEnforcer = 51,  // Guardiano del Firewall
        DataPredator = 52       // Predatore di Dati
    }
    
    /// <summary>
    /// Abilità speciali dei mid-boss
    /// </summary>
    public enum SpecialAbilityType : byte
    {
        None = 0,             // Nessuna abilità speciale
        AreaStun = 1,         // Stordimento ad area
        ShieldBurst = 2,      // Esplosione di scudo
        TeleportStrike = 3,   // Colpo con teletrasporto
        SummonMinions = 4,    // Evocazione servitori
        ElementalFury = 5,    // Furia elementale
        HealthDrain = 6,      // Risucchio di salute
        BerserkRage = 7,      // Furia berserker
        DamageReflection = 8, // Riflesso del danno
        DeathThroes = 9       // Ultima resistenza
    }
}
