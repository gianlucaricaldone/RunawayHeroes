// Path: Assets/_Project/Runtime/Bridge/InputBridge.cs
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Systems.Abilities;

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// Componente bridge che collega il sistema di input di Unity con l'ECS.
    /// Questo componente deve essere aggiunto a un GameObject nella scena
    /// e si occupa di convertire gli input in componenti ECS.
    /// </summary>
    [AddComponentMenu("RunawayHeroes/Bridges/Input Bridge")]
    public class InputBridge : MonoBehaviour
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
        
        // Riferimento all'entità creata
        private Entity _inputEntity;
        private EntityManager _entityManager;
        private EntityCommandBuffer _commandBuffer;
        
        // Classe Baker interna che gestisce la conversione GameObject -> Entity
        public class InputBridgeBaker : Baker<InputBridge>
        {
            public override void Bake(InputBridge authoring)
            {
                // Crea un'entità con flag di utilizzo dinamico
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Aggiungi i tag necessari all'entità
                AddComponent<TagComponent>(entity);
                
                // Inizializza l'InputComponent con valori predefiniti
                AddComponent(entity, InputComponent.Default());
                
                // Aggiungi i componenti di input specifici
                AddComponent<JumpInputComponent>(entity);
                AddComponent<SlideInputComponent>(entity);
                AddComponent<FocusTimeInputComponent>(entity);
                AddComponent<AbilityInputComponent>(entity);
                AddComponent<ResonanceInputComponent>(entity);
                
                // Aggiungi un componente che contiene le impostazioni di input
                AddComponent(entity, new InputSettingsComponent
                {
                    LateralSensitivity = authoring.lateralSensitivity,
                    FocusTimeActivationDuration = authoring.focusTimeActivationDuration,
                    EnableKeyboardControls = authoring.enableKeyboardControls,
                    EnableTouchControls = authoring.enableTouchControls
                });
                
                // Aggiungi un tag per identificare questa entità come il bridge di input
                AddComponent<InputBridgeTag>(entity);
            }
        }
        
        /// <summary>
        /// Inizializza il bridge all'avvio
        /// </summary>
        private void Start()
        {
            // Ottieni il World e l'EntityManager
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                _entityManager = defaultWorld.EntityManager;
                
                // Trova l'entità di input creata dal baker
                var query = _entityManager.CreateEntityQuery(typeof(InputBridgeTag));
                if (query.CalculateEntityCount() > 0)
                {
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                    _inputEntity = entities[0];
                    entities.Dispose();
                    
                    Debug.Log($"Input Bridge trovato: Entity {_inputEntity.Index}");
                }
                else
                {
                    Debug.LogWarning("Nessuna entità InputBridge trovata. Gli input potrebbero non funzionare correttamente.");
                }
            }
            else
            {
                Debug.LogError("Nessun World ECS trovato. Assicurati che il sistema ECS sia correttamente inizializzato.");
            }
        }
        
        /// <summary>
        /// Aggiorna gli input ad ogni frame
        /// </summary>
        private void Update()
        {
            if (_entityManager == null || !_entityManager.Exists(_inputEntity))
                return;
            
            // Crea un command buffer temporaneo per questo frame
            _commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            
            // Processa gli input
            ProcessFocusTimeInput(_inputEntity);
            ProcessMovementInput(_inputEntity);
            ProcessJumpInput(_inputEntity);
            ProcessSlideInput(_inputEntity);
            ProcessAbilityInput(_inputEntity);
            
            // Esegui i comandi accumulati
            _commandBuffer.Playback(_entityManager);
            _commandBuffer.Dispose();
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

        private void ProcessFocusTimeInput(Entity playerEntity)
        {
            if (!_entityManager.HasComponent<FocusTimeInputComponent>(playerEntity))
                return;
                
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
                // Implementazione tastiera per Focus Time (se necessario)
                if (Input.GetKeyDown(KeyCode.F))
                {
                    focusTimeInput.ActivateFocusTime = true;
                }
                
                if (Input.GetKeyUp(KeyCode.F))
                {
                    focusTimeInput.DeactivateFocusTime = true;
                }
                
                // Selezione slot con i tasti numerici
                for (int i = 0; i < 9; i++)
                {
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        focusTimeInput.SelectedItemIndex = i;
                    }
                }
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
                                var movementInput = _entityManager.GetComponentData<InputComponent>(playerEntity);
                                
                                // Imposta il movimento laterale in base allo spostamento del dito
                                movementInput.LateralMovement = normalizedDelta;
                                // Assicurati che il movimento sia abilitato durante il Focus Time
                                movementInput.IsMovementEnabled = true;
                                
                                // Aggiorna il componente di input
                                _commandBuffer.SetComponent(playerEntity, movementInput);
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
                        var movementInput = _entityManager.GetComponentData<InputComponent>(playerEntity);
                        movementInput.LateralMovement = 0;
                        _commandBuffer.SetComponent(playerEntity, movementInput);
                    }
                }
            }
            
            _commandBuffer.SetComponent(playerEntity, focusTimeInput);
        }
        
        private void ProcessMovementInput(Entity playerEntity)
        {
            if (!_entityManager.HasComponent<InputComponent>(playerEntity))
                return;
                
            var inputComponent = _entityManager.GetComponentData<InputComponent>(playerEntity);
            
            // Inizialmente assumiamo che il movimento sia abilitato
            inputComponent.IsMovementEnabled = true;
            
            // Input da tastiera per movimento laterale
            if (enableKeyboardControls)
            {
                float lateralInput = 0f;
                
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                {
                    lateralInput -= 1.0f;
                }
                
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                {
                    lateralInput += 1.0f;
                }
                
                // Applica la sensibilità e aggiorna l'input
                inputComponent.LateralMovement = lateralInput * lateralSensitivity;
                lastLateralInput = inputComponent.LateralMovement;
            }
            
            // Input touch per movimento laterale (se non gestito da Focus Time)
            if (enableTouchControls && Input.touchCount > 0 && !currentTouch.HasValue)
            {
                Touch touch = Input.GetTouch(0);
                
                // Swipe orizzontale sulla metà inferiore dello schermo
                if (touch.position.y < Screen.height * 0.5f)
                {
                    // Calcola la posizione normalizzata orizzontale (-1 a 1)
                    float normalizedX = (touch.position.x / Screen.width) * 2.0f - 1.0f;
                    inputComponent.LateralMovement = normalizedX * lateralSensitivity;
                    lastLateralInput = inputComponent.LateralMovement;
                }
            }
            
            // Calcola la direzione di movimento 3D
            inputComponent.MoveDirection = new float2(inputComponent.LateralMovement, 1.0f);
            
            // Aggiorna il componente
            _commandBuffer.SetComponent(playerEntity, inputComponent);
        }
        
        private void ProcessJumpInput(Entity playerEntity)
        {
            if (!_entityManager.HasComponent<JumpInputComponent>(playerEntity))
                return;
                        
            var jumpInput = new JumpInputComponent
            {
                JumpPressed = false,
                JumpForceMultiplier = 1.0f // Valore di default senza potenziamenti
            };
            
            // Input da tastiera per salto
            if (enableKeyboardControls && Input.GetKeyDown(KeyCode.Space))
            {
                jumpInput.JumpPressed = true;
            }
            
            // Input touch per salto (tap rapido sulla parte superiore dello schermo)
            if (enableTouchControls && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && touch.position.y > Screen.height * 0.5f)
                {
                    // Tap nella metà superiore dello schermo attiva il salto
                    jumpInput.JumpPressed = true;
                }
            }
            
            _commandBuffer.SetComponent(playerEntity, jumpInput);
        }
        
        private void ProcessSlideInput(Entity playerEntity)
        {
            if (!_entityManager.HasComponent<SlideInputComponent>(playerEntity))
                return;
                
            var slideInput = new SlideInputComponent
            {
                SlidePressed = false
            };
            
            // Input da tastiera per scivolata
            if (enableKeyboardControls && Input.GetKeyDown(KeyCode.S))
            {
                slideInput.SlidePressed = true;
            }
            
            // Input touch per scivolata (swipe veloce verso il basso)
            if (enableTouchControls && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    // Se il dito ha fatto uno swipe verso il basso di almeno il 10% dell'altezza dello schermo
                    if (touch.deltaPosition.y < -Screen.height * 0.1f)
                    {
                        slideInput.SlidePressed = true;
                    }
                }
            }
            
            _commandBuffer.SetComponent(playerEntity, slideInput);
        }
        
        private void ProcessAbilityInput(Entity playerEntity)
        {
            if (!_entityManager.HasComponent<AbilityInputComponent>(playerEntity))
                return;
                
            var abilityInput = new AbilityInputComponent
            {
                ActivateAbility = false,
                TargetPosition = float2.zero,
                CurrentAbilityType = AbilityType.None
            };
            
            // Input da tastiera per abilità
            if (enableKeyboardControls && Input.GetKeyDown(KeyCode.E))
            {
                abilityInput.ActivateAbility = true;
                abilityInput.TargetPosition = new float2(Screen.width * 0.5f, Screen.height * 0.5f);
            }
            
            // Input touch per abilità (doppio tap)
            if (enableTouchControls && Input.touchCount > 0)
            {
                // Implementazione semplificata - in una versione completa bisognerebbe
                // tracciare i touch per rilevare un vero doppio tap
                Touch touch = Input.GetTouch(0);
                if (touch.tapCount >= 2)
                {
                    abilityInput.ActivateAbility = true;
                    abilityInput.TargetPosition = new float2(touch.position.x, touch.position.y);
                }
            }
            
            _commandBuffer.SetComponent(playerEntity, abilityInput);
        }
    }
    
    /// <summary>
    /// Componente tag per identificare l'entità InputBridge
    /// </summary>
    public struct InputBridgeTag : IComponentData { }
    
    /// <summary>
    /// Componente per memorizzare le impostazioni di input
    /// </summary>
    public struct InputSettingsComponent : IComponentData
    {
        public float LateralSensitivity;
        public float FocusTimeActivationDuration;
        public bool EnableKeyboardControls;
        public bool EnableTouchControls;
    }
}