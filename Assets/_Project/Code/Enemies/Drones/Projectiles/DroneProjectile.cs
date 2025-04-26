using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RunawayHeroes.Characters;
using RunawayHeroes.Enemies;

namespace RunawayHeroes.Enemies
{
    /// <summary>
    /// Classe che gestisce i proiettili lanciati dai droni.
    /// Può essere configurata per diversi tipi di proiettili con diversi comportamenti.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class DroneProjectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private ProjectileType projectileType = ProjectileType.Energy;
        [SerializeField] private float lifetime = 5f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float explosionRadius = 2f;
        [SerializeField] private LayerMask damageLayers;
        [SerializeField] private bool canDamagePlayer = true;

        [Header("Effects")]
        [SerializeField] private GameObject impactEffect;
        [SerializeField] private GameObject trailEffect;
        [SerializeField] private Light projectileLight;
        [SerializeField] private Color projectileColor = Color.red;
        [SerializeField] private float lightIntensity = 2f;
        [SerializeField] private AudioClip flySound;
        [SerializeField] private AudioClip impactSound;

        // References
        private Rigidbody rb;
        private DroneController sourceController;
        private AudioSource audioSource;

        // State
        private bool isInitialized = false;
        private bool hasImpacted = false;
        private float impactTimer = 0f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.useGravity = false;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.volume = 0.5f;
                audioSource.maxDistance = 20f;
            }

            // Setup collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // Setup light
            if (projectileLight != null)
            {
                projectileLight.color = projectileColor;
                projectileLight.intensity = lightIntensity;
                projectileLight.range = projectileType == ProjectileType.Explosive ? 4f : 2f;
            }
        }

        private void Start()
        {
            // Play fly sound
            if (audioSource != null && flySound != null)
            {
                audioSource.clip = flySound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Initialize trail effect
            if (trailEffect != null)
            {
                GameObject trail = Instantiate(trailEffect, transform.position, Quaternion.identity, transform);
            }

            // Set lifetime
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            if (hasImpacted)
            {
                // Handle impact effect duration
                impactTimer -= Time.deltaTime;
                if (impactTimer <= 0f)
                {
                    Destroy(gameObject);
                }
                return;
            }

            // Update projectile effects based on type
            UpdateProjectileEffects();
        }

        private void UpdateProjectileEffects()
        {
            switch (projectileType)
            {
                case ProjectileType.Energy:
                    // Pulsing light
                    if (projectileLight != null)
                    {
                        projectileLight.intensity = lightIntensity * (0.8f + 0.2f * Mathf.Sin(Time.time * 10f));
                    }
                    break;

                case ProjectileType.Laser:
                    // Steady intense light
                    if (projectileLight != null)
                    {
                        projectileLight.intensity = lightIntensity * 1.2f;
                    }
                    break;

                case ProjectileType.Homing:
                    // Find closest target and adjust trajectory slightly
                    if (rb != null && rb.linearVelocity.magnitude > 0.1f)
                    {
                        GameObject player = GameObject.FindGameObjectWithTag("Player");
                        if (player != null)
                        {
                            Vector3 directionToTarget = (player.transform.position - transform.position).normalized;
                            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity.normalized, directionToTarget, Time.deltaTime * 2f) * rb.linearVelocity.magnitude;
                            transform.forward = rb.linearVelocity.normalized;
                        }
                    }
                    break;

                case ProjectileType.Explosive:
                    // Pulsing more intense light
                    if (projectileLight != null)
                    {
                        projectileLight.intensity = lightIntensity * (1f + 0.5f * Mathf.Sin(Time.time * 5f));
                    }
                    break;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasImpacted)
                return;

            // Check if we hit the player
            if (other.CompareTag("Player"))
            {
                if (canDamagePlayer)
                {
                    PlayerController playerController = other.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        HandlePlayerImpact(playerController);
                    }
                }
            }
            else if ((damageLayers.value & (1 << other.gameObject.layer)) != 0)
            {
                // We hit something else in our damage layer
                HandleImpact(other.transform.position);
            }
        }

        private void HandlePlayerImpact(PlayerController playerController)
        {
            switch (projectileType)
            {
                case ProjectileType.Explosive:
                    // Create explosion damage
                    Explode();
                    break;

                default:
                    // Direct damage
                    playerController.TakeDamage(damage, transform.position);
                    break;
            }

            // Handle impact
            HandleImpact(playerController.transform.position);
        }

        private void HandleImpact(Vector3 impactPoint)
        {
            hasImpacted = true;
            impactTimer = 2f; // Time to finish effects before destroying

            // Stop movement
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // Play impact sound
            if (audioSource != null)
            {
                audioSource.Stop(); // Stop fly sound
                if (impactSound != null)
                {
                    audioSource.PlayOneShot(impactSound);
                }
            }

            // Show impact effect
            if (impactEffect != null)
            {
                Instantiate(impactEffect, impactPoint, Quaternion.identity);
            }

            // Disable trail
            TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = false;
            }

            // Disable collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            // Disable renderer
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            // Disable light with fade
            if (projectileLight != null)
            {
                StartCoroutine(FadeOutLight());
            }
        }

        private void Explode()
        {
            // Find all colliders in explosion radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius, damageLayers);
            
            foreach (Collider hitCollider in hitColliders)
            {
                // Check if it's the player
                if (hitCollider.CompareTag("Player") && canDamagePlayer)
                {
                    PlayerController playerController = hitCollider.GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        // Calculate damage based on distance
                        float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                        float damagePercent = 1f - (distance / explosionRadius);
                        float actualDamage = damage * Mathf.Clamp01(damagePercent);
                        
                        playerController.TakeDamage(actualDamage, transform.position);
                    }
                }
                
                // Handle other damageable objects if needed
                // ...
            }
            
            // Create explosion visual effect
            if (impactEffect != null)
            {
                GameObject explosion = Instantiate(impactEffect, transform.position, Quaternion.identity);
                // Scale the effect based on explosion radius
                explosion.transform.localScale = Vector3.one * (explosionRadius / 2f);
            }
        }

        private IEnumerator FadeOutLight()
        {
            float initialIntensity = projectileLight.intensity;
            float fadeTime = 0.5f;
            float timer = 0f;
            
            while (timer < fadeTime)
            {
                timer += Time.deltaTime;
                projectileLight.intensity = Mathf.Lerp(initialIntensity, 0f, timer / fadeTime);
                yield return null;
            }
            
            projectileLight.enabled = false;
        }

        /// <summary>
        /// Inizializza il proiettile con i parametri necessari
        /// </summary>
        /// <param name="projectileDamage">Danno causato dal proiettile</param>
        /// <param name="source">Drone che ha sparato il proiettile</param>
        /// <param name="canHurtPlayer">Se il proiettile può danneggiare il giocatore</param>
        public void Initialize(float projectileDamage, DroneController source, bool canHurtPlayer)
        {
            damage = projectileDamage;
            sourceController = source;
            canDamagePlayer = canHurtPlayer;
            isInitialized = true;
            
            // Adjust settings based on drone type
            if (source != null)
            {
                switch (source.Type)
                {
                    case DroneType.Combat:
                        projectileType = ProjectileType.Explosive;
                        damage *= 1.2f;
                        if (projectileLight != null)
                        {
                            projectileLight.color = Color.red;
                            projectileLight.range *= 1.5f;
                        }
                        break;
                        
                    case DroneType.Surveillance:
                        projectileType = ProjectileType.Laser;
                        if (projectileLight != null)
                        {
                            projectileLight.color = Color.cyan;
                        }
                        break;
                        
                    case DroneType.Advertising:
                        projectileType = ProjectileType.Energy;
                        if (projectileLight != null)
                        {
                            projectileLight.color = Color.magenta;
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Tipi di proiettili disponibili
    /// </summary>
    public enum ProjectileType
    {
        Energy,     // Basic energy projectile
        Laser,      // Fast laser beam
        Homing,     // Projectile that homes in on target
        Explosive   // Explodes on impact
    }
}