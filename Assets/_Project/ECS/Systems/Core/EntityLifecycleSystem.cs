using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema che gestisce il ciclo di vita delle entità nel mondo di gioco.
    /// Responsabile della creazione, distruzione e gestione dello stato di attivazione
    /// delle entità in base a criteri come distanza dalla telecamera, tempo di vita,
    /// o eventi di gioco.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EntityLifecycleSystem : SystemBase
    {
        /// <summary>
        /// Inizializza il sistema di gestione del ciclo di vita delle entità e 
        /// configura le query necessarie.
        /// </summary>
        protected override void OnCreate()
        {
            // TODO: Inizializzare query di entità e command buffer
        }

        /// <summary>
        /// Gestisce la creazione, distruzione e lo stato di attivazione delle entità
        /// in base a vari criteri come distanza, tempo di vita, o eventi specifici.
        /// </summary>
        protected override void OnUpdate()
        {
            // TODO: Implementare la logica di gestione del ciclo di vita
            // - Distruggere entità che hanno raggiunto il loro tempo di vita massimo
            // - Disattivare/attivare entità in base alla distanza dalla telecamera
            // - Gestire pool di entità per oggetti riutilizzabili
        }
    }
}
