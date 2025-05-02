using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Systems.World
{
    /// <summary>
    /// Sistema che raggruppa tutti i sistemi relativi agli ostacoli
    /// </summary>
    [UpdateAfter(typeof(SegmentContentGenerationSystem))]
    public class ObstacleSystemGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Aggiungi i sistemi al gruppo
            World.GetOrCreateSystemManaged<ThematicObstacleGenerationSystem>().AddSystemToUpdateList(this);
        }
    }
}