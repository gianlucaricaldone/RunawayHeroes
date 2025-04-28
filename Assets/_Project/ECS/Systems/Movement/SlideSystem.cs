using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce la meccanica di scivolata del giocatore.
    /// Si occupa di avviare le scivolate in risposta all'input, gestire la durata
    /// e gli effetti collaterali come l'altezza ridotta per passare sotto gli ostacoli.
    /// </summary>
    public partial class SlideSystem : SystemBase
    {
        private EntityQuery _slidableEntitiesQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            // Riferimento al command buffer system per generare eventi
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Query per trovare entità che possono scivolare
            _slidableEntitiesQuery = GetEntityQuery(
                ComponentType.ReadWrite<PhysicsComponent>(),
                ComponentType.ReadWrite<MovementComponent>(),
                ComponentType.ReadOnly<SlideInputComponent>()
            );
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            RequireForUpdate(_slidableEntitiesQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora gli input di scivolata
            Entities
                .WithName("SlideProcessor")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref PhysicsComponent physics,
                          ref MovementComponent movement,
                          ref TransformComponent transform,
                          in SlideInputComponent slideInput) => 
                {
                    // Se è stata richiesta una scivolata e il personaggio può scivolare
                    if (slideInput.SlideRequested && !movement.IsSliding && physics.IsGrounded && movement.IsMoving)
                    {
                        // Avvia la scivolata
                        movement.IsSliding = true;
                        movement.SlideTimeRemaining = movement.SlideDuration;
                        
                        // Riduce temporaneamente l'altezza del personaggio per passare sotto ostacoli
                        // Questo verrebbe gestito da un componente separato in un'implementazione più completa
                        transform.Scale *= 0.5f; // Riduce l'altezza a metà
                        
                        // Applica un impulso in avanti per dare il senso di slancio
                        float3 slideDirection = new float3(0, 0, 1); // Assume che Z sia "avanti"
                        physics.Velocity += slideDirection * 2.0f; // Boost iniziale
                        
                        // Genera un evento di inizio scivolata
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new SlideStartedEvent
                        {
                            EntitySliding = entity,
                            SlideDuration = movement.SlideDuration
                        });
                    }
                    
                    // Gestisce la scivolata in corso
                    if (movement.IsSliding)
                    {
                        // Diminuisci il tempo rimanente
                        movement.SlideTimeRemaining -= deltaTime;
                        
                        // Se il tempo è scaduto o la scivolata è stata interrotta
                        if (movement.SlideTimeRemaining <= 0 || slideInput.SlideInterrupted)
                        {
                            // Termina la scivolata
                            movement.IsSliding = false;
                            movement.SlideTimeRemaining = 0;
                            
                            // Ripristina l'altezza normale
                            transform.Scale *= 2.0f; // Riporta all'altezza originale
                            
                            // Genera un evento di fine scivolata
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new SlideEndedEvent
                            {
                                EntitySliding = entity
                            });
                        }
                        
                        // Durante la scivolata, il personaggio è più resistente alle collisioni frontali
                        // Questa logica sarebbe parte di un sistema di collisione più avanzato
                    }
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}