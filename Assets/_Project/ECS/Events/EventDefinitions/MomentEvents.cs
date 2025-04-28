using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Events.EventDefinitions
{
    /// <summary>
    /// Evento generato quando un personaggio inizia a saltare
    /// </summary>
    public struct JumpStartedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha saltato
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Forza applicata per il salto
        /// </summary>
        public float JumpForce;
        
        /// <summary>
        /// Numero di salti rimanenti dopo questo salto
        /// </summary>
        public int RemainingJumps;
        
        /// <summary>
        /// Posizione da cui è iniziato il salto
        /// </summary>
        public float3 JumpPosition;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio atterra
    /// </summary>
    public struct LandingEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che è atterrato
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Velocità di discesa al momento dell'atterraggio (negativa)
        /// </summary>
        public float LandingVelocity;
        
        /// <summary>
        /// Posizione di atterraggio
        /// </summary>
        public float3 LandingPosition;
        
        /// <summary>
        /// Indica se l'atterraggio è considerato "pesante" (alta velocità)
        /// </summary>
        public bool IsHardLanding;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio inizia a scivolare
    /// </summary>
    public struct SlideStartedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha iniziato a scivolare
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Durata prevista della scivolata
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Posizione di inizio scivolata
        /// </summary>
        public float3 StartPosition;
        
        /// <summary>
        /// Velocità all'inizio della scivolata
        /// </summary>
        public float InitialSpeed;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio termina una scivolata
    /// </summary>
    public struct SlideEndedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha terminato la scivolata
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Posizione di fine scivolata
        /// </summary>
        public float3 EndPosition;
        
        /// <summary>
        /// Distanza totale percorsa durante la scivolata
        /// </summary>
        public float SlideDistance;
        
        /// <summary>
        /// Durata effettiva della scivolata
        /// </summary>
        public float ActualDuration;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio inizia a muoversi
    /// </summary>
    public struct MovementStartedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha iniziato a muoversi
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Posizione di inizio movimento
        /// </summary>
        public float3 StartPosition;
        
        /// <summary>
        /// Direzione iniziale del movimento
        /// </summary>
        public float3 InitialDirection;
    }
    
    /// <summary>
    /// Evento generato quando un personaggio smette di muoversi
    /// </summary>
    public struct MovementStoppedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha smesso di muoversi
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Posizione finale
        /// </summary>
        public float3 StopPosition;
        
        /// <summary>
        /// Velocità al momento dell'arresto
        /// </summary>
        public float3 FinalVelocity;
    }
    
    /// <summary>
    /// Evento generato quando cambia lo stato di animazione
    /// </summary>
    public struct AnimationStateChangedEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Nuovo stato dell'animazione
        /// </summary>
        public MovementAnimationState State;
        
        /// <summary>
        /// Velocità corrente dell'entità, usata per regolare la velocità dell'animazione
        /// </summary>
        public float Speed;
        
        /// <summary>
        /// Indicatore di blend tra stati per transizioni fluide
        /// </summary>
        public float BlendFactor;
    }
    
    /// <summary>
    /// Evento generato quando il personaggio urta un ostacolo
    /// </summary>
    public struct ObstacleHitEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore che ha urtato l'ostacolo
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità dell'ostacolo colpito
        /// </summary>
        public Entity ObstacleEntity;
        
        /// <summary>
        /// Posizione dell'impatto
        /// </summary>
        public float3 ImpactPosition;
        
        /// <summary>
        /// Normale della superficie al punto di impatto
        /// </summary>
        public float3 ImpactNormal;
        
        /// <summary>
        /// Velocità relativa al momento dell'impatto
        /// </summary>
        public float ImpactVelocity;
        
        /// <summary>
        /// Quantità di danni causati dall'impatto
        /// </summary>
        public float DamageAmount;
    }
    
    /// <summary>
    /// Enumerazione degli stati di animazione per il movimento
    /// </summary>
    public enum MovementAnimationState : byte
    {
        /// <summary>
        /// Personaggio fermo
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// Personaggio in corsa
        /// </summary>
        Run = 1,
        
        /// <summary>
        /// Personaggio in salto (fase ascendente)
        /// </summary>
        Jump = 2,
        
        /// <summary>
        /// Personaggio in caduta (fase discendente)
        /// </summary>
        Fall = 3,
        
        /// <summary>
        /// Personaggio in scivolata
        /// </summary>
        Slide = 4,
        
        /// <summary>
        /// Personaggio in fase di atterraggio
        /// </summary>
        Land = 5,
        
        /// <summary>
        /// Personaggio colpito da un ostacolo
        /// </summary>
        Hit = 6,
        
        /// <summary>
        /// Personaggio che utilizza l'abilità speciale
        /// </summary>
        Ability = 7
    }
}