// Path: Assets/_Project/ECS/Systems/UI/UIMessageSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RunawayHeroes.ECS.Components.UI;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Sistema che gestisce la visualizzazione dei messaggi UI nel gioco,
    /// inclusi i messaggi di istruzione del tutorial, notifiche e avvisi.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct UIMessageSystem : ISystem
    {
        // Riferimenti UI
        private GameObject _messagePanel;
        private TextMeshProUGUI _messageText;
        private Animator _messageAnimator;
        private Image _backgroundImage;
        
        // Configurazione visiva per diversi tipi di messaggi
        private Color[] _messageTypeColors;
        
        // Gestione stati e coda
        private bool _isMessageVisible;
        private float _messageTimer;
        private Entity _currentMessageEntity;
        private Queue<Entity> _messageQueue;
        
        // Costanti di animazione
        private const float FADE_IN_TIME = 0.5f;
        private const float FADE_OUT_TIME = 0.4f;
        
        public void OnCreate(ref SystemState state)
        {
            // Inizializza i valori che normalmente sarebbero inizializzati in-line
            _messageTypeColors = new Color[]
            {
                new Color(0.1f, 0.6f, 1f, 0.85f),  // Tutorial - Blu
                new Color(0.2f, 0.9f, 0.2f, 0.85f), // Notifica - Verde
                new Color(1f, 0.6f, 0.1f, 0.85f)   // Avviso - Arancione
            };
            
            _isMessageVisible = false;
            _messageTimer = 0f;
            _currentMessageEntity = Entity.Null;
            _messageQueue = new Queue<Entity>();
            
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Non è necessario un RequireForUpdate aggiuntivo poiché questo sistema dovrebbe
            // essere sempre attivo per gestire potenziali messaggi
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        public void OnStartRunning(ref SystemState state)
        {
            // Cerca il panel dei messaggi tramite UIManager
            if (UIManager.Instance != null && UIManager.Instance.IsPanelActive("GameHUD"))
            {
                // Assumiamo che il panel dei messaggi sia un figlio del GameHUD
                Transform hudTransform = GameObject.Find("GameHUD").transform;
                if (hudTransform != null)
                {
                    _messagePanel = hudTransform.Find("MessagePanel").gameObject;
                    if (_messagePanel != null)
                    {
                        _messageText = _messagePanel.transform.Find("MessageText").GetComponent<TextMeshProUGUI>();
                        _messageAnimator = _messagePanel.GetComponent<Animator>();
                        _backgroundImage = _messagePanel.transform.Find("Background").GetComponent<Image>();
                        
                        // Inizialmente nascondiamo il pannello
                        _messagePanel.SetActive(false);
                    }
                    else
                    {
                        // Se il pannello non esiste, lo creiamo dinamicamente
                        CreateMessagePanelDynamically(hudTransform);
                    }
                }
            }
        }
        
        /// <summary>
        /// Crea dinamicamente un pannello per i messaggi se non esiste già
        /// </summary>
        private void CreateMessagePanelDynamically(Transform parentTransform)
        {
            // Crea il pannello dei messaggi
            _messagePanel = new GameObject("MessagePanel");
            _messagePanel.transform.SetParent(parentTransform, false);
            
            // Configura RectTransform
            RectTransform rectTransform = _messagePanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.75f); // In alto al centro
            rectTransform.anchorMax = new Vector2(0.5f, 0.75f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(800f, 100f);
            
            // Aggiungi uno sfondo
            GameObject background = new GameObject("Background");
            background.transform.SetParent(_messagePanel.transform, false);
            RectTransform bgRectTransform = background.AddComponent<RectTransform>();
            bgRectTransform.anchorMin = Vector2.zero;
            bgRectTransform.anchorMax = Vector2.one;
            bgRectTransform.sizeDelta = Vector2.zero;
            
            _backgroundImage = background.AddComponent<Image>();
            _backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            // Aggiungi il testo
            GameObject textObject = new GameObject("MessageText");
            textObject.transform.SetParent(_messagePanel.transform, false);
            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            textRectTransform.anchorMin = new Vector2(0.05f, 0.1f);
            textRectTransform.anchorMax = new Vector2(0.95f, 0.9f);
            textRectTransform.sizeDelta = Vector2.zero;
            
            _messageText = textObject.AddComponent<TextMeshProUGUI>();
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.fontSize = 28;
            _messageText.color = Color.white;
            
            // Aggiungi un animator
            _messageAnimator = _messagePanel.AddComponent<Animator>();
            // Nota: In un'implementazione reale, qui caricheremmo un controller di animazione
            
            // Inizialmente nascondiamo il pannello
            _messagePanel.SetActive(false);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            // Verifica se ci sono nuovi messaggi da mostrare
            ProcessNewMessages(ref state);
            
            // Gestione del messaggio corrente
            if (_isMessageVisible && _currentMessageEntity != Entity.Null)
            {
                UpdateCurrentMessage(ref state);
            }
            // Passa al messaggio successivo se disponibile e nessun messaggio è attivo
            else if (!_isMessageVisible && _messageQueue.Count > 0 && _currentMessageEntity == Entity.Null)
            {
                ShowNextMessage(ref state);
            }
            
            // Processa gli eventi di visualizzazione messaggi
            ProcessMessageEvents(ref state);
        }
        
        /// <summary>
        /// Processa nuovi messaggi aggiunti al sistema
        /// </summary>
        private void ProcessNewMessages(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Raccogli i nuovi messaggi e li metti in coda
            foreach (var (entity, message) in 
                     SystemAPI.Query<RefRW<UIMessageComponent>>()
                     .WithAll<QueuedMessageTag>()
                     .WithEntityAccess())
            {
                // Aggiungi il messaggio alla coda
                _messageQueue.Enqueue(entity);
                
                // Rimuovi il tag di accodamento
                commandBuffer.RemoveComponent<QueuedMessageTag>(entity);
            }
        }
        
        /// <summary>
        /// Aggiorna lo stato del messaggio correntemente visualizzato
        /// </summary>
        private void UpdateCurrentMessage(ref SystemState state)
        {
            // Accedi al deltaTime dalla state passata come parametro
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            if (state.EntityManager.Exists(_currentMessageEntity) && 
                state.EntityManager.HasComponent<UIMessageComponent>(_currentMessageEntity))
            {
                var message = state.EntityManager.GetComponentData<UIMessageComponent>(_currentMessageEntity);
                
                // Aggiorna il timer del messaggio
                message.RemainingTime -= (float)deltaTime;
                state.EntityManager.SetComponentData(_currentMessageEntity, message);
                
                // Se il tempo è scaduto e il messaggio non è persistente, nascondilo
                if (message.RemainingTime <= 0 && !message.IsPersistent)
                {
                    HideCurrentMessage(state);
                    
                    // Genera un evento di nascondiglio messaggio
                    Entity hideEvent = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(hideEvent, new MessageHideEvent
                    {
                        MessageEntity = _currentMessageEntity,
                        Forced = false
                    });
                    
                    // Rimuovi il tag di messaggio attivo
                    commandBuffer.RemoveComponent<ActiveMessageTag>(_currentMessageEntity);
                    _currentMessageEntity = Entity.Null;
                }
            }
            else
            {
                // Se l'entità non esiste più, puliamo lo stato
                HideCurrentMessage(state);
                _currentMessageEntity = Entity.Null;
            }
        }
        
        /// <summary>
        /// Mostra il prossimo messaggio nella coda
        /// </summary>
        private void ShowNextMessage(ref SystemState state)
        {
            if (_messageQueue.Count > 0)
            {
                _currentMessageEntity = _messageQueue.Dequeue();
                
                if (state.EntityManager.Exists(_currentMessageEntity) && 
                    state.EntityManager.HasComponent<UIMessageComponent>(_currentMessageEntity))
                {
                    var message = state.EntityManager.GetComponentData<UIMessageComponent>(_currentMessageEntity);
                    
                    // Aggiungi tag per indicare che il messaggio è attivo
                    state.EntityManager.AddComponent<ActiveMessageTag>(_currentMessageEntity);
                    
                    // Configura l'UI
                    if (_messageText != null)
                    {
                        _messageText.text = message.Message.ToString();
                    }
                    
                    if (_backgroundImage != null && message.MessageType < _messageTypeColors.Length)
                    {
                        _backgroundImage.color = _messageTypeColors[message.MessageType];
                    }
                    
                    // Mostra il messaggio con animazione
                    ShowMessage(state);
                    
                    // Inizializza il timer
                    message.RemainingTime = message.Duration;
                    state.EntityManager.SetComponentData(_currentMessageEntity, message);
                    
                    // Genera evento di visualizzazione
                    var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
                    var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
                    Entity showEvent = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(showEvent, new MessageShowEvent
                    {
                        MessageEntity = _currentMessageEntity
                    });
                }
                else
                {
                    _currentMessageEntity = Entity.Null;
                }
            }
        }
        
        /// <summary>
        /// Processa eventi relativi ai messaggi
        /// </summary>
        private void ProcessMessageEvents(ref SystemState state)
        {
            // Rimuovi entità eventi dopo l'elaborazione
            foreach (var (entity, showEvent) in 
                     SystemAPI.Query<RefRO<MessageShowEvent>>().WithEntityAccess())
            {
                // L'evento è già stato gestito nel metodo ShowNextMessage
                state.EntityManager.DestroyEntity(entity);
            }
            
            foreach (var (entity, hideEvent) in 
                     SystemAPI.Query<RefRO<MessageHideEvent>>().WithEntityAccess())
            {
                // L'evento è già stato gestito nel metodo UpdateCurrentMessage
                state.EntityManager.DestroyEntity(entity);
            }
        }
        
        /// <summary>
        /// Mostra il pannello dei messaggi con animazione
        /// </summary>
        private void ShowMessage(SystemState state)
        {
            if (_messagePanel != null)
            {
                _messagePanel.SetActive(true);
                _isMessageVisible = true;
                
                if (_messageAnimator != null)
                {
                    _messageAnimator.SetTrigger("Show");
                }
                else
                {
                    // Fallback se non c'è animator: semplice fade in
                    CanvasGroup canvasGroup = _messagePanel.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = _messagePanel.AddComponent<CanvasGroup>();
                    }
                    
                    canvasGroup.alpha = 0f;
                    // Utilizziamo UIManager per gestire le coroutine invece di FindObjectOfType
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.StartCoroutine(FadeIn(canvasGroup));
                    }
                    else
                    {
                        // Fallback senza coroutine
                        canvasGroup.alpha = 1f;
                    }
                }
            }
        }
        
        /// <summary>
        /// Nasconde il pannello dei messaggi con animazione
        /// </summary>
        private void HideCurrentMessage(SystemState state)
        {
            if (_messagePanel != null)
            {
                if (_messageAnimator != null)
                {
                    _messageAnimator.SetTrigger("Hide");
                    
                    // Disattiva il pannello dopo la fine dell'animazione
                    // Utilizziamo UIManager per gestire le coroutine invece di FindObjectOfType
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.StartCoroutine(DisableAfterDelay(_messagePanel, FADE_OUT_TIME));
                    }
                    else
                    {
                        // Fallback senza coroutine
                        GameObject.Destroy(_messagePanel, FADE_OUT_TIME);
                    }
                }
                else
                {
                    // Fallback se non c'è animator: semplice fade out
                    CanvasGroup canvasGroup = _messagePanel.GetComponent<CanvasGroup>();
                    if (canvasGroup == null)
                    {
                        canvasGroup = _messagePanel.AddComponent<CanvasGroup>();
                    }
                    
                    // Utilizziamo UIManager per gestire le coroutine invece di FindObjectOfType
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.StartCoroutine(FadeOut(canvasGroup, _messagePanel));
                    }
                    else
                    {
                        // Fallback senza coroutine
                        canvasGroup.alpha = 0f;
                        _messagePanel.SetActive(false);
                    }
                }
                
                _isMessageVisible = false;
            }
        }
        
        // Helper di animazione
        
        // Utilizza UnityEngine.Time.deltaTime perché queste coroutine vengono eseguite nel contesto MonoBehaviour
        private System.Collections.IEnumerator FadeIn(CanvasGroup canvasGroup)
        {
            float time = 0f;
            while (time < FADE_IN_TIME)
            {
                time += UnityEngine.Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / FADE_IN_TIME);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }
        
        private System.Collections.IEnumerator FadeOut(CanvasGroup canvasGroup, GameObject panel)
        {
            float time = 0f;
            while (time < FADE_OUT_TIME)
            {
                time += UnityEngine.Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / FADE_OUT_TIME);
                yield return null;
            }
            canvasGroup.alpha = 0f;
            panel.SetActive(false);
        }
        
        private System.Collections.IEnumerator DisableAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            obj.SetActive(false);
        }
    }
}