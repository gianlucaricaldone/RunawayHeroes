using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.Unity.Authoring
{
    /// <summary>
    /// Base class per i componenti di authoring degli ostacoli.
    /// Fornisce funzionalità comuni per tutti i tipi di ostacoli.
    /// </summary>
    public abstract class BaseObstacleAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Proprietà Ostacolo Base")]
        [Tooltip("Altezza dell'ostacolo, usata per determinare se può essere superato con scivolata")]
        public float height = 1.0f;
        
        [Tooltip("Larghezza dell'ostacolo")]
        public float width = 1.0f;
        
        [Tooltip("Raggio di collisione dell'ostacolo")]
        public float collisionRadius = 0.5f;
        
        [Tooltip("Resistenza dell'ostacolo, determina se può essere sfondato")]
        public float strength = 100.0f;
        
        [Tooltip("Danno inflitto in caso di collisione (0 per calcolo automatico basato sulla velocità)")]
        public float damageValue = 0.0f;
        
        [Tooltip("Se true, l'ostacolo può essere completamente distrutto")]
        public bool isDestructible = false;
        
        [Tooltip("Preset di configurazione rapida")]
        public ObstaclePreset preset = ObstaclePreset.Custom;
        
        /// <summary>
        /// Conversione base per tutti gli ostacoli
        /// </summary>
        public virtual void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Applica preset se selezionato
            ApplyPreset();
            
            // Aggiunge il componente ObstacleComponent base a tutti gli ostacoli
            dstManager.AddComponentData(entity, new ObstacleComponent
            {
                Height = height,
                Width = width,
                CollisionRadius = collisionRadius,
                Strength = strength,
                DamageValue = damageValue,
                IsDestructible = isDestructible
            });
            
            // Le classi derivate aggiungeranno tag specifici
        }
        
        /// <summary>
        /// Applica i valori predefiniti in base al preset selezionato
        /// </summary>
        protected virtual void ApplyPreset()
        {
            switch (preset)
            {
                case ObstaclePreset.Small:
                    var small = ObstacleComponent.CreateSmall();
                    height = small.Height;
                    width = small.Width;
                    collisionRadius = small.CollisionRadius;
                    strength = small.Strength;
                    damageValue = small.DamageValue;
                    isDestructible = small.IsDestructible;
                    break;
                    
                case ObstaclePreset.Medium:
                    var medium = ObstacleComponent.CreateMedium();
                    height = medium.Height;
                    width = medium.Width;
                    collisionRadius = medium.CollisionRadius;
                    strength = medium.Strength;
                    damageValue = medium.DamageValue;
                    isDestructible = medium.IsDestructible;
                    break;
                    
                case ObstaclePreset.Large:
                    var large = ObstacleComponent.CreateLarge();
                    height = large.Height;
                    width = large.Width;
                    collisionRadius = large.CollisionRadius;
                    strength = large.Strength;
                    damageValue = large.DamageValue;
                    isDestructible = large.IsDestructible;
                    break;
                    
                case ObstaclePreset.Custom:
                default:
                    // Mantiene i valori correnti
                    break;
            }
        }
    }
    
    /// <summary>
    /// Presets predefiniti per la configurazione rapida degli ostacoli
    /// </summary>
    public enum ObstaclePreset
    {
        Custom,
        Small,
        Medium,
        Large
    }
    
    /// <summary>
    /// Componente di authoring per ostacoli standard senza tag speciali
    /// </summary>
    public class StandardObstacleAuthoring : BaseObstacleAuthoring
    {
        // Utilizza solo la configurazione base
    }
    
    /// <summary>
    /// Componente di authoring per ostacoli di lava (Ember può attraversarli con Corpo Ignifugo)
    /// </summary>
    public class LavaObstacleAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Lava")]
        [Tooltip("Danno al secondo quando si sta sulla lava senza protezione")]
        public float damagePerSecond = 20.0f;
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge il tag LavaTag
            dstManager.AddComponent<LavaTag>(entity);
            
            // Aggiunge ToxicGroundTag per il danno continuativo
            dstManager.AddComponentData(entity, new ToxicGroundTag
            {
                ToxicType = 1, // Tipo fuoco/lava
                DamagePerSecond = damagePerSecond
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per ostacoli di ghiaccio (Kai può scioglierli con Aura di Calore)
    /// </summary>
    public class IceObstacleAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Ghiaccio")]
        [Tooltip("Integrità massima del ghiaccio (quanto calore può assorbire prima di sciogliersi)")]
        public float maxIntegrity = 100.0f;
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge il tag IceObstacleTag
            dstManager.AddComponent<IceObstacleTag>(entity);
            
            // Aggiunge il componente per l'integrità del ghiaccio
            dstManager.AddComponentData(entity, new IceIntegrityComponent
            {
                MaxIntegrity = maxIntegrity,
                CurrentIntegrity = maxIntegrity
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per superfici scivolose (Kai con Aura di Calore è immune)
    /// </summary>
    public class SlipperyObstacleAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Superficie Scivolosa")]
        [Tooltip("Fattore di scivolosità (0-1, dove 1 è il massimo)")]
        [Range(0, 1)]
        public float slipFactor = 0.7f;
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge il tag SlipperyTag
            dstManager.AddComponentData(entity, new SlipperyTag
            {
                SlipFactor = slipFactor
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per barriere digitali (Neo può attraversarle con Glitch Controllato)
    /// </summary>
    public class DigitalBarrierAuthoring : BaseObstacleAuthoring
    {
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Le barriere digitali sono impostazioni predefinite resistenti e non distruttibili
            if (preset == ObstaclePreset.Custom)
            {
                // Solo se non è stato scelto un preset specifico
                strength = 500.0f;
                isDestructible = false;
            }
            
            // Aggiunge il tag DigitalBarrierTag
            dstManager.AddComponent<DigitalBarrierTag>(entity);
        }
    }
    
    /// <summary>
    /// Componente di authoring per zone subacquee (Marina è avvantaggiata con Bolla d'Aria)
    /// </summary>
    public class UnderwaterObstacleAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Subacquee")]
        [Tooltip("Se true, questa zona richiede ossigeno per respirare")]
        public bool requiresOxygen = true;
        
        [Tooltip("Forza della corrente (0 per nessuna corrente)")]
        public float currentStrength = 0.0f;
        
        [Tooltip("Direzione della corrente")]
        public Vector3 currentDirection = new Vector3(0, 0, 0);
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge il tag UnderwaterTag
            dstManager.AddComponent<UnderwaterTag>(entity);
            
            // Se ha una corrente, aggiunge anche il componente CurrentTag
            if (currentStrength > 0)
            {
                dstManager.AddComponentData(entity, new CurrentTag
                {
                    Direction = new float3(currentDirection.x, currentDirection.y, currentDirection.z),
                    Strength = currentStrength,
                    CurrentType = 2 // Tipo corrente acquatica
                });
            }
        }
    }
    
    /// <summary>
    /// Componente di authoring per correnti d'aria (utilizzate in vari mondi)
    /// </summary>
    public class AirCurrentAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Corrente d'Aria")]
        [Tooltip("Forza della corrente")]
        public float currentStrength = 5.0f;
        
        [Tooltip("Direzione della corrente")]
        public Vector3 currentDirection = new Vector3(0, 1, 0);
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge il componente CurrentTag
            dstManager.AddComponentData(entity, new CurrentTag
            {
                Direction = new float3(currentDirection.x, currentDirection.y, currentDirection.z),
                Strength = currentStrength,
                CurrentType = 1 // Tipo corrente aerea
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per zone di gas tossico
    /// </summary>
    public class ToxicGasAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Gas Tossico")]
        [Tooltip("Danno al secondo nell'area tossica")]
        public float damagePerSecond = 10.0f;
        
        public override void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Prima chiama la conversione base per aggiungere ObstacleComponent
            base.Convert(entity, dstManager, conversionSystem);
            
            // Aggiunge ToxicGroundTag per il danno continuativo
            dstManager.AddComponentData(entity, new ToxicGroundTag
            {
                ToxicType = 2, // Tipo gas tossico
                DamagePerSecond = damagePerSecond
            });
        }
    }
}