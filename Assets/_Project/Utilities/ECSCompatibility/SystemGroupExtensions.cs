// Path: Assets/_Project/Utilities/ECSCompatibility/SystemGroupExtensions.cs
using Unity.Entities;

namespace RunawayHeroes.Utilities.ECSCompatibility
{
    /// <summary>
    /// Classe di utilità che fornisce metodi di estensione per ComponentSystemGroup.
    /// </summary>
    public static class SystemGroupExtensions
    {
        /// <summary>
        /// Metodo di estensione per ottenere lo stato del sistema (SystemState)
        /// da un ComponentSystemGroup.
        /// </summary>
        /// <param name="systemGroup">Il gruppo di sistemi</param>
        /// <returns>Lo stato del sistema</returns>
        public static SystemState GetCheckedState(this ComponentSystemGroup systemGroup)
        {
            // Ottiene l'accesso a SystemState attraverso un World esistente
            var world = systemGroup.World;
            if (world != null && world.IsCreated)
            {
                // Ottiene SystemState usando la modalità più recente supportata
                return world.Unmanaged.ResolveSystemStateRef(systemGroup.SystemHandle);
            }
            
            throw new System.InvalidOperationException("Impossibile accedere a SystemState: World non valido");
        }
    }
}