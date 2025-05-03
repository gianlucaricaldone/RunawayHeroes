using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.World;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce la generazione procedurale di livelli randomizzati
    /// in base ai parametri definiti in RandomLevelConfigComponent
    /// </summary>
    public partial struct RandomLevelGenerationSystem : ISystem
    {
        private EntityQuery _configQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Prepara la query per le configurazioni di livello
            _configQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RandomLevelConfigComponent, LocalTransform>()
                .Build(ref state);
                
            // Richiedi che il sistema venga eseguito solo se esiste almeno un'entità 
            // con RandomLevelConfigComponent
            state.RequireForUpdate(_configQuery);
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Pulizia risorse se necessario
        }

        public void OnUpdate(ref SystemState state)
        {
            // Command buffer per operazioni di creazione/modifica entità
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Esegui il job per processare tutte le configurazioni di livelli
            state.Dependency = new ProcessRandomLevelConfigsJob
            {
                ECB = commandBuffer
            }.ScheduleParallel(_configQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per processare le configurazioni di livelli casuali
    /// </summary>
    [Unity.Burst.BurstCompile]
    public partial struct ProcessRandomLevelConfigsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [Unity.Burst.BurstDiscard]
        public void Execute(Entity entity, 
                        [ChunkIndexInQuery] int entityInQueryIndex,
                        in RandomLevelConfigComponent config, 
                        in LocalTransform transform)
        {
            // Inizializza il generatore casuale con il seed fornito
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)config.Seed);
            
            // Genera la struttura del livello
            GenerateLevel(entity, entityInQueryIndex, config, transform, random, ref ECB);
            
            // Rimuovi il componente di configurazione per evitare di rigenerare
            ECB.RemoveComponent<RandomLevelConfigComponent>(entityInQueryIndex, entity);
        }
        
        /// <summary>
        /// Genera un livello procedurale basato sulla configurazione specificata
        /// </summary>
        [Unity.Burst.BurstDiscard]
        private void GenerateLevel(Entity configEntity, int entityInQueryIndex, 
                                RandomLevelConfigComponent config, 
                                LocalTransform transform,
                                Unity.Mathematics.Random random,
                                ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Determina il numero di stanze da generare
            int numRooms = random.NextInt(config.MinRooms, config.MaxRooms + 1);
            
            // Crea l'entità principale del livello
            Entity levelEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, new LevelComponent());
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, transform);
            
            // Preparazione algoritmo BSP (Binary Space Partitioning) o altro algoritmo
            // di generazione procedurale
            // ...
            
            // Genera la struttura principale del livello
            // ...
            
            // Posiziona la stanza iniziale
            Entity startRoom = CreateRoom(entityInQueryIndex, RoomType.Standard, numRooms > 0 ? true : false, false, 
                                       levelEntity, ref commandBuffer);
                                       
            // Collega le stanze con corridoi e doorway
            // ...
            
            // Posiziona stanze speciali (boss, tesoro, ecc.)
            // ...
            
            // Popola le stanze con nemici, oggetti, ostacoli in base alla difficoltà
            // ...
            
            // Aggiungi dettagli tematici in base al WorldTheme
            // ...
        }
        
        /// <summary>
        /// Crea una nuova stanza nell'ambito del livello generato
        /// </summary>
        [Unity.Burst.BurstDiscard]
        private Entity CreateRoom(int entityInQueryIndex, RoomType type, bool isStartRoom, bool isEndRoom, 
                              Entity levelEntity, ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Crea l'entità stanza
            Entity roomEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base
            commandBuffer.AddComponent(entityInQueryIndex, roomEntity, new RoomComponent
            {
                Position = float3.zero, // Sarà posizionata correttamente in seguito
                Rotation = quaternion.identity,
                GridPosition = int2.zero, // Sarà impostata in seguito
                Size = new int2(1, 1), // Dimensione base
                TemplateID = 0, // Template base
                LevelEntity = levelEntity,
                State = isStartRoom ? RoomState.Active : RoomState.Inactive,
                IsVisited = isStartRoom,
                IsMapped = isStartRoom,
                ContainsCollectibles = false,
                ContainsEnemies = false
            });
            
            // Aggiungi il buffer di doorway
            commandBuffer.AddBuffer<RoomDoorwayBuffer>(entityInQueryIndex, roomEntity);
            
            // Aggiungi il componente transform
            commandBuffer.AddComponent(entityInQueryIndex, roomEntity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            return roomEntity;
        }
    }
}