// Path: Assets/_Project/ECS/Components/Input/ResonanceInputComponent.cs
using System;
using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Input
{
    /// <summary>
    /// Componente che gestisce l'input relativo alla Risonanza dei Frammenti,
    /// permettendo al giocatore di cambiare personaggio durante il gameplay.
    /// </summary>
    [Serializable]
    public struct ResonanceInputComponent : IComponentData
    {
        /// <summary>
        /// Indice del personaggio a cui cambiare (-1 se nessun cambio richiesto)
        /// </summary>
        public int SwitchToCharacterIndex;
        
        /// <summary>
        /// Indica se è stato sbloccato un nuovo personaggio
        /// </summary>
        public bool NewCharacterUnlocked;
        
        /// <summary>
        /// Entità del nuovo personaggio sbloccato
        /// </summary>
        public Entity NewCharacterEntity;
        
        /// <summary>
        /// Indica se è stato richiesto un aumento di livello Risonanza
        /// </summary>
        public bool ResonanceLevelUp;
        
        /// <summary>
        /// Nuovo livello di Risonanza dopo l'upgrade
        /// </summary>
        public int NewResonanceLevel;
        
        /// <summary>
        /// Crea un nuovo ResonanceInputComponent con valori predefiniti
        /// </summary>
        /// <returns>Componente inizializzato con valori predefiniti</returns>
        public static ResonanceInputComponent Default()
        {
            return new ResonanceInputComponent
            {
                SwitchToCharacterIndex = -1,
                NewCharacterUnlocked = false,
                NewCharacterEntity = Entity.Null,
                ResonanceLevelUp = false,
                NewResonanceLevel = 1
            };
        }
        
        /// <summary>
        /// Reimposta gli input a singolo frame per evitare input ripetuti
        /// </summary>
        public void Reset()
        {
            SwitchToCharacterIndex = -1;
            NewCharacterUnlocked = false;
            NewCharacterEntity = Entity.Null;
            ResonanceLevelUp = false;
        }
        
        /// <summary>
        /// Imposta la richiesta di cambio personaggio
        /// </summary>
        /// <param name="characterIndex">Indice del personaggio da attivare</param>
        public void RequestCharacterSwitch(int characterIndex)
        {
            SwitchToCharacterIndex = characterIndex;
        }
        
        /// <summary>
        /// Imposta un nuovo personaggio sbloccato
        /// </summary>
        /// <param name="character">Entità del personaggio sbloccato</param>
        public void UnlockNewCharacter(Entity character)
        {
            NewCharacterUnlocked = true;
            NewCharacterEntity = character;
        }
        
        /// <summary>
        /// Richiede un aumento di livello per la Risonanza
        /// </summary>
        /// <param name="newLevel">Nuovo livello di Risonanza</param>
        public void UpgradeResonance(int newLevel)
        {
            ResonanceLevelUp = true;
            NewResonanceLevel = newLevel;
        }
    }
}