using UnityEngine;
using System.Collections;

namespace RunawayHeroes
{
    /// <summary>
    /// Base class for hackable terminals that Alex can interact with
    /// </summary>
    public class HackableTerminal : MonoBehaviour
    {
        [Header("Terminal Settings")]
        [SerializeField] protected float hackingDifficulty = 1f; // Multiplier for hacking time
        [SerializeField] protected bool isHackable = true;
        [SerializeField] protected string terminalID;
        [SerializeField] protected string terminalName = "Generic Terminal";
        [SerializeField] protected string successMessage = "Access Granted";
        
        [Header("Terminal States")]
        [SerializeField] protected GameObject lockedVisuals;
        [SerializeField] protected GameObject unlockedVisuals;
        [SerializeField] protected GameObject hackingVisuals;
        
        [Header("Terminal Effects")]
        [SerializeField] protected AudioClip lockedSound;
        [SerializeField] protected AudioClip unlockSound;
        [SerializeField] protected AudioClip hackingSound;
        [SerializeField] protected ParticleSystem unlockParticles;
        [SerializeField] protected Light terminalLight;
        [SerializeField] protected Color lockedColor = Color.red;
        [SerializeField] protected Color hackingColor = Color.yellow;
        [SerializeField] protected Color unlockedColor = Color.green;
        
        // Terminal state
        protected bool isUnlocked = false;
        protected bool isBeingHacked = false;
        protected AudioSource audioSource;
        
        /// <summary>
        /// Whether this terminal can be hacked
        /// </summary>
        public bool IsHackable => isHackable && !isUnlocked;
        
        /// <summary>
        /// Terminal identifier
        /// </summary>
        public string TerminalID => terminalID;
        
        /// <summary>
        /// Terminal display name
        /// </summary>
        public string TerminalName => terminalName;

        protected virtual void Awake()
        {
            // Get audio source
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            // Set initial visuals
            UpdateVisuals();
        }

        /// <summary>
        /// Update terminal visuals based on current state
        /// </summary>
        protected virtual void UpdateVisuals()
        {
            // Update object visuals
            if (lockedVisuals != null) lockedVisuals.SetActive(!isUnlocked && !isBeingHacked);
            if (unlockedVisuals != null) unlockedVisuals.SetActive(isUnlocked);
            if (hackingVisuals != null) hackingVisuals.SetActive(isBeingHacked);
            
            // Update light color
            if (terminalLight != null)
            {
                if (isUnlocked)
                {
                    terminalLight.color = unlockedColor;
                }
                else if (isBeingHacked)
                {
                    terminalLight.color = hackingColor;
                }
                else
                {
                    terminalLight.color = lockedColor;
                }
            }
        }

        /// <summary>
        /// Called when hacking begins
        /// </summary>
        public virtual void OnHackingStarted()
        {
            isBeingHacked = true;
            
            // Play sound
            if (hackingSound != null && audioSource != null)
            {
                audioSource.clip = hackingSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            
            // Update visuals
            UpdateVisuals();
        }

        /// <summary>
        /// Called when hacking is cancelled
        /// </summary>
        public virtual void OnHackingCancelled()
        {
            isBeingHacked = false;
            
            // Stop sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            
            // Play locked sound
            if (lockedSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(lockedSound);
            }
            
            // Update visuals
            UpdateVisuals();
        }

        /// <summary>
        /// Called when hacking is successfully completed
        /// </summary>
        public virtual void OnHackingCompleted()
        {
            isBeingHacked = false;
            isUnlocked = true;
            
            // Stop hacking sound
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
                audioSource.loop = false;
            }
            
            // Play unlock sound
            if (unlockSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(unlockSound);
            }
            
            // Play particles
            if (unlockParticles != null)
            {
                unlockParticles.Play();
            }
            
            // Update visuals
            UpdateVisuals();
            
            // Activate terminal functionality
            ActivateTerminalEffect();
        }

        /// <summary>
        /// Activate the terminal's specific effect when successfully hacked
        /// </summary>
        protected virtual void ActivateTerminalEffect()
        {
            // Base implementation - override in child classes
            Debug.Log($"Terminal {terminalName} ({terminalID}) has been hacked: {successMessage}");
        }

        /// <summary>
        /// Reset the terminal to locked state
        /// </summary>
        public virtual void ResetTerminal()
        {
            isUnlocked = false;
            isBeingHacked = false;
            UpdateVisuals();
        }
    }
}