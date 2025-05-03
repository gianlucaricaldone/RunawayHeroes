using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.AI
{
    /// <summary>
    /// Sistema che gestisce le fasi dei boss e mid-boss.
    /// Coordina i cambiamenti di fase basati sulla salute,
    /// gestisce gli stati di rabbia/enrage, e attiva comportamenti
    /// specifici per ogni fase del combattimento.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAISystem))]
    [BurstCompile]
    public partial struct BossPhasesSystem : ISystem
    {
        // Query per i boss
        private EntityQuery _bossQuery;
        
        // Query per i mid-boss
        private EntityQuery _midBossQuery;
        
        // Query per i giocatori
        private EntityQuery _playerQuery;
        
        /// <summary>
        /// Inizializza il sistema di fasi dei boss
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura query per i boss
            _bossQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BossComponent, HealthComponent>()
                .Build(ref state);
                
            // Configura query per i mid-boss
            _midBossQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MidBossComponent, HealthComponent>()
                .Build(ref state);
                
            // Configura query per i giocatori
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano boss o mid-boss per l'aggiornamento
            var bossOrMidBossQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<BossComponent, MidBossComponent>()
                .Build(ref state);
                
            state.RequireForUpdate(bossOrMidBossQuery);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna le fasi e stati dei boss
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // 1. Gestisci i boss principali
            if (!_bossQuery.IsEmpty)
            {
                // Raccogli posizioni dei giocatori per l'attivazione
                var playerTransforms = new NativeArray<float3>(0, Allocator.TempJob);
                
                if (!_playerQuery.IsEmpty)
                {
                    var playerTransformComponents = _playerQuery.ToComponentDataArray<TransformComponent>(Allocator.Temp);
                    playerTransforms = new NativeArray<float3>(playerTransformComponents.Length, Allocator.TempJob);
                    
                    for (int i = 0; i < playerTransformComponents.Length; i++)
                    {
                        playerTransforms[i] = playerTransformComponents[i].Position;
                    }
                    
                    playerTransformComponents.Dispose();
                }
                
                // Gestisci le fasi dei boss
                state.Dependency = new BossPhaseManagementJob
                {
                    DeltaTime = deltaTime,
                    PlayerPositions = playerTransforms,
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_bossQuery, state.Dependency);
                
                // Cleanup
                state.Dependency = playerTransforms.Dispose(state.Dependency);
            }
            
            // 2. Gestisci i mid-boss
            if (!_midBossQuery.IsEmpty)
            {
                // Raccogli posizioni dei giocatori per l'attivazione
                var playerTransforms = new NativeArray<float3>(0, Allocator.TempJob);
                
                if (!_playerQuery.IsEmpty)
                {
                    var playerTransformComponents = _playerQuery.ToComponentDataArray<TransformComponent>(Allocator.Temp);
                    playerTransforms = new NativeArray<float3>(playerTransformComponents.Length, Allocator.TempJob);
                    
                    for (int i = 0; i < playerTransformComponents.Length; i++)
                    {
                        playerTransforms[i] = playerTransformComponents[i].Position;
                    }
                    
                    playerTransformComponents.Dispose();
                }
                
                // Gestisci gli stati dei mid-boss
                state.Dependency = new MidBossStateManagementJob
                {
                    DeltaTime = deltaTime,
                    PlayerPositions = playerTransforms,
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_midBossQuery, state.Dependency);
                
                // Cleanup
                state.Dependency = playerTransforms.Dispose(state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che gestisce i cambiamenti di fase dei boss
        /// </summary>
        [BurstCompile]
        private partial struct BossPhaseManagementJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeArray<float3> PlayerPositions;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(Entity entity, 
                        [EntityIndexInQuery] int sortKey, 
                        ref BossComponent boss,
                        ref HealthComponent health,
                        in TransformComponent transform)
            {
                // 1. Attivazione del boss
                if (!boss.IsActivated)
                {
                    bool shouldActivate = false;
                    
                    // Controlla se un giocatore è abbastanza vicino per attivare il boss
                    for (int i = 0; i < PlayerPositions.Length; i++)
                    {
                        float distance = math.distance(transform.Position, PlayerPositions[i]);
                        if (distance < 15.0f) // Distanza di attivazione
                        {
                            shouldActivate = true;
                            break;
                        }
                    }
                    
                    if (shouldActivate)
                    {
                        // Attiva il boss
                        boss.IsActivated = true;
                        
                        // Crea un evento di attivazione
                        Entity activationEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, activationEvent, new BossActivationEvent
                        {
                            BossEntity = entity,
                            BossType = (byte)boss.Type,
                            Position = transform.Position
                        });
                    }
                    else
                    {
                        // Non attivato, salta il resto dell'elaborazione
                        return;
                    }
                }
                
                // 2. Gestione timer di transizione di fase
                if (boss.PhaseTransitionTimer > 0)
                {
                    boss.PhaseTransitionTimer -= DeltaTime;
                    
                    // Mantieni invulnerabilità durante la transizione
                    boss.IsInvulnerable = true;
                    
                    // Se la transizione è completata
                    if (boss.PhaseTransitionTimer <= 0)
                    {
                        boss.IsInvulnerable = false;
                        
                        // Crea un evento di fine transizione
                        Entity phaseEndEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, phaseEndEvent, new BossPhaseTransitionEndEvent
                        {
                            BossEntity = entity,
                            PhaseIndex = boss.CurrentPhase
                        });
                    }
                    
                    // Durante la transizione, non eseguire altre logiche di fase
                    return;
                }
                
                // 3. Gestione timer di invulnerabilità
                if (boss.InvulnerabilityTimer > 0)
                {
                    boss.InvulnerabilityTimer -= DeltaTime;
                    if (boss.InvulnerabilityTimer <= 0)
                    {
                        boss.IsInvulnerable = false;
                    }
                }
                
                // 4. Controllo di transizione di fase basato sulla salute
                if (boss.ShouldTransitionToNextPhase(health.CurrentHealth, health.MaxHealth))
                {
                    // Incrementa la fase
                    boss.CurrentPhase++;
                    
                    // Imposta timer di transizione
                    boss.PhaseTransitionTimer = 3.0f; // 3 secondi di transizione
                    boss.IsInvulnerable = true;
                    
                    // Azzera l'intensità della fase
                    boss.CurrentPhaseIntensity = 0.0f;
                    
                    // Crea un evento di inizio transizione
                    Entity phaseStartEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, phaseStartEvent, new BossPhaseTransitionStartEvent
                    {
                        BossEntity = entity,
                        PhaseIndex = boss.CurrentPhase
                    });
                    
                    // Se è l'ultima fase, attiva lo stato infuriato
                    if (boss.CurrentPhase == boss.TotalPhases - 1)
                    {
                        boss.IsEnraged = true;
                    }
                }
                
                // 5. Gestione dell'intensità della fase corrente
                boss.CurrentPhaseIntensity += DeltaTime * boss.PhaseIntensityRate;
                boss.CurrentPhaseIntensity = math.min(boss.CurrentPhaseIntensity, 1.0f);
                
                // 6. Gestione cooldown degli attacchi speciali
                if (boss.SpecialAttackCooldown > 0)
                {
                    boss.SpecialAttackCooldown -= DeltaTime;
                    if (boss.SpecialAttackCooldown < 0)
                    {
                        boss.SpecialAttackCooldown = 0;
                        
                        // Genera evento per attacco speciale
                        Entity specialAttackEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, specialAttackEvent, new BossSpecialAttackReadyEvent
                        {
                            BossEntity = entity,
                            BossType = (byte)boss.Type,
                            PhaseIndex = boss.CurrentPhase
                        });
                    }
                }
                
                // 7. Gestione spawn di servitori
                if (boss.HasMinions && boss.MinionSpawnCooldown > 0)
                {
                    boss.MinionSpawnCooldown -= DeltaTime;
                    // Logica per spawn servitori
                }
            }
        }
        
        /// <summary>
        /// Job che gestisce gli stati dei mid-boss
        /// </summary>
        [BurstCompile]
        private partial struct MidBossStateManagementJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeArray<float3> PlayerPositions;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(Entity entity, 
                        [EntityIndexInQuery] int sortKey, 
                        ref MidBossComponent midBoss,
                        ref HealthComponent health,
                        in TransformComponent transform)
            {
                // 1. Attivazione del mid-boss
                if (!midBoss.IsActivated)
                {
                    bool shouldActivate = false;
                    
                    // Controlla se un giocatore è abbastanza vicino per attivare il mid-boss
                    for (int i = 0; i < PlayerPositions.Length; i++)
                    {
                        float distance = math.distance(transform.Position, PlayerPositions[i]);
                        if (distance < 12.0f) // Distanza di attivazione più breve rispetto ai boss
                        {
                            shouldActivate = true;
                            break;
                        }
                    }
                    
                    if (shouldActivate)
                    {
                        // Attiva il mid-boss
                        midBoss.IsActivated = true;
                        
                        // Crea un evento di attivazione
                        Entity activationEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, activationEvent, new MidBossActivationEvent
                        {
                            MidBossEntity = entity,
                            MidBossType = (byte)midBoss.Type,
                            Position = transform.Position
                        });
                    }
                    else
                    {
                        // Non attivato, salta il resto dell'elaborazione
                        return;
                    }
                }
                
                // 2. Gestione dello stato infuriato
                if (midBoss.HasEnragedState && !midBoss.IsEnraged)
                {
                    float healthPercentage = health.CurrentHealth / health.MaxHealth;
                    
                    if (healthPercentage <= midBoss.EnrageThreshold)
                    {
                        // Attiva lo stato infuriato
                        midBoss.IsEnraged = true;
                        
                        // Crea un evento di enrage
                        Entity enrageEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, enrageEvent, new MidBossEnrageEvent
                        {
                            MidBossEntity = entity,
                            MidBossType = (byte)midBoss.Type
                        });
                    }
                }
                
                // 3. Gestione cooldown abilità speciale
                if (midBoss.CurrentSpecialCooldown > 0)
                {
                    midBoss.CurrentSpecialCooldown -= DeltaTime;
                    
                    if (midBoss.CurrentSpecialCooldown <= 0)
                    {
                        // Abilità pronta
                        midBoss.CurrentSpecialCooldown = 0;
                        
                        // Genera evento per abilità speciale
                        Entity specialAbilityEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, specialAbilityEvent, new MidBossSpecialAbilityReadyEvent
                        {
                            MidBossEntity = entity,
                            MidBossType = (byte)midBoss.Type,
                            AbilityType = (byte)midBoss.SpecialAbility
                        });
                    }
                }
            }
        }
    }
    
    // Eventi del sistema
    
    /// <summary>
    /// Evento generato quando un boss viene attivato
    /// </summary>
    public struct BossActivationEvent : IComponentData
    {
        public Entity BossEntity;    // Entità del boss
        public byte BossType;        // Tipo di boss
        public float3 Position;      // Posizione di attivazione
    }
    
    /// <summary>
    /// Evento generato quando un boss inizia la transizione di fase
    /// </summary>
    public struct BossPhaseTransitionStartEvent : IComponentData
    {
        public Entity BossEntity;    // Entità del boss
        public int PhaseIndex;       // Indice della nuova fase
    }
    
    /// <summary>
    /// Evento generato quando un boss completa la transizione di fase
    /// </summary>
    public struct BossPhaseTransitionEndEvent : IComponentData
    {
        public Entity BossEntity;    // Entità del boss
        public int PhaseIndex;       // Indice della fase completata
    }
    
    /// <summary>
    /// Evento generato quando un attacco speciale del boss è pronto
    /// </summary>
    public struct BossSpecialAttackReadyEvent : IComponentData
    {
        public Entity BossEntity;    // Entità del boss
        public byte BossType;        // Tipo di boss
        public int PhaseIndex;       // Fase corrente
    }
    
    /// <summary>
    /// Evento generato quando un mid-boss viene attivato
    /// </summary>
    public struct MidBossActivationEvent : IComponentData
    {
        public Entity MidBossEntity; // Entità del mid-boss
        public byte MidBossType;     // Tipo di mid-boss
        public float3 Position;      // Posizione di attivazione
    }
    
    /// <summary>
    /// Evento generato quando un mid-boss entra nello stato infuriato
    /// </summary>
    public struct MidBossEnrageEvent : IComponentData
    {
        public Entity MidBossEntity; // Entità del mid-boss
        public byte MidBossType;     // Tipo di mid-boss
    }
    
    /// <summary>
    /// Evento generato quando un'abilità speciale del mid-boss è pronta
    /// </summary>
    public struct MidBossSpecialAbilityReadyEvent : IComponentData
    {
        public Entity MidBossEntity; // Entità del mid-boss
        public byte MidBossType;     // Tipo di mid-boss
        public byte AbilityType;     // Tipo di abilità
    }
}
