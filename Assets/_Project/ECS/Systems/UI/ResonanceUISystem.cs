// Path: Assets/_Project/ECS/Systems/UI/ResonanceUISystem.cs
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using System.Collections;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Sistema che gestisce l'interfaccia utente per la Risonanza dei Frammenti,
    /// permettendo al giocatore di cambiare personaggio durante il gameplay.
    /// </summary>
    public partial class ResonanceUISystem : SystemBase
    {
        // Riferimenti UI
        private GameObject _resonanceButton;       // Pulsante principale in basso
        private GameObject _characterMenu;         // Menu che si espande verso l'alto
        private Button[] _characterButtons;        // Bottoni per ciascun personaggio
        private Image[] _characterButtonImages;    // Immagini dei personaggi
        private Image _resonanceButtonImage;       // Immagine del personaggio attivo
        private Image _cooldownOverlay;            // Overlay per indicare il cooldown
        private GameObject _resonanceWaveEffect;   // Effetto onda al cambio personaggio
        private GameObject _resonanceUnlockEffect; // Effetto per nuovo personaggio sbloccato
        
        // Sprite e testi
        private Sprite[] _characterSprites;
        private Text _resonanceLevelText;
        
        // Animazione
        private Animator _menuAnimator;
        private bool _isMenuOpen = false;
        
        // Query
        private EntityQuery _resonanceQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            // Ottieni riferimento al command buffer system
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Definisci la query per le entità con Risonanza
            _resonanceQuery = GetEntityQuery(
                ComponentType.ReadOnly<FragmentResonanceComponent>(),
                ComponentType.ReadWrite<ResonanceInputComponent>()
            );
            
            RequireForUpdate(_resonanceQuery);
        }
        
        protected override void OnStartRunning()
        {
            // Trova i riferimenti UI necessari
            GameObject uiRoot = GameObject.FindGameObjectWithTag("ResonanceUI");
            if (uiRoot != null)
            {
                // Pulsante principale
                _resonanceButton = uiRoot.transform.Find("ResonanceButton").gameObject;
                _resonanceButtonImage = _resonanceButton.GetComponent<Image>();
                _cooldownOverlay = _resonanceButton.transform.Find("CooldownOverlay").GetComponent<Image>();
                
                // Menu personaggi
                _characterMenu = uiRoot.transform.Find("CharacterMenu").gameObject;
                _menuAnimator = _characterMenu.GetComponent<Animator>();
                
                // Ottieni riferimento all'effetto onda
                _resonanceWaveEffect = uiRoot.transform.Find("ResonanceWaveEffect").gameObject;
                _resonanceWaveEffect.SetActive(false);
                
                // Ottieni riferimento all'effetto di sblocco
                _resonanceUnlockEffect = uiRoot.transform.Find("UnlockEffect").gameObject;
                _resonanceUnlockEffect.SetActive(false);
                
                // Testo livello risonanza
                _resonanceLevelText = uiRoot.transform.Find("ResonanceLevel").GetComponent<Text>();
                
                // Configura i bottoni dei personaggi
                Transform characterButtonsContainer = _characterMenu.transform.Find("Buttons");
                int buttonCount = characterButtonsContainer.childCount;
                _characterButtons = new Button[buttonCount];
                _characterButtonImages = new Image[buttonCount];
                
                for (int i = 0; i < buttonCount; i++)
                {
                    Transform buttonTransform = characterButtonsContainer.GetChild(i);
                    _characterButtons[i] = buttonTransform.GetComponent<Button>();
                    _characterButtonImages[i] = buttonTransform.Find("Icon").GetComponent<Image>();
                    
                    // Aggiungi listener per il click
                    int characterIndex = i;
                    _characterButtons[i].onClick.AddListener(() => OnCharacterSelected(characterIndex));
                }
                
                // Pulsante principale - apre il menu
                Button mainButton = _resonanceButton.GetComponent<Button>();
                mainButton.onClick.AddListener(ToggleCharacterMenu);
                
                // Carica le sprite dei personaggi
                _characterSprites = Resources.LoadAll<Sprite>("UI/CharacterIcons");
                
                // Inizialmente nascondi il menu
                _characterMenu.SetActive(false);
                _isMenuOpen = false;
            }
            else
            {
                Debug.LogError("ResonanceUI non trovato nella scena!");
            }
        }
        
        protected override void OnUpdate()
        {
            // Gestisci lo stato del cooldown e aggiorna l'UI
            Entities
                .WithoutBurst() // Necessario per accedere a oggetti Unity
                .WithAll<FragmentResonanceComponent>()
                .ForEach((in FragmentResonanceComponent resonance, in ResonanceInputComponent input) =>
                {
                    if (_cooldownOverlay != null)
                    {
                        // Aggiorna l'overlay del cooldown
                        float cooldownRatio = resonance.CooldownRemaining / resonance.Cooldown;
                        _cooldownOverlay.fillAmount = cooldownRatio;
                        
                        // Cambia il colore in base alla disponibilità
                        _cooldownOverlay.color = resonance.IsAvailable ? 
                            new Color(0, 0.8f, 1f, 0.3f) : // Blu chiaro semi-trasparente quando pronto
                            new Color(0.5f, 0.5f, 0.5f, 0.7f); // Grigio scuro quando in cooldown
                    }
                    
                    // Aggiorna l'icona del personaggio attivo nel pulsante principale
                    UpdateActiveCharacterIcon(resonance);
                    
                    // Aggiorna le icone dei personaggi nel menu
                    UpdateCharacterMenu(resonance);
                    
                    // Aggiorna il testo del livello di risonanza
                    if (_resonanceLevelText != null)
                    {
                        _resonanceLevelText.text = "Risonanza Lvl " + resonance.ResonanceLevel;
                    }
                }).Run();
        }
        
        /// <summary>
        /// Aggiorna l'icona del personaggio attivo nel pulsante principale
        /// </summary>
        private void UpdateActiveCharacterIcon(FragmentResonanceComponent resonance)
        {
            if (_resonanceButtonImage == null || _characterSprites == null) return;
            
            int activeIndex = GetActiveCharacterIndex(resonance);
            if (activeIndex >= 0 && activeIndex < _characterSprites.Length)
            {
                _resonanceButtonImage.sprite = _characterSprites[activeIndex];
                
                // Applica colore attenuato se la risonanza non è disponibile
                _resonanceButtonImage.color = resonance.IsAvailable ? 
                    Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }
        
        /// <summary>
        /// Aggiorna le icone e lo stato dei personaggi nel menu
        /// </summary>
        private void UpdateCharacterMenu(FragmentResonanceComponent resonance)
        {
            if (_characterButtonImages == null || _characterSprites == null) return;
            
            int activeIndex = GetActiveCharacterIndex(resonance);
            
            for (int i = 0; i < _characterButtonImages.Length; i++)
            {
                // Determina se il personaggio è sbloccato
                bool isUnlocked = i < resonance.CharacterCount;
                
                // Abilita/disabilita il pulsante
                if (_characterButtons[i] != null)
                {
                    _characterButtons[i].interactable = isUnlocked && resonance.IsAvailable && i != activeIndex;
                }
                
                // Imposta l'icona e il colore appropriati
                if (_characterButtonImages[i] != null)
                {
                    if (i < _characterSprites.Length)
                    {
                        _characterButtonImages[i].sprite = _characterSprites[i];
                    }
                    
                    if (isUnlocked)
                    {
                        // Personaggio sbloccato
                        if (i == activeIndex)
                        {
                            // Personaggio attivo
                            _characterButtonImages[i].color = Color.white;
                        }
                        else
                        {
                            // Personaggio sbloccato ma non attivo
                            _characterButtonImages[i].color = resonance.IsAvailable ? 
                                new Color(0.9f, 0.9f, 0.9f, 1f) : 
                                new Color(0.6f, 0.6f, 0.6f, 1f);
                        }
                    }
                    else
                    {
                        // Personaggio bloccato
                        _characterButtonImages[i].color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                    }
                }
            }
        }
        
        /// <summary>
        /// Ottiene l'indice del personaggio attivo
        /// </summary>
        private int GetActiveCharacterIndex(FragmentResonanceComponent resonance)
        {
            for (int i = 0; i < resonance.CharacterCount; i++)
            {
                if (resonance.UnlockedCharacters[i] == resonance.ActiveCharacter)
                {
                    return i;
                }
            }
            return 0; // Default to Alex
        }
        
        /// <summary>
        /// Attiva/disattiva il menu dei personaggi
        /// </summary>
        private void ToggleCharacterMenu()
        {
            _isMenuOpen = !_isMenuOpen;
            
            // Se non è ancora attivo, attivalo prima di animarlo
            if (_isMenuOpen && !_characterMenu.activeSelf)
            {
                _characterMenu.SetActive(true);
            }
            
            // Avvia l'animazione appropriata
            if (_menuAnimator != null)
            {
                _menuAnimator.SetTrigger(_isMenuOpen ? "Open" : "Close");
            }
            
            // Se si sta chiudendo, disattiva dopo l'animazione
            if (!_isMenuOpen)
            {
                StartCoroutine(DisableMenuAfterAnimation());
            }
        }
        
        /// <summary>
        /// Disattiva il menu dopo l'animazione di chiusura
        /// </summary>
        private IEnumerator DisableMenuAfterAnimation()
        {
            // Aspetta che l'animazione termini
            if (_menuAnimator != null)
            {
                yield return new WaitForSeconds(_menuAnimator.GetCurrentAnimatorStateInfo(0).length);
            }
            else
            {
                yield return new WaitForSeconds(0.3f); // Fallback se non c'è l'animator
            }
            
            _characterMenu.SetActive(false);
        }
        
        /// <summary>
        /// Gestisce la selezione di un personaggio
        /// </summary>
        private void OnCharacterSelected(int characterIndex)
        {
            // Chiudi menu
            ToggleCharacterMenu();
            
            // Ottieni l'entità del giocatore
            Entity playerEntity = _resonanceQuery.GetSingletonEntity();
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer();
            
            // Aggiorna il componente di input della Risonanza
            commandBuffer.SetComponent(playerEntity, new ResonanceInputComponent
            {
                SwitchToCharacterIndex = characterIndex,
                NewCharacterUnlocked = false,
                NewCharacterEntity = Entity.Null,
                ResonanceLevelUp = false,
                NewResonanceLevel = 0
            });
            
            // Mostra l'effetto onda
            if (_resonanceWaveEffect != null)
            {
                _resonanceWaveEffect.SetActive(true);
                
                // Avvia l'animazione
                Animator waveAnimator = _resonanceWaveEffect.GetComponent<Animator>();
                if (waveAnimator != null)
                {
                    waveAnimator.SetTrigger("Play");
                }
                
                // Disattiva dopo l'animazione
                StartCoroutine(DisableAfterAnimation(_resonanceWaveEffect, 1.5f));
            }
        }
        
        /// <summary>
        /// Mostra l'effetto per lo sblocco di un nuovo personaggio
        /// </summary>
        public void PlayUnlockEffect(int characterIndex)
        {
            if (_resonanceUnlockEffect != null && characterIndex < _characterSprites.Length)
            {
                // Configura l'effetto con la sprite del personaggio sbloccato
                Image characterImage = _resonanceUnlockEffect.transform.Find("CharacterIcon").GetComponent<Image>();
                if (characterImage != null)
                {
                    characterImage.sprite = _characterSprites[characterIndex];
                }
                
                // Attiva e anima l'effetto
                _resonanceUnlockEffect.SetActive(true);
                Animator unlockAnimator = _resonanceUnlockEffect.GetComponent<Animator>();
                if (unlockAnimator != null)
                {
                    unlockAnimator.SetTrigger("Play");
                }
                
                // Disattiva dopo l'animazione
                StartCoroutine(DisableAfterAnimation(_resonanceUnlockEffect, 3.0f));
            }
        }
        
        /// <summary>
        /// Disattiva un oggetto dopo un ritardo
        /// </summary>
        private IEnumerator DisableAfterAnimation(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
        }
    }
}