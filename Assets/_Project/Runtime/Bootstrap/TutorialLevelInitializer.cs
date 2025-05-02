// Path: Assets/_Project/Runtime/Bootstrap/TutorialLevelInitializer.cs
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.Authoring;

namespace RunawayHeroes.Runtime.Bootstrap
{
    /// <summary>
    /// Inizializzatore del livello tutorial che gestisce la creazione di scenari con
    /// ostacoli multipli per ogni scenario.
    /// </summary>
    public class TutorialLevelInitializer : MonoBehaviour
    {
        [System.Serializable]
        public class ScenarioSetup
        {
            public string scenarioName;
            public ObstacleSetup[] obstacles;
            public float startPositionZ;
            public float endPositionZ;
        }

        [System.Serializable]
        public class ObstacleSetup
        {
            public enum ObstacleType
            {
                Standard,
                Lava,
                Ice,
                Slippery,
                DigitalBarrier,
                Underwater,
                AirCurrent,
                ToxicGas
            }

            public ObstacleType type;
            public ObstaclePreset preset = ObstaclePreset.Medium;
            public Vector3 position;
            public float rotation;
            
            // Proprietà specifiche per tipi di ostacoli
            [Header("Proprietà Specifiche")]
            // Lava
            public float damagePerSecond = 20.0f;
            
            // Ice
            public float maxIntegrity = 100.0f;
            
            // Slippery
            [Range(0, 1)]
            public float slipFactor = 0.7f;
            
            // Current (Air/Water)
            public float currentStrength = 5.0f;
            public Vector3 currentDirection = new Vector3(0, 1, 0);
            
            // Underwater
            public bool requiresOxygen = true;
            
            // Custom properties
            public float height = 1.0f;
            public float width = 1.0f;
            public float collisionRadius = 0.5f;
            public float strength = 100.0f;
            public float damageValue = 0.0f;
            public bool isDestructible = false;
        }

        [Header("Scenari di Tutorial")]
        [SerializeField] private ScenarioSetup[] scenarios;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private Color gizmoColor = Color.cyan;

        private EntityManager _entityManager;
        private EntityArchetype _standardObstacleArchetype;
        private EntityArchetype _lavaObstacleArchetype;
        private EntityArchetype _iceObstacleArchetype;
        private EntityArchetype _slipperyObstacleArchetype;
        private EntityArchetype _digitalBarrierArchetype;
        private EntityArchetype _underwaterObstacleArchetype;
        private EntityArchetype _airCurrentArchetype;
        private EntityArchetype _toxicGasArchetype;

        private void Awake()
        {
            // Ottiene il World di default e l'EntityManager
            var world = World.DefaultGameObjectInjectionWorld;
            _entityManager = world.EntityManager;

            // Inizializza gli archetipi
            InitializeArchetypes();
        }

        private void Start()
        {
            // Inizializza tutti gli scenari del tutorial
            InitializeScenarios();
        }

        private void InitializeArchetypes()
        {
            // Crea archetipi per ogni tipo di ostacolo
            _standardObstacleArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(LocalTransform)  // Unity Transforms
            );

            _lavaObstacleArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(LavaTag),
                typeof(ToxicGroundTag),
                typeof(LocalTransform)
            );

            _iceObstacleArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(IceObstacleTag),
                typeof(IceIntegrityComponent),
                typeof(LocalTransform)
            );

            _slipperyObstacleArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(SlipperyTag),
                typeof(LocalTransform)
            );

            _digitalBarrierArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(DigitalBarrierTag),
                typeof(LocalTransform)
            );

            _underwaterObstacleArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(UnderwaterTag),
                typeof(LocalTransform)
            );

            _airCurrentArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(CurrentTag),
                typeof(LocalTransform)
            );

            _toxicGasArchetype = _entityManager.CreateArchetype(
                typeof(TransformComponent),
                typeof(ObstacleComponent),
                typeof(ToxicGroundTag),
                typeof(LocalTransform)
            );
        }

        private void InitializeScenarios()
        {
            if (scenarios == null || scenarios.Length == 0)
            {
                Debug.LogWarning("Nessuno scenario configurato per il livello tutorial.");
                return;
            }

            foreach (var scenario in scenarios)
            {
                InitializeScenario(scenario);
            }
        }

        private void InitializeScenario(ScenarioSetup scenario)
        {
            Debug.Log($"Inizializzazione scenario: {scenario.scenarioName}");

            if (scenario.obstacles == null || scenario.obstacles.Length == 0)
            {
                Debug.LogWarning($"Nessun ostacolo configurato per lo scenario: {scenario.scenarioName}");
                return;
            }

            // Crea ogni ostacolo configurato per questo scenario
            foreach (var obstacleSetup in scenario.obstacles)
            {
                CreateObstacle(obstacleSetup);
            }
        }

        private void CreateObstacle(ObstacleSetup setup)
        {
            Entity entity;

            // Seleziona l'archetipo corretto in base al tipo di ostacolo
            switch (setup.type)
            {
                case ObstacleSetup.ObstacleType.Lava:
                    entity = _entityManager.CreateEntity(_lavaObstacleArchetype);
                    // Imposta i dati specifici per il tipo Lava
                    _entityManager.SetComponentData(entity, new ToxicGroundTag
                    {
                        ToxicType = 1, // Tipo fuoco/lava
                        DamagePerSecond = setup.damagePerSecond
                    });
                    break;

                case ObstacleSetup.ObstacleType.Ice:
                    entity = _entityManager.CreateEntity(_iceObstacleArchetype);
                    // Imposta i dati specifici per il tipo Ice
                    _entityManager.SetComponentData(entity, new IceIntegrityComponent
                    {
                        MaxIntegrity = setup.maxIntegrity,
                        CurrentIntegrity = setup.maxIntegrity
                    });
                    break;

                case ObstacleSetup.ObstacleType.Slippery:
                    entity = _entityManager.CreateEntity(_slipperyObstacleArchetype);
                    // Imposta i dati specifici per il tipo Slippery
                    _entityManager.SetComponentData(entity, new SlipperyTag
                    {
                        SlipFactor = setup.slipFactor
                    });
                    break;

                case ObstacleSetup.ObstacleType.DigitalBarrier:
                    entity = _entityManager.CreateEntity(_digitalBarrierArchetype);
                    break;

                case ObstacleSetup.ObstacleType.Underwater:
                    entity = _entityManager.CreateEntity(_underwaterObstacleArchetype);
                    // Se ha una corrente, aggiunge anche il componente CurrentTag
                    if (setup.currentStrength > 0)
                    {
                        _entityManager.AddComponentData(entity, new CurrentTag
                        {
                            Direction = new float3(setup.currentDirection.x, setup.currentDirection.y, setup.currentDirection.z),
                            Strength = setup.currentStrength,
                            CurrentType = 2 // Tipo corrente acquatica
                        });
                    }
                    break;

                case ObstacleSetup.ObstacleType.AirCurrent:
                    entity = _entityManager.CreateEntity(_airCurrentArchetype);
                    // Imposta i dati specifici per il tipo AirCurrent
                    _entityManager.SetComponentData(entity, new CurrentTag
                    {
                        Direction = new float3(setup.currentDirection.x, setup.currentDirection.y, setup.currentDirection.z),
                        Strength = setup.currentStrength,
                        CurrentType = 1 // Tipo corrente aerea
                    });
                    break;

                case ObstacleSetup.ObstacleType.ToxicGas:
                    entity = _entityManager.CreateEntity(_toxicGasArchetype);
                    // Imposta i dati specifici per il tipo ToxicGas
                    _entityManager.SetComponentData(entity, new ToxicGroundTag
                    {
                        ToxicType = 2, // Tipo gas tossico
                        DamagePerSecond = setup.damagePerSecond
                    });
                    break;

                case ObstacleSetup.ObstacleType.Standard:
                default:
                    entity = _entityManager.CreateEntity(_standardObstacleArchetype);
                    break;
            }

            // Applica le proprietà di base dell'ostacolo in base al preset o ai valori personalizzati
            ObstacleComponent obstacleComponent;
            
            switch (setup.preset)
            {
                case ObstaclePreset.Small:
                    obstacleComponent = ObstacleComponent.CreateSmall();
                    break;
                case ObstaclePreset.Medium:
                    obstacleComponent = ObstacleComponent.CreateMedium();
                    break;
                case ObstaclePreset.Large:
                    obstacleComponent = ObstacleComponent.CreateLarge();
                    break;
                case ObstaclePreset.Custom:
                default:
                    obstacleComponent = new ObstacleComponent
                    {
                        Height = setup.height,
                        Width = setup.width,
                        CollisionRadius = setup.collisionRadius,
                        Strength = setup.strength,
                        DamageValue = setup.damageValue,
                        IsDestructible = setup.isDestructible
                    };
                    break;
            }
            
            _entityManager.SetComponentData(entity, obstacleComponent);

            // Imposta la trasformazione dell'ostacolo
            _entityManager.SetComponentData(entity, new TransformComponent
            {
                Position = setup.position,
                Rotation = quaternion.Euler(0, setup.rotation, 0),
                Scale = 1.0f // Scale di default o parametrizzata se necessario
            });
            
            // Imposta anche la LocalTransform di Unity
            _entityManager.SetComponentData(entity, new LocalTransform
            {
                Position = setup.position,
                Rotation = quaternion.Euler(0, setup.rotation, 0),
                Scale = 1.0f
            });

            Debug.Log($"Creato ostacolo di tipo {setup.type} a posizione {setup.position}");
        }

        #region Debug Visualization
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || scenarios == null) return;

            Gizmos.color = gizmoColor;

            foreach (var scenario in scenarios)
            {
                // Disegna i limiti dello scenario
                Gizmos.DrawLine(
                    new Vector3(-10, 0, scenario.startPositionZ),
                    new Vector3(10, 0, scenario.startPositionZ)
                );
                Gizmos.DrawLine(
                    new Vector3(-10, 0, scenario.endPositionZ),
                    new Vector3(10, 0, scenario.endPositionZ)
                );

                if (scenario.obstacles == null) continue;

                foreach (var obstacle in scenario.obstacles)
                {
                    // Disegna ogni ostacolo con colore diverso in base al tipo
                    switch (obstacle.type)
                    {
                        case ObstacleSetup.ObstacleType.Lava:
                            Gizmos.color = Color.red;
                            break;
                        case ObstacleSetup.ObstacleType.Ice:
                            Gizmos.color = Color.cyan;
                            break;
                        case ObstacleSetup.ObstacleType.Slippery:
                            Gizmos.color = Color.blue;
                            break;
                        case ObstacleSetup.ObstacleType.DigitalBarrier:
                            Gizmos.color = Color.green;
                            break;
                        case ObstacleSetup.ObstacleType.Underwater:
                            Gizmos.color = new Color(0, 0, 0.8f);
                            break;
                        case ObstacleSetup.ObstacleType.AirCurrent:
                            Gizmos.color = new Color(0.8f, 0.8f, 1.0f);
                            break;
                        case ObstacleSetup.ObstacleType.ToxicGas:
                            Gizmos.color = new Color(0.5f, 0.5f, 0);
                            break;
                        default:
                            Gizmos.color = Color.white;
                            break;
                    }

                    // Disegna un cubo per rappresentare l'ostacolo
                    Gizmos.DrawWireCube(obstacle.position, new Vector3(obstacle.width, obstacle.height, obstacle.width));
                    
                    // Per le correnti, disegna anche una freccia che indica la direzione
                    if (obstacle.type == ObstacleSetup.ObstacleType.AirCurrent || 
                        (obstacle.type == ObstacleSetup.ObstacleType.Underwater && obstacle.currentStrength > 0))
                    {
                        Vector3 dirEnd = obstacle.position + obstacle.currentDirection.normalized * 2.0f;
                        Gizmos.DrawLine(obstacle.position, dirEnd);
                        
                        // Disegna una piccola punta di freccia
                        Vector3 right = Vector3.Cross(obstacle.currentDirection.normalized, Vector3.up) * 0.5f;
                        Vector3 up = Vector3.Cross(right, obstacle.currentDirection.normalized) * 0.5f;
                        Gizmos.DrawLine(dirEnd, dirEnd - obstacle.currentDirection.normalized * 0.5f + right);
                        Gizmos.DrawLine(dirEnd, dirEnd - obstacle.currentDirection.normalized * 0.5f - right);
                        Gizmos.DrawLine(dirEnd, dirEnd - obstacle.currentDirection.normalized * 0.5f + up);
                        Gizmos.DrawLine(dirEnd, dirEnd - obstacle.currentDirection.normalized * 0.5f - up);
                    }
                }

                // Resetta il colore
                Gizmos.color = gizmoColor;
            }
        }
        #endregion
    }
}