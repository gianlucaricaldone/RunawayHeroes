using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.ECS.Systems.Input
{
    /// <summary>
    /// Sistema responsabile del riconoscimento di gesti complessi
    /// come pinch, rotate, e altri pattern di movimento che possono
    /// attivare abilità speciali o comandi di gioco avanzati.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(TouchInputSystem))]
    public partial struct GestureRecognitionSystem : ISystem
    {
        // Entità per il singleton dei gesti
        private Entity _gestureEntity;
        
        // Parametri di configurazione per il riconoscimento dei gesti
        private float _pinchThreshold;          // Soglia per identificare un pinch
        private float _rotationThreshold;       // Soglia per identificare una rotazione
        private float _longPressThreshold;      // Soglia temporale per long press
        private float _edgeSwipeThreshold;      // Margine di schermo per edge swipe
        
        // Stato per il riconoscimento dei gesti
        private bool _isMultiTouch;
        private float _touchStartTime;
        private float2 _touch1StartPos;
        private float2 _touch2StartPos;
        private float _initialTouchDistance;
        private float _initialTouchAngle;
        
        /// <summary>
        /// Inizializza il sistema di riconoscimento gesti
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Inizializza i parametri di configurazione
            _pinchThreshold = 10.0f;           // 10 pixel di differenza per considerare un pinch
            _rotationThreshold = 0.1f;         // 0.1 radianti (circa 5.7 gradi) per considerare una rotazione
            _longPressThreshold = 0.8f;        // 800ms per un long press
            _edgeSwipeThreshold = 50.0f;       // 50 pixel dal bordo per edge swipe
            
            // Inizializza lo stato
            _isMultiTouch = false;
            _touchStartTime = 0f;
            _touch1StartPos = float2.zero;
            _touch2StartPos = float2.zero;
            _initialTouchDistance = 0f;
            _initialTouchAngle = 0f;
            
            // Richiede il singleton TouchInput
            state.RequireForUpdate<TouchInputSingleton>();
            
            // Crea l'entità singleton per i gesti se non esiste già
            var gestureQuery = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<GestureRecognitionSingleton>());
            
            if (gestureQuery.IsEmpty)
            {
                _gestureEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(_gestureEntity, new GestureRecognitionSingleton());
                state.EntityManager.AddComponentData(_gestureEntity, new GestureComponent
                {
                    PinchIn = false,
                    PinchOut = false,
                    Rotate = false,
                    RotationAngle = 0f,
                    LongPress = false,
                    LongPressPosition = float2.zero,
                    EdgeSwipeTop = false,
                    EdgeSwipeBottom = false,
                    EdgeSwipeLeft = false,
                    EdgeSwipeRight = false
                });
            }
            else
            {
                _gestureEntity = gestureQuery.GetSingletonEntity();
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
        /// Aggiorna il riconoscimento dei gesti ad ogni frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Ottieni il singleton di input touch
            var touchInputEntity = SystemAPI.GetSingletonEntity<TouchInputSingleton>();
            var touchInput = state.EntityManager.GetComponentData<TouchInputComponent>(touchInputEntity);
            
            // Ottieni il componente gesti da aggiornare
            var gesture = state.EntityManager.GetComponentData<GestureComponent>(_gestureEntity);
            
            // Resetta i gesti che durano solo un frame
            gesture.PinchIn = false;
            gesture.PinchOut = false;
            gesture.Rotate = false;
            gesture.RotationAngle = 0f;
            gesture.EdgeSwipeTop = false;
            gesture.EdgeSwipeBottom = false;
            gesture.EdgeSwipeLeft = false;
            gesture.EdgeSwipeRight = false;
            
            // Gestisce i gesti con multitouch (solo su dispositivi mobili o editor con touch abilitato)
            #if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR
            // Controlla multitouch
            if (Input.touchCount >= 2)
            {
                Touch touch1 = Input.GetTouch(0);
                Touch touch2 = Input.GetTouch(1);
                
                float2 touch1Pos = new float2(touch1.position.x, touch1.position.y);
                float2 touch2Pos = new float2(touch2.position.x, touch2.position.y);
                
                // Inizia multitouch tracking
                if (!_isMultiTouch && 
                    (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began))
                {
                    _isMultiTouch = true;
                    _touch1StartPos = touch1Pos;
                    _touch2StartPos = touch2Pos;
                    
                    // Calcola distanza e angolo iniziali
                    _initialTouchDistance = math.distance(touch1Pos, touch2Pos);
                    _initialTouchAngle = math.atan2(touch2Pos.y - touch1Pos.y, touch2Pos.x - touch1Pos.x);
                }
                // Continua il tracking
                else if (_isMultiTouch)
                {
                    // Calcola distanza e angolo correnti
                    float currentDistance = math.distance(touch1Pos, touch2Pos);
                    float currentAngle = math.atan2(touch2Pos.y - touch1Pos.y, touch2Pos.x - touch1Pos.x);
                    
                    // Calcola delta per distanza e angolo
                    float distanceDelta = currentDistance - _initialTouchDistance;
                    float angleDelta = currentAngle - _initialTouchAngle;
                    
                    // Normalizza l'angolo tra -PI e PI
                    if (angleDelta > math.PI) angleDelta -= 2 * math.PI;
                    else if (angleDelta < -math.PI) angleDelta += 2 * math.PI;
                    
                    // Riconosce pinch
                    if (math.abs(distanceDelta) > _pinchThreshold)
                    {
                        gesture.PinchIn = distanceDelta < 0;
                        gesture.PinchOut = distanceDelta > 0;
                    }
                    
                    // Riconosce rotazione
                    if (math.abs(angleDelta) > _rotationThreshold)
                    {
                        gesture.Rotate = true;
                        gesture.RotationAngle = angleDelta;
                    }
                }
                
                // Termina multitouch se uno dei touch termina
                if (touch1.phase == TouchPhase.Ended || touch1.phase == TouchPhase.Canceled ||
                    touch2.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Canceled)
                {
                    _isMultiTouch = false;
                }
            }
            else
            {
                _isMultiTouch = false;
            }
            
            // Gestisce i gesti con singolo touch
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                float2 touchPos = new float2(touch.position.x, touch.position.y);
                
                // Inizia tracking per long press
                if (touch.phase == TouchPhase.Began)
                {
                    _touchStartTime = currentTime;
                    gesture.LongPress = false;
                }
                
                // Controlla long press
                if (touch.phase == TouchPhase.Stationary && 
                    (currentTime - _touchStartTime) > _longPressThreshold &&
                    !gesture.LongPress)
                {
                    gesture.LongPress = true;
                    gesture.LongPressPosition = touchPos;
                }
                
                // Controlla edge swipe
                if (touch.phase == TouchPhase.Ended)
                {
                    // Ottiene dimensioni dello schermo
                    float screenWidth = Screen.width;
                    float screenHeight = Screen.height;
                    
                    // Controlla se il touch termina vicino a un bordo
                    bool isNearTopEdge = touchPos.y > screenHeight - _edgeSwipeThreshold;
                    bool isNearBottomEdge = touchPos.y < _edgeSwipeThreshold;
                    bool isNearLeftEdge = touchPos.x < _edgeSwipeThreshold;
                    bool isNearRightEdge = touchPos.x > screenWidth - _edgeSwipeThreshold;
                    
                    // Genera edge swipe se il touch è iniziato lontano dal bordo
                    if (isNearTopEdge && (_touch1StartPos.y < screenHeight - _edgeSwipeThreshold))
                    {
                        gesture.EdgeSwipeTop = true;
                    }
                    else if (isNearBottomEdge && (_touch1StartPos.y > _edgeSwipeThreshold))
                    {
                        gesture.EdgeSwipeBottom = true;
                    }
                    else if (isNearLeftEdge && (_touch1StartPos.x > _edgeSwipeThreshold))
                    {
                        gesture.EdgeSwipeLeft = true;
                    }
                    else if (isNearRightEdge && (_touch1StartPos.x < screenWidth - _edgeSwipeThreshold))
                    {
                        gesture.EdgeSwipeRight = true;
                    }
                    
                    // Resetta long press alla fine del touch
                    gesture.LongPress = false;
                }
            }
            else if (Input.touchCount == 0)
            {
                // Resetta long press se non ci sono touch
                gesture.LongPress = false;
            }
            #endif
            
            // Supporto per testing in editor con mouse e tastiera
            #if UNITY_EDITOR
            if (!_isMultiTouch && Input.touchCount == 0)
            {
                // Simula long press con alt+click
                if (Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftAlt))
                {
                    _touchStartTime = currentTime;
                }
                
                if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt) &&
                    (currentTime - _touchStartTime) > _longPressThreshold &&
                    !gesture.LongPress)
                {
                    gesture.LongPress = true;
                    gesture.LongPressPosition = new float2(Input.mousePosition.x, Input.mousePosition.y);
                }
                
                if (Input.GetMouseButtonUp(0) && Input.GetKey(KeyCode.LeftAlt))
                {
                    gesture.LongPress = false;
                }
                
                // Simula pinch con ctrl+mousewheel
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if (scroll > 0.01f)
                    {
                        gesture.PinchOut = true;
                    }
                    else if (scroll < -0.01f)
                    {
                        gesture.PinchIn = true;
                    }
                }
                
                // Simula rotazione con shift+mousewheel
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    float scroll = Input.GetAxis("Mouse ScrollWheel");
                    if (math.abs(scroll) > 0.01f)
                    {
                        gesture.Rotate = true;
                        gesture.RotationAngle = scroll * math.PI * 0.2f;
                    }
                }
                
                // Simula edge swipe con shift+frecce direzionali
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                    {
                        gesture.EdgeSwipeTop = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                    {
                        gesture.EdgeSwipeBottom = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.LeftArrow))
                    {
                        gesture.EdgeSwipeLeft = true;
                    }
                    else if (Input.GetKeyDown(KeyCode.RightArrow))
                    {
                        gesture.EdgeSwipeRight = true;
                    }
                }
            }
            #endif
            
            // Aggiorna il componente gesti
            state.EntityManager.SetComponentData(_gestureEntity, gesture);
        }
    }
    
    /// <summary>
    /// Tag per il singleton del sistema di riconoscimento gesti
    /// </summary>
    public struct GestureRecognitionSingleton : IComponentData
    {
    }
    
    /// <summary>
    /// Componente che contiene lo stato dei gesti riconosciuti
    /// </summary>
    public struct GestureComponent : IComponentData
    {
        // Gesti multitouch
        public bool PinchIn;               // Pinch per zoom out
        public bool PinchOut;              // Pinch per zoom in
        public bool Rotate;                // Rotazione con due dita
        public float RotationAngle;        // Angolo di rotazione in radianti
        
        // Altri gesti
        public bool LongPress;             // Pressione prolungata
        public float2 LongPressPosition;   // Posizione del long press
        
        // Edge swipe (swipe che iniziano o terminano ai bordi dello schermo)
        public bool EdgeSwipeTop;          // Swipe verso/dal bordo superiore
        public bool EdgeSwipeBottom;       // Swipe verso/dal bordo inferiore
        public bool EdgeSwipeLeft;         // Swipe verso/dal bordo sinistro
        public bool EdgeSwipeRight;        // Swipe verso/dal bordo destro
    }
}
