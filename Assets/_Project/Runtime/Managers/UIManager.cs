// Path: Assets/_Project/Runtime/Managers/UIManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RunawayHeroes.Runtime.Managers
{
    /// <summary>
    /// Gestore centralizzato del sistema UI del gioco.
    /// Controlla pannelli, schermate, transizioni e stati dell'interfaccia.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton pattern
        public static UIManager Instance { get; private set; }
        
        [System.Serializable]
        public class UIPanel
        {
            public string name;
            public GameObject panel;
            public Animator animator;
            [Tooltip("Se true, il pannello sarà attivo all'avvio")]
            public bool activeAtStart = false;
        }
        
        [Header("UI Panels")]
        [SerializeField] private UIPanel[] panels;
        
        [Header("Transizioni")]
        [SerializeField] private Animator transitionAnimator;
        [SerializeField] private float transitionTime = 0.5f;
        
        [Header("Riferimenti globali")]
        [SerializeField] private Button globalBackButton;
        [SerializeField] private GameObject loadingIndicator;
        
        private Dictionary<string, UIPanel> _panelsDictionary = new Dictionary<string, UIPanel>();
        private Stack<string> _panelHistory = new Stack<string>();
        private string _currentPanel;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Inizializza i pannelli
                InitializePanels();
                
                // Configura il pulsante Back globale
                if (globalBackButton != null)
                {
                    globalBackButton.onClick.AddListener(BackToPreviousPanel);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializePanels()
        {
            _panelsDictionary.Clear();
            
            foreach (UIPanel panel in panels)
            {
                if (panel.panel != null)
                {
                    // Aggiungi al dizionario per accesso rapido
                    _panelsDictionary[panel.name] = panel;
                    
                    // Imposta lo stato iniziale
                    panel.panel.SetActive(panel.activeAtStart);
                    
                    // Se è attivo all'avvio, impostalo come pannello corrente
                    if (panel.activeAtStart)
                    {
                        _currentPanel = panel.name;
                    }
                }
            }
        }
        
        /// <summary>
        /// Apre un pannello UI specifico.
        /// </summary>
        /// <param name="panelName">Nome del pannello da aprire</param>
        /// <param name="addToHistory">Se true, il pannello corrente viene aggiunto alla cronologia</param>
        public void OpenPanel(string panelName, bool addToHistory = true)
        {
            if (_panelsDictionary.TryGetValue(panelName, out UIPanel targetPanel))
            {
                // Aggiungi il pannello corrente alla cronologia se richiesto
                if (!string.IsNullOrEmpty(_currentPanel) && addToHistory)
                {
                    _panelHistory.Push(_currentPanel);
                }
                
                // Chiudi il pannello corrente
                if (!string.IsNullOrEmpty(_currentPanel) && _panelsDictionary.TryGetValue(_currentPanel, out UIPanel currentPanel))
                {
                    ClosePanel(currentPanel);
                }
                
                // Apri il nuovo pannello
                ShowPanel(targetPanel);
                
                // Aggiorna il pannello corrente
                _currentPanel = panelName;
            }
            else
            {
                Debug.LogWarning($"Panel: {panelName} not found in UIManager");
            }
        }
        
        /// <summary>
        /// Torna al pannello precedente nella cronologia.
        /// </summary>
        public void BackToPreviousPanel()
        {
            if (_panelHistory.Count > 0)
            {
                string previousPanel = _panelHistory.Pop();
                OpenPanel(previousPanel, false); // Non aggiungere alla cronologia
            }
            else
            {
                // Se non ci sono pannelli nella cronologia, comportamento predefinito
                // Ad esempio, tornare al menu principale o uscire dall'app
                if (_currentPanel != "MainMenu")
                {
                    OpenPanel("MainMenu", false);
                }
            }
        }
        
        /// <summary>
        /// Chiude tutti i pannelli e apre il pannello specificato.
        /// </summary>
        /// <param name="panelName">Nome del pannello da aprire</param>
        public void SwitchToPanel(string panelName)
        {
            // Chiudi tutti i pannelli
            foreach (var panel in _panelsDictionary.Values)
            {
                ClosePanel(panel, false);
            }
            
            // Resetta la cronologia
            _panelHistory.Clear();
            
            // Apri il nuovo pannello
            if (_panelsDictionary.TryGetValue(panelName, out UIPanel targetPanel))
            {
                ShowPanel(targetPanel);
                _currentPanel = panelName;
            }
        }
        
        /// <summary>
        /// Ritorna al menu principale resettando la cronologia.
        /// </summary>
        public void ReturnToMainMenu()
        {
            SwitchToPanel("MainMenu");
        }
        
        /// <summary>
        /// Mostra l'indicatore di caricamento.
        /// </summary>
        /// <param name="show">True per mostrare, False per nascondere</param>
        public void ShowLoadingIndicator(bool show)
        {
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(show);
            }
        }
        
        /// <summary>
        /// Aggiorna un testo UI specifico.
        /// </summary>
        /// <param name="panelName">Nome del pannello contenente il testo</param>
        /// <param name="textName">Nome del componente di testo</param>
        /// <param name="value">Nuovo valore del testo</param>
        public void UpdateText(string panelName, string textName, string value)
        {
            if (_panelsDictionary.TryGetValue(panelName, out UIPanel panel))
            {
                TextMeshProUGUI text = panel.panel.transform.Find(textName)?.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = value;
                }
            }
        }
        
        /// <summary>
        /// Aggiorna un'immagine UI specifica.
        /// </summary>
        /// <param name="panelName">Nome del pannello contenente l'immagine</param>
        /// <param name="imageName">Nome del componente di immagine</param>
        /// <param name="sprite">Nuovo sprite dell'immagine</param>
        public void UpdateImage(string panelName, string imageName, Sprite sprite)
        {
            if (_panelsDictionary.TryGetValue(panelName, out UIPanel panel))
            {
                Image image = panel.panel.transform.Find(imageName)?.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                }
            }
        }
        
        /// <summary>
        /// Verifica se un pannello specifico è attualmente attivo.
        /// </summary>
        /// <param name="panelName">Nome del pannello da verificare</param>
        /// <returns>True se il pannello è attivo, false altrimenti</returns>
        public bool IsPanelActive(string panelName)
        {
            if (_panelsDictionary.TryGetValue(panelName, out UIPanel panel))
            {
                return panel.panel.activeSelf;
            }
            return false;
        }
        
        /// <summary>
        /// Ottiene il nome del pannello attualmente attivo.
        /// </summary>
        /// <returns>Nome del pannello attivo</returns>
        public string GetCurrentPanelName()
        {
            return _currentPanel;
        }
        
        // Metodi privati di supporto
        
        // Apre un pannello con animazione se disponibile
        private void ShowPanel(UIPanel panel)
        {
            panel.panel.SetActive(true);
            
            if (panel.animator != null)
            {
                panel.animator.SetBool("Open", true);
            }
        }
        
        // Chiude un pannello con animazione se disponibile
        private void ClosePanel(UIPanel panel, bool animate = true)
        {
            if (panel.animator != null && animate)
            {
                panel.animator.SetBool("Open", false);
                
                // Disattiva il pannello dopo l'animazione
                StartCoroutine(DisablePanelDelayed(panel.panel, panel.animator.GetCurrentAnimatorStateInfo(0).length));
            }
            else
            {
                panel.panel.SetActive(false);
            }
        }
        
        // Coroutine per disattivare un pannello dopo un ritardo
        private System.Collections.IEnumerator DisablePanelDelayed(GameObject panel, float delay)
        {
            yield return new WaitForSeconds(delay);
            panel.SetActive(false);
        }
        
        // Avvia una transizione tra scene o stati
        private void StartTransition(System.Action onComplete = null)
        {
            if (transitionAnimator != null)
            {
                transitionAnimator.SetTrigger("Start");
                
                if (onComplete != null)
                {
                    StartCoroutine(InvokeAfterDelay(onComplete, transitionTime));
                }
            }
            else if (onComplete != null)
            {
                onComplete.Invoke();
            }
        }
        
        // Coroutine per invocare un'azione dopo un ritardo
        private System.Collections.IEnumerator InvokeAfterDelay(System.Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }
    }
}