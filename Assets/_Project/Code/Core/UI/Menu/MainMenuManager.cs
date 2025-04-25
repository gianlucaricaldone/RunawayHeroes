using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    // Riferimenti ai panel
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject creditsPanel;
    public GameObject loadingPanel;

    // Riferimenti ai pulsanti
    [Header("Buttons")]
    public Button newGameButton;
    public Button continueButton;
    public Button optionsButton;
    public Button creditsButton;
    public Button quitButton;
    public Button optionsBackButton;
    public Button creditsBackButton;

    // Riferimenti alle opzioni
    [Header("Options")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Toggle vibrationToggle; // Cambio da fullscreenToggle a vibrationToggle per mobile

    // Riferimenti al caricamento
    [Header("Loading")]
    public Slider loadingBar;
    public TextMeshProUGUI loadingText;
    public RectTransform loadingIcon;

    // Audio
    [Header("Audio")]
    public AudioClip buttonClickSound;
    private AudioSource audioSource;

    // Costanti
    private const string TUTORIAL_SCENE = "Level1_FirstSteps";
    private const float FADE_DURATION = 0.5f;

    private void Start()
    {
        // Inizializza l'audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Imposta il volume iniziale
        audioSource.volume = 0.5f;

        // Blocca l'orientamento in portrait
        Screen.orientation = ScreenOrientation.Portrait;

        // Nasconde i panel secondari
        optionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
        loadingPanel.SetActive(false);

        // Configura i listener dei pulsanti
        newGameButton.onClick.AddListener(OnNewGameClick);
        continueButton.onClick.AddListener(OnContinueClick);
        optionsButton.onClick.AddListener(OnOptionsClick);
        creditsButton.onClick.AddListener(OnCreditsClick);
        quitButton.onClick.AddListener(OnQuitClick);
        optionsBackButton.onClick.AddListener(OnOptionsBackClick);
        creditsBackButton.onClick.AddListener(OnCreditsBackClick);

        // Verifica se esiste un salvataggio
        bool hasSaveGame = PlayerPrefs.HasKey("LevelProgress");
        continueButton.interactable = hasSaveGame;
        
        // Carica le impostazioni salvate
        LoadSettings();
    }

    private void LoadSettings()
    {
        // Carica impostazioni audio
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        bool vibration = PlayerPrefs.GetInt("Vibration", 1) == 1;
        
        // Applica valori ai controlli UI
        musicVolumeSlider.value = musicVolume;
        sfxVolumeSlider.value = sfxVolume;
        vibrationToggle.isOn = vibration;
        
        // Imposta il callback per salvataggio modifiche
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        // Aggiorna e salva il volume della musica
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
        
        // Nel gioco reale, qui si aggiornerebbe anche il volume effettivo della musica
    }
    
    private void OnSFXVolumeChanged(float value)
    {
        // Aggiorna e salva il volume degli effetti
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
        
        // Applica al volume dell'audio source corrente
        audioSource.volume = value;
    }
    
    private void OnVibrationChanged(bool isOn)
    {
        // Aggiorna e salva l'impostazione vibrazione
        PlayerPrefs.SetInt("Vibration", isOn ? 1 : 0);
        PlayerPrefs.Save();
        
        // Feedback di vibrazione quando attivato
        if (isOn && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound);
            
        // Aggiungi vibrazione se abilitata (per feedback touch)
        if (PlayerPrefs.GetInt("Vibration", 1) == 1 && SystemInfo.supportsVibration)
        {
            Handheld.Vibrate();
        }
    }

    // Gestori eventi pulsanti
    public void OnNewGameClick()
    {
        PlayButtonSound();
        StartCoroutine(LoadLevel(TUTORIAL_SCENE));
    }

    public void OnContinueClick()
    {
        PlayButtonSound();
        // Carica l'ultimo livello salvato
        string lastLevel = PlayerPrefs.GetString("LastLevel", TUTORIAL_SCENE);
        StartCoroutine(LoadLevel(lastLevel));
    }

    public void OnOptionsClick()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void OnCreditsClick()
    {
        PlayButtonSound();
        mainPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void OnQuitClick()
    {
        PlayButtonSound();
        // Attendi la fine del suono prima di uscire
        StartCoroutine(QuitAfterSound());
    }

    public void OnOptionsBackClick()
    {
        PlayButtonSound();
        optionsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnCreditsBackClick()
    {
        PlayButtonSound();
        creditsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    private IEnumerator QuitAfterSound()
    {
        yield return new WaitForSeconds(buttonClickSound.length);
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    private IEnumerator LoadLevel(string sceneName)
    {
        // Mostra il panel di caricamento
        loadingPanel.SetActive(true);
        mainPanel.SetActive(false);
        
        // Animazione icona di caricamento
        float rotationSpeed = 120f; // gradi al secondo
        
        // Simulazione di caricamento progressivo
        float loadingProgress = 0f;
        while (loadingProgress < 1f)
        {
            loadingProgress += Time.deltaTime * 0.5f; // Simula il caricamento
            loadingBar.value = loadingProgress;
            
            // Aggiorna il testo con percentuale
            loadingText.text = $"CARICAMENTO... {Mathf.FloorToInt(loadingProgress * 100)}%";
            
            // Ruota l'icona di caricamento
            if (loadingIcon != null)
            {
                loadingIcon.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            }
            
            yield return null;
        }

        // Carica la scena
        SceneManager.LoadScene(sceneName);
    }
    
}