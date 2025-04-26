using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Classe base per i controller di animazione dei personaggi
    /// Gestisce la comunicazione tra i sistemi di movimento e l'Animator Unity
    /// </summary>
    public abstract class CharacterAnimatorBase : MonoBehaviour
    {
        [Header("Componenti")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected CharacterBase character;

        [Header("Debug")]
        [SerializeField] protected bool showDebugInfo = false;

        // Animator parameter hashes per efficienza
        protected int hashIsGrounded;
        protected int hashIsSliding;
        protected int hashJumping;
        protected int hashSpeed;
        protected int hashVerticalVelocity;
        protected int hashJump;
        protected int hashDamaged;
        protected int hashDeath;
        protected int hashSpecialAbility;

        protected virtual void Awake()
        {
            // Ottieni i componenti se non assegnati
            if (animator == null)
                animator = GetComponent<Animator>();

            if (character == null)
                character = GetComponentInParent<CharacterBase>();

            // Inizializza gli hash dei parametri comuni
            InitializeParameterHashes();
        }

        protected virtual void Start()
        {
            // Registra gli eventi del personaggio
            if (character != null)
            {
                character.OnHealthChange += OnHealthChanged;
                character.OnSpecialAbility += OnSpecialAbilityActivated;
                character.OnCharacterDeath += OnCharacterDeath;
            }
        }

        protected virtual void OnDestroy()
        {
            // Annulla la registrazione degli eventi
            if (character != null)
            {
                character.OnHealthChange -= OnHealthChanged;
                character.OnSpecialAbility -= OnSpecialAbilityActivated;
                character.OnCharacterDeath -= OnCharacterDeath;
            }
        }

        protected virtual void Update()
        {
            if (animator == null || character == null)
                return;

            UpdateAnimatorParameters();
        }

        /// <summary>
        /// Inizializza gli hash dei parametri dell'animator per migliorare le prestazioni
        /// </summary>
        protected virtual void InitializeParameterHashes()
        {
            // Parametri comuni a tutti i personaggi
            hashIsGrounded = Animator.StringToHash("isGrounded");
            hashIsSliding = Animator.StringToHash("isSliding");
            hashJumping = Animator.StringToHash("Jumping");
            hashSpeed = Animator.StringToHash("Speed");
            hashVerticalVelocity = Animator.StringToHash("VerticalVelocity");
            hashJump = Animator.StringToHash("Jump");
            hashDamaged = Animator.StringToHash("Damaged");
            hashDeath = Animator.StringToHash("Death");
            hashSpecialAbility = Animator.StringToHash("specialAbility");
        }

        /// <summary>
        /// Aggiorna i parametri dell'animator in base allo stato del personaggio
        /// </summary>
        protected virtual void UpdateAnimatorParameters()
        {
            // Aggiorna parametri comuni a tutti i personaggi
            animator.SetBool(hashIsGrounded, character.IsGrounded);
            animator.SetBool(hashIsSliding, character.IsSliding);
            animator.SetBool(hashJumping, character.IsJumping);
            
            // Velocità di movimento
            float speedPercent = character.CurrentMovementSpeed / character.BaseMovementSpeed;
            animator.SetFloat(hashSpeed, speedPercent);
            
            // Velocità verticale (per animazioni di salto/caduta)
            animator.SetFloat(hashVerticalVelocity, character.VerticalVelocity);
        }

        /// <summary>
        /// Attiva il trigger di salto nell'animator
        /// </summary>
        public virtual void TriggerJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashJump);
            }
        }

        /// <summary>
        /// Gestisce le animazioni di danno
        /// </summary>
        protected virtual void OnHealthChanged(float newHealth, float maxHealth)
        {
            // Se la salute è diminuita significativamente, attiva l'animazione di danno
            if (newHealth < character.LastFrameHealth && character.LastFrameHealth - newHealth > 5)
            {
                animator.SetTrigger(hashDamaged);
            }
        }

        /// <summary>
        /// Gestisce l'animazione dell'abilità speciale
        /// </summary>
        protected virtual void OnSpecialAbilityActivated(bool activated)
        {
            if (activated)
            {
                animator.SetTrigger(hashSpecialAbility);
            }
        }

        /// <summary>
        /// Gestisce l'animazione di morte
        /// </summary>
        protected virtual void OnCharacterDeath()
        {
            animator.SetTrigger(hashDeath);
        }
    }
}