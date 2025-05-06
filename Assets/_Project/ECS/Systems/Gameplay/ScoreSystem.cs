using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce il conteggio del punteggio, i moltiplicatori di punteggio,
    /// le combo e il tracciamento dei punteggi più alti.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ScoreSystem : ISystem
    {
        // Query per le entità con componente punteggio
        private EntityQuery _scoreEntitiesQuery;
        
        // Query per gli eventi di aggiornamento punteggio
        private EntityQuery _scoreUpdateEventsQuery;
        
        // Query per gli eventi di completamento livello
        private EntityQuery _levelCompletionEventsQuery;
        
        // Stato del sistema
        private NativeParallelHashMap<Entity, ComboState> _playerCombos;
        
        /// <summary>
        /// Inizializza il sistema e le sue strutture dati
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità con punteggio
            _scoreEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ScoreComponent>()
                .Build(ref state);
                
            // Configura la query per gli eventi di aggiornamento punteggio
            _scoreUpdateEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ScoreUpdatedEvent>()
                .Build(ref state);
                
            // Configura la query per gli eventi di completamento livello
            _levelCompletionEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LevelCompletedEvent>()
                .Build(ref state);
                
            // Inizializza la mappa di stato delle combo
            _playerCombos = new NativeParallelHashMap<Entity, ComboState>(16, Allocator.Persistent);
            
            // Richiedi un command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Pulisce le risorse allocate
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_playerCombos.IsCreated)
            {
                _playerCombos.Dispose();
            }
        }

        /// <summary>
        /// Aggiorna il punteggio e le statistiche correlate
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer di comandi per eventuali modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // 1. Aggiorna i timer delle combo per tutti i giocatori
            var comboKeys = _playerCombos.GetKeyArray(Allocator.Temp);
            for (int i = 0; i < comboKeys.Length; i++)
            {
                Entity playerEntity = comboKeys[i];
                
                if (SystemAPI.Exists(playerEntity))
                {
                    ComboState comboState = _playerCombos[playerEntity];
                    
                    // Aggiorna il timer della combo
                    comboState.ComboTimeRemaining -= deltaTime;
                    
                    // Se la combo è scaduta, resettala
                    if (comboState.ComboTimeRemaining <= 0 && comboState.CurrentCombo > 0)
                    {
                        comboState.CurrentCombo = 0;
                        comboState.ComboMultiplier = 1.0f;
                        
                        // Crea un evento di fine combo
                        Entity comboEndEvent = ecb.CreateEntity();
                        ecb.AddComponent(comboEndEvent, new ComboEndedEvent
                        {
                            PlayerEntity = playerEntity,
                            FinalComboCount = comboState.CurrentCombo,
                            FinalMultiplier = comboState.ComboMultiplier,
                            TotalScoreFromCombo = comboState.ScoreFromCurrentCombo
                        });
                        
                        // Resetta anche il conteggio del punteggio della combo
                        comboState.ScoreFromCurrentCombo = 0;
                    }
                    
                    // Salva lo stato aggiornato della combo
                    _playerCombos[playerEntity] = comboState;
                }
            }
            comboKeys.Dispose();
            
            // 2. Elabora gli eventi di aggiornamento punteggio
            if (!_scoreUpdateEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessScoreUpdatesJob
                {
                    PlayerCombos = _playerCombos.AsParallelWriter(),
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_scoreUpdateEventsQuery, state.Dependency);
            }
            
            // 3. Elabora gli eventi di completamento livello (per punteggi finali)
            if (!_levelCompletionEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessLevelCompletionJob
                {
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_levelCompletionEventsQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che elabora gli aggiornamenti di punteggio
        /// </summary>
        [BurstCompile]
        private partial struct ProcessScoreUpdatesJob : IJobEntity
        {
            public NativeParallelHashMap<Entity, ComboState>.ParallelWriter PlayerCombos;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in ScoreUpdatedEvent scoreEvent)
            {
                // Controlla se l'entità giocatore esiste ancora
                if (!SystemAPI.Exists(scoreEvent.PlayerEntity))
                {
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }
                
                // Aggiorna la combo del giocatore se il punteggio proviene da azioni di combattimento
                if (scoreEvent.ScoreSource <= 3) // 0-3 sono tipi di punteggio da combattimento
                {
                    // Ottieni o inizializza lo stato della combo
                    ComboState comboState;
                    if (!PlayerCombos.TryGetValue(scoreEvent.PlayerEntity, out comboState))
                    {
                        comboState = new ComboState
                        {
                            CurrentCombo = 0,
                            ComboMultiplier = 1.0f,
                            ComboTimeRemaining = 0,
                            ScoreFromCurrentCombo = 0
                        };
                    }
                    
                    // Incrementa la combo
                    comboState.CurrentCombo++;
                    
                    // Aggiorna il moltiplicatore di combo (esempio: 1.0 + 0.1 * combo fino a un massimo di 3.0)
                    comboState.ComboMultiplier = math.min(1.0f + 0.1f * comboState.CurrentCombo, 3.0f);
                    
                    // Resetta il timer della combo
                    comboState.ComboTimeRemaining = 5.0f; // 5 secondi di tempo per continuare la combo
                    
                    // Somma il punteggio guadagnato con questa combo
                    comboState.ScoreFromCurrentCombo += scoreEvent.ScoreIncrement * comboState.ComboMultiplier;
                    
                    // Aggiorna lo stato della combo per questo giocatore
                    // Utilizziamo TryAdd con ParallelWriter per aggiornare in modo thread-safe
                    // Nota: In un contesto parallelo, potrebbe sovrascrivere altri aggiornamenti
                    // ma è accettabile in questo scenario di gioco
                    PlayerCombos.TryAdd(scoreEvent.PlayerEntity, comboState);
                    
                    // Milestone combo: ogni 10 hit crea un evento speciale
                    if (comboState.CurrentCombo % 10 == 0)
                    {
                        Entity comboMilestoneEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, comboMilestoneEvent, new ComboMilestoneEvent
                        {
                            PlayerEntity = scoreEvent.PlayerEntity,
                            ComboCount = comboState.CurrentCombo,
                            ComboMultiplier = comboState.ComboMultiplier
                        });
                    }
                }
                
                // Crea evento di feedback UI per il punteggio
                Entity uiEvent = ECB.CreateEntity(sortKey);
                ECB.AddComponent(sortKey, uiEvent, new ScoreUIUpdateEvent
                {
                    PlayerEntity = scoreEvent.PlayerEntity,
                    NewScore = scoreEvent.NewScore,
                    ScoreIncrement = scoreEvent.ScoreIncrement,
                    ScoreSource = scoreEvent.ScoreSource
                });
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che elabora gli eventi di completamento livello
        /// </summary>
        [BurstCompile]
        private partial struct ProcessLevelCompletionJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in LevelCompletedEvent completionEvent)
            {
                // Controlla tutti i giocatori che hanno completato il livello
                if (SystemAPI.HasComponent<ScoreComponent>(entity))
                {
                    var scoreComponent = SystemAPI.GetComponent<ScoreComponent>(entity);
                    
                    // Verifica se è un nuovo record
                    bool isHighScore = scoreComponent.CurrentScore > scoreComponent.HighScore;
                    
                    // Se è un nuovo record, aggiorna il punteggio più alto
                    if (isHighScore)
                    {
                        scoreComponent.HighScore = scoreComponent.CurrentScore;
                        scoreComponent.HighScoreDate = SystemAPI.Time.ElapsedTime;
                        SystemAPI.SetComponent(entity, scoreComponent);
                        
                        // Crea un evento di nuovo record
                        Entity highScoreEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, highScoreEvent, new HighScoreAchievedEvent
                        {
                            PlayerEntity = entity,
                            LevelID = completionEvent.LevelID,
                            NewHighScore = scoreComponent.HighScore,
                            PreviousHighScore = scoreComponent.PreviousHighScore
                        });
                    }
                    
                    // Salva il punteggio attuale come punteggio precedente per il prossimo confronto
                    scoreComponent.PreviousHighScore = scoreComponent.CurrentScore;
                    SystemAPI.SetComponent(entity, scoreComponent);
                    
                    // Crea un evento per l'UI del punteggio finale
                    Entity finalScoreEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, finalScoreEvent, new FinalScoreUIEvent
                    {
                        PlayerEntity = entity,
                        LevelID = completionEvent.LevelID,
                        FinalScore = scoreComponent.CurrentScore,
                        IsHighScore = isHighScore,
                        TotalCollectibles = scoreComponent.TotalCollectibles,
                        TotalEnemiesDefeated = scoreComponent.TotalEnemiesDefeated,
                        TimeBonus = scoreComponent.TimeBonus
                    });
                }
                
                // Non distruggere l'evento originale perché altri sistemi potrebbero utilizzarlo
            }
        }
    }
    
    #region Componenti e Eventi ScoreSystem
    
    /// <summary>
    /// Componente per il punteggio di un'entità
    /// </summary>
    [System.Serializable]
    public struct ScoreComponent : IComponentData
    {
        // Punteggio corrente
        public float CurrentScore;           // Punteggio attuale della sessione
        public float HighScore;              // Punteggio più alto di sempre
        public float PreviousHighScore;      // Punteggio precedente
        public double HighScoreDate;         // Data (tempo di gioco) del punteggio più alto
        
        // Statistiche di punteggio
        public int TotalCollectibles;        // Numero totale di collezionabili raccolti
        public int TotalEnemiesDefeated;     // Numero totale di nemici sconfitti
        public float TimeBonus;              // Bonus punteggio per velocità di completamento
        
        // Modificatori
        public float ScoreMultiplier;        // Moltiplicatore globale di punteggio
    }
    
    /// <summary>
    /// Stato di una combo mantenuto nel sistema
    /// </summary>
    [System.Serializable]
    public struct ComboState
    {
        public int CurrentCombo;             // Conteggio attuale della combo
        public float ComboMultiplier;        // Moltiplicatore di punteggio attuale
        public float ComboTimeRemaining;     // Tempo rimanente prima che la combo scada
        public float ScoreFromCurrentCombo;  // Punteggio guadagnato da questa combo
    }
    
    /// <summary>
    /// Evento per aggiornamenti UI del punteggio
    /// </summary>
    [System.Serializable]
    public struct ScoreUIUpdateEvent : IComponentData
    {
        public Entity PlayerEntity;          // Entità del giocatore
        public float NewScore;               // Nuovo punteggio totale
        public float ScoreIncrement;         // Incremento punteggio
        public byte ScoreSource;             // Fonte del punteggio (0=nemico, 1=moneta, ecc.)
    }
    
    /// <summary>
    /// Evento quando una combo raggiunge una milestone
    /// </summary>
    [System.Serializable]
    public struct ComboMilestoneEvent : IComponentData
    {
        public Entity PlayerEntity;          // Entità del giocatore
        public int ComboCount;               // Conteggio della combo
        public float ComboMultiplier;        // Moltiplicatore attuale
    }
    
    /// <summary>
    /// Evento quando una combo termina
    /// </summary>
    [System.Serializable]
    public struct ComboEndedEvent : IComponentData
    {
        public Entity PlayerEntity;          // Entità del giocatore
        public int FinalComboCount;          // Conteggio finale della combo
        public float FinalMultiplier;        // Moltiplicatore finale
        public float TotalScoreFromCombo;    // Punteggio totale dalla combo
    }
    
    /// <summary>
    /// Evento per il punteggio finale del livello
    /// </summary>
    [System.Serializable]
    public struct FinalScoreUIEvent : IComponentData
    {
        public Entity PlayerEntity;          // Entità del giocatore
        public int LevelID;                  // ID del livello
        public float FinalScore;             // Punteggio finale
        public bool IsHighScore;             // Se è un nuovo record
        public int TotalCollectibles;        // Collezionabili raccolti
        public int TotalEnemiesDefeated;     // Nemici sconfitti
        public float TimeBonus;              // Bonus tempo
    }
    
    /// <summary>
    /// Evento quando si ottiene un nuovo punteggio più alto
    /// </summary>
    [System.Serializable]
    public struct HighScoreAchievedEvent : IComponentData
    {
        public Entity PlayerEntity;          // Entità del giocatore
        public int LevelID;                  // ID del livello
        public float NewHighScore;           // Nuovo punteggio record
        public float PreviousHighScore;      // Punteggio record precedente
    }
    
    #endregion
}
