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
    public partial struct FocusTimeSystem : ISystem
    {
        private EntityQuery _focusTimeQuery;
        
        // Singleton globale per il controllo del tempo di gioco
        private Entity _timeManagerEntity;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Query per entità con il componente FocusTime
            _focusTimeQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<FocusTimeInputComponent>()
                .WithAllRW<FocusTimeComponent>()
                .Build(ref state);
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_focusTimeQuery);
            
            // Crea un'entità singleton per gestire il tempo di gioco globale
            _timeManagerEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(_timeManagerEntity, new TimeScaleComponent { Scale = 1.0f });
            state.EntityManager.AddComponentData(_timeManagerEntity, new TimeManagerTag());
        }
        
        [Unity.Burst.BurstCompile]
        public partial struct FocusTimeProcessorJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            public NativeReference<bool> AnyFocusTimeActive;
            
            void Execute(
                Entity entity, 
                [ChunkIndexInQuery] int chunkIndexInQuery,
                ref FocusTimeComponent focusTime,
                in FocusTimeInputComponent input)
            {
                // Aggiorna lo stato del Focus Time (durata, cooldown, energia)
                bool stateChanged = focusTime.Update(DeltaTime);
                
                // Se è stata richiesta l'attivazione del Focus Time
                if (input.ActivateFocusTime && focusTime.IsAvailable)
                {
                    bool activated = focusTime.Activate();
                    
                    if (activated)
                    {
                        // Genera un evento di attivazione Focus Time
                        var eventEntity = CommandBuffer.CreateEntity(chunkIndexInQuery);
                        CommandBuffer.AddComponent(chunkIndexInQuery, eventEntity, new FocusTimeActivatedEvent
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
                    var eventEntity = CommandBuffer.CreateEntity(chunkIndexInQuery);
                    CommandBuffer.AddComponent(chunkIndexInQuery, eventEntity, new FocusTimeDeactivatedEvent
                    {
                        EntityDeactivated = entity,
                        CooldownRemaining = focusTime.CooldownRemaining
                    });
                }
                
                // Gestione della selezione degli oggetti durante il Focus Time
                if (focusTime.IsActive)
                {
                    AnyFocusTimeActive.Value = true;
                    
                    // Se è stato selezionato un oggetto
                    if (input.SelectedItemIndex >= 0 && input.SelectedItemIndex < focusTime.MaxItemSlots)
                    {
                        Entity selectedItem = focusTime.ItemSlots[input.SelectedItemIndex];
                        
                        if (selectedItem != Entity.Null)
                        {
                            // Genera un evento di utilizzo oggetto
                            var useItemEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                            CommandBuffer.AddComponent(chunkIndexInQuery, useItemEvent, new ItemUsedEvent
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
                            var addItemEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                            CommandBuffer.AddComponent(chunkIndexInQuery, addItemEvent, new ItemAddedToSlotEvent
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
                        var readyEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                        CommandBuffer.AddComponent(chunkIndexInQuery, readyEvent, new FocusTimeReadyEvent
                        {
                            EntityReady = entity
                        });
                    }
                    
                    // Se l'energia è stata completamente ricaricata
                    if (focusTime.CurrentEnergy >= focusTime.MaxEnergy)
                    {
                        var fullEnergyEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                        CommandBuffer.AddComponent(chunkIndexInQuery, fullEnergyEvent, new FocusTimeFullEnergyEvent
                        {
                            EntityWithFullEnergy = entity
                        });
                    }
                }
            }
        }
        
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottiene l'entità time manager per l'accesso in lettura/scrittura
            var timeScaleComponent = state.EntityManager.GetComponentData<TimeScaleComponent>(_timeManagerEntity);
            
            // Traccia se qualche giocatore ha il Focus Time attivo
            var anyFocusTimeActive = new NativeReference<bool>(false, Allocator.TempJob);
            
            // Elabora le richieste e lo stato del Focus Time
            state.Dependency = new FocusTimeProcessorJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = commandBuffer,
                AnyFocusTimeActive = anyFocusTimeActive
            }.ScheduleParallel(state.Dependency);
            
            // Completa tutte le job per garantire che i dati siano pronti
            state.CompleteDependency();
            
            // Se qualche giocatore ha il Focus Time attivo, rallenta il tempo globale
            if (anyFocusTimeActive.Value)
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
            state.EntityManager.SetComponentData(_timeManagerEntity, timeScaleComponent);
            
            // Rilascia le risorse native
            anyFocusTimeActive.Dispose();
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