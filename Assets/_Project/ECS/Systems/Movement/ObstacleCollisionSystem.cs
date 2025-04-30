// Path: Assets/_Project/ECS/Systems/Movement/ObstacleCollisionSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce le collisioni tra il giocatore e gli ostacoli.
    /// Rileva collisioni, genera eventi appropriati e applica gli effetti delle collisioni.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlayerMovementSystem))]
    public partial struct ObstacleCollisionSystem : ISystem
    {
        private EntityQuery _playerQuery;
        private EntityQuery _obstacleQuery;
        
        // Costanti di configurazione
        private const float OBSTACLE_DAMAGE_MULTIPLIER = 10.0f;  // Moltiplicatore del danno basato sulla velocità
        private const float MIN_IMPACT_VELOCITY = 2.0f;          // Velocità minima per considerare un impatto
        private const float SMALL_OBSTACLE_THRESHOLD = 0.5f;     // Soglia per definire un ostacolo piccolo
        private const float PLAYER_RADIUS = 0.5f;                // Raggio di collisione del giocatore
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Definisce la query per identificare le entità giocatore
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TagComponent, TransformComponent, PhysicsComponent>()
                .WithAllRW<HealthComponent>()
                .Build(ref state);
            
            // Definisce la query per identificare gli ostacoli
            _obstacleQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, TransformComponent>()
                .Build(ref state);
            
            // Richiede che ci siano sia giocatori che ostacoli per l'esecuzione
            state.RequireForUpdate(_playerQuery);
            state.RequireForUpdate(_obstacleQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Pulizia quando il sistema viene distrutto
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il command buffer dal singleton
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Ottiene le entità di ostacoli per questo frame
            var obstacles = _obstacleQuery.ToEntityArray(Allocator.TempJob);
            var obstacleComponents = _obstacleQuery.ToComponentDataArray<ObstacleComponent>(Allocator.TempJob);
            var obstacleTransforms = _obstacleQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Elabora le collisioni con un job
            state.Dependency = new ProcessCollisionsJob
            {
                Obstacles = obstacles,
                ObstacleComponents = obstacleComponents,
                ObstacleTransforms = obstacleTransforms,
                ECB = commandBuffer.AsParallelWriter(),
                PlayerRadius = PLAYER_RADIUS,
                ObstacleDamageMultiplier = OBSTACLE_DAMAGE_MULTIPLIER,
                MinImpactVelocity = MIN_IMPACT_VELOCITY,
                SmallObstacleThreshold = SMALL_OBSTACLE_THRESHOLD,
                UrbanDashLookup = state.GetComponentLookup<UrbanDashAbilityComponent>(true),
                FireproofBodyLookup = state.GetComponentLookup<FireproofBodyAbilityComponent>(true),
                ControlledGlitchLookup = state.GetComponentLookup<ControlledGlitchAbilityComponent>(true),
                HeatAuraLookup = state.GetComponentLookup<HeatAuraAbilityComponent>(true),
                AirBubbleLookup = state.GetComponentLookup<AirBubbleAbilityComponent>(true),
                AlexLookup = state.GetComponentLookup<AlexComponent>(true),
                EmberLookup = state.GetComponentLookup<EmberComponent>(true),
                KaiLookup = state.GetComponentLookup<KaiComponent>(true),
                MarinaLookup = state.GetComponentLookup<MarinaComponent>(true),
                NeoLookup = state.GetComponentLookup<NeoComponent>(true),
                LavaTagLookup = state.GetComponentLookup<LavaTag>(true),
                IceObstacleTagLookup = state.GetComponentLookup<IceObstacleTag>(true),
                DigitalBarrierTagLookup = state.GetComponentLookup<DigitalBarrierTag>(true),
                UnderwaterTagLookup = state.GetComponentLookup<UnderwaterTag>(true)
            }.Schedule(_playerQuery, state.Dependency);
            
            // Rilascia le risorse temporanee
            state.Dependency = obstacles.Dispose(state.Dependency);
            state.Dependency = obstacleComponents.Dispose(state.Dependency);
            state.Dependency = obstacleTransforms.Dispose(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct ProcessCollisionsJob : IJobEntity
        {
            // Resto del codice invariato
            [ReadOnly] public NativeArray<Entity> Obstacles;
            [ReadOnly] public NativeArray<ObstacleComponent> ObstacleComponents;
            [ReadOnly] public NativeArray<TransformComponent> ObstacleTransforms;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            // Costanti
            public float PlayerRadius;
            public float ObstacleDamageMultiplier;
            public float MinImpactVelocity;
            public float SmallObstacleThreshold;
            
            // Component lookups per verificare se le entità hanno determinati componenti
            [ReadOnly] public ComponentLookup<UrbanDashAbilityComponent> UrbanDashLookup;
            [ReadOnly] public ComponentLookup<FireproofBodyAbilityComponent> FireproofBodyLookup;
            [ReadOnly] public ComponentLookup<ControlledGlitchAbilityComponent> ControlledGlitchLookup;
            [ReadOnly] public ComponentLookup<HeatAuraAbilityComponent> HeatAuraLookup;
            [ReadOnly] public ComponentLookup<AirBubbleAbilityComponent> AirBubbleLookup;
            [ReadOnly] public ComponentLookup<AlexComponent> AlexLookup;
            [ReadOnly] public ComponentLookup<EmberComponent> EmberLookup;
            [ReadOnly] public ComponentLookup<KaiComponent> KaiLookup;
            [ReadOnly] public ComponentLookup<MarinaComponent> MarinaLookup;
            [ReadOnly] public ComponentLookup<NeoComponent> NeoLookup;
            [ReadOnly] public ComponentLookup<LavaTag> LavaTagLookup;
            [ReadOnly] public ComponentLookup<IceObstacleTag> IceObstacleTagLookup;
            [ReadOnly] public ComponentLookup<DigitalBarrierTag> DigitalBarrierTagLookup;
            [ReadOnly] public ComponentLookup<UnderwaterTag> UnderwaterTagLookup;
            
            public void Execute(
                Entity playerEntity, 
                [ChunkIndexInQuery] int entityInQueryIndex,
                ref HealthComponent health,
                in TransformComponent transform,
                in PhysicsComponent physics,
                in MovementComponent movement)
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
                if (UrbanDashLookup.HasComponent(playerEntity))
                {
                    var urbanDash = UrbanDashLookup[playerEntity];
                    hasActiveUrbanDash = urbanDash.IsActive;
                    urbanDashBreakForce = urbanDash.BreakThroughForce;
                    
                    // Urban Dash conferisce temporaneamente invulnerabilità
                    if (hasActiveUrbanDash)
                        isInvulnerable = true;
                }
                
                // Controlla abilità Corpo Ignifugo (Ember)
                if (FireproofBodyLookup.HasComponent(playerEntity))
                {
                    var fireproofBody = FireproofBodyLookup[playerEntity];
                    hasActiveFireproofBody = fireproofBody.IsActive;
                }
                
                // Controlla abilità Glitch Controllato (Neo)
                if (ControlledGlitchLookup.HasComponent(playerEntity))
                {
                    var glitchControl = ControlledGlitchLookup[playerEntity];
                    hasActiveGlitchControl = glitchControl.IsActive && glitchControl.BarrierPenetration;
                }
                
                // Controlla abilità Aura di Calore (Kai)
                if (HeatAuraLookup.HasComponent(playerEntity))
                {
                    var heatAura = HeatAuraLookup[playerEntity];
                    hasActiveHeatAura = heatAura.IsActive;
                    heatAuraRadius = heatAura.AuraRadius;
                }
                
                // Controlla abilità Bolla d'Aria (Marina)
                if (AirBubbleLookup.HasComponent(playerEntity))
                {
                    var airBubble = AirBubbleLookup[playerEntity];
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
                if (AlexLookup.HasComponent(playerEntity))
                {
                    var alexComp = AlexLookup[playerEntity];
                    obstacleBreakThroughBonus = alexComp.ObstacleBreakThroughBonus;
                }
                
                // Ember: resistenza al fuoco/lava
                if (EmberLookup.HasComponent(playerEntity))
                {
                    var emberComp = EmberLookup[playerEntity];
                    fireResistance = emberComp.FireDamageReduction;
                }
                
                // Kai: resistenza al ghiaccio
                if (KaiLookup.HasComponent(playerEntity))
                {
                    var kaiComp = KaiLookup[playerEntity];
                    iceResistance = kaiComp.ColdResistance;
                }
                
                // Marina: resistenza acquatica
                if (MarinaLookup.HasComponent(playerEntity))
                {
                    var marinaComp = MarinaLookup[playerEntity];
                    waterResistance = marinaComp.PressureResistance;
                }
                
                // Neo: bypass barriere
                if (NeoLookup.HasComponent(playerEntity))
                {
                    var neoComp = NeoLookup[playerEntity];
                    digitalBarrierBypassChance = neoComp.FirewallBypass;
                }
                
                // Verifica collisioni con tutti gli ostacoli
                for (int i = 0; i < Obstacles.Length; i++)
                {
                    Entity obstacleEntity = Obstacles[i];
                    ObstacleComponent obstacle = ObstacleComponents[i];
                    TransformComponent obstacleTransform = ObstacleTransforms[i];
                    
                    // Controllo collisione semplificato (sfera vs sfera)
                    if (CheckCollision(transform.Position, obstacleTransform.Position, PlayerRadius, obstacle.CollisionRadius))
                    {
                        // Calcola i dettagli della collisione
                        float3 impactPosition = CalculateImpactPosition(transform.Position, obstacleTransform.Position);
                        float3 impactNormal = math.normalize(transform.Position - obstacleTransform.Position);
                        float impactVelocity = math.dot(physics.Velocity, -impactNormal);
                        
                        // Ignora impatti a bassa velocità (sfioramenti)
                        if (impactVelocity < MinImpactVelocity)
                            continue;
                        
                        // Determina il danno basato sulla velocità e tipo di ostacolo
                        float baseDamage = obstacle.DamageValue;
                        if (baseDamage <= 0)
                        {
                            // Se il danno non è specificato, calcolalo dalla velocità
                            baseDamage = impactVelocity * ObstacleDamageMultiplier;
                        }
                        
                        // Applica modificatori in base al tipo di ostacolo e abilità del personaggio
                        bool canBreakThrough = false;
                        bool isImmune = false;
                        
                        // Gestione di ostacoli da sfondare
                        if (obstacle.IsDestructible)
                        {
                            // Sfondamento tramite scivolata per ostacoli piccoli
                            if (movement.IsSliding && obstacle.Height < SmallObstacleThreshold)
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
                        if (LavaTagLookup.HasComponent(obstacleEntity))
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
                        if (IceObstacleTagLookup.HasComponent(obstacleEntity))
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
                        if (DigitalBarrierTagLookup.HasComponent(obstacleEntity))
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
                        if (UnderwaterTagLookup.HasComponent(obstacleEntity))
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
                            var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, eventEntity, new ObstacleHitEvent
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
                            var damageEventEntity = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, damageEventEntity, new DamageEvent
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
                            var eventEntity = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, eventEntity, new ObstacleBreakThroughEvent
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
                                ECB.DestroyEntity(entityInQueryIndex, obstacleEntity);
                            }
                        }
                        
                        // Interrompe il controllo degli altri ostacoli se ha già colpito uno
                        break;
                    }
                }
            }
            
            /// <summary>
            /// Verifica se c'è una collisione tra due entità basata su posizione e raggio
            /// </summary>
            private bool CheckCollision(float3 posA, float3 posB, float radiusA, float radiusB)
            {
                // Calcola la distanza al quadrato (più efficiente del calcolo della distanza)
                float distanceSq = math.distancesq(posA, posB);
                
                // Somma dei raggi
                float radiusSum = radiusA + radiusB;
                
                // Collisione se la distanza è minore della somma dei raggi
                return distanceSq < (radiusSum * radiusSum);
            }
            
            /// <summary>
            /// Calcola la posizione di impatto tra due entità
            /// </summary>
            private float3 CalculateImpactPosition(float3 posA, float3 posB)
            {
                // Posizione a metà strada tra i due oggetti
                return (posA + posB) * 0.5f;
            }
        }
    }
    
    /// <summary>
    /// Evento generato quando il giocatore sfonda un ostacolo
    /// </summary>
    public struct ObstacleBreakThroughEvent : IComponentData
    {
        /// <summary>
        /// Riferimento all'entità del giocatore
        /// </summary>
        public Entity PlayerEntity;
        
        /// <summary>
        /// Riferimento all'entità dell'ostacolo
        /// </summary>
        public Entity ObstacleEntity;
        
        /// <summary>
        /// Posizione dove è avvenuto lo sfondamento
        /// </summary>
        public float3 BreakThroughPosition;
    }
    
    /// <summary>
    /// Tipi di danno che possono essere applicati
    /// </summary>
    public enum DamageType : byte
    {
        /// <summary>
        /// Danno da collisione con ostacoli
        /// </summary>
        Obstacle = 0,
        
        /// <summary>
        /// Danno da caduta
        /// </summary>
        Fall = 1,
        
        /// <summary>
        /// Danno da nemici
        /// </summary>
        Enemy = 2,
        
        /// <summary>
        /// Danno da trappole ambientali
        /// </summary>
        Hazard = 3,
        
        /// <summary>
        /// Danno da effetti di stato (es. veleno, fuoco)
        /// </summary>
        StatusEffect = 4
    }
    
    /// <summary>
    /// Evento generico di danno
    /// </summary>
    public struct DamageEvent : IComponentData
    {
        /// <summary>
        /// Entità che riceve il danno
        /// </summary>
        public Entity TargetEntity;
        
        /// <summary>
        /// Entità che causa il danno (può essere Entity.Null)
        /// </summary>
        public Entity SourceEntity;
        
        /// <summary>
        /// Quantità di danno
        /// </summary>
        public float DamageAmount;
        
        /// <summary>
        /// Tipo di danno
        /// </summary>
        public DamageType DamageType;
        
        /// <summary>
        /// Posizione dell'impatto
        /// </summary>
        public float3 ImpactPosition;
    }
}