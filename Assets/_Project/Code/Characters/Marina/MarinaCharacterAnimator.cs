using UnityEngine;

namespace RunawayHeroes.Animations
{
    /// <summary>
    /// Controller di animazione specifico per Marina, la Biologa degli Abissi
    /// Gestisce le animazioni legate all'ambiente subacqueo e al Frammento Abissale
    /// </summary>
    public class MarinaCharacterAnimator : CharacterAnimatorBase
    {
        // Hash specifici per Marina
        private int hashIsSwimming;
        private int hashIsInWater;
        private int hashIsBubbleActive;
        private int hashIsUsingCurrents;
        private int hashWaterDepth;
        private int hashOxygenLevel;
        private int hashWaterPressure;
        private int hashSwimDirection;
        private int hashSwim;
        private int hashAirBubble;
        private int hashDeepDive;
        private int hashFragmentPurified;

        // Riferimento al componente MarinaCharacter
        private MarinaCharacter marinaCharacter;

        protected override void Awake()
        {
            base.Awake();
            
            // Ottieni il riferimento a MarinaCharacter
            marinaCharacter = character as MarinaCharacter;
            
            if (marinaCharacter == null)
            {
                Debug.LogError("CharacterAnimator_Marina richiede un componente MarinaCharacter");
            }
        }

        protected override void InitializeParameterHashes()
        {
            base.InitializeParameterHashes();
            
            // Inizializza hash specifici per Marina
            hashIsSwimming = Animator.StringToHash("isSwimming");
            hashIsInWater = Animator.StringToHash("isInWater");
            hashIsBubbleActive = Animator.StringToHash("isBubbleActive");
            hashIsUsingCurrents = Animator.StringToHash("isUsingCurrents");
            hashWaterDepth = Animator.StringToHash("WaterDepth");
            hashOxygenLevel = Animator.StringToHash("OxygenLevel");
            hashWaterPressure = Animator.StringToHash("WaterPressure");
            hashSwimDirection = Animator.StringToHash("SwimDirection");
            hashSwim = Animator.StringToHash("Swim");
            hashAirBubble = Animator.StringToHash("AirBubble");
            hashDeepDive = Animator.StringToHash("DeepDive");
            hashFragmentPurified = Animator.StringToHash("FragmentPurified");
        }

        protected override void UpdateAnimatorParameters()
        {
            base.UpdateAnimatorParameters();
            
            if (marinaCharacter == null || animator == null)
                return;
            
            // Aggiorna parametri specifici di Marina
            animator.SetBool(hashIsSwimming, marinaCharacter.IsSwimming);
            animator.SetBool(hashIsInWater, marinaCharacter.IsInWater);
            animator.SetBool(hashIsBubbleActive, marinaCharacter.IsBubbleActive);
            animator.SetBool(hashIsUsingCurrents, marinaCharacter.IsUsingCurrents);
            
            // Aggiorna valori float specifici
            animator.SetFloat(hashWaterDepth, marinaCharacter.WaterDepth);
            animator.SetFloat(hashOxygenLevel, marinaCharacter.OxygenLevel);
            animator.SetFloat(hashWaterPressure, marinaCharacter.WaterPressure);
            
            // Gestisci la direzione di nuoto 3D come un valore float (codifica)
            float swimDirectionValue = EncodeSwimDirection(marinaCharacter.SwimDirection);
            animator.SetFloat(hashSwimDirection, swimDirectionValue);
        }

        /// <summary>
        /// Codifica la direzione di nuoto 3D in un singolo valore float
        /// Questo è un esempio - potrebbe richiedere più parametri per un controllo più preciso
        /// </summary>
        private float EncodeSwimDirection(Vector3 direction)
        {
            // Esempio semplice: 
            // 0 = avanti, 1 = destra, 2 = sinistra, 3 = su, 4 = giù
            
            if (direction.y > 0.5f)
                return 3f;
            else if (direction.y < -0.5f)
                return 4f;
            else if (direction.x > 0.5f)
                return 1f;
            else if (direction.x < -0.5f)
                return 2f;
            else
                return 0f; // avanti di default
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
        /// Attiva l'animazione della Bolla d'Aria (abilità speciale)
        /// </summary>
        public void TriggerAirBubble()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashAirBubble);
            }
        }

        /// <summary>
        /// Attiva l'animazione di immersione profonda
        /// </summary>
        public void TriggerDeepDive()
        {
            if (animator != null)
            {
                animator.SetTrigger(hashDeepDive);
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