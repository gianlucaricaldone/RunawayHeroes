// Path: Assets/_Project/ECS/Systems/Movement/PlayerMovementSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.Input;
using RunawayHeroes.ECS.Systems.Movement.Group;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce il movimento del giocatore in base all'input ricevuto.
    /// Elabora la corsa automatica, i movimenti laterali, e coordina con altri sistemi
    /// come salto e scivolata.
    /// </summary>
    [UpdateInGroup(typeof(RunawayHeroes.ECS.Systems.Movement.Group.MovementSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        // Costanti di configurazione
        private const float LANE_WIDTH = 3.0f;             // Larghezza della corsia
        private const float MAX_LANE_OFFSET = LANE_WIDTH;  // Offset massimo per corsia
        private const float GROUND_LEVEL = 0.0f;           // Livello del terreno
        private const float GRAVITY_MULTIPLIER = 1.5f;     // Moltiplicatore della gravità per salti più realistici
        private const float GROUND_CHECK_DISTANCE = 0.1f;  // Distanza per controllo contatto con suolo
        
        private EntityQuery _playerQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisce la query per identificare le entità giocatore
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TagComponent, InputComponent>()
                .WithAllRW<TransformComponent>()
                .WithAllRW<PhysicsComponent>()
                .WithAllRW<MovementComponent>()
                .Build(ref state);
            
            // Richiede almeno un giocatore per l'esecuzione
            state.RequireForUpdate(_playerQuery);
            
            // Richiede il singleton di EndSimulationEntityCommandBufferSystem per il CommandBuffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il tempo delta
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Processa l'input di salto
            new PlayerMovementJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                LaneWidth = LANE_WIDTH,
                MaxLaneOffset = MAX_LANE_OFFSET,
                GroundLevel = GROUND_LEVEL,
                GravityMultiplier = GRAVITY_MULTIPLIER,
                GroundCheckDistance = GROUND_CHECK_DISTANCE
            }.ScheduleParallel(state.Dependency).Complete();
        }
        
        [BurstCompile]
        private partial struct PlayerMovementJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            public float LaneWidth;
            public float MaxLaneOffset;
            public float GroundLevel;
            public float GravityMultiplier;
            public float GroundCheckDistance;
            
            // Definisci il metodo Execute che verrà chiamato per ogni entità corrispondente
            public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                    ref TransformComponent transform,
                    ref PhysicsComponent physics,
                    ref MovementComponent movement,
                    in InputComponent input)
            {
                // Implementa qui la logica di movimento del giocatore
                // (Questo è uno scheletro di base, la logica specifica dipenderà dall'implementazione originale)
                
                // Esempio di implementazione di base:
                // Gestione movimenti laterali
                float lateralMovement = input.LateralMovement;
                if (lateralMovement != 0)
                {
                    // Calcola nuova posizione laterale con limite alle corsie
                    float newXPosition = transform.Position.x + lateralMovement * movement.CurrentSpeed * DeltaTime;
                    newXPosition = math.clamp(newXPosition, -MaxLaneOffset, MaxLaneOffset);
                    transform.Position.x = newXPosition;
                }
                
                // Gestione movimento in avanti (corsa automatica)
                transform.Position.z += movement.CurrentSpeed * DeltaTime;
                
                // Gestione gravità e salto
                if (!physics.IsGrounded)
                {
                    // Applica gravità
                    physics.Velocity.y -= 9.81f * GravityMultiplier * DeltaTime;
                }
                
                // Aggiorna la posizione verticale
                transform.Position.y += physics.Velocity.y * DeltaTime;
                
                // Controllo contatto con il suolo
                if (transform.Position.y <= GroundLevel + GroundCheckDistance)
                {
                    transform.Position.y = GroundLevel;
                    physics.IsGrounded = true;
                    physics.Velocity.y = 0;
                    
                    // Resetta i salti disponibili se a terra
                    if (movement.IsJumping)
                    {
                        movement.IsJumping = false;
                        movement.ResetJumps();
                    }
                }
            }
        }
    }
}