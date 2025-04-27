using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Core
{
    /// <summary>
    /// Componente che memorizza la posizione, rotazione e scala di un'entità.
    /// Usato per entità che richiedono un posizionamento nello spazio 3D.
    /// </summary>
    [Serializable]
    public struct TransformComponent : IComponentData
    {
        /// <summary>
        /// Posizione dell'entità nello spazio 3D
        /// </summary>
        public float3 Position;
        
        /// <summary>
        /// Rotazione dell'entità come quaternione
        /// </summary>
        public quaternion Rotation;
        
        /// <summary>
        /// Scala uniforme dell'entità
        /// </summary>
        public float Scale;
        
        /// <summary>
        /// Crea un nuovo TransformComponent con valori predefiniti
        /// </summary>
        /// <returns>TransformComponent inizializzato con valori predefiniti</returns>
        public static TransformComponent Default()
        {
            return new TransformComponent
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1.0f
            };
        }
        
        /// <summary>
        /// Crea un nuovo TransformComponent con una posizione specifica
        /// </summary>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>TransformComponent inizializzato con la posizione specificata</returns>
        public static TransformComponent WithPosition(float3 position)
        {
            return new TransformComponent
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1.0f
            };
        }
    }
}