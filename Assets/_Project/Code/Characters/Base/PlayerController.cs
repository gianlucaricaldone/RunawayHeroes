using System;
using System.Collections;
using UnityEngine;
using RunawayHeroes.Core.Tutorial;
using RunawayHeroes.Items;
using RunawayHeroes.Manager;

namespace RunawayHeroes.Characters
{
    /// <summary>
    /// Controlla il movimento e le azioni del giocatore. Gestisce gli input, la fisica
    /// e l'interazione con l'ambiente di gioco. Supporta controlli touch e tastiera/mouse.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Character Reference")]
        [SerializeField] private CharacterType characterType = CharacterType.Alex;
        [SerializeField] private GameObject characterModel;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Transform ceilingCheck;

        [Header("Movement Settings")]
        [SerializeField] private float runSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float slideSpeed = 8f;
        [SerializeField] private float slideDuration = 1f;
        [SerializeField] private float lateralMoveSpeed = 5f;
        [SerializeField] private int availableLanes = 3;
        [SerializeField] private float laneWidth = 2f;
        
        [Header("Special Movement")]
        [SerializeField] private float wallRunDuration = 1.5f;
        [SerializeField] private float wallRunCooldown = 3f;
        [SerializeField] private float grindSpeed = 7f;
        [SerializeField] private float grindDuration = 2f;

        [Header("Special Abilities")]
        [SerializeField] private float specialAbilityCooldown = 15f;
        [SerializeField] private float urbanDashDuration = 2f;
        [SerializeField] private float urbanDashSpeed = 10f;
        [SerializeField] private float urbanDashInvincibilityDuration = 2f;
        [SerializeField] private GameObject urbanDashVFX;

        [Header("Physics Settings")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private float obstacleCheckDistance = 1f;

        [Header("Health & Damage")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float invincibilityDuration = 1f;
        [SerializeField] private GameObject damageVFX;
        [SerializeField] private AudioClip damageSFX;
        [SerializeField] private AudioClip deathSFX;

        [Header("FX")]
        [SerializeField] private ParticleSystem runDustFX;
        [SerializeField] private ParticleSystem jumpFX;
        [SerializeField] private ParticleSystem landFX;
        [SerializeField] private ParticleSystem slideFX;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip jumpSFX;
        [SerializeField] private AudioClip slideSFX;
        [SerializeField] private AudioClip specialAbilitySFX;

        [Header("Tutorial Control")]
        [SerializeField] private bool _canJump = true;
        [SerializeField] private bool _canSlide = true;
        [SerializeField] private bool _canMoveHorizontal = true;
        [SerializeField] private bool _isAutoRunning = false;

        // Proprietà per il Tutorial
        public bool canJump
        {
            get { return _canJump; }
            set { _canJump = value; }
        }

        public bool canSlide
        {
            get { return _canSlide; }
            set { _canSlide = value; }
        }

        public bool canMoveHorizontal
        {
            get { return _canMoveHorizontal; }
            set { _canMoveHorizontal = value; }
        }

        public bool isAutoRunning
        {
            get { return _isAutoRunning; }
            set { _isAutoRunning = value; }
        }

        // Events
        public event Action OnJump;
        public event Action OnLand;
        public event Action OnSlide;
        public event Action OnSlideEnd;
        public event Action OnLaneChange;
        public event Action OnSpecialAbility;
        public event Action<float, Vector3> OnDamaged;
        public event Action OnDeath;
        public event Action<int> OnHealthChanged;
        public event Action<CollectibleType, int> OnCollectiblePickup;

        // Properties
        public int CurrentHealth { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsGrounded { get; private set; }
        public bool IsSliding { get; private set; }
        public bool IsWallRunning { get; private set; }
        public bool IsGrinding { get; private set; }
        public bool IsUsingSpecialAbility { get; private set; }
        public float SpecialAbilityCooldownRemaining { get; private set; }
        public int CurrentLane { get; private set; }
        public CharacterType CurrentCharacter => characterType;
        public bool CanJump => IsGrounded && !IsSliding && canJump;
        public bool CanSlide => IsGrounded && !IsSliding && canSlide;
        public bool CanUseSpecialAbility => SpecialAbilityCooldownRemaining <= 0 && !IsUsingSpecialAbility;

        // Private variables
        private CharacterController characterController;
        private Vector3 moveDirection = Vector3.zero;
        private Vector3 targetLanePosition;
        private bool isMovingLaterally = false;
        private float slideTimeRemaining = 0f;
        private float wallRunTimeRemaining = 0f;
        private float wallRunCooldownRemaining = 0f;
        private float grindTimeRemaining = 0f;
        private float invincibilityTimeRemaining = 0f;
        private float specialAbilityTimeRemaining = 0f;
        private bool isDead = false;
        private bool isGamePaused = false;

        // Touch input tracking
        private Vector2 touchStartPosition;
        private bool isTouchTracking = false;
        private float swipeThreshold = 50f;

        // Animation parameter hashes (for performance)
        private readonly int animIsGrounded = Animator.StringToHash("isGrounded");
        private readonly int animIsSliding = Animator.StringToHash("isSliding");
        private readonly int animIsWallRunning = Animator.StringToHash("isWallRunning");
        private readonly int animIsGrinding = Animator.StringToHash("isGrinding");
        private readonly int animJump = Animator.StringToHash("jump");
        private readonly int animSpeed = Animator.StringToHash("speed");
        private readonly int animSpecialAbility = Animator.StringToHash("specialAbility");
        private readonly int animDamaged = Animator.StringToHash("damaged");
        private readonly int animDeath = Animator.StringToHash("death");

        private void Awake()
        {
            // Get components
            characterController = GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Initialize health
            CurrentHealth = maxHealth;

            // Initialize lane position
            CurrentLane = availableLanes / 2; // Start in middle lane
            targetLanePosition = new Vector3(GetLanePosition(CurrentLane), transform.position.y, transform.position.z);
            transform.position = new Vector3(GetLanePosition(CurrentLane), transform.position.y, transform.position.z);
        }

        private void Start()
        {
            // Register with GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused += OnGamePaused;
                GameManager.Instance.OnGameResumed += OnGameResumed;
            }

            // Activate character model based on type
            SetupCharacterModel();
        }

        private void Update()
        {
            if (isDead || isGamePaused)
                return;

            // Check if grounded
            CheckGroundStatus();

            // Handle cooldowns
            UpdateCooldowns();

            // Handle input or auto-running
            if (isAutoRunning)
            {
                // In modalità auto-running, gestisce solo l'avanzamento automatico
                // Input per salto, scivolata e movimenti laterali sono ancora possibili
                HandleInput();
            }
            else
            {
                // Modalità normale con input completo
                HandleInput();
            }

            // Apply gravity
            ApplyGravity();

            // Handle lane positioning
            HandleLanePosition();

            // Move character
            MoveCharacter();

            // Update animations
            UpdateAnimations();
        }

        private void OnDestroy()
        {
            // Unregister from GameManager if available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGamePaused -= OnGamePaused;
                GameManager.Instance.OnGameResumed -= OnGameResumed;
            }
        }

        #region Input Handling
        /// <summary>
        /// Gestisce gli input del giocatore, sia da tastiera che touch
        /// </summary>
        private void HandleInput()
        {
            // Keyboard input
            if (Input.GetButtonDown("Jump") && CanJump)
            {
                Jump();
            }

            if (Input.GetButtonDown("Slide") && CanSlide)
            {
                StartSlide();
            }

            if (canMoveHorizontal)
            {
                if (Input.GetButtonDown("Left") && !isMovingLaterally && CurrentLane > 0)
                {
                    MoveLane(-1);
                }
                else if (Input.GetButtonDown("Right") && !isMovingLaterally && CurrentLane < availableLanes - 1)
                {
                    MoveLane(1);
                }
            }

            if (Input.GetButtonDown("Ability") && CanUseSpecialAbility)
            {
                ActivateSpecialAbility();
            }

            // Touch input
            HandleTouchInput();
        }

        /// <summary>
        /// Gestisce gli input touch per dispositivi mobili
        /// </summary>
        private void HandleTouchInput()
        {
            if (Input.touchCount <= 0)
                return;

            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    isTouchTracking = true;
                    break;

                case TouchPhase.Moved:
                    // Process swipes while moving
                    if (isTouchTracking)
                    {
                        Vector2 swipeDelta = touch.position - touchStartPosition;

                        // Check for vertical swipe (jump or slide)
                        if (Mathf.Abs(swipeDelta.y) > swipeThreshold && Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
                        {
                            isTouchTracking = false; // Prevent multiple swipe detections

                            if (swipeDelta.y > 0 && CanJump) // Swipe up = Jump
                            {
                                Jump();
                            }
                            else if (swipeDelta.y < 0 && CanSlide) // Swipe down = Slide
                            {
                                StartSlide();
                            }
                        }
                        // Check for horizontal swipe (lane change)
                        else if (canMoveHorizontal && Mathf.Abs(swipeDelta.x) > swipeThreshold && Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y) && !isMovingLaterally)
                        {
                            isTouchTracking = false; // Prevent multiple swipe detections

                            if (swipeDelta.x < 0 && CurrentLane > 0) // Swipe left
                            {
                                MoveLane(-1);
                            }
                            else if (swipeDelta.x > 0 && CurrentLane < availableLanes - 1) // Swipe right
                            {
                                MoveLane(1);
                            }
                        }
                    }
                    break;

                case TouchPhase.Ended:
                    // Check for tap (special ability)
                    if (isTouchTracking && Vector2.Distance(touch.position, touchStartPosition) < swipeThreshold)
                    {
                        // Double tap detection would be added here
                        // For simplicity, long press for special ability:
                        if (touch.tapCount >= 2 && CanUseSpecialAbility)
                        {
                            ActivateSpecialAbility();
                        }
                    }
                    
                    isTouchTracking = false;
                    break;
            }
        }
        #endregion

        #region Movement Methods
        /// <summary>
        /// Fa saltare il personaggio
        /// </summary>
        public void Jump()
        {
            if (!CanJump)
                return;

            moveDirection.y = jumpForce;
            
            // Play animation
            animator.SetTrigger(animJump);
            
            // Play effects
            if (jumpFX != null)
                jumpFX.Play();
                
            if (audioSource != null && jumpSFX != null)
                audioSource.PlayOneShot(jumpSFX);

            // Trigger event
            OnJump?.Invoke();
        }

        /// <summary>
        /// Inizia una scivolata
        /// </summary>
        public void StartSlide()
        {
            if (!CanSlide)
                return;

            IsSliding = true;
            slideTimeRemaining = slideDuration;
            
            // Reduce character controller height for slide
            float originalHeight = characterController.height;
            characterController.height = originalHeight / 2;
            
            // Adjust center to keep feet at same position
            Vector3 newCenter = characterController.center;
            newCenter.y = -originalHeight / 4;
            characterController.center = newCenter;
            
            // Play animation
            animator.SetBool(animIsSliding, true);
            
            // Play effects
            if (slideFX != null)
                slideFX.Play();
                
            if (audioSource != null && slideSFX != null)
                audioSource.PlayOneShot(slideSFX);

            // Trigger event
            OnSlide?.Invoke();
        }

        /// <summary>
        /// Termina una scivolata
        /// </summary>
        private void EndSlide()
        {
            if (!IsSliding)
                return;

            IsSliding = false;
            
            // Restore character controller height
            characterController.height = 2f; // Assuming default height is 2
            characterController.center = Vector3.up; // Assuming default center is (0, 1, 0)
            
            // Update animation
            animator.SetBool(animIsSliding, false);
            
            // Stop effects
            if (slideFX != null && slideFX.isPlaying)
                slideFX.Stop();

            // Trigger event
            OnSlideEnd?.Invoke();
        }

        /// <summary>
        /// Cambia la corsia del personaggio
        /// </summary>
        public void MoveLane(int direction)
        {
            if (isMovingLaterally || !canMoveHorizontal)
                return;

            int targetLane = CurrentLane + direction;
            
            // Validate target lane
            if (targetLane < 0 || targetLane >= availableLanes)
                return;

            CurrentLane = targetLane;
            targetLanePosition.x = GetLanePosition(CurrentLane);
            isMovingLaterally = true;
            
            // Trigger event
            OnLaneChange?.Invoke();
        }

        /// <summary>
        /// Gestisce il posizionamento sulle corsie
        /// </summary>
        private void HandleLanePosition()
        {
            if (!isMovingLaterally)
                return;

            // Move smoothly towards target lane
            Vector3 currentPos = transform.position;
            float newX = Mathf.MoveTowards(currentPos.x, targetLanePosition.x, lateralMoveSpeed * Time.deltaTime);
            
            // Check if we've reached target lane
            if (Mathf.Approximately(newX, targetLanePosition.x))
            {
                isMovingLaterally = false;
            }
            
            transform.position = new Vector3(newX, currentPos.y, currentPos.z);
        }

        /// <summary>
        /// Muove il personaggio in avanti
        /// </summary>
        private void MoveCharacter()
        {
            // Determine current speed
            float currentSpeed = isAutoRunning ? runSpeed : 0f;
            
            if (IsSliding)
                currentSpeed = slideSpeed;
            else if (IsWallRunning)
                currentSpeed = runSpeed * 1.2f; // Wall run boost
            else if (IsGrinding)
                currentSpeed = grindSpeed;
            else if (IsUsingSpecialAbility && characterType == CharacterType.Alex)
                currentSpeed = urbanDashSpeed;

            // Forward movement
            moveDirection.z = currentSpeed;
            
            // Apply movement
            characterController.Move(moveDirection * Time.deltaTime);
            
            // Update animator
            animator.SetFloat(animSpeed, currentSpeed / runSpeed);
            
            // Play run dust effect when grounded and moving
            if (IsGrounded && !IsSliding && !IsWallRunning && !IsGrinding && runDustFX != null && currentSpeed > 0)
            {
                if (!runDustFX.isPlaying)
                    runDustFX.Play();
            }
            else if (runDustFX != null && runDustFX.isPlaying)
            {
                runDustFX.Stop();
            }
        }

        /// <summary>
        /// Applica la gravità al movimento
        /// </summary>
        private void ApplyGravity()
        {
            if (IsGrounded && moveDirection.y < 0)
            {
                moveDirection.y = -2f; // Small negative value to ensure grounded status
            }
            else
            {
                moveDirection.y += gravity * Time.deltaTime;
            }
        }

        /// <summary>
        /// Attiva l'abilità speciale del personaggio
        /// </summary>
        public void ActivateSpecialAbility()
        {
            if (!CanUseSpecialAbility)
                return;

            IsUsingSpecialAbility = true;
            specialAbilityTimeRemaining = GetSpecialAbilityDuration();
            SpecialAbilityCooldownRemaining = specialAbilityCooldown;
            
            // Play animation
            animator.SetTrigger(animSpecialAbility);
            
            // Play effects
            if (audioSource != null && specialAbilitySFX != null)
                audioSource.PlayOneShot(specialAbilitySFX);

            // Handle character-specific ability
            switch (characterType)
            {
                case CharacterType.Alex:
                    StartUrbanDash();
                    break;
                case CharacterType.Maya:
                    // Nature Call implementation would go here
                    break;
                case CharacterType.Kai:
                    // Heat Aura implementation would go here
                    break;
                case CharacterType.Ember:
                    // Fireproof Body implementation would go here
                    break;
                case CharacterType.Marina:
                    // Air Bubble implementation would go here
                    break;
                case CharacterType.Neo:
                    // Controlled Glitch implementation would go here
                    break;
            }
            
            // Trigger event
            OnSpecialAbility?.Invoke();
        }

        /// <summary>
        /// Implementa l'abilità "Scatto Urbano" di Alex
        /// </summary>
        private void StartUrbanDash()
        {
            // Make invincible during urban dash
            SetInvincibility(urbanDashInvincibilityDuration);
            
            // Spawn VFX
            if (urbanDashVFX != null)
            {
                GameObject vfx = Instantiate(urbanDashVFX, transform.position, Quaternion.identity);
                vfx.transform.parent = transform;
                Destroy(vfx, urbanDashDuration);
            }
        }

        /// <summary>
        /// Ottiene la durata dell'abilità speciale in base al tipo di personaggio
        /// </summary>
        private float GetSpecialAbilityDuration()
        {
            switch (characterType)
            {
                case CharacterType.Alex:
                    return urbanDashDuration;
                case CharacterType.Maya:
                    return 5f; // Nature Call duration
                case CharacterType.Kai:
                    return 8f; // Heat Aura duration
                case CharacterType.Ember:
                    return 3f; // Fireproof Body duration
                case CharacterType.Marina:
                    return 6f; // Air Bubble duration
                case CharacterType.Neo:
                    return 3f; // Controlled Glitch duration
                default:
                    return 2f;
            }
        }
        #endregion

        #region Collision & Physics
        /// <summary>
        /// Verifica se il personaggio è a terra
        /// </summary>
        private void CheckGroundStatus()
        {
            // Previous ground state
            bool wasGrounded = IsGrounded;
            
            // Check if grounded using sphere cast
            IsGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);
            
            // Update animator
            animator.SetBool(animIsGrounded, IsGrounded);
            
            // Handle landing
            if (IsGrounded && !wasGrounded)
            {
                // Play landing effects
                if (landFX != null)
                    landFX.Play();
                
                // Trigger landing event
                OnLand?.Invoke();
            }
        }

        /// <summary>
        /// Verifica se c'è un ostacolo davanti
        /// </summary>
        private bool IsObstacleAhead()
        {
            return Physics.Raycast(transform.position, transform.forward, obstacleCheckDistance, obstacleLayer);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Handle obstacle collisions
            if (obstacleLayer == (obstacleLayer | (1 << hit.gameObject.layer)))
            {
                HandleObstacleCollision(hit);
            }
            
            // Check for collectible
            Collectible collectible = hit.gameObject.GetComponent<Collectible>();
            if (collectible != null)
            {
                PickupCollectible(collectible);
            }
            
            // Check for special surfaces
            if (hit.gameObject.CompareTag("Wall") && !IsWallRunning && wallRunCooldownRemaining <= 0)
            {
                StartWallRun();
            }
            else if (hit.gameObject.CompareTag("Grind") && !IsGrinding)
            {
                StartGrind();
            }
        }

        /// <summary>
        /// Gestisce la collisione con un ostacolo
        /// </summary>
        private void HandleObstacleCollision(ControllerColliderHit hit)
        {
            // If invincible or using Alex's special ability, ignore collision
            if (IsInvincible || (IsUsingSpecialAbility && characterType == CharacterType.Alex))
            {
                // Small boost when breaking through obstacles with dash
                if (IsUsingSpecialAbility && characterType == CharacterType.Alex)
                {
                    // Break small obstacles
                    BreakableObstacle breakable = hit.gameObject.GetComponent<BreakableObstacle>();
                    if (breakable != null)
                    {
                        breakable.Break();
                    }
                }
                return;
            }

            // Calculate damage based on obstacle and speed
            float damage = 10f; // Default damage
            
            // Apply damage
            TakeDamage(damage, hit.point);
        }

        /// <summary>
        /// Raccoglie un collezionabile
        /// </summary>
        private void PickupCollectible(Collectible collectible)
        {
            // Get collectible info
            CollectibleType type = collectible.GetCollectibleType();
            int value = collectible.GetValue();
            
            // Trigger collectible pickup event
            OnCollectiblePickup?.Invoke(type, value);
            
            // Process collectible based on type
            switch (type)
            {
                case CollectibleType.Coin:
                    // Add to currency
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddCurrency(value);
                    }
                    break;
                    
                case CollectibleType.Gem:
                    // Add to premium currency or score
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddScore(value * 50);
                    }
                    break;
                    
                case CollectibleType.HealthKit:
                    // Heal player
                    Heal(value);
                    break;
                    
                case CollectibleType.SpeedBoost:
                    // Apply speed boost temporarily
                    StartCoroutine(ApplySpeedBoost(value, 5f));
                    break;
                    
                case CollectibleType.Shield:
                    // Apply invincibility
                    SetInvincibility(value);
                    break;
                    
                case CollectibleType.Fragment:
                    // Collect fragment piece
                    // This would interface with a Fragment system
                    break;
            }
            
            // Destroy collectible
            collectible.Collect();
        }

        /// <summary>
        /// Inizia una corsa sul muro
        /// </summary>
        private void StartWallRun()
        {
            if (IsWallRunning || IsSliding || wallRunCooldownRemaining > 0)
                return;

            IsWallRunning = true;
            wallRunTimeRemaining = wallRunDuration;
            
            // Reduce gravity effect during wall run
            gravity *= 0.3f;
            
            // Update animation
            animator.SetBool(animIsWallRunning, true);
        }

        /// <summary>
        /// Termina una corsa sul muro
        /// </summary>
        private void EndWallRun()
        {
            if (!IsWallRunning)
                return;

            IsWallRunning = false;
            wallRunCooldownRemaining = wallRunCooldown;
            
            // Restore normal gravity
            gravity = -20f;
            
            // Update animation
            animator.SetBool(animIsWallRunning, false);
        }

        /// <summary>
        /// Inizia uno scorrimento su ringhiera
        /// </summary>
        private void StartGrind()
        {
            if (IsGrinding || IsSliding)
                return;

            IsGrinding = true;
            grindTimeRemaining = grindDuration;
            
            // Update animation
            animator.SetBool(animIsGrinding, true);
        }

        /// <summary>
        /// Termina uno scorrimento su ringhiera
        /// </summary>
        private void EndGrind()
        {
            if (!IsGrinding)
                return;

            IsGrinding = false;
            
            // Update animation
            animator.SetBool(animIsGrinding, false);
        }
        #endregion

        #region Health & Damage
        /// <summary>
        /// Causa danno al personaggio
        /// </summary>
        public void TakeDamage(float amount, Vector3 damageSource)
        {
            if (IsInvincible || isDead)
                return;

            // Apply damage
            CurrentHealth -= Mathf.RoundToInt(amount);
            
            // Clamp health
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            
            // Update UI
            OnHealthChanged?.Invoke(CurrentHealth);
            
            // Apply invincibility frames
            SetInvincibility(invincibilityDuration);
            
            // Play damaged animation
            animator.SetTrigger(animDamaged);
            
            // Play effects
            if (damageVFX != null)
            {
                GameObject vfx = Instantiate(damageVFX, transform.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
            
            if (audioSource != null && damageSFX != null)
            {
                audioSource.PlayOneShot(damageSFX);
            }
            
            // Trigger event
            OnDamaged?.Invoke(amount, damageSource);
            
            // Check for death
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        /// <summary>
        /// Cura il personaggio
        /// </summary>
        public void Heal(int amount)
        {
            if (isDead)
                return;

            CurrentHealth += amount;
            
            // Clamp health
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);
            
            // Update UI
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        /// <summary>
        /// Rende il personaggio invincibile per un determinato periodo
        /// </summary>
        public void SetInvincibility(float duration)
        {
            IsInvincible = true;
            invincibilityTimeRemaining = duration;
        }

        /// <summary>
        /// Applica un boost di velocità temporaneo
        /// </summary>
        private IEnumerator ApplySpeedBoost(float multiplier, float duration)
        {
            float originalSpeed = runSpeed;
            runSpeed *= multiplier;
            
            yield return new WaitForSeconds(duration);
            
            runSpeed = originalSpeed;
        }

        /// <summary>
        /// Gestisce la morte del personaggio
        /// </summary>
        private void Die()
        {
            if (isDead)
                return;

            isDead = true;
            
            // Play death animation
            animator.SetTrigger(animDeath);
            
            // Play effects
            if (audioSource != null && deathSFX != null)
            {
                audioSource.PlayOneShot(deathSFX);
            }
            
            // Disable character controller
            characterController.enabled = false;
            
            // Trigger event
            OnDeath?.Invoke();
            
            // Notify GameManager
            if (GameManager.Instance != null)
            {
                // Create level result with failure
                LevelResult result = new LevelResult
                {
                    playerWon = false,
                    score = 0,
                    completionTime = GameManager.Instance.GameTime
                };
                
                // Report to GameManager after short delay
                StartCoroutine(ReportDeathAfterDelay(result, 2f));
            }
        }

        private IEnumerator ReportDeathAfterDelay(LevelResult result, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel(result);
            }
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// Aggiorna i cooldown delle abilità e degli stati
        /// </summary>
        private void UpdateCooldowns()
        {
            // Slide timer
            if (IsSliding)
            {
                slideTimeRemaining -= Time.deltaTime;
                if (slideTimeRemaining <= 0)
                {
                    EndSlide();
                }
            }

            // Wall run timer
            if (IsWallRunning)
            {
                wallRunTimeRemaining -= Time.deltaTime;
                if (wallRunTimeRemaining <= 0)
                {
                    EndWallRun();
                }
            }

            // Wall run cooldown
            if (wallRunCooldownRemaining > 0)
            {
                wallRunCooldownRemaining -= Time.deltaTime;
            }

            // Grind timer
            if (IsGrinding)
            {
                grindTimeRemaining -= Time.deltaTime;
                if (grindTimeRemaining <= 0)
                {
                    EndGrind();
                }
            }

            // Invincibility timer
            if (IsInvincible)
            {
                invincibilityTimeRemaining -= Time.deltaTime;
                if (invincibilityTimeRemaining <= 0)
                {
                    IsInvincible = false;
                }
            }

            // Special ability timer and cooldown
            if (IsUsingSpecialAbility)
            {
                specialAbilityTimeRemaining -= Time.deltaTime;
                if (specialAbilityTimeRemaining <= 0)
                {
                    IsUsingSpecialAbility = false;
                }
            }

            if (SpecialAbilityCooldownRemaining > 0)
            {
                SpecialAbilityCooldownRemaining -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Aggiorna le animazioni del personaggio
        /// </summary>
        private void UpdateAnimations()
        {
            // Most animations are handled by animator parameters set throughout the code
            
            // Handle invincibility visual feedback if needed
            if (IsInvincible && characterModel != null)
            {
                // For example, blink the character model
                float blinkSpeed = 10f;
                float visibility = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                
                // Get all renderers
                Renderer[] renderers = characterModel.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    // Skip particle system renderers
                    if (renderer.GetComponent<ParticleSystem>() != null)
                        continue;
                    
                    Color color = renderer.material.color;
                    color.a = Mathf.Lerp(0.5f, 1f, visibility);
                    renderer.material.color = color;
                }
            }
        }

        /// <summary>
        /// Calcola la posizione X della corsia
        /// </summary>
        private float GetLanePosition(int lane)
        {
            float centerX = 0f;
            float offset = -((availableLanes - 1) * laneWidth / 2f);
            return centerX + offset + (lane * laneWidth);
        }

        /// <summary>
        /// Imposta il modello del personaggio in base al tipo
        /// </summary>
        private void SetupCharacterModel()
        {
            // This would be expanded to load the appropriate character model based on characterType
            // For now, just ensure the model we have is active
            if (characterModel != null)
            {
                characterModel.SetActive(true);
            }
        }

        /// <summary>
        /// Gestisce la pausa del gioco
        /// </summary>
        private void OnGamePaused()
        {
            isGamePaused = true;
        }

        /// <summary>
        /// Gestisce la ripresa del gioco dalla pausa
        /// </summary>
        private void OnGameResumed()
        {
            isGamePaused = false;
        }

        /// <summary>
        /// Determina se il personaggio è attualmente in stato di salto
        /// </summary>
        /// <returns>True se il personaggio sta saltando, false altrimenti</returns>
        public bool IsJumping()
        {
            // Un personaggio è considerato in salto se:
            // 1. Non è a terra (IsGrounded è false)
            // 2. Ha una velocità verticale positiva (sta andando verso l'alto)
            // 3. Non sta scivolando, correndo su un muro o facendo grinding
            
            // Controlla se il personaggio sta andando verso l'alto (component Y della velocità positiva)
            bool isMovingUpward = moveDirection.y > 0.1f;
            
            // Un personaggio non sta saltando se è a terra, sta scivolando, correndo su un muro o facendo grinding
            bool isNotJumping = IsGrounded || IsSliding || IsWallRunning || IsGrinding;
            
            // Se il personaggio sta usando l'abilità speciale di Urban Dash con boost verticale, 
            // potrebbe essere considerato in salto anche se tecnicamente è un dash
            bool isUrbanDashJumping = IsUsingSpecialAbility && characterType == CharacterType.Alex 
                                    && moveDirection.y > 0f;
            
            // Un personaggio sta saltando se si sta muovendo verso l'alto e non è in nessuno degli stati che impediscono il salto
            return (isMovingUpward && !isNotJumping) || isUrbanDashJumping;
        }
        #endregion
    }

    /// <summary>
    /// Tipi di personaggi disponibili
    /// </summary>
    public enum CharacterType
    {
        Alex,
        Maya,
        Kai,
        Ember,
        Marina,
        Neo
    }

    /// <summary>
    /// Tipi di collezionabili
    /// </summary>
    public enum CollectibleType
    {
        Coin,
        Gem,
        HealthKit,
        SpeedBoost,
        Shield,
        Fragment
    }

    /// <summary>
    /// Interfaccia di base per i collezionabili
    /// </summary>
    public interface Collectible
    {
        CollectibleType GetCollectibleType();
        int GetValue();
        void Collect();
    }

    /// <summary>
    /// Classe per gli ostacoli distruttibili
    /// </summary>
    public class BreakableObstacle : MonoBehaviour
    {
        public void Break()
        {
            // Implementation would include:
            // - Playing break animation/particle effects
            // - Playing sound effect
            // - Potentially spawning debris
            // - Disabling collider
            // - Eventually destroying the object

            GetComponent<Collider>().enabled = false;
            
            // Example implementation:
            ParticleSystem breakFX = GetComponent<ParticleSystem>();
            if (breakFX != null)
            {
                breakFX.Play();
            }
            
            // Disable mesh
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
            
            // Destroy after delay
            Destroy(gameObject, 2f);
        }
    }

}