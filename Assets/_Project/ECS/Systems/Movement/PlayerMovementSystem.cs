// Path: Assets/_Project/ECS/Systems/Movement/PlayerMovementSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.Input;

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
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
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
            // Resto del metodo invariato
            float deltaTime = SystemAPI.Time.DeltaTime;
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
                    // Resto del codice invariato...
                }).ScheduleParallel();
            
            // Resto del metodo invariato...
            
            // Assicura che il command buffer venga eseguito dopo che il job è completo
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}