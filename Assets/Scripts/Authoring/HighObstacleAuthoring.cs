using UnityEngine;
using RunawayHeroes.Authoring;

/// <summary>
/// Componente di authoring per ostacoli alti per la scivolata
/// </summary>
public class HighObstacleAuthoring : StandardObstacleAuthoring
{
    private void Reset()
    {
        // Configura per un ostacolo alto
        preset = ObstaclePreset.Medium;
        height = 2.0f;
        width = 1.5f;
        collisionRadius = 0.75f;
        isDestructible = false;
        
        // Applica il preset per inizializzare le proprietà rimanenti
        ApplyPreset();
    }
}