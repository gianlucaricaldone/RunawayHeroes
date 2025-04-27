using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce il salto del giocatore in risposta all'input.
    /// Si occupa di avviare i salti, gestire i salti multipli e coordinare
    /// con il sistema di fisica per applicare le forze appropriate.
    /// </summary>
    public partial class JumpSystem : SystemBase
    {
        private EntityQuery _jumpableEntitiesQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            // Riferimento al command buffer system per generare eventi
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Query per trovare entità che possono saltare
            _jumpableEntitiesQuery = GetEntityQuery(
                ComponentType.ReadWrite<PhysicsComponent>(),
                ComponentType.ReadWrite<MovementComponent>(),
                ComponentType.ReadOnly<JumpInputComponent>()
            );
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            RequireForUpdate(_jumpableEntitiesQuery);
        }
        
        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora gli input di salto
            Entities
                .WithName("JumpProcessor")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          in JumpInputComponent jumpInput) => 
                {
                    // Se è stato richiesto un salto e il personaggio può saltare
                    if (jumpInput.JumpRequested && movement.RemainingJumps > 0)
                    {
                        // Attiva lo stato di salto
                        movement.IsJumping = true;
                        movement.RemainingJumps--;
                        
                        // Applica la forza di salto direttamente alla velocità verticale
                        physics.Velocity.y = movement.JumpForce;
                        
                        // Imposta lo stato "non a terra"
                        physics.IsGrounded = false;
                        
                        // Genera un evento di salto
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new JumpEvent
                        {
                            EntityJumped = entity,
                            JumpForce = movement.JumpForce,
                            JumpsRemaining = movement.RemainingJumps
                        });
                    }
                    
                    // Se il personaggio atterra dopo un salto
                    if (physics.IsGrounded && movement.IsJumping)
                    {
                        // Resetta lo stato di salto
                        movement.IsJumping = false;
                        movement.ResetJumps(); // Ripristina i salti disponibili
                        
                        // Genera un evento di atterraggio
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new LandedEvent
                        {
                            EntityLanded = entity,
                            LandingVelocity = physics.Velocity.y
                        });
                    }
                    
                    // Gestisce la fisica del salto variabile (salto più alto se il tasto è tenuto premuto)
                    if (movement.IsJumping && physics.Velocity.y > 0 && !jumpInput.JumpHeld)
                    {
                        // Riduce la velocità di salto se il pulsante è rilasciato a metà salto
                        // per consentire salti di altezza variabile
                        physics.Velocity.y *= 0.6f;
                    }
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}