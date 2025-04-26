using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace RunawayHeroes
{
    /// <summary>
    /// Terminal that provides map or intel information when hacked
    /// </summary>
    public class IntelTerminal : HackableTerminal
    {
        [Header("Intel Data")]
        [SerializeField] private string[] intelData;
        [SerializeField] private GameObject intelUI;
        [SerializeField] private float displayTime = 10f;
        [SerializeField] private bool revealMapArea = false;
        [SerializeField] private string mapAreaID;
        [SerializeField] private bool unlockWaypoint = false;
        [SerializeField] private string waypointID;
        [SerializeField] private string waypointName;
        [SerializeField] private bool addCollectible = false;
        [SerializeField] private CollectibleType collectibleType = CollectibleType.Document;
        [SerializeField] private string collectibleID;
        [SerializeField] private string collectibleName;
        
        private bool intelRevealed = false;
        
        /// <summary>
        /// Display intel information when hacked
        /// </summary>
        protected override void ActivateTerminalEffect()
        {
            base.ActivateTerminalEffect();
            
            if (intelRevealed) return;
            intelRevealed = true;
            
            // Display intel UI
            if (intelUI != null)
            {
                intelUI.SetActive(true);
                
                // Set intel text if possible
                Text intelText = intelUI.GetComponentInChildren<Text>();
                if (intelText != null && intelData.Length > 0)
                {
                    intelText.text = string.Join("\n", intelData);
                }
                
                // Auto-hide after delay
                StartCoroutine(HideIntelUI());
            }
            
            // Reveal map area if needed
            if (revealMapArea)
            {
                MapController mapController = FindAnyObjectByType<MapController>();
                if (mapController != null)
                {
                    mapController.RevealMapArea(mapAreaID);
                    UIManager.Instance?.ShowNotification($"Map area revealed: {mapAreaID}");
                }
            }
            
            // Unlock waypoint if needed
            if (unlockWaypoint)
            {
                WaypointSystem waypointSystem = FindAnyObjectByType<WaypointSystem>();
                if (waypointSystem != null)
                {
                    waypointSystem.UnlockWaypoint(waypointID, waypointName);
                    UIManager.Instance?.ShowNotification($"New waypoint unlocked: {waypointName}");
                }
            }
            
            // Add collectible to inventory if needed
            if (addCollectible)
            {
                InventorySystem inventorySystem = FindAnyObjectByType<InventorySystem>();
                if (inventorySystem != null)
                {
                    inventorySystem.AddCollectible(collectibleType, collectibleID, collectibleName);
                    UIManager.Instance?.ShowNotification($"Added to inventory: {collectibleName}");
                }
            }
        }
        
        /// <summary>
        /// Hide intel UI after delay
        /// </summary>
        private IEnumerator HideIntelUI()
        {
            yield return new WaitForSeconds(displayTime);
            
            if (intelUI != null)
            {
                intelUI.SetActive(false);
            }
        }
        
        /// <summary>
        /// Reset intel revealed state
        /// </summary>
        public override void ResetTerminal()
        {
            base.ResetTerminal();
            intelRevealed = false;
        }
        
        /// <summary>
        /// Types of collectibles that can be added to inventory
        /// </summary>
        public enum CollectibleType
        {
            Document,
            AudioLog,
            Keycard,
            Password,
            Blueprint,
            Map
        }
    }
    
    /// <summary>
    /// Simple map controller interface to avoid compilation errors
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public void RevealMapArea(string areaID)
        {
            // Implementation would be in the actual MapController class
            Debug.Log($"Map area revealed: {areaID}");
        }
    }
    
    /// <summary>
    /// Simple waypoint system interface to avoid compilation errors
    /// </summary>
    public class WaypointSystem : MonoBehaviour
    {
        public void UnlockWaypoint(string waypointID, string waypointName)
        {
            // Implementation would be in the actual WaypointSystem class
            Debug.Log($"Waypoint unlocked: {waypointName} ({waypointID})");
        }
    }
    
    /// <summary>
    /// Simple inventory system interface to avoid compilation errors
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        public void AddCollectible(IntelTerminal.CollectibleType type, string itemID, string itemName)
        {
            // Implementation would be in the actual InventorySystem class
            Debug.Log($"Added to inventory: {itemName} ({itemID}) of type {type}");
        }
    }
}