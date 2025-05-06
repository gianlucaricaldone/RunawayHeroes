using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che gestisce la salute di un'entità, inclusi punti vita attuali, massimi,
    /// stato di invulnerabilità e rigenerazione. Utilizzato sia per il giocatore che per i nemici.
    /// </summary>
    [Serializable]
    public struct HealthComponent : IComponentData
    {
        // ------ Salute ------
        /// <summary>
        /// Punti vita attuali dell'entità
        /// </summary>
        public float CurrentHealth;
        
        /// <summary>
        /// Punti vita massimi dell'entità
        /// </summary>
        public float MaxHealth;

        // ------ Invulnerabilità ------
        /// <summary>
        /// Indica se l'entità è attualmente invulnerabile ai danni
        /// </summary>
        public bool IsInvulnerable;
        
        /// <summary>
        /// Tempo rimanente di invulnerabilità (in secondi)
        /// </summary>
        public float InvulnerabilityTime;
        
        /// <summary>
        /// Timer per tracciare il tempo di invulnerabilità rimanente
        /// </summary>
        public float InvulnerabilityTimer;

        // ------ Rigenerazione salute ------
        /// <summary>
        /// Indica se l'entità ha rigenerazione automatica della salute
        /// </summary>
        public bool HasAutoRegen;
        
        /// <summary>
        /// Tasso di rigenerazione della salute (punti vita per secondo)
        /// </summary>
        public float RegenRate;
        
        /// <summary>
        /// Ritardo prima dell'inizio della rigenerazione dopo aver subito danni (in secondi)
        /// </summary>
        public float RegenDelay;
        
        /// <summary>
        /// Tempo trascorso dall'ultimo danno subito (per la rigenerazione)
        /// </summary>
        public float TimeSinceLastDamage;
        
        /// <summary>
        /// Indica se la rigenerazione della salute è attualmente attiva
        /// </summary>
        public bool IsRegenerating;

        // ------ Scudo ------
        /// <summary>
        /// Indica se l'entità ha uno scudo
        /// </summary>
        public bool HasShield;
        
        /// <summary>
        /// Punti scudo attuali
        /// </summary>
        public float CurrentShield;
        
        /// <summary>
        /// Punti scudo massimi
        /// </summary>
        public float MaxShield;
        
        /// <summary>
        /// Indica se lo scudo ha rigenerazione automatica
        /// </summary>
        public bool HasShieldAutoRegen;
        
        /// <summary>
        /// Tasso di rigenerazione dello scudo (punti per secondo)
        /// </summary>
        public float ShieldRegenRate;
        
        /// <summary>
        /// Ritardo prima dell'inizio della rigenerazione dello scudo dopo aver subito danni (in secondi)
        /// </summary>
        public float ShieldRegenDelay;
        
        /// <summary>
        /// Tempo trascorso dall'ultimo danno allo scudo (per la rigenerazione)
        /// </summary>
        public float TimeSinceLastShieldDamage;
        
        // ------ Proprietà calcolate ------
        /// <summary>
        /// Indica se l'entità è morta (punti vita <= 0)
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;
        
        /// <summary>
        /// Percentuale di salute attuale (0.0 - 1.0)
        /// </summary>
        public float HealthPercentage => CurrentHealth / MaxHealth;
        
        /// <summary>
        /// Percentuale di scudo attuale (0.0 - 1.0)
        /// </summary>
        public float ShieldPercentage => HasShield ? CurrentShield / MaxShield : 0;
        
        /// <summary>
        /// Crea un nuovo HealthComponent con valori predefiniti
        /// </summary>
        /// <param name="maxHealth">Punti vita massimi</param>
        /// <param name="maxShield">Punti scudo massimi (0 per nessuno scudo)</param>
        /// <param name="enableAutoRegen">Abilita rigenerazione automatica della salute</param>
        /// <param name="enableShieldRegen">Abilita rigenerazione automatica dello scudo</param>
        /// <returns>HealthComponent inizializzato con i valori specificati</returns>
        public static HealthComponent Default(float maxHealth = 100.0f, float maxShield = 0.0f, 
                                              bool enableAutoRegen = false, bool enableShieldRegen = false)
        {
            bool hasShield = maxShield > 0.0f;
            
            return new HealthComponent
            {
                // Valori salute
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
                
                // Invulnerabilità
                IsInvulnerable = false,
                InvulnerabilityTime = 0.0f,
                InvulnerabilityTimer = 0.0f,
                
                // Rigenerazione salute
                HasAutoRegen = enableAutoRegen,
                RegenRate = enableAutoRegen ? 5.0f : 0.0f,
                RegenDelay = 3.0f,
                TimeSinceLastDamage = 3.0f,
                IsRegenerating = false,
                
                // Scudo
                HasShield = hasShield,
                CurrentShield = maxShield,
                MaxShield = maxShield,
                HasShieldAutoRegen = enableShieldRegen && hasShield,
                ShieldRegenRate = enableShieldRegen && hasShield ? 10.0f : 0.0f,
                ShieldRegenDelay = 5.0f,
                TimeSinceLastShieldDamage = 5.0f
            };
        }
        
        /// <summary>
        /// Applica danni all'entità, tenendo conto dell'invulnerabilità
        /// </summary>
        /// <param name="damage">Quantità di danno da applicare</param>
        /// <returns>Danno effettivamente applicato</returns>
        public float ApplyDamage(float damage)
        {
            if (IsInvulnerable || IsDead)
                return 0.0f;
                
            float actualDamage = Math.Min(CurrentHealth, damage);
            CurrentHealth -= actualDamage;
            
            return actualDamage;
        }
        
        /// <summary>
        /// Ripristina salute all'entità
        /// </summary>
        /// <param name="amount">Quantità di salute da ripristinare</param>
        /// <returns>Salute effettivamente ripristinata</returns>
        public float Heal(float amount)
        {
            if (IsDead)
                return 0.0f;
                
            float previousHealth = CurrentHealth;
            CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
            
            return CurrentHealth - previousHealth;
        }
        
        /// <summary>
        /// Attiva l'invulnerabilità per un periodo di tempo specificato
        /// </summary>
        /// <param name="duration">Durata dell'invulnerabilità in secondi</param>
        public void SetInvulnerable(float duration)
        {
            IsInvulnerable = true;
            InvulnerabilityTime = duration;
        }
    }
}