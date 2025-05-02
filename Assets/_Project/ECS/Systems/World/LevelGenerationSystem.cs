using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.World;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema base per la generazione di livelli che coordina sia la generazione predefinita
    /// che quella procedurale per runner
    /// </summary>
    public partial class LevelGenerationSystem : SystemBase
    {
        private EntityCommandBufferSystem _commandBufferSystem;
        private RunnerLevelGenerationSystem _runnerLevelSystem;
        private SegmentContentGenerationSystem _contentGenerationSystem;
        
        protected override void OnCreate()
        {
            // Inizializza i sistemi correlati
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            _runnerLevelSystem = World.GetOrCreateSystemManaged<RunnerLevelGenerationSystem>();
            _contentGenerationSystem = World.GetOrCreateSystemManaged<SegmentContentGenerationSystem>();
            
            // Richiedi che il sistema venga eseguito solo se esiste almeno un'entità 
            // con LevelComponent
            RequireForUpdate<LevelComponent>();
        }

        protected override void OnUpdate()
        {
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
            
            // Crea livelli predefiniti (non casuali)
            Entities
                .WithName("ProcessLevelGenerationRequests")
                .WithNone<RunnerLevelConfigComponent>() // Escludi quelli randomizzati
                .ForEach((Entity entity, int entityInQueryIndex, 
                         in LevelComponent level,
                         in LocalTransform transform) =>
                {
                    // Gestisci la creazione di livelli predefiniti (non randomizzati)
                    GeneratePredefinedLevel(entity, entityInQueryIndex, ref commandBuffer);
                    
                }).ScheduleParallel();
            
            // Assicurati che i comandi vengano eseguiti
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Genera un livello predefinito in base ai dati di configurazione
        /// </summary>
        private void GeneratePredefinedLevel(Entity levelEntity, int entityInQueryIndex,
                                           ref EntityCommandBuffer.ParallelWriter commandBuffer)
        {
            // Implementazione della generazione di livelli predefiniti (non randomizzati)
            // Questa sarebbe basata su dati di configurazione o asset predefiniti
            // ...
        }
        
        /// <summary>
        /// Crea un'entità con configurazione per la generazione di un livello runner casuale
        /// </summary>
        public Entity CreateRunnerLevelRequest(WorldTheme theme, int levelLength, int seed)
        {
            // Crea una nuova entità
            var entity = EntityManager.CreateEntity();
            
            // Aggiungi la configurazione di livello casuale
            EntityManager.AddComponentData(entity, new RunnerLevelConfigComponent
            {
                LevelLength = levelLength,
                MinSegments = levelLength / 50,  // Un segmento ogni 50 metri circa
                MaxSegments = levelLength / 20,  // Un segmento ogni 20 metri circa
                Seed = seed,
                StartDifficulty = 1,
                EndDifficulty = 8,
                DifficultyRamp = 0.6f,
                ObstacleDensity = 0.7f,
                EnemyDensity = 0.5f,
                CollectibleDensity = 0.6f,
                PrimaryTheme = theme,
                SecondaryTheme = GetComplementaryTheme(theme),
                ThemeBlendFactor = 0.2f,
                GenerateCheckpoints = true,
                DynamicDifficulty = true,
                SegmentVarietyFactor = 0.7f
            });
            
            // Aggiungi il componente transform
            EntityManager.AddComponentData(entity, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1.0f
            });
            
            return entity;
        }
        
        /// <summary>
        /// Ottiene un tema complementare per creare varietà nei livelli
        /// </summary>
        private WorldTheme GetComplementaryTheme(WorldTheme primaryTheme)
        {
            switch (primaryTheme)
            {
                case WorldTheme.City:
                    return WorldTheme.Virtual;
                case WorldTheme.Forest:
                    return WorldTheme.Tundra;
                case WorldTheme.Tundra:
                    return WorldTheme.Volcano;
                case WorldTheme.Volcano:
                    return WorldTheme.Abyss;
                case WorldTheme.Abyss:
                    return WorldTheme.Forest;
                case WorldTheme.Virtual:
                    return WorldTheme.City;
                default:
                    return WorldTheme.City;
            }
        }
    }
}