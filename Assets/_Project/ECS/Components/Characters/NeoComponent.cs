// Path: Assets/_Project/ECS/Components/Characters/NeoComponent.cs
using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Characters
{
    /// <summary>
    /// Componente specifico per Neo, l'hacker della realtà virtuale.
    /// Contiene attributi e bonus specifici per questo personaggio,
    /// in particolare quelli legati alla sua abilità Glitch Controllato
    /// e la sua capacità di manipolare l'ambiente digitale.
    /// </summary>
    [Serializable]
    public struct NeoComponent : IComponentData
    {
        /// <summary>
        /// Capacità di vedere codice e glitch nascosti nell'ambiente
        /// </summary>
        public float CodeSight;
        
        /// <summary>
        /// Resistenza agli attacchi di corruzione dati
        /// </summary>
        public float DataCorruptionResistance;
        
        /// <summary>
        /// Capacità di manipolare glitch ambientali a proprio vantaggio
        /// </summary>
        public float GlitchManipulation;
        
        /// <summary>
        /// Efficienza nei mini-giochi di hacking
        /// </summary>
        public float HackingEfficiency;
        
        /// <summary>
        /// Capacità di bypassare firewalls e barriere di sicurezza
        /// </summary>
        public float FirewallBypass;
        
        /// <summary>
        /// Abilità di ripristinare sistemi corrotti (cura aumentata in mondo virtuale)
        /// </summary>
        public float SystemRestoration;
        
        /// <summary>
        /// Velocità di trasferimento dati (movimento più rapido attraverso stream di dati)
        /// </summary>
        public float DataTransferSpeed;
        
        /// <summary>
        /// Resistenza agli attacchi di malware e virus
        /// </summary>
        public float MalwareImmunity;
        
        /// <summary>
        /// Crea un nuovo componente Neo con valori di default
        /// </summary>
        /// <returns>Componente NeoComponent inizializzato</returns>
        public static NeoComponent Default()
        {
            return new NeoComponent
            {
                CodeSight = 0.9f,                 // 90% visibilità di codice nascosto
                DataCorruptionResistance = 0.7f,  // -70% danni da corruzione dati
                GlitchManipulation = 0.6f,        // 60% efficacia nella manipolazione glitch
                HackingEfficiency = 0.8f,         // 80% efficienza nei mini-giochi hacking
                FirewallBypass = 0.5f,            // 50% probabilità di bypassare firewall
                SystemRestoration = 0.4f,         // +40% efficacia cura in ambiente virtuale
                DataTransferSpeed = 1.3f,         // +30% velocità in stream di dati
                MalwareImmunity = 0.6f            // -60% danni da virus e malware
            };
        }
        
        /// <summary>
        /// Versione potenziata di Neo (livello medio)
        /// </summary>
        public static NeoComponent Advanced()
        {
            var neo = Default();
            neo.CodeSight = 1.0f;                 // 100% visibilità di codice nascosto
            neo.DataCorruptionResistance = 0.85f; // Potenziato a -85%
            neo.GlitchManipulation = 0.8f;        // Potenziato a 80%
            neo.FirewallBypass = 0.7f;            // Potenziato a 70%
            neo.DataTransferSpeed = 1.5f;         // Potenziato a +50%
            return neo;
        }
        
        /// <summary>
        /// Versione completamente potenziata di Neo (fine gioco)
        /// </summary>
        public static NeoComponent Master()
        {
            var neo = Advanced();
            neo.DataCorruptionResistance = 0.95f; // Quasi immune (-95%)
            neo.GlitchManipulation = 1.0f;        // Manipolazione glitch perfetta
            neo.HackingEfficiency = 1.0f;         // Efficienza hacking perfetta
            neo.FirewallBypass = 0.9f;            // 90% probabilità di bypass
            neo.SystemRestoration = 0.8f;         // +80% efficacia cura
            neo.DataTransferSpeed = 2.0f;         // +100% velocità in stream di dati
            neo.MalwareImmunity = 0.9f;           // -90% danni da virus
            return neo;
        }
        
        /// <summary>
        /// Calcola la probabilità di individuare un glitch nascosto
        /// </summary>
        /// <param name="glitchHiddenFactor">Fattore di occultamento del glitch (0-1)</param>
        /// <returns>True se il glitch è individuato, false altrimenti</returns>
        public bool DetectHiddenGlitch(float glitchHiddenFactor)
        {
            // Più alto è CodeSight, più è probabile individuare glitch nascosti
            return UnityEngine.Random.value < (CodeSight - glitchHiddenFactor * 0.5f);
        }
        
        /// <summary>
        /// Calcola la riduzione del danno da corruzione dati
        /// </summary>
        /// <param name="corruptionDamage">Danno da corruzione base</param>
        /// <returns>Danno da corruzione ridotto</returns>
        public float ReduceCorruptionDamage(float corruptionDamage)
        {
            return corruptionDamage * (1f - DataCorruptionResistance);
        }
        
        /// <summary>
        /// Calcola la probabilità di bypassare un firewall
        /// </summary>
        /// <param name="firewallStrength">Forza del firewall (0-1)</param>
        /// <returns>True se il bypass ha successo, false altrimenti</returns>
        public bool BypassFirewall(float firewallStrength)
        {
            return UnityEngine.Random.value < (FirewallBypass / firewallStrength);
        }
        
        /// <summary>
        /// Calcola la velocità effettiva nei data stream
        /// </summary>
        /// <param name="baseSpeed">Velocità base di movimento</param>
        /// <returns>Velocità potenziata nei data stream</returns>
        public float CalculateDataStreamSpeed(float baseSpeed)
        {
            return baseSpeed * DataTransferSpeed;
        }
    }
}