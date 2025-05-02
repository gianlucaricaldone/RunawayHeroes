using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RunawayHeroes.ECS.Components.World;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce la difficoltà progressiva del gioco, sia all'interno di un livello
    /// che tra diversi mondi tematici. Si occupa di regolare parametri di difficoltà in base
    /// al progresso del giocatore e al tema del mondo corrente.
    /// </summary>
    public partial class DifficultySystem : SystemBase
    {
        private EntityQuery _worldConfigQuery;
        private const float DEFAULT_DIFFICULTY_UPDATE_INTERVAL = 3.0f; // Secondi
        private float _timeSinceLastUpdate = 0f;
        
        protected override void OnCreate()
        {
            // Crea la query per la configurazione di difficoltà del mondo
            _worldConfigQuery = GetEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
            
            // Richiedi che il sistema venga eseguito solo quando esiste almeno un livello attivo
            RequireForUpdate<LevelComponent>();
            
            // Crea una configurazione di difficoltà predefinita se non esiste già
            if (!_worldConfigQuery.HasAnyEntities())
            {
                CreateDefaultWorldDifficultyConfig();
            }
        }

        protected override void OnUpdate()
        {
            // Aggiorna solo a intervalli regolari per risparmiare risorse
            _timeSinceLastUpdate += Time.DeltaTime;
            if (_timeSinceLastUpdate < DEFAULT_DIFFICULTY_UPDATE_INTERVAL)
                return;
                
            _timeSinceLastUpdate = 0f;
            
            // Ottieni la configurazione di difficoltà corrente
            var difficultyConfig = GetSingleton<WorldDifficultyConfigComponent>();
            var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
            
            // Applica modificatori di difficoltà ai segmenti di percorso
            Entities
                .WithName("ApplyDifficultyToSegments")
                .ForEach((Entity entity, ref PathSegmentComponent segment, in LocalTransform transform) =>
                {
                    // Verifica se il segmento è attivo prima di aggiornarne la difficoltà
                    if (!segment.IsActive)
                        return;
                    
                    // Determina se è un livello tutorial
                    bool isTutorial = segment.SegmentIndex < 10;
                    
                    // Ottieni fattori di difficoltà specifici per il tema
                    float difficultyFactor = difficultyConfig.GetDifficultyRampScaleForTheme(segment.Theme, isTutorial);
                    
                    // Calcola una difficoltà di base in funzione del progresso del livello
                    int baseDifficulty = difficultyConfig.GetBaseDifficultyForTheme(segment.Theme);
                    
                    // Se è un tutorial, mantieni una difficoltà più bassa
                    if (isTutorial)
                    {
                        baseDifficulty = difficultyConfig.TutorialBaseDifficulty;
                        
                        // Crea zone sicure nel tutorial se abilitato
                        if (difficultyConfig.EnableTutorialSafezones && segment.SegmentIndex % 3 == 0)
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
                    
                }).Run();
            
            // Applica modificatori di difficoltà ai nemici in base alla difficoltà del segmento
            ApplyDifficultyToEnemies();
            
            // Applica modificatori di difficoltà agli ostacoli in base alla difficoltà del segmento
            ApplyDifficultyToObstacles();
            
            commandBuffer.Dispose();
        }
        
        /// <summary>
        /// Crea una configurazione di difficoltà predefinita nel mondo
        /// </summary>
        private void CreateDefaultWorldDifficultyConfig()
        {
            var entity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(entity, WorldDifficultyConfigComponent.CreateDefault());
        }
        
        /// <summary>
        /// Applica modificatori di difficoltà a tutti i nemici in base alla difficoltà del segmento
        /// </summary>
        private void ApplyDifficultyToEnemies()
        {
            // Implementazione dell'aumento di difficoltà dei nemici
            // Qui si modificherebbero parametri come velocità, danno, salute, ecc.
            // in base al livello di difficoltà del segmento in cui si trovano
            
            // Questa implementazione sarebbe più complessa e richiederebbe l'accesso
            // ai parametri specifici dei nemici, ma l'idea generale è quella di
            // scalare le loro caratteristiche in base al livello di difficoltà
        }
        
        /// <summary>
        /// Applica modificatori di difficoltà a tutti gli ostacoli in base alla difficoltà del segmento
        /// </summary>
        private void ApplyDifficultyToObstacles()
        {
            // Implementazione dell'aumento di difficoltà degli ostacoli
            // Qui si modificherebbero parametri come danno, dimensioni, ecc.
            // in base al livello di difficoltà del segmento in cui si trovano
            
            // Questa implementazione sarebbe più complessa e richiederebbe l'accesso
            // ai parametri specifici degli ostacoli, ma l'idea generale è quella di
            // scalare le loro caratteristiche in base al livello di difficoltà
        }
    }
}