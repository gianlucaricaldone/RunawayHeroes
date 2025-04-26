using System;
using UnityEngine;
using UnityEngine.Events;

namespace RunawayHeroes.Core.Tutorial
{
    /// <summary>
    /// Gestisce gli obiettivi specifici per ciascun livello del tutorial,
    /// tracciando il progresso e segnalando il completamento.
    /// </summary>
    public class TutorialObjective : MonoBehaviour
    {
        [Header("Objective Settings")]
        [SerializeField] private string objectiveId = "objective_01";
        [SerializeField] private string objectiveName = "Complete the objective";
        [TextArea(2, 4)]
        [SerializeField] private string objectiveDescription = "Description of what to do";
        [SerializeField] private bool isRequired = true;
        [SerializeField] private bool autoActivate = true;
        
        [Header("Progress Tracking")]
        [SerializeField] private int requiredAmount = 1;
        [SerializeField] private bool showProgress = true;
        [SerializeField] private string progressFormat = "{0}/{1}"; // Es. "2/5"
        
        [Header("Completion Settings")]
        [SerializeField] private bool autoComplete = true;
        [SerializeField] private float completeDelay = 0.5f;
        [SerializeField] private AudioClip completionSound;
        [SerializeField] private GameObject completionVFX;
        
        [Header("Events")]
        [SerializeField] private UnityEvent onActivate;
        [SerializeField] private UnityEvent onProgress;
        [SerializeField] private UnityEvent onComplete;
        
        // Properties
        public string ObjectiveId => objectiveId;
        public string ObjectiveName => objectiveName;
        public string ObjectiveDescription => objectiveDescription;
        public bool IsRequired => isRequired;
        public bool IsActive { get; private set; }
        public bool IsCompleted { get; private set; }
        public int CurrentAmount { get; private set; }
        public int RequiredAmount => requiredAmount;
        public float Progress => (float)CurrentAmount / requiredAmount;
        public string ProgressText => string.Format(progressFormat, CurrentAmount, requiredAmount);
        
        // Events
        public event Action<TutorialObjective> OnObjectiveActivated;
        public event Action<TutorialObjective, int, int> OnObjectiveProgressed;
        public event Action<TutorialObjective> OnObjectiveCompleted;
        
        private void Start()
        {
            // Auto-attivazione se configurato
            if (autoActivate)
            {
                Activate();
            }
            
            // Registra l'obiettivo al TutorialManager
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.RegisterObjective(this);
            }
        }
        
        private void OnDestroy()
        {
            // Deregistra l'obiettivo
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.UnregisterObjective(this);
            }
        }
        
        /// <summary>
        /// Attiva l'obiettivo rendendolo tracciabile
        /// </summary>
        public void Activate()
        {
            if (IsActive)
                return;
            
            IsActive = true;
            CurrentAmount = 0;
            IsCompleted = false;
            
            // Notifica gli ascoltatori
            OnObjectiveActivated?.Invoke(this);
            onActivate?.Invoke();
            
            // Log
            Debug.Log($"Tutorial Objective Activated: {objectiveName} ({objectiveId})");
        }
        
        /// <summary>
        /// Incrementa il progresso dell'obiettivo
        /// </summary>
        /// <param name="amount">Quantità da incrementare</param>
        public void IncrementProgress(int amount = 1)
        {
            if (!IsActive || IsCompleted)
                return;
            
            CurrentAmount += amount;
            
            // Limita il progresso al massimo
            if (CurrentAmount > requiredAmount)
                CurrentAmount = requiredAmount;
            
            // Notifica gli ascoltatori del progresso
            OnObjectiveProgressed?.Invoke(this, CurrentAmount, requiredAmount);
            onProgress?.Invoke();
            
            // Log
            Debug.Log($"Tutorial Objective Progress: {objectiveName} - {CurrentAmount}/{requiredAmount}");
            
            // Verifica se l'obiettivo è stato completato
            if (autoComplete && CurrentAmount >= requiredAmount)
            {
                // Completa dopo un delay opzionale
                if (completeDelay > 0)
                {
                    Invoke(nameof(Complete), completeDelay);
                }
                else
                {
                    Complete();
                }
            }
        }
        
        /// <summary>
        /// Imposta il progresso dell'obiettivo a un valore specifico
        /// </summary>
        /// <param name="amount">Valore del progresso</param>
        public void SetProgress(int amount)
        {
            if (!IsActive || IsCompleted)
                return;
            
            CurrentAmount = Mathf.Clamp(amount, 0, requiredAmount);
            
            // Notifica gli ascoltatori del progresso
            OnObjectiveProgressed?.Invoke(this, CurrentAmount, requiredAmount);
            onProgress?.Invoke();
            
            // Log
            Debug.Log($"Tutorial Objective Progress Set: {objectiveName} - {CurrentAmount}/{requiredAmount}");
            
            // Verifica se l'obiettivo è stato completato
            if (autoComplete && CurrentAmount >= requiredAmount)
            {
                // Completa dopo un delay opzionale
                if (completeDelay > 0)
                {
                    Invoke(nameof(Complete), completeDelay);
                }
                else
                {
                    Complete();
                }
            }
        }
        
        /// <summary>
        /// Marca l'obiettivo come completato
        /// </summary>
        public void Complete()
        {
            if (!IsActive || IsCompleted)
                return;
            
            IsCompleted = true;
            CurrentAmount = requiredAmount;
            
            // Suono di completamento
            if (completionSound != null)
            {
                AudioSource.PlayClipAtPoint(completionSound, transform.position);
            }
            
            // Effetti visivi di completamento
            if (completionVFX != null)
            {
                Instantiate(completionVFX, transform.position, Quaternion.identity);
            }
            
            // Notifica gli ascoltatori
            OnObjectiveCompleted?.Invoke(this);
            onComplete?.Invoke();
            
            // Log
            Debug.Log($"Tutorial Objective Completed: {objectiveName} ({objectiveId})");
            
            // Notifica il TutorialManager
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnObjectiveCompleted(this);
            }
        }
        
        /// <summary>
        /// Resetta lo stato dell'obiettivo
        /// </summary>
        public void Reset()
        {
            IsCompleted = false;
            CurrentAmount = 0;
            
            // Log
            Debug.Log($"Tutorial Objective Reset: {objectiveName} ({objectiveId})");
        }
        
        /// <summary>
        /// Verifica se questo obiettivo corrisponde all'ID specificato
        /// </summary>
        /// <param name="id">ID da verificare</param>
        /// <returns>True se l'obiettivo ha l'ID specificato</returns>
        public bool MatchesId(string id)
        {
            return objectiveId.Equals(id, StringComparison.OrdinalIgnoreCase);
        }
    }
}