using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Neo, l'Hacker della Realtà Virtuale
    /// Gestisce le animazioni legate al mondo digitale e al Frammento Digitale
    /// </summary>
    public class NeoCharacterAnimator : CharacterAnimatorBase
    {
        // Hash specifici per Neo
        private int hashIsGlitching;
        private int hashIsTeleporting;
        private int hashIsHacking;
        private int hashIsDigitalShifting;
        private int hashGlitchIntensity;
        private int hashHackProgress;
        private int hashDigitalShiftLevel;
        private int hashGlitchStart;
        private int hashTeleport;
        private int hashHackingStart;
        private int hashControlledGlitch;
        private int hashFragmentPurified;

        // Riferimento al componente NeoCharacter
        private NeoCharacter neoCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a NeoCharacter
            neoCharacter = character as NeoCharacter;
            
            if (neoCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Neo richiede un componente NeoCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Neo
            hashIsGlitching = Animator.StringToHash("isGlitching");
            hashIsTeleporting = Animator.StringToHash("isTeleporting");
            hashIsHacking = Animator.StringToHash("isHacking");
            hashIsDigitalShifting = Animator.StringToHash("isDigitalShifting");
            hashGlitchIntensity = Animator.StringToHash("GlitchIntensity");
            hashHackProgress = Animator.StringToHash("HackProgress");
            hashDigitalShiftLevel = Animator.StringToHash("DigitalShiftLevel");
            hashGlitchStart = Animator.StringToHash("GlitchStart");
            hashTeleport = Animator.StringToHash("Teleport");
            hashHackingStart = Animator.StringToHash("HackingStart");
            hashControlledGlitch = Animator.StringToHash("ControlledGlitch");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (neoCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Neo
            animator.SetBool(hashIsGlitching, neoCharacter.IsGlitching);
            animator.SetBool(hashIsTeleporting, neoCharacter.IsTeleporting);
            animator.SetBool(hashIsHacking, neoCharacter.IsHacking);
            animator.SetBool(hashIsDigitalShifting, neoCharacter.IsDigitalShifting);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashGlitchIntensity, neoCharacter.GlitchIntensity);
            animator.SetFloat(hashHackProgress, neoCharacter.HackProgress);
            animator.SetFloat(hashDigitalShiftLevel, neoCharacter.DigitalShiftLevel);
        }

        /// <summary>
        /// Attiva l'animazione di inizio glitch
        /// </summary>
        public void TriggerGlitchStart()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashGlitchStart);
            }
        }

        /// <summary>
        /// Attiva l'animazione di teleport
        /// </summary>
        public void TriggerTeleport()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashTeleport);
            }
        }

        /// <summary>
        /// Attiva l'animazione di inizio hacking
        /// </summary>
        public void TriggerHackingStart()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashHackingStart);
            }
        }

        /// <summary>
        /// Attiva l'animazione del Glitch Controllato (abilità speciale)
        /// </summary>
        public void TriggerControlledGlitch()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashControlledGlitch);
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