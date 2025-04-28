using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema che gestisce la meccanica "Risonanza dei Frammenti", che permette
    /// ai giocatori di cambiare personaggio istantaneamente durante il gameplay.
    /// </summary>
    public partial class FragmentResonanceSystem : SystemBase
    {
        private EntityQuery _resonanceQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        protected override void OnCreate()
        {
            // Riferimento al command buffer system per generare eventi
            _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Query per entità con il componente Risonanza
            _resonanceQuery = GetEntityQuery(
                ComponentType.ReadWrite<FragmentResonanceComponent>(),
                ComponentType.ReadOnly<ResonanceInputComponent>()
            );
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            RequireForUpdate(_resonanceQuery);
        }
        
        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora le richieste e lo stato della Risonanza
            Entities
                .WithName("FragmentResonanceProcessor")
                .ForEach((Entity entity, int entityInQueryIndex,
                          ref FragmentResonanceComponent resonance,
                          ref FocusTimeComponent focusTime,
                          ref HealthComponent health,
                          in ResonanceInputComponent input,
                          in TransformComponent transform) => 
                {
                    // Aggiorna i timer di cooldown
                    bool stateChanged = resonance.Update(deltaTime);
                    
                    // Se è stato richiesto un cambio di personaggio
                    if (input.SwitchToCharacterIndex >= 0 && resonance.IsUnlocked)
                    {
                        // Verifica che ci sia energia Focus sufficiente
                        bool hasSufficientFocus = (resonance.ResonanceLevel >= 3) || // Risonanza Perfetta elimina il costo
                                                 (focusTime.CurrentEnergy >= resonance.FocusTimeCost);
                        
                        if (resonance.IsAvailable && hasSufficientFocus)
                        {
                            // Memorizza informazioni sul personaggio corrente prima del cambio
                            Entity previousCharacter = resonance.ActiveCharacter;
                            float3 currentPosition = transform.Position;
                            float3 currentVelocity = new float3(0, 0, 0); // In un sistema reale, questa sarebbe ottenuta da PhysicsComponent
                            
                            // Tenta di cambiare personaggio
                            bool switched = resonance.SwitchCharacter(input.SwitchToCharacterIndex);
                            
                            if (switched)
                            {
                                // 1. Consuma energia Focus Time (se necessario)
                                if (resonance.ResonanceLevel < 3) // Skip se Risonanza Perfetta
                                {
                                    focusTime.CurrentEnergy -= resonance.FocusTimeCost;
                                }
                                
                                // 2. Attiva invulnerabilità temporanea
                                health.SetInvulnerable(resonance.InvulnerabilityDuration);
                                
                                // 3. Genera un'onda di energia che danneggia i nemici
                                var waveEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, waveEvent, new EnergyWaveEvent
                                {
                                    Origin = currentPosition,
                                    Radius = resonance.EnergyWaveRadius,
                                    Damage = resonance.EnergyWaveDamage,
                                    SourceEntity = entity
                                });
                                
                                // 4. Applica bonus ambientali in base al personaggio e all'ambiente
                                // (in un sistema reale, otterremmo il tipo di mondo attuale dal livello)
                                WorldType currentWorld = WorldType.Urban; // Esempio
                                EnvironmentalBonus bonus = resonance.GetEnvironmentalBonus(currentWorld);
                                
                                // 5. Applica bonus di Risonanza Amplificata se sbloccata
                                if (resonance.ResonanceLevel >= 2)
                                {
                                    resonance.EnergyWaveRadius *= 1.5f; // Aumenta il raggio dell'onda
                                }
                                
                                // 6. Controlla se è possibile attivare le abilità combinate (Risonanza Totale)
                                if (resonance.ResonanceLevel >= 4)
                                {
                                    // Logica per attivare brevemente due abilità speciali combinate
                                    var combinedAbilityEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                    commandBuffer.AddComponent(entityInQueryIndex, combinedAbilityEvent, new CombinedAbilitiesEvent
                                    {
                                        PrimaryCharacter = resonance.ActiveCharacter,
                                        SecondaryCharacter = previousCharacter,
                                        Duration = 3.0f // Durata breve dell'effetto combinato
                                    });
                                }
                                
                                // 7. Genera l'evento principale di cambio personaggio
                                var switchEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, switchEvent, new CharacterSwitchedEvent
                                {
                                    PlayerEntity = entity,
                                    PreviousCharacter = previousCharacter,
                                    NewCharacter = resonance.ActiveCharacter,
                                    Position = currentPosition,
                                    Velocity = currentVelocity,
                                    WorldType = currentWorld,
                                    AppliedBonus = bonus
                                });
                            }
                        }
                    }
                    
                    // Se è stato sbloccato un nuovo personaggio
                    if (input.NewCharacterUnlocked && input.NewCharacterEntity != Entity.Null)
                    {
                        bool added = resonance.AddCharacter(input.NewCharacterEntity);
                        
                        if (added)
                        {
                            // Genera un evento di sblocco personaggio
                            var unlockEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, unlockEvent, new CharacterUnlockedEvent
                            {
                                PlayerEntity = entity,
                                UnlockedCharacter = input.NewCharacterEntity,
                                TotalCharacters = resonance.CharacterCount
                            });
                            
                            // Se questo è il secondo personaggio, la Risonanza viene sbloccata
                            if (resonance.CharacterCount == 2)
                            {
                                var resonanceUnlockedEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, resonanceUnlockedEvent, new ResonanceUnlockedEvent
                                {
                                    PlayerEntity = entity
                                });
                            }
                        }
                    }
                    
                    // Se è stato sbloccato un nuovo livello di Risonanza
                    if (input.ResonanceLevelUp && input.NewResonanceLevel > resonance.ResonanceLevel)
                    {
                        int previousLevel = resonance.ResonanceLevel;
                        resonance.ResonanceLevel = input.NewResonanceLevel;
                        
                        // Genera un evento di potenziamento Risonanza
                        var upgradeEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, upgradeEvent, new ResonanceUpgradedEvent
                        {
                            PlayerEntity = entity,
                            PreviousLevel = previousLevel,
                            NewLevel = resonance.ResonanceLevel
                        });
                    }
                    
                    // Se lo stato della Risonanza è cambiato (cooldown terminato)
                    if (stateChanged && resonance.IsAvailable)
                    {
                        var readyEvent = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, readyEvent, new ResonanceReadyEvent
                        {
                            PlayerEntity = entity
                        });
                    }
                    
                }).ScheduleParallel();
            
            // Assicura che il command buffer venga eseguito
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
    }
}