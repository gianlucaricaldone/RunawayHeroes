using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace RunawayHeroes.Tutorial
{
    /// <summary>
    /// Estensione della classe TutorialUI esistente
    /// Da aggiungere al file TutorialUI.cs che vedi nella struttura del progetto
    /// </summary>
    public partial class TutorialUI : MonoBehaviour
    {
        [Header("Primi Passi Tutorial UI")]
        [SerializeField] private RectTransform jumpPromptContainer;
        [SerializeField] private Image jumpPromptIcon;
        [SerializeField] private TextMeshProUGUI jumpPromptText;
        [SerializeField] private RectTransform tapIndicator;
        
        [Header("UI Animations")]
        [SerializeField] private float promptAnimationDuration = 0.5f;
        [SerializeField] private Ease promptAnimationEase = Ease.OutBack;
        [SerializeField] private float tapIndicatorPulseDuration = 1f;
        
        private Sequence tapIndicatorSequence;

        /// <summary>
        /// Mostra un suggerimento per il salto sul touchscreen
        /// </summary>
        public void ShowJumpPrompt(bool show)
        {
            if (jumpPromptContainer == null) return;
            
            // Interrompi eventuali animazioni in corso
            jumpPromptContainer.DOKill();
            
            if (show)
            {
                // Prepara il container
                jumpPromptContainer.gameObject.SetActive(true);
                jumpPromptContainer.localScale = Vector3.zero;
                
                // Anima l'apparizione del suggerimento
                jumpPromptContainer.DOScale(Vector3.one, promptAnimationDuration)
                    .SetEase(promptAnimationEase);
                
                // Avvia l'animazione dell'indicatore di tap
                AnimateTapIndicator(true);
            }
            else
            {
                // Anima la scomparsa del suggerimento
                jumpPromptContainer.DOScale(Vector3.zero, promptAnimationDuration)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => jumpPromptContainer.gameObject.SetActive(false));
                
                // Ferma l'animazione dell'indicatore di tap
                AnimateTapIndicator(false);
            }
        }
        
        /// <summary>
        /// Animazione pulsante dell'indicatore tap
        /// </summary>
        private void AnimateTapIndicator(bool animate)
        {
            if (tapIndicator == null) return;
            
            // Interrompi la sequenza precedente se esiste
            if (tapIndicatorSequence != null)
            {
                tapIndicatorSequence.Kill();
                tapIndicatorSequence = null;
            }
            
            if (animate)
            {
                // Resetta lo stato iniziale
                tapIndicator.gameObject.SetActive(true);
                tapIndicator.localScale = Vector3.one;
                
                // Crea una nuova sequenza di animazione
                tapIndicatorSequence = DOTween.Sequence();
                
                // Aggiungi le animazioni alla sequenza
                tapIndicatorSequence.Append(tapIndicator.DOScale(1.2f, tapIndicatorPulseDuration * 0.5f)
                    .SetEase(Ease.InOutSine));
                tapIndicatorSequence.Append(tapIndicator.DOScale(1f, tapIndicatorPulseDuration * 0.5f)
                    .SetEase(Ease.InOutSine));
                
                // Imposta la sequenza per ripetersi all'infinito
                tapIndicatorSequence.SetLoops(-1, LoopType.Restart);
            }
            else
            {
                // Nasconde l'indicatore
                tapIndicator.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Mostra una freccia che indica la direzione di un ostacolo
        /// </summary>
        public void ShowObstacleDirection(Vector3 obstaclePosition, float duration = 2f)
        {
            StartCoroutine(ShowObstacleDirectionCoroutine(obstaclePosition, duration));
        }
        
        private IEnumerator ShowObstacleDirectionCoroutine(Vector3 obstaclePosition, float duration)
        {
            // Creazione dinamica della freccia direzionale
            GameObject arrowObj = new GameObject("DirectionArrow");
            arrowObj.transform.SetParent(transform);
            
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(100, 50);
            
            Image arrowImage = arrowObj.AddComponent<Image>();
            // Assegna qui lo sprite della freccia
            // arrowImage.sprite = arrowSprite;
            arrowImage.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            
            // Calcola la posizione della freccia nell'UI
            Vector3 screenPos = Camera.main.WorldToScreenPoint(obstaclePosition);
            arrowRect.position = screenPos;
            arrowRect.anchoredPosition = new Vector2(arrowRect.anchoredPosition.x, arrowRect.anchoredPosition.y + 100);
            
            // Animazione della freccia
            Sequence arrowSequence = DOTween.Sequence();
            arrowSequence.Append(arrowRect.DOAnchorPosY(arrowRect.anchoredPosition.y - 30, 0.5f).SetEase(Ease.InOutSine));
            arrowSequence.Append(arrowRect.DOAnchorPosY(arrowRect.anchoredPosition.y, 0.5f).SetEase(Ease.InOutSine));
            arrowSequence.SetLoops(-1);
            
            // Attendi la durata specificata
            yield return new WaitForSeconds(duration);
            
            // Rimuovi la freccia con una dissolvenza
            arrowSequence.Kill();
            arrowImage.DOFade(0, 0.5f).OnComplete(() => Destroy(arrowObj));
        }
        
        /// <summary>
        /// Mostra un suggerimento basato su un trigger del tutorial
        /// </summary>
        public void ShowTutorialTip(string tipText, float duration = 3f)
        {
            StartCoroutine(ShowTutorialTipCoroutine(tipText, duration));
        }
        
        private IEnumerator ShowTutorialTipCoroutine(string tipText, float duration)
        {
            // Creazione dinamica di un pannello di suggerimento
            GameObject tipPanel = new GameObject("TutorialTip");
            tipPanel.transform.SetParent(transform);
            
            RectTransform tipRect = tipPanel.AddComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.5f, 0.5f);
            tipRect.anchorMax = new Vector2(0.5f, 0.5f);
            tipRect.pivot = new Vector2(0.5f, 0.5f);
            tipRect.sizeDelta = new Vector2(500, 100);
            
            Image panelImage = tipPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            GameObject textObj = new GameObject("TipText");
            textObj.transform.SetParent(tipRect);
            
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
            
            TextMeshProUGUI tipTextComponent = textObj.AddComponent<TextMeshProUGUI>();
            tipTextComponent.text = tipText;
            tipTextComponent.color = Color.white;
            tipTextComponent.fontSize = 24;
            tipTextComponent.alignment = TextAlignmentOptions.Center;
            
            // Animazione di entrata
            tipRect.localScale = Vector3.zero;
            tipRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            
            // Attendi la durata specificata
            yield return new WaitForSeconds(duration);
            
            // Animazione di uscita
            tipRect.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => Destroy(tipPanel));
        }
    }
}