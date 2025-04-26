// File: Assets/_Project/Code/Core/Managers/FocusTimeManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using RunawayHeroes.Items;

namespace RunawayHeroes.Manager
{
    public class FocusTimeManager : MonoBehaviour
    {
        // Singleton pattern
        private static FocusTimeManager _instance;
        public static FocusTimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<FocusTimeManager>();
                    if (_instance == null)
                    {
                        Debug.LogError("FocusTimeManager non trovato nella scena!");
                    }
                }
                return _instance;
            }
        }

        [Header("Focus Time Settings")]
        [SerializeField] private float maxDuration = 10f;          // Durata massima in secondi
        [SerializeField] private float cooldownTime = 25f;         // Cooldown tra utilizzi
        [SerializeField] private float timeScale = 0.3f;           // 30% della velocità normale
        [SerializeField] private Image focusTimeFillImage;         // Riferimento all'UI

        [Header("Item Selection")]
        [SerializeField] private GameObject itemSelectionWheel;    // Ruota di selezione oggetti
        [SerializeField] private int maxItemSlots = 4;             // Numero massimo di slot oggetti
        
        private bool isActive = false;
        private float remainingTime = 0f;
        private float cooldownRemaining = 0f;
        private float focusTimeEnergy = 1f;  // da 0 a 1, rappresenta l'energia disponibile
        
        public bool IsActive => isActive;
        public float CooldownRemaining => cooldownRemaining;
        public float FocusTimeEnergy => focusTimeEnergy;

        // Eventi richiesti da TutorialManager
        public event Action OnFocusTimeActivated;
        public event Action OnFocusTimeDeactivated;
        public event Action<IUsableItem> OnItemSelected;
        public event Action<IUsableItem> OnItemUsed;
        
        private void Awake()
        {
            // Implementazione pattern singleton
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Inizializza componenti
            if (itemSelectionWheel != null)
            {
                itemSelectionWheel.SetActive(false);
            }
        }
        
        private void Update()
        {
            // Gestione focus time attivo
            if (isActive)
            {
                remainingTime -= Time.unscaledDeltaTime;
                
                // Aggiorna UI
                if (focusTimeFillImage != null)
                {
                    focusTimeFillImage.fillAmount = remainingTime / maxDuration;
                }
                
                // Termina focus time se il tempo è esaurito
                if (remainingTime <= 0)
                {
                    DeactivateFocusTime();
                }
            }
            
            // Gestione cooldown
            if (cooldownRemaining > 0)
            {
                cooldownRemaining -= Time.unscaledDeltaTime;
            }
            
            // Rigenerazione dell'energia focus time quando non attivo
            if (!isActive && focusTimeEnergy < 1f)
            {
                focusTimeEnergy += Time.deltaTime * 0.1f; // Rigenerazione del 10% al secondo
                focusTimeEnergy = Mathf.Clamp01(focusTimeEnergy);
                
                // Aggiorna UI
                UpdateFocusTimeUI();
            }
        }
        
        public bool CanActivateFocusTime()
        {
            return !isActive && cooldownRemaining <= 0 && focusTimeEnergy > 0.1f;
        }
        
        public void ActivateFocusTime()
        {
            if (!CanActivateFocusTime())
                return;
            
            isActive = true;
            remainingTime = maxDuration * focusTimeEnergy; // Durata basata sull'energia disponibile
            
            // Rallenta il tempo di gioco
            Time.timeScale = Mathf.Max(0.01f, timeScale);
            
            // Attiva interfaccia selezione oggetti
            if (itemSelectionWheel != null)
            {
                itemSelectionWheel.SetActive(true);
            }
            
            // Attiva effetti visivi
            ActivateFocusTimeEffects();
            
            // Aggiorna UI
            UpdateFocusTimeUI();

            // Trigger the event
            OnFocusTimeActivated?.Invoke();
        }
        
        public void DeactivateFocusTime()
        {
            if (!isActive)
                return;
            
            isActive = false;
            remainingTime = 0f;
            cooldownRemaining = cooldownTime;
            
            // Resetta il tempo di gioco
            Time.timeScale = 1f;
            
            // Disattiva interfaccia selezione oggetti
            if (itemSelectionWheel != null)
            {
                itemSelectionWheel.SetActive(false);
            }
            
            // Disattiva effetti visivi
            DeactivateFocusTimeEffects();
            
            // Resetta energia focus time
            focusTimeEnergy = 0f;
            
            // Aggiorna UI
            UpdateFocusTimeUI();

            // Trigger the event
            OnFocusTimeDeactivated?.Invoke();
        }
        
        public bool CanSpendFocusTime(float amount)
        {
            return focusTimeEnergy >= amount;
        }
        
        public void SpendFocusTime(float amount)
        {
            if (!CanSpendFocusTime(amount))
                return;
            
            focusTimeEnergy -= amount;
            focusTimeEnergy = Mathf.Max(0f, focusTimeEnergy);
            
            // Se focus time è attivo, aggiorna il tempo rimanente
            if (isActive)
            {
                remainingTime = maxDuration * focusTimeEnergy;
                
                // Se l'energia è esaurita, disattiva focus time
                if (focusTimeEnergy <= 0f)
                {
                    DeactivateFocusTime();
                }
            }
            
            // Aggiorna UI
            UpdateFocusTimeUI();
        }
        
        // Metodo per il tutorial che resetta il sistema Focus Time
        public void ResetForTutorial()
        {
            // Resetta cooldown
            cooldownRemaining = 0f;
            
            // Ricarica completamente l'energia
            focusTimeEnergy = 1f;
            
            // Se era attivo, disattiva prima
            if (isActive)
            {
                DeactivateFocusTime();
            }
            
            // Aggiorna UI
            UpdateFocusTimeUI();
        }
        
        // Metodi per la selezione e l'uso degli oggetti
        public void SelectItem(IUsableItem item)
        {
            if (item == null)
                return;

            // Logica per selezionare un oggetto
            // ...

            // Trigger the event
            OnItemSelected?.Invoke(item);
        }

        public void UseItem(IUsableItem item)
        {
            if (item == null)
                return;

            // Logica per usare un oggetto
            // ...

            // Trigger the event
            OnItemUsed?.Invoke(item);
        }

        private void UpdateFocusTimeUI()
        {
            if (focusTimeFillImage != null)
            {
                focusTimeFillImage.fillAmount = isActive ? remainingTime / maxDuration : focusTimeEnergy;
            }
        }
        
        private void ActivateFocusTimeEffects()
        {
            // Implementare effetti visivi del Focus Time
            // Ad esempio: post-processing, filtri, particelle, etc.
        }
        
        private void DeactivateFocusTimeEffects()
        {
            // Rimuovere effetti visivi del Focus Time
        }
    }
}