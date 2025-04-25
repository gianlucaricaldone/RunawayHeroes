using UnityEngine;
using System.Collections;

namespace RunawayHeroes
{
    /// <summary>
    /// Terminal that opens a door or barrier when hacked
    /// </summary>
    public class DoorTerminal : HackableTerminal
    {
        [Header("Door Control")]
        [SerializeField] private GameObject door;
        [SerializeField] private Transform openPosition;
        [SerializeField] private Transform closedPosition;
        [SerializeField] private float doorMoveSpeed = 2f;
        [SerializeField] private AudioClip doorOpenSound;
        [SerializeField] private AudioClip doorCloseSound;
        [SerializeField] private bool autoClose = false;
        [SerializeField] private float autoCloseTime = 10f;
        [SerializeField] private bool slidingDoor = true; // If false, door will rotate
        [SerializeField] private string openNotification = "Door unlocked";
        
        private Coroutine doorMovementCoroutine;
        private AudioSource doorAudioSource;
        private bool isDoorOpen = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Get or create door audio source
            if (door != null)
            {
                doorAudioSource = door.GetComponent<AudioSource>();
                if (doorAudioSource == null)
                {
                    doorAudioSource = door.AddComponent<AudioSource>();
                }
            }
            
            // Initialize door position if needed
            if (door != null && closedPosition != null && !isUnlocked)
            {
                door.transform.position = closedPosition.position;
                door.transform.rotation = closedPosition.rotation;
            }
        }
        
        /// <summary>
        /// Open door when terminal is hacked
        /// </summary>
        protected override void ActivateTerminalEffect()
        {
            base.ActivateTerminalEffect();
            
            OpenDoor();
            
            // Show notification
            UIManager.Instance?.ShowNotification(openNotification);
        }
        
        /// <summary>
        /// Open the door
        /// </summary>
        public void OpenDoor()
        {
            if (door != null && openPosition != null && !isDoorOpen)
            {
                // Stop any current door movement
                if (doorMovementCoroutine != null)
                {
                    StopCoroutine(doorMovementCoroutine);
                }
                
                // Start door opening
                if (slidingDoor)
                {
                    doorMovementCoroutine = StartCoroutine(MoveDoor(door.transform.position, openPosition.position));
                }
                else
                {
                    doorMovementCoroutine = StartCoroutine(RotateDoor(door.transform.rotation, openPosition.rotation));
                }
                
                // Play sound
                if (doorAudioSource != null && doorOpenSound != null)
                {
                    doorAudioSource.PlayOneShot(doorOpenSound);
                }
                
                isDoorOpen = true;
                
                // Set auto-close if needed
                if (autoClose)
                {
                    StartCoroutine(AutoCloseDoor());
                }
            }
        }
        
        /// <summary>
        /// Close the door
        /// </summary>
        public void CloseDoor()
        {
            if (door != null && closedPosition != null && isDoorOpen)
            {
                // Stop any current door movement
                if (doorMovementCoroutine != null)
                {
                    StopCoroutine(doorMovementCoroutine);
                }
                
                // Start door closing
                if (slidingDoor)
                {
                    doorMovementCoroutine = StartCoroutine(MoveDoor(door.transform.position, closedPosition.position));
                }
                else
                {
                    doorMovementCoroutine = StartCoroutine(RotateDoor(door.transform.rotation, closedPosition.rotation));
                }
                
                // Play sound
                if (doorAudioSource != null && doorCloseSound != null)
                {
                    doorAudioSource.PlayOneShot(doorCloseSound);
                }
                
                isDoorOpen = false;
            }
        }
        
        /// <summary>
        /// Move door smoothly between positions
        /// </summary>
        private IEnumerator MoveDoor(Vector3 startPos, Vector3 endPos)
        {
            float journeyLength = Vector3.Distance(startPos, endPos);
            float startTime = Time.time;
            
            while (Vector3.Distance(door.transform.position, endPos) > 0.01f)
            {
                float distCovered = (Time.time - startTime) * doorMoveSpeed;
                float fractionOfJourney = distCovered / journeyLength;
                
                door.transform.position = Vector3.Lerp(startPos, endPos, fractionOfJourney);
                
                yield return null;
            }
            
            // Ensure final position
            door.transform.position = endPos;
            doorMovementCoroutine = null;
        }
        
        /// <summary>
        /// Rotate door smoothly between rotations (for hinged doors)
        /// </summary>
        private IEnumerator RotateDoor(Quaternion startRot, Quaternion endRot)
        {
            float journeyLength = Quaternion.Angle(startRot, endRot);
            float startTime = Time.time;
            
            while (Quaternion.Angle(door.transform.rotation, endRot) > 0.1f)
            {
                float distCovered = (Time.time - startTime) * doorMoveSpeed * 50f; // Adjusted for rotation
                float fractionOfJourney = distCovered / journeyLength;
                
                door.transform.rotation = Quaternion.Slerp(startRot, endRot, fractionOfJourney);
                
                yield return null;
            }
            
            // Ensure final rotation
            door.transform.rotation = endRot;
            doorMovementCoroutine = null;
        }
        
        /// <summary>
        /// Auto-close the door after delay
        /// </summary>
        private IEnumerator AutoCloseDoor()
        {
            yield return new WaitForSeconds(autoCloseTime);
            
            CloseDoor();
            
            // Reset terminal
            ResetTerminal();
        }
        
        /// <summary>
        /// Override reset to handle door state
        /// </summary>
        public override void ResetTerminal()
        {
            base.ResetTerminal();
            
            // Close door if it's open
            if (isDoorOpen)
            {
                CloseDoor();
            }
        }
    }
}