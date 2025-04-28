// Path: Assets/_Project/ECS/Components/Characters/MayaComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Maya, l'esploratrice della foresta primordiale.
    /// Contiene attributi e bonus specifici per questo personaggio, in particolare
    /// quelli legati alla sua abilità Richiamo della Natura e la sua affinità
    /// con l'ambiente naturale.
    /// </summary>
    [Serializable]
    public struct MayaComponent : IComponentData
    {
        /// <summary>
        /// Rigenerazione della salute in ambienti naturali (percentuale per secondo)
        /// </summary>
        public float NaturalRegeneration;
        
        /// <summary>
        /// Riduzione del danno da veleni naturali
        /// </summary>
        public float PoisonResistance;
        
        /// <summary>
        /// Capacità di vedere meglio in ambienti forestali bui
        /// </summary>
        public float ForestVision;
        
        /// <summary>
        /// Affinità con gli animali selvatici (riduce aggressività)
        /// </summary>
        public float WildlifeAffinity;
        
        /// <summary>
        /// Capacità di identificare piante medicinali (aumenta efficacia oggetti curativi)
        /// </summary>
        public float HerbKnowledge;
        
        /// <summary>
        /// Abilità di arrampicata su alberi e superfici naturali
        /// </summary>
        public float TreeClimbingAbility;
        
        /// <summary>
        /// Riduzione del rumore durante il movimento in ambienti naturali
        /// </summary>
        public float StealthInNature;
        
        /// <summary>
        /// Capacità di muoversi attraverso la vegetazione fitta senza rallentare
        /// </summary>
        public float BushTraversalSpeed;
        
        /// <summary>
        /// Crea un nuovo componente Maya con valori di default
        /// </summary>
        /// <returns>Componente MayaComponent inizializzato</returns>
        public static MayaComponent Default()
        {
            return new MayaComponent
            {
                NaturalRegeneration = 0.5f,   // 0.5% salute per secondo in ambienti naturali
                PoisonResistance = 0.5f,      // -50% danno da veleni
                ForestVision = 1.0f,          // +100% visibilità in foreste buie
                WildlifeAffinity = 0.8f,      // -80% aggressività animali selvatici
                HerbKnowledge = 0.3f,         // +30% efficacia oggetti curativi
                TreeClimbingAbility = 0.7f,   // Buona abilità di arrampicata sugli alberi
                StealthInNature = 0.6f,       // -60% rumore in ambienti naturali
                BushTraversalSpeed = 0.25f    // +25% velocità attraverso vegetazione fitta
            };
        }
        
        /// <summary>
        /// Versione potenziata di Maya (livello medio)
        /// </summary>
        public static MayaComponent Advanced()
        {
            var maya = Default();
            maya.NaturalRegeneration = 1.0f;     // Potenziato a 1% per secondo
            maya.PoisonResistance = 0.7f;        // Potenziato a -70%
            maya.HerbKnowledge = 0.5f;           // Potenziato a +50%
            maya.BushTraversalSpeed = 0.5f;      // Potenziato a +50%
            return maya;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Maya (fine gioco)
        /// </summary>
        public static MayaComponent Master()
        {
            var maya = Advanced();
            maya.NaturalRegeneration = 1.5f;     // Potenziato a 1.5% per secondo
            maya.PoisonResistance = 0.9f;        // Potenziato a -90%
            maya.WildlifeAffinity = 1.0f;        // Immunità completa all'aggressione animale
            maya.HerbKnowledge = 0.8f;           // Potenziato a +80%
            maya.TreeClimbingAbility = 1.0f;     // Arrampicata perfetta su alberi
            maya.StealthInNature = 0.9f;         // Quasi invisibile in ambienti naturali
            maya.BushTraversalSpeed = 0.8f;      // +80% velocità in vegetazione fitta
            return maya;
        }
        
        /// <summary>
        /// Calcola il bonus di guarigione basato sulla conoscenza delle erbe
        /// </summary>
        /// <param name="baseHealAmount">Quantità base di guarigione</param>
        /// <returns>Quantità di guarigione potenziata</returns>
        public float ApplyHerbKnowledgeBonus(float baseHealAmount)
        {
            return baseHealAmount * (1f + HerbKnowledge);
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da veleno
        /// </summary>
        /// <param name="poisonDamage">Danno da veleno base</param>
        /// <returns>Danno da veleno ridotto</returns>
        public float ReducePoisonDamage(float poisonDamage)
        {
            return poisonDamage * (1f - PoisonResistance);
        }
    }
}