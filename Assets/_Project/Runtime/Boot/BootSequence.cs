// File: Assets/_Project/Runtime/Boot/BootSequence.cs

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RunawayHeroes.Runtime.Boot
{
    public class BootSequence : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private Image logoImage;
        [SerializeField] private RectTransform logoTransform;
        
        [Header("Timing Settings")]
        [SerializeField] private float logoDelay = 0.5f;
        [SerializeField] private float totalDuration = 4.0f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 1.0f;
        
        [Header("System Settings")]
        [SerializeField] private bool initializeECS = true;
        [SerializeField] private string nextSceneName = "MainMenu";

        private void Start()
        {
            // Inizializzazione
            fadeOverlay.alpha = 1f;
            logoImage.color = new Color(1f, 1f, 1f, 0f);
            logoTransform.localScale = new Vector3(0.5f, 0.5f, 1f);
            
            // Disabilita interazione con UI durante la sequenza
            fadeOverlay.blocksRaycasts = true;
            
            // Avvio della sequenza
            StartCoroutine(BootSequenceCoroutine());
        }

        private IEnumerator BootSequenceCoroutine()
        {
            // Fade in (dal nero)
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeInDuration));
            
            // Attesa prima di mostrare il logo
            yield return new WaitForSeconds(logoDelay);
            
            // Animazione del logo
            StartCoroutine(AnimateLogo());
            
            // Attesa per la durata specificata
            yield return new WaitForSeconds(totalDuration - logoDelay - fadeOutDuration);
            
            // Inizializza ECS se necessario
            if (initializeECS)
            {
                Debug.Log("Initializing ECS system...");
                InitializeECSSystem();
            }
            
            // Fade out (al nero)
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeOutDuration));
            
            // Caricamento della prossima scena
            SceneManager.LoadScene(nextSceneName);
        }

        private void InitializeECSSystem()
        {
            // Inizializzazione del sistema ECS
            try
            {
                // Creare il GameObject per l'ECSBootstrap se non esiste
                GameObject ecsBootstrap = GameObject.Find("ECSBootstrap");
                if (ecsBootstrap == null)
                {
                    ecsBootstrap = new GameObject("ECSBootstrap");
                    ecsBootstrap.AddComponent<RunawayHeroes.Runtime.Bootstrap.ECSBootstrap>();
                    // Assicuriamoci che non venga distrutto durante il caricamento della prossima scena
                    DontDestroyOnLoad(ecsBootstrap);
                    Debug.Log("ECS Bootstrap component created successfully");
                }
                else
                {
                    Debug.Log("ECS Bootstrap already exists");
                }
                
                // Inizializza anche il WorldBootstrap se necessario
                GameObject worldBootstrap = GameObject.Find("WorldBootstrap");
                if (worldBootstrap == null)
                {
                    worldBootstrap = new GameObject("WorldBootstrap");
                    worldBootstrap.AddComponent<RunawayHeroes.Runtime.Bootstrap.WorldBootstrap>();
                    DontDestroyOnLoad(worldBootstrap);
                    Debug.Log("World Bootstrap component created successfully");
                }
                
                // Crea anche un GameBootstrap se richiesto
                GameObject gameBootstrap = GameObject.Find("GameBootstrap");
                if (gameBootstrap == null)
                {
                    gameBootstrap = new GameObject("GameBootstrap");
                    gameBootstrap.AddComponent<RunawayHeroes.Runtime.Bootstrap.GameBootstrap>();
                    DontDestroyOnLoad(gameBootstrap);
                    Debug.Log("Game Bootstrap component created successfully");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to initialize ECS system: " + e.Message);
                Debug.LogException(e);
            }
        }

        private IEnumerator AnimateLogo()
        {
            float duration = 1.0f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                
                // Effetto rimbalzo per la scala
                float scale = BounceEaseOut(t);
                logoTransform.localScale = new Vector3(0.5f + 0.5f * scale, 0.5f + 0.5f * scale, 1f);
                
                // Fade in del logo
                logoImage.color = new Color(1f, 1f, 1f, t);
                
                elapsed += Time.unscaledDeltaTime; // Usiamo unscaledDeltaTime per evitare problemi con la pausa
                yield return null;
            }
            
            // Valori finali
            logoTransform.localScale = Vector3.one;
            logoImage.color = new Color(1f, 1f, 1f, 1f);
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float targetAlpha, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                elapsed += Time.unscaledDeltaTime; // Usiamo unscaledDeltaTime per evitare problemi con la pausa
                yield return null;
            }
            
            group.alpha = targetAlpha;
        }

        // Funzione di rimbalzo per l'animazione
        private float BounceEaseOut(float t)
        {
            if (t < 0.36364f)
                return 7.5625f * t * t;
            else if (t < 0.72727f)
                return 7.5625f * (t -= 0.54545f) * t + 0.75f;
            else if (t < 0.90909f)
                return 7.5625f * (t -= 0.81818f) * t + 0.9375f;
            else
                return 7.5625f * (t -= 0.95455f) * t + 0.984375f;
        }
    }
}