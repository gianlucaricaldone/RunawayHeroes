// Path: Assets/_Project/ECS/Systems/Abilities/ControlledGlitchSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce l'abilità "Glitch Controllato" di Neo.
    /// Si occupa della deformazione temporanea della realtà che permette
    /// di attraversare barriere digitali.
    /// </summary>
    public partial class ControlledGlitchSystem : SystemBase
    {
        private EntityQuery _abilityQuery;
        private EntityQuery _barrierQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            _abilityQuery = GetEntityQuery(
                ComponentType.ReadWrite<ControlledGlitchAbilityComponent>(),
                ComponentType.ReadOnly<AbilityInputComponent>(),
                ComponentType.ReadOnly<NeoComponent>()
            );
            
            // Query per barriere digitali
            _barrierQuery = GetEntityQuery(
                ComponentType.ReadOnly<ObstacleComponent>(),
                ComponentType.ReadOnly<DigitalBarrierTag>()
            );
            
            RequireForUpdate(_abilityQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni la lista di barriere prima di entrare nel job
            var barriers = _barrierQuery.ToEntityArray(Allocator.TempJob);
            var barrierPositions = new NativeArray<float3>(barriers.Length, Allocator.TempJob);
            
            // Popola le posizioni delle barriere
            for (int i = 0; i < barriers.Length; i++)
            {
                if (EntityManager.HasComponent<TransformComponent>(barriers[i]))
                {
                    barrierPositions[i] = EntityManager.GetComponentData<TransformComponent>(barriers[i]).Position;
                }
            }
            
            Entities
                .WithName("ControlledGlitchSystem")
                .WithReadOnly(barriers)
                .WithReadOnly(barrierPositions)
                .WithDisposeOnCompletion(barriers)
                .WithDisposeOnCompletion(barrierPositions)
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref ControlledGlitchAbilityComponent glitch,
                          ref TransformComponent transform,
                          ref PhysicsComponent physics,
                          in AbilityInputComponent abilityInput,
                          in NeoComponent neoComponent) =>
                {
                    // Aggiorna timer e stato
                    bool stateChanged = false;
                    
                    // Se l'abilità è attiva, gestisci la durata
                    if (glitch.IsActive)
                    {
                        glitch.RemainingTime -= deltaTime;
                        
                        if (glitch.RemainingTime <= 0 || glitch.GlitchCompleted)
                        {
                            // Termina l'abilità
                            glitch.IsActive = false;
                            glitch.RemainingTime = 0;
                            glitch.GlitchCompleted = false;
                            stateChanged = true;
                            
                            // Crea evento di fine abilità
                            var endAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, endAbilityEvent, new AbilityEndedEvent
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
                                
                                for (int i = 0; i < barriers.Length; i++)
                                {
                                    float3 currentBarrierPos = barrierPositions[i];
                                    
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
                                            closestBarrier = barriers[i];
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
                                    var teleportStartEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                    commandBuffer.AddComponent(entityInQueryIndex, teleportStartEvent, new GlitchTeleportStartEvent
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
                                    var teleportEndEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                    commandBuffer.AddComponent(entityInQueryIndex, teleportEndEvent, new GlitchTeleportEndEvent
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
                        glitch.CooldownRemaining -= deltaTime;
                        
                        if (glitch.CooldownRemaining <= 0)
                        {
                            glitch.CooldownRemaining = 0;
                            stateChanged = true;
                            
                            // Crea evento di abilità pronta
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new AbilityReadyEvent
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
                        var activateEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, activateEvent, new AbilityActivatedEvent
                        {
                            EntityID = entity,
                            AbilityType = AbilityType.ControlledGlitch,
                            Position = transform.Position,
                            Duration = glitch.Duration
                        });
                    }
                    
                }).ScheduleParallel();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
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
}