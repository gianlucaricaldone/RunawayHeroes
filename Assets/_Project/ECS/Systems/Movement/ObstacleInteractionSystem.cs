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

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce le interazioni speciali tra i personaggi e gli ostacoli in base alle loro abilità.
    /// Estende le funzionalità di base dell'ObstacleCollisionSystem per supportare l'interazione con tutti i tipi di ostacoli
    /// in base alle abilità dei vari personaggi.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ObstacleCollisionSystem))]
    public partial class ObstacleInteractionSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EntityQuery _obstacleQuery;
        private EntityQuery _specialObstaclesQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Costanti per la gestione delle interazioni
        private const float INTERACTION_RADIUS = 1.5f; // Raggio per l'interazione con ostacoli speciali
        private const float MELT_RATE = 0.2f;         // Velocità di scioglimento del ghiaccio
        private const float BARRIER_PENETRATION_DISTANCE = 1.0f; // Distanza di penetrazione nelle barriere
        
        protected override void OnCreate()
        {
            // Ottiene il sistema di command buffer per le modifiche strutturali
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Definisce la query per identificare i giocatori con abilità speciali
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<TagComponent>(),
                ComponentType.ReadOnly<TransformComponent>(),
                ComponentType.ReadOnly<PhysicsComponent>()
            );
            
            // Definisce la query per identificare gli ostacoli base
            _obstacleQuery = GetEntityQuery(
                ComponentType.ReadOnly<ObstacleComponent>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Query per ostacoli speciali (da espandere in base alle necessità)
            EntityQueryDesc specialObstaclesDesc = new EntityQueryDesc
            {
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<LavaTag>(),
                    ComponentType.ReadOnly<IceObstacleTag>(),
                    ComponentType.ReadOnly<DigitalBarrierTag>(),
                    ComponentType.ReadOnly<UnderwaterTag>()
                },
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<TransformComponent>()
                }
            };
            
            _specialObstaclesQuery = GetEntityQuery(specialObstaclesDesc);
            
            // Richiede che ci siano almeno giocatori per l'esecuzione
            RequireForUpdate(_playerQuery);
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            // Prepara il command buffer per le modifiche strutturali
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            float deltaTime = Time.DeltaTime;
            
            // Processa le interazioni speciali per ogni giocatore
            Entities
                .WithName("ProcessSpecialObstacleInteractions")
                .WithAll<TagComponent>()
                .ForEach((Entity playerEntity, int entityInQueryIndex,
                          in TransformComponent playerTransform,
                          in PhysicsComponent physics) =>
                {
                    // Verifica le abilità speciali del giocatore
                    bool hasFireproofBody = HasComponent<FireproofBodyAbilityComponent>(playerEntity) && 
                                           GetComponent<FireproofBodyAbilityComponent>(playerEntity).IsActive;
                    
                    bool hasHeatAura = HasComponent<HeatAuraAbilityComponent>(playerEntity) &&
                                      GetComponent<HeatAuraAbilityComponent>(playerEntity).IsActive;
                    
                    bool hasAirBubble = HasComponent<AirBubbleAbilityComponent>(playerEntity) && 
                                       GetComponent<AirBubbleAbilityComponent>(playerEntity).IsActive;
                    
                    bool hasGlitch = HasComponent<ControlledGlitchAbilityComponent>(playerEntity) &&
                                    GetComponent<ControlledGlitchAbilityComponent>(playerEntity).IsActive;
                    
                    bool hasUrbanDash = HasComponent<UrbanDashAbilityComponent>(playerEntity) && 
                                       GetComponent<UrbanDashAbilityComponent>(playerEntity).IsActive;
                    
                    float3 playerPos = playerTransform.Position;
                    
                    // 1. Gestione ostacoli di lava per Ember
                    if (hasFireproofBody)
                    {
                        HandleLavaInteraction(playerEntity, entityInQueryIndex, playerPos, commandBuffer);
                    }
                    
                    // 2. Gestione scioglimento ghiaccio per Kai
                    if (hasHeatAura)
                    {
                        var auraComponent = GetComponent<HeatAuraAbilityComponent>(playerEntity);
                        HandleIceMeltingInteraction(playerEntity, entityInQueryIndex, playerPos, 
                                                   auraComponent.AuraRadius, commandBuffer, deltaTime);
                    }
                    
                    // 3. Gestione barriere digitali per Neo
                    if (hasGlitch)
                    {
                        var glitchComponent = GetComponent<ControlledGlitchAbilityComponent>(playerEntity);
                        if (glitchComponent.BarrierPenetration)
                        {
                            HandleDigitalBarrierInteraction(playerEntity, entityInQueryIndex, playerPos, 
                                                          glitchComponent.GlitchDistance, commandBuffer);
                        }
                    }
                    
                    // 4. Gestione respingimento nemici acquatici per Marina
                    if (hasAirBubble)
                    {
                        var bubbleComponent = GetComponent<AirBubbleAbilityComponent>(playerEntity);
                        HandleUnderwaterInteraction(playerEntity, entityInQueryIndex, playerPos, 
                                                  bubbleComponent.BubbleRadius, bubbleComponent.RepelForce, 
                                                  commandBuffer);
                    }
                    
                    // 5. Controllo superfici scivolose per tutti i personaggi
                    HandleSlipperyInteraction(playerEntity, entityInQueryIndex, playerPos, hasHeatAura, commandBuffer);
                    
                }).ScheduleParallel();
            
            // Aggiungi il job handle per la produzione del command buffer
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Gestisce l'interazione con gli ostacoli di lava per Ember
        /// </summary>
        private void HandleLavaInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                          EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Ottiene tutte le entità con tag LavaTag vicine al giocatore
            Entities
                .WithAll<LavaTag>()
                .WithAll<TransformComponent>()
                .ForEach((Entity lavaEntity, in TransformComponent transform) =>
                {
                    // Calcola distanza dalla lava
                    float distSq = math.distancesq(playerPos, transform.Position);
                    
                    // Se il giocatore è sopra/dentro la lava
                    if (distSq < INTERACTION_RADIUS * INTERACTION_RADIUS)
                    {
                        // Genera evento di attraversamento lava
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new LavaWalkingEvent
                        {
                            EntityID = playerEntity,
                            Position = playerPos
                        });
                        
                        // Qui potremmo aggiungere altri effetti, come impronte di passi o effetti VFX
                    }
                }).Run(); // Esegui immediatamente poiché influisce solo sui dati Entity
        }
        
        /// <summary>
        /// Gestisce l'interazione di scioglimento del ghiaccio per Kai
        /// </summary>
        private void HandleIceMeltingInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                                float auraRadius, EntityCommandBuffer.ParallelWriter commandBuffer,
                                                float deltaTime)
        {
            // Ottiene tutte le entità con tag IceObstacleTag vicine al giocatore
            Entities
                .WithAll<IceObstacleTag>()
                .ForEach((Entity iceEntity, ref IceIntegrityComponent integrity, in TransformComponent transform) =>
                {
                    // Calcola distanza dal ghiaccio
                    float distSq = math.distancesq(playerPos, transform.Position);
                    float sqRadius = auraRadius * auraRadius;
                    
                    // Se il ghiaccio è nel raggio dell'aura
                    if (distSq < sqRadius)
                    {
                        // Calcola l'efficacia in base alla distanza (più forte al centro)
                        float effectiveness = 1.0f - (distSq / sqRadius);
                        
                        // Riduce l'integrità del ghiaccio
                        integrity.CurrentIntegrity -= MELT_RATE * effectiveness * deltaTime;
                        
                        // Se l'integrità raggiunge zero, scioglie completamente il ghiaccio
                        if (integrity.CurrentIntegrity <= 0)
                        {
                            // Crea effetto visivo di scioglimento
                            var effectEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            commandBuffer.AddComponent(entityInQueryIndex, effectEntity, new IceMeltEffectComponent
                            {
                                Position = transform.Position,
                                Size = transform.Scale
                            });
                            
                            // Marca l'entità ghiaccio per la distruzione
                            commandBuffer.DestroyEntity(entityInQueryIndex, iceEntity);
                        }
                    }
                }).Run(); // Esegui immediatamente poiché modifica IceIntegrityComponent
        }
        
        /// <summary>
        /// Gestisce l'interazione con le barriere digitali per Neo
        /// </summary>
        private void HandleDigitalBarrierInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                                    float glitchDistance, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Ottiene tutte le entità con tag DigitalBarrierTag vicine al giocatore
            Entities
                .WithAll<DigitalBarrierTag>()
                .WithAll<TransformComponent>()
                .ForEach((Entity barrierEntity, in TransformComponent transform) =>
                {
                    // Calcola distanza dalla barriera
                    float distSq = math.distancesq(playerPos, transform.Position);
                    
                    // Se il giocatore è vicino alla barriera
                    if (distSq < INTERACTION_RADIUS * INTERACTION_RADIUS)
                    {
                        // Calcola direzione di attraversamento
                        float3 direction = math.normalize(playerPos - transform.Position);
                        float3 targetPosition = playerPos + direction * BARRIER_PENETRATION_DISTANCE;
                        
                        // Genera evento di inizio teletrasporto
                        var startEventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, startEventEntity, new GlitchTeleportStartEvent
                        {
                            EntityID = playerEntity,
                            StartPosition = playerPos,
                            TargetPosition = targetPosition,
                            BarrierEntity = barrierEntity
                        });
                        
                        // Potremmo anche spostare direttamente il giocatore se il sistema lo supporta
                    }
                }).Run(); // Esegui immediatamente poiché influisce solo sui dati Entity
        }
        
        /// <summary>
        /// Gestisce l'interazione con l'ambiente sottomarino per Marina
        /// </summary>
        private void HandleUnderwaterInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                               float bubbleRadius, float repelForce, 
                                               EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Ottiene tutte le entità nemiche con tag UnderwaterTag vicine al giocatore
            Entities
                .WithAll<UnderwaterTag>()
                .WithAll<TransformComponent>()
                .WithAll<PhysicsComponent>()
                .ForEach((Entity enemyEntity, ref PhysicsComponent physics, in TransformComponent transform) =>
                {
                    // Calcola distanza dal nemico
                    float distSq = math.distancesq(playerPos, transform.Position);
                    float sqRadius = bubbleRadius * bubbleRadius;
                    
                    // Se il nemico è nel raggio della bolla
                    if (distSq < sqRadius)
                    {
                        // Calcola direzione di repulsione
                        float3 direction = math.normalize(transform.Position - playerPos);
                        
                        // Applica forza di repulsione (proporzionale alla vicinanza)
                        float distFactor = 1.0f - (distSq / sqRadius); // Più forte vicino al centro
                        float actualForce = repelForce * distFactor;
                        
                        // Applica la forza al nemico
                        physics.Velocity += direction * actualForce;
                        
                        // Genera evento di repulsione
                        var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                        commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new EnemyRepulsionEvent
                        {
                            EnemyEntity = enemyEntity,
                            SourceEntity = playerEntity,
                            RepulsionForce = actualForce,
                            Direction = direction
                        });
                    }
                }).Run(); // Esegui immediatamente poiché modifica PhysicsComponent
        }
        
        /// <summary>
        /// Gestisce l'interazione con superfici scivolose per tutti i personaggi
        /// </summary>
        private void HandleSlipperyInteraction(Entity playerEntity, int entityInQueryIndex, float3 playerPos, 
                                             bool hasHeatAura, EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Implementa la logica per superfici scivolose qui
            // Se Kai con Aura di Calore attiva, ignora gli effetti di scivolosità
            // Altrimenti applica modificatore di attrito alle entità giocatore
        }
        
        /// <summary>
        /// Verifica se un'entità contiene una certa abilità e se è attiva
        /// </summary>
        private bool HasActiveAbility<T>(Entity entity) where T : struct, IComponentData
        {
            if (!HasComponent<T>(entity))
                return false;
            
            // Devi implementare questo metodo per ogni tipo di abilità
            // dato che non esiste un'interfaccia comune per verificare se è attiva
            return false;
        }
    }
}