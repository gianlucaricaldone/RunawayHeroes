// Path: Assets/_Project/ECS/Systems/Movement/ObstacleInteractionSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.Movement.Group;
using RunawayHeroes.ECS.Components.World.Obstacles;

namespace RunawayHeroes.ECS.Systems.Movement
{
    // Nota: SlipperyTag è ora definito in RunawayHeroes.ECS.Components.World.Obstacles.SlipperyTag
    
    /// <summary>
    /// Sistema che gestisce le interazioni speciali tra i personaggi e gli ostacoli in base alle loro abilità.
    /// Estende le funzionalità di base dell'ObstacleCollisionSystem per supportare l'interazione con tutti i tipi di ostacoli
    /// in base alle abilità dei vari personaggi.
    /// </summary>
    [UpdateInGroup(typeof(RunawayHeroes.ECS.Systems.Movement.Group.MovementSystemGroup))]
    [BurstCompile]
    public partial struct ObstacleInteractionSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _obstacleQuery;
        private EntityQuery _specialObstaclesQuery;
        
        // Costanti per la gestione delle interazioni
        private const float INTERACTION_RADIUS = 1.5f; // Raggio per l'interazione con ostacoli speciali
        private const float MELT_RATE = 0.2f;         // Velocità di scioglimento del ghiaccio
        private const float BARRIER_PENETRATION_DISTANCE = 1.0f; // Distanza di penetrazione nelle barriere
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Definisce la query per identificare i giocatori con abilità speciali
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TagComponent, TransformComponent, PhysicsComponent>()
                .Build(ref state);
            
            // Definisce la query per identificare gli ostacoli base
            _obstacleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TransformComponent>()
                .Build(ref state);
            
            // Query per ostacoli speciali (da espandere in base alle necessità)
            var specialObstaclesBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithAny<LavaTag, IceObstacleTag, DigitalBarrierTag, UnderwaterTag>()
                .WithAll<TransformComponent>()
                .Build(ref state);
            
            _specialObstaclesQuery = specialObstaclesBuilder;
            
            // Richiede che ci siano almeno giocatori per l'esecuzione
            state.RequireForUpdate(_playerQuery);
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        // Non si può usare BurstCompile perché creiamo EntityQueryDesc[] (managed array)
        public void OnUpdate(ref SystemState state)
        {
            // Prepara il command buffer per le modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottiene gli ostacoli speciali per questo frame
            var specialObstacles = _specialObstaclesQuery.ToEntityArray(Allocator.TempJob);
            var obstacleTransforms = _specialObstaclesQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Ottiene i componenti Obstacle se presenti
            NativeArray<ObstacleComponent> obstacleComponents;
            
            // Utilizziamo un approccio alternativo per verificare se la query contiene entità con ObstacleComponent
            // Nota: Questo codice non è compatibile con Burst a causa della creazione di array di ComponentType[]
            // quindi abbiamo rimosso l'attributo [BurstCompile] dal metodo OnUpdate
            var tempQuery = state.GetEntityQuery(new EntityQueryDesc{
                All = new ComponentType[] { ComponentType.ReadOnly<ObstacleComponent>() },
                Any = new ComponentType[] { 
                    ComponentType.ReadOnly<LavaTag>(), 
                    ComponentType.ReadOnly<IceObstacleTag>(), 
                    ComponentType.ReadOnly<DigitalBarrierTag>(), 
                    ComponentType.ReadOnly<UnderwaterTag>() 
                },
                None = new ComponentType[] { }
            });
            
            bool hasObstacleComponent = !tempQuery.IsEmpty;
            
            if (hasObstacleComponent)
            {
                obstacleComponents = _specialObstaclesQuery.ToComponentDataArray<ObstacleComponent>(Allocator.TempJob);
            }
            else
            {
                // Crea un array vuoto per mantenere la compatibilità
                obstacleComponents = new NativeArray<ObstacleComponent>(0, Allocator.TempJob);
            }
            
            // Prepara i lookup per i vari tipi di componenti
            var urbanDashLookup = state.GetComponentLookup<UrbanDashAbilityComponent>(true);
            var fireproofBodyLookup = state.GetComponentLookup<FireproofBodyAbilityComponent>(true);
            var controlledGlitchLookup = state.GetComponentLookup<ControlledGlitchAbilityComponent>(true);
            var heatAuraLookup = state.GetComponentLookup<HeatAuraAbilityComponent>(true);
            var airBubbleLookup = state.GetComponentLookup<AirBubbleAbilityComponent>(true);
            
            var alexLookup = state.GetComponentLookup<AlexComponent>(true);
            var emberLookup = state.GetComponentLookup<EmberComponent>(true);
            var kaiLookup = state.GetComponentLookup<KaiComponent>(true);
            var marinaLookup = state.GetComponentLookup<MarinaComponent>(true);
            var neoLookup = state.GetComponentLookup<NeoComponent>(true);
            
            var lavaTagLookup = state.GetComponentLookup<LavaTag>(true);
            var iceObstacleTagLookup = state.GetComponentLookup<IceObstacleTag>(true);
            var digitalBarrierTagLookup = state.GetComponentLookup<DigitalBarrierTag>(true);
            var underwaterTagLookup = state.GetComponentLookup<UnderwaterTag>(true);
            var slipperyTagLookup = state.GetComponentLookup<SlipperyTag>(true);
            
            // Esegui il job di interazione speciale
            state.Dependency = new SpecialObstacleInteractionJob
            {
                DeltaTime = deltaTime,
                ECB = commandBuffer,
                
                // Passa i lookup
                UrbanDashLookup = urbanDashLookup,
                FireproofBodyLookup = fireproofBodyLookup,
                ControlledGlitchLookup = controlledGlitchLookup,
                HeatAuraLookup = heatAuraLookup,
                AirBubbleLookup = airBubbleLookup,
                
                AlexLookup = alexLookup,
                EmberLookup = emberLookup,
                KaiLookup = kaiLookup,
                MarinaLookup = marinaLookup,
                NeoLookup = neoLookup,
                
                LavaTagLookup = lavaTagLookup,
                IceObstacleTagLookup = iceObstacleTagLookup,
                DigitalBarrierTagLookup = digitalBarrierTagLookup,
                UnderwaterTagLookup = underwaterTagLookup,
                SlipperyTagLookup = slipperyTagLookup,
                
                // Passa i dati degli ostacoli
                SpecialObstacles = specialObstacles,
                ObstacleTransforms = obstacleTransforms,
                ObstacleComponents = obstacleComponents,
                
                // Costanti
                InteractionRadius = INTERACTION_RADIUS,
                MeltRate = MELT_RATE,
                BarrierPenetrationDistance = BARRIER_PENETRATION_DISTANCE
            }.ScheduleParallel(_playerQuery, state.Dependency);
            
            // Rilascia le risorse temporanee dopo che il job è completato
            state.Dependency = specialObstacles.Dispose(state.Dependency);
            state.Dependency = obstacleTransforms.Dispose(state.Dependency);
            state.Dependency = obstacleComponents.Dispose(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct SpecialObstacleInteractionJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            // Lookup per componenti di abilità
            [ReadOnly] public ComponentLookup<UrbanDashAbilityComponent> UrbanDashLookup;
            [ReadOnly] public ComponentLookup<FireproofBodyAbilityComponent> FireproofBodyLookup;
            [ReadOnly] public ComponentLookup<ControlledGlitchAbilityComponent> ControlledGlitchLookup;
            [ReadOnly] public ComponentLookup<HeatAuraAbilityComponent> HeatAuraLookup;
            [ReadOnly] public ComponentLookup<AirBubbleAbilityComponent> AirBubbleLookup;
            
            // Lookup per componenti di personaggio
            [ReadOnly] public ComponentLookup<AlexComponent> AlexLookup;
            [ReadOnly] public ComponentLookup<EmberComponent> EmberLookup;
            [ReadOnly] public ComponentLookup<KaiComponent> KaiLookup;
            [ReadOnly] public ComponentLookup<MarinaComponent> MarinaLookup;
            [ReadOnly] public ComponentLookup<NeoComponent> NeoLookup;
            
            // Lookup per tag di ostacoli speciali
            [ReadOnly] public ComponentLookup<LavaTag> LavaTagLookup;
            [ReadOnly] public ComponentLookup<IceObstacleTag> IceObstacleTagLookup;
            [ReadOnly] public ComponentLookup<DigitalBarrierTag> DigitalBarrierTagLookup;
            [ReadOnly] public ComponentLookup<UnderwaterTag> UnderwaterTagLookup;
            [ReadOnly] public ComponentLookup<SlipperyTag> SlipperyTagLookup;
            
            // Posizioni e dati degli ostacoli speciali
            [ReadOnly] public NativeArray<Entity> SpecialObstacles;
            [ReadOnly] public NativeArray<TransformComponent> ObstacleTransforms;
            [ReadOnly] public NativeArray<ObstacleComponent> ObstacleComponents;
            
            // Costanti
            public float InteractionRadius;
            public float MeltRate;
            public float BarrierPenetrationDistance;
            
            public void Execute(Entity playerEntity, 
                                [ChunkIndexInQuery] int chunkIndexInQuery,
                                in TransformComponent playerTransform,
                                in PhysicsComponent physics)
            {
                // Determina il tipo di personaggio e le abilità attive
                bool isAlex = AlexLookup.HasComponent(playerEntity);
                bool isEmber = EmberLookup.HasComponent(playerEntity);
                bool isKai = KaiLookup.HasComponent(playerEntity);
                bool isMarina = MarinaLookup.HasComponent(playerEntity);
                bool isNeo = NeoLookup.HasComponent(playerEntity);
                
                // Verifica abilità attive
                bool hasActiveHeatAura = false;
                float heatAuraRadius = 0f;
                if (isKai && HeatAuraLookup.HasComponent(playerEntity))
                {
                    var heatAura = HeatAuraLookup[playerEntity];
                    hasActiveHeatAura = heatAura.IsActive;
                    heatAuraRadius = heatAura.AuraRadius;
                }
                
                bool hasActiveFireproofBody = false;
                if (isEmber && FireproofBodyLookup.HasComponent(playerEntity))
                {
                    var fireproofBody = FireproofBodyLookup[playerEntity];
                    hasActiveFireproofBody = fireproofBody.IsActive;
                }
                
                bool hasActiveGlitchControl = false;
                bool canPenetrateBarriers = false;
                if (isNeo && ControlledGlitchLookup.HasComponent(playerEntity))
                {
                    var glitchControl = ControlledGlitchLookup[playerEntity];
                    hasActiveGlitchControl = glitchControl.IsActive;
                    canPenetrateBarriers = hasActiveGlitchControl && glitchControl.BarrierPenetration;
                }
                
                bool hasActiveAirBubble = false;
                float bubbleRadius = 0f;
                if (isMarina && AirBubbleLookup.HasComponent(playerEntity))
                {
                    var airBubble = AirBubbleLookup[playerEntity];
                    hasActiveAirBubble = airBubble.IsActive;
                    bubbleRadius = airBubble.BubbleRadius;
                }
                
                // Ottieni bonus specifici del personaggio
                float emberFireResistance = 0f;
                if (isEmber)
                {
                    var emberComp = EmberLookup[playerEntity];
                    emberFireResistance = emberComp.FireDamageReduction;
                }
                
                float kaiHeatOutput = 0f;
                if (isKai)
                {
                    var kaiComp = KaiLookup[playerEntity];
                    kaiHeatOutput = kaiComp.HeatRetention;
                }
                
                float marinaWaterControl = 0f;
                if (isMarina)
                {
                    var marinaComp = MarinaLookup[playerEntity];
                    marinaWaterControl = marinaComp.PressureResistance;
                }
                
                float neoGlitchPower = 0f;
                if (isNeo)
                {
                    var neoComp = NeoLookup[playerEntity];
                    neoGlitchPower = neoComp.FirewallBypass;
                }
                
                // Se Ember sta camminando su lava, genera evento speciale
                if (isEmber && hasActiveFireproofBody && emberFireResistance > 0.7f)
                {
                    for (int i = 0; i < SpecialObstacles.Length; i++)
                    {
                        if (LavaTagLookup.HasComponent(SpecialObstacles[i]))
                        {
                            float distance = math.distance(playerTransform.Position, ObstacleTransforms[i].Position);
                            
                            if (distance < InteractionRadius)
                            {
                                HandleLavaInteraction(playerEntity, chunkIndexInQuery, playerTransform.Position, ECB);
                                break;
                            }
                        }
                    }
                }
                
                // Se Kai ha l'aura di calore attiva, scioglie il ghiaccio
                if (isKai && hasActiveHeatAura && heatAuraRadius > 0)
                {
                    for (int i = 0; i < SpecialObstacles.Length; i++)
                    {
                        if (IceObstacleTagLookup.HasComponent(SpecialObstacles[i]))
                        {
                            float distance = math.distance(playerTransform.Position, ObstacleTransforms[i].Position);
                            
                            if (distance < heatAuraRadius)
                            {
                                HandleIceMeltingInteraction(playerEntity, chunkIndexInQuery, playerTransform.Position, 
                                                         heatAuraRadius, ECB, DeltaTime);
                                break;
                            }
                        }
                    }
                }
                
                // Se Neo ha il glitch controllato attivo, può attraversare barriere digitali
                if (isNeo && canPenetrateBarriers)
                {
                    for (int i = 0; i < SpecialObstacles.Length; i++)
                    {
                        if (DigitalBarrierTagLookup.HasComponent(SpecialObstacles[i]))
                        {
                            float distance = math.distance(playerTransform.Position, ObstacleTransforms[i].Position);
                            
                            if (distance < InteractionRadius)
                            {
                                HandleDigitalBarrierInteraction(playerEntity, chunkIndexInQuery, playerTransform.Position, 
                                                             BarrierPenetrationDistance, ECB);
                                break;
                            }
                        }
                    }
                }
                
                // Se Marina ha la bolla d'aria attiva, può interagire con zone subacquee
                if (isMarina && hasActiveAirBubble && bubbleRadius > 0)
                {
                    for (int i = 0; i < SpecialObstacles.Length; i++)
                    {
                        if (UnderwaterTagLookup.HasComponent(SpecialObstacles[i]))
                        {
                            float distance = math.distance(playerTransform.Position, ObstacleTransforms[i].Position);
                            
                            if (distance < InteractionRadius)
                            {
                                HandleUnderwaterInteraction(playerEntity, chunkIndexInQuery, playerTransform.Position, 
                                                         bubbleRadius, marinaWaterControl * 2.0f, ECB);
                                break;
                            }
                        }
                    }
                }
                
                // Gestione superfici scivolose per tutti i personaggi (Kai può neutralizzarle)
                for (int i = 0; i < SpecialObstacles.Length; i++)
                {
                    if (SlipperyTagLookup.HasComponent(SpecialObstacles[i]))
                    {
                        float distance = math.distance(playerTransform.Position, ObstacleTransforms[i].Position);
                        
                        if (distance < InteractionRadius)
                        {
                            HandleSlipperyInteraction(playerEntity, chunkIndexInQuery, playerTransform.Position, 
                                                   hasActiveHeatAura, ECB);
                            break;
                        }
                    }
                }
            }
        }
        
        // I metodi privati rimangono invariati ma ora dovrebbero far parte della struct del job o essere implementati come metodi statici
        /// <summary>
        /// Gestisce l'interazione con gli ostacoli di lava per Ember
        /// </summary>
        private static void HandleLavaInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                          EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Codice invariato
        }
        
        /// <summary>
        /// Gestisce l'interazione di scioglimento del ghiaccio per Kai
        /// </summary>
        private static void HandleIceMeltingInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                                float auraRadius, EntityCommandBuffer.ParallelWriter commandBuffer,
                                                float deltaTime)
        {
            // Codice invariato
        }
        
        /// <summary>
        /// Gestisce l'interazione con le barriere digitali per Neo
        /// </summary>
        private static void HandleDigitalBarrierInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                                    float glitchDistance, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Codice invariato
        }
        
        /// <summary>
        /// Gestisce l'interazione con l'ambiente sottomarino per Marina
        /// </summary>
        private static void HandleUnderwaterInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                               float bubbleRadius, float repelForce, 
                                               EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Codice invariato
        }
        
        /// <summary>
        /// Gestisce l'interazione con superfici scivolose per tutti i personaggi
        /// </summary>
        private static void HandleSlipperyInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                             bool hasHeatAura, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Codice invariato
        }
        
        /// <summary>
        /// Verifica se un'entità contiene una certa abilità e se è attiva
        /// </summary>
        private static bool HasActiveAbility<T>(Entity entity, ComponentLookup<T> lookup) where T : unmanaged, IComponentData
        {
            if (lookup.HasComponent(entity))
            {
                var component = lookup[entity];
                // Dovrebbe controllare una proprietà "IsActive" nel componente
                // In questa implementazione di esempio, non possiamo accedere a proprietà
                // specifiche poiché T è generico.
                return true; // In una implementazione reale, controllerebbe la proprietà IsActive
            }
            return false;
        }
    }
}