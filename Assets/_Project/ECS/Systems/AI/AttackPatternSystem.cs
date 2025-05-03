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
    /// Sistema che gestisce i pattern di attacco dei nemici.
    /// Implementa logiche di attacco complesse in base ai tipi di pattern
    /// definiti, coordina le fasi di attacco e genera gli eventi di danno.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EnemyAISystem))]
    [BurstCompile]
    public partial struct AttackPatternSystem : ISystem
    {
        // Query per le entità in stato di attacco
        private EntityQuery _attackingQuery;
        
        // Query per i giocatori (target)
        private EntityQuery _targetQuery;
        
        // Stati di attacco in corso
        private ComponentLookup<AttackingStateComponent> _attackingStateLookup;
        
        /// <summary>
        /// Inizializza il sistema di pattern di attacco
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per i nemici in stato di attacco
            _attackingQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AttackComponent, TransformComponent, PhysicsComponent>()
                .WithNone<StunnedTag>() // Escludi nemici storditi
                .Build(ref state);
                
            // Configura la query per i possibili target
            _targetQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità con attacchi e target per l'aggiornamento
            state.RequireForUpdate(_attackingQuery);
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Inizializza il lookup per lo stato di attacco
            _attackingStateLookup = state.GetComponentLookup<AttackingStateComponent>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna i pattern di attacco per tutti i nemici in stato di attacco
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Aggiorna il lookup
            _attackingStateLookup.Update(ref state);
            
            // Raccogli le posizioni dei target (giocatori) se ce ne sono
            NativeArray<float3> targetPositions;
            
            if (!_targetQuery.IsEmpty)
            {
                var targetTransforms = _targetQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
                targetPositions = new NativeArray<float3>(targetTransforms.Length, Allocator.TempJob);
                
                for (int i = 0; i < targetTransforms.Length; i++)
                {
                    targetPositions[i] = targetTransforms[i].Position;
                }
                
                targetTransforms.Dispose();
            }
            else
            {
                targetPositions = new NativeArray<float3>(0, Allocator.TempJob);
            }
            
            // Processa gli attacchi
            // Utilizziamo un job per aggiornare i cooldown e preparare gli attacchi
            var updateCooldownsJob = new UpdateAttackCooldownsJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel(_attackingQuery, state.Dependency);
            
            // Processa gli attacchi in esecuzione
            var processActiveAttacksJob = new ProcessActiveAttacksJob
            {
                DeltaTime = deltaTime,
                TargetPositions = targetPositions,
                AttackingStates = _attackingStateLookup,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_attackingQuery, updateCooldownsJob);
            
            // Gestione degli eventi di attacco in arrivo dal EnemyAISystem
            // [BurstDiscard] perché la creazione di eventi di attacco specifici potrebbe richiedere logica non Burst
            ProcessEnemyAttackEvents(ref state, commandBuffer);
            
            // Cleanup
            state.Dependency = targetPositions.Dispose(processActiveAttacksJob);
        }
        
        [BurstDiscard]
        private void ProcessEnemyAttackEvents(ref SystemState state, EntityCommandBuffer ecb)
        {
            // Cerca eventi di attacco generati da altri sistemi (come EnemyAISystem)
            var eventQuery = SystemAPI.QueryBuilder().WithAll<EnemyAttackEvent>().Build();
            
            if (!eventQuery.IsEmpty)
            {
                var attackEvents = eventQuery.ToComponentDataArray<EnemyAttackEvent>(Allocator.Temp);
                var eventEntities = eventQuery.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < attackEvents.Length; i++)
                {
                    var attackEvent = attackEvents[i];
                    
                    // Verifica che l'entità attaccante abbia un componente AttackComponent
                    if (state.EntityManager.HasComponent<AttackComponent>(attackEvent.AttackerEntity))
                    {
                        var attackComponent = state.EntityManager.GetComponentData<AttackComponent>(attackEvent.AttackerEntity);
                        
                        // Aggiungi il componente di stato di attacco se non è già presente
                        if (!state.EntityManager.HasComponent<AttackingStateComponent>(attackEvent.AttackerEntity))
                        {
                            ecb.AddComponent(attackEvent.AttackerEntity, new AttackingStateComponent
                            {
                                TargetPosition = attackEvent.TargetPosition,
                                AttackType = attackEvent.AttackType,
                                StartTime = (float)SystemAPI.Time.ElapsedTime,
                                Phase = AttackPhase.Startup,
                                Progress = 0.0f
                            });
                        }
                    }
                    
                    // Rimuovi l'evento dopo averlo gestito
                    ecb.DestroyEntity(eventEntities[i]);
                }
                
                attackEvents.Dispose();
                eventEntities.Dispose();
            }
        }
        
        /// <summary>
        /// Job che aggiorna i timer di cooldown degli attacchi
        /// </summary>
        [BurstCompile]
        private partial struct UpdateAttackCooldownsJob : IJobEntity
        {
            public float DeltaTime;
            
            void Execute(ref AttackComponent attackComponent)
            {
                // Aggiorna il cooldown se necessario
                if (attackComponent.CurrentCooldown > 0)
                {
                    attackComponent.CurrentCooldown -= DeltaTime;
                    if (attackComponent.CurrentCooldown < 0)
                    {
                        attackComponent.CurrentCooldown = 0;
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che elabora gli attacchi attivamente in esecuzione
        /// </summary>
        [BurstCompile]
        private partial struct ProcessActiveAttacksJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeArray<float3> TargetPositions;
            [NativeDisableParallelForRestriction] public ComponentLookup<AttackingStateComponent> AttackingStates;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            void Execute(Entity entity, 
                        [EntityIndexInQuery] int sortKey, 
                        ref AttackComponent attackComponent,
                        ref TransformComponent transform,
                        ref PhysicsComponent physics)
            {
                // Verifica se l'entità ha uno stato di attacco attivo
                if (!AttackingStates.HasComponent(entity))
                {
                    return;
                }
                
                var attackState = AttackingStates[entity];
                
                // Aggiorna la progressione dell'attacco
                float patternDuration = GetPatternDuration(attackComponent.PatternType);
                attackState.Progress += DeltaTime / patternDuration * attackComponent.PatternSpeed;
                
                // Determina la fase dell'attacco in base al progresso
                if (attackState.Progress < 0.3f)
                {
                    attackState.Phase = AttackPhase.Startup;
                }
                else if (attackState.Progress < 0.7f)
                {
                    attackState.Phase = AttackPhase.Active;
                }
                else if (attackState.Progress < 1.0f)
                {
                    attackState.Phase = AttackPhase.Recovery;
                }
                else
                {
                    // Attacco completato
                    ECB.RemoveComponent<AttackingStateComponent>(sortKey, entity);
                    
                    // Imposta il cooldown
                    attackComponent.CurrentCooldown = attackComponent.AttackCooldown;
                    return;
                }
                
                // Applica il comportamento specifico in base al tipo di pattern
                ApplyAttackPatternBehavior(
                    entity, 
                    sortKey,
                    ref attackComponent, 
                    ref transform, 
                    ref physics, 
                    ref attackState
                );
                
                // Aggiorna lo stato
                AttackingStates[entity] = attackState;
            }
            
            /// <summary>
            /// Ottiene la durata di un pattern di attacco in base al tipo
            /// </summary>
            private float GetPatternDuration(AttackPatternType patternType)
            {
                switch (patternType)
                {
                    case AttackPatternType.Direct:
                        return 0.8f;
                    case AttackPatternType.Sweep:
                        return 1.2f;
                    case AttackPatternType.Burst:
                        return 1.5f;
                    case AttackPatternType.Charge:
                        return 2.0f;
                    case AttackPatternType.Projectile:
                        return 1.0f;
                    case AttackPatternType.AOE:
                        return 1.8f;
                    case AttackPatternType.Summon:
                        return 2.5f;
                    case AttackPatternType.DoT:
                        return 3.0f;
                    case AttackPatternType.Teleport:
                        return 1.2f;
                    case AttackPatternType.Special:
                        return 3.5f;
                    default:
                        return 1.0f;
                }
            }
            
            /// <summary>
            /// Applica il comportamento specifico per il pattern di attacco
            /// </summary>
            private void ApplyAttackPatternBehavior(
                Entity entity,
                int sortKey,
                ref AttackComponent attackComponent,
                ref TransformComponent transform,
                ref PhysicsComponent physics,
                ref AttackingStateComponent attackState)
            {
                // Ottieni il target più vicino o quello specificato nell'attacco
                float3 targetPosition = attackState.TargetPosition;
                
                // In base al tipo di pattern applica comportamenti diversi
                switch (attackComponent.PatternType)
                {
                    case AttackPatternType.Direct:
                        // Attacco diretto - muovi verso il target e poi colpisci
                        if (attackState.Phase == AttackPhase.Startup)
                        {
                            // Avvicinati al target
                            float3 direction = math.normalize(targetPosition - transform.Position);
                            physics.Velocity = direction * 5.0f; // Velocità di avvicinamento
                        }
                        else if (attackState.Phase == AttackPhase.Active)
                        {
                            // Colpisci - genera un evento di danno se abbastanza vicino
                            float distance = math.distance(transform.Position, targetPosition);
                            if (distance <= attackComponent.AttackRange)
                            {
                                // Crea un evento di danno
                                Entity damageEvent = ECB.CreateEntity(sortKey);
                                ECB.AddComponent(sortKey, damageEvent, new DamageEvent
                                {
                                    SourceEntity = entity,
                                    TargetPosition = targetPosition,
                                    Damage = attackComponent.BaseDamage,
                                    DamageRadius = attackComponent.IsAreaEffect ? attackComponent.AreaRadius : 0.5f,
                                    ElementType = (byte)attackComponent.ElementType,
                                    StatusEffectType = (byte)attackComponent.StatusEffect,
                                    StatusEffectDuration = attackComponent.StatusEffectDuration
                                });
                            }
                            
                            // Ferma il movimento durante la fase attiva
                            physics.Velocity = float3.zero;
                        }
                        else if (attackState.Phase == AttackPhase.Recovery)
                        {
                            // Recovery - muoviti leggermente all'indietro
                            float3 direction = math.normalize(transform.Position - targetPosition);
                            physics.Velocity = direction * 2.0f; // Velocità di recovery
                        }
                        break;
                        
                    case AttackPatternType.Sweep:
                        // Attacco a spazzata - ruota attorno al target
                        if (attackState.Phase == AttackPhase.Startup)
                        {
                            // Preparati per lo sweep
                            float3 direction = math.normalize(transform.Position - targetPosition);
                            float sweepRadius = math.min(attackComponent.AttackRange * 0.8f, 
                                                      math.distance(transform.Position, targetPosition));
                            
                            // Muoviti in posizione
                            physics.Velocity = direction * 3.0f;
                        }
                        else if (attackState.Phase == AttackPhase.Active)
                        {
                            // Esegui lo sweep - movimento circolare
                            float sweepAngle = math.PI * 2 * (attackState.Progress - 0.3f) / 0.4f;
                            float sweepRadius = attackComponent.AttackRange * 0.8f;
                            
                            float3 sweepPos = new float3(
                                math.cos(sweepAngle) * sweepRadius,
                                0,
                                math.sin(sweepAngle) * sweepRadius
                            );
                            
                            float3 targetDir = math.normalize(targetPosition - transform.Position);
                            quaternion rotation = quaternion.LookRotation(targetDir, new float3(0, 1, 0));
                            sweepPos = math.rotate(rotation, sweepPos) + targetPosition;
                            
                            // Applica il movimento
                            physics.Velocity = (sweepPos - transform.Position) * 10.0f;
                            
                            // Genera eventi di danno periodicamente durante lo sweep
                            if (math.fmod(attackState.Progress * 10, 1.0f) < 0.1f)
                            {
                                Entity damageEvent = ECB.CreateEntity(sortKey);
                                ECB.AddComponent(sortKey, damageEvent, new DamageEvent
                                {
                                    SourceEntity = entity,
                                    TargetPosition = transform.Position + math.normalize(physics.Velocity) * attackComponent.AttackRange,
                                    Damage = attackComponent.BaseDamage * 0.5f,
                                    DamageRadius = 1.0f,
                                    ElementType = (byte)attackComponent.ElementType,
                                    StatusEffectType = (byte)attackComponent.StatusEffect,
                                    StatusEffectDuration = attackComponent.StatusEffectDuration * 0.5f
                                });
                            }
                        }
                        else if (attackState.Phase == AttackPhase.Recovery)
                        {
                            // Recovery - rallenta
                            physics.Velocity *= 0.8f;
                        }
                        break;
                        
                    case AttackPatternType.Burst:
                        // Implementazione di altri pattern
                        // ... (altri pattern implementati con logiche simili)
                        break;
                        
                    default:
                        // Pattern non implementato, usa direct come fallback
                        break;
                }
            }
        }
    }
    
    /// <summary>
    /// Componente che rappresenta lo stato di un attacco in esecuzione
    /// </summary>
    public struct AttackingStateComponent : IComponentData
    {
        public float3 TargetPosition;   // Posizione target dell'attacco
        public byte AttackType;         // Tipo/variazione dell'attacco
        public float StartTime;         // Momento in cui è iniziato l'attacco
        public AttackPhase Phase;       // Fase corrente dell'attacco
        public float Progress;          // Progresso dell'attacco (0-1)
    }
    
    /// <summary>
    /// Fasi di un attacco
    /// </summary>
    public enum AttackPhase : byte
    {
        Startup = 0,    // Fase di preparazione
        Active = 1,     // Fase attiva (danno)
        Recovery = 2    // Fase di recupero
    }
    
    /// <summary>
    /// Evento di danno generato dagli attacchi
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        public Entity SourceEntity;      // Entità che ha generato il danno
        public float3 TargetPosition;    // Posizione target dell'attacco
        public float Damage;             // Quantità di danno
        public float DamageRadius;       // Raggio del danno
        public byte ElementType;         // Tipo di elemento
        public byte StatusEffectType;    // Tipo di effetto di stato
        public float StatusEffectDuration; // Durata dell'effetto di stato
    }
}
