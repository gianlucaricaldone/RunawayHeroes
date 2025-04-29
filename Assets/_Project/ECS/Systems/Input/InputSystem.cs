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
    public partial class InputSystem : SystemBase
    {
        private EntityQuery _inputQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query per le entità
        /// </summary>
        protected override void OnCreate()
        {
            // Definisce quali entità processare (giocatori con un componente di input)
            _inputQuery = GetEntityQuery(
                ComponentType.ReadWrite<InputComponent>(),
                ComponentType.ReadOnly<TagComponent>()
            );
            
            // Richiede che ci siano entità con input per aggiornare questo sistema
            RequireForUpdate(_inputQuery);
        }
        
        /// <summary>
        /// Aggiorna gli input ad ogni frame
        /// </summary>
        protected override void OnUpdate()
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
            
            // Aggiorna i componenti di input per tutte le entità giocatore
            Entities
                .WithName("UpdatePlayerInput")
                .WithAll<TagComponent>()
                .ForEach((ref InputComponent input) =>
                {
                    // Aggiorna i vari input
                    input.JumpPressed = jumpKey;
                    input.SlidePressed = slideKey;
                    input.FocusTimePressed = focusTimeKey;
                    input.AbilityPressed = abilityKey;
                    input.CharacterSwitchPressed = switchCharacterKey;
                    input.IsMovementEnabled = isMovementEnabled;
                    
                    // Aggiorna il movimento laterale
                    input.LateralMovement = lateralInput;
                    input.MoveDirection = moveDirection;
                    
                    // Aggiorna lo stato del touch
                    input.TouchActive = touchActive;
                    input.TouchPosition = touchPosition;
                    input.TouchDuration = touchDuration;
                    
                }).ScheduleParallel();
        }
    }
}