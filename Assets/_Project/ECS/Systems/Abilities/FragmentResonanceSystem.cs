using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
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
    public partial struct FragmentResonanceSystem : ISystem
    {
        private EntityQuery _resonanceQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Query per entità con il componente Risonanza
            _resonanceQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAllRW<FragmentResonanceComponent, FocusTimeComponent, HealthComponent>()
                .WithAll<ResonanceInputComponent, TransformComponent>()
                .Build(ref state);
            
            // Richiede entità corrispondenti per eseguire l'aggiornamento
            state.RequireForUpdate(_resonanceQuery);
        }
        
        [BurstCompile]
        public partial struct FragmentResonanceJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            
            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndexInQuery,
                ref FragmentResonanceComponent resonance,
                ref FocusTimeComponent focusTime,
                ref HealthComponent health,
                in ResonanceInputComponent input,
                in TransformComponent transform)
            {
                // Aggiorna i timer di cooldown
                bool stateChanged = resonance.Update(DeltaTime);
                
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
                            var waveEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                            CommandBuffer.AddComponent(chunkIndexInQuery, waveEvent, new EnergyWaveEvent
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
                                var combinedAbilityEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                                CommandBuffer.AddComponent(chunkIndexInQuery, combinedAbilityEvent, new CombinedAbilitiesEvent
                                {
                                    PrimaryCharacter = resonance.ActiveCharacter,
                                    SecondaryCharacter = previousCharacter,
                                    Duration = 3.0f // Durata breve dell'effetto combinato
                                });
                            }
                            
                            // 7. Genera l'evento principale di cambio personaggio
                            var switchEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                            CommandBuffer.AddComponent(chunkIndexInQuery, switchEvent, new CharacterSwitchedEvent
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
                        var unlockEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                        CommandBuffer.AddComponent(chunkIndexInQuery, unlockEvent, new CharacterUnlockedEvent
                        {
                            PlayerEntity = entity,
                            UnlockedCharacter = input.NewCharacterEntity,
                            TotalCharacters = resonance.CharacterCount
                        });
                        
                        // Se questo è il secondo personaggio, la Risonanza viene sbloccata
                        if (resonance.CharacterCount == 2)
                        {
                            var resonanceUnlockedEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                            CommandBuffer.AddComponent(chunkIndexInQuery, resonanceUnlockedEvent, new ResonanceUnlockedEvent
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
                    var upgradeEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                    CommandBuffer.AddComponent(chunkIndexInQuery, upgradeEvent, new ResonanceUpgradedEvent
                    {
                        PlayerEntity = entity,
                        PreviousLevel = previousLevel,
                        NewLevel = resonance.ResonanceLevel
                    });
                }
                
                // Se lo stato della Risonanza è cambiato (cooldown terminato)
                if (stateChanged && resonance.IsAvailable)
                {
                    var readyEvent = CommandBuffer.CreateEntity(chunkIndexInQuery);
                    CommandBuffer.AddComponent(chunkIndexInQuery, readyEvent, new ResonanceReadyEvent
                    {
                        PlayerEntity = entity
                    });
                }
            }
        }
        
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Elabora le richieste e lo stato della Risonanza
            state.Dependency = new FragmentResonanceJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = commandBuffer
            }.ScheduleParallel(state.Dependency);
        }
    }
}