using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RunawayHeroes.Characters;
using RunawayHeroes.Core.Tutorial;
using RunawayHeroes.Manager;

namespace RunawayHeroes.Enemies
{
    /// <summary>
    /// Controlla il comportamento dei droni nel gioco, con supporto per diversi tipi e comportamenti.
    /// Utilizzabile sia nel tutorial che nei livelli regolari del Mondo 1.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        [Header("Drone Settings")]
        [SerializeField] private DroneType droneType = DroneType.Surveillance;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 120f;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 5f;
        [SerializeField] private float damageAmount = 10f;
        [SerializeField] private bool isTutorialDrone = false;
        [SerializeField] private bool canDamagePlayer = true;
        [SerializeField] private float patrolHeight = 3f;

        [Header("Movement Pattern")]
        [SerializeField] private DroneMovementPattern movementPattern = DroneMovementPattern.Patrol;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float waypointStopDistance = 1f;
        [SerializeField] private float waypointWaitTime = 2f;
        [SerializeField] private float chaseSpeed = 7f;
        [SerializeField] private float maxChaseDistance = 20f;
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderChangeInterval = 3f;

        [Header("Attack Parameters")]
        [SerializeField] private bool canAttack = true;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private int burstCount = 3;
        [SerializeField] private float burstInterval = 0.2f;
        [SerializeField] private float accuracyVariation = 0.05f;

        [Header("FX")]
        [SerializeField] private GameObject scanBeamPrefab;
        [SerializeField] private GameObject detectionEffectPrefab;
        [SerializeField] private GameObject damageFX;
        [SerializeField] private GameObject destructionFX;
        [SerializeField] private AudioClip[] movementSounds;
        [SerializeField] private AudioClip detectionSound;
        [SerializeField] private AudioClip attackSound;
        [SerializeField] private AudioClip damageSound;
        [SerializeField] private AudioClip destructionSound;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material alertMaterial;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color idleColor = Color.green;
        [SerializeField] private Color alertColor = Color.red;
        [SerializeField] private Color searchColor = Color.yellow;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool isInvulnerable = false;
        [SerializeField] private bool destructibleInTutorial = false;

        // References
        private Rigidbody rb;
        private Transform playerTransform;
        private PlayerController playerController;
        private AudioSource audioSource;
        private List<Renderer> renderers = new List<Renderer>();
        private TutorialManager tutorialManager;

        // State variables
        private float currentHealth;
        private int currentPatrolIndex = 0;
        private Vector3 wanderTarget;
        private float wanderTimer = 0f;
        private bool isAttacking = false;
        private float attackTimer = 0f;
        private DroneState currentState = DroneState.Idle;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool playerDetected = false;
        private float playerLastSeenTime = 0f;
        private Vector3 playerLastSeenPosition;
        private GameObject currentScanBeam;
        private bool isScanning = false;
        private float stunTimer = 0f;
        private bool isStunned = false;
        private bool isInitialized = false;
        private GameObject activeTarget;
        private Vector3 randomMotionOffset;
        private float randomMotionTimer = 0f;
        private float searchTimer = 0f;
        private float attackBeamIntensity = 0f;
        private Color currentLightColor;
        private float pulseTimer = 0f;

        // Events
        public event Action<DroneController> OnDroneDestroyed;
        public event Action<DroneController> OnDroneDetectedPlayer;
        public event Action<DroneController> OnDroneAttacked;
        public event Action<DroneController> OnDroneDamaged;

        // Properties
        public DroneType Type => droneType;
        public DroneState State => currentState;
        public bool IsPlayerDetected => playerDetected;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsTutorialDrone => isTutorialDrone;
        public bool CanDamagePlayer => canDamagePlayer && !isTutorialDrone;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = 20f;
            }

            // Find renderers for material swapping
            Renderer[] droneRenderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in droneRenderers)
            {
                renderers.Add(renderer);
            }

            // Store initial position/rotation for reset
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            // Setup
            currentHealth = maxHealth;
            SetupDroneByType();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                // Find player
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                    playerController = player.GetComponent<PlayerController>();
                }

                // Find tutorial manager if in tutorial
                if (isTutorialDrone)
                {
                    tutorialManager = TutorialManager.Instance;
                }

                // Generate initial wander target
                GenerateWanderTarget();

                // Start default behavior
                SetDroneState(DroneState.Idle);

                isInitialized = true;
            }

            // Play startup sound
            if (audioSource && movementSounds.Length > 0)
            {
                audioSource.clip = movementSounds[0];
                audioSource.Play();
            }

            // Initialize lights
            if (statusLight != null)
            {
                statusLight.color = idleColor;
                currentLightColor = idleColor;
            }

            // Set drone specific settings
            randomMotionOffset = new Vector3(
                UnityEngine.Random.Range(-1f, 1f), 
                UnityEngine.Random.Range(-0.5f, 0.5f), 
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;
        }

        private void Update()
        {
            // Skip updates if stunned
            if (isStunned)
            {
                stunTimer -= Time.deltaTime;
                if (stunTimer <= 0f)
                {
                    isStunned = false;
                    rb.isKinematic = false;
                }
                return;
            }

            // Handle attack cooldown
            if (isAttacking && attackTimer > 0f)
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    isAttacking = false;
                }
            }

            // Handle random motion timing
            randomMotionTimer -= Time.deltaTime;
            if (randomMotionTimer <= 0f)
            {
                randomMotionTimer = UnityEngine.Random.Range(1f, 3f);
                randomMotionOffset = new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-0.5f, 0.5f),
                    UnityEngine.Random.Range(-1f, 1f)
                ).normalized * 0.5f;
            }

            // Main state machine
            switch (currentState)
            {
                case DroneState.Idle:
                    UpdateIdleState();
                    break;
                case DroneState.Patrol:
                    UpdatePatrolState();
                    break;
                case DroneState.Wander:
                    UpdateWanderState();
                    break;
                case DroneState.Chase:
                    UpdateChaseState();
                    break;
                case DroneState.Attack:
                    UpdateAttackState();
                    break;
                case DroneState.Search:
                    UpdateSearchState();
                    break;
                case DroneState.Retreat:
                    UpdateRetreatState();
                    break;
            }

            // Always check for player if not in tutorial mode
            if (!isTutorialDrone)
            {
                CheckForPlayer();
            }

            // Update status light effects
            UpdateStatusLight();
        }

        private void FixedUpdate()
        {
            if (isStunned)
                return;

            // Apply movement based on state
            switch (currentState)
            {
                case DroneState.Patrol:
                    ApplyPatrolMovement();
                    break;
                case DroneState.Wander:
                    ApplyWanderMovement();
                    break;
                case DroneState.Chase:
                    ApplyChaseMovement();
                    break;
                case DroneState.Search:
                    ApplySearchMovement();
                    break;
                case DroneState.Retreat:
                    ApplyRetreatMovement();
                    break;
                case DroneState.Idle:
                    ApplyIdleMovement();
                    break;
            }
        }

        #region State Updates
        private void UpdateIdleState()
        {
            // In idle state, drone hovers and looks for player
            if (!isTutorialDrone && playerDetected)
            {
                SetDroneState(DroneState.Chase);
                return;
            }

            // After some time, transition to patrol or wander
            if (Time.time > playerLastSeenTime + 5f)
            {
                if (movementPattern == DroneMovementPattern.Patrol && patrolPoints.Length > 0)
                {
                    SetDroneState(DroneState.Patrol);
                }
                else
                {
                    SetDroneState(DroneState.Wander);
                }
            }
        }

        private void UpdatePatrolState()
        {
            if (patrolPoints.Length == 0)
            {
                SetDroneState(DroneState.Wander);
                return;
            }

            if (!isTutorialDrone && playerDetected)
            {
                SetDroneState(DroneState.Chase);
                return;
            }

            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null)
            {
                // Skip invalid waypoints
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, targetPoint.position);
            if (distanceToTarget <= waypointStopDistance)
            {
                // Wait at waypoint
                StartCoroutine(WaitAtWaypoint());
            }
        }

        private void UpdateWanderState()
        {
            if (!isTutorialDrone && playerDetected)
            {
                SetDroneState(DroneState.Chase);
                return;
            }

            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0f)
            {
                GenerateWanderTarget();
            }
        }

        private void UpdateChaseState()
        {
            if (playerTransform == null)
            {
                SetDroneState(DroneState.Search);
                return;
            }

            // Check if player is still in range
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > maxChaseDistance)
            {
                // Lost the player, go to search state
                SetDroneState(DroneState.Search);
                return;
            }

            // Check if we're in attack range
            if (canAttack && !isAttacking && distanceToPlayer <= attackRange)
            {
                SetDroneState(DroneState.Attack);
                return;
            }
        }

        private void UpdateAttackState()
        {
            if (playerTransform == null)
            {
                SetDroneState(DroneState.Search);
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer > attackRange)
            {
                SetDroneState(DroneState.Chase);
                return;
            }

            // Attack logic
            if (canAttack && !isAttacking && attackTimer <= 0f)
            {
                StartCoroutine(PerformAttack());
            }
        }

        private void UpdateSearchState()
        {
            // Search for player in last known location
            searchTimer -= Time.deltaTime;
            
            if (playerDetected)
            {
                SetDroneState(DroneState.Chase);
                return;
            }

            if (searchTimer <= 0f)
            {
                // Failed to find player, return to patrol/wander
                if (movementPattern == DroneMovementPattern.Patrol && patrolPoints.Length > 0)
                {
                    SetDroneState(DroneState.Patrol);
                }
                else
                {
                    SetDroneState(DroneState.Wander);
                }
            }
        }

        private void UpdateRetreatState()
        {
            // Not yet implemented
            SetDroneState(DroneState.Idle);
        }
        #endregion

        #region Movement Application
        private void ApplyIdleMovement()
        {
            // Hover in place with slight movement
            Vector3 targetPosition = initialPosition + new Vector3(0, Mathf.Sin(Time.time) * 0.2f, 0) + randomMotionOffset;
            MoveToTarget(targetPosition, moveSpeed * 0.5f);
            
            // Slow rotation
            transform.Rotate(Vector3.up, rotationSpeed * 0.2f * Time.deltaTime);
        }

        private void ApplyPatrolMovement()
        {
            if (patrolPoints.Length == 0 || currentPatrolIndex >= patrolPoints.Length)
                return;

            Transform target = patrolPoints[currentPatrolIndex];
            if (target == null)
                return;

            // Move to waypoint
            Vector3 targetPosition = target.position;
            MoveToTarget(targetPosition, moveSpeed);
            
            // Face movement direction
            LookAtTarget(targetPosition);
        }

        private void ApplyWanderMovement()
        {
            // Move toward wander target with some variation
            Vector3 targetPosition = wanderTarget + randomMotionOffset;
            MoveToTarget(targetPosition, moveSpeed * 0.7f);
            
            // Face movement direction
            LookAtTarget(targetPosition);
        }

        private void ApplyChaseMovement()
        {
            if (playerTransform == null)
                return;

            // Move toward player position
            Vector3 targetPosition = playerTransform.position + Vector3.up * patrolHeight;
            MoveToTarget(targetPosition, chaseSpeed);
            
            // Face player
            LookAtTarget(playerTransform.position);
        }

        private void ApplySearchMovement()
        {
            // Move to last known player position with circling pattern
            float searchRadius = 3f;
            float searchSpeed = 3f;
            
            Vector3 circleOffset = new Vector3(
                Mathf.Sin(Time.time * searchSpeed) * searchRadius,
                Mathf.Sin(Time.time * searchSpeed * 0.5f) * 1f, 
                Mathf.Cos(Time.time * searchSpeed) * searchRadius
            );
            
            Vector3 targetPosition = playerLastSeenPosition + circleOffset;
            MoveToTarget(targetPosition, moveSpeed * 0.8f);
            
            // Look around
            LookAtTarget(playerLastSeenPosition + 
                new Vector3(Mathf.Sin(Time.time * 2f) * 5f, 0, Mathf.Cos(Time.time * 2f) * 5f));
        }

        private void ApplyRetreatMovement()
        {
            // Move back to initial position
            MoveToTarget(initialPosition, moveSpeed * 1.2f);
            
            // Face original rotation gradually
            transform.rotation = Quaternion.Slerp(transform.rotation, initialRotation, Time.fixedDeltaTime * rotationSpeed * 0.5f);
        }
        #endregion

        #region Helper Methods
        private void MoveToTarget(Vector3 targetPosition, float speed)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            rb.velocity = direction * speed;
        }

        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 directionToTarget = targetPosition - transform.position;
            directionToTarget.y = 0; // Keep drone level
            
            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        private void GenerateWanderTarget()
        {
            // Generate random point within wander radius
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * wanderRadius;
            randomDirection.y = 0;
            wanderTarget = initialPosition + randomDirection;
            wanderTarget.y = initialPosition.y + patrolHeight;
            
            // Reset timer
            wanderTimer = wanderChangeInterval;
        }

        private IEnumerator WaitAtWaypoint()
        {
            // Stop movement
            rb.velocity = Vector3.zero;
            
            // Wait
            yield return new WaitForSeconds(waypointWaitTime);
            
            // Move to next waypoint
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        private void CheckForPlayer()
        {
            if (playerTransform == null)
                return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            
            // Check line of sight if within detection range
            if (distanceToPlayer <= detectionRange)
            {
                // Cast ray to check for obstacles
                Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
                RaycastHit hit;
                
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, detectionRange))
                {
                    if (hit.transform == playerTransform)
                    {
                        // Player detected
                        if (!playerDetected)
                        {
                            OnPlayerDetected();
                        }
                        
                        playerDetected = true;
                        playerLastSeenTime = Time.time;
                        playerLastSeenPosition = playerTransform.position;
                    }
                }
            }
            else if (playerDetected && distanceToPlayer > detectionRange)
            {
                // Lost sight of player
                playerDetected = false;
            }
        }

        private void OnPlayerDetected()
        {
            // Play detection effect
            if (detectionEffectPrefab != null)
            {
                GameObject effect = Instantiate(detectionEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // Play detection sound
            if (audioSource && detectionSound != null)
            {
                audioSource.PlayOneShot(detectionSound);
            }
            
            // Change materials
            if (alertMaterial != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == defaultMaterial)
                        {
                            materials[i] = alertMaterial;
                        }
                    }
                    renderer.materials = materials;
                }
            }
            
            // Trigger event
            OnDroneDetectedPlayer?.Invoke(this);
        }

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            attackTimer = attackCooldown;

            // Trigger event
            OnDroneAttacked?.Invoke(this);
            
            // Play attack sound
            if (audioSource && attackSound != null)
            {
                audioSource.PlayOneShot(attackSound);
            }

            // Different attack behavior based on drone type
            switch (droneType)
            {
                case DroneType.Surveillance:
                    // Surveillance drones use scan beam
                    yield return StartCoroutine(PerformScanAttack());
                    break;
                
                case DroneType.Combat:
                    // Combat drones fire projectiles
                    yield return StartCoroutine(PerformProjectileAttack());
                    break;
                
                case DroneType.Security:
                    // Security drones use direct damage
                    yield return StartCoroutine(PerformDirectAttack());
                    break;
                
                case DroneType.Advertising:
                    // Advertising drones use flashbang
                    yield return StartCoroutine(PerformFlashAttack());
                    break;
            }

            isAttacking = false;
        }

        private IEnumerator PerformScanAttack()
        {
            if (scanBeamPrefab != null && playerTransform != null)
            {
                // Create scan beam
                currentScanBeam = Instantiate(scanBeamPrefab, firePoint.position, Quaternion.identity);
                currentScanBeam.transform.parent = firePoint;
                
                // Scan for 2 seconds
                isScanning = true;
                float scanDuration = 2f;
                float elapsedTime = 0f;
                
                while (elapsedTime < scanDuration)
                {
                    if (playerTransform != null)
                    {
                        // Aim beam at player
                        Vector3 directionToPlayer = (playerTransform.position - firePoint.position).normalized;
                        currentScanBeam.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                        
                        // Check if beam hits player
                        RaycastHit hit;
                        if (Physics.Raycast(firePoint.position, directionToPlayer, out hit, attackRange))
                        {
                            if (hit.transform == playerTransform && canDamagePlayer)
                            {
                                // Damage player at reduced rate while scanning
                                playerController?.TakeDamage(damageAmount * Time.deltaTime * 0.5f, transform.position);
                            }
                        }
                    }
                    
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                isScanning = false;
                
                // Destroy beam
                if (currentScanBeam != null)
                {
                    Destroy(currentScanBeam);
                    currentScanBeam = null;
                }
            }
        }

        private IEnumerator PerformProjectileAttack()
        {
            // Fire burst of projectiles
            for (int i = 0; i < burstCount; i++)
            {
                if (projectilePrefab != null && firePoint != null)
                {
                    // Calculate direction with slight variation for accuracy
                    Vector3 direction = (playerTransform.position - firePoint.position).normalized;
                    direction += new Vector3(
                        UnityEngine.Random.Range(-accuracyVariation, accuracyVariation),
                        UnityEngine.Random.Range(-accuracyVariation, accuracyVariation),
                        UnityEngine.Random.Range(-accuracyVariation, accuracyVariation)
                    );
                    direction.Normalize();
                    
                    // Create projectile
                    GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
                    Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
                    if (projectileRb != null)
                    {
                        projectileRb.velocity = direction * projectileSpeed;
                    }
                    
                    // Set up projectile
                    DroneProjectile droneProjectile = projectile.GetComponent<DroneProjectile>();
                    if (droneProjectile != null)
                    {
                        droneProjectile.Initialize(damageAmount, this, canDamagePlayer);
                    }
                    else
                    {
                        // Auto destroy if not a proper drone projectile
                        Destroy(projectile, 5f);
                    }
                }
                
                // Wait between shots
                yield return new WaitForSeconds(burstInterval);
            }
        }

        private IEnumerator PerformDirectAttack()
        {
            if (playerTransform != null)
            {
                // Move quickly toward player
                Vector3 startPosition = transform.position;
                Vector3 targetPosition = playerTransform.position;
                float rushSpeed = chaseSpeed * 1.5f;
                float rushDuration = 0.5f;
                float elapsedTime = 0f;
                
                while (elapsedTime < rushDuration)
                {
                    float t = elapsedTime / rushDuration;
                    rb.velocity = Vector3.Lerp(Vector3.zero, (targetPosition - startPosition).normalized * rushSpeed, t);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
                
                // Check for collision with player
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer < 1.5f && canDamagePlayer)
                {
                    playerController?.TakeDamage(damageAmount, transform.position);
                    
                    // Visual effect for impact
                    if (damageFX != null)
                    {
                        Instantiate(damageFX, playerTransform.position, Quaternion.identity);
                    }
                }
                
                // Slow down after attack
                elapsedTime = 0f;
                while (elapsedTime < 0.5f)
                {
                    float t = elapsedTime / 0.5f;
                    rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, t);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
        }

        private IEnumerator PerformFlashAttack()
        {
            // Create flash effect
            GameObject flash = new GameObject("FlashEffect");
            Light flashLight = flash.AddComponent<Light>();
            flashLight.type = LightType.Point;
            flashLight.range = 10f;
            flashLight.intensity = 0f;
            flashLight.color = Color.white;
            flash.transform.position = transform.position;
            
            // Flash intensity ramp up
            float flashDuration = 0.5f;
            float elapsedTime = 0f;
            
            while (elapsedTime < flashDuration)
            {
                float t = elapsedTime / flashDuration;
                flashLight.intensity = Mathf.Lerp(0f, 8f, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // At peak intensity, damage/blind player if close enough
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
                if (distanceToPlayer < attackRange && canDamagePlayer)
                {
                    // Apply damage and visual effect
                    playerController?.TakeDamage(damageAmount * 0.5f, transform.position);
                    
                    // Here you would trigger a "blinded" effect on the player
                    // This might be handled by game manager or player controller
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.ApplyScreenEffect("Blinded", 2f);
                    }
                }
            }
            
            // Flash intensity ramp down
            elapsedTime = 0f;
            while (elapsedTime < flashDuration)
            {
                float t = elapsedTime / flashDuration;
                flashLight.intensity = Mathf.Lerp(8f, 0f, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            Destroy(flash);
        }

        private void SetupDroneByType()
        {
            switch (droneType)
            {
                case DroneType.Surveillance:
                    // Surveillance drones are faster but deal less damage
                    moveSpeed = 5f;
                    damageAmount = 5f;
                    detectionRange = 12f;
                    attackRange = 8f;
                    canAttack = true;
                    break;
                
                case DroneType.Combat:
                    // Combat drones are slower but deal more damage
                    moveSpeed = 4f;
                    damageAmount = 15f;
                    detectionRange = 15f;
                    attackRange = 10f;
                    canAttack = true;
                    break;
                
                case DroneType.Security:
                    // Security drones are balanced
                    moveSpeed = 4.5f;
                    damageAmount = 10f;
                    detectionRange = 10f;
                    attackRange = 5f;
                    canAttack = true;
                    break;
                
                case DroneType.Advertising:
                    // Advertising drones move erratically and use flash attacks
                    moveSpeed = 5.5f;
                    damageAmount = 8f;
                    detectionRange = 8f;
                    attackRange = 6f;
                    canAttack = true;
                    break;
                
                case DroneType.Trainer:
                    // Trainer drones chase but don't attack
                    moveSpeed = 4f;
                    chaseSpeed = 5f;
                    damageAmount = 0f;
                    detectionRange = 20f;
                    attackRange = 0f;
                    canAttack = false;
                    break;
            }

            // Tutorial drones never damage player
            if (isTutorialDrone)
            {
                canDamagePlayer = false;
            }
        }

        private void SetDroneState(DroneState newState)
        {
            // Don't change state if it's the same
            if (newState == currentState)
                return;
            
            // Exit actions for current state
            switch (currentState)
            {
                case DroneState.Idle:
                    break;
                case DroneState.Patrol:
                    break;
                case DroneState.Wander:
                    break;
                case DroneState.Chase:
                    break;
                case DroneState.Attack:
                    isAttacking = false;
                    break;
                case DroneState.Search:
                    break;
                case DroneState.Retreat:
                    break;
            }
            
            // Update state
            currentState = newState;
            
            // Enter actions for new state
            switch (newState)
            {
                case DroneState.Idle:
                    rb.velocity = Vector3.zero;
                    break;
                
                case DroneState.Patrol:
                    // Make sure we have a valid patrol index
                    if (patrolPoints.Length > 0)
                    {
                        currentPatrolIndex = 0;
                    }
                    break;
                
                case DroneState.Wander:
                    GenerateWanderTarget();
                    break;
                
                case DroneState.Chase:
                    if (audioSource && detectionSound != null)
                    {
                        audioSource.PlayOneShot(detectionSound);
                    }
                    break;
                
                case DroneState.Attack:
                    rb.velocity = Vector3.zero;
                    break;
                
                case DroneState.Search:
                    searchTimer = 10f; // Search for 10 seconds
                    rb.velocity = Vector3.zero;
                    break;
                
                case DroneState.Retreat:
                    break;
            }
        }

        private void UpdateStatusLight()
        {
            if (statusLight == null)
                return;

            // Set base color based on state
            Color targetColor = idleColor;
            switch (currentState)
            {
                case DroneState.Idle:
                case DroneState.Patrol:
                case DroneState.Wander:
                    targetColor = idleColor;
                    break;
                
                case DroneState.Chase:
                case DroneState.Attack:
                    targetColor = alertColor;
                    break;
                
                case DroneState.Search:
                    targetColor = searchColor;
                    break;
            }
            
            // Apply special effects based on state
            switch (currentState)
            {
                case DroneState.Idle:
                    // Steady light
                    statusLight.intensity = 1.0f;
                    break;
                
                case DroneState.Patrol:
                case DroneState.Wander:
                    // Gentle pulsing
                    statusLight.intensity = 0.8f + Mathf.Sin(Time.time * 2f) * 0.2f;
                    break;
                
                case DroneState.Chase:
                case DroneState.Attack:
                    // Rapid pulsing
                    statusLight.intensity = 0.7f + Mathf.PingPong(Time.time * 8f, 1f) * 0.5f;
                    break;
                
                case DroneState.Search:
                    // Sweeping pattern
                    pulseTimer += Time.deltaTime;
                    statusLight.intensity = Mathf.Clamp01(Mathf.Sin(pulseTimer * 4f));
                    break;
            }
            
            // Smooth color transition
            currentLightColor = Color.Lerp(currentLightColor, targetColor, Time.deltaTime * 5f);
            statusLight.color = currentLightColor;
        }
        #endregion

        #region Public Methods
        public void TakeDamage(float damage, Vector3 hitPoint)
        {
            if (isInvulnerable || (isTutorialDrone && !destructibleInTutorial))
                return;

            currentHealth -= damage;
            
            // Trigger event
            OnDroneDamaged?.Invoke(this);
            
            // Play damage sound
            if (audioSource && damageSound != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
            
            // Spawn damage effect
            if (damageFX != null)
            {
                Instantiate(damageFX, hitPoint, Quaternion.identity);
            }
            
            // Check for destruction
            if (currentHealth <= 0)
            {
                DestroyDrone();
            }
            else
            {
                // Stun briefly
                StartCoroutine(StunDroneCoroutine(0.2f));
            }
        }

        public void StunDrone(float duration)
        {
            isStunned = true;
            stunTimer = duration;
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        private IEnumerator StunDroneCoroutine(float duration)
        {
            // Applica lo stun
            StunDrone(duration);
            
            // Attendi per la durata dello stun
            yield return new WaitForSeconds(duration);
            
            // Ripristina lo stato normale
            isStunned = false;
            rb.isKinematic = false;
        }

        public void DestroyDrone()
        {
            // Trigger event
            OnDroneDestroyed?.Invoke(this);
            
            // Play destruction sound
            if (audioSource && destructionSound != null)
            {
                audioSource.PlayOneShot(destructionSound);
            }
            
            // Spawn destruction effect
            if (destructionFX != null)
            {
                Instantiate(destructionFX, transform.position, Quaternion.identity);
            }
            
            // Disable collision and visibility
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }
            
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = false;
            }
            
            // Destroy object after sound finishes
            float destroyDelay = 0.5f;
            if (audioSource && destructionSound != null)
            {
                destroyDelay = Mathf.Max(destroyDelay, destructionSound.length);
            }
            
            Destroy(gameObject, destroyDelay);
        }

        public void SetTarget(Transform target)
        {
            activeTarget = target.gameObject;
            if (target == playerTransform)
            {
                playerDetected = true;
                SetDroneState(DroneState.Chase);
            }
        }

        public void ClearTarget()
        {
            activeTarget = null;
            playerDetected = false;
            SetDroneState(DroneState.Idle);
        }

        public void StartChasing()
        {
            if (playerTransform != null)
            {
                playerDetected = true;
                playerLastSeenPosition = playerTransform.position;
                playerLastSeenTime = Time.time;
                SetDroneState(DroneState.Chase);
            }
        }

        public void StopChasing()
        {
            playerDetected = false;
            SetDroneState(DroneState.Patrol);
        }

        public void ReturnToSpawn()
        {
            SetDroneState(DroneState.Retreat);
        }

        public void ResetDrone()
        {
            // Reset state
            currentHealth = maxHealth;
            isStunned = false;
            isAttacking = false;
            playerDetected = false;
            
            // Reset position
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            rb.velocity = Vector3.zero;
            
            // Reset state machine
            SetDroneState(DroneState.Idle);
            
            // Reset materials
            if (defaultMaterial != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        if (materials[i] == alertMaterial)
                        {
                            materials[i] = defaultMaterial;
                        }
                    }
                    renderer.materials = materials;
                }
            }
            
            // Clean up any active beam
            if (currentScanBeam != null)
            {
                Destroy(currentScanBeam);
                currentScanBeam = null;
            }
        }

        // Used by TutorialManager to control trainer drones
        public void SetTutorialBehavior(bool active, TutorialDroneBehavior behavior)
        {
            if (!isTutorialDrone)
                return;
                
            switch (behavior)
            {
                case TutorialDroneBehavior.Idle:
                    SetDroneState(DroneState.Idle);
                    break;
                
                case TutorialDroneBehavior.FollowPlayer:
                    StartChasing();
                    break;
                
                case TutorialDroneBehavior.PatrolArea:
                    SetDroneState(DroneState.Patrol);
                    break;
                
                case TutorialDroneBehavior.WanderArea:
                    SetDroneState(DroneState.Wander);
                    break;
            }
        }
        #endregion
    }

    // Supporting enum for drone types
    public enum DroneType
    {
        Surveillance,  // Basic patrol drones with scanning beams
        Combat,        // Attack drones with projectile weapons
        Security,      // Heavy security drones with direct attacks
        Advertising,   // Advertising drones with flashbang-like attacks
        Trainer        // Tutorial-specific drones that chase but don't damage
    }

    // Enum for drone movement patterns
    public enum DroneMovementPattern
    {
        Patrol,     // Move between waypoints
        Wander,     // Random movement in area
        Stationary  // Stay in place but rotate
    }

    // Enum for drone states
    public enum DroneState
    {
        Idle,      // Hovering in place
        Patrol,    // Following patrol path
        Wander,    // Moving randomly
        Chase,     // Pursuing player
        Attack,    // Attacking player
        Search,    // Looking for lost player
        Retreat    // Returning to spawn point
    }

    // Enum for tutorial drone behaviors
    public enum TutorialDroneBehavior
    {
        Idle,
        FollowPlayer,
        PatrolArea,
        WanderArea
    }
}