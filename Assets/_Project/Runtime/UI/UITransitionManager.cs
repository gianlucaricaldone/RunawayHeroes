using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace RunawayHeroes.Runtime.UI
{
    public class UITransitionManager : MonoBehaviour
    {
        [SerializeField] private Image transitionImage;
        [SerializeField] private float transitionDuration = 0.5f;
        
        private void Start()
        {
            // Assicurati che l'immagine sia inizialmente trasparente
            if (transitionImage != null)
            {
                Color color = transitionImage.color;
                color.a = 0f;
                transitionImage.color = color;
            }
        }
        
        public void FadeIn(System.Action onComplete = null)
        {
            if (transitionImage != null)
                StartCoroutine(FadeRoutine(0f, 1f, transitionDuration, onComplete));
        }
        
        public void FadeOut(System.Action onComplete = null)
        {
            if (transitionImage != null)
                StartCoroutine(FadeRoutine(1f, 0f, transitionDuration, onComplete));
        }
        
        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
        {
            float timer = 0f;
            Color color = transitionImage.color;
            
            while (timer <= duration)
            {
                timer += Time.deltaTime;
                float normalizedTime = timer / duration;
                color.a = Mathf.Lerp(startAlpha, endAlpha, normalizedTime);
                transitionImage.color = color;
                yield return null;
            }
            
            // Assicurati che il valore finale sia esattamente quello desiderato
            color.a = endAlpha;
            transitionImage.color = color;
            
            onComplete?.Invoke();
        }
    }
}