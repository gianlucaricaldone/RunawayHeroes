using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce la meccanica del "Focus Time", permettendo al giocatore di
    /// rallentare il tempo per prendere decisioni strategiche e utilizzare oggetti.
    /// </summary>
    public partial class FocusTimeSystem : SystemBase
    {
        private EntityQuery _focusTimeQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Singleton globale per il controllo del tempo di gioco
        private Entity _timeManagerEntity;
        
        protected override void OnCreate()
        {
            // Riferimento al command buffer system per generare eventi
            _commandBufferSystem = World.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Query per entità con il componente FocusTime
            _focusTimeQuery = GetEntityQuery(
                ComponentType.ReadWrite<FocusTimeComponent>(),
                ComponentType.ReadOnly<FocusTimeInputComponent>()
            );
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            RequireForUpdate(_focusTimeQuery);
            
            // Crea un'entità singleton per gestire il tempo di gioco globale
            _timeManagerEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(_timeManagerEntity, new TimeScaleComponent { Scale = 1.0f });
            EntityManager.AddComponentData(_timeManagerEntity, new TimeManagerTag());
        }
        
        protected override void OnUpdate()
        {
            float deltaTime =SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottiene l'entità time manager per l'accesso in lettura/scrittura
            var timeManagerEntity = _timeManagerEntity;
            var timeScaleComponent = EntityManager.GetComponentData<TimeScaleComponent>(timeManagerEntity);
            
            // Traccia se qualche giocatore ha il Focus Time attivo
            bool anyFocusTimeActive = false;
            
            // Elabora le richieste e lo stato del Focus Time
            Entities
                .WithName("FocusTimeProcessor")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref FocusTimeComponent focusTime,
                          in FocusTimeInputComponent input) => 
                {
                    // Aggiorna lo stato del Focus Time (durata, cooldown, energia)
                    bool stateChanged = focusTime.Update(deltaTime);
                    
                    // Se è stata richiesta l'attivazione del Focus Time
                    if (input.ActivateFocusTime && focusTime.IsAvailable)
                    {
                        bool activated = focusTime.Activate();
                        
                        if (activated)
                        {
                            // Genera un evento di attivazione Focus Time
                            var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new FocusTimeActivatedEvent
                            {
                                EntityActivated = entity,
                                Duration = focusTime.RemainingTime,
                                TimeScale = focusTime.TimeScale
                            });
                        }
                    }
                    // Se è stata richiesta la disattivazione del Focus Time
                    else if (input.DeactivateFocusTime && focusTime.IsActive)
                    {
                        focusTime.Deactivate(false); // Cooldown proporzionale
                        
                        // Genera un evento di disattivazione Focus Time
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new FocusTimeDeactivatedEvent
                        {
                            EntityDeactivated = entity,
                            CooldownRemaining = focusTime.CooldownRemaining
                        });
                    }
                    
                    // Gestione della selezione degli oggetti durante il Focus Time
                    if (focusTime.IsActive)
                    {
                        anyFocusTimeActive = true;
                        
                        // Se è stato selezionato un oggetto
                        if (input.SelectedItemIndex >= 0 && input.SelectedItemIndex < focusTime.MaxItemSlots)
                        {
                            Entity selectedItem = focusTime.ItemSlots[input.SelectedItemIndex];
                            
                            if (selectedItem != Entity.Null)
                            {
                                // Genera un evento di utilizzo oggetto
                                var useItemEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, useItemEvent, new ItemUsedEvent
                                {
                                    UserEntity = entity,
                                    ItemEntity = selectedItem,
                                    SlotIndex = input.SelectedItemIndex
                                });
                                
                                // Rimuovi l'oggetto dallo slot dopo l'uso
                                focusTime.RemoveItem(input.SelectedItemIndex);
                            }
                        }
                        
                        // Controlla se sono stati trovati nuovi oggetti nel raggio durante il Focus Time
                        if (input.NewItemDetected && input.NewItemEntity != Entity.Null)
                        {
                            // Tenta di aggiungere l'oggetto a uno slot vuoto
                            int slotIndex = focusTime.AddItem(input.NewItemEntity);
                            
                            // Se l'oggetto è stato aggiunto con successo
                            if (slotIndex >= 0)
                            {
                                // Genera un evento di aggiunta oggetto
                                var addItemEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, addItemEvent, new ItemAddedToSlotEvent
                                {
                                    UserEntity = entity,
                                    ItemEntity = input.NewItemEntity,
                                    SlotIndex = slotIndex
                                });
                            }
                        }
                    }
                    
                    // Se lo stato del Focus Time è cambiato, genera eventi appropriati
                    if (stateChanged)
                    {
                        // Se il Focus Time è diventato disponibile dopo un cooldown
                        if (focusTime.IsAvailable && !focusTime.IsActive)
                        {
                            var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new FocusTimeReadyEvent
                            {
                                EntityReady = entity
                            });
                        }
                        
                        // Se l'energia è stata completamente ricaricata
                        if (focusTime.CurrentEnergy >= focusTime.MaxEnergy)
                        {
                            var fullEnergyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, fullEnergyEvent, new FocusTimeFullEnergyEvent
                            {
                                EntityWithFullEnergy = entity
                            });
                        }
                    }
                    
                }).ScheduleParallel();
            
            // Aggiorna la scala del tempo globale in base allo stato del Focus Time
            CompleteDependency(); // Assicura che il job precedente sia completo
            
            // Se qualche giocatore ha il Focus Time attivo, rallenta il tempo globale
            if (anyFocusTimeActive)
            {
                // Recupera il componente FocusTime del giocatore attivo per ottenere il TimeScale
                // (In un gioco con più giocatori, potrebbe essere necessaria una logica più complessa)
                FocusTimeComponent activePlayerFocusTime = _focusTimeQuery.GetSingleton<FocusTimeComponent>();
                timeScaleComponent.Scale = activePlayerFocusTime.TimeScale;
            }
            else
            {
                // Ripristina la velocità normale
                timeScaleComponent.Scale = 1.0f;
            }
            
            // Aggiorna il componente TimeScale sul singleton
            EntityManager.SetComponentData(timeManagerEntity, timeScaleComponent);
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
    
    /// <summary>
    /// Componente che gestisce la scala del tempo globale
    /// </summary>
    public struct TimeScaleComponent : IComponentData
    {
        public float Scale;
    }
    
    /// <summary>
    /// Tag per identificare l'entità manager del tempo
    /// </summary>
    public struct TimeManagerTag : IComponentData { }
}