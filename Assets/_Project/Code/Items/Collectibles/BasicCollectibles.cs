using UnityEngine;
using RunawayHeroes.Characters;

namespace RunawayHeroes.Items
{
    /// <summary>
    /// Implementazione base per oggetti collezionabili nel gioco.
    /// Questo script gestisce monete, gemme e altri oggetti raccoglibili.
    /// </summary>
    public class BasicCollectible : MonoBehaviour, Collectible
    {
        [Header("Collectible Settings")]
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Coin;
        [SerializeField] private int value = 1;
        [SerializeField] private bool autoRotate = true;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.2f;
        [SerializeField] private float bobFrequency = 1f;
        
        [Header("FX")]
        [SerializeField] private ParticleSystem collectFX;
        [SerializeField] private AudioClip collectSound;
        [SerializeField] private GameObject visualObject;
        
        // Private variables
        private Vector3 startPosition;
        private AudioSource audioSource;
        private bool isCollected = false;
        
        private void Awake()
        {
            // Get or add audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && collectSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
            }
            
            // Save start position for bobbing animation
            startPosition = transform.position;
        }
        
        private void Start()
        {
            // Setup based on collectible type
            SetupAppearance();
        }
        
        private void Update()
        {
            if (isCollected || !gameObject.activeInHierarchy)
                return;
                
            // Rotate the collectible
            if (autoRotate && visualObject != null)
            {
                visualObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Bob up and down
            if (bobAmplitude > 0)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // Check if this is the player
            if (other.CompareTag("Player") && !isCollected)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    // Let the player handle the collectible
                    // This is a secondary collection method in case the direct call in 
                    // PlayerController.OnControllerColliderHit fails
                    Collect();
                }
            }
        }
        
        /// <summary>
        /// Configura l'aspetto del collezionabile in base al tipo
        /// </summary>
        private void SetupAppearance()
        {
            if (visualObject == null)
                return;
                
            // Set appearance based on type
            switch (collectibleType)
            {
                case CollectibleType.Coin:
                    // Gold material and small size
                    ApplyMaterial(new Color(1f, 0.84f, 0), "Coin");
                    transform.localScale = Vector3.one * 0.5f;
                    break;
                    
                case CollectibleType.Gem:
                    // Blue shiny material and slightly larger
                    ApplyMaterial(new Color(0, 0.5f, 1f), "Gem");
                    transform.localScale = Vector3.one * 0.7f;
                    break;
                    
                case CollectibleType.HealthKit:
                    // Red cross like material
                    ApplyMaterial(new Color(1f, 0, 0), "HealthKit");
                    transform.localScale = Vector3.one * 0.8f;
                    break;
                    
                case CollectibleType.SpeedBoost:
                    // Yellow electric material
                    ApplyMaterial(new Color(1f, 1f, 0), "SpeedBoost");
                    transform.localScale = Vector3.one * 0.6f;
                    break;
                    
                case CollectibleType.Shield:
                    // Blue shield like material
                    ApplyMaterial(new Color(0.3f, 0.3f, 1f), "Shield");
                    transform.localScale = Vector3.one * 0.8f;
                    break;
                    
                case CollectibleType.Fragment:
                    // Purple glowing material
                    ApplyMaterial(new Color(0.7f, 0, 1f), "Fragment");
                    transform.localScale = Vector3.one * 0.6f;
                    break;
            }
        }
        
        /// <summary>
        /// Applica un materiale colorato all'oggetto visuale
        /// </summary>
        private void ApplyMaterial(Color color, string materialName)
        {
            Renderer renderer = visualObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Try to load material from resources if it exists
                Material material = Resources.Load<Material>($"Materials/Collectibles/{materialName}");
                
                // If material doesn't exist, create a new one with the specified color
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard"));
                    material.color = color;
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", color * 0.5f);
                }
                
                renderer.material = material;
            }
        }
        
        #region Collectible Interface
        /// <summary>
        /// Ottiene il tipo di collezionabile
        /// </summary>
        public CollectibleType GetCollectibleType()
        {
            return collectibleType;
        }
        
        /// <summary>
        /// Ottiene il valore del collezionabile
        /// </summary>
        public int GetValue()
        {
            return value;
        }
        
        /// <summary>
        /// Raccoglie il collezionabile attivando gli effetti di raccolta
        /// </summary>
        public void Collect()
        {
            if (isCollected)
                return;
                
            isCollected = true;
            
            // Disable collider
            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
            
            // Hide the visual object
            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }
            
            // Play collection effect
            if (collectFX != null)
            {
                collectFX.Play();
            }
            
            // Play collection sound
            if (audioSource != null && collectSound != null)
            {
                audioSource.PlayOneShot(collectSound);
            }
            
            // Destroy the object after effects are done
            float destroyDelay = 0f;
            
            if (collectFX != null)
            {
                destroyDelay = Mathf.Max(destroyDelay, collectFX.main.duration);
            }
            
            if (collectSound != null)
            {
                destroyDelay = Mathf.Max(destroyDelay, collectSound.length);
            }
            
            Destroy(gameObject, destroyDelay);
        }
        #endregion
    }
}