// Path: Assets/_Project/ECS/Systems/UI/ResonanceMainButton.cs
using UnityEngine;
using UnityEngine.UI;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Classe MonoBehaviour che gestisce il pulsante principale della risonanza.
    /// Utilizzata per evitare il problema delle lambda in struct.
    /// </summary>
    public class ResonanceMainButton : MonoBehaviour
    {
        [Tooltip("Riferimento al menu dei personaggi")]
        public GameObject CharacterMenu;
        
        [Tooltip("Animator del menu")]
        public Animator MenuAnimator;
        
        [Tooltip("Timer per la chiusura del menu")]
        public float MenuCloseTimer;
        
        [Tooltip("Stato del menu")]
        private bool _isMenuOpen;
        
        private void Start()
        {
            _isMenuOpen = false;
            MenuCloseTimer = -1f;
            
            // Ottiene il pulsante
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(ToggleCharacterMenu);
            }
        }
        
        /// <summary>
        /// Update per gestire il timer di chiusura
        /// </summary>
        private void Update()
        {
            // Gestione timer menu chiusura
            if (MenuCloseTimer > 0)
            {
                MenuCloseTimer -= Time.deltaTime;
                if (MenuCloseTimer <= 0)
                {
                    CharacterMenu.SetActive(false);
                    MenuCloseTimer = -1f;
                }
            }
        }
        
        /// <summary>
        /// Attiva/disattiva il menu dei personaggi
        /// </summary>
        public void ToggleCharacterMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            
            // Se non è ancora attivo, attivalo prima di animarlo
            if (_isMenuOpen && !CharacterMenu.activeSelf)
            {
                CharacterMenu.SetActive(true);
            }
            
            // Avvia l'animazione appropriata
            if (MenuAnimator != null)
            {
                MenuAnimator.SetTrigger(_isMenuOpen ? "Open" : "Close");
            }
            
            // Se si sta chiudendo, configura il timer per disattivarlo dopo l'animazione
            if (!_isMenuOpen)
            {
                float animationLength = 0.3f; // Tempo predefinito
                if (MenuAnimator != null && MenuAnimator.runtimeAnimatorController != null)
                {
                    AnimationClip[] clips = MenuAnimator.runtimeAnimatorController.animationClips;
                    foreach (AnimationClip clip in clips)
                    {
                        if (clip.name.Contains("Close"))
                        {
                            animationLength = clip.length;
                            break;
                        }
                    }
                }
                MenuCloseTimer = animationLength;
            }
        }
    }
}