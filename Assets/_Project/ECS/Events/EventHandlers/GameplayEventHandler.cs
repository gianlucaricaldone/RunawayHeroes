using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Systems.Gameplay; // For FragmentInventoryComponent

namespace RunawayHeroes.ECS.Events.Handlers
{
    /// <summary>
    /// Sistema che gestisce eventi generali di gameplay come completamento obiettivi,
    /// avanzamento di livello, e altri eventi di gioco
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct GameplayEventHandler : ISystem
    {
        #region Private Fields
        
        // Query per diversi tipi di eventi
        private EntityQuery _objectiveCompletedEventsQuery;
        private EntityQuery _objectiveFailedEventsQuery;
        private EntityQuery _objectiveUpdatedEventsQuery;
        
        private EntityQuery _levelStartEventsQuery;
        private EntityQuery _levelCompletedEventsQuery;
        private EntityQuery _levelFailedEventsQuery;
        
        private EntityQuery _fragmentCollectedEventsQuery;
        private EntityQuery _fragmentResonanceEventsQuery;
        
        private EntityQuery _checkpointEventsQuery;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Inizializza il sistema e configura le query per i vari tipi di eventi
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query per eventi obiettivi - separati in tre query distinte
            _objectiveCompletedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<ObjectiveCompletedEvent>());
            _objectiveFailedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<ObjectiveFailedEvent>());
            _objectiveUpdatedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<ObjectiveUpdatedEvent>());
                
            // Query per eventi livello - separati in tre query distinte
            _levelStartEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<LevelStartEvent>());
            _levelCompletedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<LevelCompletedEvent>());
            _levelFailedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<LevelFailedEvent>());
                
            // Query per eventi frammenti - separati in due query distinte
            _fragmentCollectedEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<FragmentCollectedEvent>());
            _fragmentResonanceEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<FragmentResonanceEvent>());
                
            // Query per eventi checkpoint
            _checkpointEventsQuery = state.GetEntityQuery(ComponentType.ReadOnly<CheckpointActivatedEvent>());
                
            // Richiedi il singleton del buffer di comandi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Pulizia risorse
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        #endregion
        
        #region System Lifecycle

        /// <summary>
        /// Gestisce tutti gli eventi di gameplay nel frame corrente
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il buffer di comandi per modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Processa eventi obiettivi completati
            if (!_objectiveCompletedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessObjectiveCompletedEventsJob
                {
                    ECB = ecb.AsParallelWriter(),
                    MissionLookup = SystemAPI.GetComponentLookup<MissionComponent>(true)
                }.ScheduleParallel(_objectiveCompletedEventsQuery, state.Dependency);
            }
            
            // Processa eventi obiettivi falliti
            if (!_objectiveFailedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessObjectiveFailedEventsJob
                {
                    ECB = ecb.AsParallelWriter(),
                    MissionLookup = SystemAPI.GetComponentLookup<MissionComponent>(true)
                }.ScheduleParallel(_objectiveFailedEventsQuery, state.Dependency);
            }
            
            // Processa eventi obiettivi aggiornati
            if (!_objectiveUpdatedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessObjectiveUpdatedEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_objectiveUpdatedEventsQuery, state.Dependency);
            }
            
            // Processa eventi livello - inizio
            if (!_levelStartEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessLevelStartEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_levelStartEventsQuery, state.Dependency);
            }
            
            // Processa eventi livello - completato
            if (!_levelCompletedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessLevelCompletedEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_levelCompletedEventsQuery, state.Dependency);
            }
            
            // Processa eventi livello - fallito
            if (!_levelFailedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessLevelFailedEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_levelFailedEventsQuery, state.Dependency);
            }
            
            // Processa eventi frammenti - raccolti
            if (!_fragmentCollectedEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessFragmentCollectedEventsJob
                {
                    ECB = ecb.AsParallelWriter(),
                    FragmentInventoryLookup = SystemAPI.GetComponentLookup<FragmentInventoryComponent>(true)
                }.ScheduleParallel(_fragmentCollectedEventsQuery, state.Dependency);
            }
            
            // Processa eventi frammenti - risonanza
            if (!_fragmentResonanceEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessFragmentResonanceEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_fragmentResonanceEventsQuery, state.Dependency);
            }
            
            // Processa eventi checkpoint
            if (!_checkpointEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessCheckpointEventsJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_checkpointEventsQuery, state.Dependency);
            }
        }
        
        #endregion
        
        #region Objective Event Jobs
        
        /// <summary>
        /// Job che elabora gli eventi di completamento obiettivi
        /// </summary>
        [BurstCompile]
        private partial struct ProcessObjectiveCompletedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<MissionComponent> MissionLookup;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in ObjectiveCompletedEvent evt)
            {
                // Crea eventi UI per mostrare il completamento dell'obiettivo
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new ObjectiveUIUpdateEvent
                {
                    ObjectiveID = evt.ObjectiveID,
                    IsCompleted = true,
                    Progress = 1.0f
                });
                
                // Aggiorna il progresso della missione
                if (MissionLookup.HasComponent(evt.MissionEntity))
                {
                    var mission = MissionLookup[evt.MissionEntity];
                    mission.CompletedObjectives++;
                    
                    // Verifica se la missione è completata
                    if (mission.CompletedObjectives >= mission.TotalObjectives)
                    {
                        // Crea evento completamento missione
                        var missionCompleteEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, missionCompleteEvent, new MissionCompletedEvent
                        {
                            MissionEntity = evt.MissionEntity,
                            MissionID = mission.MissionID
                        });
                    }
                    
                    // Aggiorna il componente missione
                    ECB.SetComponent(sortKey, evt.MissionEntity, mission);
                }
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di fallimento obiettivi
        /// </summary>
        [BurstCompile]
        private partial struct ProcessObjectiveFailedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<MissionComponent> MissionLookup;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in ObjectiveFailedEvent evt)
            {
                // Crea eventi UI per mostrare il fallimento dell'obiettivo
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new ObjectiveUIUpdateEvent
                {
                    ObjectiveID = evt.ObjectiveID,
                    IsFailed = true,
                    Progress = evt.Progress
                });
                
                // Aggiorna lo stato della missione se necessario
                if (MissionLookup.HasComponent(evt.MissionEntity) && evt.IsCritical)
                {
                    // Se l'obiettivo è critico, la missione fallisce
                    var missionFailEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, missionFailEvent, new MissionFailedEvent
                    {
                        MissionEntity = evt.MissionEntity,
                        MissionID = MissionLookup[evt.MissionEntity].MissionID,
                        FailReason = evt.FailReason
                    });
                }
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di aggiornamento obiettivi
        /// </summary>
        [BurstCompile]
        private partial struct ProcessObjectiveUpdatedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in ObjectiveUpdatedEvent evt)
            {
                // Crea eventi UI per aggiornare la visualizzazione dell'obiettivo
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new ObjectiveUIUpdateEvent
                {
                    ObjectiveID = evt.ObjectiveID,
                    Progress = evt.Progress,
                    IsCompleted = false,
                    IsFailed = false
                });
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        #endregion
        
        #region Level Event Jobs
        
        /// <summary>
        /// Job che elabora gli eventi di inizio livello
        /// </summary>
        [BurstCompile]
        private partial struct ProcessLevelStartEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LevelStartEvent evt)
            {
                // Crea eventi UI per l'inizio del livello
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new LevelUIUpdateEvent
                {
                    LevelID = evt.LevelID,
                    EventType = 0 // 0 = Start
                });
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di completamento livello
        /// </summary>
        [BurstCompile]
        private partial struct ProcessLevelCompletedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LevelCompletedEvent evt)
            {
                // Crea eventi UI per il completamento del livello
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new LevelUIUpdateEvent
                {
                    LevelID = evt.LevelID,
                    EventType = 1, // 1 = Complete
                    Score = evt.FinalScore,
                    TimeElapsed = evt.TimeElapsed,
                    FragmentsCollected = evt.FragmentsCollected
                });
                
                // Aggiorna i progressi del giocatore
                // (implementazione specifica dipende dalla struttura del progetto)
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di fallimento livello
        /// </summary>
        [BurstCompile]
        private partial struct ProcessLevelFailedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in LevelFailedEvent evt)
            {
                // Crea eventi UI per il fallimento del livello
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new LevelUIUpdateEvent
                {
                    LevelID = evt.LevelID,
                    EventType = 2, // 2 = Failed
                    FailReason = evt.FailReason
                });
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        #endregion
        
        #region Fragment Event Jobs
        
        /// <summary>
        /// Job che elabora gli eventi di raccolta frammenti
        /// </summary>
        [BurstCompile]
        private partial struct ProcessFragmentCollectedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<FragmentInventoryComponent> FragmentInventoryLookup;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in FragmentCollectedEvent evt)
            {
                // Crea eventi UI per la raccolta del frammento
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new FragmentUIUpdateEvent
                {
                    FragmentID = evt.FragmentID,
                    FragmentType = evt.FragmentType,
                    CollectorEntity = evt.CollectorEntity
                });
                
                // Aggiorna il componente di inventario frammenti se presente
                if (FragmentInventoryLookup.HasComponent(evt.CollectorEntity))
                {
                    var inventory = FragmentInventoryLookup[evt.CollectorEntity];
                    
                    // Aggiorna l'inventario (implementazione specifica)
                    // ...
                    
                    // Riapplica il componente aggiornato
                    ECB.SetComponent(sortKey, evt.CollectorEntity, inventory);
                }
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di risonanza frammenti
        /// </summary>
        [BurstCompile]
        private partial struct ProcessFragmentResonanceEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in FragmentResonanceEvent evt)
            {
                // Crea eventi UI per la risonanza del frammento
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new FragmentResonanceUIEvent
                {
                    ActivatorEntity = evt.ActivatorEntity,
                    ResonatingFragmentType = evt.FragmentType,
                    CharacterType = evt.CharacterType
                });
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        #endregion
        
        #region Checkpoint Event Jobs
        
        /// <summary>
        /// Job che elabora gli eventi relativi ai checkpoint
        /// </summary>
        [BurstCompile]
        private partial struct ProcessCheckpointEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, Entity entity, in CheckpointActivatedEvent evt)
            {
                // Crea eventi UI per l'attivazione del checkpoint
                var uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new CheckpointUIEvent
                {
                    CheckpointID = evt.CheckpointID,
                    CheckpointName = evt.CheckpointName
                });
                
                // Aggiorna i dati di salvataggio (checkpoint attivo)
                // (implementazione specifica dipende dalla struttura del progetto)
                
                // Distruggi l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        #endregion
    }
    
    #region Eventi UI
    
    
    /// <summary>
    /// Evento UI per aggiornamenti livello
    /// </summary>
    public struct LevelUIUpdateEvent : IComponentData
    {
        public int LevelID;          // ID del livello
        public byte EventType;       // 0=Start, 1=Complete, 2=Failed
        public float Score;          // Punteggio finale
        public float TimeElapsed;    // Tempo impiegato
        public byte FragmentsCollected; // Frammenti raccolti
        public byte FailReason;      // Motivo del fallimento
    }
    
    /// <summary>
    /// Evento UI per risonanza frammenti
    /// </summary>
    public struct FragmentResonanceUIEvent : IComponentData
    {
        public Entity ActivatorEntity;      // Entità che ha attivato la risonanza
        public byte ResonatingFragmentType; // Tipo di frammento in risonanza
        public byte CharacterType;          // Tipo di personaggio
    }
    
    /// <summary>
    /// Evento UI per attivazione checkpoint
    /// </summary>
    public struct CheckpointUIEvent : IComponentData
    {
        public int CheckpointID;      // ID del checkpoint
        public FixedString64Bytes CheckpointName; // Nome del checkpoint
    }
    
    #endregion
    
    #region Eventi Gameplay
    
    /// <summary>
    /// Evento per obiettivo completato
    /// </summary>
    public struct ObjectiveCompletedEvent : IComponentData
    {
        public int ObjectiveID;     // ID dell'obiettivo
        public Entity MissionEntity; // Entità missione associata
    }
    
    /// <summary>
    /// Evento per obiettivo fallito
    /// </summary>
    public struct ObjectiveFailedEvent : IComponentData
    {
        public int ObjectiveID;     // ID dell'obiettivo
        public Entity MissionEntity; // Entità missione associata
        public byte FailReason;     // Motivo del fallimento
        public bool IsCritical;     // Se il fallimento è critico per la missione
        public float Progress;      // Progresso raggiunto prima del fallimento
    }
    
    /// <summary>
    /// Evento per obiettivo aggiornato
    /// </summary>
    public struct ObjectiveUpdatedEvent : IComponentData
    {
        public int ObjectiveID;     // ID dell'obiettivo
        public float Progress;      // Nuovo progresso (0-1)
    }
    
    /// <summary>
    /// Evento per missione completata
    /// </summary>
    public struct MissionCompletedEvent : IComponentData
    {
        public Entity MissionEntity; // Entità missione
        public int MissionID;       // ID della missione
    }
    
    /// <summary>
    /// Evento per missione fallita
    /// </summary>
    public struct MissionFailedEvent : IComponentData
    {
        public Entity MissionEntity; // Entità missione
        public int MissionID;       // ID della missione
        public byte FailReason;     // Motivo del fallimento
    }
    
    /// <summary>
    /// Evento per inizio livello
    /// </summary>
    public struct LevelStartEvent : IComponentData
    {
        public int LevelID;         // ID del livello
    }
    
    /// <summary>
    /// Evento per completamento livello
    /// </summary>
    public struct LevelCompletedEvent : IComponentData
    {
        public int LevelID;              // ID del livello
        public float FinalScore;         // Punteggio finale
        public float TimeElapsed;        // Tempo impiegato
        public byte FragmentsCollected;  // Frammenti raccolti
    }
    
    /// <summary>
    /// Evento per fallimento livello
    /// </summary>
    public struct LevelFailedEvent : IComponentData
    {
        public int LevelID;          // ID del livello
        public byte FailReason;      // Motivo del fallimento
    }
    
    /// <summary>
    /// Evento per frammento raccolto
    /// </summary>
    public struct FragmentCollectedEvent : IComponentData
    {
        public int FragmentID;        // ID del frammento
        public byte FragmentType;     // Tipo di frammento
        public Entity CollectorEntity; // Entità che ha raccolto il frammento
    }
    
    /// <summary>
    /// Evento per risonanza frammenti
    /// </summary>
    public struct FragmentResonanceEvent : IComponentData
    {
        public Entity ActivatorEntity;  // Entità che ha attivato la risonanza
        public byte FragmentType;       // Tipo di frammento
        public byte CharacterType;      // Tipo di personaggio
    }
    
    /// <summary>
    /// Evento per checkpoint attivato
    /// </summary>
    public struct CheckpointActivatedEvent : IComponentData
    {
        public int CheckpointID;                // ID del checkpoint
        public FixedString64Bytes CheckpointName; // Nome del checkpoint
        public Entity ActivatorEntity;          // Entità che ha attivato il checkpoint
    }
    
    #endregion
}