// Path: Assets/_Project/ECS/Systems/Abilities/ControlledGlitchSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Glitch Controllato" di Neo.
    /// Si occupa della deformazione temporanea della realtà che permette
    /// di attraversare barriere digitali.
    /// </summary>
    public partial struct ControlledGlitchSystem : ISystem
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _barrierQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisci la query per l'abilità usando EntityQueryBuilder
            _abilityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ControlledGlitchAbilityComponent, AbilityInputComponent, NeoComponent>()
                .Build(ref state);
            
            // Query per barriere digitali
            _barrierQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, DigitalBarrierTag>()
                .Build(ref state);
            
            // Richiedi entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_abilityQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni la lista di barriere prima di entrare nel job
            var barriers = _barrierQuery.ToEntityArray(Allocator.TempJob);
            var barrierPositions = new NativeArray<float3>(barriers.Length, Allocator.TempJob);
            
            // Popola le posizioni delle barriere
            for (int i = 0; i < barriers.Length; i++)
            {
                if (state.EntityManager.HasComponent<TransformComponent>(barriers[i]))
                {
                    barrierPositions[i] = state.EntityManager.GetComponentData<TransformComponent>(barriers[i]).Position;
                }
            }
            
            // Usa un IJobEntity invece di Entities.ForEach
            new ControlledGlitchJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                Barriers = barriers,
                BarrierPositions = barrierPositions
            }.ScheduleParallel(_abilityQuery, state.Dependency).Complete();
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
        }
    }
    
    /// <summary>
    /// Tag per identificare le barriere digitali
    /// </summary>
    public struct DigitalBarrierTag : IComponentData { }
    
    /// <summary>
    /// Evento per l'inizio del teletrasporto
    /// </summary>
    public struct GlitchTeleportStartEvent : IComponentData
    {
        public Entity EntityID;       // Entità che si teletrasporta
        public float3 StartPosition;  // Posizione iniziale
        public float3 TargetPosition; // Posizione finale
        public Entity BarrierEntity;  // Barriera attraversata
    }
    
    /// <summary>
    /// Evento per la fine del teletrasporto
    /// </summary>
    public struct GlitchTeleportEndEvent : IComponentData
    {
        public Entity EntityID;       // Entità che ha completato il teletrasporto
        public float3 FinalPosition;  // Posizione finale
    }
    
    /// <summary>
    /// Job per gestire l'abilità di Glitch Controllato
    /// </summary>
    [BurstCompile]
    public partial struct ControlledGlitchJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public NativeArray<Entity> Barriers;
        [ReadOnly] public NativeArray<float3> BarrierPositions;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                          ref ControlledGlitchAbilityComponent glitch,
                          ref TransformComponent transform,
                          ref PhysicsComponent physics,
                          in AbilityInputComponent abilityInput,
                          in NeoComponent neoComponent)
        {
            // Aggiorna timer e stato
            bool stateChanged = false;
            
            // Se l'abilità è attiva, gestisci la durata
            if (glitch.IsActive)
            {
                glitch.RemainingTime -= DeltaTime;
                
                if (glitch.RemainingTime <= 0 || glitch.GlitchCompleted)
                {
                    // Termina l'abilità
                    glitch.IsActive = false;
                    glitch.RemainingTime = 0;
                    glitch.GlitchCompleted = false;
                    stateChanged = true;
                    
                    // Crea evento di fine abilità
                    var endAbilityEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.ControlledGlitch
                    });
                }
                else
                {
                    // L'abilità è attiva - gestisci il teletrasporto attraverso barriere
                    if (!glitch.TeleportStarted)
                    {
                        // Trova la barriera più vicina nella direzione di movimento
                        float3 forwardDirection = new float3(0, 0, 1); // Direzione di default
                        if (math.lengthsq(physics.Velocity) > 0.01f)
                        {
                            forwardDirection = math.normalize(physics.Velocity);
                        }
                        
                        Entity closestBarrier = Entity.Null;
                        float closestDistance = float.MaxValue;
                        float3 barrierPosition = float3.zero;
                        
                        for (int i = 0; i < Barriers.Length; i++)
                        {
                            float3 currentBarrierPos = BarrierPositions[i];
                            
                            // Calcola la distanza lungo la direzione di movimento
                            float3 toBarrier = currentBarrierPos - transform.Position;
                            float distanceAlongDirection = math.dot(toBarrier, forwardDirection);
                            
                            // Considera solo barriere davanti al giocatore, entro il raggio d'azione
                            if (distanceAlongDirection > 0 && distanceAlongDirection < glitch.GlitchDistance)
                            {
                                // Calcola la distanza laterale (perpendicolare alla direzione)
                                float3 projection = distanceAlongDirection * forwardDirection;
                                float3 perpendicular = toBarrier - projection;
                                float lateralDistance = math.length(perpendicular);
                                
                                // Considera solo barriere abbastanza vicino lateralmente
                                if (lateralDistance < 2.0f && distanceAlongDirection < closestDistance)
                                {
                                    closestDistance = distanceAlongDirection;
                                    closestBarrier = Barriers[i];
                                    barrierPosition = currentBarrierPos;
                                }
                            }
                        }
                        
                        // Se abbiamo trovato una barriera, inizia il teletrasporto
                        if (closestBarrier != Entity.Null)
                        {
                            glitch.TeleportStarted = true;
                            glitch.TargetPosition = barrierPosition + forwardDirection * 2.0f; // 2 metri oltre la barriera
                            
                            // Crea evento di inizio teletrasporto
                            var teleportStartEvent = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, teleportStartEvent, new GlitchTeleportStartEvent
                            {
                                EntityID = entity,
                                StartPosition = transform.Position,
                                TargetPosition = glitch.TargetPosition,
                                BarrierEntity = closestBarrier
                            });
                        }
                    }
                    else
                    {
                        // Continua il teletrasporto
                        float teleportProgress = 1.0f - (glitch.RemainingTime / glitch.Duration);
                        
                        // Usa una curva non lineare per il movimento
                        float smoothProgress = teleportProgress * teleportProgress * (3.0f - 2.0f * teleportProgress);
                        
                        // Interpola la posizione
                        transform.Position = math.lerp(glitch.StartPosition, glitch.TargetPosition, smoothProgress);
                        
                        // Se il teletrasporto è quasi completo
                        if (teleportProgress > 0.95f)
                        {
                            // Finalizza il teletrasporto
                            transform.Position = glitch.TargetPosition;
                            glitch.GlitchCompleted = true;
                            
                            // Crea evento di fine teletrasporto
                            var teleportEndEvent = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, teleportEndEvent, new GlitchTeleportEndEvent
                            {
                                EntityID = entity,
                                FinalPosition = glitch.TargetPosition
                            });
                        }
                    }
                }
            }
            
            // Aggiorna il cooldown
            if (glitch.CooldownRemaining > 0)
            {
                glitch.CooldownRemaining -= DeltaTime;
                
                if (glitch.CooldownRemaining <= 0)
                {
                    glitch.CooldownRemaining = 0;
                    stateChanged = true;
                    
                    // Crea evento di abilità pronta
                    var readyEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
                    {
                        EntityID = entity,
                        AbilityType = AbilityType.ControlledGlitch
                    });
                }
            }
            
            // Controlla input per attivazione abilità
            if (abilityInput.ActivateAbility && glitch.IsAvailable && !glitch.IsActive)
            {
                // Attiva l'abilità
                glitch.IsActive = true;
                glitch.RemainingTime = glitch.Duration;
                glitch.CooldownRemaining = glitch.Cooldown;
                glitch.TeleportStarted = false;
                glitch.GlitchCompleted = false;
                glitch.StartPosition = transform.Position;
                
                // Potenzia le capacità di glitch in base alle abilità di Neo
                glitch.GlitchDistance *= (1.0f + neoComponent.GlitchManipulation * 0.5f);
                
                // Crea evento di attivazione abilità
                var activateEvent = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                {
                    EntityID = entity,
                    AbilityType = AbilityType.ControlledGlitch,
                    Position = transform.Position,
                    Duration = glitch.Duration
                });
            }
        }
    }
}