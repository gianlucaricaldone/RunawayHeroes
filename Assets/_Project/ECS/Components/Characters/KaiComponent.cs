// Path: Assets/_Project/ECS/Components/Characters/KaiComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Kai, l'alpinista della tundra eterna.
    /// Contiene attributi e bonus specifici per questo personaggio,
    /// in particolare quelli legati alla sua abilità Aura di Calore
    /// e la sua resistenza agli ambienti freddi.
    /// </summary>
    [Serializable]
    public struct KaiComponent : IComponentData
    {
        /// <summary>
        /// Resistenza al freddo e agli effetti di congelamento
        /// </summary>
        public float ColdResistance;
        
        /// <summary>
        /// Capacità di mantenere presa su superfici ghiacciate
        /// </summary>
        public float IceGrip;
        
        /// <summary>
        /// Bonus alla stamina in ambienti freddi
        /// </summary>
        public float StaminaBonus;
        
        /// <summary>
        /// Abilità di arrampicata su pareti ghiacciate
        /// </summary>
        public float ClimbingAbility;
        
        /// <summary>
        /// Capacità di individuare punti deboli nel ghiaccio
        /// </summary>
        public float IceAnalysis;
        
        /// <summary>
        /// Protezione contro valanghe e cadute di ghiaccio
        /// </summary>
        public float AvalancheAwareness;
        
        /// <summary>
        /// Visibilità in condizioni di tormenta o nebbia
        /// </summary>
        public float BlizzardVision;
        
        /// <summary>
        /// Capacità di trattenere il calore corporeo (riduce effetti di status negativi dal freddo)
        /// </summary>
        public float HeatRetention;
        
        /// <summary>
        /// Crea un nuovo componente Kai con valori di default
        /// </summary>
        /// <returns>Componente KaiComponent inizializzato</returns>
        public static KaiComponent Default()
        {
            return new KaiComponent
            {
                ColdResistance = 0.8f,       // -80% effetti negativi dal freddo
                IceGrip = 0.7f,              // Riduzione del 70% dello scivolamento sul ghiaccio
                StaminaBonus = 0.2f,         // +20% stamina in ambienti freddi
                ClimbingAbility = 0.6f,      // Buone capacità di arrampicata
                IceAnalysis = 0.5f,          // 50% probabilità di individuare ghiaccio fragile
                AvalancheAwareness = 0.4f,   // 40% probabilità di evitare danni da valanghe
                BlizzardVision = 0.6f,       // 60% della visibilità normale in tormente
                HeatRetention = 0.5f         // 50% riduzione durata status negativi da freddo
            };
        }
        
        /// <summary>
        /// Versione potenziata di Kai (livello medio)
        /// </summary>
        public static KaiComponent Advanced()
        {
            var kai = Default();
            kai.ColdResistance = 0.9f;       // Potenziato a -90%
            kai.IceGrip = 0.85f;             // Potenziato a 85%
            kai.ClimbingAbility = 0.8f;      // Potenziato a 80%
            kai.BlizzardVision = 0.8f;       // Potenziato a 80%
            return kai;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Kai (fine gioco)
        /// </summary>
        public static KaiComponent Master()
        {
            var kai = Advanced();
            kai.ColdResistance = 1.0f;       // Immunità completa al freddo
            kai.IceGrip = 1.0f;              // Nessuno scivolamento sul ghiaccio
            kai.StaminaBonus = 0.5f;         // Potenziato a +50%
            kai.ClimbingAbility = 1.0f;      // Capacità di arrampicata perfetta
            kai.IceAnalysis = 0.9f;          // 90% probabilità di individuare ghiaccio fragile
            kai.AvalancheAwareness = 0.8f;   // 80% probabilità di evitare danni da valanghe
            kai.BlizzardVision = 1.0f;       // Visibilità perfetta in tormente
            kai.HeatRetention = 0.9f;        // 90% riduzione durata status negativi
            return kai;
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da freddo
        /// </summary>
        /// <param name="coldDamage">Danno da freddo base</param>
        /// <returns>Danno da freddo ridotto</returns>
        public float ReduceColdDamage(float coldDamage)
        {
            return coldDamage * (1f - ColdResistance);
        }
        
        /// <summary>
        /// Calcola il modificatore di scivolamento su ghiaccio
        /// </summary>
        /// <param name="baseSlipperiness">Scivolosità base della superficie</param>
        /// <returns>Scivolosità ridotta</returns>
        public float CalculateIceSlip(float baseSlipperiness)
        {
            return baseSlipperiness * (1f - IceGrip);
        }
        
        /// <summary>
        /// Verifica se Kai rileva ghiaccio debole
        /// </summary>
        /// <returns>True se rileva un punto debole, false altrimenti</returns>
        public bool DetectWeakIce()
        {
            // Simulazione semplificata: una probabilità casuale basata su IceAnalysis
            return UnityEngine.Random.value < IceAnalysis;
        }
    }
}