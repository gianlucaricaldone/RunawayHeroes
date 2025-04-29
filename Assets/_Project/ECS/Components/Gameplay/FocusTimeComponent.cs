using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che gestisce la meccanica del "Focus Time" che permette
    /// al giocatore di rallentare il tempo per prendere decisioni strategiche
    /// e selezionare oggetti durante l'azione frenetica.
    /// </summary>
    [Serializable]
    public struct FocusTimeComponent : IComponentData
    {
        /// <summary>
        /// Durata massima del Focus Time in secondi
        /// </summary>
        public float MaxDuration;
        
        /// <summary>
        /// Tempo rimanente di Focus Time in secondi
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// Tempo di ricarica (cooldown) in secondi prima di poter riutilizzare il Focus Time
        /// </summary>
        public float Cooldown;
        
        /// <summary>
        /// Tempo rimanente di cooldown in secondi
        /// </summary>
        public float CooldownRemaining;
        
        /// <summary>
        /// Fattore di rallentamento del tempo (es. 0.3 = 30% della velocità normale)
        /// </summary>
        public float TimeScale;
        
        /// <summary>
        /// Indica se il Focus Time è attualmente attivo
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Capacità massima di energia Focus (percentuale 0-1)
        /// </summary>
        public float MaxEnergy;
        
        /// <summary>
        /// Energia Focus attuale (percentuale 0-1)
        /// </summary>
        public float CurrentEnergy;
        
        /// <summary>
        /// Tasso di consumo dell'energia durante l'attivazione (percentuale al secondo)
        /// </summary>
        public float EnergyConsumptionRate;
        
        /// <summary>
        /// Tasso di ricarica dell'energia durante l'inattività (percentuale al secondo)
        /// </summary>
        public float EnergyRechargeRate;
        
        /// <summary>
        /// Numero massimo di slot per oggetti disponibili durante il Focus Time
        /// </summary>
        public int MaxItemSlots;
        
        /// <summary>
        /// Array di ID entità degli oggetti attualmente negli slot del Focus Time
        /// </summary>
        public FixedList128Bytes<Entity> ItemSlots;
        
        /// <summary>
        /// Indica se il Focus Time è disponibile per l'uso (energia sufficiente e non in cooldown)
        /// </summary>
        public bool IsAvailable => CurrentEnergy > 0 && CooldownRemaining <= 0 && !IsActive;
        
        /// <summary>
        /// Percentuale di energia rimanente (0-1)
        /// </summary>
        public float EnergyPercentage => CurrentEnergy / MaxEnergy;
        
        /// <summary>
        /// Crea un nuovo FocusTimeComponent con valori predefiniti
        /// </summary>
        /// <returns>Componente inizializzato con valori predefiniti</returns>
        public static FocusTimeComponent Default()
        {
            var component = new FocusTimeComponent
            {
                MaxDuration = 10.0f,
                RemainingTime = 0.0f,
                Cooldown = 25.0f,
                CooldownRemaining = 0.0f,
                TimeScale = 0.3f,
                IsActive = false,
                MaxEnergy = 1.0f,
                CurrentEnergy = 1.0f,
                EnergyConsumptionRate = 0.1f, // 10% al secondo
                EnergyRechargeRate = 0.05f,   // 5% al secondo
                MaxItemSlots = 4,
                ItemSlots = new FixedList128Bytes<Entity>()
            };
            
            // Inizializza gli slot degli oggetti
            for (int i = 0; i < component.MaxItemSlots; i++)
            {
                component.ItemSlots.Add(Entity.Null);
            }
            
            return component;
        }
        
        /// <summary>
        /// Attiva il Focus Time se disponibile
        /// </summary>
        /// <returns>True se il Focus Time è stato attivato, false altrimenti</returns>
        public bool Activate()
        {
            if (IsAvailable)
            {
                IsActive = true;
                RemainingTime = MaxDuration;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Disattiva il Focus Time e avvia il cooldown
        /// </summary>
        /// <param name="applyFullCooldown">Se true, applica l'intero cooldown, altrimenti un cooldown proporzionale al tempo utilizzato</param>
        public void Deactivate(bool applyFullCooldown = false)
        {
            IsActive = false;
            
            if (applyFullCooldown)
            {
                CooldownRemaining = Cooldown;
            }
            else
            {
                // Applica un cooldown proporzionale alla durata utilizzata
                float usedPercentage = 1.0f - (RemainingTime / MaxDuration);
                CooldownRemaining = Cooldown * usedPercentage;
            }
            
            RemainingTime = 0.0f;
        }
        
        /// <summary>
        /// Aggiorna i timer e l'energia del Focus Time
        /// </summary>
        /// <param name="deltaTime">Il tempo trascorso dall'ultimo aggiornamento in secondi</param>
        /// <returns>True se lo stato del Focus Time è cambiato, false altrimenti</returns>
        public bool Update(float deltaTime)
        {
            bool stateChanged = false;
            
            // Se il Focus Time è attivo
            if (IsActive)
            {
                // Riduci il tempo rimanente
                RemainingTime -= deltaTime;
                
                // Consuma energia
                float energyToConsume = EnergyConsumptionRate * deltaTime;
                CurrentEnergy = math.max(0, CurrentEnergy - energyToConsume);
                
                // Se il tempo è scaduto o l'energia è esaurita, disattiva
                if (RemainingTime <= 0 || CurrentEnergy <= 0)
                {
                    Deactivate(false); // Cooldown proporzionale
                    stateChanged = true;
                }
            }
            // Altrimenti, gestisci cooldown e ricarica energia
            else
            {
                // Riduci il cooldown se necessario
                if (CooldownRemaining > 0)
                {
                    CooldownRemaining -= deltaTime;
                    
                    if (CooldownRemaining <= 0)
                    {
                        CooldownRemaining = 0;
                        stateChanged = true;
                    }
                }
                
                // Ricarica energia quando non attivo
                if (CurrentEnergy < MaxEnergy)
                {
                    float energyToRecharge = EnergyRechargeRate * deltaTime;
                    CurrentEnergy = math.min(MaxEnergy, CurrentEnergy + energyToRecharge);
                    
                    // Se l'energia raggiunge la capacità massima, segnala il cambio
                    if (CurrentEnergy >= MaxEnergy && CurrentEnergy - energyToRecharge < MaxEnergy)
                    {
                        stateChanged = true;
                    }
                }
            }
            
            return stateChanged;
        }
        
        /// <summary>
        /// Aggiunge un oggetto a uno slot vuoto del Focus Time
        /// </summary>
        /// <param name="item">L'entità oggetto da aggiungere</param>
        /// <returns>L'indice dello slot assegnato, o -1 se non è stato possibile aggiungere l'oggetto</returns>
        public int AddItem(Entity item)
        {
            for (int i = 0; i < ItemSlots.Length; i++)
            {
                if (ItemSlots[i] == Entity.Null)
                {
                    ItemSlots[i] = item;
                    return i;
                }
            }
            
            return -1; // Nessuno slot disponibile
        }
        
        /// <summary>
        /// Rimuove un oggetto dagli slot del Focus Time
        /// </summary>
        /// <param name="slotIndex">L'indice dello slot da cui rimuovere l'oggetto</param>
        /// <returns>L'entità oggetto rimossa, o Entity.Null se lo slot era già vuoto</returns>
        public Entity RemoveItem(int slotIndex)
        {
            if (slotIndex >= 0 && slotIndex < ItemSlots.Length)
            {
                Entity item = ItemSlots[slotIndex];
                ItemSlots[slotIndex] = Entity.Null;
                return item;
            }
            
            return Entity.Null;
        }
    }
}