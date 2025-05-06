using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Enemies
{
    /// <summary>
    /// Tag che indica che un'entità è attualmente stordita.
    /// Le entità con questo tag sono escluse da sistemi di movimento e attacco.
    /// </summary>
    public struct StunnedTag : IComponentData { }
    
    /// <summary>
    /// Tag che indica che un'entità è attualmente paralizzata.
    /// Simile allo stordimento ma con effetti visivi diversi.
    /// </summary>
    public struct ParalyzedTag : IComponentData { }
    
    /// <summary>
    /// Tag che indica che un'entità è vulnerabile a danni aggiuntivi.
    /// </summary>
    public struct VulnerableTag : IComponentData { }
}