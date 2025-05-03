using System;
using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.UI
{
    /// <summary>
    /// Componente che rappresenta un messaggio UI da visualizzare.
    /// Utilizzato principalmente per i messaggi di istruzione del tutorial.
    /// </summary>
    [Serializable]
    public struct UIMessageComponent : IComponentData
    {
        /// <summary>
        /// Testo del messaggio
        /// </summary>
        public FixedString128Bytes Message;
        
        /// <summary>
        /// Durata in secondi per cui il messaggio deve essere visualizzato
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Tempo rimanente prima che il messaggio scompaia
        /// </summary>
        public float RemainingTime;
        
        /// <summary>
        /// Tipo di messaggio
        /// 0 = Istruzione Tutorial
        /// 1 = Notifica
        /// 2 = Avviso
        /// </summary>
        public byte MessageType;
        
        /// <summary>
        /// Se true, il messaggio non scomparirà automaticamente quando Duration è trascorsa
        /// </summary>
        public bool IsPersistent;
        
        /// <summary>
        /// Identificatore univoco per riferimenti esterni
        /// </summary>
        public int MessageId;
    }
    
    /// <summary>
    /// Tag per identificare un messaggio attualmente visualizzato nell'UI
    /// </summary>
    [Serializable]
    public struct ActiveMessageTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag per identificare un messaggio in coda di visualizzazione
    /// </summary>
    [Serializable]
    public struct QueuedMessageTag : IComponentData
    {
        public int QueuePosition;
    }
    
    /// <summary>
    /// Evento generato quando un nuovo messaggio deve essere visualizzato
    /// </summary>
    [Serializable]
    public struct MessageShowEvent : IComponentData
    {
        public Entity MessageEntity;
    }
    
    /// <summary>
    /// Evento generato quando un messaggio deve essere nascosto
    /// </summary>
    [Serializable]
    public struct MessageHideEvent : IComponentData
    {
        public Entity MessageEntity;
        public bool Forced; // Se true, il messaggio è stato nascosto forzatamente
    }
}