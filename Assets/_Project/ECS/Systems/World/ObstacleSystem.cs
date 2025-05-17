using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Core;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.World.Obstacles;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce il comportamento e lo stato degli ostacoli nel gioco.
    /// Si occupa di aggiornare, attivare e disattivare ostacoli in base a varie condizioni.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MovementSystemGroup))]
    [BurstCompile]
    public partial struct ObstacleSystem : ISystem
    {
        // Query per gli ostacoli attivi
        private EntityQuery _activeObstaclesQuery;
        
        // Query per vari tipi di ostacoli
        private EntityQuery _movingObstaclesQuery;
        private EntityQuery _temporaryObstaclesQuery;
        private EntityQuery _sequencedObstaclesQuery;
        private EntityQuery _damagedObstaclesQuery;
        
        // Timer per comportamenti specifici
        private float _obstacleUpdateTimer;
        
        /// <summary>
        /// Inizializza il sistema di gestione ostacoli
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query principale per gli ostacoli attivi
            _activeObstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura query per ostacoli in movimento
            _movingObstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TransformComponent, PhysicsComponent>()
                .WithAny<MovingObstacleTag>()
                .Build(ref state);
                
            // Configura query per ostacoli temporanei
            _temporaryObstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TemporaryObstacleComponent>()
                .Build(ref state);
                
            // Configura query per ostacoli con comportamenti in sequenza
            _sequencedObstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, SequencedBehaviorComponent>()
                .Build(ref state);
                
            // Configura query per ostacoli danneggiati
            _damagedObstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, DamagedStateComponent>()
                .Build(ref state);
                
            // Inizializza il timer
            _obstacleUpdateTimer = 0;
            
            // Richiedi che ci siano ostacoli attivi per l'aggiornamento
            state.RequireForUpdate(_activeObstaclesQuery);
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
        /// Aggiorna lo stato e il comportamento degli ostacoli
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Incrementa il timer di aggiornamento
            _obstacleUpdateTimer += deltaTime;
            
            // 1. Gestisci gli ostacoli in movimento
            if (!_movingObstaclesQuery.IsEmpty)
            {
                state.Dependency = new UpdateMovingObstaclesJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter(),
                    PatrolLookup = SystemAPI.GetComponentLookup<PatrolPathComponent>(true),
                    OscillatingLookup = SystemAPI.GetComponentLookup<OscillatingObstacleComponent>(true),
                    RotatingLookup = SystemAPI.GetComponentLookup<RotatingObstacleComponent>(true)
                }.ScheduleParallel(_movingObstaclesQuery, state.Dependency);
            }
            
            // 2. Gestisci gli ostacoli temporanei (con timer di vita)
            if (!_temporaryObstaclesQuery.IsEmpty)
            {
                state.Dependency = new UpdateTemporaryObstaclesJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter(),
                    RenderLookup = SystemAPI.GetComponentLookup<RenderComponent>(true)
                }.ScheduleParallel(_temporaryObstaclesQuery, state.Dependency);
            }
            
            // 3. Gestisci gli ostacoli con comportamenti in sequenza
            if (!_sequencedObstaclesQuery.IsEmpty)
            {
                state.Dependency = new UpdateSequencedObstaclesJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter(),
                    PhysicsLookup = SystemAPI.GetComponentLookup<PhysicsComponent>(true)
                }.ScheduleParallel(_sequencedObstaclesQuery, state.Dependency);
            }
            
            // 4. Gestisci gli ostacoli danneggiati
            if (!_damagedObstaclesQuery.IsEmpty)
            {
                state.Dependency = new UpdateDamagedObstaclesJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter(),
                    RenderLookup = SystemAPI.GetComponentLookup<RenderComponent>(true)
                }.ScheduleParallel(_damagedObstaclesQuery, state.Dependency);
            }
            
            // 5. Aggiornamenti a intervalli (ogni 0.5 secondi)
            if (_obstacleUpdateTimer >= 0.5f)
            {
                _obstacleUpdateTimer = 0;
                
                // Gestisci l'attivazione di ostacoli in base alla distanza
                state.Dependency = new ActivateObstaclesByDistanceJob
                {
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_activeObstaclesQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che aggiorna gli ostacoli in movimento
        /// </summary>
        [BurstCompile]
        private partial struct UpdateMovingObstaclesJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<PatrolPathComponent> PatrolLookup;
            [ReadOnly] public ComponentLookup<OscillatingObstacleComponent> OscillatingLookup;
            [ReadOnly] public ComponentLookup<RotatingObstacleComponent> RotatingLookup;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                ref PhysicsComponent physics, 
                ref TransformComponent transform, 
                in ObstacleComponent obstacle)
            {
                // Se ha un percorso di pattuglia
                if (PatrolLookup.HasComponent(entity))
                {
                    var patrol = PatrolLookup[entity];
                    
                    // Avanzamento lungo il percorso
                    float3 currentTarget = patrol.CurrentPoint;
                    float3 vectorToTarget = currentTarget - transform.Position;
                    float distanceToTarget = math.length(vectorToTarget);
                    
                    // Se abbiamo raggiunto il punto corrente, passiamo al successivo
                    if (distanceToTarget < 0.2f)
                    {
                        patrol.CurrentPointIndex = (byte)((patrol.CurrentPointIndex + 1) % patrol.TotalPoints);
                        patrol.CurrentPoint = patrol.GetPoint(patrol.CurrentPointIndex);
                        ECB.SetComponent(sortKey, entity, patrol);
                    }
                    
                    // Muovi verso il target corrente
                    if (distanceToTarget > 0.01f)
                    {
                        float3 direction = math.normalize(vectorToTarget);
                        physics.Velocity = direction * patrol.MoveSpeed;
                    }
                }
                // Se ha un comportamento oscillatorio
                else if (OscillatingLookup.HasComponent(entity))
                {
                    var oscillating = OscillatingLookup[entity];
                    
                    // Calcola la nuova posizione con il moto oscillatorio
                    oscillating.CurrentTime += DeltaTime;
                    
                    // Oscillazione lungo X
                    float xOffset = math.sin(oscillating.CurrentTime * oscillating.FrequencyX) * oscillating.AmplitudeX;
                    // Oscillazione lungo Y
                    float yOffset = math.sin(oscillating.CurrentTime * oscillating.FrequencyY) * oscillating.AmplitudeY;
                    // Oscillazione lungo Z
                    float zOffset = math.sin(oscillating.CurrentTime * oscillating.FrequencyZ) * oscillating.AmplitudeZ;
                    
                    // Calcola la velocità in base al cambiamento di posizione
                    float3 newPosition = oscillating.CenterPosition + new float3(xOffset, yOffset, zOffset);
                    physics.Velocity = (newPosition - transform.Position) / DeltaTime;
                    
                    // Aggiorna il componente
                    ECB.SetComponent(sortKey, entity, oscillating);
                }
                // Se è un ostacolo rotante
                else if (RotatingLookup.HasComponent(entity))
                {
                    var rotating = RotatingLookup[entity];
                    
                    // Calcola la nuova rotazione
                    quaternion addRotation = quaternion.AxisAngle(rotating.RotationAxis, rotating.RotationSpeed * DeltaTime);
                    transform.Rotation = math.mul(transform.Rotation, addRotation);
                    
                    // Aggiorna anche la velocità angolare se necessario
                    physics.AngularVelocity = rotating.RotationAxis * rotating.RotationSpeed;
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna gli ostacoli temporanei (con durata limitata)
        /// </summary>
        [BurstCompile]
        private partial struct UpdateTemporaryObstaclesJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<RenderComponent> RenderLookup;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                ref TemporaryObstacleComponent temporary)
            {
                // Decrementa il timer di vita
                temporary.RemainingLifetime -= DeltaTime;
                
                // Se ha raggiunto la fine della vita, programma la distruzione
                if (temporary.RemainingLifetime <= 0)
                {
                    // Crea un evento di distruzione
                    Entity destroyEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, destroyEvent, new ObstacleDestroyedEvent { ObstacleEntity = entity });
                    
                    // Distruggi l'ostacolo
                    ECB.DestroyEntity(sortKey, entity);
                }
                // Se è vicino alla fine della vita e ha una fase di fade out, aggiorna l'opacità
                else if (temporary.RemainingLifetime < temporary.FadeOutTime && temporary.HasFadeOut)
                {
                    float fadeRatio = temporary.RemainingLifetime / temporary.FadeOutTime;
                    
                    // Se ha un componente di rendering, aggiorna l'alpha (trasparenza)
                    if (RenderLookup.HasComponent(entity))
                    {
                        var render = RenderLookup[entity];
                        // Mantiene i colori RGB originali ma aggiorna l'alpha (trasparenza)
                        // fadeRatio va da 1.0 (completamente visibile) a 0.0 (invisibile)
                        render.Color = new float4(render.Color.x, render.Color.y, render.Color.z, fadeRatio);
                        ECB.SetComponent(sortKey, entity, render);
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna gli ostacoli con comportamenti in sequenza
        /// </summary>
        [BurstCompile]
        private partial struct UpdateSequencedObstaclesJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<PhysicsComponent> PhysicsLookup;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                ref SequencedBehaviorComponent sequence)
            {
                // Aggiorna il timer della fase attuale
                sequence.CurrentPhaseTimer += DeltaTime;
                
                // Se la fase corrente è completata, passa alla successiva
                if (sequence.CurrentPhaseTimer >= sequence.GetPhaseDuration(sequence.CurrentPhase))
                {
                    sequence.CurrentPhase = (byte)((sequence.CurrentPhase + 1) % sequence.TotalPhases);
                    sequence.CurrentPhaseTimer = 0;
                    
                    // Crea un evento di cambio fase
                    Entity phaseChangeEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, phaseChangeEvent, new ObstaclePhaseChangedEvent 
                    { 
                        ObstacleEntity = entity,
                        NewPhase = sequence.CurrentPhase,
                        TotalPhases = sequence.TotalPhases
                    });
                }
                
                // Aggiorna eventuali parametri basati sulla fase
                // Ad esempio, la velocità per ostacoli con fasi di movimento
                if (PhysicsLookup.HasComponent(entity))
                {
                    var physics = PhysicsLookup[entity];
                    
                    // Esempio: modifica velocità in base alla fase
                    switch (sequence.GetPhaseType(sequence.CurrentPhase))
                    {
                        case SequencedBehaviorComponent.PhaseType.Idle:
                            physics.Velocity = float3.zero;
                            break;
                        case SequencedBehaviorComponent.PhaseType.Active:
                            // Attiva il movimento nella direzione specificata
                            physics.Velocity = sequence.GetPhaseDirection(sequence.CurrentPhase) * 
                                              sequence.GetPhaseSpeed(sequence.CurrentPhase);
                            break;
                        case SequencedBehaviorComponent.PhaseType.Warning:
                            // Fase di avviso, pochi movimenti o effetti visivi
                            physics.Velocity = float3.zero;
                            break;
                    }
                    
                    ECB.SetComponent(sortKey, entity, physics);
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna gli ostacoli danneggiati
        /// </summary>
        [BurstCompile]
        private partial struct UpdateDamagedObstaclesJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<RenderComponent> RenderLookup;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                ref ObstacleComponent obstacle,
                ref DamagedStateComponent damaged)
            {
                // Aggiorna il timer di recupero se applicabile
                if (damaged.CanRecover && damaged.RecoveryDelay > 0)
                {
                    damaged.RecoveryTimer += DeltaTime;
                    
                    // Se il tempo di recupero è completo, ripara l'ostacolo
                    if (damaged.RecoveryTimer >= damaged.RecoveryDelay)
                    {
                        // Ripristina l'integrità dell'ostacolo
                        damaged.CurrentIntegrity = damaged.MaxIntegrity;
                        damaged.DamageLevel = 0;
                        damaged.RecoveryTimer = 0;
                        
                        // Crea evento di ripristino
                        Entity recoveryEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, recoveryEvent, new ObstacleRepairedEvent
                        {
                            ObstacleEntity = entity
                        });
                    }
                }
                
                // Gestisci eventuali effetti continui (fumo, fuoco, ecc.)
                if (damaged.HasContinuousEffects)
                {
                    damaged.EffectTimer += DeltaTime;
                    
                    // Attiva gli effetti a intervalli regolari
                    if (damaged.EffectTimer >= damaged.EffectInterval)
                    {
                        damaged.EffectTimer = 0;
                        
                        // Crea evento di effetto
                        Entity effectEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, effectEvent, new ObstacleDamageEffectEvent
                        {
                            ObstacleEntity = entity,
                            DamageLevel = damaged.DamageLevel,
                            EffectType = damaged.EffectType
                        });
                    }
                }
                
                // Gestisci il crollo dell'ostacolo se la resistenza è esaurita
                if (damaged.CurrentIntegrity <= 0 && !damaged.IsDestroyed)
                {
                    damaged.IsDestroyed = true;
                    
                    // Crea evento di distruzione
                    Entity destroyEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, destroyEvent, new ObstacleDestroyedEvent
                    {
                        ObstacleEntity = entity
                    });
                    
                    // Se l'ostacolo è completamente distruttibile
                    if (obstacle.IsDestructible)
                    {
                        // Pianifica la rimozione dell'ostacolo dopo un breve ritardo
                        ECB.AddComponent(sortKey, entity, new DestructionDelayComponent
                        {
                            DestroyDelay = 2.0f,  // 2 secondi di animazione di distruzione
                            RemainingTime = 2.0f
                        });
                    }
                    else
                    {
                        // Altrimenti, aggiorna solo il modello/aspetto
                        if (RenderLookup.HasComponent(entity))
                        {
                            var render = RenderLookup[entity];
                            render.ModelVariant = (byte)damaged.DamageLevel;
                            ECB.SetComponent(sortKey, entity, render);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che attiva gli ostacoli in base alla distanza dai giocatori
        /// </summary>
        [BurstCompile]
        private partial struct ActivateObstaclesByDistanceJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                in TransformComponent transform, 
                in ObstacleComponent obstacle)
            {
                // Questo lavoro dovrebbe essere integrato con una lista di posizioni giocatore
                // e la logica per attivare gli ostacoli quando un giocatore si avvicina
                
                // Esempio: attivare ostacoli speciali quando un giocatore è a distanza X
                // Per ora lasciamo la logica vuota, in quanto richiede le posizioni dei giocatori
            }
        }
    }
    
    #region Componenti per tipi speciali di ostacoli
    
    /// <summary>
    /// Componente per ostacoli che si muovono lungo un percorso predefinito
    /// </summary>
    [System.Serializable]
    public struct PatrolPathComponent : IComponentData
    {
        public float3 StartPoint;        // Punto di partenza
        public float3 EndPoint;          // Punto di arrivo
        public byte CurrentPointIndex;   // Indice del punto corrente
        public byte TotalPoints;         // Numero totale di punti (2 per percorsi lineari)
        public float3 CurrentPoint;      // Punto target corrente
        public float MoveSpeed;          // Velocità di movimento
        public bool IsLooping;           // Se il percorso si ripete all'infinito
        
        // Ottiene un punto specifico del percorso
        public float3 GetPoint(int index)
        {
            if (TotalPoints <= 2)
            {
                return index == 0 ? StartPoint : EndPoint;
            }
            
            // Per percorsi più complessi si dovrebbe implementare una logica più avanzata
            // o utilizzare un buffer per memorizzare i punti
            return StartPoint;
        }
    }
    
    /// <summary>
    /// Componente per ostacoli che si muovono con moto oscillatorio
    /// </summary>
    [System.Serializable]
    public struct OscillatingObstacleComponent : IComponentData
    {
        public float3 CenterPosition;   // Posizione centrale attorno a cui oscilla
        public float AmplitudeX;        // Ampiezza di oscillazione sull'asse X
        public float AmplitudeY;        // Ampiezza di oscillazione sull'asse Y
        public float AmplitudeZ;        // Ampiezza di oscillazione sull'asse Z
        public float FrequencyX;        // Frequenza di oscillazione sull'asse X
        public float FrequencyY;        // Frequenza di oscillazione sull'asse Y
        public float FrequencyZ;        // Frequenza di oscillazione sull'asse Z
        public float CurrentTime;       // Tempo corrente per il calcolo dell'oscillazione
    }
    
    /// <summary>
    /// Componente per ostacoli rotanti
    /// </summary>
    [System.Serializable]
    public struct RotatingObstacleComponent : IComponentData
    {
        public float3 RotationAxis;     // Asse di rotazione
        public float RotationSpeed;     // Velocità di rotazione in radianti al secondo
    }
    
    /// <summary>
    /// Componente per ostacoli temporanei con durata limitata
    /// </summary>
    [System.Serializable]
    public struct TemporaryObstacleComponent : IComponentData
    {
        public float TotalLifetime;      // Durata totale dell'ostacolo in secondi
        public float RemainingLifetime;  // Tempo rimanente prima della scomparsa
        public bool HasFadeOut;          // Se l'ostacolo ha un effetto di dissolvenza
        public float FadeOutTime;        // Tempo di dissolvenza prima della rimozione
    }
    
    /// <summary>
    /// Componente per ostacoli con comportamenti in sequenza
    /// </summary>
    [System.Serializable]
    public struct SequencedBehaviorComponent : IComponentData
    {
        public byte CurrentPhase;         // Fase corrente
        public byte TotalPhases;          // Numero totale di fasi
        public float CurrentPhaseTimer;   // Timer della fase corrente
        
        // Dati di ogni fase
        // Nota: per supportare più di 4 fasi, si dovrebbe usare un buffer
        public PhaseType Phase1Type;
        public PhaseType Phase2Type;
        public PhaseType Phase3Type;
        public PhaseType Phase4Type;
        
        public float Phase1Duration;
        public float Phase2Duration;
        public float Phase3Duration;
        public float Phase4Duration;
        
        public float3 Phase1Direction;
        public float3 Phase2Direction;
        public float3 Phase3Direction;
        public float3 Phase4Direction;
        
        public float Phase1Speed;
        public float Phase2Speed;
        public float Phase3Speed;
        public float Phase4Speed;
        
        public enum PhaseType : byte
        {
            Idle = 0,       // Fase inattiva
            Warning = 1,    // Fase di avviso
            Active = 2,     // Fase attiva
            Recovery = 3    // Fase di recupero
        }
        
        // Ottiene la durata di una fase specifica
        public float GetPhaseDuration(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return Phase1Duration;
                case 1: return Phase2Duration;
                case 2: return Phase3Duration;
                case 3: return Phase4Duration;
                default: return 1.0f;
            }
        }
        
        // Ottiene il tipo di una fase specifica
        public PhaseType GetPhaseType(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return Phase1Type;
                case 1: return Phase2Type;
                case 2: return Phase3Type;
                case 3: return Phase4Type;
                default: return PhaseType.Idle;
            }
        }
        
        // Ottiene la direzione di una fase specifica
        public float3 GetPhaseDirection(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return Phase1Direction;
                case 1: return Phase2Direction;
                case 2: return Phase3Direction;
                case 3: return Phase4Direction;
                default: return float3.zero;
            }
        }
        
        // Ottiene la velocità di una fase specifica
        public float GetPhaseSpeed(int phaseIndex)
        {
            switch (phaseIndex)
            {
                case 0: return Phase1Speed;
                case 1: return Phase2Speed;
                case 2: return Phase3Speed;
                case 3: return Phase4Speed;
                default: return 0.0f;
            }
        }
    }
    
    /// <summary>
    /// Componente per ostacoli danneggiati
    /// </summary>
    [System.Serializable]
    public struct DamagedStateComponent : IComponentData
    {
        public float MaxIntegrity;        // Resistenza massima
        public float CurrentIntegrity;    // Resistenza corrente
        public byte DamageLevel;          // Livello di danno (0-3)
        public bool IsDestroyed;          // Se è stato distrutto
        
        public bool CanRecover;           // Se può ripararsi col tempo
        public float RecoveryDelay;       // Tempo necessario per ripararsi
        public float RecoveryTimer;       // Timer di recupero
        
        public bool HasContinuousEffects; // Se ha effetti continui (fumo, ecc.)
        public float EffectInterval;      // Intervallo tra gli effetti
        public float EffectTimer;         // Timer per gli effetti
        public byte EffectType;           // Tipo di effetto
    }
    
    /// <summary>
    /// Componente per il ritardo della distruzione
    /// </summary>
    [System.Serializable]
    public struct DestructionDelayComponent : IComponentData
    {
        public float DestroyDelay;        // Ritardo prima della distruzione
        public float RemainingTime;       // Tempo rimanente
    }
    
    /// <summary>
    /// Tag per ostacoli in movimento
    /// </summary>
    [System.Serializable]
    public struct MovingObstacleTag : IComponentData {}
    
    #endregion
    
    #region Eventi
    
    /// <summary>
    /// Evento generato quando un ostacolo cambia fase
    /// </summary>
    public struct ObstaclePhaseChangedEvent : IComponentData
    {
        public Entity ObstacleEntity;   // Entità dell'ostacolo
        public byte NewPhase;           // Nuova fase
        public byte TotalPhases;        // Numero totale di fasi
    }
    
    /// <summary>
    /// Evento generato quando un ostacolo viene distrutto
    /// </summary>
    public struct ObstacleDestroyedEvent : IComponentData
    {
        public Entity ObstacleEntity;   // Entità dell'ostacolo
    }
    
    /// <summary>
    /// Evento generato quando un ostacolo viene riparato
    /// </summary>
    public struct ObstacleRepairedEvent : IComponentData
    {
        public Entity ObstacleEntity;   // Entità dell'ostacolo
    }
    
    /// <summary>
    /// Evento generato per gli effetti di danno
    /// </summary>
    public struct ObstacleDamageEffectEvent : IComponentData
    {
        public Entity ObstacleEntity;   // Entità dell'ostacolo
        public byte DamageLevel;        // Livello di danno
        public byte EffectType;         // Tipo di effetto
    }
    
    #endregion
}
