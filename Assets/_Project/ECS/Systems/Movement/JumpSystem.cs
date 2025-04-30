using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
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
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct JumpSystem : ISystem
    {
        private EntityQuery _jumpableEntitiesQuery;
        
        // Rimuovo l'attributo BurstCompile dal metodo OnCreate poiché contiene creazione di array gestiti
        public void OnCreate(ref SystemState state)
        {
            // Query per trovare entità che possono saltare usando EntityQueryBuilder
            _jumpableEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<JumpInputComponent, TransformComponent>()
                .WithAllRW<PhysicsComponent, MovementComponent>()
                .Build(ref state);
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_jumpableEntitiesQuery);
            
            // Richiede il singleton di EndSimulationEntityCommandBufferSystem per eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Elabora gli input di salto
            new JumpProcessorJob
            {
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_jumpableEntitiesQuery, state.Dependency).Complete();
        }
        
        [BurstCompile]
        private partial struct JumpProcessorJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                               ref PhysicsComponent physics,
                               ref MovementComponent movement,
                               in TransformComponent transform,
                               in JumpInputComponent jumpInput)
            {
                // Se è stato richiesto un salto e il personaggio può saltare
                if (jumpInput.JumpPressed && movement.RemainingJumps > 0)
                {
                    // Attiva lo stato di salto
                    movement.IsJumping = true;
                    movement.RemainingJumps--;
                    
                    // Applica la forza di salto direttamente alla velocità verticale
                    physics.Velocity.y = movement.JumpForce;
                    
                    // Imposta lo stato "non a terra"
                    physics.IsGrounded = false;
                    
                    // Genera un evento di salto
                    var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, eventEntity, new JumpStartedEvent
                    {
                        PlayerEntity = entity,
                        JumpForce = movement.JumpForce,
                        RemainingJumps = movement.RemainingJumps,
                        JumpPosition = transform.Position
                    });
                }
                
                // Se il personaggio atterra dopo un salto
                if (physics.IsGrounded && movement.IsJumping)
                {
                    // Resetta lo stato di salto
                    movement.IsJumping = false;
                    movement.ResetJumps(); // Ripristina i salti disponibili
                    
                    // Genera un evento di atterraggio
                    var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, eventEntity, new LandingEvent
                    {
                        PlayerEntity = entity,
                        LandingVelocity = physics.Velocity.y,
                        LandingPosition = transform.Position,
                        IsHardLanding = physics.Velocity.y < -10.0f // Esempio di logica per atterraggio duro
                    });
                }
                
                // Gestisce la fisica del salto variabile (salto più alto se il tasto è tenuto premuto)
                // Assumiamo che non esista JumpHeld, quindi rimuoviamo questa logica
                // Se in futuro dovesse essere necessaria, può essere reimplementata una volta definita la proprietà corretta
                /*
                if (movement.IsJumping && physics.Velocity.y > 0 && !jumpInput.JumpHeld)
                {
                    // Riduce la velocità di salto se il pulsante è rilasciato a metà salto
                    // per consentire salti di altezza variabile
                    physics.Velocity.y *= 0.6f;
                }
                */
            }
        }
    }
}