// Path: Assets/_Project/Runtime/Boot/BootSequence.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace RunawayHeroes.Runtime.Boot
{
    public class BootSequence : MonoBehaviour
    {
        [SerializeField] private float splashDuration = 3.0f;
        [SerializeField] private Animator logoAnimator;
        
        private void Start()
        {
            StartCoroutine(BootSequenceCoroutine());
        }
        
        private IEnumerator BootSequenceCoroutine()
        {
            // Avvia animazione logo se presente
            if (logoAnimator != null)
            {
                logoAnimator.SetTrigger("Play");
            }
            
            // Attendi durata splash
            yield return new WaitForSeconds(splashDuration);
            
            // Carica il menu principale
            SceneManager.LoadScene("MainMenu");
        }
    }
}