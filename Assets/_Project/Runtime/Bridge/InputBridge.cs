using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// Componente bridge che collega il sistema di input di Unity con l'ECS.
    /// Questo componente deve essere aggiunto a un GameObject nella scena
    /// e si occupa di convertire gli input in componenti ECS.
    /// </summary>
    public class InputBridge : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Input Settings")]
        [Tooltip("Sensibilità per il touch/swipe laterale")]
        public float lateralSensitivity = 1.0f;
        
        [Tooltip("Durata minima del tocco per attivare il Focus Time (in secondi)")]
        public float focusTimeActivationDuration = 0.3f;
        
        [Tooltip("Flag per abilitare i controlli da tastiera (per debug)")]
        public bool enableKeyboardControls = true;
        
        [Tooltip("Flag per abilitare i controlli touch (per dispositivi mobili)")]
        public bool enableTouchControls = true;
        
        // Tocco corrente per il tracking della durata
        private Touch? currentTouch = null;
        private float touchStartTime = 0f;
        
        // Ultimo input laterale per smoothing
        private float lastLateralInput = 0f;
        
        /// <summary>
        /// Converte questo componente Unity in componenti ECS
        /// </summary>
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Aggiunge i tag necessari all'entità
            dstManager.AddComponent<TagComponent>(entity);
            
            // Inizializza l'InputComponent con valori predefiniti
            var inputComponent = InputComponent.Default();
            dstManager.AddComponentData(entity, inputComponent);
            
            // Se necessario, aggiunge altri componenti di input specifici
            dstManager.AddComponent<JumpInputComponent>(entity);
            dstManager.AddComponent<SlideInputComponent>(entity);
            dstManager.AddComponent<FocusTimeInputComponent>(entity);
            dstManager.AddComponent<AbilityInputComponent>(entity);
            
            // Notifica alla console di log
            Debug.Log($"Input Bridge convertito in ECS: Entity {entity.Index}");
        }
        
        /// <summary>
        /// Metodo chiamato quando il bridge viene abilitato
        /// </summary>
        private void OnEnable()
        {
            // Inizializza i valori necessari
            currentTouch = null;
            touchStartTime = 0f;
            lastLateralInput = 0f;
        }
        
        /// <summary>
        /// Metodo chiamato quando il bridge viene disabilitato
        /// </summary>
        private void OnDisable()
        {
            // Rilascia eventuali risorse
            currentTouch = null;
        }
        
        /// <summary>
        /// Implementazione di Unity per update visivi
        /// </summary>
        private void OnGUI()
        {
            // Se necessario, visualizza debug info per input durante lo sviluppo
            if (Debug.isDebugBuild && enableKeyboardControls)
            {
                GUILayout.BeginArea(new Rect(10, 10, 300, 100));
                GUILayout.Label("Input Debug:");
                GUILayout.Label($"Lateral: {lastLateralInput}");
                GUILayout.Label($"Touch Active: {currentTouch.HasValue}");
                if (currentTouch.HasValue)
                {
                    GUILayout.Label($"Touch Duration: {Time.time - touchStartTime:F2}s");
                }
                GUILayout.EndArea();
            }
        }

        private void ProcessFocusTimeInput(Entity playerEntity, EntityCommandBuffer commandBuffer)
        {
            var focusTimeInput = new FocusTimeInputComponent
            {
                ActivateFocusTime = false,
                DeactivateFocusTime = false,
                SelectedItemIndex = -1,
                NewItemDetected = false,
                NewItemEntity = Entity.Null,
                FocusPointerPosition = float2.zero,
                ActivationHoldTime = 0.0f
            };
            
            // Input da tastiera
            if (enableKeyboardControls)
            {
                // [Codice identico a quello precedente per la tastiera]
            }
            
            // Input touch
            if (enableTouchControls && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                // Salva la posizione del tocco attuale
                focusTimeInput.FocusPointerPosition = new float2(touch.position.x, touch.position.y);
                
                // Tocco prolungato per attivare Focus Time
                if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
                {
                    if (currentTouch.HasValue && touch.fingerId == currentTouch.Value.fingerId)
                    {
                        // Calcola il tempo di pressione per l'attivazione del Focus Time
                        focusTimeInput.ActivationHoldTime = Time.time - touchStartTime;
                        
                        // Attiva dopo la soglia di tempo
                        if (focusTimeInput.ActivationHoldTime >= focusTimeActivationDuration)
                        {
                            focusTimeInput.ActivateFocusTime = true;
                            
                            // Se il dito si è spostato, gestisci anche l'input di movimento laterale
                            if (touch.phase == TouchPhase.Moved)
                            {
                                // Calcola lo spostamento laterale dal punto iniziale del tocco
                                float deltaX = touch.position.x - currentTouch.Value.position.x;
                                
                                // Normalizza lo spostamento in base alla larghezza dello schermo
                                float normalizedDelta = deltaX / Screen.width * lateralSensitivity;
                                
                                // Aggiungi anche un componente di input di movimento
                                var movementInput = EntityManager.HasComponent<InputComponent>(playerEntity) 
                                    ? EntityManager.GetComponentData<InputComponent>(playerEntity) 
                                    : new InputComponent();
                                
                                // Imposta il movimento laterale in base allo spostamento del dito
                                movementInput.LateralMovement = normalizedDelta;
                                // Assicurati che il movimento sia abilitato durante il Focus Time
                                movementInput.IsMovementEnabled = true;
                                
                                // Aggiorna il componente di input
                                commandBuffer.SetComponent(playerEntity, movementInput);
                            }
                        }
                    }
                    else
                    {
                        // Primo frame di tocco
                        currentTouch = touch;
                        touchStartTime = Time.time;
                    }
                }
                
                // Rilascio del tocco disattiva Focus Time
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    if (currentTouch.HasValue && touch.fingerId == currentTouch.Value.fingerId)
                    {
                        focusTimeInput.DeactivateFocusTime = true;
                        currentTouch = null;
                        touchStartTime = 0;
                        
                        // Resetta anche l'input di movimento quando si rilascia il tocco
                        if (EntityManager.HasComponent<InputComponent>(playerEntity))
                        {
                            var movementInput = EntityManager.GetComponentData<InputComponent>(playerEntity);
                            movementInput.LateralMovement = 0;
                            commandBuffer.SetComponent(playerEntity, movementInput);
                        }
                    }
                }
            }
            
            // Aggiungi o sostituisci il componente FocusTimeInput al giocatore
            if (EntityManager.HasComponent<FocusTimeInputComponent>(playerEntity))
            {
                commandBuffer.SetComponent(playerEntity, focusTimeInput);
            }
            else
            {
                commandBuffer.AddComponent(playerEntity, focusTimeInput);
            }
        }
    }
}