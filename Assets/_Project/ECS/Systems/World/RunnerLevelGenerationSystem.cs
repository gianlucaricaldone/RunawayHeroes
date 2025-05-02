using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.World;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile della generazione procedurale dei livelli per il gioco runner
    /// </summary>
    public partial class RunnerLevelGenerationSystem : SystemBase
    {
        private EntityCommandBufferSystem _commandBufferSystem;
        private Unity.Mathematics.Random _random;
        private const float DEFAULT_SEGMENT_LENGTH = 30f; // Lunghezza predefinita di un segmento in metri
        
        protected override void OnCreate()
        {
            // Ottieni il sistema di command buffer per la creazione/distruzione di entità
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi che il sistema venga eseguito solo se esiste almeno un'entità 
            // con RunnerLevelConfigComponent
            RequireForUpdate<RunnerLevelConfigComponent>();
        }

        protected override void OnUpdate()
        {
            // Command buffer per operazioni di creazione/modifica entità
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Elabora le richieste di generazione livello
            Entities
                .WithName("ProcessRunnerLevelRequests")
                .ForEach((Entity entity, int entityInQueryIndex, 
                         in RunnerLevelConfigComponent config,
                         in LocalTransform transform) =>
                {
                    // Inizializza il generatore casuale con il seed fornito
                    _random = Unity.Mathematics.Random.CreateFromIndex((uint)config.Seed);
                    
                    // Calcola il numero di segmenti in base alla lunghezza desiderata
                    int numSegments = math.min(
                        math.max(config.MinSegments, config.LevelLength / (int)DEFAULT_SEGMENT_LENGTH),
                        config.MaxSegments
                    );
                    
                    // Genera l'entità del livello
                    Entity levelEntity = GenerateLevel(entityInQueryIndex, config, numSegments, transform, ref commandBuffer);
                    
                    // Genera i segmenti del percorso
                    GeneratePathSegments(entityInQueryIndex, levelEntity, numSegments, config, ref commandBuffer);
                    
                    // Rimuovi il componente di configurazione per evitare di rigenerare
                    commandBuffer.RemoveComponent<RunnerLevelConfigComponent>(entityInQueryIndex, entity);
                    
                }).ScheduleParallel();
            
            // Assicurati che i comandi vengano eseguiti
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Genera l'entità principale del livello
        /// </summary>
        private Entity GenerateLevel(int entityInQueryIndex, RunnerLevelConfigComponent config, 
                                  int numSegments, LocalTransform transform,
                                  ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Crea l'entità del livello
            Entity levelEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti base
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, new LevelComponent());
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, transform);
            
            // Aggiungi il componente di tema del mondo
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, new WorldIdentifierComponent());
            
            // Aggiungi il buffer per i segmenti di percorso
            commandBuffer.AddBuffer<PathSegmentBuffer>(entityInQueryIndex, levelEntity);
            
            // Aggiungi il componente per le configurazioni di spawn
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, new ObstacleSpawnConfigComponent
            {
                DensityFactor = config.ObstacleDensity,
                MinObstacles = 1,
                MaxObstacles = 5,
                SmallObstacleProbability = 0.5f,
                MediumObstacleProbability = 0.3f,
                LargeObstacleProbability = 0.2f,
                // Pesi per tema
                CityObstacleWeight = config.PrimaryTheme == WorldTheme.City ? 1.0f : 0.2f,
                ForestObstacleWeight = config.PrimaryTheme == WorldTheme.Forest ? 1.0f : 0.2f,
                TundraObstacleWeight = config.PrimaryTheme == WorldTheme.Tundra ? 1.0f : 0.2f,
                VolcanoObstacleWeight = config.PrimaryTheme == WorldTheme.Volcano ? 1.0f : 0.2f,
                AbyssObstacleWeight = config.PrimaryTheme == WorldTheme.Abyss ? 1.0f : 0.2f,
                VirtualObstacleWeight = config.PrimaryTheme == WorldTheme.Virtual ? 1.0f : 0.2f,
                // Probabilità di pericoli speciali
                LavaObstacleProbability = config.PrimaryTheme == WorldTheme.Volcano ? 0.4f : 0.05f,
                IceObstacleProbability = config.PrimaryTheme == WorldTheme.Tundra ? 0.4f : 0.05f,
                DigitalBarrierProbability = config.PrimaryTheme == WorldTheme.Virtual ? 0.4f : 0.05f,
                UnderwaterProbability = config.PrimaryTheme == WorldTheme.Abyss ? 0.4f : 0.05f,
                SlipperyProbability = config.PrimaryTheme == WorldTheme.Tundra ? 0.3f : 0.05f,
                ToxicGroundProbability = 0.1f,
                CurrentProbability = config.PrimaryTheme == WorldTheme.Abyss ? 0.3f : 0.05f,
                SpecialHazardDensity = 0.2f
            });
            
            commandBuffer.AddComponent(entityInQueryIndex, levelEntity, new EnemySpawnConfigComponent
            {
                DensityFactor = config.EnemyDensity,
                MinEnemies = 0,
                MaxEnemies = 4,
                DroneProbability = 0.4f,
                PatrolProbability = 0.4f,
                AmbushProbability = 0.2f,
                // Pesi per tema
                CityEnemyWeight = config.PrimaryTheme == WorldTheme.City ? 1.0f : 0.2f,
                ForestEnemyWeight = config.PrimaryTheme == WorldTheme.Forest ? 1.0f : 0.2f,
                TundraEnemyWeight = config.PrimaryTheme == WorldTheme.Tundra ? 1.0f : 0.2f,
                VolcanoEnemyWeight = config.PrimaryTheme == WorldTheme.Volcano ? 1.0f : 0.2f,
                AbyssEnemyWeight = config.PrimaryTheme == WorldTheme.Abyss ? 1.0f : 0.2f,
                VirtualEnemyWeight = config.PrimaryTheme == WorldTheme.Virtual ? 1.0f : 0.2f,
                // Boss e gruppi
                MidBossProbability = 0.1f,
                BossProbability = 0.01f,
                GroupSpawnProbability = 0.3f,
                MinGroupSize = 2,
                MaxGroupSize = 4,
                EliteEnemyProbability = 0.1f
            });
            
            return levelEntity;
        }
        
        /// <summary>
        /// Genera i segmenti del percorso per il livello
        /// </summary>
        private void GeneratePathSegments(int entityInQueryIndex, Entity levelEntity, 
                                       int numSegments, RunnerLevelConfigComponent config,
                                       ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Calcola la difficoltà iniziale e l'incremento per segmento
            float difficultyStep = (config.EndDifficulty - config.StartDifficulty) / (float)numSegments;
            
            // Ottieni il buffer dei segmenti
            var pathBuffer = commandBuffer.SetBuffer<PathSegmentBuffer>(entityInQueryIndex, levelEntity);
            
            // Crea il primo segmento (sempre attivo al caricamento del livello)
            Entity prevSegment = Entity.Null;
            
            for (int i = 0; i < numSegments; i++)
            {
                // Calcola la difficoltà corrente
                int currentDifficulty = config.StartDifficulty + (int)(difficultyStep * i);
                
                // Determina il tipo di segmento in base alla posizione e al fattore di varietà
                SegmentType segmentType = DetermineSegmentType(i, numSegments, config.SegmentVarietyFactor);
                
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
                    ref commandBuffer
                );
                
                // Memorizza il riferimento al segmento appena creato
                pathBuffer.Add(new PathSegmentBuffer { SegmentEntity = segmentEntity });
                
                // Se non è il primo segmento, collega il precedente a questo
                if (i > 0 && prevSegment != Entity.Null)
                {
                    commandBuffer.SetComponent(entityInQueryIndex, prevSegment, 
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
        private SegmentType DetermineSegmentType(int index, int totalSegments, float varietyFactor)
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
            if (_random.NextFloat() > specialSegmentChance)
                return SegmentType.Straight; // Segmento standard
                
            // Altrimenti scegliamo un tipo casuale di segmento speciale
            float typeRoll = _random.NextFloat();
            
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
                                      ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Crea l'entità segmento
            Entity segmentEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Calcola la posizione in base all'indice (per ora semplice, lineare)
            float3 startPos = new float3(0, 0, segmentIndex * DEFAULT_SEGMENT_LENGTH);
            float3 endPos = new float3(0, 0, (segmentIndex + 1) * DEFAULT_SEGMENT_LENGTH);
            
            // Per curve e altri tipi speciali, potremmo voler modificare la posizione
            if (type == SegmentType.Curve)
            {
                // Implementazione di curve per esempio
                float curveAmount = 10f; // Quantità di curvatura in unità
                float curveDir = _random.NextFloat() > 0.5f ? 1f : -1f; // Direzione casuale
                
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
            commandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new PathSegmentComponent
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
            commandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new LocalTransform
            {
                Position = startPos,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            // Aggiungi il buffer per i contenuti del segmento
            commandBuffer.AddBuffer<SegmentContentBuffer>(entityInQueryIndex, segmentEntity);
            
            // Se il segmento è attivo, aggiungere un tag per generare i contenuti immediatamente
            if (isActive)
            {
                commandBuffer.AddComponent(entityInQueryIndex, segmentEntity, new RequiresContentGenerationTag());
            }
            
            return segmentEntity;
        }
    }
    
    /// <summary>
    /// Tag per indicare che un segmento richiede la generazione dei contenuti
    /// </summary>
    public struct RequiresContentGenerationTag : IComponentData { }
}