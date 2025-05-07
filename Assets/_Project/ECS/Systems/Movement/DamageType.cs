namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Tipi di danno che possono essere applicati
    /// </summary>
    public enum DamageType : byte
    {
        /// <summary>
        /// Danno da collisione con ostacoli
        /// </summary>
        Obstacle = 0,
        
        /// <summary>
        /// Danno da caduta
        /// </summary>
        Fall = 1,
        
        /// <summary>
        /// Danno da nemici
        /// </summary>
        Enemy = 2,
        
        /// <summary>
        /// Danno da trappole ambientali
        /// </summary>
        Hazard = 3,
        
        /// <summary>
        /// Danno da effetti di stato (es. veleno, fuoco)
        /// </summary>
        StatusEffect = 4
    }
}