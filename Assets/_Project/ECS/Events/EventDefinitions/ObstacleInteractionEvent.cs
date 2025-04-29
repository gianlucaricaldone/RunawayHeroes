using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Events.EventDefinitions
{
    /// <summary>
    /// Evento per l'attraversamento della lava
    /// </summary>
    public struct LavaWalkingEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity EntityID;
        
        /// <summary>
        /// Posizione dove avviene l'attraversamento
        /// </summary>
        public float3 Position;
    }

    /// <summary>
    /// Evento per l'inizio del teletrasporto tramite Glitch
    /// </summary>
    public struct GlitchTeleportStartEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity EntityID;
        
        /// <summary>
        /// Posizione di partenza
        /// </summary>
        public float3 StartPosition;
        
        /// <summary>
        /// Posizione di destinazione
        /// </summary>
        public float3 TargetPosition;
        
        /// <summary>
        /// Riferimento all'entità della barriera attraversata
        /// </summary>
        public Entity BarrierEntity;
    }

    /// <summary>
    /// Evento per la fine del teletrasporto tramite Glitch
    /// </summary>
    public struct GlitchTeleportEndEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity EntityID;
        
        /// <summary>
        /// Posizione finale dopo il teletrasporto
        /// </summary>
        public float3 FinalPosition;
    }

    /// <summary>
    /// Evento per la repulsione di un nemico
    /// </summary>
    public struct EnemyRepulsionEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del nemico respinto
        /// </summary>
        public Entity EnemyEntity;
        
        /// <summary>
        /// Riferimento all'entità che causa la repulsione
        /// </summary>
        public Entity SourceEntity;
        
        /// <summary>
        /// Forza della repulsione
        /// </summary>
        public float RepulsionForce;
        
        /// <summary>
        /// Direzione della repulsione
        /// </summary>
        public float3 Direction;
    }

    /// <summary>
    /// Evento per lo scioglimento completo di un ostacolo di ghiaccio
    /// </summary>
    public struct IceMeltedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità che causa lo scioglimento
        /// </summary>
        public Entity SourceEntity;
        
        /// <summary>
        /// Riferimento all'entità di ghiaccio sciolta
        /// </summary>
        public Entity IceEntity;
        
        /// <summary>
        /// Posizione dove è avvenuto lo scioglimento
        /// </summary>
        public float3 Position;
        
        /// <summary>
        /// Dimensione dell'elemento sciolto
        /// </summary>
        public float Size;
    }

    /// <summary>
    /// Evento per l'interazione con superfici scivolose
    /// </summary>
    public struct SlipInteractionEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità della superficie scivolosa
        /// </summary>
        public Entity SurfaceEntity;
        
        /// <summary>
        /// Fattore di scivolosità effettivo (considerando abilità e boost)
        /// </summary>
        public float EffectiveSlipFactor;
        
        /// <summary>
        /// Indica se l'effetto è stato negato da un'abilità
        /// </summary>
        public bool IsNegated;
    }

    /// <summary>
    /// Evento per l'interazione con correnti (aria/acqua)
    /// </summary>
    public struct CurrentInteractionEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità della corrente
        /// </summary>
        public Entity CurrentEntity;
        
        /// <summary>
        /// Forza effettiva applicata (considerando abilità e boost)
        /// </summary>
        public float EffectiveForce;
        
        /// <summary>
        /// Direzione della corrente
        /// </summary>
        public float3 Direction;
        
        /// <summary>
        /// Indica se l'effetto è stato negato da un'abilità
        /// </summary>
        public bool IsNegated;
    }

    /// <summary>
    /// Evento per l'interazione con zone prive di ossigeno
    /// </summary>
    public struct OxygenInteractionEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità della zona senza ossigeno
        /// </summary>
        public Entity NoOxygenZoneEntity;
        
        /// <summary>
        /// Quantità di ossigeno rimanente
        /// </summary>
        public float RemainingOxygen;
        
        /// <summary>
        /// Indica se l'effetto è stato negato da un'abilità
        /// </summary>
        public bool IsNegated;
    }
}