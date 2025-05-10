// Path: Assets/_Project/Utilities/ECSCompatibility/WorldCompatibility.cs
using Unity.Entities;
using UnityEngine;

namespace RunawayHeroes.Utilities.ECSCompatibility
{
    /// <summary>
    /// Classe di utilità che fornisce compatibilità tra diverse versioni di Unity DOTS.
    /// Implementa metodi di estensione per World per supportare proprietà mancanti.
    /// </summary>
    public static class RunawayWorldCompatibility
    {
        private static World _defaultGameObjectInjectionWorld;

        /// <summary>
        /// Ottiene o imposta il World predefinito per l'iniezione di GameObject.
        /// Questa proprietà è disponibile nelle versioni più recenti di Entities.
        /// Questa implementazione offre compatibilità per versioni dove questa proprietà non esiste.
        /// </summary>
        public static World DefaultGameObjectInjectionWorld
        {
            get
            {
                // Se non è stato impostato, restituisci il mondo predefinito
                if (_defaultGameObjectInjectionWorld == null || !_defaultGameObjectInjectionWorld.IsCreated)
                {
                    _defaultGameObjectInjectionWorld = World.DefaultGameObjectInjectionWorld_Internal();
                }
                return _defaultGameObjectInjectionWorld;
            }
            set
            {
                _defaultGameObjectInjectionWorld = value;
            }
        }

        /// <summary>
        /// Implementazione interna per ottenere il World predefinito.
        /// </summary>
        private static World DefaultGameObjectInjectionWorld_Internal()
        {
            // Cerca di ottenere il mondo di default
            foreach (var world in World.All)
            {
                if (world.Name == "Default World")
                {
                    return world;
                }
            }

            // Se non esiste, crea un nuovo mondo
            Debug.LogWarning("Nessun world 'Default World' trovato. Creazione di un nuovo world predefinito.");
            return new World("Default World");
        }
    }

    /// <summary>
    /// Estensione per la classe World che aggiunge proprietà mancanti.
    /// </summary>
    public static class RunawayWorldExtensions
    {
        /// <summary>
        /// Estensione che fornisce accesso a DefaultGameObjectInjectionWorld
        /// </summary>
        public static World DefaultGameObjectInjectionWorld
        {
            get { return RunawayWorldCompatibility.DefaultGameObjectInjectionWorld; }
            set { RunawayWorldCompatibility.DefaultGameObjectInjectionWorld = value; }
        }
    }
}