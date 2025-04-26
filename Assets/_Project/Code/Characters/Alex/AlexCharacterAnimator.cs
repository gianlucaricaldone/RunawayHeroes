using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Alex, il Corriere Urbano
    /// Gestisce le animazioni uniche legate al parkour urbano e al Frammento Urbano
    /// </summary>
    public class CharacterAnimator_Alex : CharacterAnimatorBase
    {
        // Hash specifici per Alex
        private int hashWallRunning;
        private int hashWallRunRight;
        private int hashWallRunLeft;
        private int hashIsGrinding;
        private int hashVaulting;
        private int hashUrbanDash;
        private int hashGrindBalance;
        private int hashAirTime;
        private int hashDoubleJump;
        private int hashWallJump;
        private int hashVault;
        private int hashQuickTurn;
        private int hashFragmentPurified;

        // Riferimento al componente AlexCharacter per accedere alle proprietà specifiche
        private AlexCharacter alexCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a AlexCharacter
            alexCharacter = character as AlexCharacter;
            
            if (alexCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Alex richiede un componente AlexCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Alex
            hashWallRunning = Animator.StringToHash("isWallRunning");
            hashWallRunRight = Animator.StringToHash("WallRunRight");
            hashWallRunLeft = Animator.StringToHash("WallRunLeft");
            hashIsGrinding = Animator.StringToHash("isGrinding");
            hashVaulting = Animator.StringToHash("Vaulting");
            hashUrbanDash = Animator.StringToHash("UrbanDash");
            hashGrindBalance = Animator.StringToHash("GrindBalance");
            hashAirTime = Animator.StringToHash("AirTime");
            hashDoubleJump = Animator.StringToHash("DoubleJump");
            hashWallJump = Animator.StringToHash("WallJump");
            hashVault = Animator.StringToHash("Vault");
            hashQuickTurn = Animator.StringToHash("QuickTurn");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (alexCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Alex
            animator.SetBool(hashWallRunning, alexCharacter.IsWallRunning);
            animator.SetBool(hashWallRunRight, alexCharacter.IsWallRunningRight);
            animator.SetBool(hashWallRunLeft, alexCharacter.IsWallRunningLeft);
            animator.SetBool(hashIsGrinding, alexCharacter.IsGrinding);
            animator.SetBool(hashVaulting, alexCharacter.IsVaulting);
            animator.SetBool(hashUrbanDash, alexCharacter.IsUsingSpecialAbility);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashGrindBalance, alexCharacter.GrindBalanceAmount);
            animator.SetFloat(hashAirTime, alexCharacter.AirTime);
        }

        /// <summary>
        /// Attiva l'animazione di doppio salto
        /// </summary>
        public void TriggerDoubleJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashDoubleJump);
            }
        }

        /// <summary>
        /// Attiva l'animazione di salto dal muro
        /// </summary>
        public void TriggerWallJump()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashWallJump);
            }
        }

        /// <summary>
        /// Attiva l'animazione di scavalcamento
        /// </summary>
        public void TriggerVault()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashVault);
            }
        }

        /// <summary>
        /// Attiva l'animazione di giro rapido
        /// </summary>
        public void TriggerQuickTurn()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashQuickTurn);
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