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
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RoomConnectionSystem : ISystem
    {
        #region Private Fields
        
        private EntityQuery _roomsQuery;
        private EntityQuery _unconnectedRoomsQuery;
        
        #endregion
        
        #region Initialization
        
        public void OnCreate(ref SystemState state)
        {
            // Configura le query necessarie
            _roomsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RoomComponent>(),
                ComponentType.ReadWrite<DynamicBuffer<RoomDoorwayBuffer>>()
            );
            
            _unconnectedRoomsQuery = state.GetEntityQuery(
                ComponentType.ReadOnly<RequiresConnectionTag>(),
                ComponentType.ReadOnly<RoomComponent>(),
                ComponentType.ReadWrite<DynamicBuffer<RoomDoorwayBuffer>>()
            );
            
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi che il sistema venga eseguito solo durante la generazione del livello
            state.RequireForUpdate<RoomComponent>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa specifica da pulire
        }
        
        #endregion
        
        #region System Lifecycle
        
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Primo passaggio: identifica le stanze non collegate
            FindUnconnectedRooms(ref state, commandBuffer);
            
            // Secondo passaggio: collega le stanze che richiedono un collegamento
            ConnectRooms(ref state, commandBuffer);
        }
        
        #endregion
        
        #region Room Connection Logic
        
        /// <summary>
        /// Identifica le stanze che non hanno connessioni e le marca per la connessione
        /// </summary>
        private void FindUnconnectedRooms(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            var entityManager = state.EntityManager;
            
            foreach (var (doorways, entity) in 
                     SystemAPI.Query<DynamicBuffer<RoomDoorwayBuffer>>()
                     .WithAll<RoomComponent>()
                     .WithEntityAccess())
            {
                // Se la stanza non ha doorway, aggiungi un tag per la successiva elaborazione
                if (doorways.Length == 0)
                {
                    commandBuffer.AddComponent(entity, new RequiresConnectionTag());
                }
            }
        }
        
        /// <summary>
        /// Collega le stanze che richiedono una connessione
        /// </summary>
        private void ConnectRooms(ref SystemState state, EntityCommandBuffer commandBuffer)
        {
            var entityManager = state.EntityManager;
            
            // Ottieni tutte le stanze non collegate
            var unconnectedRooms = _unconnectedRoomsQuery.ToEntityArray(Allocator.Temp);
            
            foreach (var roomEntity in unconnectedRooms)
            {
                // Ottieni i dati della stanza
                var room = entityManager.GetComponentData<RoomComponent>(roomEntity);
                var doorways = entityManager.GetBuffer<RoomDoorwayBuffer>(roomEntity);
                
                // Trova la stanza più vicina per effettuare una connessione
                // (Questo richiederebbe un sistema di query più complesso 
                //  per trovare le stanze vicine, qui semplificato)
                
                // Crea una doorway
                CreateDoorway(roomEntity, doorways, DoorwayDirection.North, commandBuffer);
                
                // Rimuovi il tag di richiesta connessione
                commandBuffer.RemoveComponent<RequiresConnectionTag>(roomEntity);
            }
            
            // Assicurati di eliminare l'array temporaneo
            unconnectedRooms.Dispose();
        }
        
        /// <summary>
        /// Crea una nuova doorway per una stanza nella direzione specificata
        /// </summary>
        private void CreateDoorway(Entity roomEntity, 
                                  DynamicBuffer<RoomDoorwayBuffer> doorways,
                                  DoorwayDirection direction,
                                  EntityCommandBuffer commandBuffer)
        {
            // Crea l'entità doorway
            Entity doorwayEntity = commandBuffer.CreateEntity();
            
            // Aggiungi il componente doorway
            commandBuffer.AddComponent(doorwayEntity, new DoorwayComponent
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
            commandBuffer.AddComponent(doorwayEntity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            // Aggiungi la doorway al buffer della stanza
            doorways.Add(new RoomDoorwayBuffer { DoorwayEntity = doorwayEntity });
        }
        
        #endregion
    }
    
    /// <summary>
    /// Tag component per identificare le stanze che richiedono una connessione
    /// </summary>
    public struct RequiresConnectionTag : IComponentData { }
}