// File: Assets/_Project/ECS/Events/EventDefinitions/FocusTimeEvents.cs

using Unity.Entities;

namespace RunawayHeroes.ECS.Events.EventDefinitions
{
    /// <summary>
    /// Evento generato quando il Focus Time viene attivato
    /// </summary>
    public struct FocusTimeActivatedEvent : IComponentData
    {
        /// <summary>
        /// Entità che ha attivato il Focus Time
        /// </summary>
        public Entity EntityActivated;
        
        /// <summary>
        /// Durata del Focus Time attivato
        /// </summary>
        public float Duration;
        
        /// <summary>
        /// Fattore di scala temporale durante il Focus Time
        /// </summary>
        public float TimeScale;
    }
    
    /// <summary>
    /// Evento generato quando il Focus Time viene disattivato
    /// </summary>
    public struct FocusTimeDeactivatedEvent : IComponentData
    {
        /// <summary>
        /// Entità che ha disattivato il Focus Time
        /// </summary>
        public Entity EntityDeactivated;
        
        /// <summary>
        /// Tempo di cooldown rimanente
        /// </summary>
        public float CooldownRemaining;
    }
    
    /// <summary>
    /// Evento generato quando il Focus Time è pronto dopo un cooldown
    /// </summary>
    public struct FocusTimeReadyEvent : IComponentData
    {
        /// <summary>
        /// Entità il cui Focus Time è pronto
        /// </summary>
        public Entity EntityReady;
    }
    
    /// <summary>
    /// Evento generato quando l'energia del Focus Time è completamente ricaricata
    /// </summary>
    public struct FocusTimeFullEnergyEvent : IComponentData
    {
        /// <summary>
        /// Entità con energia Focus Time completamente ricaricata
        /// </summary>
        public Entity EntityWithFullEnergy;
    }
    
    /// <summary>
    /// Evento generato quando un oggetto viene aggiunto a uno slot del Focus Time
    /// </summary>
    public struct ItemAddedToSlotEvent : IComponentData
    {
        /// <summary>
        /// Entità che ha ricevuto l'oggetto
        /// </summary>
        public Entity UserEntity;
        
        /// <summary>
        /// Entità oggetto aggiunto
        /// </summary>
        public Entity ItemEntity;
        
        /// <summary>
        /// Indice dello slot a cui è stato aggiunto l'oggetto
        /// </summary>
        public int SlotIndex;
    }
    
    /// <summary>
    /// Evento generato quando un oggetto viene utilizzato durante il Focus Time
    /// </summary>
    public struct ItemUsedEvent : IComponentData
    {
        /// <summary>
        /// Entità che ha utilizzato l'oggetto
        /// </summary>
        public Entity UserEntity;
        
        /// <summary>
        /// Entità oggetto utilizzato
        /// </summary>
        public Entity ItemEntity;
        
        /// <summary>
        /// Indice dello slot da cui è stato utilizzato l'oggetto
        /// </summary>
        public int SlotIndex;
    }
}