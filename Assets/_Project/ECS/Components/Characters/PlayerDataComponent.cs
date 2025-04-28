// Path: Assets/_Project/ECS/Components/Characters/PlayerDataComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente che memorizza i dati comuni a tutti i personaggi giocabili.
    /// Contiene informazioni generali come il tipo di personaggio, il livello di sbloccamento,
    /// e le statistiche base condivise tra tutti i personaggi.
    /// </summary>
    [Serializable]
    public struct PlayerDataComponent : IComponentData
    {
        /// <summary>
        /// Il tipo di personaggio (Alex, Maya, Kai, ecc.)
        /// </summary>
        public CharacterType Type;
        
        /// <summary>
        /// Il nome del personaggio visualizzato nell'interfaccia
        /// </summary>
        public FixedString64Bytes Name;
        
        /// <summary>
        /// Livello di sbloccamento del personaggio (da 1 a 3)
        /// </summary>
        public int UnlockLevel;
        
        /// <summary>
        /// Indica se il personaggio è sbloccato e utilizzabile
        /// </summary>
        public bool IsUnlocked;
        
        /// <summary>
        /// Livello di esperienza del personaggio
        /// </summary>
        public int ExperienceLevel;
        
        /// <summary>
        /// Esperienza attuale nel livello corrente
        /// </summary>
        public int CurrentExperience;
        
        /// <summary>
        /// Esperienza necessaria per salire al livello successivo
        /// </summary>
        public int ExperienceToNextLevel;
        
        /// <summary>
        /// Modificatore base alle statistiche del personaggio (scala con il livello)
        /// </summary>
        public float StatMultiplier;
        
        /// <summary>
        /// ID univoco del frammento posseduto dal personaggio
        /// </summary>
        public int FragmentID;
        
        /// <summary>
        /// Livello di potere del frammento (da 1 a 5)
        /// </summary>
        public int FragmentPowerLevel;
        
        /// <summary>
        /// Tipo di mondo associato a questo personaggio
        /// </summary>
        public WorldType NativeWorldType;
        
        /// <summary>
        /// Costruttore di base per un personaggio
        /// </summary>
        /// <param name="type">Tipo di personaggio</param>
        /// <param name="name">Nome del personaggio</param>
        /// <param name="worldType">Tipo di mondo associato</param>
        /// <returns>Un nuovo componente PlayerData inizializzato</returns>
        public static PlayerDataComponent Create(CharacterType type, string name, WorldType worldType)
        {
            return new PlayerDataComponent
            {
                Type = type,
                Name = new FixedString64Bytes(name),
                UnlockLevel = 1,
                IsUnlocked = type == CharacterType.Alex, // Solo Alex è sbloccato all'inizio
                ExperienceLevel = 1,
                CurrentExperience = 0,
                ExperienceToNextLevel = 100,
                StatMultiplier = 1.0f,
                FragmentID = (int)type,
                FragmentPowerLevel = 1,
                NativeWorldType = worldType
            };
        }
        
        /// <summary>
        /// Aggiunge esperienza al personaggio e gestisce l'aumento di livello
        /// </summary>
        /// <param name="expAmount">Quantità di esperienza guadagnata</param>
        /// <returns>True se il personaggio è salito di livello, false altrimenti</returns>
        public bool AddExperience(int expAmount)
        {
            CurrentExperience += expAmount;
            
            if (CurrentExperience >= ExperienceToNextLevel)
            {
                ExperienceLevel++;
                CurrentExperience -= ExperienceToNextLevel;
                
                // La quantità di esperienza necessaria aumenta del 20% per livello
                ExperienceToNextLevel = (int)(ExperienceToNextLevel * 1.2f);
                
                // Incrementa il moltiplicatore di statistiche
                StatMultiplier = 1.0f + (ExperienceLevel - 1) * 0.1f;
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Aumenta il livello di potere del frammento
        /// </summary>
        /// <returns>True se il potenziamento è stato applicato, false se già al massimo</returns>
        public bool UpgradeFragment()
        {
            if (FragmentPowerLevel < 5)
            {
                FragmentPowerLevel++;
                return true;
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Tipi di personaggi disponibili nel gioco
    /// </summary>
    public enum CharacterType
    {
        Alex = 0,   // Protagonista urbano
        Maya = 1,   // Protagonista della foresta
        Kai = 2,    // Protagonista della tundra
        Ember = 3,  // Protagonista del vulcano
        Marina = 4, // Protagonista degli abissi
        Neo = 5     // Protagonista virtuale
    }
    
    /// <summary>
    /// Tipi di mondo nel gioco
    /// </summary>
    public enum WorldType
    {
        None = 0,
        Urban = 1,      // Città in Caos
        Forest = 2,     // Foresta Primordiale
        Tundra = 3,     // Tundra Eterna
        Volcano = 4,    // Inferno di Lava
        Abyss = 5,      // Abissi Inesplorati
        Virtual = 6     // Realtà Virtuale
    }
}