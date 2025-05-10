using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce i collezionabili nel gioco, inclusa la generazione,
    /// raccolta, e il conteggio dei collezionabili per i vari tipi.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CollectibleSystem : ISystem
    {
        // Query per i collezionabili nel mondo
        private EntityQuery _collectiblesQuery;
        
        // Query per i collezionabili magnetizzati
        private EntityQuery _magnetizedCollectiblesQuery;
        
        // Query per gli eventi di raccolta collezionabili
        private EntityQuery _collectibleEventsQuery;
        
        // Dati per attrattori magnetici
        private EntityQuery _magnetSourcesQuery;
        
        /// <summary>
        /// Inizializza il sistema e configura le query necessarie
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per i collezionabili nel mondo
            _collectiblesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CollectibleComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per i collezionabili magnetizzati
            _magnetizedCollectiblesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CollectibleComponent, TransformComponent, MagnetizedTag>()
                .Build(ref state);
                
            // Configura la query per gli eventi di raccolta
            _collectibleEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CollectibleCollectedEvent>()
                .Build(ref state);
                
            // Configura la query per gli attrattori magnetici
            _magnetSourcesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MagnetSourceComponent, TransformComponent>()
                .Build(ref state);
                
            // Richiedi un command buffer e verifica che ci siano collezionabili per l'aggiornamento
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
        /// Aggiorna i collezionabili, gestisce le raccolte e magnetizzazioni
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
            var transformLookup = SystemAPI.GetComponentLookup<TransformComponent>(true);
            var scoreLookup = SystemAPI.GetComponentLookup<ScoreComponent>(true);
            var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true);
            var fragmentInventoryLookup = SystemAPI.GetComponentLookup<FragmentInventoryComponent>(true);
            var keyInventoryLookup = SystemAPI.GetComponentLookup<KeyInventoryComponent>(true);
            var collectibleLookup = SystemAPI.GetComponentLookup<CollectibleComponent>(true);
            var entityLookup = SystemAPI.GetEntityStorageInfoLookup();
            
            // 1. Aggiorna l'animazione dei collezionabili (rotazione, fluttuazione, ecc.)
            if (!_collectiblesQuery.IsEmpty)
            {
                state.Dependency = new UpdateCollectiblesAnimationJob
                {
                    DeltaTime = deltaTime
                }.ScheduleParallel(_collectiblesQuery, state.Dependency);
            }
            
            // 2. Gestisci gli attrattori magnetici (se presenti)
            if (!_magnetSourcesQuery.IsEmpty && !_collectiblesQuery.IsEmpty)
            {
                // Raccogli le informazioni sugli attrattori
                var magnetSources = _magnetSourcesQuery.ToEntityArray(Allocator.TempJob);
                var magnetPositions = _magnetSourcesQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
                var magnetProperties = _magnetSourcesQuery.ToComponentDataArray<MagnetSourceComponent>(Allocator.TempJob);
                
                // Aggiorna i collezionabili in base agli attrattori
                state.Dependency = new UpdateMagnetEffectsJob
                {
                    DeltaTime = deltaTime,
                    MagnetSources = magnetSources,
                    MagnetPositions = magnetPositions,
                    MagnetProperties = magnetProperties,
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_collectiblesQuery, state.Dependency);
                
                // Pulisci le risorse
                state.Dependency = magnetSources.Dispose(state.Dependency);
                state.Dependency = magnetPositions.Dispose(state.Dependency);
                state.Dependency = magnetProperties.Dispose(state.Dependency);
            }
            
            // 3. Aggiorna il movimento dei collezionabili magnetizzati
            if (!_magnetizedCollectiblesQuery.IsEmpty)
            {
                state.Dependency = new MoveMagnetizedCollectiblesJob
                {
                    DeltaTime = deltaTime,
                    TransformLookup = transformLookup,
                    EntityLookupTable = entityLookup
                }.ScheduleParallel(_magnetizedCollectiblesQuery, state.Dependency);
            }
            
            // 4. Elabora gli eventi di raccolta collezionabili
            if (!_collectibleEventsQuery.IsEmpty)
            {
                state.Dependency = new ProcessCollectionEventsJob
                {
                    ECB = ecb.AsParallelWriter(),
                    CollectibleLookup = collectibleLookup,
                    TransformLookup = transformLookup,
                    ScoreLookup = scoreLookup,
                    HealthLookup = healthLookup,
                    FragmentInventoryLookup = fragmentInventoryLookup,
                    KeyInventoryLookup = keyInventoryLookup,
                    EntityLookupTable = entityLookup
                }.ScheduleParallel(_collectibleEventsQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Job che aggiorna l'animazione dei collezionabili
        /// </summary>
        [BurstCompile]
        private partial struct UpdateCollectiblesAnimationJob : IJobEntity
        {
            public float DeltaTime;
            
            [BurstCompile]
            private void Execute(
                ref TransformComponent transform,
                ref CollectibleComponent collectible)
            {
                // Aggiorna il tempo di animazione
                collectible.AnimationTime += DeltaTime;
                
                // Rotazione
                if (collectible.HasRotation)
                {
                    quaternion rotationDelta = quaternion.AxisAngle(collectible.RotationAxis, 
                                                                   collectible.RotationSpeed * DeltaTime);
                    transform.Rotation = math.mul(transform.Rotation, rotationDelta);
                }
                
                // Fluttuazione verticale
                if (collectible.HasFloating)
                {
                    float bobOffset = math.sin(collectible.AnimationTime * collectible.FloatFrequency) * 
                                     collectible.FloatAmplitude;
                    transform.Position.y = collectible.OriginalHeight + bobOffset;
                }
                
                // Pulsazione (scaling)
                if (collectible.HasPulsation)
                {
                    float pulseFactor = 1.0f + math.sin(collectible.AnimationTime * collectible.PulseFrequency) * 
                                       collectible.PulseAmplitude;
                    transform.Scale = collectible.OriginalScale * pulseFactor;
                }
                
                // Effetto bagliore/luce (impostato solo come flag, l'effetto visivo è gestito dal rendering)
                if (collectible.HasGlow)
                {
                    collectible.GlowIntensity = 0.5f + math.sin(collectible.AnimationTime * collectible.GlowFrequency) * 0.5f;
                }
            }
        }
        
        /// <summary>
        /// Job che aggiorna i collezionabili in risposta agli attrattori magnetici
        /// </summary>
        [BurstCompile]
        private partial struct UpdateMagnetEffectsJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public NativeArray<Entity> MagnetSources;
            [ReadOnly] public NativeArray<TransformComponent> MagnetPositions;
            [ReadOnly] public NativeArray<MagnetSourceComponent> MagnetProperties;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref TransformComponent transform,
                in CollectibleComponent collectible)
            {
                // Verifica che il collezionabile possa essere magnetizzato
                if (!collectible.IsMagnetizable)
                    return;
                
                // Se è già magnetizzato, non fa nulla (gestito da MoveMagnetizedCollectiblesJob)
                if (SystemAPI.HasComponent<MagnetizedTag>(entity))
                    return;
                
                // Controlla ogni sorgente magnetica
                for (int i = 0; i < MagnetSources.Length; i++)
                {
                    var magnetPosition = MagnetPositions[i].Position;
                    var magnetProperties = MagnetProperties[i];
                    
                    // Calcola la distanza dalla sorgente magnetica
                    float3 toMagnet = magnetPosition - transform.Position;
                    float distanceSq = math.lengthsq(toMagnet);
                    float magnetRadiusSq = magnetProperties.Radius * magnetProperties.Radius;
                    
                    // Se è nel raggio del magnete, magnetizza il collezionabile
                    if (distanceSq <= magnetRadiusSq)
                    {
                        // Aggiungi il tag di magnetizzazione
                        ECB.AddComponent<MagnetizedTag>(sortKey, entity);
                        
                        // Aggiungi dati di movimento per questa magnetizzazione
                        ECB.AddComponent(sortKey, entity, new MagnetizationMovementData
                        {
                            TargetEntity = MagnetSources[i],
                            TargetPosition = magnetPosition,
                            MoveSpeed = magnetProperties.PullSpeed,
                            AccelerationRate = magnetProperties.AccelerationRate
                        });
                        
                        // Un singolo magnete è sufficiente, esci dal ciclo
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che muove i collezionabili magnetizzati verso i loro attrattori
        /// </summary>
        [BurstCompile]
        private partial struct MoveMagnetizedCollectiblesJob : IJobEntity
        {
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<TransformComponent> TransformLookup;
            [ReadOnly] public EntityStorageInfoLookup EntityLookupTable;
            
            [BurstCompile]
            private void Execute(
                ref TransformComponent transform,
                in MagnetizationMovementData magnetData)
            {
                // Se l'entità target esiste ancora
                if (EntityLookupTable.Exists(magnetData.TargetEntity))
                {
                    // Aggiorna la posizione target se il magnete si muove
                    float3 targetPosition;
                    if (TransformLookup.HasComponent(magnetData.TargetEntity))
                    {
                        targetPosition = TransformLookup[magnetData.TargetEntity].Position;
                    }
                    else
                    {
                        // Usa l'ultima posizione nota se il componente transform non è disponibile
                        targetPosition = magnetData.TargetPosition;
                    }
                    
                    // Calcola la direzione verso l'attrattore
                    float3 toTarget = targetPosition - transform.Position;
                    float distance = math.length(toTarget);
                    
                    // Se non è già arrivato
                    if (distance > 0.1f)
                    {
                        // Normalizza la direzione
                        float3 direction = toTarget / distance;
                        
                        // Calcola la velocità di movimento, aumentando con la vicinanza
                        float speedFactor = 1.0f + (1.0f - math.min(1.0f, distance / 5.0f)) * magnetData.AccelerationRate;
                        float moveSpeed = magnetData.MoveSpeed * speedFactor;
                        
                        // Sposta verso il target
                        float moveAmount = math.min(moveSpeed * DeltaTime, distance);
                        transform.Position += direction * moveAmount;
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che processa gli eventi di raccolta dei collezionabili
        /// </summary>
        [BurstCompile]
        private partial struct ProcessCollectionEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<CollectibleComponent> CollectibleLookup;
            [ReadOnly] public ComponentLookup<TransformComponent> TransformLookup;
            [ReadOnly] public ComponentLookup<ScoreComponent> ScoreLookup;
            [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
            [ReadOnly] public ComponentLookup<FragmentInventoryComponent> FragmentInventoryLookup;
            [ReadOnly] public ComponentLookup<KeyInventoryComponent> KeyInventoryLookup;
            [ReadOnly] public EntityStorageInfoLookup EntityLookupTable;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                in CollectibleCollectedEvent collectionEvent)
            {
                // Ottiene il riferimento al collezionabile e all'entità che lo ha raccolto
                Entity collectibleEntity = collectionEvent.CollectibleEntity;
                Entity collectorEntity = collectionEvent.CollectorEntity;
                
                if (!EntityLookupTable.Exists(collectibleEntity) || !EntityLookupTable.Exists(collectorEntity))
                {
                    // Entità invalide, distruggi l'evento e termina
                    ECB.DestroyEntity(sortKey, entity);
                    return;
                }
                
                // Ottieni il componente collezionabile
                if (CollectibleLookup.HasComponent(collectibleEntity))
                {
                    var collectible = CollectibleLookup[collectibleEntity];
                    
                    // Applica gli effetti in base al tipo di collezionabile
                    ApplyCollectibleEffects(collectibleEntity, collectorEntity, collectible, ECB, sortKey);
                    
                    // Crea evento di feedback visivo/audio
                    Entity feedbackEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, feedbackEvent, new CollectibleFeedbackEvent
                    {
                        CollectibleType = collectible.CollectibleType,
                        CollectorEntity = collectorEntity,
                        CollectionPoint = TransformLookup.HasComponent(collectibleEntity) ? 
                                          TransformLookup[collectibleEntity].Position : float3.zero,
                        Value = collectible.Value
                    });
                    
                    // Distrugge il collezionabile
                    ECB.DestroyEntity(sortKey, collectibleEntity);
                }
                
                // Distrugge l'evento di raccolta
                ECB.DestroyEntity(sortKey, entity);
            }
            
            private void ApplyCollectibleEffects(Entity collectibleEntity, Entity collectorEntity, 
                                               CollectibleComponent collectible, 
                                               EntityCommandBuffer.ParallelWriter ecb, int sortKey)
            {
                // Applica effetti in base al tipo di collezionabile
                switch (collectible.CollectibleType)
                {
                    case CollectibleType.Coin:
                    case CollectibleType.Gem:
                        // Aggiorna il componente score del collettore
                        if (ScoreLookup.HasComponent(collectorEntity))
                        {
                            var score = ScoreLookup[collectorEntity];
                            score.CurrentScore += collectible.Value;
                            score.TotalCollectibles++;
                            ecb.SetComponent(sortKey, collectorEntity, score);
                            
                            // Crea un evento di aggiornamento punteggio
                            Entity scoreEvent = ecb.CreateEntity(sortKey);
                            ecb.AddComponent(sortKey, scoreEvent, new ScoreUpdatedEvent
                            {
                                PlayerEntity = collectorEntity,
                                NewScore = score.CurrentScore,
                                ScoreIncrement = collectible.Value,
                                ScoreSource = (byte)collectible.CollectibleType
                            });
                        }
                        break;
                        
                    case CollectibleType.HealthPickup:
                        // Ripristina salute al collettore
                        if (HealthLookup.HasComponent(collectorEntity))
                        {
                            var health = HealthLookup[collectorEntity];
                            float originalHealth = health.CurrentHealth;
                            health.CurrentHealth = math.min(health.CurrentHealth + collectible.Value, health.MaxHealth);
                            ecb.SetComponent(sortKey, collectorEntity, health);
                            
                            // Crea un evento di aggiornamento salute
                            Entity healthEvent = ecb.CreateEntity(sortKey);
                            ecb.AddComponent(sortKey, healthEvent, new HealthUpdatedEvent
                            {
                                PlayerEntity = collectorEntity,
                                NewHealth = health.CurrentHealth,
                                HealthIncrement = health.CurrentHealth - originalHealth,
                                IsFullHeal = (health.CurrentHealth >= health.MaxHealth)
                            });
                        }
                        break;
                        
                    case CollectibleType.Fragment:
                        // Aggiungi il frammento all'inventario
                        if (FragmentInventoryLookup.HasComponent(collectorEntity))
                        {
                            var inventory = FragmentInventoryLookup[collectorEntity];
                            
                            // Aggiorna l'inventario in base al tipo di frammento
                            switch (collectible.FragmentType)
                            {
                                case 0: // Urbano
                                    inventory.UrbanFragments++;
                                    break;
                                case 1: // Foresta
                                    inventory.ForestFragments++;
                                    break;
                                case 2: // Tundra
                                    inventory.TundraFragments++;
                                    break;
                                case 3: // Vulcano
                                    inventory.VolcanoFragments++;
                                    break;
                                case 4: // Abisso
                                    inventory.AbyssFragments++;
                                    break;
                                case 5: // Virtuale
                                    inventory.VirtualFragments++;
                                    break;
                            }
                            
                            ecb.SetComponent(sortKey, collectorEntity, inventory);
                            
                            // Crea un evento di frammento raccolto
                            Entity fragmentEvent = ecb.CreateEntity(sortKey);
                            ecb.AddComponent(sortKey, fragmentEvent, new FragmentCollectedEvent
                            {
                                FragmentID = collectible.ItemID,
                                FragmentType = collectible.FragmentType,
                                CollectorEntity = collectorEntity
                            });
                        }
                        break;
                        
                    case CollectibleType.Key:
                        // Aggiunge la chiave all'inventario
                        if (KeyInventoryLookup.HasComponent(collectorEntity))
                        {
                            var keyInventory = KeyInventoryLookup[collectorEntity];
                            
                            // Aggiorna l'inventario chiavi in base al tipo di chiave
                            switch (collectible.KeyType)
                            {
                                case 0: // Comune
                                    keyInventory.CommonKeys++;
                                    break;
                                case 1: // Rara
                                    keyInventory.RareKeys++;
                                    break;
                                case 2: // Epica
                                    keyInventory.EpicKeys++;
                                    break;
                                case 3: // Leggendaria
                                    keyInventory.LegendaryKeys++;
                                    break;
                            }
                            
                            ecb.SetComponent(sortKey, collectorEntity, keyInventory);
                            
                            // Crea un evento di chiave raccolta
                            Entity keyEvent = ecb.CreateEntity(sortKey);
                            ecb.AddComponent(sortKey, keyEvent, new KeyCollectedEvent
                            {
                                KeyID = collectible.ItemID,
                                KeyType = collectible.KeyType,
                                CollectorEntity = collectorEntity
                            });
                        }
                        break;
                        
                    case CollectibleType.PowerupPickup:
                        // Crea un evento di powerup raccolto
                        Entity powerupEvent = ecb.CreateEntity(sortKey);
                        ecb.AddComponent(sortKey, powerupEvent, new PowerupCollectedEvent
                        {
                            PowerupEntity = collectibleEntity,
                            CollectorEntity = collectorEntity
                        });
                        break;
                }
            }
        }
    }
    
    #region Componenti e Eventi CollectibleSystem
    
    /// <summary>
    /// Tipi di collezionabili
    /// </summary>
    public enum CollectibleType : byte
    {
        Coin = 0,
        Gem = 1,
        HealthPickup = 2,
        Fragment = 3,
        Key = 4,
        PowerupPickup = 5
    }
    
    /// <summary>
    /// Componente che definisce un collezionabile
    /// </summary>
    [System.Serializable]
    public struct CollectibleComponent : IComponentData
    {
        // Proprietà base
        public CollectibleType CollectibleType;  // Tipo di collezionabile
        public int ItemID;                       // ID univoco dell'item (per tracking)
        public float Value;                      // Valore del collezionabile (monete, salute, ecc.)
        public byte Rarity;                      // Rarità (0=comune, 1=raro, 2=epico, 3=leggendario)
        
        // Proprietà specifiche per tipo
        public byte FragmentType;                // Tipo di frammento (0=Urbano, 1=Foresta, ecc.)
        public byte KeyType;                     // Tipo di chiave (0=comune, 1=rara, ecc.)
        
        // Proprietà visive
        public bool HasRotation;                 // Se ruota
        public float3 RotationAxis;              // Asse di rotazione
        public float RotationSpeed;              // Velocità di rotazione
        
        public bool HasFloating;                 // Se fluttua verticalmente
        public float FloatAmplitude;             // Ampiezza di fluttuazione
        public float FloatFrequency;             // Frequenza di fluttuazione
        public float OriginalHeight;             // Altezza base per fluttuazione
        
        public bool HasPulsation;                // Se pulsa (cambia scala)
        public float PulseAmplitude;             // Ampiezza pulsazione
        public float PulseFrequency;             // Frequenza pulsazione
        public float3 OriginalScale;             // Scala base per pulsazione
        
        public bool HasGlow;                     // Se ha bagliore
        public float GlowFrequency;              // Frequenza bagliore
        public float GlowIntensity;              // Intensità bagliore
        
        // Proprietà fisiche 
        public bool IsMagnetizable;              // Se può essere attratto da magneti
        public bool HasCollision;                // Se ha collisioni fisiche
        
        // Timer e stato
        public float AnimationTime;              // Timer per animazioni
        public bool IsPlaced;                    // Se è stato piazzato permanentemente o generato dinamicamente
    }
    
    /// <summary>
    /// Tag per collezionabili magnetizzati
    /// </summary>
    [System.Serializable]
    public struct MagnetizedTag : IComponentData {}
    
    /// <summary>
    /// Dati di movimento per collezionabili magnetizzati
    /// </summary>
    [System.Serializable]
    public struct MagnetizationMovementData : IComponentData
    {
        public Entity TargetEntity;      // Entità del magnete target
        public float3 TargetPosition;    // Posizione del target
        public float MoveSpeed;          // Velocità di movimento
        public float AccelerationRate;   // Tasso di accelerazione all'avvicinarsi
    }
    
    /// <summary>
    /// Componente che definisce una sorgente magnetica
    /// </summary>
    [System.Serializable]
    public struct MagnetSourceComponent : IComponentData
    {
        public float Radius;             // Raggio di attrazione
        public float PullSpeed;          // Velocità di attrazione base
        public float AccelerationRate;   // Tasso di accelerazione all'avvicinarsi
        public bool IsActive;            // Se la sorgente è attiva
        public float Duration;           // Durata (0 = permanente)
        public float RemainingTime;      // Tempo rimanente
    }
    
    /// <summary>
    /// Evento generato quando viene raccolto un collezionabile
    /// </summary>
    [System.Serializable]
    public struct CollectibleCollectedEvent : IComponentData
    {
        public Entity CollectibleEntity;  // Entità del collezionabile
        public Entity CollectorEntity;    // Entità che ha raccolto
    }
    
    /// <summary>
    /// Evento per effetti visivi/audio di raccolta collezionabile
    /// </summary>
    [System.Serializable]
    public struct CollectibleFeedbackEvent : IComponentData
    {
        public CollectibleType CollectibleType;  // Tipo di collezionabile
        public Entity CollectorEntity;           // Entità che ha raccolto
        public float3 CollectionPoint;           // Punto di raccolta
        public float Value;                      // Valore del collezionabile
    }
    
    /// <summary>
    /// Eventi specifici per tipo di collezionabile
    /// </summary>
    
    [System.Serializable]
    public struct ScoreUpdatedEvent : IComponentData
    {
        public Entity PlayerEntity;      // Entità del giocatore
        public float NewScore;           // Nuovo punteggio
        public float ScoreIncrement;     // Incremento
        public byte ScoreSource;         // Origine dell'incremento
    }
    
    [System.Serializable]
    public struct HealthUpdatedEvent : IComponentData
    {
        public Entity PlayerEntity;      // Entità del giocatore
        public float NewHealth;          // Nuova salute
        public float HealthIncrement;    // Incremento
        public bool IsFullHeal;          // Se è un ripristino completo
    }
    
    [System.Serializable]
    public struct KeyCollectedEvent : IComponentData
    {
        public int KeyID;                // ID della chiave
        public byte KeyType;             // Tipo di chiave
        public Entity CollectorEntity;   // Entità che ha raccolto
    }
    
    /// <summary>
    /// Componente per inventario frammenti
    /// </summary>
    [System.Serializable]
    public struct FragmentInventoryComponent : IComponentData
    {
        public byte UrbanFragments;      // Frammenti Urban
        public byte ForestFragments;     // Frammenti Forest
        public byte TundraFragments;     // Frammenti Tundra
        public byte VolcanoFragments;    // Frammenti Vulcano
        public byte AbyssFragments;      // Frammenti Abisso
        public byte VirtualFragments;    // Frammenti Virtual
        
        public byte GetFragmentCount(byte fragmentType)
        {
            switch (fragmentType)
            {
                case 0: return UrbanFragments;
                case 1: return ForestFragments;
                case 2: return TundraFragments;
                case 3: return VolcanoFragments;
                case 4: return AbyssFragments;
                case 5: return VirtualFragments;
                default: return 0;
            }
        }
    }
    
    /// <summary>
    /// Componente per inventario chiavi
    /// </summary>
    [System.Serializable]
    public struct KeyInventoryComponent : IComponentData
    {
        public byte CommonKeys;          // Chiavi comuni
        public byte RareKeys;            // Chiavi rare
        public byte EpicKeys;            // Chiavi epiche
        public byte LegendaryKeys;       // Chiavi leggendarie
        
        public byte GetKeyCount(byte keyType)
        {
            switch (keyType)
            {
                case 0: return CommonKeys;
                case 1: return RareKeys;
                case 2: return EpicKeys;
                case 3: return LegendaryKeys;
                default: return 0;
            }
        }
    }
    
    #endregion
}
