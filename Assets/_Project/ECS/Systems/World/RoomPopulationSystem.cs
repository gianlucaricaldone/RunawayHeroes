using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Gameplay;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile del popolamento delle stanze con nemici, collezionabili e ostacoli
    /// </summary>
    public partial struct RoomPopulationSystem : ISystem
    {
        private uint _seed;
        private EntityQuery _roomQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Inizializza il generatore di numeri casuali
            _seed = (uint)SystemAPI.Time.ElapsedTime * 1000000 + 654321u; // Usa SystemAPI.Time.ElapsedTime per compatibilità con Burst
            
            // Configura query per le stanze non ancora popolate
            _roomQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RoomComponent>()
                .WithNone<RoomPopulatedTag>()
                .Build(ref state);
            
            // Richiedi che il sistema venga eseguito solo durante la generazione del livello
            state.RequireForUpdate<RoomComponent>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Risorse da ripulire, se necessario
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Popola le stanze appena create usando IJobEntity
            state.Dependency = new PopulateRoomsJob
            {
                ECB = commandBuffer,
                Seed = _seed
            }.ScheduleParallel(_roomQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per popolare le stanze con contenuto
    /// </summary>
    [BurstCompile]
    public partial struct PopulateRoomsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint Seed;
        
        [BurstDiscard]
        public void Execute(Entity roomEntity, 
                         [EntityIndexInQuery] int entityInQueryIndex,
                         ref RoomComponent room)
        {
            // Crea un generatore di numeri casuali unico per questa stanza
            uint roomSeed = Seed + (uint)(room.GridPosition.x * 1000 + room.GridPosition.y);
            var random = Unity.Mathematics.Random.CreateFromIndex(roomSeed);
            
            // Popola la stanza in base al tipo
            switch (room.State)
            {
                case RoomState.Active:
                    // La stanza iniziale ha meno nemici
                    PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.2f, 
                               0.5f, 0.3f, random);
                    break;
                    
                case RoomState.Inactive:
                    // Stanze standard con più nemici
                    PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.6f, 
                               0.4f, 0.5f, random);
                    break;
                    
                case RoomState.Secret:
                    // Stanze segrete con più collezionabili
                    PopulateRoom(entityInQueryIndex, roomEntity, ref room, 0.2f, 
                               0.8f, 0.3f, random);
                    break;
            }
            
            // Aggiungi tag per evitare di ripopolare
            ECB.AddComponent(entityInQueryIndex, roomEntity, new RoomPopulatedTag { });
        }
        
        /// <summary>
        /// Popola una stanza con nemici, collezionabili e ostacoli in base alle probabilità specificate
        /// </summary>
        [BurstDiscard]
        private void PopulateRoom(int entityInQueryIndex, Entity roomEntity, 
                                ref RoomComponent room,
                                float enemyProbability, 
                                float collectibleProbability,
                                float obstacleProbability,
                                Unity.Mathematics.Random random)
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
                    CreateEnemy(entityInQueryIndex, roomEntity, ref room, random);
                }
            }
            
            // Genera collezionabili
            int numCollectibles = random.NextInt(0, (int)(maxEntities * collectibleProbability) + 1);
            if (numCollectibles > 0)
            {
                room.ContainsCollectibles = true;
                
                for (int i = 0; i < numCollectibles; i++)
                {
                    CreateCollectible(entityInQueryIndex, roomEntity, ref room, random);
                }
            }
            
            // Genera ostacoli
            int numObstacles = random.NextInt(0, (int)(maxEntities * obstacleProbability) + 1);
            for (int i = 0; i < numObstacles; i++)
            {
                CreateObstacle(entityInQueryIndex, roomEntity, ref room, random);
            }
        }
        
        /// <summary>
        /// Crea un nemico nella stanza specificata
        /// </summary>
        [BurstDiscard]
        private void CreateEnemy(int entityInQueryIndex, Entity roomEntity, 
                               ref RoomComponent room,
                               Unity.Mathematics.Random random)
        {
            // Crea l'entità nemico
            Entity enemyEntity = ECB.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un nemico
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new EnemyComponent
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
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new LocalTransform
            {
                Position = randomPosition,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
        }
        
        /// <summary>
        /// Crea un oggetto collezionabile nella stanza specificata
        /// </summary>
        [BurstDiscard]
        private void CreateCollectible(int entityInQueryIndex, Entity roomEntity, 
                                     ref RoomComponent room,
                                     Unity.Mathematics.Random random)
        {
            // Crea l'entità collezionabile
            Entity collectibleEntity = ECB.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un collezionabile
            ECB.AddComponent(entityInQueryIndex, collectibleEntity, new CollectibleComponent
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
            ECB.AddComponent(entityInQueryIndex, collectibleEntity, new LocalTransform
            {
                Position = randomPosition,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
        }
        
        /// <summary>
        /// Crea un ostacolo nella stanza specificata
        /// </summary>
        [BurstDiscard]
        private void CreateObstacle(int entityInQueryIndex, Entity roomEntity, 
                                  ref RoomComponent room,
                                  Unity.Mathematics.Random random)
        {
            // Crea l'entità ostacolo
            Entity obstacleEntity = ECB.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base per un ostacolo
            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new ObstacleComponent
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
            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new LocalTransform
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