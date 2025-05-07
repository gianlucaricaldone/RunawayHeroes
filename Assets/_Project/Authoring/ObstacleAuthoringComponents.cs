// Path: Assets/_Project/Authoring/ObstacleAuthoringComponents.cs
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.World.Obstacles;

namespace RunawayHeroes.Authoring
{
    /// <summary>
    /// Base class per i componenti di authoring degli ostacoli.
    /// Fornisce funzionalità comuni per tutti i tipi di ostacoli.
    /// </summary>
    public abstract class BaseObstacleAuthoring : MonoBehaviour
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
        /// Applica i valori predefiniti in base al preset selezionato
        /// </summary>
        // Cambiato da protected a public in modo che i Baker possano accedervi
        public virtual void ApplyPreset()
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
    /// Baker per ostacoli standard
    /// </summary>
    public class StandardObstacleBaker : Baker<StandardObstacleAuthoring>
    {
        public override void Bake(StandardObstacleAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent (converte da Unity Transform a ECS TransformComponent)
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity, // O Quaternion.ToECSQuaternion(transform.rotation)
                Scale = transform.lossyScale.x
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per ostacoli di lava (Ember può attraversarli con Corpo Ignifugo)
    /// </summary>
    public class LavaObstacleAuthoring : BaseObstacleAuthoring
    {
        [Header("Proprietà Lava")]
        [Tooltip("Danno al secondo quando si sta sulla lava senza protezione")]
        public float damagePerSecond = 20.0f;
    }
    
    /// <summary>
    /// Baker per ostacoli di lava
    /// </summary>
    public class LavaObstacleBaker : Baker<LavaObstacleAuthoring>
    {
        public override void Bake(LavaObstacleAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il tag LavaTag
            AddComponent<LavaTag>(entity);
            
            // Aggiunge ToxicGroundTag per il danno continuativo
            AddComponent(entity, new ToxicGroundTag
            {
                ToxicType = 1, // Tipo fuoco/lava
                DamagePerSecond = authoring.damagePerSecond
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
    }
    
    /// <summary>
    /// Baker per ostacoli di ghiaccio
    /// </summary>
    public class IceObstacleBaker : Baker<IceObstacleAuthoring>
    {
        public override void Bake(IceObstacleAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il tag IceObstacleTag
            AddComponent<IceObstacleTag>(entity);
            
            // Aggiunge il componente per l'integrità del ghiaccio
            AddComponent(entity, new IceIntegrityComponent
            {
                MaxIntegrity = authoring.maxIntegrity,
                CurrentIntegrity = authoring.maxIntegrity
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
    }
    
    /// <summary>
    /// Baker per superfici scivolose
    /// </summary>
    public class SlipperyObstacleBaker : Baker<SlipperyObstacleAuthoring>
    {
        public override void Bake(SlipperyObstacleAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il tag SlipperyTag
            AddComponent(entity, new SlipperyTag
            {
                SlipFactor = authoring.slipFactor
            });
        }
    }
    
    /// <summary>
    /// Componente di authoring per barriere digitali (Neo può attraversarle con Glitch Controllato)
    /// </summary>
    public class DigitalBarrierAuthoring : BaseObstacleAuthoring
    {
        // Override del metodo ApplyPreset per impostare valori predefiniti diversi
        public override void ApplyPreset()
        {
            base.ApplyPreset();
            
            // Le barriere digitali sono impostazioni predefinite resistenti e non distruttibili
            if (preset == ObstaclePreset.Custom)
            {
                // Solo se non è stato scelto un preset specifico
                strength = 500.0f;
                isDestructible = false;
            }
        }
    }
    
    /// <summary>
    /// Baker per barriere digitali
    /// </summary>
    public class DigitalBarrierBaker : Baker<DigitalBarrierAuthoring>
    {
        public override void Bake(DigitalBarrierAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il tag DigitalBarrierTag
            AddComponent<DigitalBarrierTag>(entity);
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
    }
    
    /// <summary>
    /// Baker per zone subacquee
    /// </summary>
    public class UnderwaterObstacleBaker : Baker<UnderwaterObstacleAuthoring>
    {
        public override void Bake(UnderwaterObstacleAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il tag UnderwaterTag
            AddComponent<UnderwaterTag>(entity);
            
            // Se ha una corrente, aggiunge anche il componente CurrentTag
            if (authoring.currentStrength > 0)
            {
                AddComponent(entity, new CurrentTag
                {
                    Direction = new float3(authoring.currentDirection.x, authoring.currentDirection.y, authoring.currentDirection.z),
                    Strength = authoring.currentStrength,
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
    }
    
    /// <summary>
    /// Baker per correnti d'aria
    /// </summary>
    public class AirCurrentBaker : Baker<AirCurrentAuthoring>
    {
        public override void Bake(AirCurrentAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge il componente CurrentTag
            AddComponent(entity, new CurrentTag
            {
                Direction = new float3(authoring.currentDirection.x, authoring.currentDirection.y, authoring.currentDirection.z),
                Strength = authoring.currentStrength,
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
    }
    
    /// <summary>
    /// Baker per zone di gas tossico
    /// </summary>
    public class ToxicGasBaker : Baker<ToxicGasAuthoring>
    {
        public override void Bake(ToxicGasAuthoring authoring)
        {
            authoring.ApplyPreset();
            
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            
            // Aggiunge componenti base
            AddComponent(entity, new ObstacleComponent
            {
                Height = authoring.height,
                Width = authoring.width,
                CollisionRadius = authoring.collisionRadius,
                Strength = authoring.strength,
                DamageValue = authoring.damageValue,
                IsDestructible = authoring.isDestructible
            });
            
            // Aggiunge TransformComponent
            var transform = GetComponent<Transform>();
            AddComponent(entity, new TransformComponent
            {
                Position = transform.position,
                Rotation = quaternion.identity,
                Scale = transform.lossyScale.x
            });
            
            // Aggiunge ToxicGroundTag per il danno continuativo
            AddComponent(entity, new ToxicGroundTag
            {
                ToxicType = 2, // Tipo gas tossico
                DamagePerSecond = authoring.damagePerSecond
            });
        }
    }
}