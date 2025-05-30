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
        [SerializeField] private Text versionText; // Aggiunto testo versione
        
        [Header("Timing Settings")]
        [SerializeField] private float logoDelay = 0.5f;
        [SerializeField] private float totalDuration = 4.0f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 1.0f;
        
        [Header("System Settings")]
        [SerializeField] private bool initializeECS = true;
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private string versionNumber = "1.0.0";

        private bool _isInitialized = false;

        private void Awake()
        {
            // Imposta il frameRate per garantire l'esecuzione fluida delle animazioni
            Application.targetFrameRate = 60;
            
            // Inizializza i valori delle UI
            if (fadeOverlay != null) 
            {
                fadeOverlay.alpha = 1f;
                fadeOverlay.blocksRaycasts = true;
            }
            
            if (logoImage != null)
                logoImage.color = new Color(1f, 1f, 1f, 0f);
            
            if (logoTransform != null)
                logoTransform.localScale = new Vector3(0.5f, 0.5f, 1f);
                
            if (versionText != null)
                versionText.text = "v" + versionNumber;
                
            _isInitialized = true;
            
            // Log per debug
            Debug.Log("BootSequence Awake completed");
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Debug.LogError("BootSequence not properly initialized in Awake");
                return;
            }
            
            // Avvio della sequenza con piccolo ritardo per garantire che tutto sia pronto
            StartCoroutine(DelayedBootStart());
            
            // Log per debug
            Debug.Log("BootSequence Start completed");
        }
        
        private IEnumerator DelayedBootStart()
        {
            // Piccolo ritardo per garantire che tutto sia pronto
            yield return new WaitForSeconds(0.1f);
            
            // Avvio della sequenza principale
            StartCoroutine(BootSequenceCoroutine());
            
            Debug.Log("BootSequence main coroutine started");
        }

        private IEnumerator BootSequenceCoroutine()
        {
            Debug.Log("Boot sequence starting");
            
            // Fade in (dal nero)
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 1f, 0f, fadeInDuration));
            
            Debug.Log("Fade in completed");
            
            // Attesa prima di mostrare il logo
            yield return new WaitForSeconds(logoDelay);
            
            // Animazione del logo
            yield return StartCoroutine(AnimateLogo());
            
            Debug.Log("Logo animation completed");
            
            // Calcola il tempo rimanente
            float remainingTime = Mathf.Max(0, totalDuration - logoDelay - fadeInDuration - fadeOutDuration);
            
            // Attesa per la durata rimanente
            if (remainingTime > 0)
                yield return new WaitForSeconds(remainingTime);
            
            // Inizializza ECS se necessario
            if (initializeECS)
            {
                Debug.Log("Initializing ECS system...");
                InitializeECSSystem();
            }
            
            // Fade out (al nero)
            yield return StartCoroutine(FadeCanvasGroup(fadeOverlay, 0f, 1f, fadeOutDuration));
            
            Debug.Log("Fade out completed, loading next scene: " + nextSceneName);
            
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
            Debug.Log("Starting logo animation");
            
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
                
                elapsed += Time.deltaTime; // Uso deltaTime normale per coerenza
                yield return null;
            }
            
            // Valori finali per garantire che l'animazione sia completa
            logoTransform.localScale = Vector3.one;
            logoImage.color = new Color(1f, 1f, 1f, 1f);
            
            Debug.Log("Logo animation complete");
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float targetAlpha, float duration)
        {
            if (group == null)
            {
                Debug.LogError("CanvasGroup is null in FadeCanvasGroup");
                yield break;
            }
            
            Debug.Log($"Fading from {startAlpha} to {targetAlpha} over {duration} seconds");
            
            float elapsed = 0f;
            
            // Imposta l'alpha iniziale
            group.alpha = startAlpha;
            
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                
                elapsed += Time.deltaTime; // Uso deltaTime normale per coerenza
                yield return null;
            }
            
            // Imposta il valore finale per garantire che il fade sia completo
            group.alpha = targetAlpha;
            
            Debug.Log("Fade completed");
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
        
        // Metodo per test e debug
        public void ForceCompleteSequence()
        {
            StopAllCoroutines();
            fadeOverlay.alpha = 1f;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}