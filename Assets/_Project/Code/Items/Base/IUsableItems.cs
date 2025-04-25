using UnityEngine;

namespace RunawayHeroes.Items
{
    public interface IUsableItem
    {
        string ItemName { get; }
        Sprite ItemIcon { get; }
        
        // Metodo chiamato quando l'oggetto viene usato
        void Use(GameObject target);
        
        // Metodo opzionale per verificare se l'oggetto può essere usato
        bool CanUse(GameObject target);
    }
}