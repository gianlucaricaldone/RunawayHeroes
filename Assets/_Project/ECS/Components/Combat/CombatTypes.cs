namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Motivi per cui un danno può essere bloccato
    /// </summary>
    public enum BlockReason : byte
    {
        Invulnerability = 0,       // Invulnerabilità temporanea
        Immunity = 1,              // Immunità al tipo di danno
        Shield = 2,                // Assorbito da uno scudo
        Dodge = 3,                 // Schivato
        Parry = 4,                 // Parato/contrattaccato
        AbilityBlock = 5           // Bloccato da un'abilità speciale
    }
}