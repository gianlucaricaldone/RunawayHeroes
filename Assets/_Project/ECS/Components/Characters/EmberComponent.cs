// Path: Assets/_Project/ECS/Components/Characters/EmberComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Ember, la sopravvissuta dell'inferno di lava.
    /// Contiene attributi e bonus specifici per questo personaggio,
    /// in particolare quelli legati alla sua abilità Corpo Ignifugo
    /// e la sua resistenza agli ambienti vulcanici.
    /// </summary>
    [Serializable]
    public struct EmberComponent : IComponentData
    {
        /// <summary>
        /// Resistenza al calore e alle alte temperature
        /// </summary>
        public float HeatResistance;
        
        /// <summary>
        /// Riduzione del danno da fuoco
        /// </summary>
        public float FireDamageReduction;
        
        /// <summary>
        /// Resistenza ai gas tossici e vulcanici
        /// </summary>
        public float ToxicGasResistance;
        
        /// <summary>
        /// Resistenza ai danni da esplosione
        /// </summary>
        public float ExplosionResistance;
        
        /// <summary>
        /// Capacità di individuare flussi di magma sicuri
        /// </summary>
        public float MagmaPathfinding;
        
        /// <summary>
        /// Capacità di prevedere eruzioni imminenti
        /// </summary>
        public float EruptionSense;
        
        /// <summary>
        /// Durata estesa per oggetti raffreddanti
        /// </summary>
        public float CoolantEfficiency;
        
        /// <summary>
        /// Visibilità migliorata attraverso fumo e cenere
        /// </summary>
        public float AshVision;
        
        /// <summary>
        /// Crea un nuovo componente Ember con valori di default
        /// </summary>
        /// <returns>Componente EmberComponent inizializzato</returns>
        public static EmberComponent Default()
        {
            return new EmberComponent
            {
                HeatResistance = 0.8f,          // -80% effetti negativi dal calore
                FireDamageReduction = 0.6f,     // -60% danno da fuoco
                ToxicGasResistance = 0.4f,      // -40% effetti da gas tossici
                ExplosionResistance = 0.3f,     // -30% danno da esplosioni
                MagmaPathfinding = 0.5f,        // 50% probabilità di trovare percorsi sicuri
                EruptionSense = 0.4f,           // 40% probabilità di prevedere eruzioni
                CoolantEfficiency = 0.3f,       // +30% durata oggetti raffreddanti
                AshVision = 0.7f                // 70% visibilità in aree con cenere e fumo
            };
        }
        
        /// <summary>
        /// Versione potenziata di Ember (livello medio)
        /// </summary>
        public static EmberComponent Advanced()
        {
            var ember = Default();
            ember.HeatResistance = 0.9f;           // Potenziato a -90%
            ember.FireDamageReduction = 0.8f;      // Potenziato a -80%
            ember.ToxicGasResistance = 0.6f;       // Potenziato a -60%
            ember.MagmaPathfinding = 0.7f;         // Potenziato a 70%
            ember.AshVision = 0.9f;                // Potenziato a 90%
            return ember;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Ember (fine gioco)
        /// </summary>
        public static EmberComponent Master()
        {
            var ember = Advanced();
            ember.HeatResistance = 1.0f;           // Immunità completa al calore
            ember.FireDamageReduction = 0.95f;     // Quasi immune al fuoco (-95%)
            ember.ToxicGasResistance = 0.9f;       // Potenziato a -90%
            ember.ExplosionResistance = 0.7f;      // Potenziato a -70%
            ember.MagmaPathfinding = 0.9f;         // 90% probabilità di trovare percorsi sicuri
            ember.EruptionSense = 0.8f;            // 80% probabilità di prevedere eruzioni
            ember.CoolantEfficiency = 0.6f;        // +60% durata oggetti raffreddanti
            ember.AshVision = 1.0f;                // Visibilità perfetta in aree con cenere
            return ember;
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da fuoco
        /// </summary>
        /// <param name="fireDamage">Danno da fuoco base</param>
        /// <returns>Danno da fuoco ridotto</returns>
        public float ReduceFireDamage(float fireDamage)
        {
            return fireDamage * (1f - FireDamageReduction);
        }
        
        /// <summary>
        /// Calcola l'efficacia aumentata degli oggetti raffreddanti
        /// </summary>
        /// <param name="baseDuration">Durata base dell'oggetto raffreddante</param>
        /// <returns>Durata aumentata</returns>
        public float EnhanceCoolantDuration(float baseDuration)
        {
            return baseDuration * (1f + CoolantEfficiency);
        }
        
        /// <summary>
        /// Verifica se Ember può prevedere un'eruzione imminente
        /// </summary>
        /// <returns>True se prevede un'eruzione, false altrimenti</returns>
        public bool DetectEruption()
        {
            // Simulazione semplificata: una probabilità casuale basata su EruptionSense
            return UnityEngine.Random.value < EruptionSense;
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da gas tossici
        /// </summary>
        /// <param name="toxicDamage">Danno da gas tossico base</param>
        /// <returns>Danno da gas tossico ridotto</returns>
        public float ReduceToxicGasDamage(float toxicDamage)
        {
            return toxicDamage * (1f - ToxicGasResistance);
        }
    }
}