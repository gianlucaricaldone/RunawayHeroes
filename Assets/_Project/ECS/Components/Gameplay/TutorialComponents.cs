using System;
using System.Diagnostics;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Informazioni aggiuntive sul livello tutorial
    /// </summary>
    [Serializable]
    public struct TutorialLevelInfoComponent : IComponentData
    {
        public FixedString128Bytes Description;  // Descrizione del livello tutorial
        public int Difficulty;                  // Livello di difficoltà (1-10)
        public bool IsLastTutorial;             // Indica se è l'ultimo tutorial della sequenza
    }
    
    /// <summary>
    /// Buffer di scenari di insegnamento per un livello tutorial
    /// </summary>
    [Serializable]
    public struct TutorialScenarioBuffer : IBufferElementData
    {
        public FixedString64Bytes Name;                // Nome dello scenario
        public float DistanceFromStart;                // Distanza dall'inizio del livello in metri
        public FixedString128Bytes InstructionMessage; // Messaggio di istruzione da mostrare
        public float MessageDuration;                  // Durata del messaggio in secondi
        public bool Triggered;                         // Indica se lo scenario è stato già attivato
        public bool RandomPlacement;                   // Se posizionare gli ostacoli in modo casuale
        public float ObstacleSpacing;                  // Spaziatura tra gli ostacoli
    }
    
    /// <summary>
    /// Buffer per gli ostacoli in uno scenario di insegnamento
    /// </summary>
    [Serializable]
    public struct TutorialObstacleBuffer : IBufferElementData
    {
        public FixedString32Bytes ObstacleCode;       // Codice dell'ostacolo (es. "U01")
        public int Count;                             // Numero di istanze
        public byte Placement;                        // Posizionamento (0=centro, 1=sinistra, 2=destra, 3=casuale, 4=pattern)
        public bool RandomizeHeight;                  // Se variare l'altezza
        public float2 HeightRange;                    // Range per la randomizzazione dell'altezza (min, max)
        public bool RandomizeScale;                   // Se variare la scala
        public float2 ScaleRange;                     // Range per la randomizzazione della scala (min, max)
        public float StartOffset;                     // Offset dall'inizio dello scenario
    }
    
    /// <summary>
    /// Riferimento allo scenario di insegnamento
    /// </summary>
    [Serializable]
    public struct ScenarioReference : IComponentData
    {
        public Entity TutorialLevelEntity;            // Riferimento all'entità del livello tutorial
        public int ScenarioIndex;                     // Indice dello scenario nel buffer
    }
    
    /// <summary>
    /// Componente per tracciare il progresso del giocatore nei tutorial
    /// </summary>
    [Serializable]
    public struct TutorialProgressComponent : IComponentData
    {
        public int CompletedTutorialCount;    // Numero di tutorial completati
        public int HighestUnlockedTutorial;   // Indice del tutorial più alto sbloccato
        public bool TutorialsCompleted;       // Flag che indica se tutti i tutorial sono stati completati
    }
    
    /// <summary>
    /// Tag interno per identificare il completamento di un livello tutorial
    /// </summary>
    [Serializable]
    public struct TutorialCompletionTag : IComponentData
    {
        public int CompletedTutorialIndex;  // Indice del tutorial completato
    }
    
    /// <summary>
    /// Evento generato quando viene completato un tutorial
    /// </summary>
    [Serializable]
    public struct TutorialFinishedEvent : IComponentData
    {
        public int CompletedTutorialIndex;   // Indice del tutorial completato
        public bool AllTutorialsCompleted;   // Flag che indica se tutti i tutorial sono stati completati
        public int NextTutorialToUnlock;     // Indice del prossimo tutorial da sbloccare
    }
    
    /// <summary>
    /// Tag per identificare uno scenario di insegnamento che è pronto per essere attivato
    /// </summary>
    [Serializable]
    public struct ScenarioActivationTag : IComponentData
    {
        public Entity ScenarioEntity;  // Riferimento all'entità dello scenario
        public int ScenarioIndex;      // Indice dello scenario
        public float ActivationTime;   // Tempo di attivazione
    }
}