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
        /// <param name="theme">Tema del mondo da generare</param>
        /// <param name="levelLength">Lunghezza del livello in metri</param>
        /// <param name="seed">Seed per la generazione casuale</param>
        /// <param name="isTutorial">Indica se il livello è un tutorial (difficoltà ridotta)</param>
        public Entity CreateRunnerLevelRequest(WorldTheme theme, int levelLength, int seed, bool isTutorial = false)
        {
            // Crea una nuova entità
            var entity = EntityManager.CreateEntity();
            
            // Ottieni la configurazione di difficoltà se disponibile
            WorldDifficultyConfigComponent difficultyConfig = default;
            bool hasDifficultyConfig = false;
            
            var difficultyQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
            if (difficultyQuery.HasAnyEntities())
            {
                difficultyConfig = difficultyQuery.GetSingleton<WorldDifficultyConfigComponent>();
                hasDifficultyConfig = true;
            }
            
            // Valori predefiniti per la configurazione
            int startDifficulty = 1;
            int endDifficulty = 8;
            float difficultyRamp = 0.6f;
            float obstacleDensity = 0.7f;
            float enemyDensity = 0.5f;
            float segmentVarietyFactor = 0.7f;
            
            // Adatta la configurazione in base al tema e alla difficoltà del mondo
            if (hasDifficultyConfig)
            {
                // Imposta la difficoltà iniziale in base al tema
                startDifficulty = math.max(1, difficultyConfig.GetBaseDifficultyForTheme(theme));
                
                // Riduci la difficoltà se è un tutorial
                if (isTutorial)
                {
                    startDifficulty = math.min(startDifficulty, difficultyConfig.TutorialBaseDifficulty);
                    endDifficulty = math.min(4, startDifficulty + 2); // Massimo difficoltà 4 per tutorial
                    difficultyRamp = difficultyConfig.TutorialDifficultyRampScale;
                    obstacleDensity *= difficultyConfig.TutorialObstacleDensityScale;
                    enemyDensity *= difficultyConfig.TutorialEnemyDensityScale;
                    segmentVarietyFactor = 0.4f; // Minore varietà nel tutorial
                }
                else
                {
                    // Per livelli non-tutorial, scala la difficoltà in base al tema
                    endDifficulty = math.min(10, startDifficulty + 4); // Massimo incremento 4 livelli
                    difficultyRamp *= difficultyConfig.GetDifficultyRampScaleForTheme(theme);
                    obstacleDensity *= difficultyConfig.GetObstacleDensityScaleForTheme(theme);
                    enemyDensity *= difficultyConfig.GetEnemyDensityScaleForTheme(theme);
                    
                    // Incrementa la varietà per mondi più avanzati
                    if (theme == WorldTheme.Volcano || theme == WorldTheme.Abyss)
                    {
                        segmentVarietyFactor = 0.9f; // Massima varietà nei mondi avanzati
                    }
                }
            }
            
            // Aggiungi la configurazione di livello casuale con i valori scalati
            EntityManager.AddComponentData(entity, new RunnerLevelConfigComponent
            {
                LevelLength = levelLength,
                MinSegments = levelLength / 50,  // Un segmento ogni 50 metri circa
                MaxSegments = levelLength / 20,  // Un segmento ogni 20 metri circa
                Seed = seed,
                StartDifficulty = startDifficulty,
                EndDifficulty = endDifficulty,
                DifficultyRamp = difficultyRamp,
                ObstacleDensity = obstacleDensity,
                EnemyDensity = enemyDensity,
                CollectibleDensity = isTutorial ? 0.8f : 0.6f, // Più collezionabili nel tutorial
                PrimaryTheme = theme,
                SecondaryTheme = GetComplementaryTheme(theme),
                ThemeBlendFactor = isTutorial ? 0.1f : 0.2f, // Minore mescolamento nel tutorial
                GenerateCheckpoints = true,
                DynamicDifficulty = !isTutorial, // Disabilita difficoltà dinamica nel tutorial
                SegmentVarietyFactor = segmentVarietyFactor
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