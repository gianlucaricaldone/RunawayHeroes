using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.ECS.Systems.Input
{
    /// <summary>
    /// Sistema che gestisce gli input touch per dispositivi mobili.
    /// Elabora gli eventi di touch, riconosce i gesti principali e li
    /// converte in componenti di input utilizzabili dagli altri sistemi.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct TouchInputSystem : ISystem
    {
        // Singleton per l'input
        private Entity _inputEntity;
        
        // Configurazione
        private float _swipeThreshold;      // Distanza minima per considerare un touch come swipe
        private float _tapThreshold;        // Tempo massimo per considerare un touch come tap
        private float _doubleTapThreshold;  // Tempo massimo tra due tap per double tap
        
        // Flag per gestione dei touch
        private bool _isTouching;
        private float _touchStartTime;
        private float2 _touchStartPosition;
        private float _lastTapTime;
        
        /// <summary>
        /// Inizializza il sistema di input touch
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Inizializza i valori di configurazione
            _swipeThreshold = 50.0f;
            _tapThreshold = 0.3f;
            _doubleTapThreshold = 0.5f;
            
            // Inizializza i flag di stato
            _isTouching = false;
            _touchStartTime = 0f;
            _lastTapTime = -1f;
            
            // Crea l'entità singleton per l'input se non esiste già
            var inputQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<TouchInputSingleton>());
            
            if (inputQuery.IsEmpty)
            {
                _inputEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(_inputEntity, new TouchInputSingleton());
                state.EntityManager.AddComponentData(_inputEntity, new TouchInputComponent
                {
                    IsTouching = false,
                    TouchPosition = float2.zero,
                    TouchDelta = float2.zero,
                    Tap = false,
                    DoubleTap = false,
                    SwipeUp = false,
                    SwipeDown = false,
                    SwipeLeft = false,
                    SwipeRight = false
                });
            }
            else
            {
                _inputEntity = inputQuery.GetSingletonEntity();
            }
        }
        
        /// <summary>
        /// Pulizia delle risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Elabora gli input touch ad ogni frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Resetta i flag di input che valgono per un solo frame
            var touchInput = state.EntityManager.GetComponentData<TouchInputComponent>(_inputEntity);
            touchInput.Tap = false;
            touchInput.DoubleTap = false;
            touchInput.SwipeUp = false;
            touchInput.SwipeDown = false;
            touchInput.SwipeLeft = false;
            touchInput.SwipeRight = false;
            touchInput.TouchDelta = float2.zero;
            
            // Gestisce gli input touch (solo su dispositivi mobili o editor con touch abilitato)
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                float2 touchPosition = new float2(touch.position.x, touch.position.y);
                
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        _isTouching = true;
                        _touchStartTime = currentTime;
                        _touchStartPosition = touchPosition;
                        touchInput.IsTouching = true;
                        touchInput.TouchPosition = touchPosition;
                        break;
                        
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        touchInput.IsTouching = true;
                        touchInput.TouchDelta = touchPosition - touchInput.TouchPosition;
                        touchInput.TouchPosition = touchPosition;
                        break;
                        
                    case TouchPhase.Ended:
                        _isTouching = false;
                        touchInput.IsTouching = false;
                        
                        // Calcola la durata del touch
                        float touchDuration = currentTime - _touchStartTime;
                        
                        // Calcola la distanza percorsa dal dito
                        float2 touchDelta = touchPosition - _touchStartPosition;
                        float touchDistance = math.length(touchDelta);
                        
                        // Se la distanza è piccola e la durata breve, è un tap
                        if (touchDistance < _swipeThreshold && touchDuration < _tapThreshold)
                        {
                            touchInput.Tap = true;
                            
                            // Controlla se è un double tap
                            if (currentTime - _lastTapTime < _doubleTapThreshold)
                            {
                                touchInput.DoubleTap = true;
                                _lastTapTime = -1f; // Resetta per evitare triple tap
                            }
                            else
                            {
                                _lastTapTime = currentTime;
                            }
                        }
                        // Altrimenti potrebbe essere uno swipe
                        else if (touchDistance >= _swipeThreshold)
                        {
                            // Determina la direzione dello swipe in base al componente maggiore del delta
                            if (math.abs(touchDelta.x) > math.abs(touchDelta.y))
                            {
                                // Swipe orizzontale
                                touchInput.SwipeRight = touchDelta.x > 0;
                                touchInput.SwipeLeft = touchDelta.x < 0;
                            }
                            else
                            {
                                // Swipe verticale
                                touchInput.SwipeUp = touchDelta.y > 0;
                                touchInput.SwipeDown = touchDelta.y < 0;
                            }
                        }
                        break;
                        
                    case TouchPhase.Canceled:
                        _isTouching = false;
                        touchInput.IsTouching = false;
                        break;
                }
            }
            // Supporto per testing in editor con mouse
            else if (Application.isEditor)
            {
                float2 mousePosition = new float2(Input.mousePosition.x, Input.mousePosition.y);
                
                if (Input.GetMouseButtonDown(0))
                {
                    _isTouching = true;
                    _touchStartTime = currentTime;
                    _touchStartPosition = mousePosition;
                    touchInput.IsTouching = true;
                    touchInput.TouchPosition = mousePosition;
                }
                else if (Input.GetMouseButton(0))
                {
                    touchInput.IsTouching = true;
                    touchInput.TouchDelta = mousePosition - touchInput.TouchPosition;
                    touchInput.TouchPosition = mousePosition;
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    _isTouching = false;
                    touchInput.IsTouching = false;
                    
                    // Calcola la durata del click
                    float touchDuration = currentTime - _touchStartTime;
                    
                    // Calcola la distanza percorsa dal mouse
                    float2 touchDelta = mousePosition - _touchStartPosition;
                    float touchDistance = math.length(touchDelta);
                    
                    // Applica la stessa logica del touch
                    if (touchDistance < _swipeThreshold && touchDuration < _tapThreshold)
                    {
                        touchInput.Tap = true;
                        
                        if (currentTime - _lastTapTime < _doubleTapThreshold)
                        {
                            touchInput.DoubleTap = true;
                            _lastTapTime = -1f;
                        }
                        else
                        {
                            _lastTapTime = currentTime;
                        }
                    }
                    else if (touchDistance >= _swipeThreshold)
                    {
                        if (math.abs(touchDelta.x) > math.abs(touchDelta.y))
                        {
                            touchInput.SwipeRight = touchDelta.x > 0;
                            touchInput.SwipeLeft = touchDelta.x < 0;
                        }
                        else
                        {
                            touchInput.SwipeUp = touchDelta.y > 0;
                            touchInput.SwipeDown = touchDelta.y < 0;
                        }
                    }
                }
            }
            #endif
            
            // Aggiorna il componente di input
            state.EntityManager.SetComponentData(_inputEntity, touchInput);
        }
    }
    
    /// <summary>
    /// Tag per il singleton dell'input touch
    /// </summary>
    public struct TouchInputSingleton : IComponentData
    {
    }
    
    /// <summary>
    /// Componente che contiene lo stato dell'input touch
    /// </summary>
    public struct TouchInputComponent : IComponentData
    {
        // Stato del touch
        public bool IsTouching;        // Indica se il touch è attivo
        public float2 TouchPosition;   // Posizione attuale del touch
        public float2 TouchDelta;      // Delta della posizione rispetto al frame precedente
        
        // Gesti
        public bool Tap;               // Tap singolo
        public bool DoubleTap;         // Doppio tap
        public bool SwipeUp;           // Swipe verso l'alto
        public bool SwipeDown;         // Swipe verso il basso
        public bool SwipeLeft;         // Swipe verso sinistra
        public bool SwipeRight;        // Swipe verso destra
    }
}
