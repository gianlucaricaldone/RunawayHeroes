using Unity.Entities;

namespace RunawayHeroes.ECS.Components.Combat
{
    /// <summary>
    /// Componente che rappresenta le proprietà difensive di un'entità, includendo resistenze
    /// a diversi tipi di danno e capacità di protezione.
    /// </summary>
    public struct DefenseComponent : IComponentData
    {
        /// <summary>
        /// Valore base di difesa
        /// </summary>
        public float BaseDefense;
        
        /// <summary>
        /// Valore corrente di difesa (può essere aumentato da powerup)
        /// </summary>
        public float CurrentDefense;
        
        /// <summary>
        /// Resistenza ai danni fisici (colpi, cadute, armi corpo a corpo, proiettili, ecc.)
        /// Valore percentuale (0-100)
        /// </summary>
        public float PhysicalResistance;
        
        /// <summary>
        /// Resistenza ai danni elementali (fuoco, ghiaccio, acido, elettricità, ecc.)
        /// Valore percentuale (0-100)
        /// </summary>
        public float ElementalResistance;
        
        /// <summary>
        /// Resistenza ai danni energetici (radiazioni, soniche, plasma, ecc.)
        /// Valore percentuale (0-100)
        /// </summary>
        public float EnergyResistance;
        
        /// <summary>
        /// Crea un componente di difesa con valori predefiniti (nessuna resistenza)
        /// </summary>
        public static DefenseComponent CreateDefault()
        {
            return new DefenseComponent
            {
                BaseDefense = 10,
                CurrentDefense = 10,
                PhysicalResistance = 0,
                ElementalResistance = 0,
                EnergyResistance = 0
            };
        }
        
        /// <summary>
        /// Crea un componente di difesa con resistenze bilanciate (10% a tutti i tipi di danno)
        /// </summary>
        public static DefenseComponent CreateBalanced()
        {
            return new DefenseComponent
            {
                BaseDefense = 20,
                CurrentDefense = 20,
                PhysicalResistance = 10,
                ElementalResistance = 10,
                EnergyResistance = 10
            };
        }
    }
}