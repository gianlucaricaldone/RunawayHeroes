// Path: Assets/_Project/ECS/Components/Core/TagComponent.cs
using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Core
{
    /// <summary>
    /// Componente per etichettare le entità con tag personalizzati.
    /// Permette di identificare facilmente il tipo o ruolo di un'entità.
    /// </summary>
    public struct TagComponent : IComponentData
    {
        /// <summary>
        /// Il tag associato all'entità. Può essere utilizzato per identificare categorie, 
        /// tipi o altri raggruppamenti di entità nel sistema ECS.
        /// </summary>
        public FixedString64Bytes Tag;

        /// <summary>
        /// Crea un nuovo TagComponent con il valore di tag specificato
        /// </summary>
        /// <param name="tag">Il valore del tag</param>
        /// <returns>Un nuovo TagComponent con il tag specificato</returns>
        public static TagComponent Create(string tag)
        {
            return new TagComponent { Tag = new FixedString64Bytes(tag) };
        }

        /// <summary>
        /// Verifica se il tag corrisponde a quello specificato
        /// </summary>
        /// <param name="tagToCheck">Il tag da confrontare</param>
        /// <returns>True se i tag corrispondono, false altrimenti</returns>
        public bool HasTag(string tagToCheck)
        {
            return Tag.Equals(new FixedString64Bytes(tagToCheck));
        }
    }
}