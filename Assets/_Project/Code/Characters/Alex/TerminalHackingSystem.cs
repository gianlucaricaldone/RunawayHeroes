using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RunawayHeroes
{
    /// <summary>
    /// Component for terminal hacking, used by Alex to interface with environment systems
    /// </summary>
    public class TerminalHackingSystem : MonoBehaviour
    {
        [Header("Hacking Settings")]
        [SerializeField] private float hackingTime = 3f;
        [SerializeField] private float hackingRange = 2f;
        [SerializeField] private LayerMask terminalLayers;
        
        [Header("Hacking UI")]
        [SerializeField] private GameObject hackingUI;
        [SerializeField] private Slider hackingProgressBar;
        
        [Header("Hacking Effects")]
        [SerializeField] private ParticleSystem hackingParticles;
        [SerializeField] private AudioClip startHackingSound;
        [SerializeField] private AudioClip hackingProgressSound;
        [SerializeField] private AudioClip hackingSuccessSound;
        [SerializeField] private AudioClip hackingFailSound;
        
        // Private fields
        private HackableTerminal currentTerminal;
        private float hackingProgress = 0f;
        private Coroutine hackingCoroutine;
        private AudioSource audioSource;
        
        /// <summary>
        /// Whether currently hacking a terminal
        /// </summary>
        public bool IsHacking { get; private set; }

        private void Awake()
        {
            // Get audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Hide UI at start
            if (hackingUI != null)
            {
                hackingUI.SetActive(false);
            }
        }

        /// <summary>
        /// Start hacking a terminal
        /// </summary>
        public void StartHacking(HackableTerminal terminal)
        {
            if (IsHacking || terminal == null) return;
            
            // Check if within range
            if (Vector3.Distance(transform.position, terminal.transform.position) > hackingRange)
            {
                return;
            }
            
            IsHacking = true;
            currentTerminal = terminal;
            hackingProgress = 0f;
            
            // Show UI
            if (hackingUI != null)
            {
                hackingUI.SetActive(true);
                if (hackingProgressBar != null)
                {
                    hackingProgressBar.value = 0f;
                }
            }
            
            // Play start sound
            if (startHackingSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(startHackingSound);
            }
            
            // Start particles
            if (hackingParticles != null)
            {
                hackingParticles.Play();
            }
            
            // Notify terminal
            currentTerminal.OnHackingStarted();
            
            // Start hacking coroutine
            hackingCoroutine = StartCoroutine(HackingProcess());
        }

        /// <summary>
        /// Cancel current hacking attempt
        /// </summary>
        public void CancelHacking()
        {
            if (!IsHacking) return;
            
            // Stop coroutine
            if (hackingCoroutine != null)
            {
                StopCoroutine(hackingCoroutine);
                hackingCoroutine = null;
            }
            
            // Hide UI
            if (hackingUI != null)
            {
                hackingUI.SetActive(false);
            }
            
            // Stop particles
            if (hackingParticles != null)
            {
                hackingParticles.Stop();
            }
            
            // Play fail sound
            if (hackingFailSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hackingFailSound);
            }
            
            // Notify terminal
            if (currentTerminal != null)
            {
                currentTerminal.OnHackingCancelled();
                currentTerminal = null;
            }
            
            IsHacking = false;
        }

        /// <summary>
        /// Coroutine to handle the hacking process
        /// </summary>
        private IEnumerator HackingProcess()
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < hackingTime)
            {
                // Check if still in range
                if (currentTerminal == null || 
                    Vector3.Distance(transform.position, currentTerminal.transform.position) > hackingRange)
                {
                    CancelHacking();
                    yield break;
                }
                
                // Update progress
                elapsedTime += Time.deltaTime;
                hackingProgress = elapsedTime / hackingTime;
                
                // Update UI
                if (hackingProgressBar != null)
                {
                    hackingProgressBar.value = hackingProgress;
                }
                
                // Play progress sound
                if (hackingProgressSound != null && audioSource != null && Mathf.Floor(hackingProgress * 10) != Mathf.Floor((hackingProgress - Time.deltaTime / hackingTime) * 10))
                {
                    audioSource.PlayOneShot(hackingProgressSound, 0.5f);
                }
                
                yield return null;
            }
            
            // Hacking complete!
            if (hackingSuccessSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(hackingSuccessSound);
            }
            
            // Hide UI
            if (hackingUI != null)
            {
                hackingUI.SetActive(false);
            }
            
            // Notify terminal
            if (currentTerminal != null)
            {
                currentTerminal.OnHackingCompleted();
                currentTerminal = null;
            }
            
            IsHacking = false;
        }

        /// <summary>
        /// Check for terminals in range
        /// </summary>
        public HackableTerminal FindNearestTerminal()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, hackingRange, terminalLayers);
            
            HackableTerminal nearestTerminal = null;
            float closestDistance = float.MaxValue;
            
            foreach (Collider collider in colliders)
            {
                HackableTerminal terminal = collider.GetComponent<HackableTerminal>();
                if (terminal != null && terminal.IsHackable)
                {
                    float distance = Vector3.Distance(transform.position, terminal.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        nearestTerminal = terminal;
                    }
                }
            }
            
            return nearestTerminal;
        }
    }
}