// Path: Assets/_Project/Runtime/Managers/MainMenuManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RunawayHeroes.Runtime.UI;

namespace RunawayHeroes.Runtime.Managers
{
    /// <summary>
    /// Manager specifico per il menu principale che estende la funzionalità
    /// del UIManager per gestire la navigazione tra livelli e altre interazioni
    /// specifiche del menu principale.
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Menu References")]
        [SerializeField] private string mainMenuPanelName = "MainMenu";
        [SerializeField] private string arcadeMenuPanelName = "ArcadeMenu";
        [SerializeField] private string shopMenuPanelName = "ShopMenu";
        [SerializeField] private string settingsPanelName = "Settings";
        
        [Header("Button References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button arcadeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        
        [Header("Transition Settings")]
        [SerializeField] private UITransitionManager transitionManager;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private float transitionTime = 1.0f;

        // Riferimento al UIManager
        private UIManager _uiManager;
        
        // Flag per indicare se esiste un salvataggio
        private bool _hasSaveGame = false;
        
        private void Start()
        {
            // Ottiene il riferimento al UIManager
            _uiManager = UIManager.Instance;
            if (_uiManager == null)
            {
                Debug.LogError("UIManager not found! Make sure it's initialized before MainMenuManager.");
                return;
            }
            
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // Controlla se esiste un salvataggio
            _hasSaveGame = PlayerPrefs.HasKey("SaveData");
            
            // Configura i listener dei bottoni
            if (playButton != null) playButton.onClick.AddListener(OnPlayButtonClicked);
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueButtonClicked);
                continueButton.gameObject.SetActive(_hasSaveGame);
            }
            if (arcadeButton != null) arcadeButton.onClick.AddListener(OnArcadeButtonClicked);
            if (shopButton != null) shopButton.onClick.AddListener(OnShopButtonClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            
            // Assicura che il menu principale sia attivo all'inizio
            _uiManager.OpenPanel(mainMenuPanelName);
        }
        
        /// <summary>
        /// Carica una scena di gioco con effetto di transizione
        /// </summary>
        public void LoadGameScene(string sceneName)
        {
            // Mostra l'indicatore di caricamento
            _uiManager.ShowLoadingIndicator(true);
            
            // Avvia il caricamento della scena
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        private System.Collections.IEnumerator LoadSceneAsync(string sceneName)
        {
            // Carica la scena in modo asincrono
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;
            
            // Attendi che il caricamento raggiunga il 90%
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }
            
            // Completa il caricamento
            asyncLoad.allowSceneActivation = true;
            
            // Nascondi l'indicatore di caricamento quando la scena è pronta
            _uiManager.ShowLoadingIndicator(false);
        }
        
        #region Button Event Handlers
        private void OnPlayButtonClicked()
        {
            // Avvia una nuova partita
            LoadGameScene("Tutorial/Level1_FirstSteps");
        }
        
        private void OnContinueButtonClicked()
        {
            // Carica l'ultimo livello salvato
            string lastLevel = PlayerPrefs.GetString("LastLevel", "Tutorial/Level1_FirstSteps");
            LoadGameScene(lastLevel);
        }
        
        private void OnArcadeButtonClicked()
        {
            // Mostra il menu arcade
            _uiManager.OpenPanel(arcadeMenuPanelName);
        }
        
        private void OnShopButtonClicked()
        {
            // Mostra il negozio
            _uiManager.OpenPanel(shopMenuPanelName);
        }
        
        private void OnSettingsButtonClicked()
        {
            // Mostra le impostazioni
            _uiManager.OpenPanel(settingsPanelName);
        }

        private System.Collections.IEnumerator LoadSceneCoroutine(string sceneName)
        {
            // Mostra il pannello di caricamento
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
            }
            
            // Avvia la transizione
            if (transitionManager != null)
            {
                bool transitionComplete = false;
                transitionManager.FadeIn(() => { transitionComplete = true; });
                while (!transitionComplete)
                    yield return null;
            }
            else
            {
                yield return new WaitForSeconds(transitionTime);
            }
            
            // Carica la scena
            SceneManager.LoadSceneAsync(sceneName);
            
            // Attendi il completamento del caricamento
            yield return new WaitForSeconds(0.5f);
            
            // Nascondi il pannello di caricamento
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
            
            // Esegui la transizione inversa se necessario
            if (transitionManager != null)
            {
                transitionManager.FadeOut();
            }
        }

        #endregion
    }
}