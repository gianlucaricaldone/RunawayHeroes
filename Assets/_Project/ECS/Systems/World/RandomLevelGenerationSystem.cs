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
    public partial class RandomLevelGenerationSystem : SystemBase
    {
        private EntityCommandBufferSystem _commandBufferSystem;
        private Unity.Mathematics.Random _random;
        
        protected override void OnCreate()
        {
            // Ottieni il sistema di command buffer per la creazione/distruzione di entità
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi che il sistema venga eseguito solo se esiste almeno un'entità 
            // con RandomLevelConfigComponent
            RequireForUpdate<RandomLevelConfigComponent>();
        }

        protected override void OnUpdate()
        {
            // Command buffer per operazioni di creazione/modifica entità
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni tutte le configurazioni di livelli casuali che non sono ancora state elaborate
            Entities
                .WithName("ProcessRandomLevelConfigs")
                .ForEach((Entity entity, int entityInQueryIndex, 
                         in RandomLevelConfigComponent config,
                         in LocalTransform transform) =>
                {
                    // Inizializza il generatore casuale con il seed fornito
                    _random = Unity.Mathematics.Random.CreateFromIndex((uint)config.Seed);
                    
                    // Genera la struttura del livello con Binary Space Partitioning
                    // o un altro algoritmo di generazione procedurale
                    GenerateLevel(entity, entityInQueryIndex, config, transform, ref commandBuffer);
                    
                    // Rimuovi il componente di configurazione per evitare di rigenerare
                    commandBuffer.RemoveComponent<RandomLevelConfigComponent>(entityInQueryIndex, entity);
                    
                }).ScheduleParallel();
            
            // Assicurati che i comandi vengano eseguiti
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Genera un livello procedurale basato sulla configurazione specificata
        /// </summary>
        private void GenerateLevel(Entity configEntity, int entityInQueryIndex, 
                                 RandomLevelConfigComponent config, 
                                 LocalTransform transform,
                                 ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Determina il numero di stanze da generare
            int numRooms = _random.NextInt(config.MinRooms, config.MaxRooms + 1);
            
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