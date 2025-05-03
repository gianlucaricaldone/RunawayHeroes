using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.UI;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.ECS.Events.Handlers
{
    /// <summary>
    /// Sistema che gestisce gli eventi relativi all'interfaccia utente, 
    /// instradandoli verso i componenti UI appropriati.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct UIEventHandler : ISystem
    {
        // Query per i vari tipi di eventi UI
        private EntityQuery _genericUIEventsQuery;
        private EntityQuery _scoreUIEventsQuery;
        private EntityQuery _healthUIEventsQuery;
        private EntityQuery _objectiveUIEventsQuery;
        private EntityQuery _collectibleUIEventsQuery;
        private EntityQuery _notificationUIEventsQuery;
        
        /// <summary>
        /// Inizializza il sistema e configura le query per i diversi tipi di eventi UI
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per eventi UI generici
            _genericUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<UIGenericEvent, UIVisibilityEvent>()
                .Build(ref state);
                
            // Configura la query per eventi UI punteggio
            _scoreUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<ScoreUIUpdateEvent, FinalScoreUIEvent>()
                .Build(ref state);
                
            // Configura la query per eventi UI salute
            _healthUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<HealthUIUpdateEvent, HealthChangeAnimationEvent>()
                .Build(ref state);
                
            // Configura la query per eventi UI obiettivi
            _objectiveUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<ObjectiveUIUpdateEvent, MissionUIUpdateEvent>()
                .Build(ref state);
                
            // Configura la query per eventi UI collezionabili
            _collectibleUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<CollectibleFeedbackEvent, FragmentUIUpdateEvent>()
                .Build(ref state);
                
            // Configura la query per eventi UI notifiche
            _notificationUIEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<NotificationEvent, TooltipEvent>()
                .Build(ref state);
                
            // Richiedi un command buffer per modifiche strutturali
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Pulisce le risorse allocate
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }

        /// <summary>
        /// Elabora i vari tipi di eventi UI e li invia ai componenti UI appropriati
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il buffer di comandi per eventuali modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // 1. Elabora eventi UI generici (come apertura/chiusura pannelli)
            if (!_genericUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessGenericUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_genericUIEventsQuery, state.Dependency);
            }
            
            // 2. Elabora eventi UI punteggio
            if (!_scoreUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessScoreUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_scoreUIEventsQuery, state.Dependency);
            }
            
            // 3. Elabora eventi UI salute
            if (!_healthUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessHealthUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_healthUIEventsQuery, state.Dependency);
            }
            
            // 4. Elabora eventi UI obiettivi
            if (!_objectiveUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessObjectiveUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_objectiveUIEventsQuery, state.Dependency);
            }
            
            // 5. Elabora eventi UI collezionabili
            if (!_collectibleUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessCollectibleUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_collectibleUIEventsQuery, state.Dependency);
            }
            
            // 6. Elabora eventi UI notifiche
            if (!_notificationUIEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessNotificationUIEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_notificationUIEventsQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI generici
        /// </summary>
        [BurstCompile]
        private partial struct ProcessGenericUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di visibilità UI
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in UIVisibilityEvent visibilityEvent)
            {
                // Cerca l'entità UI di destinazione
                if (SystemAPI.Exists(visibilityEvent.UIEntityTarget))
                {
                    // Se l'entità ha un componente di visibilità UI, aggiornalo
                    if (SystemAPI.HasComponent<UIVisibilityComponent>(visibilityEvent.UIEntityTarget))
                    {
                        var visibility = SystemAPI.GetComponent<UIVisibilityComponent>(visibilityEvent.UIEntityTarget);
                        visibility.IsVisible = visibilityEvent.ShouldBeVisible;
                        visibility.FadeTime = visibilityEvent.FadeTime;
                        SystemAPI.SetComponent(visibilityEvent.UIEntityTarget, visibility);
                        
                        // Aggiungi eventi specifici per animazioni se necessario
                        if (visibilityEvent.ShouldAnimate)
                        {
                            // Crea un evento di animazione UI
                            Entity animEvent = ECB.CreateEntity(sortKey);
                            ECB.AddComponent(sortKey, animEvent, new UIAnimationEvent
                            {
                                UIEntityTarget = visibilityEvent.UIEntityTarget,
                                AnimationType = visibilityEvent.ShouldBeVisible ? 
                                    UIAnimationType.FadeIn : UIAnimationType.FadeOut,
                                Duration = visibilityEvent.FadeTime,
                                Delay = visibilityEvent.Delay
                            });
                        }
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi UI generici
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in UIGenericEvent genericEvent)
            {
                // In base al tipo di azione richesta, manipola i componenti UI
                switch (genericEvent.ActionType)
                {
                    case UIActionType.Highlight:
                        if (SystemAPI.Exists(genericEvent.UIEntityTarget) && 
                            SystemAPI.HasComponent<UIHighlightComponent>(genericEvent.UIEntityTarget))
                        {
                            var highlight = SystemAPI.GetComponent<UIHighlightComponent>(genericEvent.UIEntityTarget);
                            highlight.IsHighlighted = true;
                            highlight.HighlightColor = genericEvent.Color;
                            highlight.Duration = genericEvent.Duration;
                            highlight.PulseIntensity = genericEvent.Intensity;
                            SystemAPI.SetComponent(genericEvent.UIEntityTarget, highlight);
                        }
                        break;
                        
                    case UIActionType.Shake:
                        if (SystemAPI.Exists(genericEvent.UIEntityTarget))
                        {
                            // Crea un evento di animazione UI per shake
                            Entity animEvent = ECB.CreateEntity(sortKey);
                            ECB.AddComponent(sortKey, animEvent, new UIAnimationEvent
                            {
                                UIEntityTarget = genericEvent.UIEntityTarget,
                                AnimationType = UIAnimationType.Shake,
                                Duration = genericEvent.Duration,
                                Intensity = genericEvent.Intensity
                            });
                        }
                        break;
                        
                    case UIActionType.Pulse:
                        if (SystemAPI.Exists(genericEvent.UIEntityTarget))
                        {
                            // Crea un evento di animazione UI per pulse
                            Entity animEvent = ECB.CreateEntity(sortKey);
                            ECB.AddComponent(sortKey, animEvent, new UIAnimationEvent
                            {
                                UIEntityTarget = genericEvent.UIEntityTarget,
                                AnimationType = UIAnimationType.Pulse,
                                Duration = genericEvent.Duration,
                                Intensity = genericEvent.Intensity,
                                Color = genericEvent.Color
                            });
                        }
                        break;
                        
                    case UIActionType.Enable:
                        if (SystemAPI.Exists(genericEvent.UIEntityTarget) && 
                            SystemAPI.HasComponent<UIInteractableComponent>(genericEvent.UIEntityTarget))
                        {
                            var interactable = SystemAPI.GetComponent<UIInteractableComponent>(genericEvent.UIEntityTarget);
                            interactable.IsEnabled = true;
                            SystemAPI.SetComponent(genericEvent.UIEntityTarget, interactable);
                        }
                        break;
                        
                    case UIActionType.Disable:
                        if (SystemAPI.Exists(genericEvent.UIEntityTarget) && 
                            SystemAPI.HasComponent<UIInteractableComponent>(genericEvent.UIEntityTarget))
                        {
                            var interactable = SystemAPI.GetComponent<UIInteractableComponent>(genericEvent.UIEntityTarget);
                            interactable.IsEnabled = false;
                            SystemAPI.SetComponent(genericEvent.UIEntityTarget, interactable);
                        }
                        break;
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI relativi al punteggio
        /// </summary>
        [BurstCompile]
        private partial struct ProcessScoreUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di aggiornamento punteggio
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in ScoreUIUpdateEvent scoreEvent)
            {
                // Se esiste un'entità UI per il punteggio associata al giocatore
                if (SystemAPI.HasComponent<UIScoreDisplayComponent>(scoreEvent.PlayerEntity))
                {
                    var scoreDisplay = SystemAPI.GetComponent<UIScoreDisplayComponent>(scoreEvent.PlayerEntity);
                    
                    // Controlla se l'entità UI esiste ancora
                    if (SystemAPI.Exists(scoreDisplay.UIScoreEntity))
                    {
                        // Aggiorna l'informazione sul punteggio nell'entità UI
                        if (SystemAPI.HasComponent<UIScoreInfoComponent>(scoreDisplay.UIScoreEntity))
                        {
                            var scoreInfo = SystemAPI.GetComponent<UIScoreInfoComponent>(scoreDisplay.UIScoreEntity);
                            scoreInfo.CurrentScore = scoreEvent.NewScore;
                            scoreInfo.LastScoreIncrement = scoreEvent.ScoreIncrement;
                            scoreInfo.LastScoreSource = scoreEvent.ScoreSource;
                            scoreInfo.LastUpdateTime = SystemAPI.Time.ElapsedTime;
                            SystemAPI.SetComponent(scoreDisplay.UIScoreEntity, scoreInfo);
                        }
                        
                        // Crea evento di animazione per feedback punteggio
                        Entity animEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, animEvent, new UIScoreAnimationEvent
                        {
                            UIScoreEntity = scoreDisplay.UIScoreEntity,
                            Amount = scoreEvent.ScoreIncrement,
                            SourceType = scoreEvent.ScoreSource
                        });
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi di punteggio finale
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in FinalScoreUIEvent finalScoreEvent)
            {
                // Qui si inoltrerebbe l'evento alla UI di fine livello
                // Per semplicità, in questo handler creiamo un evento per
                // mostrare la schermata di punteggio finale
                
                Entity finalScoreScreenEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, finalScoreScreenEvent, new ShowFinalScoreScreenEvent
                {
                    LevelID = finalScoreEvent.LevelID,
                    PlayerEntity = finalScoreEvent.PlayerEntity,
                    FinalScore = finalScoreEvent.FinalScore,
                    IsHighScore = finalScoreEvent.IsHighScore,
                    TotalCollectibles = finalScoreEvent.TotalCollectibles,
                    TotalEnemiesDefeated = finalScoreEvent.TotalEnemiesDefeated,
                    TimeBonus = finalScoreEvent.TimeBonus
                });
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI relativi alla salute
        /// </summary>
        [BurstCompile]
        private partial struct ProcessHealthUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di aggiornamento salute
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in HealthUIUpdateEvent healthEvent)
            {
                // Se esiste un'entità UI per la salute associata al giocatore
                if (SystemAPI.HasComponent<UIHealthDisplayComponent>(healthEvent.PlayerEntity))
                {
                    var healthDisplay = SystemAPI.GetComponent<UIHealthDisplayComponent>(healthEvent.PlayerEntity);
                    
                    // Controlla se l'entità UI esiste ancora
                    if (SystemAPI.Exists(healthDisplay.UIHealthEntity))
                    {
                        // Aggiorna l'informazione sulla salute nell'entità UI
                        if (SystemAPI.HasComponent<UIHealthInfoComponent>(healthDisplay.UIHealthEntity))
                        {
                            var healthInfo = SystemAPI.GetComponent<UIHealthInfoComponent>(healthDisplay.UIHealthEntity);
                            healthInfo.CurrentHealth = healthEvent.NewHealth;
                            healthInfo.MaxHealth = healthEvent.MaxHealth;
                            healthInfo.LastHealthChange = healthEvent.HealthChange;
                            healthInfo.LastUpdateTime = SystemAPI.Time.ElapsedTime;
                            SystemAPI.SetComponent(healthDisplay.UIHealthEntity, healthInfo);
                        }
                        
                        // Se c'è stato un cambiamento di salute, crea un evento di animazione
                        if (healthEvent.HealthChange != 0)
                        {
                            Entity animEvent = ECB.CreateEntity(sortKey);
                            ECB.AddComponent(sortKey, animEvent, new UIHealthAnimationEvent
                            {
                                UIHealthEntity = healthDisplay.UIHealthEntity,
                                Amount = healthEvent.HealthChange,
                                IsCritical = healthEvent.IsCritical
                            });
                        }
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi di animazione cambio salute
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in HealthChangeAnimationEvent animEvent)
            {
                // Questo evento verrebbe inoltrato al sistema di animazione UI
                
                // Distruggi l'evento originale dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI relativi agli obiettivi
        /// </summary>
        [BurstCompile]
        private partial struct ProcessObjectiveUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di aggiornamento obiettivi
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in ObjectiveUIUpdateEvent objectiveEvent)
            {
                // Cerca l'entità UI per questo obiettivo
                Entity objectiveUIEntity = FindObjectiveUIEntity(objectiveEvent.ObjectiveID);
                
                if (SystemAPI.Exists(objectiveUIEntity))
                {
                    // Aggiorna l'informazione sull'obiettivo nell'entità UI
                    if (SystemAPI.HasComponent<UIObjectiveInfoComponent>(objectiveUIEntity))
                    {
                        var objectiveInfo = SystemAPI.GetComponent<UIObjectiveInfoComponent>(objectiveUIEntity);
                        objectiveInfo.Progress = objectiveEvent.Progress;
                        objectiveInfo.IsCompleted = objectiveEvent.IsCompleted;
                        objectiveInfo.IsFailed = objectiveEvent.IsFailed;
                        objectiveInfo.LastUpdateTime = SystemAPI.Time.ElapsedTime;
                        SystemAPI.SetComponent(objectiveUIEntity, objectiveInfo);
                    }
                    
                    // Crea evento di animazione in base allo stato
                    UIAnimationType animType = UIAnimationType.Progress;
                    if (objectiveEvent.IsCompleted)
                        animType = UIAnimationType.Complete;
                    else if (objectiveEvent.IsFailed)
                        animType = UIAnimationType.Fail;
                        
                    // Crea l'evento di animazione
                    Entity animEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, animEvent, new UIAnimationEvent
                    {
                        UIEntityTarget = objectiveUIEntity,
                        AnimationType = animType,
                        Duration = 0.5f
                    });
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi di aggiornamento missione
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in MissionUIUpdateEvent missionEvent)
            {
                // Cerca l'entità UI per questa missione
                Entity missionUIEntity = FindMissionUIEntity(missionEvent.MissionID);
                
                if (SystemAPI.Exists(missionUIEntity))
                {
                    // Aggiorna l'informazione sulla missione nell'entità UI
                    if (SystemAPI.HasComponent<UIMissionInfoComponent>(missionUIEntity))
                    {
                        var missionInfo = SystemAPI.GetComponent<UIMissionInfoComponent>(missionUIEntity);
                        missionInfo.CompletedObjectives = missionEvent.CompletedObjectives;
                        missionInfo.TotalObjectives = missionEvent.TotalObjectives;
                        missionInfo.IsCompleted = missionEvent.IsCompleted;
                        missionInfo.IsFailed = missionEvent.IsFailed;
                        SystemAPI.SetComponent(missionUIEntity, missionInfo);
                    }
                    
                    // Crea evento di animazione in base allo stato
                    if (missionEvent.IsCompleted || missionEvent.IsFailed)
                    {
                        UIAnimationType animType = missionEvent.IsCompleted ? 
                            UIAnimationType.Complete : UIAnimationType.Fail;
                            
                        // Crea l'evento di animazione
                        Entity animEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, animEvent, new UIAnimationEvent
                        {
                            UIEntityTarget = missionUIEntity,
                            AnimationType = animType,
                            Duration = 1.0f
                        });
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            // Helper per trovare l'entità UI per un obiettivo specifico
            // In un'implementazione reale, questo verrebbe gestito attraverso 
            // un sistema più avanzato di lookup o registry
            private Entity FindObjectiveUIEntity(int objectiveID)
            {
                // Implementazione simulata
                return Entity.Null;
            }
            
            // Helper per trovare l'entità UI per una missione specifica
            private Entity FindMissionUIEntity(int missionID)
            {
                // Implementazione simulata
                return Entity.Null;
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI relativi ai collezionabili
        /// </summary>
        [BurstCompile]
        private partial struct ProcessCollectibleUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di feedback collezionabili
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in CollectibleFeedbackEvent feedbackEvent)
            {
                // Crea un evento per mostrare popup di collezione
                Entity popupEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, popupEvent, new ShowCollectiblePopupEvent
                {
                    CollectorEntity = feedbackEvent.CollectorEntity,
                    CollectibleType = feedbackEvent.CollectibleType,
                    Value = feedbackEvent.Value,
                    WorldPosition = feedbackEvent.CollectionPoint
                });
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi UI per frammenti
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in FragmentUIUpdateEvent fragmentEvent)
            {
                // Cerca l'entità UI per l'inventario frammenti
                if (SystemAPI.HasComponent<UIFragmentInventoryComponent>(fragmentEvent.CollectorEntity))
                {
                    var inventoryDisplay = SystemAPI.GetComponent<UIFragmentInventoryComponent>(fragmentEvent.CollectorEntity);
                    
                    if (SystemAPI.Exists(inventoryDisplay.UIInventoryEntity))
                    {
                        // Aggiorna UI inventario frammenti
                        // (implementazione specifica dipendente dalla struttura UI)
                        
                        // Crea un evento di animazione per il frammento raccolto
                        Entity animEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, animEvent, new UIFragmentCollectionAnimEvent
                        {
                            FragmentType = fragmentEvent.FragmentType,
                            UIInventoryEntity = inventoryDisplay.UIInventoryEntity
                        });
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi UI relativi alle notifiche
        /// </summary>
        [BurstCompile]
        private partial struct ProcessNotificationUIEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            /// <summary>
            /// Elabora eventi di notifica
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in NotificationEvent notificationEvent)
            {
                // Cerca il gestore delle notifiche UI
                Entity notificationManager = FindNotificationManager();
                
                if (SystemAPI.Exists(notificationManager))
                {
                    // Crea un'entità per la notifica
                    Entity newNotification = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, newNotification, new UINotificationComponent
                    {
                        Type = notificationEvent.NotificationType,
                        Priority = notificationEvent.Priority,
                        Duration = notificationEvent.Duration,
                        TextID = notificationEvent.TextID,
                        CreationTime = SystemAPI.Time.ElapsedTime
                    });
                    
                    // Collega la notifica al gestore
                    if (SystemAPI.HasComponent<UINotificationManagerComponent>(notificationManager))
                    {
                        var manager = SystemAPI.GetComponent<UINotificationManagerComponent>(notificationManager);
                        manager.ActiveNotificationsCount++;
                        SystemAPI.SetComponent(notificationManager, manager);
                        
                        // Aggiorna il buffer di notifiche attive
                        // (simulazione - in pratica si userebbe un approccio con DynamicBuffer)
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Elabora eventi tooltip
            /// </summary>
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in TooltipEvent tooltipEvent)
            {
                // Cerca il gestore dei tooltip UI
                Entity tooltipManager = FindTooltipManager();
                
                if (SystemAPI.Exists(tooltipManager))
                {
                    // Se è una richiesta di mostrare un tooltip
                    if (tooltipEvent.Show)
                    {
                        // Crea un'entità per il tooltip
                        Entity newTooltip = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, newTooltip, new UITooltipComponent
                        {
                            TargetUIElement = tooltipEvent.TargetUIElement,
                            TextID = tooltipEvent.TextID,
                            Position = tooltipEvent.Position,
                            Width = tooltipEvent.Width,
                            CreationTime = SystemAPI.Time.ElapsedTime
                        });
                        
                        // Aggiorna il gestore dei tooltip
                        if (SystemAPI.HasComponent<UITooltipManagerComponent>(tooltipManager))
                        {
                            var manager = SystemAPI.GetComponent<UITooltipManagerComponent>(tooltipManager);
                            manager.CurrentTooltipEntity = newTooltip;
                            SystemAPI.SetComponent(tooltipManager, manager);
                        }
                    }
                    // Altrimenti, nascondi il tooltip corrente
                    else if (SystemAPI.HasComponent<UITooltipManagerComponent>(tooltipManager))
                    {
                        var manager = SystemAPI.GetComponent<UITooltipManagerComponent>(tooltipManager);
                        if (SystemAPI.Exists(manager.CurrentTooltipEntity))
                        {
                            ECB.DestroyEntity(sortKey, manager.CurrentTooltipEntity);
                        }
                        manager.CurrentTooltipEntity = Entity.Null;
                        SystemAPI.SetComponent(tooltipManager, manager);
                    }
                }
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
            
            // Helper per trovare il gestore delle notifiche
            private Entity FindNotificationManager()
            {
                // Implementazione simulata
                return Entity.Null;
            }
            
            // Helper per trovare il gestore dei tooltip
            private Entity FindTooltipManager()
            {
                // Implementazione simulata
                return Entity.Null;
            }
        }
    }
    
    #region Componenti e Eventi UI
    
    /// <summary>
    /// Tipi di animazione UI
    /// </summary>
    public enum UIAnimationType : byte
    {
        None = 0,
        FadeIn = 1,
        FadeOut = 2,
        Shake = 3,
        Pulse = 4,
        Progress = 5,
        Complete = 6,
        Fail = 7
    }
    
    /// <summary>
    /// Tipi di azioni UI
    /// </summary>
    public enum UIActionType : byte
    {
        None = 0,
        Show = 1,
        Hide = 2,
        Highlight = 3,
        Shake = 4,
        Pulse = 5,
        Enable = 6,
        Disable = 7
    }
    
    /// <summary>
    /// Evento per la visibilità di elementi UI
    /// </summary>
    [System.Serializable]
    public struct UIVisibilityEvent : IComponentData
    {
        public Entity UIEntityTarget;    // Entità UI target
        public bool ShouldBeVisible;     // Se deve essere visibile
        public bool ShouldAnimate;       // Se deve usare un'animazione
        public float FadeTime;           // Tempo di fade
        public float Delay;              // Ritardo prima dell'animazione
    }
    
    /// <summary>
    /// Evento generico per azioni UI
    /// </summary>
    [System.Serializable]
    public struct UIGenericEvent : IComponentData
    {
        public Entity UIEntityTarget;    // Entità UI target
        public UIActionType ActionType;  // Tipo di azione
        public float4 Color;             // Colore (per highlight)
        public float Duration;           // Durata dell'effetto
        public float Intensity;          // Intensità dell'effetto
    }
    
    /// <summary>
    /// Evento per animazioni UI
    /// </summary>
    [System.Serializable]
    public struct UIAnimationEvent : IComponentData
    {
        public Entity UIEntityTarget;    // Entità UI target
        public UIAnimationType AnimationType; // Tipo di animazione
        public float Duration;           // Durata dell'animazione
        public float Delay;              // Ritardo prima dell'animazione
        public float Intensity;          // Intensità dell'animazione
        public float4 Color;             // Colore (per animazioni colorate)
    }
    
    /// <summary>
    /// Evento per animazioni punteggio
    /// </summary>
    [System.Serializable]
    public struct UIScoreAnimationEvent : IComponentData
    {
        public Entity UIScoreEntity;     // Entità UI punteggio
        public float Amount;             // Quantità di punteggio
        public byte SourceType;          // Tipo di fonte
    }
    
    /// <summary>
    /// Evento per animazioni salute
    /// </summary>
    [System.Serializable]
    public struct UIHealthAnimationEvent : IComponentData
    {
        public Entity UIHealthEntity;    // Entità UI salute
        public float Amount;             // Quantità di cambio
        public bool IsCritical;          // Se il cambio è critico
    }
    
    /// <summary>
    /// Evento per popup collezione
    /// </summary>
    [System.Serializable]
    public struct ShowCollectiblePopupEvent : IComponentData
    {
        public Entity CollectorEntity;   // Entità che ha raccolto
        public byte CollectibleType;     // Tipo di collezionabile
        public float Value;              // Valore
        public float3 WorldPosition;     // Posizione nel mondo
    }
    
    /// <summary>
    /// Evento per animazione frammento
    /// </summary>
    [System.Serializable]
    public struct UIFragmentCollectionAnimEvent : IComponentData
    {
        public byte FragmentType;        // Tipo di frammento
        public Entity UIInventoryEntity; // Entità UI inventario
    }
    
    /// <summary>
    /// Evento per schermata punteggio finale
    /// </summary>
    [System.Serializable]
    public struct ShowFinalScoreScreenEvent : IComponentData
    {
        public int LevelID;              // ID del livello
        public Entity PlayerEntity;      // Entità giocatore
        public float FinalScore;         // Punteggio finale
        public bool IsHighScore;         // Se è un nuovo record
        public int TotalCollectibles;    // Collezionabili raccolti
        public int TotalEnemiesDefeated; // Nemici sconfitti
        public float TimeBonus;          // Bonus tempo
    }
    
    /// <summary>
    /// Evento notifica
    /// </summary>
    [System.Serializable]
    public struct NotificationEvent : IComponentData
    {
        public byte NotificationType;    // Tipo di notifica
        public byte Priority;            // Priorità (0-5)
        public float Duration;           // Durata visualizzazione
        public int TextID;               // ID del testo localizzato
    }
    
    /// <summary>
    /// Evento tooltip
    /// </summary>
    [System.Serializable]
    public struct TooltipEvent : IComponentData
    {
        public Entity TargetUIElement;   // Elemento UI target
        public bool Show;                // Se mostrare o nascondere
        public int TextID;               // ID del testo localizzato
        public float2 Position;          // Posizione (se non legata a un target)
        public float Width;              // Larghezza tooltip
    }
    
    /// <summary>
    /// Evento cambio salute animazione
    /// </summary>
    [System.Serializable]
    public struct HealthChangeAnimationEvent : IComponentData
    {
        public Entity TargetEntity;      // Entità target
        public float HealthChange;       // Cambio di salute
        public bool IsCritical;          // Se critico
        public float3 WorldPosition;     // Posizione nel mondo
    }
    
    /// <summary>
    /// Evento aggiornamento missione UI
    /// </summary>
    [System.Serializable]
    public struct MissionUIUpdateEvent : IComponentData
    {
        public int MissionID;            // ID missione
        public byte CompletedObjectives; // Obiettivi completati
        public byte TotalObjectives;     // Obiettivi totali
        public bool IsCompleted;         // Se completata
        public bool IsFailed;            // Se fallita
    }
    
    #endregion
    
    #region Componenti UI
    
    /// <summary>
    /// Componente per la visibilità UI
    /// </summary>
    [System.Serializable]
    public struct UIVisibilityComponent : IComponentData
    {
        public bool IsVisible;           // Se è visibile
        public float FadeTime;           // Tempo di fade
    }
    
    /// <summary>
    /// Componente per highlight UI
    /// </summary>
    [System.Serializable]
    public struct UIHighlightComponent : IComponentData
    {
        public bool IsHighlighted;       // Se è evidenziato
        public float4 HighlightColor;    // Colore dell'evidenziazione
        public float Duration;           // Durata
        public float PulseIntensity;     // Intensità pulsazione
    }
    
    /// <summary>
    /// Componente per interagibilità UI
    /// </summary>
    [System.Serializable]
    public struct UIInteractableComponent : IComponentData
    {
        public bool IsEnabled;           // Se è abilitato
        public bool IsHovered;           // Se è sotto il puntatore
        public bool IsPressed;           // Se è premuto
    }
    
    /// <summary>
    /// Componente per display punteggio
    /// </summary>
    [System.Serializable]
    public struct UIScoreDisplayComponent : IComponentData
    {
        public Entity UIScoreEntity;     // Entità UI per il punteggio
    }
    
    /// <summary>
    /// Componente per info punteggio UI
    /// </summary>
    [System.Serializable]
    public struct UIScoreInfoComponent : IComponentData
    {
        public float CurrentScore;       // Punteggio corrente
        public float LastScoreIncrement; // Ultimo incremento
        public byte LastScoreSource;     // Ultima fonte
        public double LastUpdateTime;    // Ultimo aggiornamento
    }
    
    /// <summary>
    /// Componente per display salute
    /// </summary>
    [System.Serializable]
    public struct UIHealthDisplayComponent : IComponentData
    {
        public Entity UIHealthEntity;    // Entità UI per la salute
    }
    
    /// <summary>
    /// Componente per info salute UI
    /// </summary>
    [System.Serializable]
    public struct UIHealthInfoComponent : IComponentData
    {
        public float CurrentHealth;      // Salute corrente
        public float MaxHealth;          // Salute massima
        public float LastHealthChange;   // Ultimo cambio
        public double LastUpdateTime;    // Ultimo aggiornamento
    }
    
    /// <summary>
    /// Componente per info obiettivo UI
    /// </summary>
    [System.Serializable]
    public struct UIObjectiveInfoComponent : IComponentData
    {
        public int ObjectiveID;          // ID obiettivo
        public float Progress;           // Progresso (0-1)
        public bool IsCompleted;         // Se completato
        public bool IsFailed;            // Se fallito
        public double LastUpdateTime;    // Ultimo aggiornamento
    }
    
    /// <summary>
    /// Componente per info missione UI
    /// </summary>
    [System.Serializable]
    public struct UIMissionInfoComponent : IComponentData
    {
        public int MissionID;            // ID missione
        public byte CompletedObjectives; // Obiettivi completati
        public byte TotalObjectives;     // Obiettivi totali
        public bool IsCompleted;         // Se completata
        public bool IsFailed;            // Se fallita
    }
    
    /// <summary>
    /// Componente per display inventario frammenti
    /// </summary>
    [System.Serializable]
    public struct UIFragmentInventoryComponent : IComponentData
    {
        public Entity UIInventoryEntity; // Entità UI per l'inventario
    }
    
    /// <summary>
    /// Componente per gestione notifiche
    /// </summary>
    [System.Serializable]
    public struct UINotificationManagerComponent : IComponentData
    {
        public byte MaxNotifications;    // Numero massimo di notifiche visualizzabili
        public byte ActiveNotificationsCount; // Notifiche attualmente visualizzate
        public float DefaultDuration;    // Durata predefinita
    }
    
    /// <summary>
    /// Componente per notifica
    /// </summary>
    [System.Serializable]
    public struct UINotificationComponent : IComponentData
    {
        public byte Type;                // Tipo di notifica
        public byte Priority;            // Priorità
        public float Duration;           // Durata
        public int TextID;               // ID testo localizzato
        public double CreationTime;      // Momento creazione
    }
    
    /// <summary>
    /// Componente per gestione tooltip
    /// </summary>
    [System.Serializable]
    public struct UITooltipManagerComponent : IComponentData
    {
        public Entity CurrentTooltipEntity; // Tooltip attualmente visualizzato
    }
    
    /// <summary>
    /// Componente per tooltip
    /// </summary>
    [System.Serializable]
    public struct UITooltipComponent : IComponentData
    {
        public Entity TargetUIElement;   // Elemento UI target
        public int TextID;               // ID testo localizzato
        public float2 Position;          // Posizione
        public float Width;              // Larghezza
        public double CreationTime;      // Momento creazione
    }
    
    #endregion
}
