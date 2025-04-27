using Unity.Entities;
using UnityEngine;

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// 
    /// </summary>
    public class PlayerBridge : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Conversione da GameObject a Entity
        }
    }
}
