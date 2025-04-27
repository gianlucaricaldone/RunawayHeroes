using Unity.Entities;

namespace RunawayHeroes.ECS.Entities
{
    /// <summary>
    /// 
    /// </summary>
    public static class MutantKraken
    {
        public static Entity Create(EntityManager entityManager)
        {
            Entity entity = entityManager.CreateEntity();
            // Aggiungi componenti
            return entity;
        }
    }
}
