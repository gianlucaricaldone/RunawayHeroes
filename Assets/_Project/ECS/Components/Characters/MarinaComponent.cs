// Path: Assets/_Project/ECS/Components/Characters/MarinaComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Marina, la biologa degli abissi inesplorati.
    /// Contiene attributi e bonus specifici per questo personaggio,
    /// in particolare quelli legati alla sua abilità Bolla d'Aria
    /// e la sua adattabilità agli ambienti acquatici.
    /// </summary>
    [Serializable]
    public struct MarinaComponent : IComponentData
    {
        /// <summary>
        /// Moltiplicatore della velocità di nuoto rispetto al movimento terrestre
        /// </summary>
        public float SwimSpeed;
        
        /// <summary>
        /// Riduzione del consumo di ossigeno sott'acqua
        /// </summary>
        public float WaterBreathing;
        
        /// <summary>
        /// Resistenza alle pressioni estreme delle profondità
        /// </summary>
        public float PressureResistance;
        
        /// <summary>
        /// Visibilità migliorata in ambienti acquatici scuri
        /// </summary>
        public float UnderwaterVision;
        
        /// <summary>
        /// Resistenza agli shock elettrici (utile contro anguille elettriche)
        /// </summary>
        public float ElectricResistance;
        
        /// <summary>
        /// Capacità di comunicare con la vita marina (riduce aggressività)
        /// </summary>
        public float MarineEmpathy;
        
        /// <summary>
        /// Manovrabilità sott'acqua durante il nuoto
        /// </summary>
        public float UnderwaterAgility;
        
        /// <summary>
        /// Resistenza ai veleni e tossine acquatiche
        /// </summary>
        public float ToxinResistance;
        
        /// <summary>
        /// Crea un nuovo componente Marina con valori di default
        /// </summary>
        /// <returns>Componente MarinaComponent inizializzato</returns>
        public static MarinaComponent Default()
        {
            return new MarinaComponent
            {
                SwimSpeed = 1.5f,              // +50% velocità in acqua rispetto a terra
                WaterBreathing = 0.2f,         // Riduce il consumo di ossigeno dell'80%
                PressureResistance = 0.7f,     // -70% danni da pressione
                UnderwaterVision = 0.9f,       // 90% visibilità sott'acqua
                ElectricResistance = 0.5f,     // -50% danni da shock elettrici
                MarineEmpathy = 0.6f,          // -60% aggressività creature marine
                UnderwaterAgility = 0.8f,      // 80% agilità sott'acqua
                ToxinResistance = 0.4f         // -40% effetti da tossine marine
            };
        }
        
        /// <summary>
        /// Versione potenziata di Marina (livello medio)
        /// </summary>
        public static MarinaComponent Advanced()
        {
            var marina = Default();
            marina.SwimSpeed = 1.8f;              // Potenziato a +80%
            marina.WaterBreathing = 0.1f;         // Riduce il consumo di ossigeno del 90%
            marina.PressureResistance = 0.85f;    // Potenziato a -85%
            marina.ElectricResistance = 0.7f;     // Potenziato a -70%
            marina.UnderwaterAgility = 0.9f;      // Potenziato a 90%
            return marina;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Marina (fine gioco)
        /// </summary>
        public static MarinaComponent Master()
        {
            var marina = Advanced();
            marina.SwimSpeed = 2.2f;              // Potenziato a +120%
            marina.WaterBreathing = 0.05f;        // Riduce il consumo di ossigeno del 95%
            marina.PressureResistance = 1.0f;     // Immunità ai danni da pressione
            marina.UnderwaterVision = 1.0f;       // Visibilità perfetta sott'acqua
            marina.ElectricResistance = 0.9f;     // Potenziato a -90%
            marina.MarineEmpathy = 0.9f;          // -90% aggressività creature marine
            marina.UnderwaterAgility = 1.0f;      // Agilità perfetta sott'acqua
            marina.ToxinResistance = 0.8f;        // -80% effetti da tossine marine
            return marina;
        }
        
        /// <summary>
        /// Calcola la velocità di nuoto effettiva basata sulle condizioni
        /// </summary>
        /// <param name="baseSpeed">Velocità base di movimento</param>
        /// <param name="waterDensity">Densità dell'acqua (1.0 = normale)</param>
        /// <returns>Velocità di nuoto calcolata</returns>
        public float CalculateSwimSpeed(float baseSpeed, float waterDensity = 1.0f)
        {
            return baseSpeed * SwimSpeed * (1f / math.max(0.5f, waterDensity));
        }
        
        /// <summary>
        /// Calcola il tasso di consumo di ossigeno
        /// </summary>
        /// <param name="baseConsumption">Consumo base di ossigeno</param>
        /// <param name="depth">Profondità attuale (influenza il consumo)</param>
        /// <returns>Tasso di consumo ossigeno ridotto</returns>
        public float CalculateOxygenConsumption(float baseConsumption, float depth)
        {
            // Più profondo = consumo maggiore, ma mitigato dall'abilità WaterBreathing
            float depthFactor = 1.0f + (depth / 100.0f); // +1% ogni metro di profondità
            return baseConsumption * WaterBreathing * depthFactor;
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da pressione
        /// </summary>
        /// <param name="pressureDamage">Danno da pressione base</param>
        /// <returns>Danno da pressione ridotto</returns>
        public float ReducePressureDamage(float pressureDamage)
        {
            return pressureDamage * (1f - PressureResistance);
        }
        
        /// <summary>
        /// Calcola la riduzione del danno elettrico
        /// </summary>
        /// <param name="electricDamage">Danno elettrico base</param>
        /// <returns>Danno elettrico ridotto</returns>
        public float ReduceElectricDamage(float electricDamage)
        {
            return electricDamage * (1f - ElectricResistance);
        }
    }
}