using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace RunawayHeroes.Manager
{
    /// <summary>
    /// Gestisce tutti gli aspetti audio del gioco, inclusi musica, effetti sonori
    /// e dialoghi. Supporta il mixaggio, il fade tra tracce e l'adattamento della
    /// musica in base agli eventi di gioco.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource voiceSource;
        
        [Header("Audio Mixers")]
        [SerializeField] private AudioMixer masterMixer;
        
        [Header("Default Clips")]
        [SerializeField] private AudioClip defaultMusic;
        [SerializeField] private AudioClip defaultAmbience;
        [SerializeField] private AudioClip buttonClickSFX;
        
        [Header("Audio Settings")]
        [SerializeField] private float defaultMusicVolume = 0.5f;
        [SerializeField] private float defaultSFXVolume = 0.75f;
        [SerializeField] private float defaultAmbienceVolume = 0.3f;
        [SerializeField] private float defaultVoiceVolume = 1.0f;
        [SerializeField] private float fadeSpeed = 1.0f;
        
        // Dictionary to store preloaded audio clips
        private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        
        // Variables for controlling fades
        private Coroutine musicFadeCoroutine;
        private bool isMusicMuted = false;
        
        // Singleton pattern
        public static AudioManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create audio sources if not assigned
            if (musicSource == null)
            {
                musicSource = CreateAudioSource("MusicSource", 0.5f, true, true);
            }
            
            if (sfxSource == null)
            {
                sfxSource = CreateAudioSource("SFXSource", 0.75f, false, false);
            }
            
            if (ambienceSource == null)
            {
                ambienceSource = CreateAudioSource("AmbienceSource", 0.3f, true, true);
            }
            
            if (uiSource == null)
            {
                uiSource = CreateAudioSource("UISource", 0.75f, false, false);
            }
            
            if (voiceSource == null)
            {
                voiceSource = CreateAudioSource("VoiceSource", 1.0f, false, false);
            }
            
            // Load saved volume settings
            LoadVolumeSettings();
        }
        
        private void Start()
        {
            // Play default audio if set
            if (defaultMusic != null && !musicSource.isPlaying)
            {
                PlayMusic(defaultMusic);
            }
            
            if (defaultAmbience != null && !ambienceSource.isPlaying)
            {
                PlayAmbience(defaultAmbience);
            }
        }
        
        #region Audio Source Creation
        /// <summary>
        /// Crea un nuovo AudioSource con le impostazioni specificate
        /// </summary>
        private AudioSource CreateAudioSource(string name, float volume, bool loop, bool playOnAwake)
        {
            GameObject audioSourceObj = new GameObject(name);
            audioSourceObj.transform.SetParent(transform);
            
            AudioSource source = audioSourceObj.AddComponent<AudioSource>();
            source.volume = volume;
            source.loop = loop;
            source.playOnAwake = playOnAwake;
            
            return source;
        }
        #endregion
        
        #region Music Methods
        /// <summary>
        /// Riproduce una traccia musicale
        /// </summary>
        public void PlayMusic(AudioClip musicClip, bool fade = true, float delay = 0f)
        {
            if (musicClip == null)
                return;
                
            if (fade)
            {
                StartMusicFade(musicClip, delay);
            }
            else
            {
                if (delay > 0)
                {
                    StartCoroutine(PlayMusicDelayed(musicClip, delay));
                }
                else
                {
                    musicSource.clip = musicClip;
                    musicSource.Play();
                }
            }
        }
        
        /// <summary>
        /// Riproduce una traccia musicale dal nome
        /// </summary>
        public void PlayMusic(string musicName, bool fade = true, float delay = 0f)
        {
            AudioClip clip = GetAudioClip(musicName, AudioType.Music);
            if (clip != null)
            {
                PlayMusic(clip, fade, delay);
            }
        }
        
        /// <summary>
        /// Avvia un fade tra tracce musicali
        /// </summary>
        private void StartMusicFade(AudioClip newClip, float delay)
        {
            // Stop any existing fade
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            // Start new fade
            musicFadeCoroutine = StartCoroutine(FadeMusicTrack(newClip, delay));
        }
        
        /// <summary>
        /// Esegue un fade tra tracce musicali
        /// </summary>
        private IEnumerator FadeMusicTrack(AudioClip newClip, float delay)
        {
            // Wait for delay if specified
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            
            // Store original volume
            float originalVolume = musicSource.volume;
            
            // Fade out current music
            float fadeTime = fadeSpeed;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeTime && musicSource.volume > 0)
            {
                musicSource.volume = Mathf.Lerp(originalVolume, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            musicSource.volume = 0;
            
            // Change clip
            musicSource.clip = newClip;
            musicSource.Play();
            
            // Fade in new music
            elapsedTime = 0;
            
            while (elapsedTime < fadeTime)
            {
                musicSource.volume = Mathf.Lerp(0, originalVolume, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            musicSource.volume = originalVolume;
            musicFadeCoroutine = null;
        }
        
        /// <summary>
        /// Riproduce una traccia musicale dopo un ritardo
        /// </summary>
        private IEnumerator PlayMusicDelayed(AudioClip musicClip, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            musicSource.clip = musicClip;
            musicSource.Play();
        }
        
        /// <summary>
        /// Mette in pausa la musica
        /// </summary>
        public void PauseMusic()
        {
            if (musicSource.isPlaying)
            {
                musicSource.Pause();
            }
        }
        
        /// <summary>
        /// Riprende la riproduzione della musica
        /// </summary>
        public void ResumeMusic()
        {
            if (!musicSource.isPlaying)
            {
                musicSource.UnPause();
            }
        }
        
        /// <summary>
        /// Ferma la musica
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
            }
        }
        
        /// <summary>
        /// Esegue un fade out della musica
        /// </summary>
        private IEnumerator FadeOutMusic()
        {
            float originalVolume = musicSource.volume;
            float fadeTime = fadeSpeed;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeTime && musicSource.volume > 0)
            {
                musicSource.volume = Mathf.Lerp(originalVolume, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            musicSource.volume = 0;
            musicSource.Stop();
            musicSource.volume = originalVolume;
        }
        #endregion
        
        #region SFX Methods
        /// <summary>
        /// Riproduce un effetto sonoro
        /// </summary>
        public void PlaySFX(AudioClip sfxClip, float volumeScale = 1.0f)
        {
            if (sfxClip == null)
                return;
                
            sfxSource.PlayOneShot(sfxClip, volumeScale);
        }
        
        /// <summary>
        /// Riproduce un effetto sonoro dal nome
        /// </summary>
        public void PlaySFX(string sfxName, float volumeScale = 1.0f)
        {
            AudioClip clip = GetAudioClip(sfxName, AudioType.SFX);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale);
            }
        }
        
        /// <summary>
        /// Riproduce un suono di UI
        /// </summary>
        public void PlayUISound(AudioClip uiClip, float volumeScale = 1.0f)
        {
            if (uiClip == null)
                return;
                
            uiSource.PlayOneShot(uiClip, volumeScale);
        }
        
        /// <summary>
        /// Riproduce il suono predefinito per i pulsanti
        /// </summary>
        public void PlayButtonClick()
        {
            if (buttonClickSFX != null)
            {
                PlayUISound(buttonClickSFX);
            }
        }
        #endregion
        
        #region Ambience Methods
        /// <summary>
        /// Riproduce un suono ambientale
        /// </summary>
        public void PlayAmbience(AudioClip ambienceClip, bool fade = true)
        {
            if (ambienceClip == null)
                return;
                
            if (fade)
            {
                StartCoroutine(FadeAmbienceTrack(ambienceClip));
            }
            else
            {
                ambienceSource.clip = ambienceClip;
                ambienceSource.Play();
            }
        }
        
        /// <summary>
        /// Riproduce un suono ambientale dal nome
        /// </summary>
        public void PlayAmbience(string ambienceName, bool fade = true)
        {
            AudioClip clip = GetAudioClip(ambienceName, AudioType.Ambience);
            if (clip != null)
            {
                PlayAmbience(clip, fade);
            }
        }
        
        /// <summary>
        /// Esegue un fade tra tracce ambientali
        /// </summary>
        private IEnumerator FadeAmbienceTrack(AudioClip newClip)
        {
            // Store original volume
            float originalVolume = ambienceSource.volume;
            
            // Fade out current ambience
            float fadeTime = fadeSpeed * 2; // Slower fade for ambience
            float elapsedTime = 0;
            
            while (elapsedTime < fadeTime && ambienceSource.volume > 0)
            {
                ambienceSource.volume = Mathf.Lerp(originalVolume, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            ambienceSource.volume = 0;
            
            // Change clip
            ambienceSource.clip = newClip;
            ambienceSource.Play();
            
            // Fade in new ambience
            elapsedTime = 0;
            
            while (elapsedTime < fadeTime)
            {
                ambienceSource.volume = Mathf.Lerp(0, originalVolume, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            ambienceSource.volume = originalVolume;
        }
        
        /// <summary>
        /// Ferma il suono ambientale
        /// </summary>
        public void StopAmbience(bool fade = true)
        {
            if (fade)
            {
                StartCoroutine(FadeOutAmbience());
            }
            else
            {
                ambienceSource.Stop();
            }
        }
        
        /// <summary>
        /// Esegue un fade out del suono ambientale
        /// </summary>
        private IEnumerator FadeOutAmbience()
        {
            float originalVolume = ambienceSource.volume;
            float fadeTime = fadeSpeed * 2;
            float elapsedTime = 0;
            
            while (elapsedTime < fadeTime && ambienceSource.volume > 0)
            {
                ambienceSource.volume = Mathf.Lerp(originalVolume, 0, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            ambienceSource.volume = 0;
            ambienceSource.Stop();
            ambienceSource.volume = originalVolume;
        }
        #endregion
        
        #region Voice Methods
        /// <summary>
        /// Riproduce un dialogo vocale
        /// </summary>
        public void PlayVoice(AudioClip voiceClip, float volumeScale = 1.0f)
        {
            if (voiceClip == null)
                return;
                
            voiceSource.clip = voiceClip;
            voiceSource.volume = defaultVoiceVolume * volumeScale;
            voiceSource.Play();
        }
        
        /// <summary>
        /// Riproduce un dialogo vocale dal nome
        /// </summary>
        public void PlayVoice(string voiceName, float volumeScale = 1.0f)
        {
            AudioClip clip = GetAudioClip(voiceName, AudioType.Voice);
            if (clip != null)
            {
                PlayVoice(clip, volumeScale);
            }
        }
        
        /// <summary>
        /// Ferma la riproduzione vocale
        /// </summary>
        public void StopVoice()
        {
            voiceSource.Stop();
        }
        #endregion
        
        #region Volume Control
        /// <summary>
        /// Imposta il volume della musica
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            musicSource.volume = volume;
            PlayerPrefs.SetFloat("MusicVolume", volume);
            PlayerPrefs.Save();
            
            if (masterMixer != null)
            {
                masterMixer.SetFloat("MusicVolume", ConvertToDecibel(volume));
            }
        }
        
        /// <summary>
        /// Imposta il volume degli effetti sonori
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            sfxSource.volume = volume;
            uiSource.volume = volume;
            PlayerPrefs.SetFloat("SFXVolume", volume);
            PlayerPrefs.Save();
            
            if (masterMixer != null)
            {
                masterMixer.SetFloat("SFXVolume", ConvertToDecibel(volume));
            }
        }
        
        /// <summary>
        /// Imposta il volume dell'ambience
        /// </summary>
        public void SetAmbienceVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            ambienceSource.volume = volume;
            PlayerPrefs.SetFloat("AmbienceVolume", volume);
            PlayerPrefs.Save();
            
            if (masterMixer != null)
            {
                masterMixer.SetFloat("AmbienceVolume", ConvertToDecibel(volume));
            }
        }
        
        /// <summary>
        /// Imposta il volume dei dialoghi
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            voiceSource.volume = volume;
            PlayerPrefs.SetFloat("VoiceVolume", volume);
            PlayerPrefs.Save();
            
            if (masterMixer != null)
            {
                masterMixer.SetFloat("VoiceVolume", ConvertToDecibel(volume));
            }
        }
        
        /// <summary>
        /// Imposta il volume master
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.Save();
            
            if (masterMixer != null)
            {
                masterMixer.SetFloat("MasterVolume", ConvertToDecibel(volume));
            }
        }
        
        /// <summary>
        /// Muta/Smuta la musica
        /// </summary>
        public void ToggleMuteMusic()
        {
            isMusicMuted = !isMusicMuted;
            
            if (isMusicMuted)
            {
                if (masterMixer != null)
                {
                    masterMixer.SetFloat("MusicVolume", -80f); // -80dB = silence
                }
                else
                {
                    musicSource.volume = 0f;
                }
            }
            else
            {
                float volume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
                if (masterMixer != null)
                {
                    masterMixer.SetFloat("MusicVolume", ConvertToDecibel(volume));
                }
                else
                {
                    musicSource.volume = volume;
                }
            }
            
            PlayerPrefs.SetInt("MusicMuted", isMusicMuted ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Carica le impostazioni di volume salvate
        /// </summary>
        private void LoadVolumeSettings()
        {
            float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
            float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);
            float ambienceVolume = PlayerPrefs.GetFloat("AmbienceVolume", defaultAmbienceVolume);
            float voiceVolume = PlayerPrefs.GetFloat("VoiceVolume", defaultVoiceVolume);
            float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            
            // Apply volume settings
            musicSource.volume = musicVolume;
            sfxSource.volume = sfxVolume;
            ambienceSource.volume = ambienceVolume;
            voiceSource.volume = voiceVolume;
            
            // Apply to mixer if available
            if (masterMixer != null)
            {
                masterMixer.SetFloat("MusicVolume", ConvertToDecibel(musicVolume));
                masterMixer.SetFloat("SFXVolume", ConvertToDecibel(sfxVolume));
                masterMixer.SetFloat("AmbienceVolume", ConvertToDecibel(ambienceVolume));
                masterMixer.SetFloat("VoiceVolume", ConvertToDecibel(voiceVolume));
                masterMixer.SetFloat("MasterVolume", ConvertToDecibel(masterVolume));
            }
            
            // Check if music is muted
            isMusicMuted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;
            if (isMusicMuted)
            {
                if (masterMixer != null)
                {
                    masterMixer.SetFloat("MusicVolume", -80f);
                }
                else
                {
                    musicSource.volume = 0f;
                }
            }
        }
        
        /// <summary>
        /// Converte un valore lineare (0-1) in decibel per i mixer
        /// </summary>
        private float ConvertToDecibel(float linearVolume)
        {
            // Avoid log(0)
            if (linearVolume <= 0)
                return -80f; // Silent
                
            // Convert to dB scale (log10)
            return Mathf.Log10(linearVolume) * 20;
        }
        #endregion
        
        #region Audio Clip Loading
        /// <summary>
        /// Ottiene un audio clip dal nome e dal tipo
        /// </summary>
        private AudioClip GetAudioClip(string clipName, AudioType type)
        {
            string fullName = $"{type.ToString().ToLower()}_{clipName}";
            
            // Check if already loaded
            if (audioClips.ContainsKey(fullName))
            {
                return audioClips[fullName];
            }
            
            // Try to load from resources
            string path = GetResourcePath(clipName, type);
            AudioClip clip = Resources.Load<AudioClip>(path);
            
            if (clip != null)
            {
                audioClips[fullName] = clip;
                return clip;
            }
            
            Debug.LogWarning($"Audio clip not found: {clipName} of type {type}");
            return null;
        }
        
        /// <summary>
        /// Ottiene il percorso della risorsa audio
        /// </summary>
        private string GetResourcePath(string clipName, AudioType type)
        {
            switch (type)
            {
                case AudioType.Music:
                    return $"Audio/Music/{clipName}";
                case AudioType.SFX:
                    return $"Audio/SFX/{clipName}";
                case AudioType.Ambience:
                    return $"Audio/Ambience/{clipName}";
                case AudioType.Voice:
                    return $"Audio/Voice/{clipName}";
                case AudioType.UI:
                    return $"Audio/UI/{clipName}";
                default:
                    return $"Audio/{clipName}";
            }
        }
        
        /// <summary>
        /// Precarica clip audio per uso futuro
        /// </summary>
        public void PreloadAudioClips(string[] clipNames, AudioType type)
        {
            foreach (string clipName in clipNames)
            {
                GetAudioClip(clipName, type);
            }
        }
        #endregion
        
        /// <summary>
        /// Tipi di audio supportati
        /// </summary>
        public enum AudioType
        {
            Music,
            SFX,
            Ambience,
            Voice,
            UI
        }
    }
}