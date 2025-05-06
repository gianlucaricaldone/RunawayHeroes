using System;
using Unity.Entities;
using Unity.Collections;

namespace RunawayHeroes.ECS.Components.Gameplay
{
    /// <summary>
    /// Componente che rappresenta una missione o quest all'interno del gioco.
    /// Una missione è composta da uno o più obiettivi che devono essere completati.
    /// </summary>
    [Serializable]
    public struct MissionComponent : IComponentData
    {
        /// <summary>
        /// ID univoco della missione
        /// </summary>
        public int MissionID;
        
        /// <summary>
        /// Nome localizzato della missione
        /// </summary>
        public FixedString64Bytes MissionName;
        
        /// <summary>
        /// Descrizione localizzata della missione
        /// </summary>
        public FixedString128Bytes Description;
        
        /// <summary>
        /// Tipo di missione
        /// 0 = Principale (story)
        /// 1 = Secondaria
        /// 2 = Tutorial
        /// 3 = Sfida
        /// </summary>
        public byte MissionType;
        
        /// <summary>
        /// Obiettivi completati
        /// </summary>
        public int CompletedObjectives;
        
        /// <summary>
        /// Obiettivi totali richiesti
        /// </summary>
        public int TotalObjectives;
        
        /// <summary>
        /// Se la missione è stata completata
        /// </summary>
        public bool IsCompleted;
        
        /// <summary>
        /// Se la missione è fallita
        /// </summary>
        public bool IsFailed;
        
        /// <summary>
        /// Se la missione è disponibile
        /// </summary>
        public bool IsActive;
        
        /// <summary>
        /// Livello associato alla missione (se applicabile)
        /// </summary>
        public int AssociatedLevelID;
        
        /// <summary>
        /// Ricompensa alla missione (punti, collezionabili, ecc.)
        /// </summary>
        public int RewardValue;
        
        /// <summary>
        /// Tipo di ricompensa
        /// 0 = Punti
        /// 1 = Frammento
        /// 2 = Chiave
        /// 3 = Abilità
        /// </summary>
        public byte RewardType;
    }
}