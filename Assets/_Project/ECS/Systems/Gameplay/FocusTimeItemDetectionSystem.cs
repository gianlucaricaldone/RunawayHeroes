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
    public partial class FocusTimeItemDetectionSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EntityQuery _collectibleQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Raggio di rilevamento degli oggetti durante il Focus Time
        private const float FOCUS_TIME_DETECTION_RADIUS = 15.0f;
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Query per il giocatore
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<TransformComponent>(),
                ComponentType.ReadOnly<FocusTimeComponent>(),
                ComponentType.ReadWrite<FocusTimeInputComponent>()
            );
            
            // Query per gli oggetti collezionabili
            _collectibleQuery = GetEntityQuery(
                ComponentType.ReadOnly<TransformComponent>(),
                ComponentType.ReadOnly<CollectibleComponent>()
            );
            
            RequireForUpdate(_playerQuery);
            RequireForUpdate(_collectibleQuery);
        }
        
        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottieni il tempo di gioco corrente
            float currentTime = (float)Time.ElapsedTime;
            
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
                                var focusTimeInput = EntityManager.GetComponentData<FocusTimeInputComponent>(playerEntity);
                                focusTimeInput.NewItemDetected = true;
                                focusTimeInput.NewItemEntity = collectibleEntity;
                                
                                commandBuffer.SetComponent(0, playerEntity, focusTimeInput);
                                
                                // Segnala visivamente che l'oggetto è rilevato (si illumina)
                                if (EntityManager.HasComponent<RenderComponent>(collectibleEntity))
                                {
                                    var renderComponent = EntityManager.GetComponentData<RenderComponent>(collectibleEntity);
                                    // Imposta l'emissione o altri effetti visivi
                                    // (Questa implementazione dipende dalla specifica struttura di RenderComponent)
                                    
                                    commandBuffer.SetComponent(0, collectibleEntity, renderComponent);
                                }
                                
                                // Interrompi dopo aver trovato un oggetto (elabora un oggetto alla volta)
                                break;
                            }
                        }
                    }
                }
            }
            
            // Rilascia le array native
            playerEntities.Dispose();
            playerTransforms.Dispose();
            focusTimeComponents.Dispose();
            collectibleEntities.Dispose();
            collectibleTransforms.Dispose();
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}