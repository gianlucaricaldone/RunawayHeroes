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
    /// Sistema migliorato per gestire le collisioni tra il giocatore e gli ostacoli.
    /// Incorpora le abilità speciali dei personaggi direttamente nel sistema di collisione principale.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial class EnhancedObstacleCollisionSystem : SystemBase
    {
        private EntityQuery _playerQuery;
        private EntityQuery _obstacleQuery;
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        
        // Costanti di configurazione
        private const float OBSTACLE_DAMAGE_MULTIPLIER = 10.0f;  // Moltiplicatore del danno basato sulla velocità
        private const float MIN_IMPACT_VELOCITY = 2.0f;          // Velocità minima per considerare un impatto
        private const float SMALL_OBSTACLE_THRESHOLD = 0.5f;     // Soglia per definire un ostacolo piccolo
        private const float PLAYER_RADIUS = 0.5f;                // Raggio di collisione del giocatore
        
        protected override void OnCreate()
        {
            // Ottiene il sistema di command buffer per le modifiche strutturali
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Definisce la query per identificare le entità giocatore
            _playerQuery = GetEntityQuery(
                ComponentType.ReadOnly<TagComponent>(),
                ComponentType.ReadOnly<TransformComponent>(),
                ComponentType.ReadOnly<PhysicsComponent>(),
                ComponentType.ReadWrite<HealthComponent>()
            );
            
            // Definisce la query per identificare gli ostacoli
            _obstacleQuery = GetEntityQuery(
                ComponentType.ReadOnly<ObstacleComponent>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Richiede che ci siano sia giocatori che ostacoli per l'esecuzione
            RequireForUpdate(_playerQuery);
            RequireForUpdate(_obstacleQuery);
        }
        
        [BurstCompile]
        protected override void OnUpdate()
        {
            // Prepara il command buffer per le modifiche strutturali
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Ottiene le entità di ostacoli per questo frame
            NativeArray<Entity> obstacles = _obstacleQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<ObstacleComponent> obstacleComponents = _obstacleQuery.ToComponentDataArray<ObstacleComponent>(Allocator.TempJob);
            NativeArray<TransformComponent> obstacleTransforms = _obstacleQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Elabora le collisioni per ogni giocatore
            Entities
                .WithName("ProcessPlayerObstacleCollisions")
                .WithReadOnly(obstacles)
                .WithReadOnly(obstacleComponents)
                .WithReadOnly(obstacleTransforms)
                .WithAll<TagComponent>()
                .ForEach((Entity playerEntity, int entityInQueryIndex,
                          ref HealthComponent health,
                          in TransformComponent transform,
                          in PhysicsComponent physics,
                          in MovementComponent movement) =>
                {
                    // Determina le abilità attive del giocatore
                    bool isInvulnerable = health.IsInvulnerable;
                    bool hasActiveUrbanDash = false;
                    float urbanDashBreakForce = 0;
                    bool hasActiveFireproofBody = false;
                    bool hasActiveGlitchControl = false;
                    bool hasActiveHeatAura = false;
                    float heatAuraRadius = 0;
                    bool hasActiveAirBubble = false;
                    
                    // Controlla abilità Urban Dash (Alex)
                    if (HasComponent<UrbanDashAbilityComponent>(playerEntity))
                    {
                        var urbanDash = GetComponent<UrbanDashAbilityComponent>(playerEntity);
                        hasActiveUrbanDash = urbanDash.IsActive;
                        urbanDashBreakForce = urbanDash.BreakThroughForce;
                        
                        // Urban Dash conferisce temporaneamente invulnerabilità
                        if (hasActiveUrbanDash)
                            isInvulnerable = true;
                    }
                    
                    // Controlla abilità Corpo Ignifugo (Ember)
                    if (HasComponent<FireproofBodyAbilityComponent>(playerEntity))
                    {
                        var fireproofBody = GetComponent<FireproofBodyAbilityComponent>(playerEntity);
                        hasActiveFireproofBody = fireproofBody.IsActive;
                    }
                    
                    // Controlla abilità Glitch Controllato (Neo)
                    if (HasComponent<ControlledGlitchAbilityComponent>(playerEntity))
                    {
                        var glitchControl = GetComponent<ControlledGlitchAbilityComponent>(playerEntity);
                        hasActiveGlitchControl = glitchControl.IsActive && glitchControl.BarrierPenetration;
                    }
                    
                    // Controlla abilità Aura di Calore (Kai)
                    if (HasComponent<HeatAuraAbilityComponent>(playerEntity))
                    {
                        var heatAura = GetComponent<HeatAuraAbilityComponent>(playerEntity);
                        hasActiveHeatAura = heatAura.IsActive;
                        heatAuraRadius = heatAura.AuraRadius;
                    }
                    
                    // Controlla abilità Bolla d'Aria (Marina)
                    if (HasComponent<AirBubbleAbilityComponent>(playerEntity))
                    {
                        var airBubble = GetComponent<AirBubbleAbilityComponent>(playerEntity);
                        hasActiveAirBubble = airBubble.IsActive;
                    }
                    
                    // Salta se il giocatore è invulnerabile
                    if (isInvulnerable)
                        return;
                    
                    // Ottieni bonus specifici per personaggio
                    float obstacleBreakThroughBonus = 0f;
                    float fireResistance = 0f;
                    float iceResistance = 0f;
                    float waterResistance = 0f;
                    float digitalBarrierBypassChance = 0f;
                    
                    // Alex: bonus sfondamento
                    if (HasComponent<AlexComponent>(playerEntity))
                    {
                        var alexComp = GetComponent<AlexComponent>(playerEntity);
                        obstacleBreakThroughBonus = alexComp.ObstacleBreakThroughBonus;
                    }
                    
                    // Ember: resistenza al fuoco/lava
                    if (HasComponent<EmberComponent>(playerEntity))
                    {
                        var emberComp = GetComponent<EmberComponent>(playerEntity);
                        fireResistance = emberComp.FireDamageReduction;
                    }
                    
                    // Kai: resistenza al ghiaccio
                    if (HasComponent<KaiComponent>(playerEntity))
                    {
                        var kaiComp = GetComponent<KaiComponent>(playerEntity);
                        iceResistance = kaiComp.ColdResistance;
                    }
                    
                    // Marina: resistenza acquatica
                    if (HasComponent<MarinaComponent>(playerEntity))
                    {
                        var marinaComp = GetComponent<MarinaComponent>(playerEntity);
                        waterResistance = marinaComp.PressureResistance;
                    }
                    
                    // Neo: bypass barriere
                    if (HasComponent<NeoComponent>(playerEntity))
                    {
                        var neoComp = GetComponent<NeoComponent>(playerEntity);
                        digitalBarrierBypassChance = neoComp.FirewallBypass;
                    }
                    
                    // Verifica collisioni con tutti gli ostacoli
                    for (int i = 0; i < obstacles.Length; i++)
                    {
                        Entity obstacleEntity = obstacles[i];
                        ObstacleComponent obstacle = obstacleComponents[i];
                        TransformComponent obstacleTransform = obstacleTransforms[i];
                        
                        // Controllo collisione semplificato (box vs box)
                        if (CheckCollision(transform, obstacleTransform, obstacle.CollisionRadius))
                        {
                            // Calcola i dettagli della collisione
                            float3 impactPosition = CalculateImpactPosition(transform.Position, obstacleTransform.Position);
                            float3 impactNormal = math.normalize(transform.Position - obstacleTransform.Position);
                            float impactVelocity = math.dot(physics.Velocity, -impactNormal);
                            
                            // Ignora impatti a bassa velocità (sfioramenti)
                            if (impactVelocity < MIN_IMPACT_VELOCITY)
                                continue;
                            
                            // Determina il danno basato sulla velocità e tipo di ostacolo
                            float baseDamage = obstacle.DamageValue;
                            if (baseDamage <= 0)
                            {
                                // Se il danno non è specificato, calcolalo dalla velocità
                                baseDamage = impactVelocity * OBSTACLE_DAMAGE_MULTIPLIER;
                            }
                            
                            // Applica modificatori in base al tipo di ostacolo e abilità del personaggio
                            bool canBreakThrough = false;
                            bool isImmune = false;
                            
                            // Gestione di ostacoli da sfondare
                            if (obstacle.IsDestructible)
                            {
                                // Sfondamento tramite scivolata per ostacoli piccoli
                                if (movement.IsSliding && obstacle.Height < SMALL_OBSTACLE_THRESHOLD)
                                {
                                    canBreakThrough = true;
                                }
                                
                                // Sfondamento tramite Urban Dash (Alex)
                                if (hasActiveUrbanDash && (urbanDashBreakForce + obstacleBreakThroughBonus) > obstacle.Strength)
                                {
                                    canBreakThrough = true;
                                }
                            }
                            
                            // Gestione ostacoli speciali in base al tag
                            
                            // Lava (Ember è immune)
                            if (HasComponent<LavaTag>(obstacleEntity))
                            {
                                if (hasActiveFireproofBody || fireResistance >= 0.9f)
                                {
                                    isImmune = true;
                                }
                                else if (fireResistance > 0)
                                {
                                    // Riduzione danno in base alla resistenza
                                    baseDamage *= (1.0f - fireResistance);
                                }
                            }
                            
                            // Ghiaccio (Kai con Aura di Calore può scioglierlo)
                            if (HasComponent<IceObstacleTag>(obstacleEntity))
                            {
                                if (hasActiveHeatAura)
                                {
                                    // Possibilità di sciogliere il ghiaccio
                                    float distSq = math.distancesq(transform.Position, obstacleTransform.Position);
                                    if (distSq < heatAuraRadius * heatAuraRadius)
                                    {
                                        canBreakThrough = true;
                                    }
                                }
                                
                                if (iceResistance > 0)
                                {
                                    // Riduzione danno in base alla resistenza
                                    baseDamage *= (1.0f - iceResistance);
                                }
                            }
                            
                            // Barriere digitali (Neo può attraversarle)
                            if (HasComponent<DigitalBarrierTag>(obstacleEntity))
                            {
                                if (hasActiveGlitchControl || 
                                    (digitalBarrierBypassChance > 0 && 
                                     Unity.Mathematics.Random.CreateFromIndex((uint)entityInQueryIndex).NextFloat() < digitalBarrierBypassChance))
                                {
                                    canBreakThrough = true;
                                    isImmune = true;
                                }
                            }
                            
                            // Zone subacquee (Marina è avvantaggiata)
                            if (HasComponent<UnderwaterTag>(obstacleEntity))
                            {
                                if (hasActiveAirBubble || waterResistance >= 0.9f)
                                {
                                    isImmune = true;
                                }
                                else if (waterResistance > 0)
                                {
                                    // Riduzione danno in base alla resistenza
                                    baseDamage *= (1.0f - waterResistance);
                                }
                            }
                            
                            // Se è immune, salta la collisione
                            if (isImmune)
                            {
                                continue;
                            }
                            
                            // Se non può sfondare, applica danno e genera evento
                            if (!canBreakThrough)
                            {
                                // Applica il danno
                                float actualDamage = health.ApplyDamage(baseDamage);
                                
                                // Genera evento di collisione con ostacolo
                                var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new ObstacleHitEvent
                                {
                                    PlayerEntity = playerEntity,
                                    ObstacleEntity = obstacleEntity,
                                    ImpactPosition = impactPosition,
                                    ImpactNormal = impactNormal,
                                    ImpactVelocity = impactVelocity,
                                    DamageAmount = actualDamage
                                });
                                
                                // Applica invulnerabilità temporanea per evitare danni multipli dallo stesso ostacolo
                                health.SetInvulnerable(1.0f);
                                
                                // Genera anche un evento di danno generico
                                var damageEventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, damageEventEntity, new DamageEvent
                                {
                                    TargetEntity = playerEntity,
                                    SourceEntity = obstacleEntity,
                                    DamageAmount = actualDamage,
                                    DamageType = DamageType.Obstacle,
                                    ImpactPosition = impactPosition
                                });
                            }
                            else
                            {
                                // Genera evento di attraversamento ostacolo
                                var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                                commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new ObstacleBreakThroughEvent
                                {
                                    PlayerEntity = playerEntity,
                                    ObstacleEntity = obstacleEntity,
                                    BreakThroughPosition = impactPosition
                                });
                                
                                // Se l'ostacolo è distruttibile e lo sfonda, segna per la distruzione
                                if (obstacle.IsDestructible)
                                {
                                    // Potrebbe essere necessario controllare qui la forza di sfondamento
                                    // vs la resistenza dell'ostacolo per determinare se viene distrutto
                                    commandBuffer.DestroyEntity(entityInQueryIndex, obstacleEntity);
                                }
                            }
                            
                            // Interrompe il controllo degli altri ostacoli se ha già colpito uno
                            break;
                        }
                    }
                }).ScheduleParallel();
            
            // Aggiungi il job handle per la produzione del command buffer
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
            
            // Rilascia le risorse temporanee
            Dependency = obstacles.Dispose(Dependency);
            Dependency = obstacleComponents.Dispose(Dependency);
            Dependency = obstacleTransforms.Dispose(Dependency);
        }
        
        /// <summary>
        /// Verifica se c'è una collisione tra giocatore e ostacolo
        /// </summary>
        private bool CheckCollision(TransformComponent playerTransform, TransformComponent obstacleTransform, float obstacleRadius)
        {
            float3 playerPos = playerTransform.Position;
            float3 obstaclePos = obstacleTransform.Position;
            
            // Calcola la distanza al quadrato (più efficiente del calcolo della distanza)
            float distanceSq = math.distancesq(playerPos, obstaclePos);
            
            // Somma dei raggi
            float radiusSum = PLAYER_RADIUS + obstacleRadius;
            
            // Collisione se la distanza è minore della somma dei raggi
            return distanceSq < (radiusSum * radiusSum);
        }
        
        /// <summary>
        /// Calcola la posizione di impatto tra giocatore e ostacolo
        /// </summary>
        private float3 CalculateImpactPosition(float3 playerPos, float3 obstaclePos)
        {
            // Posizione a metà strada tra i due oggetti
            return (playerPos + obstaclePos) * 0.5f;
        }
    }
}