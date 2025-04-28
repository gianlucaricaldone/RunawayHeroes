// Path: Assets/_Project/ECS/Components/Characters/AlexComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Alex, il corriere urbano.
    /// Contiene attributi e bonus specifici per questo personaggio,
    /// in particolare legati alla sua abilità Scatto Urbano e la sua
    /// maestria nell'ambiente urbano.
    /// </summary>
    [Serializable]
    public struct AlexComponent : IComponentData
    {
        /// <summary>
        /// Bonus alla velocità di parkour in ambiente urbano
        /// </summary>
        public float UrbanParkourBonus;
        
        /// <summary>
        /// Bonus alla velocità di scivolata
        /// </summary>
        public float SlideSpeedBonus;
        
        /// <summary>
        /// Capacità di rimbalzare dai muri per brevi periodi
        /// </summary>
        public bool WallRunEnabled;
        
        /// <summary>
        /// Durata massima del wall-run in secondi
        /// </summary>
        public float MaxWallRunTime;
        
        /// <summary>
        /// Tempo rimanente di wall-run
        /// </summary>
        public float WallRunTimeRemaining;
        
        /// <summary>
        /// Abilità di agganciare ringhiere per grinding (sbloccabile)
        /// </summary>
        public bool RailGrindEnabled;
        
        /// <summary>
        /// Bonus alla velocità quando si effettua grinding su ringhiere
        /// </summary>
        public float RailGrindSpeedBonus;
        
        /// <summary>
        /// Bonus all'altezza del salto in ambiente urbano 
        /// </summary>
        public float UrbanJumpBonus;
        
        /// <summary>
        /// Capacità di eseguire un doppio salto (sbloccabile)
        /// </summary>
        public bool DoubleJumpEnabled;
        
        /// <summary>
        /// Tempo di ricarica ridotto per l'abilità Scatto Urbano
        /// </summary>
        public float UrbanDashCooldownReduction;
        
        /// <summary>
        /// Riduzione del danno da impatto con ostacoli urbani
        /// </summary>
        public float UrbanObstacleDamageReduction;
        
        /// <summary>
        /// Capacità di sfondare più facilmente piccoli ostacoli urbani
        /// </summary>
        public float ObstacleBreakThroughBonus;
        
        /// <summary>
        /// Bonus alla visibilità in ambienti urbani scarsamente illuminati
        /// </summary>
        public float UrbanNightVisionBonus;
        
        /// <summary>
        /// Crea un nuovo componente Alex con valori di default
        /// </summary>
        /// <returns>Componente AlexComponent inizializzato</returns>
        public static AlexComponent Default()
        {
            return new AlexComponent
            {
                UrbanParkourBonus = 0.15f,        // +15% velocità in ambiente urbano
                SlideSpeedBonus = 0.2f,           // +20% velocità scivolata
                WallRunEnabled = false,           // Sbloccabile con progressione
                MaxWallRunTime = 2.5f,
                WallRunTimeRemaining = 0f,
                RailGrindEnabled = false,         // Sbloccabile con progressione
                RailGrindSpeedBonus = 0.3f,       // +30% velocità grinding
                UrbanJumpBonus = 0.1f,            // +10% altezza salto in città
                DoubleJumpEnabled = false,        // Sbloccabile con progressione
                UrbanDashCooldownReduction = 0.1f, // -10% tempo ricarica Scatto Urbano
                UrbanObstacleDamageReduction = 0.2f, // -20% danno da ostacoli urbani
                ObstacleBreakThroughBonus = 0.25f,   // +25% chance di sfondare ostacoli
                UrbanNightVisionBonus = 0.5f         // +50% visibilità in ambienti bui urbani
            };
        }
        
        /// <summary>
        /// Versione potenziata di Alex (livello medio)
        /// </summary>
        public static AlexComponent Advanced()
        {
            var alex = Default();
            alex.UrbanParkourBonus = 0.25f;      // Potenziato a +25%
            alex.SlideSpeedBonus = 0.3f;         // Potenziato a +30%
            alex.WallRunEnabled = true;          // Sbloccato
            alex.UrbanJumpBonus = 0.15f;         // Potenziato a +15%
            alex.UrbanDashCooldownReduction = 0.2f; // Potenziato a -20%
            return alex;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Alex (fine gioco)
        /// </summary>
        public static AlexComponent Master()
        {
            var alex = Advanced();
            alex.UrbanParkourBonus = 0.4f;       // Potenziato a +40%
            alex.SlideSpeedBonus = 0.5f;         // Potenziato a +50%
            alex.MaxWallRunTime = 4f;            // Aumentato a 4 secondi
            alex.RailGrindEnabled = true;        // Sbloccato
            alex.RailGrindSpeedBonus = 0.5f;     // Potenziato a +50%
            alex.DoubleJumpEnabled = true;       // Sbloccato
            alex.UrbanDashCooldownReduction = 0.35f; // Potenziato a -35%
            alex.UrbanObstacleDamageReduction = 0.5f; // Potenziato a -50%
            alex.ObstacleBreakThroughBonus = 0.6f;   // Potenziato a +60%
            return alex;
        }
        
        /// <summary>
        /// Ripristina il tempo di wall-run al massimo
        /// </summary>
        public void ResetWallRunTime()
        {
            WallRunTimeRemaining = MaxWallRunTime;
        }
        
        /// <summary>
        /// Consuma tempo di wall-run
        /// </summary>
        /// <param name="deltaTime">Tempo trascorso dall'ultimo frame</param>
        /// <returns>True se il wall-run è ancora attivo, false se il tempo è esaurito</returns>
        public bool ConsumeWallRunTime(float deltaTime)
        {
            if (WallRunTimeRemaining > 0)
            {
                WallRunTimeRemaining -= deltaTime;
                return WallRunTimeRemaining > 0;
            }
            
            return false;
        }
    }
}