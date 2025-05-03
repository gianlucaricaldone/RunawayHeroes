using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.ECS.Systems.Input
{
    /// <summary>
    /// Sistema che gestisce l'input del giocatore, elaborando i comandi
    /// da tastiera, touch o controller e aggiornando l'InputComponent.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct InputSystem : ISystem
    {
        private EntityQuery _inputQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query per le entità
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Definisce quali entità processare (giocatori con un componente di input)
            _inputQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<InputComponent, TagComponent>()
                .Build(ref state);
            
            // Richiede che ci siano entità con input per aggiornare questo sistema
            state.RequireForUpdate(_inputQuery);
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Pulizia delle risorse se necessario
        }
        
        /// <summary>
        /// Aggiorna gli input ad ogni frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            // Ottiene il deltaTime per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Input da tastiera/controller (per debug/sviluppo principalmente)
            bool jumpKey = UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);
            bool slideKey = UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S);
            bool focusTimeKey = UnityEngine.Input.GetKeyDown(KeyCode.F) || UnityEngine.Input.GetKey(KeyCode.LeftShift);
            bool abilityKey = UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetKeyDown(KeyCode.RightShift);
            bool switchCharacterKey = UnityEngine.Input.GetKeyDown(KeyCode.Q) || UnityEngine.Input.GetKeyDown(KeyCode.Tab);
            
            // Per default, il movimento è abilitato
            bool isMovementEnabled = true;
            
            // Input laterale da tastiera
            float lateralInput = 0f;
            if (UnityEngine.Input.GetKey(KeyCode.LeftArrow) || UnityEngine.Input.GetKey(KeyCode.A))
                lateralInput -= 1.0f;
            if (UnityEngine.Input.GetKey(KeyCode.RightArrow) || UnityEngine.Input.GetKey(KeyCode.D))
                lateralInput += 1.0f;
            
            // Calcola la direzione di movimento normalizzata
            float2 moveDirection = new float2(lateralInput, 1.0f);
            if (math.lengthsq(moveDirection) > 0.01f)
            {
                moveDirection = math.normalize(moveDirection);
            }
            
            // Input da touch (per mobile)
            bool touchActive = UnityEngine.Input.touchCount > 0;
            float2 touchPosition = float2.zero;
            float touchDuration = 0f;
            
            if (touchActive)
            {
                Touch touch = UnityEngine.Input.GetTouch(0);
                touchPosition = new float2(touch.position.x, touch.position.y);
                
                // Calcola la durata del tocco
                if (touch.phase == TouchPhase.Began)
                {
                    touchDuration = 0f;
                }
                else if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
                {
                    touchDuration += deltaTime;
                }
                
                // Se non c'è input laterale da tastiera, controlla lo swipe laterale
                if (math.abs(lateralInput) < 0.01f && touch.phase == TouchPhase.Moved)
                {
                    // Calcola lo swipe laterale
                    float swipeDeltaX = touch.deltaPosition.x;
                    const float SWIPE_THRESHOLD = 10.0f;
                    
                    if (math.abs(swipeDeltaX) > SWIPE_THRESHOLD)
                    {
                        lateralInput = math.sign(swipeDeltaX);
                        moveDirection.x = lateralInput;
                        if (math.lengthsq(moveDirection) > 0.01f)
                        {
                            moveDirection = math.normalize(moveDirection);
                        }
                    }
                }
                
                // Rileva swipe verso il basso per sliding
                if (touch.phase == TouchPhase.Moved && !slideKey)
                {
                    float swipeDeltaY = touch.deltaPosition.y;
                    const float SWIPE_DOWN_THRESHOLD = -30.0f; // Negativo per swipe verso il basso
                    
                    if (swipeDeltaY < SWIPE_DOWN_THRESHOLD)
                    {
                        slideKey = true;
                    }
                }
                
                // Rileva tap per salto
                if (touch.phase == TouchPhase.Ended && touchDuration < 0.2f && math.abs(touch.deltaPosition.x) < 20f 
                    && math.abs(touch.deltaPosition.y) < 20f && !jumpKey)
                {
                    jumpKey = true;
                }
                
                // Rileva tocco prolungato per Focus Time
                if (touchDuration > 0.5f && touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled 
                    && !focusTimeKey)
                {
                    focusTimeKey = true;
                }
            }
            
            // Usa un job IJobEntity per aggiornare tutti i componenti di input
            state.Dependency = new UpdatePlayerInputJob 
            {
                JumpPressed = jumpKey,
                SlidePressed = slideKey,
                FocusTimePressed = focusTimeKey,
                AbilityPressed = abilityKey,
                CharacterSwitchPressed = switchCharacterKey,
                IsMovementEnabled = isMovementEnabled,
                LateralMovement = lateralInput,
                MoveDirection = moveDirection,
                TouchActive = touchActive,
                TouchPosition = touchPosition,
                TouchDuration = touchDuration
            }.ScheduleParallel(_inputQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per aggiornare gli input del giocatore
    /// </summary>
    public partial struct UpdatePlayerInputJob : IJobEntity
    {
        // Input da tastiera/controller
        public bool JumpPressed;
        public bool SlidePressed;
        public bool FocusTimePressed;
        public bool AbilityPressed;
        public bool CharacterSwitchPressed;
        public bool IsMovementEnabled;
        
        // Input di movimento
        public float LateralMovement;
        public float2 MoveDirection;
        
        // Input da touch
        public bool TouchActive;
        public float2 TouchPosition;
        public float TouchDuration;
        
        /// <summary>
        /// Aggiorna il componente di input per ogni entità giocatore
        /// </summary>
        public void Execute(ref InputComponent input)
        {
            // Aggiorna i vari input
            input.JumpPressed = JumpPressed;
            input.SlidePressed = SlidePressed;
            input.FocusTimePressed = FocusTimePressed;
            input.AbilityPressed = AbilityPressed;
            input.CharacterSwitchPressed = CharacterSwitchPressed;
            input.IsMovementEnabled = IsMovementEnabled;
            
            // Aggiorna il movimento laterale
            input.LateralMovement = LateralMovement;
            input.MoveDirection = MoveDirection;
            
            // Aggiorna lo stato del touch
            input.TouchActive = TouchActive;
            input.TouchPosition = TouchPosition;
            input.TouchDuration = TouchDuration;
        }
    }
}