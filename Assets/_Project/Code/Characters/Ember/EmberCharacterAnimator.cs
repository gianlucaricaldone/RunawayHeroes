using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Ember, la Sopravvissuta dell'Inferno di Lava
    /// Gestisce le animazioni legate all'ambiente vulcanico e al Frammento Igneo
    /// </summary>
    public class EmberCharacterAnimator : CharacterAnimatorBase
    {
        // Hash specifici per Ember
        private int hashIsFireproofActive;
        private int hashIsOnMagma;
        private int hashIsUsingGeyser;
        private int hashHeatLevel;
        private int hashFireproofIntensity;
        private int hashGeyserJump;
        private int hashFireproof;
        private int hashHeatShield;
        private int hashFragmentPurified;

        // Riferimento al componente EmberCharacter
        private EmberCharacter emberCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a EmberCharacter
            emberCharacter = character as EmberCharacter;
            
            if (emberCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Ember richiede un componente EmberCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Ember
            hashIsFireproofActive = Animator.StringToHash("isFireproofActive");
            hashIsOnMagma = Animator.StringToHash("isOnMagma");
            hashIsUsingGeyser = Animator.StringToHash("isUsingGeyser");
            hashHeatLevel = Animator.StringToHash("HeatLevel");
            hashFireproofIntensity = Animator.StringToHash("FireproofIntensity");
            hashGeyserJump = Animator.StringToHash("GeyserJump");
            hashFireproof = Animator.StringToHash("Fireproof");
            hashHeatShield = Animator.StringToHash("HeatShield");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (emberCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Ember
            animator.SetBool(hashIsFireproofActive, emberCharacter.IsFireproofActive);
            animator.SetBool(hashIsOnMagma, emberCharacter.IsOnMagma);
            animator.SetBool(hashIsUsingGeyser, emberCharacter.IsUsingGeyser);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashHeatLevel, emberCharacter.HeatLevel);
            animator.SetFloat(hashFireproofIntensity, emberCharacter.FireproofIntensity);
        }

        /// <summary>
        /// Attiva l'animazione di salto su geyser
        /// </summary>
        public void TriggerGeyserJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashGeyserJump);
            }
        }

        /// <summary>
        /// Attiva l'animazione del Corpo Ignifugo (abilità speciale)
        /// </summary>
        public void TriggerFireproof()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashFireproof);
            }
        }

        /// <summary>
        /// Attiva l'animazione dello scudo termico
        /// </summary>
        public void TriggerHeatShield()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashHeatShield);
            }
        }

        /// <summary>
        /// Attiva l'animazione di purificazione del frammento
        /// </summary>
        public void TriggerFragmentPurified()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashFragmentPurified);
            }
        }
    }
}