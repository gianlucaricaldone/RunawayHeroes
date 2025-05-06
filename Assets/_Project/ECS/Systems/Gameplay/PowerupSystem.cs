using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Combat;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce i powerup nel gioco, inclusa la loro attivazione,
    /// effetti temporanei, e durata nel tempo.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct PowerupSystem : ISystem
    {
        // Query per i powerup attivi nel mondo
        private EntityQuery _powerupEntitiesQuery;
        
        // Query per i powerup attivi sui personaggi
        private EntityQuery _activePowerupEffectsQuery;
        
        // Query per collisioni con powerup
        private EntityQuery _powerupCollisionsQuery;
        
        /// <summary>
        /// Inizializza il sistema e configura le query necessarie
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per i powerup nel mondo
            _powerupEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PowerupComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per gli effetti di powerup attivi
            _activePowerupEffectsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ActivePowerupComponent>()
                .Build(ref state);
                
            // Configura la query per le collisioni con powerup
            _powerupCollisionsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PowerupCollectedEvent>()
                .Build(ref state);
                
            // Richiedi un command buffer e verifica che ci siano powerup per l'aggiornamento
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }

        /// <summary>
        /// Gestisce gli aggiornamenti di powerup, controllando collisioni, 
        /// applicando effetti e aggiornando durate
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer di comandi per eventuali modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Prepara i ComponentLookup
            var powerupLookup = SystemAPI.GetComponentLookup<PowerupComponent>(true);
            var transformLookup = SystemAPI.GetComponentLookup<TransformComponent>(true);
            var movementLookup = SystemAPI.GetComponentLookup<MovementComponent>(true);
            var combatLookup = SystemAPI.GetComponentLookup<CombatComponent>(true);
            var defenseLookup = SystemAPI.GetComponentLookup<DefenseComponent>(true);
            var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true);
            var entityLookup = SystemAPI.GetEntityStorageInfoLookup();
            
            // 1. Aggiorna la rotazione e gli effetti visivi dei powerup nel mondo
            if (!_powerupEntitiesQuery.IsEmpty)
            {
                state.Dependency = new UpdateWorldPowerupsJob
                {
                    DeltaTime = deltaTime
                }.ScheduleParallel(_powerupEntitiesQuery, state.Dependency);
            }
            
            // 2. Elabora gli eventi di collisione con powerup
            if (!_powerupCollisionsQuery.IsEmpty)
            {
                state.Dependency = new ProcessPowerupCollectionsJob
                {
                    ECB = ecb.AsParallelWriter(),
                    PowerupLookup = powerupLookup,
                    TransformLookup = transformLookup,
                    MovementLookup = movementLookup,
                    CombatLookup = combatLookup,
                    DefenseLookup = defenseLookup,
                    HealthLookup = healthLookup,
                    EntityStorageInfoLookup = entityLookup
                }.ScheduleParallel(_powerupCollisionsQuery, state.Dependency);
            }
            
            // 3. Aggiorna la durata dei powerup attivi e rimuovi quelli scaduti
            if (!_activePowerupEffectsQuery.IsEmpty)
            {
                state.Dependency = new UpdateActivePowerupsJob
                {
                    DeltaTime = deltaTime,
                    ECB = ecb.AsParallelWriter(),
                    MovementLookup = movementLookup,
                    CombatLookup = combatLookup,
                    DefenseLookup = defenseLookup,
                    HealthLookup = healthLookup
                }.ScheduleParallel(_activePowerupEffectsQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che aggiorna l'aspetto visivo dei powerup nel mondo
        /// </summary>
        [BurstCompile]
        private partial struct UpdateWorldPowerupsJob : IJobEntity
        {
            public float DeltaTime;
            
            [BurstCompile]
            private void Execute(
                ref TransformComponent transform,
                ref PowerupComponent powerup)
            {
                // Fa roteare il powerup sull'asse Y per effetto visivo
                quaternion rotationDelta = quaternion.AxisAngle(new float3(0, 1, 0), powerup.RotationSpeed * DeltaTime);
                transform.Rotation = math.mul(transform.Rotation, rotationDelta);
                
                // Oscillazione verticale
                if (powerup.HasVerticalBob)
                {
                    powerup.AnimationTime += DeltaTime;
                    float bobOffset = math.sin(powerup.AnimationTime * powerup.BobFrequency) * powerup.BobAmplitude;
                    transform.Position.y = powerup.OriginalHeight + bobOffset;
                }
                
                // Effetto pulsazione se abilitato
                if (powerup.HasPulsation)
                {
                    powerup.PulsationTime += DeltaTime;
                    float pulseFactor = 1.0f + math.sin(powerup.PulsationTime * powerup.PulseFrequency) * powerup.PulseAmplitude;
                    transform.Scale = new float3(powerup.OriginalScale * pulseFactor);
                }
            }
        }
        
        /// <summary>
        /// Job che processa le collisioni con powerup
        /// </summary>
        [BurstCompile]
        private partial struct ProcessPowerupCollectionsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<PowerupComponent> PowerupLookup;
            [ReadOnly] public ComponentLookup<TransformComponent> TransformLookup;
            [ReadOnly] public ComponentLookup<MovementComponent> MovementLookup;
            [ReadOnly] public ComponentLookup<CombatComponent> CombatLookup;
            [ReadOnly] public ComponentLookup<DefenseComponent> DefenseLookup;
            [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
            [ReadOnly] public EntityStorageInfoLookup EntityStorageInfoLookup;
            
            [BurstCompile]
            private void Execute(
                Entity entity, 
                [EntityIndexInQuery] int sortKey,
                in PowerupCollectedEvent collectionEvent)
            {
                // Ottiene il riferimento al powerup e al personaggio
                Entity powerupEntity = collectionEvent.PowerupEntity;
                Entity characterEntity = collectionEvent.CollectorEntity;
                
                if (!EntityStorageInfoLookup.Exists(powerupEntity) || !EntityStorageInfoLookup.Exists(characterEntity))
                {
                    // Entità invalide, distruggi l'evento e termina
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }
                
                // Ottieni il componente powerup
                if (PowerupLookup.HasComponent(powerupEntity))
                {
                    var powerup = PowerupLookup[powerupEntity];
                    
                    // Crea un componente di powerup attivo sul personaggio
                    ECB.AddComponent(sortKey, characterEntity, new ActivePowerupComponent
                    {
                        PowerupType = powerup.PowerupType,
                        Duration = powerup.Duration,
                        RemainingTime = powerup.Duration,
                        StrengthMultiplier = powerup.StrengthMultiplier,
                        SpeedMultiplier = powerup.SpeedMultiplier,
                        DefenseMultiplier = powerup.DefenseMultiplier,
                        SpecialEffect = powerup.SpecialEffect
                    });
                    
                    // Applica immediatamente gli effetti al personaggio
                    ApplyPowerupEffects(characterEntity, powerup, ECB, sortKey);
                    
                    // Crea evento di feedback visivo/audio
                    Entity feedbackEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, feedbackEvent, new PowerupFeedbackEvent
                    {
                        PowerupType = powerup.PowerupType,
                        CollectorEntity = characterEntity,
                        CollectionPoint = TransformLookup.HasComponent(powerupEntity) ? 
                                         TransformLookup[powerupEntity].Position : float3.zero
                    });
                    
                    // Distrugge il powerup nel mondo
                    ECB.DestroyEntity(sortKey, powerupEntity);
                }
                
                // Distrugge l'evento di collisione
                ECB.DestroyEntity(sortKey, entity);
            }
            
            private void ApplyPowerupEffects(Entity characterEntity, PowerupComponent powerup, 
                                          EntityCommandBuffer.ParallelWriter ecb, int sortKey)
            {
                // Applica gli effetti appropriati in base al tipo di powerup
                switch (powerup.PowerupType)
                {
                    case PowerupType.SpeedBoost:
                        if (MovementLookup.HasComponent(characterEntity))
                        {
                            var movement = MovementLookup[characterEntity];
                            // Salva la velocità originale nel componente powerup attivo
                            var activePowerup = new ActivePowerupComponent
                            {
                                OriginalSpeed = movement.BaseSpeed,
                                SpeedMultiplier = powerup.SpeedMultiplier
                            };
                            ecb.SetComponent(sortKey, characterEntity, activePowerup);
                            
                            // Applica il moltiplicatore di velocità
                            movement.CurrentSpeed = movement.BaseSpeed * powerup.SpeedMultiplier;
                            ecb.SetComponent(sortKey, characterEntity, movement);
                        }
                        break;
                        
                    case PowerupType.DamageBoost:
                        if (CombatLookup.HasComponent(characterEntity))
                        {
                            var combat = CombatLookup[characterEntity];
                            // Salva il danno originale nel componente powerup attivo
                            var activePowerup = new ActivePowerupComponent
                            {
                                OriginalDamage = combat.BaseDamage,
                                StrengthMultiplier = powerup.StrengthMultiplier
                            };
                            ecb.SetComponent(sortKey, characterEntity, activePowerup);
                            
                            // Applica il moltiplicatore di danno
                            combat.CurrentDamage = combat.BaseDamage * powerup.StrengthMultiplier;
                            ecb.SetComponent(sortKey, characterEntity, combat);
                        }
                        break;
                        
                    case PowerupType.DefenseBoost:
                        if (DefenseLookup.HasComponent(characterEntity))
                        {
                            var defense = DefenseLookup[characterEntity];
                            // Salva la difesa originale nel componente powerup attivo
                            var activePowerup = new ActivePowerupComponent
                            {
                                OriginalDefense = defense.BaseDefense,
                                DefenseMultiplier = powerup.DefenseMultiplier
                            };
                            ecb.SetComponent(sortKey, characterEntity, activePowerup);
                            
                            // Applica il moltiplicatore di difesa
                            defense.CurrentDefense = defense.BaseDefense * powerup.DefenseMultiplier;
                            ecb.SetComponent(sortKey, characterEntity, defense);
                        }
                        break;
                        
                    case PowerupType.Invulnerability:
                        if (HealthLookup.HasComponent(characterEntity))
                        {
                            var health = HealthLookup[characterEntity];
                            health.IsInvulnerable = true;
                            ecb.SetComponent(sortKey, characterEntity, health);
                        }
                        break;
                        
                    case PowerupType.HealthRestore:
                        if (HealthLookup.HasComponent(characterEntity))
                        {
                            var health = HealthLookup[characterEntity];
                            health.CurrentHealth = health.MaxHealth;
                            ecb.SetComponent(sortKey, characterEntity, health);
                        }
                        break;
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna i powerup attivi e rimuove quelli scaduti
        /// </summary>
        [BurstCompile]
        private partial struct UpdateActivePowerupsJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<MovementComponent> MovementLookup;
            [ReadOnly] public ComponentLookup<CombatComponent> CombatLookup;
            [ReadOnly] public ComponentLookup<DefenseComponent> DefenseLookup;
            [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref ActivePowerupComponent activePowerup)
            {
                // Aggiorna il timer di durata
                activePowerup.RemainingTime -= DeltaTime;
                
                // Controlla se è scaduto
                if (activePowerup.RemainingTime <= 0)
                {
                    // Ripristina gli effetti originali in base al tipo
                    RevertPowerupEffects(entity, activePowerup, ECB, sortKey);
                    
                    // Crea un evento di scadenza
                    Entity expirationEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, expirationEvent, new PowerupExpiredEvent
                    {
                        CharacterEntity = entity,
                        PowerupType = activePowerup.PowerupType
                    });
                    
                    // Rimuovi il componente di powerup attivo
                    ECB.RemoveComponent<ActivePowerupComponent>(sortKey, entity);
                }
                else if (activePowerup.RemainingTime < 3.0f && !activePowerup.WarningTriggered)
                {
                    // Invia un evento di avviso quando il powerup sta per scadere
                    activePowerup.WarningTriggered = true;
                    
                    Entity warningEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, warningEvent, new PowerupExpiringWarningEvent
                    {
                        CharacterEntity = entity,
                        PowerupType = activePowerup.PowerupType,
                        RemainingTime = activePowerup.RemainingTime
                    });
                }
            }
            
            private void RevertPowerupEffects(Entity characterEntity, ActivePowerupComponent activePowerup, 
                                            EntityCommandBuffer.ParallelWriter ecb, int sortKey)
            {
                // Ripristina gli effetti originali in base al tipo di powerup
                switch (activePowerup.PowerupType)
                {
                    case PowerupType.SpeedBoost:
                        if (MovementLookup.HasComponent(characterEntity))
                        {
                            var movement = MovementLookup[characterEntity];
                            movement.CurrentSpeed = activePowerup.OriginalSpeed;
                            ecb.SetComponent(sortKey, characterEntity, movement);
                        }
                        break;
                        
                    case PowerupType.DamageBoost:
                        if (CombatLookup.HasComponent(characterEntity))
                        {
                            var combat = CombatLookup[characterEntity];
                            combat.CurrentDamage = activePowerup.OriginalDamage;
                            ecb.SetComponent(sortKey, characterEntity, combat);
                        }
                        break;
                        
                    case PowerupType.DefenseBoost:
                        if (DefenseLookup.HasComponent(characterEntity))
                        {
                            var defense = DefenseLookup[characterEntity];
                            defense.CurrentDefense = activePowerup.OriginalDefense;
                            ecb.SetComponent(sortKey, characterEntity, defense);
                        }
                        break;
                        
                    case PowerupType.Invulnerability:
                        if (HealthLookup.HasComponent(characterEntity))
                        {
                            var health = HealthLookup[characterEntity];
                            health.IsInvulnerable = false;
                            ecb.SetComponent(sortKey, characterEntity, health);
                        }
                        break;
                }
            }
        }
    }
    
    #region Componenti e Eventi PowerupSystem
    
    /// <summary>
    /// Tipi di powerup disponibili
    /// </summary>
    public enum PowerupType : byte
    {
        SpeedBoost = 0,
        DamageBoost = 1,
        DefenseBoost = 2,
        Invulnerability = 3,
        HealthRestore = 4,
        ManaRestore = 5,
        ScoreMultiplier = 6,
        ExtraLife = 7
    }
    
    /// <summary>
    /// Componente che definisce un powerup
    /// </summary>
    [System.Serializable]
    public struct PowerupComponent : IComponentData
    {
        // Proprietà base
        public PowerupType PowerupType;     // Tipo di powerup
        public float Duration;              // Durata in secondi (0 = effetto istantaneo)
        
        // Moltiplicatori per statistiche
        public float StrengthMultiplier;    // Moltiplicatore forza/danno
        public float SpeedMultiplier;       // Moltiplicatore velocità
        public float DefenseMultiplier;     // Moltiplicatore difesa
        
        // Effetti speciali
        public byte SpecialEffect;          // Effetto speciale codificato come byte
        
        // Proprietà visive per powerup nel mondo
        public float RotationSpeed;         // Velocità rotazione
        public bool HasVerticalBob;         // Se oscilla verticalmente
        public float BobAmplitude;          // Ampiezza oscillazione
        public float BobFrequency;          // Frequenza oscillazione
        public float OriginalHeight;        // Altezza originale per l'oscillazione
        
        public bool HasPulsation;           // Se pulsa (cambia scala)
        public float PulseAmplitude;        // Ampiezza pulsazione
        public float PulseFrequency;        // Frequenza pulsazione
        public float3 OriginalScale;        // Scala originale per la pulsazione
        
        // Timer per animazioni
        public float AnimationTime;
        public float PulsationTime;
    }
    
    /// <summary>
    /// Componente per powerup attivi su un'entità
    /// </summary>
    [System.Serializable]
    public struct ActivePowerupComponent : IComponentData
    {
        public PowerupType PowerupType;     // Tipo di powerup
        public float Duration;              // Durata totale in secondi
        public float RemainingTime;         // Tempo rimanente
        
        // Valori originali da ripristinare alla scadenza
        public float OriginalSpeed;
        public float OriginalDamage;
        public float OriginalDefense;
        
        // Moltiplicatori attivi
        public float StrengthMultiplier;
        public float SpeedMultiplier;
        public float DefenseMultiplier;
        
        // Effetti speciali
        public byte SpecialEffect;
        
        // Flag per eventi
        public bool WarningTriggered;       // Se l'avviso di scadenza è già stato inviato
    }
    
    /// <summary>
    /// Evento generato quando viene raccolto un powerup
    /// </summary>
    [System.Serializable]
    public struct PowerupCollectedEvent : IComponentData
    {
        public Entity PowerupEntity;      // Entità del powerup
        public Entity CollectorEntity;    // Entità che ha raccolto il powerup
    }
    
    /// <summary>
    /// Evento per l'effetto visivo/audio di raccolta powerup
    /// </summary>
    [System.Serializable]
    public struct PowerupFeedbackEvent : IComponentData
    {
        public PowerupType PowerupType;    // Tipo di powerup
        public Entity CollectorEntity;     // Entità che ha raccolto
        public float3 CollectionPoint;     // Punto di raccolta
    }
    
    /// <summary>
    /// Evento generato quando un powerup sta per scadere
    /// </summary>
    [System.Serializable]
    public struct PowerupExpiringWarningEvent : IComponentData
    {
        public Entity CharacterEntity;     // Entità con il powerup
        public PowerupType PowerupType;    // Tipo di powerup
        public float RemainingTime;        // Tempo rimanente
    }
    
    /// <summary>
    /// Evento generato quando un powerup è scaduto
    /// </summary>
    [System.Serializable]
    public struct PowerupExpiredEvent : IComponentData
    {
        public Entity CharacterEntity;     // Entità con il powerup
        public PowerupType PowerupType;    // Tipo di powerup
    }
    
    #endregion
}
