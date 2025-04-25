using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RunawayHeroes
{
    /// <summary>
    /// Implementazione completa di Alex, il corriere urbano con il Frammento Urbano.
    /// Contiene tutte le abilità e meccaniche di movimento urbano che Alex utilizzerà nel gioco.
    /// </summary>
    public class AlexCharacter : CharacterBase
    {
        #region Alex Specific Properties

        [Header("Alex - Urban Dash Settings")]
        [SerializeField] private float urbanDashDistance = 10f;
        [SerializeField] private float urbanDashSpeed = 30f;
        [SerializeField] private float urbanDashInvulnerabilityDuration = 2f;
        [SerializeField] private bool canBreakObstacles = true;
        [SerializeField] private LayerMask breakableObstaclesLayer;
        [SerializeField] private float breakableObstaclesRadius = 0.5f;
        [SerializeField] private float breakableObstaclesImpulse = 10f;
        [SerializeField] private float urbanDashCooldown = 15f;

        [Header("Alex - Parkour Settings")]
        [SerializeField] private float wallRunDuration = 1.5f;
        [SerializeField] private float wallRunSpeed = 12f;
        [SerializeField] private float wallRunCooldown = 1f;
        [SerializeField] private float wallDetectionDistance = 0.5f;
        [SerializeField] private LayerMask wallRunLayers;
        [SerializeField] private float wallJumpForce = 12f;
        [SerializeField] private float wallJumpUpwardForce = 8f;

        [Header("Alex - Grinding Settings")]
        [SerializeField] private float grindRailSnapDistance = 1f;
        [SerializeField] private float grindRailSpeed = 15f;
        [SerializeField] private LayerMask grindRailLayers;
        [SerializeField] private float grindJumpForce = 10f;
        [SerializeField] private float grindBalance = 10f; // Higher = easier to maintain balance
        [SerializeField] private float maxGrindTilt = 30f; // Maximum tilt while grinding

        [Header("Alex - Vaulting Settings")]
        [SerializeField] private float vaultDetectionDistance = 1.2f;
        [SerializeField] private float vaultHeight = 1.5f;
        [SerializeField] private float vaultSpeed = 8f;
        [SerializeField] private float vaultForwardBoost = 4f;
        [SerializeField] private LayerMask vaultableLayers;

        [Header("Alex - Wall Jump Settings")]
        [SerializeField] private float wallJumpDetectionDistance = 0.5f;
        [SerializeField] private float consecutiveWallJumpWindow = 0.8f;
        [SerializeField] private int maxConsecutiveWallJumps = 3;
        [SerializeField] private LayerMask wallJumpLayers;

        [Header("Alex - Urban Fragment Effects")]
        [SerializeField] private Color fragmentColor = new Color(0, 0.5f, 1f); // Blue electric
        [SerializeField] private Color purifiedFragmentColor = new Color(0.2f, 0.7f, 1f); // Brighter blue
        [SerializeField] private GameObject dashTrailEffect;
        [SerializeField] private ParticleSystem dashParticles;
        [SerializeField] private ParticleSystem wallRunParticles;
        [SerializeField] private ParticleSystem grindParticles;
        [SerializeField] private ParticleSystem vaultParticles;
        [SerializeField] private ParticleSystem fragmentPurificationParticles;
        [SerializeField] private GameObject electricAuraEffect;
        
        [Header("Alex - Advanced Movement")]
        [SerializeField] private float doubleJumpForce = 8f;
        [SerializeField] private bool hasDoubleJump = false; // Unlocked after purification
        [SerializeField] private float airControlMultiplier = 0.5f;
        [SerializeField] private float airDashMultiplier = 0.7f; // Reduced effectiveness in air
        [SerializeField] private float slideTurnSpeedMultiplier = 0.6f; // Reduced turning while sliding
        [SerializeField] private float quickTurnThreshold = 135f; // Angle for quick turn animation
        [SerializeField] private float quickTurnDuration = 0.2f;

        [Header("Alex - Audio")]
        [SerializeField] private AudioClip urbanDashSound;
        [SerializeField] private AudioClip wallRunSound;
        [SerializeField] private AudioClip wallJumpSound;
        [SerializeField] private AudioClip grindRailSound;
        [SerializeField] private AudioClip vaultSound;
        [SerializeField] private AudioClip doubleJumpSound;
        [SerializeField] private AudioClip[] footstepSoundsConcrete;
        [SerializeField] private AudioClip[] footstepSoundsMetal;
        [SerializeField] private AudioClip[] landingSounds;
        [SerializeField] private AudioClip fragmentPurificationSound;
        [SerializeField] private AudioClip quickTurnSound;

        // Private movement state variables
        private bool isWallRunning = false;
        private bool isGrinding = false;
        private bool isVaulting = false;
        private bool isDoubleJumping = false;
        private bool hasDoubleJumped = false;
        private bool isQuickTurning = false;
        private float currentWallRunCooldown = 0f;
        private int consecutiveWallJumps = 0;
        private float lastWallJumpTime = -10f;
        private float grindBalanceAmount = 0f; // Current balance (-1 to 1)
        private float lastGroundedTime = 0f;
        private float coyoteTime = 0.15f; // Time after leaving a surface where jump is still possible
        private float jumpBufferTime = 0.12f; // Time to buffer a jump input before landing
        private float jumpBufferCounter = 0f;
        private Vector3 lastMoveDirection;
        private Vector3 lastWallNormal;
        private EnvironmentSurface currentSurface = EnvironmentSurface.Concrete;

        // Rail grinding
        private Transform currentGrindRail;
        private float grindStartTime;
        private Vector3 grindStartPosition;
        private float currentGrindRotation = 0f;

        // Wall running
        private Vector3 wallNormal;
        private Transform currentWallRunSurface;
        private float wallRunTimer = 0f;

        // Vaulting
        private Transform currentVaultObstacle;
        private Vector3 vaultStartPosition;
        private Vector3 vaultTargetPosition;

        // Fragment state
        private bool urbanDashEnhanced = false;
        private float originalGravity;

        // Advanced movement
        private bool canWallRun = true;
        private bool canGrind = true;
        private bool canVault = true;
        private bool canWallJump = true;

        // Reference to terminal hacking
        private TerminalHackingSystem hackingSystem;

        #endregion

        #region Unity Lifecycle Methods

        protected override void Awake()
        {
            base.Awake();
            
            // Set Alex-specific properties
            characterName = "Alex";
            characterType = CharacterType.Alex;
            fragmentType = FragmentType.Urban;
            characterDescription = "Un abile corriere urbano con capacità di parkour, connesso al Frammento Urbano.";
            
            // Store original values
            originalGravity = gravity;

            // Set initial values
            currentMovementSpeed = baseMovementSpeed;
            
            // Initialize hacking system
            hackingSystem = GetComponent<TerminalHackingSystem>();
            if (hackingSystem == null)
            {
                // Add component if missing
                hackingSystem = gameObject.AddComponent<TerminalHackingSystem>();
            }
        }

        protected override void Start()
        {
            base.Start();

            // Initialize Alex-specific visuals
            if (fragmentVisual != null)
            {
                // Set fragment color
                Renderer fragmentRenderer = fragmentVisual.GetComponent<Renderer>();
                if (fragmentRenderer != null)
                {
                    fragmentRenderer.material.SetColor("_EmissionColor", fragmentColor * 2f);
                }
            }

            // Setup effects initial state
            if (dashTrailEffect != null) dashTrailEffect.SetActive(false);
            if (electricAuraEffect != null) electricAuraEffect.SetActive(false);
        }

        protected override void Update()
        {
            base.Update();

            if (isDead || !canMove) return;

            // Handle cooldowns
            HandleSpecificCooldowns();
            
            // Coyote time calculation
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
                hasDoubleJumped = false;
            }
            
            // Jump buffer counter
            if (jumpBufferCounter > 0)
            {
                jumpBufferCounter -= Time.deltaTime;
                
                // Execute jump if grounded or wall running
                if ((Time.time - lastGroundedTime <= coyoteTime || isWallRunning) && !isJumping)
                {
                    if (isWallRunning)
                    {
                        WallJump();
                    }
                    else
                    {
                        Jump();
                    }
                    jumpBufferCounter = 0;
                }
            }

            // Check for potential parkour opportunities
            if (!isWallRunning && !isJumping && !isGrinding && currentWallRunCooldown <= 0 && canWallRun)
            {
                CheckForWallRun();
            }

            // Check for potential grind rails when grounded
            if (!isGrinding && !isWallRunning && !isVaulting && isGrounded && canGrind)
            {
                CheckForGrindRail();
            }
            
            // Check for vaultable obstacles when moving forward
            if (!isVaulting && !isWallRunning && !isGrinding && movementDirection.magnitude > 0.1f && canVault)
            {
                CheckForVaultableObstacle();
            }
            
            // Check for wall jumps when in air
            if (!isGrounded && !isWallRunning && !isGrinding && !isVaulting && canWallJump)
            {
                CheckForWallJump();
            }
            
            // Handle grinding balance
            if (isGrinding)
            {
                UpdateGrindingBalance();
            }
            
            // Update quick turn
            if (isQuickTurning)
            {
                UpdateQuickTurn();
            }
            
            // Store last movement direction
            if (movementDirection.magnitude > 0.1f)
            {
                lastMoveDirection = movementDirection.normalized;
            }

            // Update animations
            UpdateAnimationStates();
            
            // Update surface type based on ground raycast
            if (isGrounded && Time.time - lastGroundedTime < 0.1f)
            {
                UpdateCurrentSurface();
            }
        }

        protected override void FixedUpdate()
        {
            if (isDead || !canMove) return;

            // Handle different movement states
            if (isWallRunning)
            {
                ApplyWallRunMovement();
            }
            else if (isGrinding)
            {
                ApplyGrindMovement();
            }
            else if (isVaulting)
            {
                ApplyVaultMovement();
            }
            else if (isQuickTurning)
            {
                // Movement is handled in UpdateQuickTurn
            }
            else
            {
                // Normal movement
                base.FixedUpdate();
            }
        }

        #endregion

        #region Movement Implementation

        protected override void ApplyMovement()
        {
            // Implementation for normal movement
            if (controller != null)
            {
                // Apply gravity
                if (!isGrounded)
                {
                    velocity.y += gravity * Time.fixedDeltaTime;
                }
                else if (velocity.y < 0)
                {
                    velocity.y = -2f; // Small negative value to ensure grounding
                }

                // Calculate movement
                Vector3 moveDirection = movementDirection;
                
                // Apply air control multiplier when in air
                if (!isGrounded && !isWallRunning && !isGrinding && !isVaulting)
                {
                    moveDirection *= airControlMultiplier;
                }
                
                Vector3 move = transform.right * moveDirection.x + transform.forward * moveDirection.z;
                controller.Move(move * currentMovementSpeed * Time.fixedDeltaTime);

                // Apply vertical velocity (gravity/jump)
                controller.Move(velocity * Time.fixedDeltaTime);
            }
            else if (rb != null)
            {
                // Rigidbody-based movement
                Vector3 targetVelocity;
                
                // Apply air control when not grounded
                if (!isGrounded && !isWallRunning && !isGrinding && !isVaulting)
                {
                    targetVelocity = new Vector3(movementDirection.x * airControlMultiplier, 0, 
                        movementDirection.z * airControlMultiplier) * currentMovementSpeed;
                }
                else
                {
                    targetVelocity = new Vector3(movementDirection.x, 0, movementDirection.z) * currentMovementSpeed;
                }
                
                Vector3 velocityChange = targetVelocity - new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }

            // Rotate character to face movement direction
            if (movementDirection.magnitude > 0.1f)
            {
                // Check for quick turn
                if (lastMoveDirection.magnitude > 0.1f)
                {
                    float angle = Vector3.Angle(lastMoveDirection, movementDirection);
                    if (angle > quickTurnThreshold && !isQuickTurning && isGrounded)
                    {
                        StartQuickTurn();
                    }
                }
                
                // Normal rotation - reduced while sliding
                float rotationSpeed = isSliding ? 5f * slideTurnSpeedMultiplier : 15f;
                transform.forward = Vector3.Slerp(transform.forward, movementDirection.normalized, rotationSpeed * Time.fixedDeltaTime);
            }
        }

        protected override void CheckGrounded()
        {
            if (controller != null)
            {
                // Sphere cast to check ground
                isGrounded = Physics.CheckSphere(
                    transform.position - new Vector3(0, 0.1f, 0), 
                    0.5f, 
                    groundLayer
                );

                // Reset jump state if grounded
                if (isGrounded && velocity.y < 0)
                {
                    isJumping = false;
                    animator?.SetBool("Jumping", false);
                    
                    // Play landing sound if falling from a height
                    if (velocity.y < -5f)
                    {
                        PlayLandingSound();
                    }
                }
            }
            else if (rb != null)
            {
                // Raycast to check ground
                isGrounded = Physics.Raycast(
                    transform.position, 
                    Vector3.down, 
                    1.1f, 
                    groundLayer
                );

                // Reset jump state if grounded
                if (isGrounded && rb.velocity.y < 0.1f)
                {
                    isJumping = false;
                    animator?.SetBool("Jumping", false);
                    
                    // Play landing sound if falling from a height
                    if (rb.velocity.y < -5f)
                    {
                        PlayLandingSound();
                    }
                }
            }
        }

        protected override void AdjustColliderForSlide(bool isSliding)
        {
            // Adjust collider height for sliding
            if (controller != null)
            {
                // Store original height if starting slide
                float originalHeight = isSliding ? controller.height : 2f; // Assuming 2 is default height
                float targetHeight = isSliding ? originalHeight * 0.5f : originalHeight;
                
                controller.height = targetHeight;
                
                // Adjust center to maintain ground contact
                Vector3 center = controller.center;
                center.y = isSliding ? -0.5f : 0f;
                controller.center = center;
            }
            else if (rb != null)
            {
                // For Rigidbody setup, adjust capsule collider
                CapsuleCollider capsule = GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    float originalHeight = isSliding ? capsule.height : 2f;
                    float targetHeight = isSliding ? originalHeight * 0.5f : originalHeight;
                    
                    capsule.height = targetHeight;
                    
                    // Adjust center to maintain ground contact
                    Vector3 center = capsule.center;
                    center.y = isSliding ? -0.5f : 0f;
                    capsule.center = center;
                }
            }
        }

        public override void Jump()
        {
            // If already in the air and has double jump
            if (!isGrounded && !isWallRunning && !isGrinding && hasDoubleJump && !hasDoubleJumped)
            {
                PerformDoubleJump();
                return;
            }
            
            // Normal jump logic with coyote time consideration
            if (isJumping || isSliding) return;
            
            // Check if within coyote time
            bool canCoyoteJump = Time.time - lastGroundedTime <= coyoteTime;
            
            if (!isGrounded && !canCoyoteJump && !isWallRunning && !isGrinding)
            {
                // Buffer the jump for a short time
                jumpBufferCounter = jumpBufferTime;
                return;
            }
            
            // Execute the jump
            isJumping = true;
            
            // Stop other movement states
            if (isGrinding) StopGrinding();
            if (isWallRunning) StopWallRun();
            
            // Play animation
            if (animator != null)
            {
                animator.SetTrigger("Jump");
                animator.SetBool("Jumping", true);
            }
            
            // Apply jump force
            if (controller != null)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            else if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            
            // Play jump sound based on surface
            PlayJumpSound();
        }

        /// <summary>
        /// Performs a double jump in mid-air
        /// </summary>
        private void PerformDoubleJump()
        {
            if (hasDoubleJumped) return;
            
            hasDoubleJumped = true;
            isDoubleJumping = true;
            
            // Apply double jump force
            if (controller != null)
            {
                velocity.y = Mathf.Sqrt(doubleJumpForce * -2f * gravity);
            }
            else if (rb != null)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.Impulse);
            }
            
            // Play animation
            if (animator != null)
            {
                animator.SetTrigger("DoubleJump");
            }
            
            // Play sound
            if (doubleJumpSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(doubleJumpSound);
            }
            
            // Visual effect
            if (dashParticles != null)
            {
                ParticleSystem.MainModule main = dashParticles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(fragmentColor);
                dashParticles.Play();
            }
            
            // Reset after short delay
            StartCoroutine(ResetDoubleJumpState());
        }
        
        private IEnumerator ResetDoubleJumpState()
        {
            yield return new WaitForSeconds(0.3f);
            isDoubleJumping = false;
        }

        private void HandleSpecificCooldowns()
        {
            // Handle Wall Run cooldown
            if (currentWallRunCooldown > 0)
            {
                currentWallRunCooldown -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Updates the current surface type for footstep sounds and effects
        /// </summary>
        private void UpdateCurrentSurface()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 1.2f, groundLayer))
            {
                // Check for surface tag or material
                string surfaceTag = hit.collider.tag;
                
                if (surfaceTag == "Metal" || surfaceTag == "MetalSurface")
                {
                    currentSurface = EnvironmentSurface.Metal;
                }
                else if (surfaceTag == "Concrete" || surfaceTag == "ConcreteSurface")
                {
                    currentSurface = EnvironmentSurface.Concrete;
                }
                else
                {
                    // Default to concrete
                    currentSurface = EnvironmentSurface.Concrete;
                }
            }
        }

        /// <summary>
        /// Plays appropriate jump sound based on current surface
        /// </summary>
        private void PlayJumpSound()
        {
            if (characterAudio == null) return;
            
            AudioClip jumpSound = null;
            
            // Select sound based on surface
            switch (currentSurface)
            {
                case EnvironmentSurface.Concrete:
                    if (footstepSoundsConcrete != null && footstepSoundsConcrete.Length > 0)
                    {
                        jumpSound = footstepSoundsConcrete[Random.Range(0, footstepSoundsConcrete.Length)];
                    }
                    break;
                case EnvironmentSurface.Metal:
                    if (footstepSoundsMetal != null && footstepSoundsMetal.Length > 0)
                    {
                        jumpSound = footstepSoundsMetal[Random.Range(0, footstepSoundsMetal.Length)];
                    }
                    break;
            }
            
            if (jumpSound != null)
            {
                characterAudio.PlayOneShot(jumpSound, 0.7f);
            }
        }
        
        /// <summary>
        /// Plays appropriate landing sound based on current surface
        /// </summary>
        private void PlayLandingSound()
        {
            if (characterAudio == null || landingSounds == null || landingSounds.Length == 0) return;
            
            AudioClip landSound = landingSounds[Random.Range(0, landingSounds.Length)];
            if (landSound != null)
            {
                characterAudio.PlayOneShot(landSound);
            }
        }

        #endregion

        #region Urban Dash (Special Ability)

        /// <summary>
        /// Activates Alex's special ability: Urban Dash
        /// </summary>
        public override void ActivateSpecialAbility()
        {
            if (isDead || currentSpecialAbilityCooldown > 0 || isSpecialAbilityActive) return;
            
            // Call base implementation for common functionality
            base.ActivateSpecialAbility();
            
            // Start dash coroutine
            StartCoroutine(UrbanDashRoutine());
        }

        /// <summary>
        /// Routine for Urban Dash ability
        /// </summary>
        private IEnumerator UrbanDashRoutine()
        {
            // Calculate dash direction (use current forward direction if no input)
            Vector3 dashDirection = movementDirection.magnitude > 0.1f ? 
                movementDirection.normalized : transform.forward;
            
            // Calculate dash parameters, adjusted if in air
            float currentDashDistance = urbanDashDistance;
            float currentDashSpeed = urbanDashSpeed;
            
            if (!isGrounded)
            {
                currentDashDistance *= airDashMultiplier;
                currentDashSpeed *= airDashMultiplier;
            }
            
            // Setup dash
            float dashDuration = currentDashDistance / currentDashSpeed;
            float startTime = Time.time;
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = startPosition + dashDirection * currentDashDistance;
            
            // Activate dash effects
            ActivateDashEffects(true);
            
            // Make invulnerable during dash
            isInvulnerable = true;
            
            // Dash movement
            while (Time.time < startTime + dashDuration)
            {
                float progress = (Time.time - startTime) / dashDuration;
                
                // Apply easing for smoother motion
                float easedProgress = EaseInOutQuad(progress);
                
                // Update position
                transform.position = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                
                // Check for breakable obstacles
                if (canBreakObstacles)
                {
                    BreakObstaclesInPath();
                }
                
                yield return null;
            }
            
            // Ensure final position
            transform.position = targetPosition;
            
            // Maintain invulnerability for the set duration
            float remainingInvulnerability = urbanDashInvulnerabilityDuration - dashDuration;
            if (remainingInvulnerability > 0)
            {
                yield return new WaitForSeconds(remainingInvulnerability);
            }
            
            // Deactivate dash effects and states
            ActivateDashEffects(false);
            isInvulnerable = false;
            DeactivateSpecialAbility();
        }

        /// <summary>
        /// Easing function for smoother dash movement
        /// </summary>
        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
        }

        /// <summary>
        /// Activates visual and audio effects for Urban Dash
        /// </summary>
        private void ActivateDashEffects(bool activate)
        {
            // Visual effects
            if (dashTrailEffect != null)
            {
                dashTrailEffect.SetActive(activate);
            }
            
            if (dashParticles != null)
            {
                ParticleSystem.MainModule main = dashParticles.main;
                main.startColor = new ParticleSystem.MinMaxGradient(isFragmentPurified ? purifiedFragmentColor : fragmentColor);
                
                if (activate)
                {
                    dashParticles.Play();
                }
                else
                {
                    dashParticles.Stop();
                }
            }
            
            // Audio effect
            if (activate && urbanDashSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(urbanDashSound);
            }
            
            // Animation
            if (animator != null)
            {
                animator.SetBool("UrbanDash", activate);
            }
        }

        /// <summary>
        /// Checks for and breaks obstacles in the dash path
        /// </summary>
        private void BreakObstaclesInPath()
        {
            Collider[] obstacles = Physics.OverlapSphere(
                transform.position, 
                breakableObstaclesRadius, 
                breakableObstaclesLayer
            );
            
            foreach (Collider obstacle in obstacles)
            {
                // Get the breakable component
                BreakableObstacle breakable = obstacle.GetComponent<BreakableObstacle>();
                if (breakable != null)
                {
                    // Apply enhanced damage if fragment is purified
                    float damageMultiplier = isFragmentPurified ? 1.5f : 1f;
                    
                    // Apply damage with dash force
                    Vector3 force = transform.forward * breakableObstaclesImpulse * damageMultiplier;
                    breakable.ApplyDamage(100f * damageMultiplier, force);
                }
                else
                {
                    // Simple physics impulse if no breakable component
                    Rigidbody rb = obstacle.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(transform.forward * breakableObstaclesImpulse, ForceMode.Impulse);
                    }
                }
            }
        }

        #endregion

        #region Wall Running

        /// <summary>
        /// Checks if wall running is possible
        /// </summary>
        private void CheckForWallRun()
        {
            // Need to be moving and not grounded
            if (movementDirection.magnitude < 0.1f || isGrounded) return;
            
            // Cast rays to left and right to find walls
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, transform.right, out hitInfo, wallDetectionDistance, wallRunLayers))
            {
                StartWallRun(hitInfo.normal, hitInfo.transform);
                return;
            }
            else if (Physics.Raycast(transform.position, -transform.right, out hitInfo, wallDetectionDistance, wallRunLayers))
            {
                StartWallRun(hitInfo.normal, hitInfo.transform);
                return;
            }
        }

        /// <summary>
        /// Starts wall running
        /// </summary>
        private void StartWallRun(Vector3 normal, Transform wall)
        {
            isWallRunning = true;
            wallNormal = normal;
            currentWallRunSurface = wall;
            gravity = 0; // Disable gravity during wall run
            wallRunTimer = 0f;
            
            // Start wall run timeout
            StartCoroutine(WallRunTimeout());
            
            // Play sound effect
            if (wallRunSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(wallRunSound);
            }
            
            // Play particle effect
            if (wallRunParticles != null)
            {
                wallRunParticles.transform.position = transform.position - wallNormal * 0.5f;
                wallRunParticles.transform.forward = Vector3.Cross(wallNormal, Vector3.up);
                wallRunParticles.Play();
            }
            
            // Play animation
            if (animator != null)
            {
                // Determine if left or right wall
                bool isRightWall = Vector3.Dot(wallNormal, -transform.right) > 0;
                animator.SetBool("WallRunning", true);
                animator.SetBool("WallRunRight", isRightWall);
                animator.SetBool("WallRunLeft", !isRightWall);
            }
        }

        /// <summary>
        /// Stops wall running
        /// </summary>
        private void StopWallRun()
        {
            if (!isWallRunning) return;
            
            isWallRunning = false;
            gravity = originalGravity; // Restore gravity
            currentWallRunCooldown = wallRunCooldown;
            lastWallNormal = wallNormal; // Store for wall jumps
            currentWallRunSurface = null;
            
            // Stop particle effect
            if (wallRunParticles != null)
            {
                wallRunParticles.Stop();
            }
            
            // Update animation
            if (animator != null)
            {
                animator.SetBool("WallRunning", false);
                animator.SetBool("WallRunRight", false);
                animator.SetBool("WallRunLeft", false);
            }
        }

        /// <summary>
        /// Wall run timeout routine
        /// </summary>
        private IEnumerator WallRunTimeout()
        {
            while (isWallRunning && wallRunTimer < wallRunDuration)
            {
                wallRunTimer += Time.deltaTime;
                
                // Gradually apply gravity as wall run progresses (for natural fall at end)
                if (wallRunTimer > wallRunDuration * 0.7f)
                {
                    float gravityFactor = Mathf.InverseLerp(wallRunDuration * 0.7f, wallRunDuration, wallRunTimer);
                    gravity = originalGravity * gravityFactor;
                }
                
                yield return null;
            }
            
            if (isWallRunning)
            {
                StopWallRun();
            }
        }

        /// <summary>
        /// Apply wall run movement
        /// </summary>
        private void ApplyWallRunMovement()
        {
            // Calculate wall run direction (along the wall)
            Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
            
            // Ensure correct direction based on player input
            if (Vector3.Dot(wallForward, transform.forward) < 0)
            {
                wallForward = -wallForward;
            }
            
            // Get movement input for acceleration/deceleration along wall
            float forwardInput = Vector3.Dot(movementDirection, transform.forward);
            float speedMultiplier = Mathf.Lerp(0.7f, 1.2f, Mathf.Max(0, forwardInput));
            
            // Move along the wall
            if (controller != null)
            {
                controller.Move(wallForward * wallRunSpeed * speedMultiplier * Time.fixedDeltaTime);
                
                // Apply gravity if being introduced in the later stages
                if (gravity != 0)
                {
                    velocity.y += gravity * Time.fixedDeltaTime;
                    controller.Move(new Vector3(0, velocity.y, 0) * Time.fixedDeltaTime);
                }
            }
            else if (rb != null)
            {
                rb.velocity = new Vector3(wallForward.x * wallRunSpeed * speedMultiplier, 
                    rb.velocity.y, wallForward.z * wallRunSpeed * speedMultiplier);
                
                // Apply gravity if being introduced
                if (gravity != 0)
                {
                    rb.AddForce(new Vector3(0, gravity, 0), ForceMode.Acceleration);
                }
            }
            
            // Rotate character to face along the wall
            transform.forward = Vector3.Slerp(transform.forward, wallForward, 15f * Time.fixedDeltaTime);
            
            // Check if still near wall
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, -wallNormal, out hit, wallDetectionDistance * 1.5f, wallRunLayers))
            {
                // No wall detected, stop wall running
                StopWallRun();
            }
            else if (hit.transform != currentWallRunSurface)
            {
                // Different wall surface, update reference
                currentWallRunSurface = hit.transform;
                wallNormal = hit.normal;
            }
        }

        /// <summary>
        /// Jump while wall running (with higher force and direction away from wall)
        /// </summary>
        public void WallJump()
        {
            if (!isWallRunning) return;
            
            // Reset consecutive wall jumps if time between jumps is too large
            if (Time.time - lastWallJumpTime > consecutiveWallJumpWindow)
            {
                consecutiveWallJumps = 0;
            }
            
            // Increment consecutive jumps
            consecutiveWallJumps++;
            lastWallJumpTime = Time.time;
            
            // Calculate jump direction (away from wall and up)
            Vector3 jumpDirection = (wallNormal + Vector3.up).normalized;
            
            // Adjust jump force based on consecutive jumps (slight reduction for balance)
            float adjustedWallJumpForce = wallJumpForce * Mathf.Pow(0.95f, consecutiveWallJumps - 1);
            float adjustedUpwardForce = wallJumpUpwardForce * Mathf.Pow(0.9f, consecutiveWallJumps - 1);
            
            // Stop wall running
            StopWallRun();
            
            // Apply jump force
            if (controller != null)
            {
                velocity = new Vector3(jumpDirection.x * adjustedWallJumpForce, adjustedUpwardForce, 
                    jumpDirection.z * adjustedWallJumpForce);
            }
            else if (rb != null)
            {
                rb.velocity = Vector3.zero; // Clear current velocity
                rb.AddForce(new Vector3(jumpDirection.x * adjustedWallJumpForce, adjustedUpwardForce, 
                    jumpDirection.z * adjustedWallJumpForce), ForceMode.Impulse);
            }
            
            // Play jump animation
            if (animator != null)
            {
                animator.SetTrigger("WallJump");
                animator.SetBool("Jumping", true);
            }
            
            // Play sound
            if (wallJumpSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(wallJumpSound);
            }
            
            isJumping = true;
        }

        /// <summary>
        /// Check for potential wall jumps when in air
        /// </summary>
        private void CheckForWallJump()
        {
            // Only check if has consecutive jumps left and within time window
            if (consecutiveWallJumps >= maxConsecutiveWallJumps || 
                Time.time - lastWallJumpTime > consecutiveWallJumpWindow)
            {
                return;
            }
            
            // Cast rays to find walls
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, transform.right, out hitInfo, wallJumpDetectionDistance, wallJumpLayers))
            {
                // Don't jump off the same wall (must be different from last wall)
                if (Vector3.Dot(hitInfo.normal, lastWallNormal) < 0.9f)
                {
                    StartWallRun(hitInfo.normal, hitInfo.transform);
                }
            }
            else if (Physics.Raycast(transform.position, -transform.right, out hitInfo, wallJumpDetectionDistance, wallJumpLayers))
            {
                // Don't jump off the same wall
                if (Vector3.Dot(hitInfo.normal, lastWallNormal) < 0.9f)
                {
                    StartWallRun(hitInfo.normal, hitInfo.transform);
                }
            }
        }

        #endregion

        #region Rail Grinding

        /// <summary>
        /// Checks for grind rails to snap to
        /// </summary>
        private void CheckForGrindRail()
        {
            // Need to be moving
            if (movementDirection.magnitude < 0.1f) return;
            
            // Cast a ray downward to find grind rails
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, grindRailSnapDistance, grindRailLayers))
            {
                StartGrinding(hitInfo.transform);
            }
        }

        /// <summary>
        /// Starts grinding on a rail
        /// </summary>
        private void StartGrinding(Transform rail)
        {
            if (isGrinding) return;
            
            isGrinding = true;
            currentGrindRail = rail;
            grindStartTime = Time.time;
            grindStartPosition = transform.position;
            grindBalanceAmount = 0f; // Reset balance
            
            // Play sound effect
            if (grindRailSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(grindRailSound);
            }
            
            // Start particles
            if (grindParticles != null)
            {
                grindParticles.Play();
            }
            
            // Play animation
            if (animator != null)
            {
                animator.SetBool("Grinding", true);
            }
            
            // Snap to rail
            Vector3 newPosition = transform.position;
            newPosition.y = currentGrindRail.position.y + 1f; // Offset for character height
            transform.position = newPosition;
            
            // Align to rail direction
            RailPath railPath = currentGrindRail.GetComponent<RailPath>();
            if (railPath != null)
            {
                Vector3 railDirection = railPath.GetDirectionAtPoint(transform.position);
                transform.forward = railDirection;
            }
            else
            {
                transform.forward = currentGrindRail.forward;
            }
        }

        /// <summary>
        /// Stops grinding
        /// </summary>
        private void StopGrinding()
        {
            if (!isGrinding) return;
            
            isGrinding = true;
            currentGrindRail = null;
            
            // Stop particles
            if (grindParticles != null)
            {
                grindParticles.Stop();
            }
            
            // Update animation
            if (animator != null)
            {
                animator.SetBool("Grinding", false);
                animator.SetFloat("GrindBalance", 0f);
            }
        }

        /// <summary>
        /// Apply grinding movement along rail
        /// </summary>
        private void ApplyGrindMovement()
        {
            if (currentGrindRail == null)
            {
                StopGrinding();
                return;
            }
            
            // Get rail direction
            Vector3 railDirection = currentGrindRail.forward;
            
            // If rail has spline or path component, use that instead
            RailPath railPath = currentGrindRail.GetComponent<RailPath>();
            if (railPath != null)
            {
                railDirection = railPath.GetDirectionAtPoint(transform.position);
            }
            
            // Calculate grind speed (can be influenced by input for acceleration/tricks)
            float speedMultiplier = 1f;
            float forwardInput = Vector3.Dot(movementDirection, transform.forward);
            if (forwardInput > 0.1f)
            {
                speedMultiplier = Mathf.Lerp(1f, 1.3f, forwardInput);
            }
            else if (forwardInput < -0.1f)
            {
                speedMultiplier = Mathf.Lerp(1f, 0.7f, -forwardInput);
            }
            
            // Apply rotation for tricks
            float horizontalInput = Vector3.Dot(movementDirection, transform.right);
            currentGrindRotation = Mathf.Lerp(currentGrindRotation, horizontalInput * maxGrindTilt, Time.deltaTime * 5f);
            
            // Move along rail
            if (controller != null)
            {
                controller.Move(railDirection * grindRailSpeed * speedMultiplier * Time.fixedDeltaTime);
            }
            else if (rb != null)
            {
                rb.velocity = new Vector3(railDirection.x * grindRailSpeed * speedMultiplier, 0, 
                    railDirection.z * grindRailSpeed * speedMultiplier);
            }
            
            // Rotate character to face along the rail with trick rotation
            transform.forward = Vector3.Slerp(transform.forward, railDirection, 15f * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 
                transform.rotation.eulerAngles.y, currentGrindRotation);
            
            // Check if still on rail
            RaycastHit hit;
            if (!Physics.Raycast(transform.position, Vector3.down, out hit, grindRailSnapDistance * 1.5f, grindRailLayers))
            {
                // No rail detected, stop grinding
                StopGrinding();
            }
            else if (hit.transform != currentGrindRail)
            {
                // Different rail, update reference
                currentGrindRail = hit.transform;
            }
            
            // Update rail position for curved rails
            if (railPath != null)
            {
                // Ensure we stay on the rail path
                Vector3 closestPoint = railPath.GetClosestPoint(transform.position, out float distance, out int segment);
                
                // Adjust y position to stay on rail (keep our own x,z for smooth movement)
                Vector3 newPosition = transform.position;
                newPosition.y = closestPoint.y + 1f; // Character height offset
                transform.position = newPosition;
            }
        }

        /// <summary>
        /// Jump while grinding
        /// </summary>
        public void GrindJump()
        {
            if (!isGrinding) return;
            
            // Calculate jump direction (up and slightly forward)
            Vector3 jumpDirection = (Vector3.up + transform.forward * 0.5f).normalized;
            
            // Stop grinding
            StopGrinding();
            
            // Apply jump force
            if (controller != null)
            {
                velocity = jumpDirection * grindJumpForce;
            }
            else if (rb != null)
            {
                rb.velocity = Vector3.zero; // Clear current velocity
                rb.AddForce(jumpDirection * grindJumpForce, ForceMode.Impulse);
            }
            
            // Play jump animation
            if (animator != null)
            {
                animator.SetTrigger("Jump");
                animator.SetBool("Jumping", true);
            }
            
            // Play jump sound
            PlayJumpSound();
            
            isJumping = true;
        }

        /// <summary>
        /// Update the grinding balance based on player input
        /// </summary>
        private void UpdateGrindingBalance()
        {
            // Calculate balance changes based on input and environment
            float horizontalInput = Vector3.Dot(movementDirection, transform.right);
            
            // Random environmental factor
            float environmentalFactor = Mathf.Sin(Time.time * 5f) * 0.2f;
            
            // Update balance
            grindBalanceAmount += horizontalInput * Time.deltaTime * 2f + environmentalFactor * Time.deltaTime;
            
            // Clamp balance
            grindBalanceAmount = Mathf.Clamp(grindBalanceAmount, -1f, 1f);
            
            // Natural balance correction (easier with higher grindBalance skill)
            float correction = grindBalanceAmount * Time.deltaTime * grindBalance * 0.1f;
            grindBalanceAmount -= correction;
            
            // Fall off if balance lost
            if (Mathf.Abs(grindBalanceAmount) > 0.95f)
            {
                // Fall in direction of imbalance
                Vector3 fallDirection = transform.right * Mathf.Sign(grindBalanceAmount);
                StopGrinding();
                
                // Apply small force in fall direction
                if (controller != null)
                {
                    velocity.y = -2f; // Fall downward
                    controller.Move(fallDirection * 2f * Time.deltaTime);
                }
                else if (rb != null)
                {
                    rb.AddForce(fallDirection * 2f, ForceMode.Impulse);
                }
                
                // Play animation
                if (animator != null)
                {
                    animator.SetTrigger("GrindFall");
                }
            }
            
            // Update animation parameter
            if (animator != null)
            {
                animator.SetFloat("GrindBalance", grindBalanceAmount);
            }
        }

        #endregion

        #region Vaulting

        /// <summary>
        /// Check for obstacles that can be vaulted over
        /// </summary>
        private void CheckForVaultableObstacle()
        {
            if (isVaulting || !isGrounded) return;
            
            // Raycast forward to find potential obstacles
            RaycastHit hitLow, hitHigh;
            
            // Lower ray to detect obstacle
            bool hitObstacleLow = Physics.Raycast(
                transform.position + Vector3.up * 0.5f, 
                transform.forward, 
                out hitLow, 
                vaultDetectionDistance, 
                vaultableLayers
            );
            
            // Higher ray to ensure space above obstacle
            bool hitObstacleHigh = Physics.Raycast(
                transform.position + Vector3.up * 1.5f, 
                transform.forward, 
                out hitHigh, 
                vaultDetectionDistance + 0.5f, 
                vaultableLayers
            );
            
            // Check if we can vault (hit low obstacle but not high)
            if (hitObstacleLow && !hitObstacleHigh)
            {
                // Check height of obstacle
                float obstacleHeight = GetObstacleHeight(hitLow.transform);
                
                if (obstacleHeight > 0.5f && obstacleHeight < vaultHeight)
                {
                    StartVault(hitLow.transform, obstacleHeight);
                }
            }
        }

        /// <summary>
        /// Gets the height of an obstacle for vaulting
        /// </summary>
        private float GetObstacleHeight(Transform obstacle)
        {
            // Try to get height from collider
            Collider collider = obstacle.GetComponent<Collider>();
            if (collider != null)
            {
                if (collider is BoxCollider)
                {
                    BoxCollider box = collider as BoxCollider;
                    return box.size.y * obstacle.lossyScale.y;
                }
                else
                {
                    // For other collider types, use bounds
                    return collider.bounds.size.y;
                }
            }
            
            // Default height if no collider
            return 1f;
        }

        /// <summary>
        /// Start vaulting over an obstacle
        /// </summary>
        private void StartVault(Transform obstacle, float obstacleHeight)
        {
            isVaulting = true;
            currentVaultObstacle = obstacle;
            
            // Calculate vault trajectory
            vaultStartPosition = transform.position;
            
            // Raycast to find landing position
            RaycastHit hit;
            if (Physics.Raycast(
                obstacle.position + Vector3.up * obstacleHeight + transform.forward * 1f, 
                Vector3.down, 
                out hit, 
                5f, 
                groundLayer
            ))
            {
                vaultTargetPosition = hit.point + Vector3.up * 0.1f; // Slightly above ground
            }
            else
            {
                // Default target if no ground found
                vaultTargetPosition = obstacle.position + transform.forward * 2f;
            }
            
            // Play sound
            if (vaultSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(vaultSound);
            }
            
            // Play particles
            if (vaultParticles != null)
            {
                vaultParticles.Play();
            }
            
            // Play animation
            if (animator != null)
            {
                animator.SetTrigger("Vault");
                animator.SetBool("Vaulting", true);
            }
            
            // Start vault routine
            StartCoroutine(VaultRoutine(obstacleHeight));
        }

        /// <summary>
        /// Routine to handle vaulting movement
        /// </summary>
        private IEnumerator VaultRoutine(float obstacleHeight)
        {
            float vaultDuration = Vector3.Distance(vaultStartPosition, vaultTargetPosition) / vaultSpeed;
            float elapsedTime = 0f;
            
            while (elapsedTime < vaultDuration)
            {
                float progress = elapsedTime / vaultDuration;
                
                // Calculate position with arc
                Vector3 straightLine = Vector3.Lerp(vaultStartPosition, vaultTargetPosition, progress);
                
                // Add height curve (parabolic arc)
                float heightMultiplier = Mathf.Sin(progress * Mathf.PI); // Sin curve for smooth up-down
                float currentHeight = obstacleHeight * 1.2f * heightMultiplier; // Peak slightly higher than obstacle
                
                // Set position
                transform.position = straightLine + Vector3.up * currentHeight;
                
                // Keep facing forward
                transform.forward = Vector3.Lerp(transform.forward, 
                    (vaultTargetPosition - vaultStartPosition).normalized, 
                    progress * 2f);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Ensure we reach the target
            transform.position = vaultTargetPosition;
            
            // Apply forward boost at the end of vault
            if (controller != null)
            {
                velocity = transform.forward * vaultForwardBoost;
            }
            else if (rb != null)
            {
                rb.velocity = transform.forward * vaultForwardBoost;
            }
            
            // End vaulting
            isVaulting = false;
            currentVaultObstacle = null;
            
            // Update animation
            if (animator != null)
            {
                animator.SetBool("Vaulting", false);
            }
        }

        /// <summary>
        /// Apply vaulting movement (implemented in VaultRoutine)
        /// </summary>
        private void ApplyVaultMovement()
        {
            // Movement is handled in the VaultRoutine coroutine
        }

        #endregion

        #region Quick Turn

        /// <summary>
        /// Starts a quick turn animation for sharp direction changes
        /// </summary>
        private void StartQuickTurn()
        {
            if (isQuickTurning || !isGrounded) return;
            
            isQuickTurning = true;
            
            // Play animation
            if (animator != null)
            {
                animator.SetTrigger("QuickTurn");
            }
            
            // Play sound
            if (quickTurnSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(quickTurnSound);
            }
            
            // Start timer
            StartCoroutine(QuickTurnRoutine());
        }

        /// <summary>
        /// Updates the quick turn state and applies movement
        /// </summary>
        private void UpdateQuickTurn()
        {
            // Movement is reduced during quick turn
            if (controller != null)
            {
                Vector3 move = transform.right * movementDirection.x * 0.3f + transform.forward * movementDirection.z * 0.3f;
                controller.Move(move * currentMovementSpeed * Time.fixedDeltaTime);
            }
            else if (rb != null)
            {
                // Reduce velocity during turn
                rb.velocity = rb.velocity * 0.9f;
            }
            
            // Rotate quickly to new direction if input is present
            if (movementDirection.magnitude > 0.1f)
            {
                transform.forward = Vector3.Slerp(transform.forward, movementDirection.normalized, 25f * Time.deltaTime);
            }
        }

        /// <summary>
        /// Routine to handle quick turn duration
        /// </summary>
        private IEnumerator QuickTurnRoutine()
        {
            yield return new WaitForSeconds(quickTurnDuration);
            isQuickTurning = false;
        }

        #endregion

        #region Terminal Hacking

        /// <summary>
        /// Interact with a hackable terminal
        /// </summary>
        public void InteractWithTerminal(HackableTerminal terminal)
        {
            if (hackingSystem != null && terminal != null)
            {
                hackingSystem.StartHacking(terminal);
            }
        }

        /// <summary>
        /// Checks if currently hacking a terminal
        /// </summary>
        public bool IsHackingTerminal()
        {
            return hackingSystem != null && hackingSystem.IsHacking;
        }

        /// <summary>
        /// Cancel current terminal hacking
        /// </summary>
        public void CancelHacking()
        {
            if (hackingSystem != null)
            {
                hackingSystem.CancelHacking();
            }
        }

        #endregion

        #region Fragment Purification

        /// <summary>
        /// Purify the Urban Fragment, enhancing Alex's abilities
        /// </summary>
        public override void PurifyFragment()
        {
            base.PurifyFragment();
            
            // Avoid re-purification
            if (urbanDashEnhanced) return;
            urbanDashEnhanced = true;
            
            // Enhance Urban Dash
            urbanDashDistance *= 1.5f;
            urbanDashInvulnerabilityDuration *= 1.5f;
            specialAbilityCooldown *= 0.7f; // 30% cooldown reduction
            
            // Unlock double jump
            hasDoubleJump = true;
            
            // Visual effects
            if (fragmentPurificationParticles != null)
            {
                fragmentPurificationParticles.Play();
            }
            
            // Update fragment color
            if (fragmentVisual != null)
            {
                Renderer fragmentRenderer = fragmentVisual.GetComponent<Renderer>();
                if (fragmentRenderer != null)
                {
                    fragmentRenderer.material.SetColor("_EmissionColor", purifiedFragmentColor * 3f);
                }
            }
            
            // Activate aura effect
            if (electricAuraEffect != null)
            {
                electricAuraEffect.SetActive(true);
            }
            
            // Enhance parkour abilities
            wallRunDuration *= 1.3f;
            wallRunSpeed *= 1.2f;
            grindRailSpeed *= 1.2f;
            maxConsecutiveWallJumps += 1;
            vaultSpeed *= 1.2f;
            
            // Play purification animation
            if (animator != null)
            {
                animator.SetTrigger("FragmentPurified");
            }
            
            // Play purification sound
            if (fragmentPurificationSound != null && characterAudio != null)
            {
                characterAudio.PlayOneShot(fragmentPurificationSound);
            }
        }

        #endregion

        #region Animation Updates

        /// <summary>
        /// Updates animation states based on current movement
        /// </summary>
        private void UpdateAnimationStates()
        {
            if (animator == null) return;
            
            // Update movement speed parameter
            animator.SetFloat("MovementSpeed", movementDirection.magnitude);
            
            // Update grounded parameter
            animator.SetBool("Grounded", isGrounded);
            
            // Update vertical velocity
            float verticalVelocity = controller != null ? velocity.y : (rb != null ? rb.velocity.y : 0);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
            
            // Update in air time
            if (!isGrounded)
            {
                float airTime = Time.time - lastGroundedTime;
                animator.SetFloat("AirTime", airTime);
            }
            else
            {
                animator.SetFloat("AirTime", 0);
            }
            
            // Other movement states are set in their respective methods
        }

        #endregion

        #region Focus Time Effects

        /// <summary>
        /// Enhanced Focus Time effects specific to Alex
        /// </summary>
        protected override void PlayFocusTimeEffects(bool activate)
        {
            base.PlayFocusTimeEffects(activate);
            
            // Custom urban focus time effects
            if (activate)
            {
                // Trails become more visible/intense
                if (dashTrailEffect != null)
                {
                    TrailRenderer trail = dashTrailEffect.GetComponent<TrailRenderer>();
                    if (trail != null)
                    {
                        trail.time *= 3f; // Trails last longer during focus time
                    }
                }
                
                // Change fragment visual intensity
                if (fragmentVisual != null)
                {
                    Renderer renderer = fragmentVisual.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.SetColor("_EmissionColor", 
                            (isFragmentPurified ? purifiedFragmentColor : fragmentColor) * 5f);
                    }
                }
                
                // Electric aura becomes more intense
                if (electricAuraEffect != null && isFragmentPurified)
                {
                    ParticleSystem[] particles = electricAuraEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem ps in particles)
                    {
                        var emission = ps.emission;
                        emission.rateOverTimeMultiplier *= 2f;
                    }
                }
            }
            else
            {
                // Restore normal trail settings
                if (dashTrailEffect != null)
                {
                    TrailRenderer trail = dashTrailEffect.GetComponent<TrailRenderer>();
                    if (trail != null)
                    {
                        trail.time /= 3f; // Restore original trail time
                    }
                }
                
                // Restore normal fragment visual intensity
                if (fragmentVisual != null)
                {
                    Renderer renderer = fragmentVisual.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.SetColor("_EmissionColor", 
                            (isFragmentPurified ? purifiedFragmentColor : fragmentColor) * 2f);
                    }
                }
                
                // Restore normal aura intensity
                if (electricAuraEffect != null && isFragmentPurified)
                {
                    ParticleSystem[] particles = electricAuraEffect.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem ps in particles)
                    {
                        var emission = ps.emission;
                        emission.rateOverTimeMultiplier /= 2f;
                    }
                }
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Types of surfaces for footstep sounds and effects
        /// </summary>
        private enum EnvironmentSurface
        {
            Concrete,
            Metal,
            Glass,
            Water,
            Grass
        }

        #endregion
    }

    /// <summary>
    /// Component for terminal hacking, used by Alex