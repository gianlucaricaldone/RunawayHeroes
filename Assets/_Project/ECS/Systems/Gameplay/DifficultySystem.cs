using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce la difficoltà progressiva del gioco, sia all'interno di un livello
    /// che tra diversi mondi tematici. Si occupa di regolare parametri di difficoltà in base
    /// al progresso del giocatore e al tema del mondo corrente.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct DifficultySystem : ISystem
    {
        private EntityQuery _worldConfigQuery;
        private EntityQuery _segmentsQuery;
        private EntityQuery _obstaclesQuery;
        
        private const float DEFAULT_DIFFICULTY_UPDATE_INTERVAL = 3.0f; // Secondi
        private float _timeSinceLastUpdate;
        
        /// <summary>
        /// Inizializza il sistema di gestione difficoltà
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Crea la query per la configurazione di difficoltà del mondo
            _worldConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<WorldDifficultyConfigComponent>()
                .Build(ref state);
                
            // Crea la query per i segmenti di percorso
            _segmentsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PathSegmentComponent, LocalTransform>()
                .Build(ref state);
                
            // Crea le query per ostacoli (le query per nemici sono create nell'OnUpdate)
                
            _obstaclesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<ObstacleComponent, SegmentReferenceComponent>()
                .Build(ref state);
            
            // Richiedi che il sistema venga eseguito solo quando esiste almeno un livello attivo
            state.RequireForUpdate<LevelComponent>();
            
            // Inizializza il timer
            _timeSinceLastUpdate = 0f;
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
        /// Aggiorna i parametri di difficoltà di segmenti, nemici e ostacoli
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Aggiorna solo a intervalli regolari per risparmiare risorse
            _timeSinceLastUpdate += SystemAPI.Time.DeltaTime;
            if (_timeSinceLastUpdate < DEFAULT_DIFFICULTY_UPDATE_INTERVAL)
                return;
                
            _timeSinceLastUpdate = 0f;
            
            // Crea una configurazione di difficoltà predefinita se non esiste già
            if (_worldConfigQuery.IsEmpty)
            {
                CreateDefaultWorldDifficultyConfig(ref state);
            }
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Ottieni la configurazione di difficoltà corrente
            var difficultyConfig = SystemAPI.GetSingleton<WorldDifficultyConfigComponent>();
            
            // 1. Applica modificatori di difficoltà ai segmenti di percorso
            state.Dependency = new ApplyDifficultyToSegmentsJob
            {
                DifficultyConfig = difficultyConfig
            }.ScheduleParallel(_segmentsQuery, state.Dependency);
            
            // 2. Applica modificatori di difficoltà ai diversi tipi di nemici
            
            // 2.1 Nemici normali
            var enemiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyComponent, SegmentReferenceComponent>()
                .Build(ref state);
                
            if (!enemiesQuery.IsEmpty)
            {
                state.Dependency = new ApplyDifficultyToEnemiesJob
                {
                    DifficultyConfig = difficultyConfig,
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(enemiesQuery, state.Dependency);
            }
            
            // 2.2 Mid-Boss
            var midBossQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<MidBossComponent, SegmentReferenceComponent>()
                .Build(ref state);
                
            if (!midBossQuery.IsEmpty)
            {
                state.Dependency = new ApplyDifficultyToMidBossesJob
                {
                    DifficultyConfig = difficultyConfig,
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(midBossQuery, state.Dependency);
            }
            
            // 2.3 Boss
            var bossQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BossComponent, SegmentReferenceComponent>()
                .Build(ref state);
                
            if (!bossQuery.IsEmpty)
            {
                state.Dependency = new ApplyDifficultyToBossesJob
                {
                    DifficultyConfig = difficultyConfig,
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(bossQuery, state.Dependency);
            }
            
            // 3. Applica modificatori di difficoltà agli ostacoli in base alla difficoltà del segmento
            if (!_obstaclesQuery.IsEmpty)
            {
                state.Dependency = new ApplyDifficultyToObstaclesJob
                {
                    DifficultyConfig = difficultyConfig,
                    ECB = ecb.AsParallelWriter()
                }.ScheduleParallel(_obstaclesQuery, state.Dependency);
            }
        }
        
        /// <summary>
        /// Crea una configurazione di difficoltà predefinita nel mondo
        /// </summary>
        private void CreateDefaultWorldDifficultyConfig(ref SystemState state)
        {
            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, WorldDifficultyConfigComponent.CreateDefault());
        }
        
        /// <summary>
        /// Job che applica la difficoltà ai segmenti di percorso
        /// </summary>
        [BurstCompile]
        private partial struct ApplyDifficultyToSegmentsJob : IJobEntity
        {
            [ReadOnly] public WorldDifficultyConfigComponent DifficultyConfig;
            
            [BurstCompile]
            private void Execute(ref PathSegmentComponent segment, in LocalTransform transform)
            {
                // Verifica se il segmento è attivo prima di aggiornarne la difficoltà
                if (!segment.IsActive)
                    return;
                
                // Determina se è un livello tutorial
                bool isTutorial = segment.SegmentIndex < 10;
                
                // Ottieni fattori di difficoltà specifici per il tema
                float difficultyFactor = DifficultyConfig.GetDifficultyRampScaleForTheme(segment.Theme, isTutorial);
                
                // Calcola una difficoltà di base in funzione del progresso del livello
                int baseDifficulty = DifficultyConfig.GetBaseDifficultyForTheme(segment.Theme);
                
                // Se è un tutorial, mantieni una difficoltà più bassa
                if (isTutorial)
                {
                    baseDifficulty = DifficultyConfig.TutorialBaseDifficulty;
                    
                    // Crea zone sicure nel tutorial se abilitato
                    if (DifficultyConfig.EnableTutorialSafezones && segment.SegmentIndex % 3 == 0)
                    {
                        segment.DifficultyLevel = 1; // Difficoltà minima per zone sicure
                        return;
                    }
                }
                
                // Calcola l'incremento di difficoltà basato sulla progressione
                float progression = math.min(1f, segment.SegmentIndex / 20f); // Normalizza la progressione
                float difficultyIncrease = progression * difficultyFactor * 5f; // Max +5 livelli
                
                // Applica la difficoltà al segmento, limitandola tra 1 e 10
                segment.DifficultyLevel = (int)math.clamp(
                    baseDifficulty + math.round(difficultyIncrease),
                    1, 10);
                
                // Per i segmenti di pericolo (Hazard), incrementa ulteriormente la difficoltà
                if (segment.Type == SegmentType.Hazard)
                {
                    segment.DifficultyLevel = math.min(10, segment.DifficultyLevel + 1);
                }
                
                // Per i checkpoint, riduci leggermente la difficoltà
                if (segment.Type == SegmentType.Checkpoint)
                {
                    segment.DifficultyLevel = math.max(1, segment.DifficultyLevel - 1);
                }
            }
        }
        
        /// <summary>
        /// Job che applica la difficoltà ai nemici normali
        /// </summary>
        [BurstCompile]
        private partial struct ApplyDifficultyToEnemiesJob : IJobEntity
        {
            [ReadOnly] public WorldDifficultyConfigComponent DifficultyConfig;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref EnemyComponent enemy,
                in SegmentReferenceComponent segmentRef)
            {
                // Ottieni il livello di difficoltà dal segmento di riferimento
                if (!SystemAPI.Exists(segmentRef.SegmentEntity))
                    return;
                    
                // Ottieni il componente segmento
                if (!SystemAPI.HasComponent<PathSegmentComponent>(segmentRef.SegmentEntity))
                    return;
                    
                var segment = SystemAPI.GetComponent<PathSegmentComponent>(segmentRef.SegmentEntity);
                int difficultyLevel = segment.DifficultyLevel;
                
                // Ora possiamo applicare modificatori in base al livello di difficoltà
                
                // Scala la velocità del nemico
                float speedScale = 1.0f + (difficultyLevel - 1) * 0.05f; // +5% per livello
                enemy.BaseSpeed *= speedScale;
                
                // Scala il danno
                float damageScale = 1.0f + (difficultyLevel - 1) * 0.08f; // +8% per livello
                enemy.AttackDamage = math.max(1, (int)(enemy.AttackDamage * damageScale));
                
                // Scala la salute
                float healthScale = 1.0f + (difficultyLevel - 1) * 0.1f; // +10% per livello
                
                // Aggiungi o aggiorna il componente HealthComponent
                if (SystemAPI.HasComponent<HealthComponent>(entity))
                {
                    var health = SystemAPI.GetComponent<HealthComponent>(entity);
                    float originalMaxHealth = health.MaxHealth;
                    health.MaxHealth = math.max(1, originalMaxHealth * healthScale);
                    
                    // Scala la salute corrente proporzionalmente
                    if (health.CurrentHealth < originalMaxHealth)
                    {
                        float healthRatio = health.CurrentHealth / originalMaxHealth;
                        health.CurrentHealth = health.MaxHealth * healthRatio;
                    }
                    else
                    {
                        health.CurrentHealth = health.MaxHealth;
                    }
                    
                    SystemAPI.SetComponent(entity, health);
                }
                
                // Se è un nemico d'élite, aggiungi ulteriori modificatori
                if (enemy.IsElite)
                {
                    enemy.AttackSpeed *= 1.1f; // +10% alla velocità di attacco
                    
                    // Aggiungi resistenze speciali se il livello di difficoltà è alto
                    if (difficultyLevel >= 8)
                    {
                        // Aggiungi il componente di resistenza se non esiste già
                        if (!SystemAPI.HasComponent<DefenseComponent>(entity))
                        {
                            ECB.AddComponent(sortKey, entity, new DefenseComponent
                            {
                                PhysicalResistance = 15, // 15% di resistenza fisica
                                ElementalResistance = 10, // 10% di resistenza elementale
                                EnergyResistance = 5 // 5% di resistenza energetica
                            });
                        }
                        else
                        {
                            var defense = SystemAPI.GetComponent<DefenseComponent>(entity);
                            defense.PhysicalResistance += 5;
                            defense.ElementalResistance += 5;
                            SystemAPI.SetComponent(entity, defense);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che applica la difficoltà ai mid-boss
        /// </summary>
        [BurstCompile]
        private partial struct ApplyDifficultyToMidBossesJob : IJobEntity
        {
            [ReadOnly] public WorldDifficultyConfigComponent DifficultyConfig;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref MidBossComponent midBoss,
                in SegmentReferenceComponent segmentRef)
            {
                // Ottieni il livello di difficoltà dal segmento di riferimento
                if (!SystemAPI.Exists(segmentRef.SegmentEntity))
                    return;
                    
                if (!SystemAPI.HasComponent<PathSegmentComponent>(segmentRef.SegmentEntity))
                    return;
                    
                var segment = SystemAPI.GetComponent<PathSegmentComponent>(segmentRef.SegmentEntity);
                int difficultyLevel = segment.DifficultyLevel;
                
                // I mid-boss hanno modificatori di difficoltà più leggeri ma comunque significativi
                
                // Scala la salute solo del 5% per livello
                float healthScale = 1.0f + (difficultyLevel - 1) * 0.05f;
                
                if (SystemAPI.HasComponent<HealthComponent>(entity))
                {
                    var health = SystemAPI.GetComponent<HealthComponent>(entity);
                    float originalMaxHealth = health.MaxHealth;
                    health.MaxHealth = math.max(1, originalMaxHealth * healthScale);
                    
                    // Scala la salute corrente proporzionalmente
                    if (health.CurrentHealth < originalMaxHealth)
                    {
                        float healthRatio = health.CurrentHealth / originalMaxHealth;
                        health.CurrentHealth = health.MaxHealth * healthRatio;
                    }
                    else
                    {
                        health.CurrentHealth = health.MaxHealth;
                    }
                    
                    SystemAPI.SetComponent(entity, health);
                }
                
                // Aumenta il danno solo del 3% per livello di difficoltà
                float damageScale = 1.0f + (difficultyLevel - 1) * 0.03f;
                midBoss.AttackDamage = (int)(midBoss.AttackDamage * damageScale);
                
                // A difficoltà elevate, aggiungi attacchi speciali
                if (difficultyLevel >= 7 && !midBoss.HasSpecialAttacks)
                {
                    midBoss.HasSpecialAttacks = true;
                    midBoss.SpecialAttackCooldown *= 0.8f; // Riduci il cooldown del 20%
                }
            }
        }
        
        /// <summary>
        /// Job che applica la difficoltà ai boss
        /// </summary>
        [BurstCompile]
        private partial struct ApplyDifficultyToBossesJob : IJobEntity
        {
            [ReadOnly] public WorldDifficultyConfigComponent DifficultyConfig;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref BossComponent boss,
                in SegmentReferenceComponent segmentRef)
            {
                // I boss hanno una scaling di difficoltà più contenuto perché già difficili
                // e calibrati per essere una sfida significativa
                
                // Ottieni il livello di difficoltà dal segmento di riferimento
                if (!SystemAPI.Exists(segmentRef.SegmentEntity))
                    return;
                    
                if (!SystemAPI.HasComponent<PathSegmentComponent>(segmentRef.SegmentEntity))
                    return;
                    
                var segment = SystemAPI.GetComponent<PathSegmentComponent>(segmentRef.SegmentEntity);
                int difficultyLevel = segment.DifficultyLevel;
                
                // Scala solo il danno di base e non la salute
                float damageScale = 1.0f + (difficultyLevel - 1) * 0.02f; // Solo +2% per livello
                boss.BaseDamage = (int)(boss.BaseDamage * damageScale);
                
                // A difficoltà alta, aggiungi pattern di attacco più complessi
                if (difficultyLevel >= 9)
                {
                    boss.AttackPatternComplexity++;
                    
                    if (SystemAPI.HasComponent<DefenseComponent>(entity))
                    {
                        var defense = SystemAPI.GetComponent<DefenseComponent>(entity);
                        defense.PhysicalResistance += 2; // Incremento leggero
                        defense.ElementalResistance += 2;
                        SystemAPI.SetComponent(entity, defense);
                    }
                }
            }
        }
        
        /// <summary>
        /// Job che applica la difficoltà agli ostacoli
        /// </summary>
        [BurstCompile]
        private partial struct ApplyDifficultyToObstaclesJob : IJobEntity
        {
            [ReadOnly] public WorldDifficultyConfigComponent DifficultyConfig;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute(
                Entity entity,
                [EntityIndexInQuery] int sortKey,
                ref ObstacleComponent obstacle,
                in SegmentReferenceComponent segmentRef)
            {
                // Ottieni il livello di difficoltà dal segmento di riferimento
                if (!SystemAPI.Exists(segmentRef.SegmentEntity))
                    return;
                    
                if (!SystemAPI.HasComponent<PathSegmentComponent>(segmentRef.SegmentEntity))
                    return;
                    
                var segment = SystemAPI.GetComponent<PathSegmentComponent>(segmentRef.SegmentEntity);
                int difficultyLevel = segment.DifficultyLevel;
                
                // Modifica parametri degli ostacoli in base alla difficoltà
                
                // Scala il danno degli ostacoli
                if (obstacle.DealsDamage)
                {
                    float damageScale = 1.0f + (difficultyLevel - 1) * 0.07f; // +7% per livello
                    obstacle.DamageAmount = math.max(1, obstacle.DamageAmount * damageScale);
                }
                
                // Scala la velocità degli ostacoli mobili
                if (obstacle.IsMoving && SystemAPI.HasComponent<PhysicsComponent>(entity))
                {
                    var physics = SystemAPI.GetComponent<PhysicsComponent>(entity);
                    float speedScale = 1.0f + (difficultyLevel - 1) * 0.04f; // +4% per livello
                    
                    // Applica solo se la velocità è significativa
                    if (math.lengthsq(physics.Velocity) > 0.01f)
                    {
                        physics.Velocity *= speedScale;
                        SystemAPI.SetComponent(entity, physics);
                    }
                }
                
                // Per ostacoli distruttibili, scala la resistenza
                if (obstacle.IsDestructible && SystemAPI.HasComponent<DamagedStateComponent>(entity))
                {
                    var damagedState = SystemAPI.GetComponent<DamagedStateComponent>(entity);
                    float resistanceScale = 1.0f + (difficultyLevel - 1) * 0.08f; // +8% per livello
                    
                    damagedState.MaxIntegrity = math.max(1, damagedState.MaxIntegrity * resistanceScale);
                    
                    // Se l'integrità attuale è piena, scalala anche
                    if (damagedState.CurrentIntegrity >= damagedState.MaxIntegrity)
                    {
                        damagedState.CurrentIntegrity = damagedState.MaxIntegrity;
                    }
                    
                    SystemAPI.SetComponent(entity, damagedState);
                }
                
                // Per ostacoli temporanei, modifica la durata in base al livello di difficoltà
                if (SystemAPI.HasComponent<TemporaryObstacleComponent>(entity))
                {
                    var temporary = SystemAPI.GetComponent<TemporaryObstacleComponent>(entity);
                    
                    // Riduci il tempo di vita se la difficoltà è alta
                    if (difficultyLevel >= 7 && temporary.TotalLifetime > 1.5f)
                    {
                        float durationScale = 1.0f - (difficultyLevel - 6) * 0.05f; // -5% per livello oltre 6
                        float newTotal = temporary.TotalLifetime * durationScale;
                        float newRemaining = temporary.RemainingLifetime * (newTotal / temporary.TotalLifetime);
                        
                        temporary.TotalLifetime = newTotal;
                        temporary.RemainingLifetime = newRemaining;
                        
                        SystemAPI.SetComponent(entity, temporary);
                    }
                }
            }
        }
    }
}