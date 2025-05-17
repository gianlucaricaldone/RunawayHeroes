using UnityEngine;
using RunawayHeroes.Authoring;

/// <summary>
/// Componente di authoring per ostacoli bassi da saltare
/// </summary>
public class LowObstacleAuthoring : StandardObstacleAuthoring
{
    private void Reset()
    {
        // Configura per un ostacolo basso
        preset = ObstaclePreset.Small;
        height = 0.5f;
        width = 2.0f;
        collisionRadius = 1.0f;
        isDestructible = false;
        
        // Applica il preset per inizializzare le proprietà rimanenti
        ApplyPreset();
    }
}