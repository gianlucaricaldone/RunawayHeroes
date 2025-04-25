using UnityEngine;

namespace RunawayHeroes
{
    /// <summary>
    /// Terminal that disables security systems when hacked
    /// </summary>
    public class SecurityTerminal : HackableTerminal
    {
        [Header("Security System")]
        [SerializeField] private SecuritySystem securitySystem;
        [SerializeField] private SecuritySystemType securityType = SecuritySystemType.Cameras;
        [SerializeField] private float disableDuration = 30f; // How long the security stays disabled
        [SerializeField] private bool permanentDisable = false; // If true, security won't reactivate
        [SerializeField] private string notificationMessage = "Security systems disabled";
        
        private bool isSecurityDisabled = false;
        private float disableTimer = 0f;
        
        /// <summary>
        /// Disable specific security system when hacked
        /// </summary>
        protected override void ActivateTerminalEffect()
        {
            base.ActivateTerminalEffect();
            
            if (securitySystem != null)
            {
                securitySystem.DisableSecuritySystem(securityType);
                isSecurityDisabled = true;
                
                // Show notification
                UIManager.Instance?.ShowNotification(notificationMessage);
                
                // Start timer for re-enabling if not permanent
                if (!permanentDisable)
                {
                    disableTimer = disableDuration;
                    StartCoroutine(ReEnableSecuritySystem());
                }
            }
        }
        
        /// <summary>
        /// Re-enable security system after duration
        /// </summary>
        private System.Collections.IEnumerator ReEnableSecuritySystem()
        {
            while (disableTimer > 0f)
            {
                disableTimer -= Time.deltaTime;
                yield return null;
            }
            
            if (securitySystem != null && isSecurityDisabled)
            {
                securitySystem.EnableSecuritySystem(securityType);
                isSecurityDisabled = false;
                
                // Show notification
                UIManager.Instance?.ShowNotification("Security systems reactivated");
                
                // Reset terminal
                ResetTerminal();
            }
        }
        
        /// <summary>
        /// Override reset to also re-enable security if needed
        /// </summary>
        public override void ResetTerminal()
        {
            base.ResetTerminal();
            
            // If security is disabled, re-enable it
            if (isSecurityDisabled && securitySystem != null)
            {
                securitySystem.EnableSecuritySystem(securityType);
                isSecurityDisabled = false;
            }
        }
        
        /// <summary>
        /// Types of security systems
        /// </summary>
        public enum SecuritySystemType
        {
            Cameras,
            Lasers,
            Turrets,
            Drones,
            AllSystems
        }
    }
    
    /// <summary>
    /// Simple UI manager interface to avoid compilation errors
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        // Singleton instance
        public static UIManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void ShowNotification(string message)
        {
            // Implementation would be in the actual UIManager class
            Debug.Log($"Notification: {message}");
        }
    }
    
    /// <summary>
    /// Extended security system interface
    /// </summary>
    public class SecuritySystem : MonoBehaviour
    {
        public void DisableSecuritySystem(SecurityTerminal.SecuritySystemType systemType)
        {
            // Implementation would be in the actual SecuritySystem class
            Debug.Log($"Security system disabled: {systemType}");
        }
        
        public void EnableSecuritySystem(SecurityTerminal.SecuritySystemType systemType)
        {
            // Implementation would be in the actual SecuritySystem class
            Debug.Log($"Security system enabled: {systemType}");
        }
    }
}