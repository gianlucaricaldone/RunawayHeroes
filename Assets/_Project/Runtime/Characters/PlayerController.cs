using UnityEngine;
using Unity.Entities;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.Runtime.Characters
{
    /// <summary>
    /// Controllore del personaggio giocatore che funziona sia in gioco normale
    /// che nel tutorial
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [Tooltip("Velocità base di movimento in avanti")]
        public float forwardSpeed = 10f;
        
        [Tooltip("Velocità di accelerazione all'inizio della corsa")]
        public float acceleration = 5f;
        
        [Tooltip("Velocità massima raggiungibile")]
        public float maxSpeed = 20f;
        
        [Tooltip("Velocità di movimento laterale")]
        public float lateralSpeed = 8f;
        
        [Header("Salto")]
        [Tooltip("Forza del salto")]
        public float jumpForce = 12f;
        
        [Tooltip("Gravità personalizzata")]
        public float gravity = 25f;
        
        [Tooltip("Salti massimi consecutivi")]
        public int maxJumps = 1;
        
        [Header("Scivolata")]
        [Tooltip("Durata della scivolata in secondi")]
        public float slideDuration = 1.0f;
        
        [Tooltip("Riduzione dell'altezza durante la scivolata")]
        public float slideHeightReduction = 0.5f;
        
        [Header("Effetti Visivi")]
        [Tooltip("Effetto particellare per la corsa")]
        public ParticleSystem runningEffect;
        
        [Tooltip("Effetto particellare per il salto")]
        public ParticleSystem jumpEffect;
        
        [Tooltip("Effetto particellare per la scivolata")]
        public ParticleSystem slideEffect;
        
        [Header("Animazione")]
        [Tooltip("Animator per controllare le animazioni")]
        public Animator animator;
        
        // Riferimenti privati
        private CharacterController _controller;
        private Entity _playerEntity;
        private EntityManager _entityManager;
        
        // Stato del movimento
        private Vector3 _moveDirection = Vector3.zero;
        private float _currentSpeed;
        private bool _isJumping = false;
        private bool _isSliding = false;
        private float _slideTimer = 0f;
        private int _jumpCount = 0;
        private float _defaultHeight;
        private float _slidingHeight;
        private Vector3 _defaultCenter;
        private Vector3 _slidingCenter;
        
        // Stato del tutorial
        private bool _isTutorial = true;
        
        // Parametri Animator
        private readonly int _animIsRunning = Animator.StringToHash("IsRunning");
        private readonly int _animIsJumping = Animator.StringToHash("IsJumping");
        private readonly int _animIsSliding = Animator.StringToHash("IsSliding");
        private readonly int _animRunSpeed = Animator.StringToHash("RunSpeed");
        
        private void Awake()
        {
            // Ottieni i componenti
            _controller = GetComponent<CharacterController>();
            _defaultHeight = _controller.height;
            _slidingHeight = _defaultHeight * slideHeightReduction;
            _defaultCenter = _controller.center;
            _slidingCenter = new Vector3(_defaultCenter.x, _defaultCenter.y * slideHeightReduction, _defaultCenter.z);
            
            // Se non abbiamo un animator, vediamo se c'è sui figli
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
            
            // Inizializza velocità con un valore basso
            _currentSpeed = forwardSpeed * 0.5f;
            
            // Ottieni il riferimento al mondo ECS
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                _entityManager = world.EntityManager;
                
                // Crea un'entità per il player se non esiste
                CreatePlayerEntity();
            }
        }
        
        private void Start()
        {
            // Verifica se siamo in una modalità tutorial
            _isTutorial = IsTutorialMode();
            
            // Se siamo nel tutorial, parte più lentamente
            if (_isTutorial)
            {
                _currentSpeed = forwardSpeed * 0.3f;
            }
        }
        
        private void Update()
        {
            HandleInput();
            ApplyMovement();
            UpdateAnimation();
            UpdateEffects();
            
            // Aggiorna l'entità ECS se esiste
            if (_entityManager != null && _entityManager.Exists(_playerEntity))
            {
                UpdatePlayerEntity();
            }
        }
        
        /// <summary>
        /// Gestisce l'input del giocatore
        /// </summary>
        private void HandleInput()
        {
            // Input per movimento laterale
            float horizontalInput = Input.GetAxis("Horizontal");
            
            // Accelerazione progressiva
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, maxSpeed, acceleration * Time.deltaTime);
            
            // Gestione salto
            bool jumpRequest = Input.GetButtonDown("Jump");
            if (jumpRequest && _jumpCount < maxJumps)
            {
                // Inizia un salto
                _isJumping = true;
                _jumpCount++;
                _moveDirection.y = jumpForce;
                
                // Attiva effetto salto
                if (jumpEffect != null)
                {
                    jumpEffect.Play();
                }
            }
            
            // Gestione scivolata
            bool slideRequest = Input.GetButtonDown("Slide") || Input.GetKeyDown(KeyCode.DownArrow);
            if (slideRequest && !_isSliding && _controller.isGrounded)
            {
                // Inizia scivolata
                _isSliding = true;
                _slideTimer = slideDuration;
                
                // Modifica collider
                _controller.height = _slidingHeight;
                _controller.center = _slidingCenter;
                
                // Attiva effetto scivolata
                if (slideEffect != null)
                {
                    slideEffect.Play();
                }
            }
            
            // Aggiorna timer scivolata
            if (_isSliding)
            {
                _slideTimer -= Time.deltaTime;
                if (_slideTimer <= 0f)
                {
                    // Termina scivolata
                    _isSliding = false;
                    _controller.height = _defaultHeight;
                    _controller.center = _defaultCenter;
                    
                    // Ferma effetto scivolata
                    if (slideEffect != null)
                    {
                        slideEffect.Stop();
                    }
                }
            }
            
            // Direzione del movimento
            _moveDirection.x = horizontalInput * lateralSpeed;
            _moveDirection.z = _currentSpeed;
        }
        
        /// <summary>
        /// Applica il movimento al character controller
        /// </summary>
        private void ApplyMovement()
        {
            // Applica la gravità se non si è a contatto con il terreno
            if (!_controller.isGrounded)
            {
                _moveDirection.y -= gravity * Time.deltaTime;
            }
            else if (_moveDirection.y < 0)
            {
                _moveDirection.y = -0.5f; // Piccola forza verso il basso quando si è a terra
                
                // Reset del salto quando si tocca terra
                if (_isJumping)
                {
                    _isJumping = false;
                    _jumpCount = 0;
                }
            }
            
            // Muove il character controller
            _controller.Move(_moveDirection * Time.deltaTime);
        }
        
        /// <summary>
        /// Aggiorna le animazioni del personaggio
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator == null)
                return;
                
            // Imposta i parametri dell'animator
            animator.SetBool(_animIsRunning, _controller.isGrounded && !_isSliding);
            animator.SetBool(_animIsJumping, _isJumping);
            animator.SetBool(_animIsSliding, _isSliding);
            animator.SetFloat(_animRunSpeed, _currentSpeed / maxSpeed);
        }
        
        /// <summary>
        /// Aggiorna gli effetti particellari
        /// </summary>
        private void UpdateEffects()
        {
            // Effetto corsa
            if (runningEffect != null)
            {
                if (_controller.isGrounded && !_isJumping && !_isSliding)
                {
                    if (!runningEffect.isPlaying)
                    {
                        runningEffect.Play();
                    }
                }
                else
                {
                    if (runningEffect.isPlaying)
                    {
                        runningEffect.Stop();
                    }
                }
            }
        }
        
        /// <summary>
        /// Crea un'entità ECS per il player
        /// </summary>
        private void CreatePlayerEntity()
        {
            // Verifica se l'entità del giocatore esiste già
            var playerQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>());
            if (playerQuery.CalculateEntityCount() > 0)
            {
                _playerEntity = playerQuery.GetSingletonEntity();
                return;
            }
            
            // Crea una nuova entità per il giocatore
            _playerEntity = _entityManager.CreateEntity(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadWrite<PlayerMovementComponent>(),
                ComponentType.ReadWrite<TransformComponent>()
            );
            
            // Inizializza i componenti
            _entityManager.SetComponentData(_playerEntity, new PlayerMovementComponent
            {
                ForwardSpeed = forwardSpeed,
                LateralSpeed = lateralSpeed,
                IsJumping = false,
                IsSliding = false
            });
            
            _entityManager.SetComponentData(_playerEntity, new TransformComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            });
            
            Debug.Log("Player entity created in ECS world");
        }
        
        /// <summary>
        /// Aggiorna l'entità ECS del giocatore
        /// </summary>
        private void UpdatePlayerEntity()
        {
            // Aggiorna il componente movimento
            _entityManager.SetComponentData(_playerEntity, new PlayerMovementComponent
            {
                ForwardSpeed = _currentSpeed,
                LateralSpeed = lateralSpeed,
                IsJumping = _isJumping,
                IsSliding = _isSliding
            });
            
            // Aggiorna il componente transform
            _entityManager.SetComponentData(_playerEntity, new TransformComponent
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale
            });
        }
        
        /// <summary>
        /// Verifica se ci troviamo nella modalità tutorial
        /// </summary>
        private bool IsTutorialMode()
        {
            if (_entityManager == null)
                return false;
                
            // Cerca entità con tag TutorialLevelTag
            var tutorialQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<TutorialLevelTag>());
            return tutorialQuery.CalculateEntityCount() > 0;
        }
        
        /// <summary>
        /// Imposta un personaggio specifico
        /// </summary>
        public void SetCharacterId(int characterId)
        {
            // In un gioco reale, qui si configurerebbero le caratteristiche specifiche del personaggio
            Debug.Log($"Setting character ID to {characterId}");
            
            // Per ora, modifichiamo solo alcune caratteristiche in base all'ID (per esempio)
            switch (characterId)
            {
                case 0: // Alex (default, bilanciato)
                    forwardSpeed = 10f;
                    jumpForce = 12f;
                    maxJumps = 1;
                    break;
                case 1: // Maya (veloce ma salti bassi)
                    forwardSpeed = 12f;
                    jumpForce = 10f;
                    maxJumps = 1;
                    break;
                case 2: // Kai (lento ma salti alti)
                    forwardSpeed = 8f;
                    jumpForce = 15f;
                    maxJumps = 2;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Componente per il movimento del giocatore nell'ECS
    /// </summary>
    public struct PlayerMovementComponent : IComponentData
    {
        public float ForwardSpeed;
        public float LateralSpeed;
        public bool IsJumping;
        public bool IsSliding;
    }
    
    /// <summary>
    /// Componente per la trasformazione nell'ECS
    /// </summary>
    public struct TransformComponent : IComponentData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
}