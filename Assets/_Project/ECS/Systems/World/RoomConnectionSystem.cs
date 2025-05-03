using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.World;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che gestisce la creazione e il collegamento di doorway tra le stanze
    /// </summary>
    public partial class RoomConnectionSystem : SystemBase
    {
        protected override void OnCreate()
        {
            // Richiedi il singleton per il command buffer
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi che il sistema venga eseguito solo durante la generazione del livello
            RequireForUpdate<RoomComponent>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(World.Unmanaged).AsParallelWriter();
            
            // Primo passaggio: identifica le stanze non collegate
            Entities
                .WithName("FindUnconnectedRooms")
                .WithAll<RoomComponent>()
                .ForEach((Entity roomEntity, int entityInQueryIndex,
                         DynamicBuffer<RoomDoorwayBuffer> doorways) =>
                {
                    // Se la stanza non ha doorway, aggiungi un tag per la successiva elaborazione
                    if (doorways.Length == 0)
                    {
                        commandBuffer.AddComponent(entityInQueryIndex, roomEntity, 
                                                 new RequiresConnectionTag { });
                    }
                    
                }).ScheduleParallel();
            
            // Secondo passaggio: collega le stanze che richiedono un collegamento
            Entities
                .WithName("ConnectRooms")
                .WithAll<RequiresConnectionTag>()
                .WithoutBurst()
                .Run((Entity roomEntity, int entityInQueryIndex,
                     ref RoomComponent room,
                     ref DynamicBuffer<RoomDoorwayBuffer> doorways) =>
                {
                    // Trova la stanza più vicina per effettuare una connessione
                    // (Questo richiederebbe un sistema di query più complesso 
                    //  per trovare le stanze vicine, qui semplificato)
                    
                    // Crea una doorway
                    CreateDoorway(entityInQueryIndex, roomEntity, doorways, DoorwayDirection.North, 
                                 ref commandBuffer);
                    
                    // Rimuovi il tag di richiesta connessione
                    commandBuffer.RemoveComponent<RequiresConnectionTag>(entityInQueryIndex, roomEntity);
                });
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
        }
        
        /// <summary>
        /// Crea una nuova doorway per una stanza nella direzione specificata
        /// </summary>
        private void CreateDoorway(int entityInQueryIndex, Entity roomEntity, 
                                  DynamicBuffer<RoomDoorwayBuffer> doorways,
                                  DoorwayDirection direction,
                                  ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Crea l'entità doorway
            Entity doorwayEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi il componente doorway
            commandBuffer.AddComponent(entityInQueryIndex, doorwayEntity, new DoorwayComponent
            {
                Position = float3.zero, // Sarà impostato in base alla posizione della stanza
                Rotation = quaternion.identity, // Sarà impostato in base alla direzione
                Direction = direction,
                SourceRoom = roomEntity,
                TargetRoom = Entity.Null, // Non ancora collegata
                Type = DoorwayType.Standard,
                IsConnected = false,
                IsLocked = false,
                KeyID = 0
            });
            
            // Aggiungi il componente transform
            commandBuffer.AddComponent(entityInQueryIndex, doorwayEntity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            // Aggiungi la doorway al buffer della stanza
            doorways.Add(new RoomDoorwayBuffer { DoorwayEntity = doorwayEntity });
        }
    }
    
    /// <summary>
    /// Tag component per identificare le stanze che richiedono una connessione
    /// </summary>
    public struct RequiresConnectionTag : IComponentData { }
}