// Path: Assets/_Project/ECS/Components/Gameplay/FragmentResonanceComponent.cs
using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Characters;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che gestisce la meccanica "Risonanza dei Frammenti",
    /// permettendo ai giocatori di cambiare personaggio istantaneamente durante il gameplay.
    /// </summary>
    [Serializable]
    public struct FragmentResonanceComponent : IComponentData
    {
        /// <summary>
        /// Il personaggio attualmente attivo
        /// </summary>
        public Entity ActiveCharacter;
        
        /// <summary>
        /// Entità dei personaggi sbloccati
        /// </summary>
        public FixedList128Bytes<Entity> UnlockedCharacters;
        
        /// <summary>
        /// Numero di personaggi attualmente sbloccati
        /// </summary>
        public int CharacterCount;
        
        /// <summary>
        /// Indica se la Risonanza dei Frammenti è sbloccata (richiede almeno 2 personaggi)
        /// </summary>
        public bool IsUnlocked;
        
        /// <summary>
        /// Livello di Risonanza raggiunto:
        /// 1: Risonanza Base - Cambio personaggio con effetti base
        /// 2: Risonanza Amplificata - Aumenta il raggio dell'onda di energia
        /// 3: Risonanza Perfetta - Elimina il costo di Focus Time
        /// 4: Risonanza Totale - Permette di attivare brevemente due abilità combinate
        /// </summary>
        public int ResonanceLevel;
        
        /// <summary>
        /// Tempo di cooldown tra cambi personaggio
        /// </summary>
        public float Cooldown;
        
        /// <summary>
        /// Tempo di cooldown rimanente
        /// </summary>
        public float CooldownRemaining;
        
        /// <summary>
        /// Costo in Focus Time per effettuare un cambio personaggio
        /// (10% della barra del Focus Time, ignorato a Risonanza Perfetta)
        /// </summary>
        public float FocusTimeCost;
        
        /// <summary>
        /// Durata dell'invulnerabilità dopo il cambio personaggio
        /// </summary>
        public float InvulnerabilityDuration;
        
        /// <summary>
        /// Raggio dell'onda di energia che danneggia i nemici durante il cambio
        /// </summary>
        public float EnergyWaveRadius;
        
        /// <summary>
        /// Danno inflitto dall'onda di energia
        /// </summary>
        public float EnergyWaveDamage;
        
        /// <summary>
        /// Verifica se la Risonanza è disponibile per l'uso (cooldown completato)
        /// </summary>
        public bool IsAvailable => CooldownRemaining <= 0 && IsUnlocked;
        
        /// <summary>
        /// Crea un nuovo componente FragmentResonance con valori di default
        /// </summary>
        /// <param name="initialCharacter">Personaggio iniziale (Alex)</param>
        /// <returns>Componente inizializzato</returns>
        public static FragmentResonanceComponent Default(Entity initialCharacter)
        {
            var resonance = new FragmentResonanceComponent
            {
                ActiveCharacter = initialCharacter,
                UnlockedCharacters = new FixedList128Bytes<Entity>(),
                CharacterCount = 1,
                IsUnlocked = false, // Si sblocca con 2+ personaggi
                ResonanceLevel = 1,
                Cooldown = 8.0f,
                CooldownRemaining = 0,
                FocusTimeCost = 0.1f, // 10% della barra Focus Time
                InvulnerabilityDuration = 1.5f,
                EnergyWaveRadius = 5.0f,
                EnergyWaveDamage = 10.0f
            };
            
            // Aggiungi il personaggio iniziale alla lista
            resonance.UnlockedCharacters.Add(initialCharacter);
            
            return resonance;
        }
        
        /// <summary>
        /// Aggiorna i timer della Risonanza
        /// </summary>
        /// <param name="deltaTime">Tempo trascorso dall'ultimo frame</param>
        /// <returns>True se lo stato è cambiato (cooldown completato), false altrimenti</returns>
        public bool Update(float deltaTime)
        {
            bool stateChanged = false;
            
            if (CooldownRemaining > 0)
            {
                CooldownRemaining -= deltaTime;
                
                if (CooldownRemaining <= 0)
                {
                    CooldownRemaining = 0;
                    stateChanged = true;
                }
            }
            
            return stateChanged;
        }
        
        /// <summary>
        /// Cambia il personaggio attivo
        /// </summary>
        /// <param name="characterIndex">Indice del personaggio da attivare</param>
        /// <returns>True se il cambio è riuscito, false altrimenti</returns>
        public bool SwitchCharacter(int characterIndex)
        {
            if (!IsAvailable || characterIndex < 0 || characterIndex >= CharacterCount)
            {
                return false;
            }
            
            Entity newCharacter = UnlockedCharacters[characterIndex];
            
            // Non cambiare se è già il personaggio attivo
            if (newCharacter == ActiveCharacter)
            {
                return false;
            }
            
            // Attiva il nuovo personaggio
            ActiveCharacter = newCharacter;
            
            // Applica cooldown
            CooldownRemaining = Cooldown;
            
            return true;
        }
        
        /// <summary>
        /// Aggiunge un nuovo personaggio alla Risonanza
        /// </summary>
        /// <param name="characterEntity">Entità del personaggio da aggiungere</param>
        /// <returns>True se il personaggio è stato aggiunto, false se già presente o lista piena</returns>
        public bool AddCharacter(Entity characterEntity)
        {
            // Verifica se il personaggio è già nella lista
            for (int i = 0; i < CharacterCount; i++)
            {
                if (UnlockedCharacters[i] == characterEntity)
                {
                    return false;
                }
            }
            
            // Verifica se c'è spazio nella lista
            if (CharacterCount >= 6) // Massimo 6 personaggi (come da design)
            {
                return false;
            }
            
            // Aggiungi il personaggio
            UnlockedCharacters.Add(characterEntity);
            CharacterCount++;
            
            // Se questo è il secondo personaggio, sblocca la Risonanza
            if (CharacterCount == 2)
            {
                IsUnlocked = true;
            }
            
            return true;
        }
        
        /// <summary>
        /// Ottiene i bonus ambientali in base al tipo di mondo e al personaggio
        /// </summary>
        /// <param name="worldType">Il tipo di mondo corrente</param>
        /// <returns>Struttura contenente i bonus applicabili</returns>
        public EnvironmentalBonus GetEnvironmentalBonus(WorldType worldType)
        {
            // Ottiene il tipo di personaggio dal componente PlayerData
            // In una implementazione reale, questo verrebbe fatto con EntityManager
            CharacterType characterType = CharacterType.Alex; // Placeholder
            
            var bonus = new EnvironmentalBonus
            {
                SpeedBonus = 0,
                JumpBonus = 0,
                HealingEfficiency = 0,
                ElementalResistance = 0,
                CooldownReduction = 0
            };
            
            // Applica bonus in base alla combinazione personaggio-ambiente
            switch (characterType)
            {
                case CharacterType.Alex when worldType == WorldType.Urban:
                    bonus.SpeedBonus = 0.25f; // +25% velocità per 3 secondi
                    break;
                case CharacterType.Maya when worldType == WorldType.Forest:
                    bonus.HealingEfficiency = 0.4f; // +40% efficacia oggetti curativi
                    break;
                case CharacterType.Kai when worldType == WorldType.Tundra:
                    bonus.ElementalResistance = 0.5f; // Resistenza al freddo per 5 secondi
                    break;
                case CharacterType.Ember when worldType == WorldType.Volcano:
                    bonus.ElementalResistance = 0.6f; // Resistenza al fuoco per 4 secondi
                    break;
                case CharacterType.Marina when worldType == WorldType.Abyss:
                    bonus.JumpBonus = 0.3f; // +30% potenza salto sott'acqua
                    break;
                case CharacterType.Neo when worldType == WorldType.Virtual:
                    bonus.CooldownReduction = 0.2f; // -20% tempo ricarica abilità
                    break;
            }
            
            return bonus;
        }
        
        /// <summary>
        /// Riduce il cooldown della Risonanza tramite oggetti o abilità
        /// </summary>
        /// <param name="reductionAmount">Quantità di riduzione in secondi</param>
        public void ReduceCooldown(float reductionAmount)
        {
            CooldownRemaining = math.max(0, CooldownRemaining - reductionAmount);
        }
    }
    
    /// <summary>
    /// Struttura che definisce i bonus ambientali applicati durante la Risonanza
    /// </summary>
    public struct EnvironmentalBonus
    {
        /// <summary>
        /// Bonus alla velocità di movimento
        /// </summary>
        public float SpeedBonus;
        
        /// <summary>
        /// Bonus alla potenza di salto
        /// </summary>
        public float JumpBonus;
        
        /// <summary>
        /// Miglioramento efficacia oggetti curativi
        /// </summary>
        public float HealingEfficiency;
        
        /// <summary>
        /// Resistenza agli elementi ambientali
        /// </summary>
        public float ElementalResistance;
        
        /// <summary>
        /// Riduzione tempo di ricarica abilità
        /// </summary>
        public float CooldownReduction;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio viene cambiato con la Risonanza
    /// </summary>
    public struct CharacterSwitchedEvent : IComponentData
    {
        public Entity PlayerEntity;
        public Entity PreviousCharacter;
        public Entity NewCharacter;
        public float3 Position;
        public float3 Velocity;
        public WorldType WorldType;
        public EnvironmentalBonus AppliedBonus;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio viene sbloccato
    /// </summary>
    public struct CharacterUnlockedEvent : IComponentData
    {
        public Entity PlayerEntity;
        public Entity UnlockedCharacter;
        public int TotalCharacters;
    }
    
    /// <summary>
    /// Evento generato quando la Risonanza viene sbloccata
    /// </summary>
    public struct ResonanceUnlockedEvent : IComponentData
    {
        public Entity PlayerEntity;
    }
    
    /// <summary>
    /// Evento generato quando il livello di Risonanza aumenta
    /// </summary>
    public struct ResonanceUpgradedEvent : IComponentData
    {
        public Entity PlayerEntity;
        public int PreviousLevel;
        public int NewLevel;
    }
    
    /// <summary>
    /// Evento generato quando la Risonanza è di nuovo disponibile dopo il cooldown
    /// </summary>
    public struct ResonanceReadyEvent : IComponentData
    {
        public Entity PlayerEntity;
    }
    
    /// <summary>
    /// Evento generato quando l'onda di energia della Risonanza viene emessa
    /// </summary>
    public struct EnergyWaveEvent : IComponentData
    {
        public float3 Origin;
        public float Radius;
        public float Damage;
        public Entity SourceEntity;
    }
    
    /// <summary>
    /// Evento generato quando si attivano abilità combinate (Risonanza Totale)
    /// </summary>
    public struct CombinedAbilitiesEvent : IComponentData
    {
        public Entity PrimaryCharacter;
        public Entity SecondaryCharacter;
        public float Duration;
    }
}