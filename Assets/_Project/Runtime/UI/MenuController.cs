// Path: Assets/_Project/Runtime/UI/MenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.Runtime.UI
{
    /// <summary>
    /// Controller per il menu principale, gestisce le interazioni specifiche
    /// e le animazioni del menu.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Menu Elements")]
        [SerializeField] private GameObject menuBackground;
        [SerializeField] private GameObject logoElement;
        [SerializeField] private Button[] menuButtons;

        
        [Header("Version Info")]
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private string versionNumber = "1.0.0";
        
        [Header("Menu Animation")]
        [SerializeField] private Animator menuAnimator;
        
        private GameObject currentCharacterDisplay;
        private int currentCharacterIndex = 0;
        
        private void Start()
        {
            InitializeMenu();
        }
        
        private void InitializeMenu()
        {
            // Anima l'entrata del menu
            if (menuAnimator != null)
            {
                menuAnimator.SetTrigger("OpenMenu");
            }
            
            // Imposta versione
            if (versionText != null)
            {
                versionText.text = "v" + versionNumber;
            }
            
            // Aggiungi listener di esempio per il tasto back
            // Questo è solo un esempio, implementa in base alle tue esigenze
            BackButtonHandler.OnBackButtonPressed += HandleBackButton;
        }
        
        private void Update()
        {

        }
    
        
        /// <summary>
        /// Gestisce il pulsante back
        /// </summary>
        private void HandleBackButton()
        {
            // Controlla il menu attivo corrente e torna indietro se possibile
            UIManager.Instance.BackToPreviousPanel();
        }
        
        /// <summary>
        /// Riproduce un suono di clic per i pulsanti
        /// </summary>
        public void PlayButtonClickSound()
        {
            // Implementazione audio click
            AudioManager.Instance?.PlaySound("ButtonClick");
        }
        
        private void OnDestroy()
        {
            // Rimuovi listener di esempio per il tasto back
            BackButtonHandler.OnBackButtonPressed -= HandleBackButton;
        }
    }
    
    /// <summary>
    /// Utility class per gestire il pulsante back (Android/iOS)
    /// </summary>
    public static class BackButtonHandler
    {
        public delegate void BackButtonEvent();
        public static event BackButtonEvent OnBackButtonPressed;
        
        public static void InvokeBackButton()
        {
            OnBackButtonPressed?.Invoke();
        }
    }
}