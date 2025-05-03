using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.World.Obstacles;
using System;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema responsabile della generazione di ostacoli tematici nei segmenti di percorso
    /// </summary>
    public partial class ThematicObstacleGenerationSystem : SystemBase
    {
        private Unity.Mathematics.Random _random;
        private uint _seed;
        
        protected override void OnCreate()
        {
            // Richiedi il singleton per il command buffer
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Inizializza il generatore di numeri casuali
            _seed = (uint)DateTime.Now.Ticks;
            _random = Unity.Mathematics.Random.CreateFromIndex(_seed);
            
            // Richiedi che il sistema venga eseguito solo quando ci sono segmenti che richiedono generazione
            RequireForUpdate<RequiresContentGenerationTag>();
        }

        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(World.Unmanaged).AsParallelWriter();
            var random = _random;
            
            // Ottieni le configurazioni di spawn dal livello
            var obstacleConfig = SystemAPI.GetSingleton<ObstacleSpawnConfigComponent>();
            
            // Creiamo una copia locale delle variabili di istanza
            var seed = _seed;
            
            // Genera ostacoli per i segmenti che lo richiedono
            Entities
                .WithName("GenerateThematicObstacles")
                .WithAll<RequiresContentGenerationTag>()
                .WithoutBurst()
                .Run((Entity segmentEntity, int entityInQueryIndex,
                      ref PathSegmentComponent segment,
                      ref DynamicBuffer<SegmentContentBuffer> contentBuffer) =>
                {
                    // Inizializza un generatore casuale deterministico per questo segmento specifico
                    Unity.Mathematics.Random segRandom = Unity.Mathematics.Random.CreateFromIndex(
                        (uint)(segment.SegmentIndex + seed));
                    
                    // Salta la generazione per segmenti speciali come checkpoint
                    if (segment.Type == SegmentType.Checkpoint)
                        return;
                    
                    // Genera ostacoli tematici specifici per il mondo
                    GenerateThematicObstacles(
                        entityInQueryIndex,
                        segmentEntity,
                        ref segment,
                        contentBuffer,
                        obstacleConfig,
                        ref commandBuffer,
                        segRandom);
                });
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
        }
        
        /// <summary>
        /// Genera ostacoli tematici per un segmento di percorso
        /// </summary>
        private void GenerateThematicObstacles(
            int entityInQueryIndex,
            Entity segmentEntity,
            ref PathSegmentComponent segment,
            DynamicBuffer<SegmentContentBuffer> contentBuffer,
            ObstacleSpawnConfigComponent config,
            ref EntityCommandBuffer.ParallelWriter commandBuffer,
            Unity.Mathematics.Random random)
        {
            // Determina se è un tutorial (primi 10 segmenti)
            bool isTutorial = segment.SegmentIndex < 10;
            
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
                case SegmentType.Checkpoint:
                    return; // Nessun ostacolo nei checkpoint
            }
            
            // Nel tutorial, riduci ulteriormente la densità
            if (isTutorial)
            {
                densityMultiplier *= 0.5f;
            }
            
            // Calcola il numero di ostacoli da generare
            int numObstacles = random.NextInt(
                math.max(1, (int)(config.MinObstacles * densityMultiplier)),
                math.max(2, (int)(config.MaxObstacles * densityMultiplier * config.DensityFactor)) + 1
            );
            
            // Per il tutorial, limita il numero massimo di ostacoli
            if (isTutorial)
            {
                numObstacles = math.min(numObstacles, 2);
            }
            
            // Genera gli ostacoli
            for (int i = 0; i < numObstacles; i++)
            {
                // Determina la posizione dell'ostacolo lungo il segmento
                float segmentProgress = random.NextFloat(0.15f, 0.85f);
                float3 obstaclePos = math.lerp(segment.StartPosition, segment.EndPosition, segmentProgress);
                
                // Distribuisci gli ostacoli anche lateralmente
                float lateralPosition = random.NextFloat(-segment.Width/2f + 1f, segment.Width/2f - 1f);
                obstaclePos.x += lateralPosition;
                
                // Determina la rotazione dell'ostacolo
                quaternion rotation = quaternion.EulerZXY(0, random.NextFloat(0, math.PI * 2), 0);
                
                // Determina la scala dell'ostacolo (variazione casuale)
                float scale = random.NextFloat(0.8f, 1.2f);
                
                // Scegli l'ostacolo in base al tema e alla difficoltà
                string obstacleCode = SelectObstacleForTheme(
                    segment.Theme, 
                    segment.DifficultyLevel, 
                    isTutorial, 
                    config, 
                    random);
                
                // Crea l'ostacolo
                Entity obstacleEntity = ObstacleFactory.CreateObstacle(
                    commandBuffer.AsParallelWriter(), 
                    obstacleCode, 
                    obstaclePos, 
                    rotation, 
                    scale);
                
                // Aggiungi l'ostacolo al buffer del segmento
                contentBuffer.Add(new SegmentContentBuffer
                {
                    ContentEntity = obstacleEntity,
                    Type = DetermineContentTypeFromObstacleCode(obstacleCode)
                });
            }
        }
        
        /// <summary>
        /// Seleziona un codice di ostacolo appropriato per il tema e la difficoltà
        /// </summary>
        private string SelectObstacleForTheme(
            WorldTheme theme, 
            int difficultyLevel, 
            bool isTutorial, 
            ObstacleSpawnConfigComponent config, 
            Unity.Mathematics.Random random)
        {
            // Nel tutorial, usa principalmente ostacoli universali e semplici
            if (isTutorial)
            {
                if (random.NextFloat() < 0.8f)
                {
                    // Usa solo gli ostacoli universali più semplici nel tutorial
                    int id = random.NextInt(1, 4); // U01-U03
                    return $"U{id:D2}";
                }
            }
            
            // Determina se usare un ostacolo universale o specifico per tema
            float themeSpecificChance = 0.7f; // 70% di probabilità di usare un ostacolo specifico per tema
            
            if (random.NextFloat() > themeSpecificChance)
            {
                // Scegli un ostacolo universale
                int id = random.NextInt(1, 9); // U01-U08
                return $"U{id:D2}";
            }
            
            // Altrimenti scegli un ostacolo specifico per tema
            string prefix;
            float specialHazardChance = 0.0f;
            
            switch (theme)
            {
                case WorldTheme.City:
                    prefix = "C";
                    specialHazardChance = 0.0f; // No hazard speciali nel tema città
                    break;
                case WorldTheme.Forest:
                    prefix = "F";
                    // Probabilità di piante velenose o nidi di vespe
                    specialHazardChance = config.ToxicGroundProbability;
                    break;
                case WorldTheme.Tundra:
                    prefix = "T";
                    // Probabilità di ostacoli di ghiaccio
                    specialHazardChance = config.IceObstacleProbability;
                    break;
                case WorldTheme.Volcano:
                    prefix = "V";
                    // Probabilità di ostacoli di lava
                    specialHazardChance = config.LavaObstacleProbability;
                    break;
                case WorldTheme.Abyss:
                    prefix = "A";
                    // Probabilità di ostacoli subacquei
                    specialHazardChance = config.UnderwaterProbability;
                    break;
                case WorldTheme.Virtual:
                    prefix = "D";
                    // Probabilità di barriere digitali
                    specialHazardChance = config.DigitalBarrierProbability;
                    break;
                default:
                    prefix = "U";
                    specialHazardChance = 0.0f;
                    break;
            }
            
            // Per i livelli tutorial, riduci drasticamente le probabilità di pericoli speciali
            if (isTutorial)
            {
                specialHazardChance *= 0.1f; // Riduci al 10%
            }
            
            // Aumenta la probabilità di pericoli speciali in base alla difficoltà
            specialHazardChance *= (1.0f + (difficultyLevel - 1) * 0.1f);
            
            // Controlla se generare un pericolo speciale in base al tema
            if (random.NextFloat() < specialHazardChance * config.SpecialHazardDensity)
            {
                // Seleziona un ostacolo speciale per il tema corrente
                // (gli ID 1-3 sono generalmente pericoli specifici del tema)
                int id = random.NextInt(1, 4);
                return $"{prefix}{id:D2}";
            }
            
            // Altrimenti scegli un ostacolo standard per questo tema
            int obstacleId = random.NextInt(1, 9); // ID da 1 a 8
            return $"{prefix}{obstacleId:D2}";
        }
        
        /// <summary>
        /// Determina il tipo di contenuto del segmento in base al codice dell'ostacolo
        /// </summary>
        private SegmentContentType DetermineContentTypeFromObstacleCode(string obstacleCode)
        {
            var obstacleType = ObstacleCatalog.GetObstacleByCode(obstacleCode);
            
            // Determina il tipo di contenuto in base alla categoria dell'ostacolo
            switch (obstacleType.Category)
            {
                case ObstacleCategory.SmallBarrier:
                case ObstacleCategory.Gap:
                case ObstacleCategory.GroundHazard:
                    return SegmentContentType.SmallObstacle;
                    
                case ObstacleCategory.LargeBarrier:
                case ObstacleCategory.HangingObject:
                case ObstacleCategory.NaturalObstacle:
                case ObstacleCategory.Vehicle:
                    return SegmentContentType.MediumObstacle;
                    
                case ObstacleCategory.MovingObstacle:
                case ObstacleCategory.SpecialBarrier:
                case ObstacleCategory.FireObstacle:
                case ObstacleCategory.IceObstacle:
                case ObstacleCategory.WaterObstacle:
                case ObstacleCategory.DigitalObstacle:
                case ObstacleCategory.ElectronicObstacle:
                    return SegmentContentType.LargeObstacle;
                    
                default:
                    return SegmentContentType.SmallObstacle;
            }
        }
    }
}