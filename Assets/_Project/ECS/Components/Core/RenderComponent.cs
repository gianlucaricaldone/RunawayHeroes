using System;
using Unity.Entities;
using Unity.Mathematics;

namespace RunawayHeroes.ECS.Components.Core
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct RenderComponent : IComponentData
    {
        /// <summary>
        /// Variante del modello da utilizzare per il rendering
        /// </summary>
        public byte ModelVariant;
        
        /// <summary>
        /// Visibilità del modello
        /// </summary>
        public bool IsVisible;
        
        /// <summary>
        /// Colore primario del modello
        /// </summary>
        public float4 Color;
        
        /// <summary>
        /// Scala del modello rispetto alla dimensione base
        /// </summary>
        public float Scale;
    }
}
