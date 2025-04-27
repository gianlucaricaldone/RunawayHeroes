using Unity.Entities;
using Unity.Mathematics;
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
    public partial class PlayerMovementSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
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
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora il movimento in avanti automatico e il movimento laterale basato sull'input
            Entities
                .WithName("PlayerMovementProcessor")
                .WithAll<TagComponent>() // Assumendo che ci sia un tag per identificare il giocatore
                .ForEach((Entity entity, int entityInQueryIndex, 
                          ref TransformComponent transform, 
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          in InputComponent input) => 
                {
                    // Se il giocatore non si sta muovendo ma dovrebbe
                    if (!movement.IsMoving && input.IsMovementEnabled)
                    {
                        movement.IsMoving = true;
                        
                        // Crea un evento di inizio movimento
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new MovementStartedEvent
                        {
                            PlayerEntity = entity
                        });
                    }
                    // Se il giocatore si sta muovendo ma dovrebbe fermarsi
                    else if (movement.IsMoving && !input.IsMovementEnabled)
                    {
                        movement.IsMoving = false;
                        
                        // Crea un evento di fine movimento
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new MovementStoppedEvent
                        {
                            PlayerEntity = entity
                        });
                    }
                    
                    // Calcola la velocità effettiva in base a stati speciali
                    float effectiveSpeed = movement.CurrentSpeed;
                    
                    // Modifica la velocità durante la scivolata
                    if (movement.IsSliding)
                    {
                        effectiveSpeed *= movement.SlideSpeedMultiplier;
                        
                        // Riduce il tempo rimanente della scivolata
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
                    
                    // Spostamento automatico in avanti (direzione Z)
                    if (movement.IsMoving)
                    {
                        // Accelerazione graduale fino alla velocità target
                        float targetSpeed = effectiveSpeed;
                        physics.Velocity.z = math.lerp(physics.Velocity.z, targetSpeed, movement.Acceleration * deltaTime);
                        
                        // Calcola la direzione di movimento
                        movement.MoveDirection = new float3(input.MoveDirection.x, 0, 1);
                        movement.MoveDirection = math.normalize(movement.MoveDirection);
                    }
                    
                    // Movimento laterale basato sull'input (direzione X)
                    float lateralSpeed = effectiveSpeed * 0.8f; // Leggermente più lento del movimento in avanti
                    float targetLateralSpeed = input.MoveDirection.x * lateralSpeed;
                    physics.Velocity.x = math.lerp(physics.Velocity.x, targetLateralSpeed, movement.Acceleration * deltaTime);
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito dopo che il job è completo
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}