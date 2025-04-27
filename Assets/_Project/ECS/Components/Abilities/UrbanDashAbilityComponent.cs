using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Abilities
{
    /// <summary>
    /// Componente che rappresenta l'abilità "Scatto Urbano" di Alex.
    /// Consente un'accelerazione improvvisa con invulnerabilità temporanea
    /// e la capacità di sfondare piccoli ostacoli.
    /// </summary>
    [Serializable]
    public struct UrbanDashAbilityComponent : IComponentData
    {
        /// <summary>
        /// Durata dell'abilità in secondi
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Tempo rimanente dell'abilità se attiva
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// Tempo di ricarica (cooldown) dell'abilità in secondi
        /// </summary>
        public float Cooldown;
        
        /// <summary>
        /// Tempo rimanente di cooldown prima che l'abilità sia nuovamente disponibile
        /// </summary>
        public float CooldownRemaining;
        
        /// <summary>
        /// Moltiplicatore di velocità durante l'attivazione
        /// </summary>
        public float SpeedMultiplier;
        
        /// <summary>
        /// Forza dell'impulso iniziale quando l'abilità viene attivata
        /// </summary>
        public float InitialBoost;
        
        /// <summary>
        /// Indica se l'abilità è attualmente attiva
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Indica se l'abilità è disponibile per l'uso (non in cooldown)
        /// </summary>
        public bool IsAvailable => CooldownRemaining <= 0 && !IsActive;
        
        /// <summary>
        /// Forza necessaria per sfondare ostacoli, determina quali ostacoli possono essere distrutti
        /// </summary>
        public float BreakThroughForce;
        
        /// <summary>
        /// Crea una nuova istanza di UrbanDashAbilityComponent con valori predefiniti
        /// </summary>
        /// <returns>Componente inizializzato con valori predefiniti per Alex</returns>
        public static UrbanDashAbilityComponent Default()
        {
            return new UrbanDashAbilityComponent
            {
                Duration = 2.0f,
                RemainingTime = 0.0f,
                Cooldown = 15.0f,
                CooldownRemaining = 0.0f,
                SpeedMultiplier = 1.75f,
                InitialBoost = 5.0f,
                IsActive = false,
                BreakThroughForce = 100.0f
            };
        }
        
        /// <summary>
        /// Attiva l'abilità se disponibile
        /// </summary>
        /// <returns>True se l'abilità è stata attivata, false altrimenti</returns>
        public bool Activate()
        {
            if (IsAvailable)
            {
                IsActive = true;
                RemainingTime = Duration;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Disattiva l'abilità e avvia il cooldown
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            RemainingTime = 0.0f;
            CooldownRemaining = Cooldown;
        }
        
        /// <summary>
        /// Aggiorna i timer dell'abilità
        /// </summary>
        /// <param name="deltaTime">Il tempo trascorso dall'ultimo aggiornamento in secondi</param>
        /// <returns>True se lo stato dell'abilità è cambiato (attivata/disattivata), false altrimenti</returns>
        public bool Update(float deltaTime)
        {
            bool stateChanged = false;
            
            // Se l'abilità è attiva, riduci il tempo rimanente
            if (IsActive)
            {
                RemainingTime -= deltaTime;
                
                // Se il tempo è scaduto, disattiva l'abilità
                if (RemainingTime <= 0)
                {
                    Deactivate();
                    stateChanged = true;
                }
            }
            // Altrimenti, riduci il cooldown se necessario
            else if (CooldownRemaining > 0)
            {
                CooldownRemaining -= deltaTime;
                
                // Se il cooldown è terminato, segnala il cambio di stato
                if (CooldownRemaining <= 0)
                {
                    CooldownRemaining = 0;
                    stateChanged = true;
                }
            }
            
            return stateChanged;
        }
    }
}