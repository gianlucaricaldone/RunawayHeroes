using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using RunawayHeroes.ECS.Components.World;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile della generazione procedurale dei livelli per il gioco runner
    /// </summary>
    public partial struct RunnerLevelGenerationSystem : ISystem
    {
        private const float DEFAULT_SEGMENT_LENGTH = 30f; // Lunghezza predefinita di un segmento in metri
        private EntityQuery _levelConfigQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Richiedi il singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi che il sistema venga eseguito solo se esiste almeno un'entità 
            // con RunnerLevelConfigComponent
            state.RequireForUpdate<RunnerLevelConfigComponent>();
            
            // Crea la query per le entità con RunnerLevelConfigComponent
            _levelConfigQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<RunnerLevelConfigComponent, LocalTransform>()
                .Build(ref state);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // Command buffer per operazioni di creazione/modifica entità
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Elabora le richieste di generazione livello utilizzando IJobEntity
            state.Dependency = new ProcessRunnerLevelRequestsJob
            {
                CommandBuffer = commandBuffer,
                EntityManager = state.EntityManager
            }.ScheduleParallel(_levelConfigQuery, state.Dependency);
        }
        
    /// <summary>
    /// Job per elaborare le richieste di generazione livello
    /// Nota: Non possiamo usare [BurstCompile] perché accediamo a EntityManager (tipo gestito)
    /// </summary>
    public partial struct ProcessRunnerLevelRequestsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [ReadOnly] public EntityManager EntityManager;
        private const float DEFAULT_SEGMENT_LENGTH = 30f; // Lunghezza predefinita di un segmento in metri
        
        void Execute(Entity entity, [EntityIndexInQuery] int entityInQueryIndex, 
                    in RunnerLevelConfigComponent config, in LocalTransform transform)
        {
            // Inizializza il generatore casuale con il seed fornito
            Unity.Mathematics.Random random = Unity.Mathematics.Random.CreateFromIndex((uint)config.Seed);
            
            // Calcola il numero di segmenti in base alla lunghezza desiderata
            int numSegments = math.min(
                math.max(config.MinSegments, config.LevelLength / (int)DEFAULT_SEGMENT_LENGTH),
                config.MaxSegments
            );
            
            // Genera l'entità del livello
            Entity levelEntity = GenerateLevel(entityInQueryIndex, config, numSegments, transform, random);
            
            // Genera i segmenti del percorso
            GeneratePathSegments(entityInQueryIndex, levelEntity, numSegments, config, random);
            
            // Rimuovi il componente di configurazione per evitare di rigenerare
            CommandBuffer.RemoveComponent<RunnerLevelConfigComponent>(entityInQueryIndex, entity);
        }
        
        /// <summary>
        /// Genera l'entità principale del livello
        /// </summary>
        private Entity GenerateLevel(int entityInQueryIndex, RunnerLevelConfigComponent config, 
                                  int numSegments, LocalTransform transform, Unity.Mathematics.Random random)
        {
            // Crea l'entità del livello
            Entity levelEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base
            CommandBuffer.AddComponent(entityInQueryIndex, levelEntity, new LevelComponent());
            CommandBuffer.AddComponent(entityInQueryIndex, levelEntity, transform);
            
            // Aggiungi il componente di tema del mondo
            CommandBuffer.AddComponent(entityInQueryIndex, levelEntity, new WorldIdentifierComponent());
            
            // Aggiungi il buffer per i segmenti di percorso
            CommandBuffer.AddBuffer<PathSegmentBuffer>(entityInQueryIndex, levelEntity);
            
            // Cerca la configurazione di difficoltà nel mondo
            WorldDifficultyConfigComponent difficultyConfig = default;
            bool hasDifficultyConfig = false;
            
            // NOTA: Questo codice non è compatibile con Burst a causa dell'array ComponentType[]
            // Per questo motivo abbiamo rimosso l'attributo [BurstCompile] da questo job
            // var difficultyQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
            
            // Approccio alternativo che non richiede la creazione di un ComponentType[] (ma comunque non compatibile con Burst a causa di EntityManager)
            var difficultyQuery = EntityManager.CreateEntityQuery(typeof(WorldDifficultyConfigComponent));
            if (difficultyQuery.CalculateEntityCount() > 0)
            {
                // Otteniamo il primo componente che troviamo (dovrebbe essere un singleton)
                var entity = difficultyQuery.GetSingletonEntity();
                if (EntityManager.HasComponent<WorldDifficultyConfigComponent>(entity))
                {
                    difficultyConfig = EntityManager.GetComponentData<WorldDifficultyConfigComponent>(entity);
                    hasDifficultyConfig = true;
                }
            }
            
            // Determina se è un tutorial (approssimazione)
            bool isTutorial = config.PrimaryTheme == WorldTheme.City && numSegments <= 20;
            
            // Calcola la densità di ostacoli in base al tema e alla configurazione di difficoltà
            float obstacleDensity = config.ObstacleDensity;
            if (hasDifficultyConfig)
            {
                obstacleDensity *= difficultyConfig.GetObstacleDensityScaleForTheme(config.PrimaryTheme, isTutorial);
            }
            
            // Aggiungi il componente per le configurazioni di spawn con difficoltà scalata
            CommandBuffer.AddComponent(entityInQueryIndex, levelEntity, new ObstacleSpawnConfigComponent
            {
                DensityFactor = obstacleDensity,
                MinObstacles = isTutorial ? 1 : 2, // Meno ostacoli nel tutorial
                MaxObstacles = isTutorial ? 3 : 5, // Meno ostacoli nel tutorial
                SmallObstacleProbability = isTutorial ? 0.7f : 0.5f, // Più ostacoli piccoli nel tutorial
                MediumObstacleProbability = isTutorial ? 0.25f : 0.3f,
                LargeObstacleProbability = isTutorial ? 0.05f : 0.2f, // Quasi nessun ostacolo grande nel tutorial
                // Pesi per tema
                CityObstacleWeight = config.PrimaryTheme == WorldTheme.City ? 1.0f : 0.2f,
                ForestObstacleWeight = config.PrimaryTheme == WorldTheme.Forest ? 1.0f : 0.2f,
                TundraObstacleWeight = config.PrimaryTheme == WorldTheme.Tundra ? 1.0f : 0.2f,
                VolcanoObstacleWeight = config.PrimaryTheme == WorldTheme.Volcano ? 1.0f : 0.2f,
                AbyssObstacleWeight = config.PrimaryTheme == WorldTheme.Abyss ? 1.0f : 0.2f,
                VirtualObstacleWeight = config.PrimaryTheme == WorldTheme.Virtual ? 1.0f : 0.2f,
                // Probabilità di pericoli speciali (ridotte nel tutorial)
                LavaObstacleProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Volcano ? 0.4f : 0.05f),
                IceObstacleProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Tundra ? 0.4f : 0.05f),
                DigitalBarrierProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Virtual ? 0.4f : 0.05f),
                UnderwaterProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Abyss ? 0.4f : 0.05f),
                SlipperyProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Tundra ? 0.3f : 0.05f),
                ToxicGroundProbability = isTutorial ? 0.0f : 0.1f,
                CurrentProbability = isTutorial ? 0.0f : (config.PrimaryTheme == WorldTheme.Abyss ? 0.3f : 0.05f),
                SpecialHazardDensity = isTutorial ? 0.05f : 0.2f
            });
            
            // Calcola la densità di nemici in base al tema e alla configurazione di difficoltà
            float enemyDensity = config.EnemyDensity;
            if (hasDifficultyConfig)
            {
                enemyDensity *= difficultyConfig.GetEnemyDensityScaleForTheme(config.PrimaryTheme, isTutorial);
            }
            
            CommandBuffer.AddComponent(entityInQueryIndex, levelEntity, new EnemySpawnConfigComponent
            {
                DensityFactor = enemyDensity,
                MinEnemies = isTutorial ? 0 : 1, // Possibilità di nessun nemico nel tutorial
                MaxEnemies = isTutorial ? 2 : 4, // Meno nemici nel tutorial
                DroneProbability = isTutorial ? 0.8f : 0.4f, // Più droni (più facili) nel tutorial
                PatrolProbability = isTutorial ? 0.2f : 0.4f,
                AmbushProbability = isTutorial ? 0.0f : 0.2f, // Nessuna imboscata nel tutorial
                // Pesi per tema
                CityEnemyWeight = config.PrimaryTheme == WorldTheme.City ? 1.0f : 0.2f,
                ForestEnemyWeight = config.PrimaryTheme == WorldTheme.Forest ? 1.0f : 0.2f,
                TundraEnemyWeight = config.PrimaryTheme == WorldTheme.Tundra ? 1.0f : 0.2f,
                VolcanoEnemyWeight = config.PrimaryTheme == WorldTheme.Volcano ? 1.0f : 0.2f,
                AbyssEnemyWeight = config.PrimaryTheme == WorldTheme.Abyss ? 1.0f : 0.2f,
                VirtualEnemyWeight = config.PrimaryTheme == WorldTheme.Virtual ? 1.0f : 0.2f,
                // Boss e gruppi (ridotti o disabilitati nel tutorial)
                MidBossProbability = isTutorial ? 0.0f : 0.1f, // Nessun mid-boss nel tutorial
                BossProbability = isTutorial ? 0.0f : 0.01f, // Nessun boss nel tutorial
                GroupSpawnProbability = isTutorial ? 0.0f : 0.3f, // Nessun gruppo nel tutorial
                MinGroupSize = 2,
                MaxGroupSize = isTutorial ? 2 : 4, // Gruppi più piccoli nel tutorial
                EliteEnemyProbability = isTutorial ? 0.0f : 0.1f // Nessun nemico élite nel tutorial
            });
            
            return levelEntity;
        }
        
        /// <summary>
        /// Genera i segmenti del percorso per il livello
        /// </summary>
        private void GeneratePathSegments(int entityInQueryIndex, Entity levelEntity, 
                                       int numSegments, RunnerLevelConfigComponent config,
                                       Unity.Mathematics.Random random)
        {
            // Ottieni la configurazione di difficoltà del mondo se disponibile
            WorldDifficultyConfigComponent difficultyConfig = default;
            bool hasDifficultyConfig = false;
            
            // Cerca la configurazione di difficoltà nel mondo
            // Approccio compatibile con EntityManager ma non con Burst
            var difficultyQuery = EntityManager.CreateEntityQuery(typeof(WorldDifficultyConfigComponent));
            if (difficultyQuery.CalculateEntityCount() > 0)
            {
                // Otteniamo il primo componente che troviamo (dovrebbe essere un singleton)
                var entity = difficultyQuery.GetSingletonEntity();
                if (EntityManager.HasComponent<WorldDifficultyConfigComponent>(entity))
                {
                    difficultyConfig = EntityManager.GetComponentData<WorldDifficultyConfigComponent>(entity);
                    hasDifficultyConfig = true;
                }
            }
            
            // Calcola la difficoltà iniziale e l'incremento per segmento
            int startDifficulty = config.StartDifficulty;
            int endDifficulty = config.EndDifficulty;
            float rampFactor = config.DifficultyRamp;
            
            // Applica configurazioni specifiche per tema se disponibili
            if (hasDifficultyConfig)
            {
                // Modifica la difficoltà iniziale in base al tema del mondo
                startDifficulty = math.max(1, math.min(10, difficultyConfig.GetBaseDifficultyForTheme(config.PrimaryTheme)));
                
                // Modifica il fattore di incremento in base al tema
                bool isTutorial = config.PrimaryTheme == WorldTheme.City && numSegments <= 20; // Approssimazione per identificare il tutorial
                rampFactor *= difficultyConfig.GetDifficultyRampScaleForTheme(config.PrimaryTheme, isTutorial);
                
                // Imposta la difficoltà finale scalando in base al fattore di incremento
                endDifficulty = math.max(startDifficulty, math.min(10, startDifficulty + (int)(rampFactor * 5)));
            }
            
            // Calcola l'incremento effettivo di difficoltà per segmento
            float difficultyStep = (endDifficulty - startDifficulty) / (float)numSegments;
            
            // Ottieni il buffer dei segmenti
            var pathBuffer = CommandBuffer.SetBuffer<PathSegmentBuffer>(entityInQueryIndex, levelEntity);
            
            // Crea il primo segmento (sempre attivo al caricamento del livello)
            Entity prevSegment = Entity.Null;
            
            for (int i = 0; i < numSegments; i++)
            {
                // Calcola la difficoltà corrente in base ai valori modificati dal sistema di difficoltà
                int currentDifficulty = startDifficulty + (int)(difficultyStep * i);
                
                // Determina il tipo di segmento in base alla posizione e al fattore di varietà
                SegmentType segmentType = DetermineSegmentType(i, numSegments, config.SegmentVarietyFactor, ref random);
                
                // Crea il segmento
                Entity segmentEntity = CreatePathSegment(
                    entityInQueryIndex,
                    levelEntity,
                    i,
                    currentDifficulty,
                    segmentType,
                    config.PrimaryTheme,
                    i == 0, // Il primo segmento è sempre attivo
                    prevSegment,
                    ref random
                );
                
                // Memorizza il riferimento al segmento appena creato
                pathBuffer.Add(new PathSegmentBuffer { SegmentEntity = segmentEntity });
                
                // Se non è il primo segmento, collega il precedente a questo
                if (i > 0 && prevSegment != Entity.Null)
                {
                    CommandBuffer.SetComponent(entityInQueryIndex, prevSegment, 
                                             new PathSegmentComponent { 
                                                 /* copia tutti i valori precedenti */ 
                                                 NextSegment = segmentEntity 
                                             });
                }
                
                // Aggiorna il riferimento al segmento precedente
                prevSegment = segmentEntity;
            }
        }
        
        /// <summary>
        /// Determina il tipo di segmento in base alla posizione e alla varietà richiesta
        /// </summary>
        private SegmentType DetermineSegmentType(int index, int totalSegments, float varietyFactor, ref Unity.Mathematics.Random random)
        {
            // Calcola la probabilità di segmenti speciali in base alla varietà
            float specialSegmentChance = math.lerp(0.1f, 0.5f, varietyFactor);
            
            // I primi segmenti sono sempre dritti (per iniziare in modo graduale)
            if (index < 2)
                return SegmentType.Straight;
                
            // L'ultimo segmento è sempre un checkpoint
            if (index == totalSegments - 1)
                return SegmentType.Checkpoint;
                
            // Ogni 5 segmenti mettiamo un checkpoint
            if (index > 0 && index % 5 == 0)
                return SegmentType.Checkpoint;
                
            // Per il resto, usiamo la probabilità per determinare segmenti speciali
            if (random.NextFloat() > specialSegmentChance)
                return SegmentType.Straight; // Segmento standard
                
            // Altrimenti scegliamo un tipo casuale di segmento speciale
            float typeRoll = random.NextFloat();
            
            if (typeRoll < 0.15f)
                return SegmentType.Uphill;
            else if (typeRoll < 0.3f)
                return SegmentType.Downhill;
            else if (typeRoll < 0.45f)
                return SegmentType.Curve;
            else if (typeRoll < 0.6f)
                return SegmentType.Jump;
            else if (typeRoll < 0.75f)
                return SegmentType.Narrow;
            else if (typeRoll < 0.9f)
                return SegmentType.Wide;
            else
                return SegmentType.Hazard;
        }
        
        /// <summary>
        /// Crea un segmento di percorso
        /// </summary>
        private Entity CreatePathSegment(int entityInQueryIndex,
                                      Entity levelEntity,
                                      int segmentIndex,
                                      int difficultyLevel,
                                      SegmentType type,
                                      WorldTheme theme,
                                      bool isActive,
                                      Entity prevSegment,
                                      ref Unity.Mathematics.Random random)
        {
            // Crea l'entità segmento
            Entity segmentEntity = CommandBuffer.CreateEntity(entityInQueryIndex);
            
            // Calcola la posizione in base all'indice (per ora semplice, lineare)
            float3 startPos = new float3(0, 0, segmentIndex * DEFAULT_SEGMENT_LENGTH);
            float3 endPos = new float3(0, 0, (segmentIndex + 1) * DEFAULT_SEGMENT_LENGTH);
            
            // Per curve e altri tipi speciali, potremmo voler modificare la posizione
            if (type == SegmentType.Curve)
            {
                // Implementazione di curve per esempio
                float curveAmount = 10f; // Quantità di curvatura in unità
                float curveDir = random.NextFloat() > 0.5f ? 1f : -1f; // Direzione casuale
                
                endPos.x += curveDir * curveAmount;
            }
            else if (type == SegmentType.Uphill)
            {
                endPos.y += 5f; // Salita di 5 unità
            }
            else if (type == SegmentType.Downhill)
            {
                endPos.y -= 5f; // Discesa di 5 unità
            }
            
            // Aggiungi il componente segmento
            CommandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new PathSegmentComponent
            {
                StartPosition = startPos,
                EndPosition = endPos,
                Rotation = quaternion.identity,
                Length = DEFAULT_SEGMENT_LENGTH,
                Width = type == SegmentType.Narrow ? 4f : 8f,
                Type = type,
                SegmentIndex = segmentIndex,
                DifficultyLevel = difficultyLevel,
                Theme = theme,
                IsActive = isActive,
                IsGenerated = false,
                NextSegment = Entity.Null // Sarà impostato in seguito
            });
            
            // Aggiungi il componente transform
            CommandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new LocalTransform
            {
                Position = startPos,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            // Aggiungi il buffer per i contenuti del segmento
            CommandBuffer.AddBuffer<SegmentContentBuffer>(entityInQueryIndex, segmentEntity);
            
            // Se il segmento è attivo, aggiungere un tag per generare i contenuti immediatamente
            if (isActive)
            {
                CommandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new RequiresContentGenerationTag());
            }
            
            return segmentEntity;
        }
    }
}

/// <summary>
/// Tag per indicare che un segmento richiede la generazione dei contenuti
/// </summary>
public struct RequiresContentGenerationTag : IComponentData { }
}