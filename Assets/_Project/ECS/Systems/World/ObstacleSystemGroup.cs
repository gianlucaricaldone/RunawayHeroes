using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che raggruppa tutti i sistemi relativi agli ostacoli
    /// </summary>
    [UpdateAfter(typeof(SegmentContentGenerationSystem))]
    partial class ObstacleSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Aggiungi i sistemi al gruppo
            // Nota: ThematicObstacleGenerationSystem è un ISystem (struct), non un ComponentSystemBase
            // In DOTS 1.3.14, dobbiamo aggiungere i sistemi ISystem in modo diverso
            
            // Ottieni un SystemHandle per il sistema ThematicObstacleGenerationSystem
            var thematicObstacleSystemHandle = World.GetExistingSystem<ThematicObstacleGenerationSystem>();
            if (thematicObstacleSystemHandle == SystemHandle.Null)
            {
                thematicObstacleSystemHandle = World.CreateSystem<ThematicObstacleGenerationSystem>();
            }
            
            // Aggiungi il sistema al gruppo tramite il suo SystemHandle
            AddSystemToUpdateList(thematicObstacleSystemHandle);
        }
    }
}