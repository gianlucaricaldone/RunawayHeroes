using Unity.Entities;

namespace RunawayHeroes.ECS.Events
{
    /// <summary>
    /// Evento generato quando un livello è completato con successo.
    /// Contiene informazioni sul livello e le statistiche di completamento.
    /// </summary>
    public struct LevelCompletedEvent : IComponentData
    {
        /// <summary>
        /// ID univoco del livello completato
        /// </summary>
        public int LevelID;
        
        /// <summary>
        /// Punteggio finale ottenuto nel livello
        /// </summary>
        public float FinalScore;
        
        /// <summary>
        /// Tempo impiegato per completare il livello in secondi
        /// </summary>
        public float TimeElapsed;
        
        /// <summary>
        /// Numero di frammenti raccolti durante il livello
        /// </summary>
        public byte FragmentsCollected;
    }
}
