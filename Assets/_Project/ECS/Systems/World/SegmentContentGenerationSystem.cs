using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Enemies;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile della generazione dei contenuti (ostacoli, nemici, ecc.) in un segmento di percorso
    /// </summary>
    public partial struct SegmentContentGenerationSystem : ISystem
    {
        private uint _seed;
        private EntityQuery _segmentQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Inizializza il generatore di numeri casuali
            _seed = (uint)DateTime.Now.Ticks;
            
            // Crea la query per i segmenti che richiedono generazione di contenuto
            _segmentQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PathSegmentComponent, RequiresContentGenerationTag, DynamicBuffer<SegmentContentBuffer>>()
                .Build(ref state);
                
            // Richiedi che il sistema venga eseguito solo quando ci sono segmenti che richiedono generazione
            state.RequireForUpdate<RequiresContentGenerationTag>();
        }

        public void OnDestroy(ref SystemState state) 
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Ottieni le configurazioni di spawn dal livello
            // Nota: in una implementazione reale, dovresti ottenerle per ogni singolo livello
            var obstacleConfig = SystemAPI.GetSingleton<ObstacleSpawnConfigComponent>();
            var enemyConfig = SystemAPI.GetSingleton<EnemySpawnConfigComponent>();
            
            // Esegui il job per generare i contenuti dei segmenti
            state.Dependency = new GenerateSegmentContentsJob
            {
                ECB = commandBuffer,
                Seed = _seed,
                ObstacleConfig = obstacleConfig,
                EnemyConfig = enemyConfig
            }.ScheduleParallel(_segmentQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per generare i contenuti dei segmenti
    /// </summary>
    [BurstCompile]
    public partial struct GenerateSegmentContentsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public uint Seed;
        public ObstacleSpawnConfigComponent ObstacleConfig;
        public EnemySpawnConfigComponent EnemyConfig;
        
        [BurstDiscard]
        public void Execute(Entity segmentEntity, 
                         [EntityIndexInQuery] int entityInQueryIndex,
                         ref PathSegmentComponent segment,
                         ref DynamicBuffer<SegmentContentBuffer> contentBuffer)
        {
            // Inizializza un generatore casuale deterministico per questo segmento specifico
            // Usiamo l'indice del segmento come parte del seed per garantire risultati coerenti
            Unity.Mathematics.Random segRandom = Unity.Mathematics.Random.CreateFromIndex((uint)(segment.SegmentIndex + Seed));
            
            // Genera ostacoli
            GenerateObstacles(entityInQueryIndex, segmentEntity, ref segment, contentBuffer, ObstacleConfig, segRandom);
            
            // Genera nemici
            GenerateEnemies(entityInQueryIndex, segmentEntity, ref segment, contentBuffer, EnemyConfig, segRandom);
            
            // Genera collezionabili
            GenerateCollectibles(entityInQueryIndex, segmentEntity, ref segment, contentBuffer, segRandom);
            
            // Segna il segmento come generato
            segment.IsGenerated = true;
            
            // Rimuovi il tag di generazione richiesta
            ECB.RemoveComponent<RequiresContentGenerationTag>(entityInQueryIndex, segmentEntity);
        }
        
        /// <summary>
        /// Genera ostacoli all'interno di un segmento
        /// </summary>
        [BurstDiscard]
        private void GenerateObstacles(int entityInQueryIndex,
                                    Entity segmentEntity,
                                    ref PathSegmentComponent segment,
                                    DynamicBuffer<SegmentContentBuffer> contentBuffer,
                                    ObstacleSpawnConfigComponent config,
                                    Unity.Mathematics.Random random)
        {
            // Salta la generazione per segmenti speciali come checkpoint
            if (segment.Type == SegmentType.Checkpoint)
                return;
                
            // Modifica la densità in base al tipo di segmento
            float densityMultiplier = 1.0f;
            switch (segment.Type)
            {
                case SegmentType.Hazard:
                    densityMultiplier = 1.5f; // Più ostacoli nelle aree pericolose
                    break;
                case SegmentType.Narrow:
                    densityMultiplier = 0.7f; // Meno ostacoli nei passaggi stretti
                    break;
                case SegmentType.Jump:
                    densityMultiplier = 0.5f; // Ancora meno ostacoli nei segmenti di salto
                    break;
            }
            
            // Calcola il numero di ostacoli da generare
            int numObstacles = random.NextInt(
                math.max(1, (int)(config.MinObstacles * densityMultiplier)),
                math.max(2, (int)(config.MaxObstacles * densityMultiplier * config.DensityFactor)) + 1
            );
            
            // Generiamo gli ostacoli
            for (int i = 0; i < numObstacles; i++)
            {
                // Determina il tipo di ostacolo in base alle probabilità
                float typeRoll = random.NextFloat();
                ObstacleComponent obstacle;
                SegmentContentType contentType;
                
                if (typeRoll < config.SmallObstacleProbability)
                {
                    obstacle = ObstacleComponent.CreateSmall();
                    contentType = SegmentContentType.SmallObstacle;
                }
                else if (typeRoll < config.SmallObstacleProbability + config.MediumObstacleProbability)
                {
                    obstacle = ObstacleComponent.CreateMedium();
                    contentType = SegmentContentType.MediumObstacle;
                }
                else
                {
                    obstacle = ObstacleComponent.CreateLarge();
                    contentType = SegmentContentType.LargeObstacle;
                }
                
                // Crea la posizione dell'ostacolo 
                // (distribuita lungo il segmento, evitando inizio e fine)
                float segmentProgress = random.NextFloat(0.15f, 0.85f);
                float3 obstaclePos = math.lerp(segment.StartPosition, segment.EndPosition, segmentProgress);
                
                // Distribuisci gli ostacoli anche lateralmente
                float lateralPosition = random.NextFloat(-segment.Width/2f + 1f, segment.Width/2f - 1f);
                obstaclePos.x += lateralPosition;
                
                // Crea l'entità ostacolo
                Entity obstacleEntity = ECB.CreateEntity(entityInQueryIndex);
                
                // Aggiungi il componente ostacolo
                ECB.AddComponent(entityInQueryIndex, obstacleEntity, obstacle);
                
                // Aggiungi il componente transform
                ECB.AddComponent(entityInQueryIndex, obstacleEntity, new LocalTransform
                {
                    Position = obstaclePos,
                    Rotation = quaternion.EulerZXY(0, random.NextFloat(0, math.PI * 2), 0),
                    Scale = random.NextFloat(0.8f, 1.2f)
                });
                
                // Aggiungi tag speciali in base al tema
                AddSpecialObstacleTags(entityInQueryIndex, obstacleEntity, segment.Theme, config, random);
                
                // Aggiungi l'ostacolo al buffer del segmento
                contentBuffer.Add(new SegmentContentBuffer
                {
                    ContentEntity = obstacleEntity,
                    Type = contentType
                });
            }
        }
        
        /// <summary>
        /// Aggiunge tag speciali agli ostacoli in base al tema del segmento
        /// </summary>
        [BurstDiscard]
        private void AddSpecialObstacleTags(int entityInQueryIndex,
                                          Entity obstacleEntity,
                                          WorldTheme theme,
                                          ObstacleSpawnConfigComponent config,
                                          Unity.Mathematics.Random random)
        {
            // In base al tema, aggiungiamo tag specifici agli ostacoli
            switch (theme)
            {
                case WorldTheme.Volcano:
                    if (random.NextFloat() < config.LavaObstacleProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new LavaTag());
                    }
                    break;
                    
                case WorldTheme.Tundra:
                    if (random.NextFloat() < config.IceObstacleProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new IceObstacleTag());
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new IceIntegrityComponent
                        {
                            MaxIntegrity = 100f,
                            CurrentIntegrity = 100f
                        });
                    }
                    else if (random.NextFloat() < config.SlipperyProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new SlipperyTag
                        {
                            SlipFactor = random.NextFloat(0.3f, 0.9f)
                        });
                    }
                    break;
                    
                case WorldTheme.Virtual:
                    if (random.NextFloat() < config.DigitalBarrierProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new DigitalBarrierTag());
                    }
                    break;
                    
                case WorldTheme.Abyss:
                    if (random.NextFloat() < config.UnderwaterProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new UnderwaterTag());
                    }
                    else if (random.NextFloat() < config.CurrentProbability)
                    {
                        ECB.AddComponent(entityInQueryIndex, obstacleEntity, new CurrentTag
                        {
                            Direction = new float3(random.NextFloat(-1f, 1f), 0, random.NextFloat(-1f, 1f)),
                            Strength = random.NextFloat(0.5f, 2f),
                            CurrentType = 1 // Acqua
                        });
                    }
                    break;
            }
            
            // Elementi speciali che possono apparire in qualsiasi tema
            if (random.NextFloat() < config.ToxicGroundProbability)
            {
                ECB.AddComponent(entityInQueryIndex, obstacleEntity, new ToxicGroundTag
                {
                    ToxicType = (byte)random.NextInt(0, 3),
                    DamagePerSecond = random.NextFloat(5f, 15f)
                });
            }
        }
        
        /// <summary>
        /// Genera nemici all'interno di un segmento
        /// </summary>
        [BurstDiscard]
        private void GenerateEnemies(int entityInQueryIndex,
                                   Entity segmentEntity,
                                   ref PathSegmentComponent segment,
                                   DynamicBuffer<SegmentContentBuffer> contentBuffer,
                                   EnemySpawnConfigComponent config,
                                   Unity.Mathematics.Random random)
        {
            // Salta la generazione per segmenti speciali come checkpoint
            if (segment.Type == SegmentType.Checkpoint)
                return;
                
            // Modifica la densità in base al tipo di segmento
            float densityMultiplier = 1.0f;
            switch (segment.Type)
            {
                case SegmentType.Hazard:
                    densityMultiplier = 0.7f; // Meno nemici nelle aree già pericolose
                    break;
                case SegmentType.Wide:
                    densityMultiplier = 1.3f; // Più nemici nelle aree ampie
                    break;
            }
            
            // Calcola il numero di nemici da generare
            int numEnemies = random.NextInt(
                config.MinEnemies,
                math.max(config.MinEnemies + 1, (int)(config.MaxEnemies * densityMultiplier * config.DensityFactor)) + 1
            );
            
            // Decidi se generare gruppi o nemici singoli
            bool spawnAsGroup = random.NextFloat() < config.GroupSpawnProbability && numEnemies >= 2;
            
            if (spawnAsGroup)
            {
                // Calcola la dimensione del gruppo
                int groupSize = math.min(numEnemies, random.NextInt(config.MinGroupSize, config.MaxGroupSize + 1));
                
                // Genera la posizione del gruppo
                float groupProgress = random.NextFloat(0.2f, 0.8f);
                float3 groupCenter = math.lerp(segment.StartPosition, segment.EndPosition, groupProgress);
                
                // Distribuisci i nemici intorno al centro del gruppo
                for (int i = 0; i < groupSize; i++)
                {
                    float radius = random.NextFloat(1f, 3f);
                    float angle = random.NextFloat(0, math.PI * 2);
                    float3 offset = new float3(math.sin(angle) * radius, 0, math.cos(angle) * radius);
                    
                    SpawnEnemy(entityInQueryIndex, segmentEntity, groupCenter + offset, 
                             segment.Theme, contentBuffer, config, random);
                }
                
                // Riduci il numero di nemici rimasti da generare
                numEnemies -= groupSize;
            }
            
            // Genera i nemici rimanenti individualmente
            for (int i = 0; i < numEnemies; i++)
            {
                // Crea la posizione del nemico
                float enemyProgress = random.NextFloat(0.15f, 0.85f);
                float3 enemyPos = math.lerp(segment.StartPosition, segment.EndPosition, enemyProgress);
                
                // Distribuisci i nemici anche lateralmente
                float lateralPosition = random.NextFloat(-segment.Width/2f + 1.5f, segment.Width/2f - 1.5f);
                enemyPos.x += lateralPosition;
                
                SpawnEnemy(entityInQueryIndex, segmentEntity, enemyPos, 
                         segment.Theme, contentBuffer, config, random);
            }
        }
        
        /// <summary>
        /// Genera un singolo nemico nella posizione specificata
        /// </summary>
        [BurstDiscard]
        private void SpawnEnemy(int entityInQueryIndex,
                              Entity segmentEntity,
                              float3 position,
                              WorldTheme theme,
                              DynamicBuffer<SegmentContentBuffer> contentBuffer,
                              EnemySpawnConfigComponent config,
                              Unity.Mathematics.Random random)
        {
            // Crea l'entità nemico
            Entity enemyEntity = ECB.CreateEntity(entityInQueryIndex);
            
            // Aggiungi il componente base nemico
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new EnemyComponent());
            
            // Determina il tipo di nemico
            float typeRoll = random.NextFloat();
            SegmentContentType contentType;
            
            if (typeRoll < config.DroneProbability)
            {
                ECB.AddComponent(entityInQueryIndex, enemyEntity, new DroneComponent());
                contentType = SegmentContentType.Drone;
            }
            else if (typeRoll < config.DroneProbability + config.PatrolProbability)
            {
                ECB.AddComponent(entityInQueryIndex, enemyEntity, new PatrolComponent());
                contentType = SegmentContentType.Patrol;
            }
            else if (random.NextFloat() < config.MidBossProbability)
            {
                ECB.AddComponent(entityInQueryIndex, enemyEntity, new MidBossComponent());
                contentType = SegmentContentType.MidBoss;
            }
            else
            {
                contentType = SegmentContentType.Enemy;
            }
            
            // Aggiungi il componente di attacco
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new AttackComponent());
            
            // Aggiungi il componente di stato AI
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new AIStateComponent());
            
            // Aggiungi il componente transform
            ECB.AddComponent(entityInQueryIndex, enemyEntity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.EulerZXY(0, random.NextFloat(0, math.PI * 2), 0),
                Scale = contentType == SegmentContentType.MidBoss ? 1.5f : 1.0f
            });
            
            // Aggiungi l'entità nemico al buffer del segmento
            contentBuffer.Add(new SegmentContentBuffer
            {
                ContentEntity = enemyEntity,
                Type = contentType
            });
        }
        
        /// <summary>
        /// Genera collezionabili all'interno di un segmento
        /// </summary>
        [BurstDiscard]
        private void GenerateCollectibles(int entityInQueryIndex,
                                       Entity segmentEntity,
                                       ref PathSegmentComponent segment,
                                       DynamicBuffer<SegmentContentBuffer> contentBuffer,
                                       Unity.Mathematics.Random random)
        {
            // Implementazione per la generazione di oggetti collezionabili lungo il percorso
            // ...
        }
    }
}