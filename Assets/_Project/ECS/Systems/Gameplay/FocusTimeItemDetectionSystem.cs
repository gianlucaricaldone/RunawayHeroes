// File: Assets/_Project/ECS/Systems/Gameplay/FocusTimeItemDetectionSystem.cs

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che rileva oggetti collezionabili nel raggio del giocatore durante il Focus Time
    /// e li segnala come disponibili per l'aggiunta agli slot.
    /// </summary>
    public partial struct FocusTimeItemDetectionSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _collectibleQuery;
        
        // Raggio di rilevamento degli oggetti durante il Focus Time
        private const float FOCUS_TIME_DETECTION_RADIUS = 15.0f;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Query per il giocatore
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TransformComponent, FocusTimeComponent>()
                .WithAllRW<FocusTimeInputComponent>()
                .Build(ref state);
            
            // Query per gli oggetti collezionabili
            _collectibleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TransformComponent, CollectibleComponent>()
                .Build(ref state);
            
            state.RequireForUpdate(_playerQuery);
            state.RequireForUpdate(_collectibleQuery);
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni il tempo di gioco corrente
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Rileva oggetti nelle vicinanze del giocatore durante il Focus Time
            NativeArray<Entity> playerEntities = _playerQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<TransformComponent> playerTransforms = _playerQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            NativeArray<FocusTimeComponent> focusTimeComponents = _playerQuery.ToComponentDataArray<FocusTimeComponent>(Allocator.TempJob);
            
            NativeArray<Entity> collectibleEntities = _collectibleQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<TransformComponent> collectibleTransforms = _collectibleQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Per ogni giocatore, verifica oggetti nel raggio durante Focus Time
            for (int i = 0; i < playerEntities.Length; i++)
            {
                Entity playerEntity = playerEntities[i];
                TransformComponent playerTransform = playerTransforms[i];
                FocusTimeComponent focusTime = focusTimeComponents[i];
                
                // Procedi solo se il Focus Time è attivo
                if (focusTime.IsActive)
                {
                    // Verifica tutti gli oggetti collezionabili
                    for (int j = 0; j < collectibleEntities.Length; j++)
                    {
                        Entity collectibleEntity = collectibleEntities[j];
                        TransformComponent collectibleTransform = collectibleTransforms[j];
                        
                        // Calcola la distanza tra giocatore e oggetto
                        float3 offset = collectibleTransform.Position - playerTransform.Position;
                        float sqrDistance = math.lengthsq(offset);
                        
                        // Se l'oggetto è nel raggio e non è già negli slot, segnalalo come rilevato
                        if (sqrDistance <= FOCUS_TIME_DETECTION_RADIUS * FOCUS_TIME_DETECTION_RADIUS)
                        {
                            bool alreadyInSlots = false;
                            for (int k = 0; k < focusTime.ItemSlots.Length; k++)
                            {
                                if (focusTime.ItemSlots[k] == collectibleEntity)
                                {
                                    alreadyInSlots = true;
                                    break;
                                }
                            }
                            
                            if (!alreadyInSlots)
                            {
                                // Aggiorna l'input per segnalare il nuovo oggetto rilevato
                                var focusTimeInput = state.EntityManager.GetComponentData<FocusTimeInputComponent>(playerEntity);
                                focusTimeInput.NewItemDetected = true;
                                focusTimeInput.NewItemEntity = collectibleEntity;
                                
                                commandBuffer.SetComponent(i, playerEntity, focusTimeInput);
                                
                                // Segnala visivamente che l'oggetto è rilevato (si illumina)
                                if (state.EntityManager.HasComponent<RenderComponent>(collectibleEntity))
                                {
                                    var renderComponent = state.EntityManager.GetComponentData<RenderComponent>(collectibleEntity);
                                    // Imposta l'emissione o altri effetti visivi
                                    // (Questa implementazione dipende dalla specifica struttura di RenderComponent)
                                    
                                    commandBuffer.SetComponent(i, collectibleEntity, renderComponent);
                                }
                                
                                // Interrompi dopo aver trovato un oggetto (elabora un oggetto alla volta)
                                break;
                            }
                        }
                    }
                }
            }
            
            // Cleanup delle risorse native
            state.Dependency = playerEntities.Dispose(state.Dependency);
            state.Dependency = playerTransforms.Dispose(state.Dependency);
            state.Dependency = focusTimeComponents.Dispose(state.Dependency);
            state.Dependency = collectibleEntities.Dispose(state.Dependency);
            state.Dependency = collectibleTransforms.Dispose(state.Dependency);
        }
    }
}