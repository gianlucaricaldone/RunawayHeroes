using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RunawayHeroes
{
    /// <summary>
    /// Base class for all playable characters in Runaway Heroes.
    /// Provides common functionality for movement, health management, abilities, and fragment interaction.
    /// </summary>
    public abstract class CharacterBase : MonoBehaviour
    {
        #region Character Properties

        [Header("Character Details")]
        [SerializeField] protected string characterName;
        [SerializeField] protected CharacterType characterType;
        [SerializeField] protected FragmentType fragmentType;
        [SerializeField] protected string characterDescription;

        [Header("Health Settings")]
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected float currentHealth;
        [SerializeField] protected float healthRegenerationRate = 0f;
        [SerializeField] protected bool isInvulnerable = false;
        [SerializeField] protected float invulnerabilityDuration = 1.5f;

        [Header("Movement Settings")]
        [SerializeField] protected float baseMovementSpeed = 8f;
        [SerializeField] protected float currentMovementSpeed;
        [SerializeField] protected float jumpForce = 10f;
        [SerializeField] protected float slideSpeed = 12f;
        [SerializeField] protected float slideDistance = 5f;
        [SerializeField] protected float gravity = -20f;
        [SerializeField] protected LayerMask groundLayer;
        [SerializeField] protected bool isGrounded = false;
        [SerializeField] protected bool canMove = true;

        [Header("Focus Time Settings")]
        [SerializeField] protected float focusTimeDuration = 10f;
        [SerializeField] protected float focusTimeCooldown = 25f;
        [SerializeField] protected float currentFocusTime;
        [SerializeField] protected float currentFocusTimeCooldown;
        [SerializeField] protected bool isFocusTimeActive = false;

        [Header("Special Ability Settings")]
        [SerializeField] protected float specialAbilityCooldown = 15f;
        [SerializeField] protected float currentSpecialAbilityCooldown = 0f;
        [SerializeField] protected bool isSpecialAbilityActive = false;

        [Header("Resonance Settings")]
        [SerializeField] protected float resonanceCooldown = 8f;
        [SerializeField] protected float currentResonanceCooldown = 0f;
        [SerializeField] protected List<CharacterBase> resonancePartners = new List<CharacterBase>();

        [Header("Fragment Settings")]
        [SerializeField] protected bool isFragmentPurified = false;
        [SerializeField] protected float fragmentPowerMultiplier = 1f;

        [Header("References")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected Rigidbody rb;
        [SerializeField] protected CharacterController controller;
        [SerializeField] protected Transform fragmentVisual;
        [SerializeField] protected ParticleSystem fragmentParticles;
        [SerializeField] protected AudioSource characterAudio;

        // Protected variables
        protected Vector3 movementDirection;
        protected Vector3 velocity;
        protected bool isJumping = false;
        protected bool isSliding = false;
        protected bool isDead = false;

        #endregion

        #region Events

        // Events
        public delegate void OnHealthChangeHandler(float newHealth, float maxHealth);
        public event OnHealthChangeHandler OnHealthChange;

        public delegate void OnSpecialAbilityHandler(bool activated);
        public event OnSpecialAbilityHandler OnSpecialAbility;

        public delegate void OnFocusTimeHandler(bool activated);
        public event OnFocusTimeHandler OnFocusTime;

        public delegate void OnResonanceHandler(CharacterBase partner);
        public event OnResonanceHandler OnResonance;

        public delegate void OnFragmentPurifiedHandler();
        public event OnFragmentPurifiedHandler OnFragmentPurified;

        public delegate void OnCharacterDeathHandler();
        public event OnCharacterDeathHandler OnCharacterDeath;

        #endregion

        #region Unity Lifecycle Methods

        protected virtual void Awake()
        {
            // Get component references if not assigned
            if (animator == null) animator = GetComponent<Animator>();
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (controller == null) controller = GetComponent<CharacterController>();
            if (characterAudio == null) characterAudio = GetComponent<AudioSource>();

            // Initialize health
            currentHealth = maxHealth;
            currentFocusTime = focusTimeDuration;
            currentMovementSpeed = baseMovementSpeed;
        }

        protected virtual void Start()
        {
            InitializeFragment();
        }

        protected virtual void Update()
        {
            if (isDead || !canMove) return;

            HandleCooldowns();
            CheckGrounded();
            
            if (healthRegenerationRate > 0)
            {
                RegenerateHealth();
            }
        }

        protected virtual void FixedUpdate()
        {
            if (isDead || !canMove) return;

            ApplyMovement();
        }

        #endregion

        #region Health Management

        /// <summary>
        /// Apply damage to the character
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        /// <param name="knockbackDirection">Direction of knockback (optional)</param>
        /// <param name="knockbackForce">Force of knockback (optional)</param>
        public virtual void TakeDamage(float damageAmount, Vector3 knockbackDirection = default, float knockbackForce = 0)
        {
            if (isInvulnerable || isDead) return;

            currentHealth -= damageAmount;
            OnHealthChange?.Invoke(currentHealth, maxHealth);

            // Apply visual feedback
            StartCoroutine(DamageVisualFeedback());

            // Apply knockback if specified
            if (knockbackForce > 0)
            {
                ApplyKnockback(knockbackDirection, knockbackForce);
            }

            // Check for death
            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                // Trigger brief invulnerability
                StartCoroutine(TemporaryInvulnerability());
            }
        }

        /// <summary>
        /// Heal the character
        /// </summary>
        /// <param name="healAmount">Amount to heal</param>
        public virtual void Heal(float healAmount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
            OnHealthChange?.Invoke(currentHealth, maxHealth);
        }

        /// <summary>
        /// Regenerate health over time
        /// </summary>
        protected virtual void RegenerateHealth()
        {
            if (currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(currentHealth + (healthRegenerationRate * Time.deltaTime), maxHealth);
                OnHealthChange?.Invoke(currentHealth, maxHealth);
            }
        }

        /// <summary>
        /// Apply knockback to the character
        /// </summary>
        protected virtual void ApplyKnockback(Vector3 direction, float force)
        {
            if (rb != null)
            {
                rb.AddForce(direction.normalized * force, ForceMode.Impulse);
            }
            else if (controller != null)
            {
                StartCoroutine(KnockbackRoutine(direction, force));
            }
        }

        /// <summary>
        /// Handle character death
        /// </summary>
        protected virtual void Die()
        {
            isDead = true;
            canMove = false;
            
            // Trigger death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
            
            OnCharacterDeath?.Invoke();
        }

        /// <summary>
        /// Provide temporary invulnerability after taking damage
        /// </summary>
        protected virtual IEnumerator TemporaryInvulnerability()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(invulnerabilityDuration);
            isInvulnerable = false;
        }

        /// <summary>
        /// Visual feedback when taking damage
        /// </summary>
        protected virtual IEnumerator DamageVisualFeedback()
        {
            // Flash character model or other visual feedback
            // This can be overridden by specific characters

            yield return null;
        }

        /// <summary>
        /// Knockback routine for characters using CharacterController
        /// </summary>
        protected virtual IEnumerator KnockbackRoutine(Vector3 direction, float force)
        {
            float duration = 0.25f;  // Short duration for knockback
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                float strength = Mathf.Lerp(force, 0, elapsed / duration);
                controller.Move(direction.normalized * strength * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        #region Movement

        /// <summary>
        /// Apply movement to the character
        /// </summary>
        protected virtual void ApplyMovement()
        {
            // This should be implemented by child classes based on whether they use
            // Rigidbody or CharacterController for movement
        }

        /// <summary>
        /// Check if the character is grounded
        /// </summary>
        protected virtual void CheckGrounded()
        {
            // This should be implemented by child classes based on their movement system
        }

        /// <summary>
        /// Perform a jump
        /// </summary>
        public virtual void Jump()
        {
            if (!isGrounded || isJumping || isSliding || isDead) return;

            isJumping = true;
            
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
            
            // Jump implementation depends on movement system used
            if (rb != null)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }
            else if (controller != null)
            {
                velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
        }

        /// <summary>
        /// Perform a slide
        /// </summary>
        public virtual void Slide()
        {
            if (!isGrounded || isJumping || isSliding || isDead) return;

            StartCoroutine(SlideRoutine());
        }

        /// <summary>
        /// Routine for slide movement
        /// </summary>
        protected virtual IEnumerator SlideRoutine()
        {
            isSliding = true;
            
            if (animator != null)
            {
                animator.SetBool("Sliding", true);
            }
            
            // Store original values to restore later
            float originalSpeed = currentMovementSpeed;
            currentMovementSpeed = slideSpeed;
            
            // Adjust collider size for slide (implementation will vary)
            AdjustColliderForSlide(true);
            
            // Wait for slide duration
            float slideDuration = slideDistance / slideSpeed;
            yield return new WaitForSeconds(slideDuration);
            
            // Restore original values
            currentMovementSpeed = originalSpeed;
            AdjustColliderForSlide(false);
            
            if (animator != null)
            {
                animator.SetBool("Sliding", false);
            }
            
            isSliding = false;
        }

        /// <summary>
        /// Adjust collider size for sliding
        /// </summary>
        protected virtual void AdjustColliderForSlide(bool isSliding)
        {
            // This should be implemented by child classes based on their collider setup
        }

        /// <summary>
        /// Set movement direction
        /// </summary>
        public virtual void SetMovementDirection(Vector3 direction)
        {
            if (isDead || !canMove) return;
            
            movementDirection = direction.normalized;
            
            if (animator != null && direction.magnitude > 0)
            {
                animator.SetFloat("Speed", direction.magnitude * currentMovementSpeed / baseMovementSpeed);
            }
        }

        /// <summary>
        /// Modify movement speed with a multiplier (for power-ups, etc.)
        /// </summary>
        public virtual void ModifyMovementSpeed(float multiplier, float duration)
        {
            StartCoroutine(TemporarySpeedModifier(multiplier, duration));
        }

        /// <summary>
        /// Temporarily modify movement speed
        /// </summary>
        protected virtual IEnumerator TemporarySpeedModifier(float multiplier, float duration)
        {
            float originalSpeed = currentMovementSpeed;
            currentMovementSpeed = baseMovementSpeed * multiplier;
            
            yield return new WaitForSeconds(duration);
            
            currentMovementSpeed = originalSpeed;
        }

        #endregion

        #region Focus Time

        /// <summary>
        /// Activate Focus Time
        /// </summary>
        public virtual void ActivateFocusTime()
        {
            if (isDead || currentFocusTimeCooldown > 0 || isFocusTimeActive) return;

            StartCoroutine(FocusTimeRoutine());
        }

        /// <summary>
        /// Focus Time activation routine
        /// </summary>
        protected virtual IEnumerator FocusTimeRoutine()
        {
            isFocusTimeActive = true;
            OnFocusTime?.Invoke(true);
            
            // Slow down game time
            Time.timeScale = 0.3f;
            
            // Make sure animations and other time-dependent systems adjust
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            // Play focus time effects
            PlayFocusTimeEffects(true);
            
            // Wait for focus time duration (using unscaled time)
            float elapsedTime = 0;
            while (elapsedTime < focusTimeDuration)
            {
                currentFocusTime = focusTimeDuration - elapsedTime;
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // Restore normal time
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            
            // Disable focus time effects
            PlayFocusTimeEffects(false);
            
            isFocusTimeActive = false;
            OnFocusTime?.Invoke(false);
            
            // Start cooldown
            currentFocusTimeCooldown = focusTimeCooldown;
        }

        /// <summary>
        /// Play visual/audio effects for Focus Time
        /// </summary>
        protected virtual void PlayFocusTimeEffects(bool activate)
        {
            // Base implementation - to be enhanced by child classes
            if (activate)
            {
                if (fragmentParticles != null)
                {
                    var main = fragmentParticles.main;
                    main.simulationSpeed = 0.3f;  // Slow down particles during focus time
                }
            }
            else
            {
                if (fragmentParticles != null)
                {
                    var main = fragmentParticles.main;
                    main.simulationSpeed = 1.0f;  // Restore normal speed
                }
            }
        }

        #endregion

        #region Special Ability

        /// <summary>
        /// Activate the character's special ability
        /// </summary>
        public virtual void ActivateSpecialAbility()
        {
            if (isDead || currentSpecialAbilityCooldown > 0 || isSpecialAbilityActive) return;
            
            // Base implementation - to be overridden by specific characters
            isSpecialAbilityActive = true;
            OnSpecialAbility?.Invoke(true);
            
            // Start cooldown
            currentSpecialAbilityCooldown = specialAbilityCooldown;
        }

        /// <summary>
        /// Deactivate the character's special ability
        /// </summary>
        protected virtual void DeactivateSpecialAbility()
        {
            isSpecialAbilityActive = false;
            OnSpecialAbility?.Invoke(false);
        }

        #endregion

        #region Resonance

        /// <summary>
        /// Activate resonance with another character
        /// </summary>
        public virtual void ActivateResonance(CharacterBase partner)
        {
            if (isDead || currentResonanceCooldown > 0 || !resonancePartners.Contains(partner)) return;
            
            // Base implementation - specific implementation in GameManager or dedicated Resonance system
            OnResonance?.Invoke(partner);
            
            // Start cooldown
            currentResonanceCooldown = resonanceCooldown;
        }

        /// <summary>
        /// Add a character as a potential resonance partner
        /// </summary>
        public virtual void AddResonancePartner(CharacterBase partner)
        {
            if (!resonancePartners.Contains(partner))
            {
                resonancePartners.Add(partner);
            }
        }

        #endregion

        #region Fragment

        /// <summary>
        /// Initialize the character's fragment
        /// </summary>
        protected virtual void InitializeFragment()
        {
            // Base implementation - to be enhanced by child classes
            
            // Set fragment visual based on purification state
            UpdateFragmentVisuals();
        }

        /// <summary>
        /// Update fragment visuals based on purification state
        /// </summary>
        protected virtual void UpdateFragmentVisuals()
        {
            if (fragmentParticles != null)
            {
                var main = fragmentParticles.main;
                main.startSize = isFragmentPurified ? 1.5f : 1.0f;
                main.startLifetime = isFragmentPurified ? 2.0f : 1.0f;
            }
        }

        /// <summary>
        /// Purify the fragment, enhancing abilities
        /// </summary>
        public virtual void PurifyFragment()
        {
            isFragmentPurified = true;
            fragmentPowerMultiplier = 1.5f;  // 50% boost to fragment-based abilities
            
            // Update visuals
            UpdateFragmentVisuals();
            
            OnFragmentPurified?.Invoke();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Handle all cooldowns
        /// </summary>
        protected virtual void HandleCooldowns()
        {
            // Handle Focus Time cooldown
            if (currentFocusTimeCooldown > 0)
            {
                currentFocusTimeCooldown -= Time.deltaTime;
            }
            
            // Handle Special Ability cooldown
            if (currentSpecialAbilityCooldown > 0)
            {
                currentSpecialAbilityCooldown -= Time.deltaTime;
            }
            
            // Handle Resonance cooldown
            if (currentResonanceCooldown > 0)
            {
                currentResonanceCooldown -= Time.deltaTime;
            }
        }

        /// <summary>
        /// Gets the current health percentage
        /// </summary>
        public float GetHealthPercentage()
        {
            return currentHealth / maxHealth;
        }

        /// <summary>
        /// Gets the focus time cooldown percentage
        /// </summary>
        public float GetFocusTimeCooldownPercentage()
        {
            return 1f - (currentFocusTimeCooldown / focusTimeCooldown);
        }

        /// <summary>
        /// Gets the special ability cooldown percentage
        /// </summary>
        public float GetSpecialAbilityCooldownPercentage()
        {
            return 1f - (currentSpecialAbilityCooldown / specialAbilityCooldown);
        }

        /// <summary>
        /// Gets the resonance cooldown percentage
        /// </summary>
        public float GetResonanceCooldownPercentage()
        {
            return 1f - (currentResonanceCooldown / resonanceCooldown);
        }

        #endregion

        #region Enums

        /// <summary>
        /// Character types in Runaway Heroes
        /// </summary>
        public enum CharacterType
        {
            Alex,  // Urban Runner
            Maya,  // Forest Explorer
            Kai,   // Tundra Climber
            Ember, // Volcano Survivor
            Marina,// Abyssal Diver
            Neo    // Virtual Hacker
        }

        /// <summary>
        /// Fragment types in Runaway Heroes
        /// </summary>
        public enum FragmentType
        {
            Urban,  // Blue electric
            Natural,// Green emerald
            Glacial,// White crystalline
            Igneous,// Red fiery
            Abyssal,// Aquamarine
            Digital // Purple violet
        }

        #endregion
    }
}