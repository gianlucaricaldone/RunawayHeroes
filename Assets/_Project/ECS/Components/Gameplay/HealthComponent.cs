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
        /// <summary>
        /// Punti vita attuali dell'entità
        /// </summary>
        public float CurrentHealth;
        
        /// <summary>
        /// Punti vita massimi dell'entità
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// Indica se l'entità è attualmente invulnerabile ai danni
        /// </summary>
        public bool IsInvulnerable;
        
        /// <summary>
        /// Tempo rimanente di invulnerabilità (in secondi)
        /// </summary>
        public float InvulnerabilityTime;
        
        /// <summary>
        /// Tasso di rigenerazione della salute (punti vita per secondo)
        /// </summary>
        public float RegenRate;
        
        /// <summary>
        /// Indica se la rigenerazione della salute è attualmente attiva
        /// </summary>
        public bool IsRegenerating;
        
        /// <summary>
        /// Indica se l'entità è morta (punti vita <= 0)
        /// </summary>
        public bool IsDead => CurrentHealth <= 0;
        
        /// <summary>
        /// Percentuale di salute attuale (0.0 - 1.0)
        /// </summary>
        public float HealthPercentage => CurrentHealth / MaxHealth;
        
        /// <summary>
        /// Crea un nuovo HealthComponent con valori predefiniti
        /// </summary>
        /// <param name="maxHealth">Punti vita massimi</param>
        /// <returns>HealthComponent inizializzato con i valori specificati</returns>
        public static HealthComponent Default(float maxHealth = 100.0f)
        {
            return new HealthComponent
            {
                CurrentHealth = maxHealth,
                MaxHealth = maxHealth,
                IsInvulnerable = false,
                InvulnerabilityTime = 0.0f,
                RegenRate = 0.0f,
                IsRegenerating = false
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