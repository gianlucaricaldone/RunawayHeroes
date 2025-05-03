using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace RunawayHeroes.Runtime.UI
{
    /// <summary>
    /// Controller per il pannello dei messaggi di tutorial.
    /// Gestisce l'aspetto visivo e le animazioni del pannello.
    /// </summary>
    public class TutorialMessagePanel : MonoBehaviour
    {
        [Header("Riferimenti UI")]
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Configurazione animazione")]
        [SerializeField] private float fadeInTime = 0.5f;
        [SerializeField] private float fadeOutTime = 0.4f;
        [SerializeField] private AnimationCurve fadeCurve;
        
        [Header("Configurazione visiva")]
        [SerializeField] private Color tutorialColor = new Color(0.1f, 0.6f, 1f, 0.85f);
        [SerializeField] private Color notificationColor = new Color(0.2f, 0.9f, 0.2f, 0.85f);
        [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0.1f, 0.85f);
        [SerializeField] private Sprite[] typeIcons;
        
        private Animator _animator;
        private Coroutine _currentAnimation;
        
        private void Awake()
        {
            // Ottieni l'animator se presente
            _animator = GetComponent<Animator>();
            
            // Se non c'è un canvas group, aggiungilo
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Nascondi il pannello all'inizio
            gameObject.SetActive(false);
            canvasGroup.alpha = 0f;
        }
        
        /// <summary>
        /// Mostra il messaggio con il testo e il tipo specificati
        /// </summary>
        public void ShowMessage(string message, byte messageType = 0)
        {
            // Imposta il testo
            if (messageText != null)
            {
                messageText.text = message;
            }
            
            // Imposta il colore in base al tipo
            if (backgroundImage != null)
            {
                Color typeColor = tutorialColor;
                switch (messageType)
                {
                    case 1: typeColor = notificationColor; break;
                    case 2: typeColor = warningColor; break;
                }
                backgroundImage.color = typeColor;
            }
            
            // Imposta l'icona in base al tipo
            if (iconImage != null && typeIcons != null && typeIcons.Length > messageType)
            {
                iconImage.sprite = typeIcons[messageType];
                iconImage.gameObject.SetActive(true);
            }
            
            // Mostra il pannello
            gameObject.SetActive(true);
            
            // Usa l'animator se disponibile, altrimenti usa la coroutine
            if (_animator != null)
            {
                _animator.SetTrigger("Show");
            }
            else
            {
                // Interrompi l'animazione corrente se presente
                if (_currentAnimation != null)
                {
                    StopCoroutine(_currentAnimation);
                }
                
                // Avvia una nuova animazione
                _currentAnimation = StartCoroutine(FadeIn());
            }
        }
        
        /// <summary>
        /// Nasconde il messaggio
        /// </summary>
        public void HideMessage()
        {
            // Usa l'animator se disponibile, altrimenti usa la coroutine
            if (_animator != null)
            {
                _animator.SetTrigger("Hide");
                
                // Disattiva il pannello dopo la durata dell'animazione
                StartCoroutine(DisableAfterDelay(_animator.GetCurrentAnimatorStateInfo(0).length));
            }
            else
            {
                // Interrompi l'animazione corrente se presente
                if (_currentAnimation != null)
                {
                    StopCoroutine(_currentAnimation);
                }
                
                // Avvia una nuova animazione
                _currentAnimation = StartCoroutine(FadeOut());
            }
        }
        
        /// <summary>
        /// Animazione di fade in
        /// </summary>
        private IEnumerator FadeIn()
        {
            canvasGroup.alpha = 0f;
            float time = 0f;
            
            while (time < fadeInTime)
            {
                time += Time.deltaTime;
                float progress = time / fadeInTime;
                
                // Usa la curva di animazione se disponibile
                if (fadeCurve != null && fadeCurve.keys.Length > 0)
                {
                    canvasGroup.alpha = fadeCurve.Evaluate(progress);
                }
                else
                {
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, progress);
                }
                
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
            _currentAnimation = null;
        }
        
        /// <summary>
        /// Animazione di fade out
        /// </summary>
        private IEnumerator FadeOut()
        {
            canvasGroup.alpha = 1f;
            float time = 0f;
            
            while (time < fadeOutTime)
            {
                time += Time.deltaTime;
                float progress = time / fadeOutTime;
                
                // Usa la curva di animazione se disponibile
                if (fadeCurve != null && fadeCurve.keys.Length > 0)
                {
                    canvasGroup.alpha = fadeCurve.Evaluate(1f - progress);
                }
                else
                {
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, progress);
                }
                
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _currentAnimation = null;
        }
        
        /// <summary>
        /// Disattiva il pannello dopo un ritardo
        /// </summary>
        private IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }
    }
}