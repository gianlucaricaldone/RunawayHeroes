using UnityEngine;
using System.Collections;

namespace RunawayHeroes
{
    /// <summary>
    /// Component that allows objects to be broken when hit with sufficient force,
    /// such as during Alex's Urban Dash ability.
    /// </summary>
    public class BreakableObstacle : MonoBehaviour
    {
        [Header("Obstacle Settings")]
        [Tooltip("Model displayed when the obstacle is intact")]
        [SerializeField] private GameObject intactModel;
        
        [Tooltip("Model displayed when the obstacle is broken (usually contains multiple rigidbody pieces)")]
        [SerializeField] private GameObject brokenModel;
        
        [Tooltip("Health of the obstacle before breaking")]
        [SerializeField] private float obstacleHealth = 100f;
        
        [Tooltip("Minimum force required to damage the obstacle")]
        [SerializeField] private float minimumForceToBreak = 5f;
        
        [Header("Physics Settings")]
        [Tooltip("Additional random force applied to broken pieces")]
        [SerializeField] private float explosionForce = 10f;
        
        [Tooltip("Radius of the explosion force")]
        [SerializeField] private float explosionRadius = 2f;
        
        [Tooltip("Upward modifier for the explosion force")]
        [SerializeField] private float explosionUpwardModifier = 1f;
        
        [Tooltip("Ensures pieces stay physically active for this duration")]
        [SerializeField] private float forceActiveDuration = 2f;
        
        [Header("Effects")]
        [Tooltip("Particle effect played when the obstacle breaks")]
        [SerializeField] private ParticleSystem breakEffect;
        
        [Tooltip("Sound played when the obstacle breaks")]
        [SerializeField] private AudioClip breakSound;
        
        [Tooltip("Volume of the break sound")]
        [SerializeField] private float breakSoundVolume = 1f;
        
        [Header("Scoring")]
        [Tooltip("Points awarded when the obstacle is broken")]
        [SerializeField] private int pointsAwarded = 50;
        
        [Tooltip("Whether this obstacle should increase the combo counter when broken")]
        [SerializeField] private bool increaseCombo = true;
        
        [Header("Collectible Spawn")]
        [Tooltip("Chance to spawn a collectible when broken (0-1)")]
        [SerializeField] private float collectibleSpawnChance = 0.2f;
        
        [Tooltip("Collectible prefab to spawn")]
        [SerializeField] private GameObject collectiblePrefab;

        // Private properties
        private bool isBroken = false;
        private Collider mainCollider;
        private AudioSource audioSource;
        private float currentHealth;

        /// <summary>
        /// Initialize components and state
        /// </summary>
        private void Awake()
        {
            // Get components
            mainCollider = GetComponent<Collider>();
            audioSource = GetComponent<AudioSource>();
            
            // Initialize health
            currentHealth = obstacleHealth;
            
            // Ensure proper initial state
            if (intactModel != null) intactModel.SetActive(true);
            if (brokenModel != null) brokenModel.SetActive(false);
        }

        /// <summary>
        /// Apply damage to the obstacle
        /// </summary>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="force">Force vector that caused the damage</param>
        /// <returns>True if the obstacle broke from this damage</returns>
        public bool ApplyDamage(float damage, Vector3 force)
        {
            if (isBroken) return false;
            
            // Check if force exceeds minimum threshold
            if (force.magnitude < minimumForceToBreak)
            {
                // Still apply some minimal damage
                currentHealth -= damage * 0.1f;
            }
            else
            {
                currentHealth -= damage;
            }
            
            // Check if broken
            if (currentHealth <= 0)
            {
                Break(force);
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Break the obstacle with a given force vector
        /// </summary>
        /// <param name="force">Force vector to apply to broken pieces</param>
        public void Break(Vector3 force)
        {
            if (isBroken) return;
            
            isBroken = true;
            
            // Switch models
            if (intactModel != null) intactModel.SetActive(false);
            if (brokenModel != null) brokenModel.SetActive(true);
            
            // Play effects
            PlayBreakEffects();
            
            // Apply physics to broken pieces
            ApplyPhysicsToBreakablePieces(force);
            
            // Award points
            AwardPoints();
            
            // Spawn collectible
            TrySpawnCollectible();
            
            // Disable the main collider
            if (mainCollider != null) mainCollider.enabled = false;
            
            // Schedule cleanup or destruction
            StartCoroutine(CleanupAfterDelay());
        }

        /// <summary>
        /// Play visual and audio effects for breaking
        /// </summary>
        private void PlayBreakEffects()
        {
            // Play particle effect
            if (breakEffect != null)
            {
                breakEffect.transform.position = transform.position;
                breakEffect.Play();
            }
            
            // Play sound effect
            if (breakSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(breakSound, breakSoundVolume);
                }
                else
                {
                    // Play at position if no audio source
                    AudioSource.PlayClipAtPoint(breakSound, transform.position, breakSoundVolume);
                }
            }
        }

        /// <summary>
        /// Apply physics forces to the broken pieces
        /// </summary>
        /// <param name="impactForce">Initial force vector from impact</param>
        private void ApplyPhysicsToBreakablePieces(Vector3 impactForce)
        {
            if (brokenModel == null) return;
            
            // Get all rigidbodies in the broken model
            Rigidbody[] pieces = brokenModel.GetComponentsInChildren<Rigidbody>();
            
            foreach (Rigidbody piece in pieces)
            {
                // Enable the rigidbody
                piece.isKinematic = false;
                
                // Apply explosion force
                piece.AddExplosionForce(
                    explosionForce, 
                    transform.position, 
                    explosionRadius, 
                    explosionUpwardModifier, 
                    ForceMode.Impulse
                );
                
                // Apply impact force
                piece.AddForce(impactForce, ForceMode.Impulse);
                
                // Ensure the rigidbody stays active for a while
                StartCoroutine(KeepRigidbodyActive(piece));
            }
        }

        /// <summary>
        /// Keeps a rigidbody active for a set duration
        /// </summary>
        /// <param name="rb">The rigidbody to keep active</param>
        private IEnumerator KeepRigidbodyActive(Rigidbody rb)
        {
            if (rb == null) yield break;
            
            rb.detectCollisions = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            
            // Keep active for the specified duration
            float activeTime = 0f;
            while (activeTime < forceActiveDuration)
            {
                rb.WakeUp();
                activeTime += Time.deltaTime;
                yield return null;
            }
            
            // Optionally make kinematic after active duration
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        /// <summary>
        /// Award points to the player's score
        /// </summary>
        private void AwardPoints()
        {
            // Find game manager or score controller to award points
            ScoreManager scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null)
            {
                scoreManager.AddPoints(pointsAwarded, increaseCombo);
            }
        }

        /// <summary>
        /// Try to spawn a collectible with the set chance
        /// </summary>
        private void TrySpawnCollectible()
        {
            if (collectiblePrefab == null) return;
            
            if (Random.value <= collectibleSpawnChance)
            {
                // Spawn the collectible slightly above the obstacle
                Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
                Instantiate(collectiblePrefab, spawnPosition, Quaternion.identity);
            }
        }

        /// <summary>
        /// Clean up or destroy the object after a delay
        /// </summary>
        private IEnumerator CleanupAfterDelay()
        {
            // Wait for effects to finish
            yield return new WaitForSeconds(5f);
            
            // Option 1: Destroy the whole object
            Destroy(gameObject);
            
            // Option 2: Just disable if pooling is used
            // gameObject.SetActive(false);
        }

        /// <summary>
        /// Handle collision with other objects
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (isBroken) return;
            
            // Check for player character impact
            CharacterBase character = collision.gameObject.GetComponent<CharacterBase>();
            if (character != null)
            {
                // Calculate impact force
                float impactForce = collision.relativeVelocity.magnitude;
                
                // Check for Alex using Urban Dash
                AlexCharacter alex = character as AlexCharacter;
                if (alex != null)
                {
                    // Apply damage based on impact force
                    ApplyDamage(impactForce * 10f, collision.relativeVelocity.normalized * impactForce);
                }
                else
                {
                    // Other characters cause less damage
                    ApplyDamage(impactForce * 2f, collision.relativeVelocity.normalized * impactForce);
                }
            }
            else
            {
                // Calculate impact force for non-character collisions
                float impactForce = collision.relativeVelocity.magnitude;
                if (impactForce > minimumForceToBreak)
                {
                    ApplyDamage(impactForce * 5f, collision.relativeVelocity.normalized * impactForce);
                }
            }
        }

        /// <summary>
        /// Draw gizmos in the editor for visualization
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw explosion radius
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
            
            // Draw minimum break force
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, minimumForceToBreak * 0.1f);
        }
    }

    /// <summary>
    /// Simple score manager interface to avoid compilation errors
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public void AddPoints(int points, bool increaseCombo = true)
        {
            // Implementation would be in the actual ScoreManager class
        }
    }
}