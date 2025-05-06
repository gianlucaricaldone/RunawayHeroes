using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Combat
{
    /// <summary>
    /// Componente che rappresenta le proprietà offensive di un'entità, includendo il danno
    /// di base, bonus e modificatori per diversi tipi di attacco.
    /// </summary>
    public struct CombatComponent : IComponentData
    {
        /// <summary>
        /// Danno di base per questo combattente
        /// </summary>
        public float BaseDamage;
        
        /// <summary>
        /// Danno attuale (modificato da potenziamenti o penalità)
        /// </summary>
        public float CurrentDamage;
        
        /// <summary>
        /// Percentuale di probabilità di colpo critico (0-100)
        /// </summary>
        public float CriticalHitChance;
        
        /// <summary>
        /// Moltiplicatore di danno per colpi critici
        /// </summary>
        public float CriticalHitMultiplier;
        
        /// <summary>
        /// Raggio di attacco, se applicabile
        /// </summary>
        public float AttackRange;
        
        /// <summary>
        /// Crea un componente di combattimento con valori predefiniti
        /// </summary>
        public static CombatComponent CreateDefault()
        {
            return new CombatComponent
            {
                BaseDamage = 10.0f,
                CurrentDamage = 10.0f,
                CriticalHitChance = 5.0f,
                CriticalHitMultiplier = 1.5f,
                AttackRange = 1.0f
            };
        }
        
        /// <summary>
        /// Crea un componente di combattimento per un'entità da mischia
        /// </summary>
        public static CombatComponent CreateMelee(float baseDamage)
        {
            return new CombatComponent
            {
                BaseDamage = baseDamage,
                CurrentDamage = baseDamage,
                CriticalHitChance = 10.0f,
                CriticalHitMultiplier = 2.0f,
                AttackRange = 1.5f
            };
        }
        
        /// <summary>
        /// Crea un componente di combattimento per un'entità a distanza
        /// </summary>
        public static CombatComponent CreateRanged(float baseDamage, float range)
        {
            return new CombatComponent
            {
                BaseDamage = baseDamage,
                CurrentDamage = baseDamage,
                CriticalHitChance = 5.0f,
                CriticalHitMultiplier = 1.5f,
                AttackRange = range
            };
        }
    }
}