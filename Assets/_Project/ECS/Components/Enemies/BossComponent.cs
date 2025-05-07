using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Componente che definisce le caratteristiche e il comportamento di un boss.
    /// I boss sono nemici speciali con meccaniche di combattimento complesse basate
    /// su fasi e con attacchi unici al tema del mondo in cui si trovano.
    /// </summary>
    [Serializable]
    public struct BossComponent : IComponentData
    {
        // Proprietà di base
        public BossType Type;                 // Tipo di boss
        public int CurrentPhase;              // Fase corrente (0-based)
        public int TotalPhases;               // Numero totale di fasi
        public int BaseDamage;                // Danno base degli attacchi
        public int AttackPatternComplexity;   // Complessità dei pattern di attacco
        
        // Soglie di fase
        // Soglie di salute per le transizioni di fase (es. 0.7f, 0.4f, 0.1f)
        // Utilizziamo valori fissi per le soglie di fase (massimo 4 fasi)
        public float PhaseThreshold1;
        public float PhaseThreshold2;
        public float PhaseThreshold3;
        public float PhaseThreshold4;
        
        // Timer e cooldown
        public float PhaseTransitionTimer;    // Timer per le transizioni di fase
        public float SpecialAttackCooldown;   // Cooldown per gli attacchi speciali
        
        // Comportamento di fase
        public float CurrentPhaseIntensity;   // Intensità corrente della fase (aumenta col tempo)
        public float PhaseIntensityRate;      // Velocità di aumento dell'intensità
        
        // Stato di invulnerabilità
        public bool IsInvulnerable;           // Se il boss è attualmente invulnerabile
        public float InvulnerabilityTimer;    // Timer di invulnerabilità rimanente
        
        // Proprietà di spawn
        public bool HasMinions;               // Se il boss può evocare servitori
        public int MaxMinionCount;            // Numero massimo di servitori simultanei
        public float MinionSpawnCooldown;     // Cooldown per l'evocazione di servitori
        
        // Flag di stato
        public bool IsEnraged;                // Se il boss è nello stato infuriato
        public bool IsActivated;              // Se il boss è stato attivato (combattimento iniziato)
        
        // Funzioni di utilità specifiche
        public float GetCurrentHealthPercentage(float currentHealth, float maxHealth)
        {
            return currentHealth / maxHealth;
        }
        
        public bool ShouldTransitionToNextPhase(float currentHealth, float maxHealth)
        {
            if (CurrentPhase >= TotalPhases - 1)
                return false;
                
            float healthPercent = GetCurrentHealthPercentage(currentHealth, maxHealth);
            
            // Controlla la soglia appropriata in base alla fase corrente
            switch (CurrentPhase)
            {
                case 0:
                    return healthPercent <= PhaseThreshold1;
                case 1:
                    return healthPercent <= PhaseThreshold2;
                case 2:
                    return healthPercent <= PhaseThreshold3;
                case 3:
                    return healthPercent <= PhaseThreshold4;
                default:
                    return false;
            }
        }
    }
    
    /// <summary>
    /// Tipi di boss disponibili nel gioco
    /// </summary>
    public enum BossType : byte
    {
        // Boss mondo 1: Centro Tecnologico
        SecurityDirector = 0,       // Direttore della Sicurezza
        PrototypeWarMech = 1,      // Prototipo Mech da Guerra
        
        // Boss mondo 2: Foresta Primordiale
        AncientGuardian = 10,      // Guardiano Antico
        CorruptedSpirit = 11,      // Spirito Corrotto
        
        // Boss mondo 3: Tundra Eterna
        FrostLord = 20,           // Signore del Gelo
        AncientConstruct = 21,    // Costruzione Antica
        
        // Boss mondo 4: Inferno di Lava
        MoltenTitan = 30,         // Titano di Magma
        FireElemental = 31,       // Elementale di Fuoco
        
        // Boss mondo 5: Abissi Inesplorati
        AbyssalHorror = 40,       // Orrore Abissale
        DeepDweller = 41,         // Abitante delle Profondità
        
        // Boss mondo 6: Realtà Virtuale
        CorruptedAI = 50,         // IA Corrotta
        SecurityOvermind = 51,    // Supermente della Sicurezza
        
        // Boss finale
        Architect = 100           // L'Architetto (boss finale)
    }
}
