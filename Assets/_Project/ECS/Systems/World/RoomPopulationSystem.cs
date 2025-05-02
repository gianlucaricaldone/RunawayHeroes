using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Gameplay;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile del popolamento delle stanze con nemici, collezionabili e ostacoli
    /// </summary>
    public partial class RoomPopulationSystem : SystemBase
    {
        private EntityCommandBufferSystem _commandBufferSystem;
        private Random _random;
        private uint _seed;
        
        protected override void OnCreate()
        {
            // Ottieni il sistema di command buffer
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Inizializza il generatore di numeri casuali
            _seed = (uint)DateTime.Now.Ticks;
            _random = Random.CreateFromIndex(_seed);
            
            // Richiedi che il sistema venga eseguito solo durante la generazione del livello
            RequireForUpdate<RoomComponent>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            var random = _random;
            
            // Popola le stanze appena create
            Entities
                .WithName("PopulateRooms")
                .WithNone<RoomPopulatedTag>()
                .ForEach((Entity roomEntity, int entityInQueryIndex,
                         ref RoomComponent room) =>
                {
                    // Popola la stanza in base al tipo
                    switch (room.State)
                    {
                        case RoomState.Active:
                            // La stanza iniziale ha meno nemici
                            PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.2f, 
                                       0.5f, 0.3f, ref commandBuffer, random);
                            break;
                            
                        case RoomState.Inactive:
                            // Stanze standard con più nemici
                            PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.6f, 
                                       0.4f, 0.5f, ref commandBuffer, random);
                            break;
                            
                        case RoomState.Secret:
                            // Stanze segrete con più collezionabili
                            PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.2f, 
                                       0.8f, 0.3f, ref commandBuffer, random);
                            break;
                    }
                    
                    // Aggiungi tag per evitare di ripopolare
                    commandBuffer.AddComponent(entityInQueryIndex, roomEntity, 
                                             new RoomPopulatedTag { });
                    
                }).ScheduleParallel();
            
            // Assicurati che i comandi vengano eseguiti
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Popola una stanza con nemici, collezionabili e ostacoli in base alle probabilità specificate
        /// </summary>
        private void PopulateRoom(int entityInQueryIndex, Entity roomEntity, 
                                ref RoomComponent room,
                                float enemyProbability, 
                                float collectibleProbability,
                                float obstacleProbability,
                                ref EntityCommandBuffer.ParallelWriter commandBuffer,
                                Random random)
        {
            // Calcola il numero di entità da generare in base alla dimensione della stanza
            int roomArea = room.Size.x * room.Size.y;
            int maxEntities = Math.Max(1, roomArea / 4);  // Approssimativo, 1 entità ogni 4 unità di area
            
            // Genera nemici
            int numEnemies = random.NextInt(0, (int)(maxEntities * enemyProbability) + 1);
            if (numEnemies > 0)
            {
                room.ContainsEnemies = true;
                
                for (int i = 0; i < numEnemies; i++)
                {
                    CreateEnemy(entityInQueryIndex, roomEntity, ref room, ref commandBuffer, random);
                }
            }
            
            // Genera collezionabili
            int numCollectibles = random.NextInt(0, (int)(maxEntities * collectibleProbability) + 1);
            if (numCollectibles > 0)
            {
                room.ContainsCollectibles = true;
                
                for (int i = 0; i < numCollectibles; i++)
                {
                    CreateCollectible(entityInQueryIndex, roomEntity, ref room, ref commandBuffer, random);
                }
            }
            
            // Genera ostacoli
            int numObstacles = random.NextInt(0, (int)(maxEntities * obstacleProbability) + 1);
            for (int i = 0; i < numObstacles; i++)
            {
                CreateObstacle(entityInQueryIndex, roomEntity, ref room, ref commandBuffer, random);
            }
        }
        
        /// <summary>
        /// Crea un nemico nella stanza specificata
        /// </summary>
        private void CreateEnemy(int entityInQueryIndex, Entity roomEntity, 
                               ref RoomComponent room,
                               ref EntityCommandBuffer.ParallelWriter commandBuffer,
                               Random random)
        {
            // Crea l'entità nemico
            Entity enemyEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un nemico
            commandBuffer.AddComponent(entityInQueryIndex, enemyEntity, new EnemyComponent
            {
                // Proprietà specifiche del nemico
            });
            
            // Calcola una posizione casuale all'interno della stanza
            float3 randomPosition = room.Position + new float3(
                random.NextFloat(-room.Size.x/2f, room.Size.x/2f),
                0,
                random.NextFloat(-room.Size.y/2f, room.Size.y/2f)
            );
            
            // Aggiungi il componente transform
            commandBuffer.AddComponent(entityInQueryIndex, enemyEntity, new LocalTransform
            {
                Position = randomPosition,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
        }
        
        /// <summary>
        /// Crea un oggetto collezionabile nella stanza specificata
        /// </summary>
        private void CreateCollectible(int entityInQueryIndex, Entity roomEntity, 
                                     ref RoomComponent room,
                                     ref EntityCommandBuffer.ParallelWriter commandBuffer,
                                     Random random)
        {
            // Crea l'entità collezionabile
            Entity collectibleEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un collezionabile
            commandBuffer.AddComponent(entityInQueryIndex, collectibleEntity, new CollectibleComponent
            {
                // Proprietà specifiche dell'oggetto collezionabile
            });
            
            // Calcola una posizione casuale all'interno della stanza
            float3 randomPosition = room.Position + new float3(
                random.NextFloat(-room.Size.x/2f, room.Size.x/2f),
                0,
                random.NextFloat(-room.Size.y/2f, room.Size.y/2f)
            );
            
            // Aggiungi il componente transform
            commandBuffer.AddComponent(entityInQueryIndex, collectibleEntity, new LocalTransform
            {
                Position = randomPosition,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
        }
        
        /// <summary>
        /// Crea un ostacolo nella stanza specificata
        /// </summary>
        private void CreateObstacle(int entityInQueryIndex, Entity roomEntity, 
                                  ref RoomComponent room,
                                  ref EntityCommandBuffer.ParallelWriter commandBuffer,
                                  Random random)
        {
            // Crea l'entità ostacolo
            Entity obstacleEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un ostacolo
            commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new ObstacleComponent
            {
                // Proprietà specifiche dell'ostacolo
            });
            
            // Calcola una posizione casuale all'interno della stanza
            float3 randomPosition = room.Position + new float3(
                random.NextFloat(-room.Size.x/2f, room.Size.x/2f),
                0,
                random.NextFloat(-room.Size.y/2f, room.Size.y/2f)
            );
            
            // Aggiungi il componente transform
            commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new LocalTransform
            {
                Position = randomPosition,
                Rotation = quaternion.EulerZXY(0, random.NextFloat(0, math.PI * 2), 0),
                Scale = random.NextFloat(0.8f, 1.2f)  // Scala leggermente variabile
            });
        }
    }
    
    /// <summary>
    /// Tag component per identificare le stanze già popolate
    /// </summary>
    public struct RoomPopulatedTag : IComponentData { }
}