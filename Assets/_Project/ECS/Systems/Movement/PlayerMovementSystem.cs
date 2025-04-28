using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce il movimento del giocatore in base all'input ricevuto.
    /// Elabora la corsa automatica, i movimenti laterali, e coordina con altri sistemi
    /// come salto e scivolata.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(InputSystem))]
    public partial class PlayerMovementSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Costanti di configurazione
        private const float LANE_WIDTH = 3.0f;             // Larghezza della corsia
        private const float MAX_LANE_OFFSET = LANE_WIDTH;  // Offset massimo per corsia
        private const float GROUND_LEVEL = 0.0f;           // Livello del terreno
        private const float GRAVITY_MULTIPLIER = 1.5f;     // Moltiplicatore della gravità per salti più realistici
        private const float GROUND_CHECK_DISTANCE = 0.1f;  // Distanza per controllo contatto con suolo
        
        protected override void OnCreate()
        {
            // Ottiene il sistema di command buffer per le modifiche strutturali
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Definisce la query per identificare le entità giocatore
            _playerQuery = GetEntityQuery(
                ComponentType.ReadWrite<TransformComponent>(),
                ComponentType.ReadWrite<PhysicsComponent>(),
                ComponentType.ReadWrite<MovementComponent>(),
                ComponentType.ReadOnly<InputComponent>()
            );
            
            // Richiede almeno un giocatore per l'esecuzione
            RequireForUpdate(_playerQuery);
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Processa l'input di salto
            Entities
                .WithName("ProcessJumpInput")
                .WithAll<TagComponent>()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref TransformComponent transform,
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          in InputComponent input) =>
                {
                    // Verifica se l'input di salto è stato attivato
                    if (input.JumpPressed && (physics.IsGrounded || movement.RemainingJumps > 0))
                    {
                        // Inizia il salto
                        if (movement.TryJump())
                        {
                            // Applica la forza di salto
                            physics.Velocity.y = movement.JumpForce;
                            physics.IsGrounded = false;
                            
                            // Crea un evento di salto
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new JumpStartedEvent
                            {
                                PlayerEntity = entity,
                                JumpForce = movement.JumpForce,
                                RemainingJumps = movement.RemainingJumps
                            });
                        }
                    }
                }).ScheduleParallel();
            
            // Processa l'input di scivolata
            Entities
                .WithName("ProcessSlideInput")
                .WithAll<TagComponent>()
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref TransformComponent transform,
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          in InputComponent input) =>
                {
                    // Verifica se l'input di scivolata è stato attivato
                    if (input.SlidePressed && physics.IsGrounded && !movement.IsSliding)
                    {
                        // Inizia la scivolata
                        if (movement.TrySlide())
                        {
                            // Crea un evento di inizio scivolata
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new SlideStartedEvent
                            {
                                PlayerEntity = entity,
                                Duration = movement.SlideDuration
                            });
                        }
                    }
                    
                    // Aggiorna il timer della scivolata se attiva
                    if (movement.IsSliding)
                    {
                        movement.SlideTimeRemaining -= deltaTime;
                        
                        // Termina la scivolata se il tempo è scaduto
                        if (movement.SlideTimeRemaining <= 0)
                        {
                            movement.IsSliding = false;
                            movement.SlideTimeRemaining = 0;
                            
                            // Crea un evento di fine scivolata
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new SlideEndedEvent
                            {
                                PlayerEntity = entity
                            });
                        }
                    }
                }).ScheduleParallel();
            
            // Elabora il movimento in avanti automatico e il movimento laterale basato sull'input
            Entities
                .WithName("PlayerMovementProcessor")
                .WithAll<TagComponent>()
                .ForEach((Entity entity, int entityInQueryIndex, 
                          ref TransformComponent transform, 
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          in InputComponent input) => 
                {
                    // Gestione dello stato di movimento
                    bool wasMoving = movement.IsMoving;
                    movement.IsMoving = input.IsMovementEnabled;
                    
                    // Se lo stato di movimento è cambiato, genera eventi appropriati
                    if (!wasMoving && movement.IsMoving)
                    {
                        // Crea un evento di inizio movimento
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new MovementStartedEvent
                        {
                            PlayerEntity = entity
                        });
                    }
                    else if (wasMoving && !movement.IsMoving)
                    {
                        // Crea un evento di fine movimento
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new MovementStoppedEvent
                        {
                            PlayerEntity = entity
                        });
                        
                        // Ferma gradualmente il movimento
                        physics.Velocity *= 0.9f;
                    }
                    
                    // Calcola la velocità effettiva in base a stati speciali
                    float effectiveSpeed = movement.CurrentSpeed;
                    
                    // Modifica la velocità durante la scivolata
                    if (movement.IsSliding)
                    {
                        effectiveSpeed *= movement.SlideSpeedMultiplier;
                    }
                    
                    // Spostamento automatico in avanti (direzione Z)
                    if (movement.IsMoving)
                    {
                        // Accelerazione graduale fino alla velocità target
                        float targetSpeed = effectiveSpeed;
                        physics.Velocity.z = math.lerp(physics.Velocity.z, targetSpeed, movement.Acceleration * deltaTime);
                    }
                    
                    // Movimento laterale basato sull'input (direzione X)
                    if (movement.IsMoving)
                    {
                        float lateralSpeed = effectiveSpeed * 0.8f; // Leggermente più lento del movimento in avanti
                        float targetLateralSpeed = input.LateralMovement * lateralSpeed;
                        physics.Velocity.x = math.lerp(physics.Velocity.x, targetLateralSpeed, movement.Acceleration * deltaTime);
                        
                        // Aggiorna la direzione di movimento
                        movement.MoveDirection = new float3(input.MoveDirection.x, 0, input.MoveDirection.y);
                    }
                    
                    // Applica la gravità se non è a terra
                    if (!physics.IsGrounded)
                    {
                        // Gravità potenziata per gameplay migliore
                        physics.Velocity.y -= physics.Gravity * GRAVITY_MULTIPLIER * deltaTime;
                    }
                    
                    // Aggiorna la posizione in base alla velocità
                    transform.Position += physics.Velocity * deltaTime;
                    
                    // Ground check semplificato
                    if (transform.Position.y <= GROUND_LEVEL + GROUND_CHECK_DISTANCE)
                    {
                        // Il personaggio ha toccato terra
                        if (!physics.IsGrounded)
                        {
                            // Crea un evento di atterraggio
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new LandingEvent
                            {
                                PlayerEntity = entity,
                                LandingVelocity = physics.Velocity.y
                            });
                            
                            // Resetta i salti disponibili
                            movement.ResetJumps();
                        }
                        
                        // Correggi la posizione e imposta IsGrounded
                        transform.Position.y = GROUND_LEVEL;
                        physics.Velocity.y = 0;
                        physics.IsGrounded = true;
                    }
                    else
                    {
                        // Il personaggio è in aria
                        physics.IsGrounded = false;
                    }
                    
                    // Limita il movimento laterale alle corsie
                    transform.Position.x = math.clamp(transform.Position.x, -MAX_LANE_OFFSET, MAX_LANE_OFFSET);
                    
                }).ScheduleParallel();
            
            // Aggiorna le animazioni in base allo stato del movimento
            Entities
                .WithName("UpdateMovementAnimations")
                .WithAll<TagComponent>()
                .ForEach((Entity entity, int entityInQueryIndex,
                          in MovementComponent movement,
                          in PhysicsComponent physics) =>
                {
                    // Se abbiamo un componente di animazione, possiamo aggiornare gli stati
                    // Nota: questo è un placeholder, dipende dall'implementazione del sistema di animazione
                    
                    // Determina lo stato di animazione in base al movimento
                    MovementAnimationState animState = MovementAnimationState.Run;
                    
                    if (!physics.IsGrounded)
                    {
                        if (physics.Velocity.y > 0)
                            animState = MovementAnimationState.Jump;
                        else
                            animState = MovementAnimationState.Fall;
                    }
                    else if (movement.IsSliding)
                    {
                        animState = MovementAnimationState.Slide;
                    }
                    else if (!movement.IsMoving)
                    {
                        animState = MovementAnimationState.Idle;
                    }
                    
                    // Crea un evento per il sistema di animazione
                    var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                    commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new AnimationStateChangedEvent
                    {
                        PlayerEntity = entity,
                        State = animState,
                        Speed = math.length(physics.Velocity)
                    });
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito dopo che il job è completo
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
}