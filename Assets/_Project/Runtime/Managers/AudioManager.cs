// Path: Assets/_Project/Runtime/Managers/AudioManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RunawayHeroes.Runtime.Managers
{
    /// <summary>
    /// Gestore centralizzato del sistema audio del gioco.
    /// Supporta effetti sonori, musica di sottofondo e transizioni audio.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // Singleton pattern
        public static AudioManager Instance { get; private set; }
        
        [System.Serializable]
        public class Sound
        {
            public string name;
            public AudioClip clip;
            [Range(0f, 1f)]
            public float volume = 1f;
            [Range(0.1f, 3f)]
            public float pitch = 1f;
            [Range(0f, 1f)]
            public float spatialBlend = 0f;
            public bool loop = false;
            
            [HideInInspector]
            public AudioSource source;
        }
        
        [Header("Audio Settings")]
        [SerializeField] private Sound[] sfxSounds;
        [SerializeField] private Sound[] musicSounds;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        
        [Header("Volume Settings")]
        [SerializeField, Range(0, 1)] private float masterVolume = 1f;
        [SerializeField, Range(0, 1)] private float sfxVolume = 1f;
        [SerializeField, Range(0, 1)] private float musicVolume = 1f;
        
        private Dictionary<string, Sound> _soundsDictionary = new Dictionary<string, Sound>();
        private string _currentMusic;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Inizializza tutti i suoni
                InitializeSounds();
                
                // Carica le preferenze di volume se disponibili
                LoadVolumeSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeSounds()
        {
            // Inizializza gli effetti sonori
            foreach (Sound s in sfxSounds)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = s.clip;
                source.volume = s.volume * sfxVolume * masterVolume;
                source.pitch = s.pitch;
                source.loop = s.loop;
                source.spatialBlend = s.spatialBlend;
                source.outputAudioMixerGroup = sfxMixerGroup;
                s.source = source;
                
                // Aggiungi al dizionario per accesso rapido
                _soundsDictionary[s.name] = s;
            }
            
            // Inizializza i brani musicali
            foreach (Sound s in musicSounds)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.clip = s.clip;
                source.volume = s.volume * musicVolume * masterVolume;
                source.pitch = s.pitch;
                source.loop = true; // La musica in genere è riprodotta in loop
                source.spatialBlend = 0f; // La musica è in genere 2D
                source.outputAudioMixerGroup = musicMixerGroup;
                s.source = source;
                
                // Aggiungi al dizionario per accesso rapido
                _soundsDictionary[s.name] = s;
            }
        }
        
        /// <summary>
        /// Riproduce un effetto sonoro.
        /// </summary>
        /// <param name="soundName">Nome dell'effetto sonoro da riprodurre</param>
        public void PlaySound(string soundName)
        {
            if (_soundsDictionary.TryGetValue(soundName, out Sound s))
            {
                if (!s.source.isPlaying)
                {
                    s.source.Play();
                }
            }
            else
            {
                Debug.LogWarning($"Sound: {soundName} not found in AudioManager");
            }
        }
        
        /// <summary>
        /// Riproduce un effetto sonoro una sola volta, anche se già in esecuzione.
        /// </summary>
        /// <param name="soundName">Nome dell'effetto sonoro da riprodurre</param>
        public void PlaySoundOneShot(string soundName)
        {
            if (_soundsDictionary.TryGetValue(soundName, out Sound s))
            {
                s.source.PlayOneShot(s.clip, s.volume * sfxVolume * masterVolume);
            }
            else
            {
                Debug.LogWarning($"Sound: {soundName} not found in AudioManager");
            }
        }
        
        /// <summary>
        /// Riproduce un brano musicale con dissolvenza.
        /// </summary>
        /// <param name="musicName">Nome del brano musicale da riprodurre</param>
        /// <param name="fadeTime">Tempo di dissolvenza in secondi</param>
        public void PlayMusic(string musicName, float fadeTime = 1.0f)
        {
            // Se è lo stesso brano, non fare nulla
            if (_currentMusic == musicName)
                return;
                
            // Interrompi la musica attuale con dissolvenza
            if (!string.IsNullOrEmpty(_currentMusic) && _soundsDictionary.TryGetValue(_currentMusic, out Sound currentSound))
            {
                StartCoroutine(FadeOut(currentSound.source, fadeTime));
            }
            
            // Avvia la nuova musica con dissolvenza
            if (_soundsDictionary.TryGetValue(musicName, out Sound newSound))
            {
                _currentMusic = musicName;
                newSound.source.volume = 0f;
                newSound.source.Play();
                StartCoroutine(FadeIn(newSound.source, newSound.volume * musicVolume * masterVolume, fadeTime));
            }
            else
            {
                Debug.LogWarning($"Music: {musicName} not found in AudioManager");
            }
        }
        
        /// <summary>
        /// Interrompe un suono.
        /// </summary>
        /// <param name="soundName">Nome del suono da interrompere</param>
        public void StopSound(string soundName)
        {
            if (_soundsDictionary.TryGetValue(soundName, out Sound s))
            {
                s.source.Stop();
            }
        }
        
        /// <summary>
        /// Interrompe tutti i suoni.
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var sound in _soundsDictionary.Values)
            {
                sound.source.Stop();
            }
        }
        
        /// <summary>
        /// Mette in pausa tutti i suoni.
        /// </summary>
        public void PauseAllSounds()
        {
            foreach (var sound in _soundsDictionary.Values)
            {
                if (sound.source.isPlaying)
                {
                    sound.source.Pause();
                }
            }
        }
        
        /// <summary>
        /// Riprende tutti i suoni in pausa.
        /// </summary>
        public void ResumeAllSounds()
        {
            foreach (var sound in _soundsDictionary.Values)
            {
                if (!sound.source.isPlaying)
                {
                    sound.source.UnPause();
                }
            }
        }
        
        /// <summary>
        /// Imposta il volume master.
        /// </summary>
        /// <param name="volume">Volume da 0 a 1</param>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// Imposta il volume degli effetti sonori.
        /// </summary>
        /// <param name="volume">Volume da 0 a 1</param>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        /// <summary>
        /// Imposta il volume della musica.
        /// </summary>
        /// <param name="volume">Volume da 0 a 1</param>
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        // Aggiorna i volumi di tutti i suoni
        private void UpdateAllVolumes()
        {
            foreach (Sound s in sfxSounds)
            {
                if (s.source != null)
                {
                    s.source.volume = s.volume * sfxVolume * masterVolume;
                }
            }
            
            foreach (Sound s in musicSounds)
            {
                if (s.source != null)
                {
                    s.source.volume = s.volume * musicVolume * masterVolume;
                }
            }
        }
        
        // Salva le impostazioni di volume nelle PlayerPrefs
        private void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.Save();
        }
        
        // Carica le impostazioni di volume dalle PlayerPrefs
        private void LoadVolumeSettings()
        {
            if (PlayerPrefs.HasKey("MasterVolume"))
            {
                masterVolume = PlayerPrefs.GetFloat("MasterVolume");
                sfxVolume = PlayerPrefs.GetFloat("SFXVolume");
                musicVolume = PlayerPrefs.GetFloat("MusicVolume");
                UpdateAllVolumes();
            }
        }
        
        // Coroutine per la dissolvenza in entrata
        private System.Collections.IEnumerator FadeIn(AudioSource audioSource, float targetVolume, float duration)
        {
            float currentTime = 0;
            float start = audioSource.volume;
            
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, targetVolume, currentTime / duration);
                yield return null;
            }
            
            audioSource.volume = targetVolume;
        }
        
        // Coroutine per la dissolvenza in uscita
        private System.Collections.IEnumerator FadeOut(AudioSource audioSource, float duration)
        {
            float currentTime = 0;
            float start = audioSource.volume;
            
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(start, 0, currentTime / duration);
                yield return null;
            }
            
            audioSource.Stop();
            audioSource.volume = start;
        }

        /// <summary>
        /// Riproduce un effetto sonoro (alias per PlaySound).
        /// </summary>
        /// <param name="sfxName">Nome dell'effetto sonoro da riprodurre</param>
        public void PlaySFX(string sfxName)
        {
            PlaySound(sfxName);
        }
    }
}