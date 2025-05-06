using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.Movement.Group;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce la meccanica di scivolata del giocatore.
    /// Si occupa di avviare le scivolate in risposta all'input, gestire la durata
    /// e gli effetti collaterali come l'altezza ridotta per passare sotto gli ostacoli.
    /// </summary>
    [UpdateInGroup(typeof(RunawayHeroes.ECS.Systems.Movement.Group.MovementSystemGroup))]
    public partial struct SlideSystem : ISystem
    {
        private EntityQuery _slidableEntitiesQuery;
        
        // Rimuovi l'attributo [BurstCompile] dal metodo OnCreate
        public void OnCreate(ref SystemState state)
        {
            // Query per trovare entità che possono scivolare
            _slidableEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SlideInputComponent,TransformComponent>()
                .WithAllRW<PhysicsComponent, MovementComponent>()
                .Build(ref state);
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_slidableEntitiesQuery);
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Elabora gli input di scivolata
            new SlideProcessorJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_slidableEntitiesQuery, state.Dependency).Complete();
        }
        
        [BurstCompile]
        private partial struct SlideProcessorJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                               ref PhysicsComponent physics,
                               ref MovementComponent movement,
                               ref TransformComponent transform,
                               in SlideInputComponent slideInput)
            {
                // Se è stata richiesta una scivolata e il personaggio può scivolare
                if (slideInput.SlidePressed && !movement.IsSliding && physics.IsGrounded && movement.IsMoving)
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
                    var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, eventEntity, new SlideStartedEvent
                    {
                        PlayerEntity = entity,
                        Duration = movement.SlideDuration,
                        StartPosition = transform.Position,
                        InitialSpeed = math.length(physics.Velocity)
                    });
                }
                
                // Gestisce la scivolata in corso
                if (movement.IsSliding)
                {
                    // Diminuisci il tempo rimanente
                    movement.SlideTimeRemaining -= DeltaTime;
                    
                    // Se il tempo è scaduto o la scivolata è stata interrotta
                    if (movement.SlideTimeRemaining <= 0)
                    {
                        // Termina la scivolata
                        movement.IsSliding = false;
                        movement.SlideTimeRemaining = 0;
                        
                        // Ripristina l'altezza normale
                        transform.Scale *= 2.0f; // Riporta all'altezza originale
                        
                        // Genera un evento di fine scivolata
                        var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, eventEntity, new SlideEndedEvent
                        {
                            PlayerEntity = entity,
                            EndPosition = transform.Position,
                            SlideDistance = 0, // Andrebbe calcolata rispetto alla posizione iniziale
                            ActualDuration = movement.SlideDuration // Andrebbe calcolata come differenza
                        });
                    }
                    
                    // Durante la scivolata, il personaggio è più resistente alle collisioni frontali
                    // Questa logica sarebbe parte di un sistema di collisione più avanzato
                }
            }
        }
    }
}