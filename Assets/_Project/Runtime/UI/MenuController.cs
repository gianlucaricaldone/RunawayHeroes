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
        
        [Header("Character Display")]
        [SerializeField] private Transform characterDisplayArea;
        [SerializeField] private GameObject[] characterPrefabs;
        [SerializeField] private float rotationSpeed = 10f;
        
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
            
            // Inizializza display del personaggio
            if (characterPrefabs.Length > 0 && characterDisplayArea != null)
            {
                currentCharacterIndex = 0;
                SpawnCharacterModel(currentCharacterIndex);
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
            // Fai ruotare il modello del personaggio
            if (currentCharacterDisplay != null)
            {
                currentCharacterDisplay.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Cambia il personaggio visualizzato nella schermata principale
        /// </summary>
        public void ChangeCharacter(int direction)
        {
            currentCharacterIndex = (currentCharacterIndex + direction + characterPrefabs.Length) % characterPrefabs.Length;
            SpawnCharacterModel(currentCharacterIndex);
        }
        
        private void SpawnCharacterModel(int index)
        {
            // Rimuovi modello corrente
            if (currentCharacterDisplay != null)
            {
                Destroy(currentCharacterDisplay);
            }
            
            // Crea nuovo modello
            if (characterPrefabs.Length > index && characterPrefabs[index] != null)
            {
                currentCharacterDisplay = Instantiate(characterPrefabs[index], characterDisplayArea);
                currentCharacterDisplay.transform.localPosition = Vector3.zero;
                currentCharacterDisplay.transform.localRotation = Quaternion.identity;
            }
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