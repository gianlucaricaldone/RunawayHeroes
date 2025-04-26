using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Kai, l'Alpinista della Tundra
    /// Gestisce le animazioni legate all'ambiente ghiacciato e al Frammento Glaciale
    /// </summary>
    public class KaiCharacterAnimator : CharacterAnimatorBase
    {
        // Hash specifici per Kai
        private int hashIsIceSliding;
        private int hashIsClimbing;
        private int hashIsSnowboarding;
        private int hashIsPickaxing;
        private int hashIceSlideSpeed;
        private int hashSnowboardAngle;
        private int hashClimbHeight;
        private int hashTemperatureLevel;
        private int hashClimbStart;
        private int hashPickaxe;
        private int hashSnowboardStart;
        private int hashHeatAura;
        private int hashFragmentPurified;

        // Riferimento al componente KaiCharacter
        private KaiCharacter kaiCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a KaiCharacter
            kaiCharacter = character as KaiCharacter;
            
            if (kaiCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Kai richiede un componente KaiCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Kai
            hashIsIceSliding = Animator.StringToHash("isIceSliding");
            hashIsClimbing = Animator.StringToHash("isClimbing");
            hashIsSnowboarding = Animator.StringToHash("isSnowboarding");
            hashIsPickaxing = Animator.StringToHash("isPickaxing");
            hashIceSlideSpeed = Animator.StringToHash("IceSlideSpeed");
            hashSnowboardAngle = Animator.StringToHash("SnowboardAngle");
            hashClimbHeight = Animator.StringToHash("ClimbHeight");
            hashTemperatureLevel = Animator.StringToHash("TemperatureLevel");
            hashClimbStart = Animator.StringToHash("ClimbStart");
            hashPickaxe = Animator.StringToHash("Pickaxe");
            hashSnowboardStart = Animator.StringToHash("SnowboardStart");
            hashHeatAura = Animator.StringToHash("HeatAura");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (kaiCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Kai
            animator.SetBool(hashIsIceSliding, kaiCharacter.IsIceSliding);
            animator.SetBool(hashIsClimbing, kaiCharacter.IsClimbing);
            animator.SetBool(hashIsSnowboarding, kaiCharacter.IsSnowboarding);
            animator.SetBool(hashIsPickaxing, kaiCharacter.IsPickaxing);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashIceSlideSpeed, kaiCharacter.IceSlideSpeed);
            animator.SetFloat(hashSnowboardAngle, kaiCharacter.SnowboardAngle);
            animator.SetFloat(hashClimbHeight, kaiCharacter.ClimbHeight);
            animator.SetFloat(hashTemperatureLevel, kaiCharacter.TemperatureLevel);
        }

        /// <summary>
        /// Attiva l'animazione di inizio arrampicata
        /// </summary>
        public void TriggerClimbStart()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashClimbStart);
            }
        }

        /// <summary>
        /// Attiva l'animazione di utilizzo piccozza
        /// </summary>
        public void TriggerPickaxe()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashPickaxe);
            }
        }

        /// <summary>
        /// Attiva l'animazione di inizio snowboard
        /// </summary>
        public void TriggerSnowboardStart()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashSnowboardStart);
            }
        }

        /// <summary>
        /// Attiva l'animazione dell'Aura di Calore (abilità speciale)
        /// </summary>
        public void TriggerHeatAura()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashHeatAura);
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