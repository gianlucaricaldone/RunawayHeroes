using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.UI;
using RunawayHeroes.Runtime.Managers;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Sistema che gestisce il feedback visivo per gli obiettivi tutorial.
    /// Mostra gli obiettivi correnti, aggiorna l'interfaccia e fornisce feedback al completamento.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct ObjectiveFeedbackSystem : ISystem
    {
        #region Private Fields
        
        // Riferimenti UI
        private GameObject _objectivesPanel;
        private RectTransform _objectivesContainer;
        private Dictionary<int, GameObject> _activeObjectiveItems;
        
        // Prefab
        private GameObject _objectiveItemPrefab;
        
        // Riferimenti sistema
        private EntityQuery _activeObjectivesQuery;
        
        #endregion
        
        #region Initialization
        
        public void OnCreate(ref SystemState state)
        {
            _activeObjectivesQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<ObjectiveComponent>(),
                ComponentType.ReadOnly<ActiveObjectiveTag>()
            );
            
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi aggiornamento solo se ci sono obiettivi attivi
            state.RequireForUpdate(_activeObjectivesQuery);
            
            // Inizializza il dizionario
            _activeObjectiveItems = new Dictionary<int, GameObject>();
        }
        
        public void OnStartRunning(ref SystemState state)
        {
            // Inizializza riferimenti UI
            InitializeUIReferences();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Pulisci le risorse
            foreach (var item in _activeObjectiveItems.Values)
            {
                if (item != null)
                {
                    GameObject.Destroy(item);
                }
            }
            
            _activeObjectiveItems.Clear();
        }
        
        #endregion
        
        #region System Lifecycle
        
        public void OnUpdate(ref SystemState state)
        {
            // Aggiorna il pannello obiettivi
            UpdateObjectivesPanel(ref state);
            
            // Gestisce eventi di completamento obiettivi
            ProcessObjectiveCompletionEvents(ref state);
            
            // Gestisce eventi di progresso obiettivi
            ProcessObjectiveProgressEvents(ref state);
        }
        
        #endregion
        
        #region UI Management
        
        /// <summary>
        /// Inizializza i riferimenti all'interfaccia utente
        /// </summary>
        private void InitializeUIReferences()
        {
            // Cerca il pannello obiettivi esistente
            _objectivesPanel = GameObject.Find("ObjectivesPanel");
            
            // Se non esiste, lo creiamo dinamicamente
            if (_objectivesPanel == null)
            {
                CreateObjectivesPanelDynamically();
            }
            else
            {
                _objectivesContainer = _objectivesPanel.transform.Find("Content") as RectTransform;
            }
            
            // Carichiamo il prefab per gli elementi obiettivo
            // In una implementazione reale questo verrebbe caricato da Resources o AddressableAssets
            _objectiveItemPrefab = Resources.Load<GameObject>("UI/ObjectiveItem");
            
            // Se non esiste, creiamo un prefab generico
            if (_objectiveItemPrefab == null)
            {
                _objectiveItemPrefab = CreateObjectiveItemPrefab();
            }
        }
        
        /// <summary>
        /// Crea dinamicamente un pannello per gli obiettivi
        /// </summary>
        private void CreateObjectivesPanelDynamically()
        {
            // Cerca il canvas di gioco
            Canvas gameCanvas = GameObject.FindFirstObjectByType<Canvas>();
            if (gameCanvas == null)
            {
                Debug.LogError("Nessun canvas trovato per il pannello obiettivi!");
                return;
            }
            
            // Crea il pannello obiettivi
            _objectivesPanel = new GameObject("ObjectivesPanel");
            _objectivesPanel.transform.SetParent(gameCanvas.transform, false);
            
            // Configura RectTransform
            RectTransform rectTransform = _objectivesPanel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.02f, 0.7f); // In alto a sinistra
            rectTransform.anchorMax = new Vector2(0.3f, 0.98f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            
            // Aggiungi un'immagine di background
            Image background = _objectivesPanel.AddComponent<Image>();
            background.color = new Color(0.1f, 0.1f, 0.1f, 0.7f);
            
            // Aggiungi un titolo
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_objectivesPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, -5);
            
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "OBIETTIVI";
            titleText.fontSize = 20;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.9f, 0.9f, 0.9f);
            
            // Crea contenitore per obiettivi
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(_objectivesPanel.transform, false);
            _objectivesContainer = contentObj.AddComponent<RectTransform>();
            _objectivesContainer.anchorMin = new Vector2(0, 0);
            _objectivesContainer.anchorMax = new Vector2(1, 0.85f);
            _objectivesContainer.offsetMin = new Vector2(5, 5);
            _objectivesContainer.offsetMax = new Vector2(-5, -5);
            
            // Aggiungi layout verticale
            VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 5;
            layout.padding = new RectOffset(5, 5, 5, 5);
            
            // Permetti adattamento contenuto
            ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // All'inizio nascondi il pannello
            _objectivesPanel.SetActive(false);
        }
        
        /// <summary>
        /// Crea un prefab base per gli elementi obiettivo
        /// </summary>
        private GameObject CreateObjectiveItemPrefab()
        {
            // Crea un GameObject temporaneo
            GameObject prefab = new GameObject("ObjectiveItem");
            
            // Aggiungi RectTransform
            RectTransform rectTransform = prefab.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(0, 30);
            
            // Aggiungi layout orizzontale
            HorizontalLayoutGroup layout = prefab.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 10;
            layout.padding = new RectOffset(5, 5, 2, 2);
            
            // Aggiungi un'icona di stato
            GameObject iconObj = new GameObject("StatusIcon");
            iconObj.transform.SetParent(prefab.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24, 24);
            
            Image iconImage = iconObj.AddComponent<Image>();
            // In una implementazione reale, caricheremo gli sprite appropriati
            iconImage.color = new Color(0.8f, 0.8f, 0.8f);
            
            // Aggiungi testo descrizione
            GameObject textObj = new GameObject("Description");
            textObj.transform.SetParent(prefab.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(0, 30);
            
            TextMeshProUGUI descText = textObj.AddComponent<TextMeshProUGUI>();
            descText.fontSize = 16;
            descText.color = new Color(0.9f, 0.9f, 0.9f);
            descText.text = "Obiettivo";
            
            // Aggiungi testo progresso
            GameObject progressObj = new GameObject("Progress");
            progressObj.transform.SetParent(prefab.transform, false);
            RectTransform progressRect = progressObj.AddComponent<RectTransform>();
            progressRect.sizeDelta = new Vector2(60, 30);
            
            TextMeshProUGUI progressText = progressObj.AddComponent<TextMeshProUGUI>();
            progressText.fontSize = 16;
            progressText.alignment = TextAlignmentOptions.Right;
            progressText.color = new Color(0.7f, 0.7f, 1.0f);
            progressText.text = "0/0";
            
            return prefab;
        }
        
        #endregion
        
        #region Objective Panel Update
        
        /// <summary>
        /// Aggiorna il pannello degli obiettivi
        /// </summary>
        private void UpdateObjectivesPanel(ref SystemState state)
        {
            // Se non ci sono obiettivi attivi, nasconde il pannello
            if (_activeObjectivesQuery.IsEmpty)
            {
                if (_objectivesPanel != null)
                {
                    _objectivesPanel.SetActive(false);
                }
                return;
            }
            
            // Mostra il pannello obiettivi
            if (_objectivesPanel != null)
            {
                _objectivesPanel.SetActive(true);
            }
            
            // Tiene traccia degli obiettivi ancora attivi
            HashSet<int> currentActiveObjectives = new HashSet<int>();
            
            // Ottiene l'EntityManager
            var entityManager = state.EntityManager;
            
            // Aggiorna o crea gli item per gli obiettivi attivi
            foreach (var entity in _activeObjectivesQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                int objectiveId = entity.Index;
                currentActiveObjectives.Add(objectiveId);
                
                // Assicurati che l'entità abbia i componenti necessari
                if (entityManager.HasComponent<ObjectiveComponent>(entity) && 
                    entityManager.HasComponent<ObjectiveProgressComponent>(entity))
                {
                    var objective = entityManager.GetComponentData<ObjectiveComponent>(entity);
                    var progress = entityManager.GetComponentData<ObjectiveProgressComponent>(entity);
                    
                    // Se l'obiettivo ha già un item UI, aggiorna i dati
                    if (_activeObjectiveItems.TryGetValue(objectiveId, out GameObject itemObj))
                    {
                        UpdateObjectiveItem(itemObj, objective, progress);
                    }
                    else
                    {
                        // Crea un nuovo item per questo obiettivo
                        GameObject newItem = CreateObjectiveItem(objective, progress);
                        if (newItem != null)
                        {
                            _activeObjectiveItems[objectiveId] = newItem;
                        }
                    }
                }
            }
            
            // Rimuovi gli item per obiettivi non più attivi
            List<int> objectivesToRemove = new List<int>();
            foreach (var kvp in _activeObjectiveItems)
            {
                if (!currentActiveObjectives.Contains(kvp.Key))
                {
                    // Distruggi l'item UI
                    if (kvp.Value != null)
                    {
                        GameObject.Destroy(kvp.Value);
                    }
                    objectivesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (int id in objectivesToRemove)
            {
                _activeObjectiveItems.Remove(id);
            }
        }
        
        /// <summary>
        /// Crea un nuovo elemento UI per un obiettivo
        /// </summary>
        private GameObject CreateObjectiveItem(ObjectiveComponent objective, ObjectiveProgressComponent progress)
        {
            if (_objectivesContainer == null || _objectiveItemPrefab == null)
                return null;
                
            // Istanzia l'item dal prefab
            GameObject itemObj = GameObject.Instantiate(_objectiveItemPrefab, _objectivesContainer);
            
            // Inizializza i dati
            UpdateObjectiveItem(itemObj, objective, progress);
            
            return itemObj;
        }
        
        /// <summary>
        /// Aggiorna i dati di un elemento obiettivo esistente
        /// </summary>
        private void UpdateObjectiveItem(GameObject itemObj, ObjectiveComponent objective, ObjectiveProgressComponent progress)
        {
            // Ottieni i componenti UI
            Transform descTrans = itemObj.transform.Find("Description");
            Transform progressTrans = itemObj.transform.Find("Progress");
            Transform iconTrans = itemObj.transform.Find("StatusIcon");
            
            // Aggiorna testo descrizione
            if (descTrans != null)
            {
                TextMeshProUGUI descText = descTrans.GetComponent<TextMeshProUGUI>();
                if (descText != null)
                {
                    descText.text = objective.Description.ToString();
                    
                    // Cambia il colore in base alla completezza
                    if (objective.IsCompleted)
                    {
                        descText.color = new Color(0.5f, 1.0f, 0.5f); // Verde chiaro per completato
                    }
                    else if (objective.IsOptional)
                    {
                        descText.color = new Color(0.9f, 0.9f, 0.6f); // Giallo chiaro per opzionale
                    }
                    else
                    {
                        descText.color = new Color(0.9f, 0.9f, 0.9f); // Bianco per normale
                    }
                }
            }
            
            // Aggiorna testo progresso
            if (progressTrans != null)
            {
                TextMeshProUGUI progressText = progressTrans.GetComponent<TextMeshProUGUI>();
                if (progressText != null)
                {
                    if (objective.ObjectiveType == 5) // Obiettivo a tempo
                    {
                        float timeLeft = progress.TimeLimit - (Time.time - progress.StartTime);
                        if (timeLeft < 0) timeLeft = 0;
                        progressText.text = $"{timeLeft:F1}s";
                    }
                    else // Obiettivo con contatore
                    {
                        progressText.text = $"{progress.Count}/{progress.MaxCount}";
                    }
                    
                    // Cambia colore in base al progresso
                    float progressRatio = (float)progress.Count / progress.MaxCount;
                    if (progressRatio < 0.3f)
                    {
                        progressText.color = new Color(1.0f, 0.5f, 0.5f); // Rosso per poco progresso
                    }
                    else if (progressRatio < 0.7f)
                    {
                        progressText.color = new Color(1.0f, 0.9f, 0.5f); // Giallo per progresso medio
                    }
                    else
                    {
                        progressText.color = new Color(0.5f, 1.0f, 0.5f); // Verde per buon progresso
                    }
                }
            }
            
            // Aggiorna icona stato
            if (iconTrans != null)
            {
                Image iconImage = iconTrans.GetComponent<Image>();
                if (iconImage != null)
                {
                    Color iconColor;
                    
                    if (objective.IsCompleted)
                    {
                        iconColor = new Color(0.3f, 0.9f, 0.3f); // Verde per completato
                    }
                    else if (objective.IsOptional)
                    {
                        iconColor = new Color(0.9f, 0.8f, 0.3f); // Giallo per opzionale
                    }
                    else
                    {
                        iconColor = new Color(0.3f, 0.6f, 1.0f); // Blu per obiettivo standard
                    }
                    
                    iconImage.color = iconColor;
                }
            }
        }
        
        #endregion
        
        #region Event Processing
        
        /// <summary>
        /// Processa eventi di completamento obiettivi
        /// </summary>
        private void ProcessObjectiveCompletionEvents(ref SystemState state)
        {
            // Crea un buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Ottieni EntityManager
            var entityManager = state.EntityManager;
            
            // Query per eventi di completamento
            var completionEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<ObjectiveCompletedEvent>());
            
            // Itera sugli eventi di completamento
            foreach (var entity in completionEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var completionEvent = entityManager.GetComponentData<ObjectiveCompletedEvent>(entity);
                
                // Ottieni dati sull'obiettivo completato
                if (entityManager.Exists(completionEvent.ObjectiveEntity) && 
                    entityManager.HasComponent<ObjectiveComponent>(completionEvent.ObjectiveEntity))
                {
                    var objective = entityManager.GetComponentData<ObjectiveComponent>(completionEvent.ObjectiveEntity);
                    
                    // Crea un messaggio UI per il completamento
                    Entity messageEntity = commandBuffer.CreateEntity();
                    
                    string message = objective.IsOptional ?
                        $"Obiettivo Bonus Completato! {objective.Description}" :
                        $"Obiettivo Completato! {objective.Description}";
                        
                    byte messageType = objective.IsOptional ? (byte)1 : (byte)0; // Verde per bonus, Blu per normale
                    
                    commandBuffer.AddComponent(messageEntity, new UIMessageComponent
                    {
                        Message = new Unity.Collections.FixedString128Bytes(message),
                        Duration = 4.0f,
                        RemainingTime = 4.0f,
                        MessageType = messageType,
                        IsPersistent = false,
                        MessageId = 500 + UnityEngine.Random.Range(0, 500)
                    });
                    
                    commandBuffer.AddComponent(messageEntity, new QueuedMessageTag
                    {
                        QueuePosition = 0
                    });
                    
                    // Riproduci effetto audio di completamento (in un'implementazione completa)
                    if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
                    {
                        string soundName = objective.IsOptional ? "ObjectiveBonusComplete" : "ObjectiveComplete";
                        RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX(soundName);
                    }
                }
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
        }
        
        /// <summary>
        /// Processa eventi di progresso obiettivi
        /// </summary>
        private void ProcessObjectiveProgressEvents(ref SystemState state)
        {
            // Ottieni EntityManager
            var entityManager = state.EntityManager;
            
            // Query per eventi di progresso
            var progressEventQuery = state.GetEntityQuery(ComponentType.ReadOnly<ObjectiveProgressEvent>());
            
            // Itera sugli eventi di progresso
            foreach (var entity in progressEventQuery.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                var progressEvent = entityManager.GetComponentData<ObjectiveProgressEvent>(entity);
                
                // Solo se c'è un cambio significativo (o il primo punto di progresso)
                if (progressEvent.NewValue > progressEvent.PreviousValue && 
                    (progressEvent.PreviousValue == 0 || 
                     progressEvent.NewValue == progressEvent.RequiredValue ||
                     (progressEvent.NewValue - progressEvent.PreviousValue) >= (progressEvent.RequiredValue / 4)))
                {
                    // Riproduci effetto audio di progresso
                    if (RunawayHeroes.Runtime.Managers.AudioManager.Instance != null)
                    {
                        RunawayHeroes.Runtime.Managers.AudioManager.Instance.PlaySFX("ObjectiveProgress");
                    }
                    
                    // Se l'obiettivo è completato
                    if (progressEvent.NewValue >= progressEvent.RequiredValue)
                    {
                        // Aggiorna lo stato dell'obiettivo
                        if (entityManager.Exists(progressEvent.ObjectiveEntity) && 
                            entityManager.HasComponent<ObjectiveComponent>(progressEvent.ObjectiveEntity))
                        {
                            var objective = entityManager.GetComponentData<ObjectiveComponent>(progressEvent.ObjectiveEntity);
                            objective.IsCompleted = true;
                            objective.CurrentProgress = progressEvent.NewValue;
                            entityManager.SetComponentData(progressEvent.ObjectiveEntity, objective);
                            
                            // Crea un evento di completamento
                            Entity completionEvent = entityManager.CreateEntity();
                            entityManager.AddComponentData(completionEvent, new ObjectiveCompletedEvent
                            {
                                ObjectiveEntity = progressEvent.ObjectiveEntity,
                                ObjectiveType = objective.ObjectiveType,
                                ScenarioId = objective.ScenarioId,
                                WasRequired = !objective.IsOptional
                            });
                        }
                    }
                }
                
                // Rimuovi l'evento dopo l'elaborazione
                entityManager.DestroyEntity(entity);
            }
        }
        
        #endregion
    }
}