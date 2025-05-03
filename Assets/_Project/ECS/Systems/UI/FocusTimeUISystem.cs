// File: Assets/_Project/ECS/Systems/UI/FocusTimeUISystem.cs

using Unity.Entities;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;
using Unity.Mathematics;
using UnityEngine;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Sistema che gestisce l'interfaccia utente per il Focus Time,
    /// visualizzando la barra di energia, il cooldown e l'interfaccia radiale per gli oggetti.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct FocusTimeUISystem : ISystem
    {
        #region Private Fields
        
        // Riferimenti UI
        private RectTransform _focusTimeEnergyBar;
        private RectTransform _focusTimeCooldownIndicator;
        private GameObject _radialMenuContainer;
        private RectTransform[] _itemSlots;
        private GameObject _timeScaleEffect;
        
        // Dati di animazione
        private float _pulseAnimationTime;
        private const float PULSE_SPEED = 2.0f;
        private const float PULSE_AMPLITUDE = 0.2f;
        
        // Queries
        private EntityQuery _playerFocusTimeQuery;
        
        #endregion
        
        #region Initialization
        
        public void OnCreate(ref SystemState state)
        {
            // Query per il focus time del giocatore
            _playerFocusTimeQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<FocusTimeComponent>()
            );
            
            state.RequireForUpdate(_playerFocusTimeQuery);
        }
        
        public void OnStartRunning(ref SystemState state)
        {
            // Cerca e inizializza i riferimenti UI
            InitializeUIReferences();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Pulizia risorse se necessario
        }
        
        private void InitializeUIReferences()
        {
            GameObject energyBarObj = GameObject.Find("FocusTimeEnergyBar");
            if (energyBarObj != null)
                _focusTimeEnergyBar = energyBarObj.GetComponent<RectTransform>();
                
            GameObject cooldownObj = GameObject.Find("FocusTimeCooldown");
            if (cooldownObj != null)
                _focusTimeCooldownIndicator = cooldownObj.GetComponent<RectTransform>();
                
            _radialMenuContainer = GameObject.Find("FocusTimeRadialMenu");
            if (_radialMenuContainer != null)
            {
                _radialMenuContainer.SetActive(false);
                
                // Inizializza gli slot degli oggetti
                Transform slotsContainer = _radialMenuContainer.transform.Find("ItemSlots");
                if (slotsContainer != null)
                {
                    _itemSlots = new RectTransform[slotsContainer.childCount];
                    for (int i = 0; i < slotsContainer.childCount; i++)
                    {
                        _itemSlots[i] = slotsContainer.GetChild(i).GetComponent<RectTransform>();
                    }
                }
            }
            
            _timeScaleEffect = GameObject.Find("TimeScaleVFX");
            if (_timeScaleEffect != null)
                _timeScaleEffect.SetActive(false);
        }
        
        #endregion
        
        #region System Lifecycle
        
        public void OnUpdate(ref SystemState state)
        {
            UpdateFocusTimeUI(ref state);
            ProcessFocusTimeEvents(ref state);
        }
        
        #endregion
        
        #region UI Updates
        
        /// <summary>
        /// Aggiorna gli elementi dell'interfaccia del Focus Time in base allo stato attuale
        /// </summary>
        private void UpdateFocusTimeUI(ref SystemState state)
        {
            if (!_playerFocusTimeQuery.IsEmpty)
            {
                var focusTimeComponent = state.EntityManager.GetComponentData<FocusTimeComponent>(
                    _playerFocusTimeQuery.ToEntityArray(Unity.Collections.Allocator.Temp)[0]
                );
                
                // Aggiorna la barra di energia
                if (_focusTimeEnergyBar != null)
                {
                    _focusTimeEnergyBar.localScale = new Vector3(
                        focusTimeComponent.EnergyPercentage,
                        1.0f,
                        1.0f
                    );
                }
                
                // Aggiorna l'indicatore di cooldown
                if (_focusTimeCooldownIndicator != null && focusTimeComponent.CooldownRemaining > 0)
                {
                    float cooldownPercentage = 1.0f - (focusTimeComponent.CooldownRemaining / focusTimeComponent.Cooldown);
                    _focusTimeCooldownIndicator.localScale = new Vector3(
                        cooldownPercentage,
                        1.0f,
                        1.0f
                    );
                    
                    // Mostra l'indicatore solo durante il cooldown
                    _focusTimeCooldownIndicator.gameObject.SetActive(focusTimeComponent.CooldownRemaining > 0);
                }
                
                // Gestisci interfaccia radiale e effetto rallentamento
                if (_radialMenuContainer != null)
                {
                    _radialMenuContainer.SetActive(focusTimeComponent.IsActive);
                    
                    if (focusTimeComponent.IsActive)
                    {
                        // Animazione pulsante per l'interfaccia radiale
                        _pulseAnimationTime += SystemAPI.Time.DeltaTime * PULSE_SPEED;
                        float pulse = 1.0f + PULSE_AMPLITUDE * math.sin(_pulseAnimationTime);
                        
                        // Aggiorna la visualizzazione degli slot degli oggetti
                        for (int i = 0; i < math.min(_itemSlots.Length, focusTimeComponent.ItemSlots.Length); i++)
                        {
                            if (_itemSlots[i] != null)
                            {
                                bool hasItem = focusTimeComponent.ItemSlots[i] != Entity.Null;
                                
                                // Trova l'icona dell'oggetto e la mostra solo se lo slot ha un oggetto
                                Transform iconTransform = _itemSlots[i].Find("ItemIcon");
                                if (iconTransform != null)
                                    iconTransform.gameObject.SetActive(hasItem);
                                
                                // Slot vuoto ha opacità ridotta
                                CanvasGroup canvasGroup = _itemSlots[i].GetComponent<CanvasGroup>();
                                if (canvasGroup != null)
                                    canvasGroup.alpha = hasItem ? 1.0f : 0.5f;
                            }
                        }
                    }
                    else
                    {
                        _pulseAnimationTime = 0;
                    }
                }
                
                // Attiva/disattiva effetto visual del rallentamento tempo
                if (_timeScaleEffect != null)
                {
                    _timeScaleEffect.SetActive(focusTimeComponent.IsActive);
                }
            }
        }
        
        #endregion
        
        #region Event Handling
        
        /// <summary>
        /// Processa gli eventi relativi al Focus Time e applica i feedback visivi corrispondenti
        /// </summary>
        private void ProcessFocusTimeEvents(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            
            // Gestisci eventi di Focus Time per feedback visivi/audio
            var activatedEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<FocusTimeActivatedEvent>());
            foreach (var entity in activatedEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                // Riproduci effetti audio/visivi di attivazione
                PlayFocusTimeActivationEffects();
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
            
            var deactivatedEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<FocusTimeDeactivatedEvent>());
            foreach (var entity in deactivatedEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                // Riproduci effetti audio/visivi di disattivazione
                PlayFocusTimeDeactivationEffects();
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
            
            var readyEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<FocusTimeReadyEvent>());
            foreach (var entity in readyEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                // Effetto visivo/audio di disponibilità
                PlayFocusTimeReadyEffects();
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
            
            var itemUsedEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<ItemUsedEvent>());
            foreach (var entity in itemUsedEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var itemUsedEvent = entityManager.GetComponentData<ItemUsedEvent>(entity);
                
                // Effetto visivo/audio di utilizzo oggetto
                PlayItemUsedEffects(itemUsedEvent.SlotIndex);
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        // Metodi per riprodurre effetti visivi e audio
        
        private void PlayFocusTimeActivationEffects()
        {
            // Implementazione degli effetti di attivazione
            // Ad esempio, riprodurre suono, animazione, particelle, ecc.
            Debug.Log("Focus Time Activated - Playing effects");
            
            // Esempio: chiamata al sound manager
            if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
            {
                RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX("FocusTimeActivation");
            }
        }
        
        private void PlayFocusTimeDeactivationEffects()
        {
            // Implementazione degli effetti di disattivazione
            Debug.Log("Focus Time Deactivated - Playing effects");
            
            // Esempio: chiamata al sound manager
            if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
            {
                RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX("FocusTimeDeactivation");
            }
        }
        
        private void PlayFocusTimeReadyEffects()
        {
            // Implementazione degli effetti di disponibilità
            Debug.Log("Focus Time Ready - Playing effects");
            
            // Esempio: chiamata al sound manager
            if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
            {
                RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX("FocusTimeReady");
            }
        }
        
        private void PlayItemUsedEffects(int slotIndex)
        {
            // Implementazione degli effetti di utilizzo oggetto
            Debug.Log($"Item Used from slot {slotIndex} - Playing effects");
            
            // Esempio: chiamata al sound manager
            if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
            {
                RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX("ItemUsed");
            }
            
            // Animazione flash dello slot
            if (slotIndex >= 0 && slotIndex < _itemSlots.Length && _itemSlots[slotIndex] != null)
            {
                // Esempio di animazione semplice (in una implementazione reale si userebbe una vera animazione)
                _itemSlots[slotIndex].localScale = Vector3.one * 1.5f;
                
                // Reset della scala dopo un breve ritardo
                MonoBehaviour.FindFirstObjectByType<MonoBehaviour>().StartCoroutine(ResetSlotScale(slotIndex));
            }
        }
        
        // Coroutine di utilità per ripristinare la scala dello slot
        private System.Collections.IEnumerator ResetSlotScale(int slotIndex)
        {
            yield return new WaitForSeconds(0.15f);
            
            if (slotIndex >= 0 && slotIndex < _itemSlots.Length && _itemSlots[slotIndex] != null)
            {
                _itemSlots[slotIndex].localScale = Vector3.one;
            }
        }
        
        #endregion
    }
}