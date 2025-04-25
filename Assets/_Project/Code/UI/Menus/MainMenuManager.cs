using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

namespace RunawayHeroes.Core.UI
{
    /// <summary>
    /// Gestisce il menu principale del gioco, inclusi i pulsanti per iniziare, continuare, 
    /// accedere alle impostazioni e uscire dal gioco.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject loadingPanel;

        [Header("Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button backFromOptionsButton;
        [SerializeField] private Button backFromCreditsButton;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private CanvasGroup fadePanel;

        [Header("Game Progression")]
        [SerializeField] private string tutorialFirstLevel = "Tutorial/Level1_FirstSteps";
        [SerializeField] private string firstWorldLevel = "World1/Level1_ParkoCentrale";
        [SerializeField] private float minLoadingTime = 1.5f;
        [SerializeField] private string[] tutorialLevelNames = new string[5]
        {
            "Level1_FirstSteps",
            "Level2_PerfectSlide",
            "Level3_ReadyReflexes",
            "Level4_ItemPower",
            "Level5_EscapeTrainer"
        };

        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private float musicVolume = 0.5f;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float buttonAnimationDuration = 0.3f;

        // Private variables
        private AudioSource audioSource;
        private bool isTutorialCompleted;
        private bool isGameStarted;
        private string lastPlayedLevel;

        private void Awake()
        {
            // Get components
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Setup audio
            audioSource.loop = true;
            audioSource.volume = musicVolume;
            if (backgroundMusic != null)
            {
                audioSource.clip = backgroundMusic;
                audioSource.Play();
            }

            // Initialize UI
            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }

            // Check saved data
            isTutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            isGameStarted = PlayerPrefs.GetInt("GameStarted", 0) == 1;
            lastPlayedLevel = PlayerPrefs.GetString("LastPlayedLevel", "");

            // Setup buttons
            SetupButtons();

            // Initial UI state
            if (mainPanel) mainPanel.SetActive(true);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (creditsPanel) creditsPanel.SetActive(false);
            if (loadingPanel) loadingPanel.SetActive(false);

            // Fade in
            FadeIn();
        }

        private void SetupButtons()
        {
            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
                AnimateButton(newGameButton.transform, 0.2f);
            }

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButton.interactable = isGameStarted;
                AnimateButton(continueButton.transform, 0.3f);
            }

            if (optionsButton != null)
            {
                optionsButton.onClick.AddListener(OnOptionsClicked);
                AnimateButton(optionsButton.transform, 0.4f);
            }

            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsClicked);
                AnimateButton(creditsButton.transform, 0.5f);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
                AnimateButton(quitButton.transform, 0.6f);
            }

            if (backFromOptionsButton != null)
            {
                backFromOptionsButton.onClick.AddListener(OnBackFromOptionsClicked);
            }

            if (backFromCreditsButton != null)
            {
                backFromCreditsButton.onClick.AddListener(OnBackFromCreditsClicked);
            }
        }

        private void AnimateButton(Transform buttonTransform, float delay)
        {
            buttonTransform.localScale = Vector3.zero;
            buttonTransform.DOScale(1f, buttonAnimationDuration)
                .SetEase(Ease.OutBack)
                .SetDelay(delay);
        }

        #region Button Click Handlers
        private void OnNewGameClicked()
        {
            PlayButtonSound();

            // Show confirmation if game already started
            if (isGameStarted)
            {
                ShowNewGameConfirmation();
                return;
            }

            StartNewGame();
        }

        private void OnContinueClicked()
        {
            PlayButtonSound();
            ContinueGame();
        }

        private void OnOptionsClicked()
        {
            PlayButtonSound();
            ShowOptionsPanel();
        }

        private void OnCreditsClicked()
        {
            PlayButtonSound();
            ShowCreditsPanel();
        }

        private void OnQuitClicked()
        {
            PlayButtonSound();
            QuitGame();
        }

        private void OnBackFromOptionsClicked()
        {
            PlayButtonSound();
            ShowMainPanel();
        }

        private void OnBackFromCreditsClicked()
        {
            PlayButtonSound();
            ShowMainPanel();
        }
        #endregion

        #region Game Flow
        private void StartNewGame()
        {
            // Mark game as started
            PlayerPrefs.SetInt("GameStarted", 1);
            PlayerPrefs.Save();

            // Reset progress (optional, based on game design)
            if (isGameStarted)
            {
                ResetGameProgress();
            }

            // Determine starting level
            string startLevel = isTutorialCompleted ? firstWorldLevel : tutorialFirstLevel;

            // Start loading
            StartCoroutine(LoadLevelWithProgress(startLevel));
        }

        private void ContinueGame()
        {
            if (!isGameStarted)
                return;

            string levelToLoad = string.IsNullOrEmpty(lastPlayedLevel) ? 
                (isTutorialCompleted ? firstWorldLevel : tutorialFirstLevel) : 
                lastPlayedLevel;

            StartCoroutine(LoadLevelWithProgress(levelToLoad));
        }

        private IEnumerator LoadLevelWithProgress(string levelName)
        {
            // Show loading panel
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (creditsPanel) creditsPanel.SetActive(false);
            if (loadingPanel) loadingPanel.SetActive(true);

            // Reset loading bar
            if (loadingBar) loadingBar.value = 0f;
            if (loadingText) loadingText.text = "Loading...";

            // Fade out music
            DOTween.To(() => audioSource.volume, x => audioSource.volume = x, 0f, 1f);

            // Start async load
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelName);
            asyncLoad.allowSceneActivation = false;

            float startTime = Time.time;
            float progress = 0f;

            // Update loading bar while loading
            while (!asyncLoad.isDone)
            {
                // Calculate progress (combine actual loading with minimum time)
                float timeProgress = (Time.time - startTime) / minLoadingTime;
                float actualProgress = asyncLoad.progress / 0.9f; // 0.9 is the progress value when loading is "done"
                progress = Mathf.Max(timeProgress, actualProgress);

                // Update UI
                if (loadingBar) loadingBar.value = progress;
                if (loadingText) loadingText.text = $"Loading... {Mathf.Round(progress * 100)}%";

                // If finished loading and minimum time has passed
                if (asyncLoad.progress >= 0.9f && timeProgress >= 1.0f)
                {
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void QuitGame()
        {
            StartCoroutine(QuitGameWithFade());
        }

        private IEnumerator QuitGameWithFade()
        {
            FadeOut();
            yield return new WaitForSeconds(fadeOutDuration);

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        #endregion

        #region UI Management
        private void ShowMainPanel()
        {
            if (mainPanel) mainPanel.SetActive(true);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (creditsPanel) creditsPanel.SetActive(false);
        }

        private void ShowOptionsPanel()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(true);
            if (creditsPanel) creditsPanel.SetActive(false);
        }

        private void ShowCreditsPanel()
        {
            if (mainPanel) mainPanel.SetActive(false);
            if (optionsPanel) optionsPanel.SetActive(false);
            if (creditsPanel) creditsPanel.SetActive(true);
        }

        private void ShowNewGameConfirmation()
        {
            // In a real implementation, this would show a confirmation dialog
            // For simplicity, we'll just use a Debug.Log
            Debug.Log("This would show a confirmation dialog. Starting new game...");
            StartNewGame();
        }

        private void FadeIn()
        {
            if (fadePanel == null)
                return;

            fadePanel.alpha = 1f;
            fadePanel.DOFade(0f, fadeInDuration).OnComplete(() => 
            {
                fadePanel.gameObject.SetActive(false);
            });
        }

        private void FadeOut()
        {
            if (fadePanel == null)
                return;

            fadePanel.gameObject.SetActive(true);
            fadePanel.alpha = 0f;
            fadePanel.DOFade(1f, fadeOutDuration);
        }
        #endregion

        #region Utility Methods
        private void PlayButtonSound()
        {
            if (audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound, 0.7f);
            }
        }

        private void ResetGameProgress()
        {
            // Reset tutorial completion
            PlayerPrefs.DeleteKey("TutorialCompleted");

            // Reset tutorial level progress
            for (int i = 0; i < tutorialLevelNames.Length; i++)
            {
                string key = $"TutorialLevel_{i}_Completed";
                PlayerPrefs.DeleteKey(key);
            }

            // Reset world progress
            // This would be expanded based on your game's progression system
            PlayerPrefs.DeleteKey("LastPlayedLevel");

            PlayerPrefs.Save();
            isTutorialCompleted = false;
        }

        // Public methods for Option panel to use
        public void SetMusicVolume(float volume)
        {
            musicVolume = volume;
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
            PlayerPrefs.SetFloat("MusicVolume", volume);
            PlayerPrefs.Save();
        }

        public void SetSoundEffectsVolume(float volume)
        {
            // This would adjust global sound effects volume
            PlayerPrefs.SetFloat("SFXVolume", volume);
            PlayerPrefs.Save();
        }

        public void ToggleFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
        #endregion
    }
}