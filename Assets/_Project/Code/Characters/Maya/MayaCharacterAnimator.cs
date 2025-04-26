using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Maya, l'Esploratrice della Foresta
    /// Gestisce le animazioni legate all'ambiente naturale e al Frammento Naturale
    /// </summary>
    public class MayaCharacterAnimator : CharacterAnimatorBase
    {
        // Hash specifici per Maya
        private int hashIsClimbing;
        private int hashIsSwinging;
        private int hashIsSwimming;
        private int hashInWater;
        private int hashSwingMomentum;
        private int hashClimbHeight;
        private int hashWaterDepth;
        private int hashClimb;
        private int hashSwim;
        private int hashSwingStart;
        private int hashNatureCall;
        private int hashFragmentPurified;

        // Riferimento al componente MayaCharacter
        private MayaCharacter mayaCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a MayaCharacter
            mayaCharacter = character as MayaCharacter;
            
            if (mayaCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Maya richiede un componente MayaCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Maya
            hashIsClimbing = Animator.StringToHash("isClimbing");
            hashIsSwinging = Animator.StringToHash("isSwinging");
            hashIsSwimming = Animator.StringToHash("isSwimming");
            hashInWater = Animator.StringToHash("inWater");
            hashSwingMomentum = Animator.StringToHash("SwingMomentum");
            hashClimbHeight = Animator.StringToHash("ClimbHeight");
            hashWaterDepth = Animator.StringToHash("WaterDepth");
            hashClimb = Animator.StringToHash("Climb");
            hashSwim = Animator.StringToHash("Swim");
            hashSwingStart = Animator.StringToHash("SwingStart");
            hashNatureCall = Animator.StringToHash("NatureCall");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (mayaCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Maya
            animator.SetBool(hashIsClimbing, mayaCharacter.IsClimbing);
            animator.SetBool(hashIsSwinging, mayaCharacter.IsSwinging);
            animator.SetBool(hashIsSwimming, mayaCharacter.IsSwimming);
            animator.SetBool(hashInWater, mayaCharacter.IsInWater);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashSwingMomentum, mayaCharacter.SwingMomentum);
            animator.SetFloat(hashClimbHeight, mayaCharacter.ClimbHeight);
            animator.SetFloat(hashWaterDepth, mayaCharacter.WaterDepth);
        }

        /// <summary>
        /// Attiva l'animazione di arrampicata
        /// </summary>
        public void TriggerClimb()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashClimb);
            }
        }

        /// <summary>
        /// Attiva l'animazione di nuoto
        /// </summary>
        public void TriggerSwim()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashSwim);
            }
        }

        /// <summary>
        /// Attiva l'animazione di inizio oscillazione
        /// </summary>
        public void TriggerSwingStart()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashSwingStart);
            }
        }

        /// <summary>
        /// Attiva l'animazione del Richiamo della Natura (abilità speciale)
        /// </summary>
        public void TriggerNatureCall()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashNatureCall);
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