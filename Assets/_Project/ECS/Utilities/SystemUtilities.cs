using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Utilities
{
    /// <summary>
    /// Classe di utilità che fornisce metodi e funzioni helper per l'implementazione
    /// e l'ottimizzazione dei sistemi ECS. Include strumenti per profiling, gestione 
    /// dei job, creazione di query ottimizzate e altre operazioni comuni.
    /// </summary>
    public static class SystemUtilities
    {

        /// <summary>
        /// Registra un job come dipendenza per un sistema con gestione degli errori.
        /// </summary>
        /// <param name="system">Sistema in cui registrare la dipendenza</param>
        /// <param name="jobHandle">Handle del job da registrare</param>
        /// <param name="jobName">Nome del job per scopi di diagnostica</param>
        /// <returns>Lo stesso jobHandle per consentire catene di chiamate</returns>
        public static Unity.Jobs.JobHandle RegisterJobDependency(SystemBase system, Unity.Jobs.JobHandle currentDependency, Unity.Jobs.JobHandle jobHandle, string jobName)
        {
            try
            {
                // Combine the dependencies and return the new handle
                return Unity.Jobs.JobHandle.CombineDependencies(currentDependency, jobHandle);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Errore nella registrazione del job '{jobName}': {ex.Message}");
                return currentDependency; // Return the original dependency in case of error
            }
        }

        /// <summary>
        /// Configura un timer per il profiling delle prestazioni di un sistema.
        /// </summary>
        /// <param name="system">Sistema da profilare</param>
        /// <param name="profileName">Nome identificativo per il profiler</param>
        /// <returns>Un oggetto IDisposable che alla Dispose ferma il timer</returns>
        public static System.IDisposable BeginSystemProfiling(SystemBase system, string profileName)
        {
            // Implementazione di esempio - da completare con logica effettiva di profiling
            UnityEngine.Profiling.Profiler.BeginSample($"ECS System: {profileName}");
            return new ProfilerScope();
        }

        /// <summary>
        /// Oggetto helper per gestire lo scope del profiler.
        /// </summary>
        private class ProfilerScope : System.IDisposable
        {
            public void Dispose()
            {
                UnityEngine.Profiling.Profiler.EndSample();
            }
        }

        /// <summary>
        /// Verifica se un sistema deve essere aggiornato in base alla presenza di entità
        /// che corrispondono ai requisiti e altre condizioni opzionali.
        /// </summary>
        /// <param name="system">Sistema da verificare</param>
        /// <param name="query">Query da utilizzare per verificare la presenza di entità</param>
        /// <param name="additionalCondition">Condizione aggiuntiva opzionale</param>
        /// <returns>True se il sistema deve essere aggiornato, False altrimenti</returns>
        public static bool ShouldUpdateSystem(SystemBase system, EntityQuery query, System.Func<bool> additionalCondition = null)
        {
            if (query.IsEmptyIgnoreFilter)
                return false;

            if (additionalCondition != null && !additionalCondition())
                return false;

            return true;
        }

        // TODO: Implementare altri metodi di utilità per sistemi
    }
}
