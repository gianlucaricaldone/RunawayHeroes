using System;
using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che rappresenta un obiettivo specifico da completare nel tutorial
    /// </summary>
    [Serializable]
    public struct ObjectiveComponent : IComponentData
    {
        /// <summary>
        /// Tipo di obiettivo
        /// 0 = Raggiungi la fine del livello
        /// 1 = Supera N ostacoli
        /// 2 = Raccogli N oggetti
        /// 3 = Supera tutti gli ostacoli senza subire danni
        /// 4 = Usa abilità specifica X volte
        /// 5 = Completa il livello in meno di X secondi
        /// </summary>
        public byte ObjectiveType;
        
        /// <summary>
        /// Valore richiesto (numero di ostacoli, oggetti, ecc.)
        /// </summary>
        public int RequiredValue;
        
        /// <summary>
        /// Progresso attuale
        /// </summary>
        public int CurrentProgress;
        
        /// <summary>
        /// Identificativo scenario associato
        /// </summary>
        public int ScenarioId;
        
        /// <summary>
        /// Descrizione dell'obiettivo mostrata al giocatore
        /// </summary>
        public FixedString128Bytes Description;
        
        /// <summary>
        /// Flag che indica se l'obiettivo è stato completato
        /// </summary>
        public bool IsCompleted;
        
        /// <summary>
        /// Flag che indica se l'obiettivo è facoltativo
        /// </summary>
        public bool IsOptional;
    }
    
    /// <summary>
    /// Tag per indicare un obiettivo attualmente attivo
    /// </summary>
    [Serializable]
    public struct ActiveObjectiveTag : IComponentData
    {
    }
    
    /// <summary>
    /// Componente per tracciare il progresso numerico di un obiettivo
    /// </summary>
    [Serializable]
    public struct ObjectiveProgressComponent : IComponentData
    {
        /// <summary>
        /// Numero corrente di oggetti raccolti, ostacoli superati, ecc.
        /// </summary>
        public int Count;
        
        /// <summary>
        /// Valore massimo richiesto
        /// </summary>
        public int MaxCount;
        
        /// <summary>
        /// Timestamp di inizio dell'obiettivo (per obiettivi a tempo)
        /// </summary>
        public float StartTime;
        
        /// <summary>
        /// Tempo limite per completare l'obiettivo (per obiettivi a tempo)
        /// </summary>
        public float TimeLimit;
    }
    
    /// <summary>
    /// Evento generato quando un obiettivo viene completato
    /// </summary>
    [Serializable]
    public struct ObjectiveCompletedEvent : IComponentData
    {
        /// <summary>
        /// Entità dell'obiettivo completato
        /// </summary>
        public Entity ObjectiveEntity;
        
        /// <summary>
        /// Tipo di obiettivo completato
        /// </summary>
        public byte ObjectiveType;
        
        /// <summary>
        /// ID dello scenario associato
        /// </summary>
        public int ScenarioId;
        
        /// <summary>
        /// Flag che indica se l'obiettivo era obbligatorio
        /// </summary>
        public bool WasRequired;
    }
    
    /// <summary>
    /// Evento generato quando si verifica un progresso in un obiettivo
    /// </summary>
    [Serializable]
    public struct ObjectiveProgressEvent : IComponentData
    {
        /// <summary>
        /// Entità dell'obiettivo in progresso
        /// </summary>
        public Entity ObjectiveEntity;
        
        /// <summary>
        /// Valore precedente
        /// </summary>
        public int PreviousValue;
        
        /// <summary>
        /// Nuovo valore
        /// </summary>
        public int NewValue;
        
        /// <summary>
        /// Valore massimo richiesto
        /// </summary>
        public int RequiredValue;
    }
}