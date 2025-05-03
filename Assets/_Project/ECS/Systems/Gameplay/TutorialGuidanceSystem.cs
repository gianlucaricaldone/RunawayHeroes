// Path: TutorialGuidanceSystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using System;
using UnityEngine;
using Random = Unity.Mathematics.Random;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.UI;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce l'avanzamento del tutorial e la generazione degli scenari didattici
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial class TutorialGuidanceSystem : SystemBase
    {
        private EntityQuery _activePlayerQuery;
        private EntityQuery _scenarioQuery;
        private EntityCommandBufferSystem _endSimulationECBSystem;
        private Random _random;
        private uint _seed;
        
        // Costanti per il posizionamento degli ostacoli
        private const float LANE_WIDTH = 9f; // Larghezza totale della corsia
        private const float LEFT_POSITION = -3f; // Posizione laterale sinistra
        private const float CENTER_POSITION = 0f; // Posizione centrale
        private const float RIGHT_POSITION = 3f; // Posizione laterale destra
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Inizializza il generatore di numeri casuali
            _seed = (uint)UnityEngine.Random.Range(1, 1000000);
            _random = new Random(_seed);
            
            // Configura query per il giocatore attivo
            _activePlayerQuery = GetEntityQuery(
                ComponentType.ReadOnly<PlayerTag>(),
                ComponentType.ReadOnly<ActiveTag>(),
                ComponentType.ReadOnly<TransformComponent>()
            );
            
            // Configura query per gli scenari di tutorial
            _scenarioQuery = GetEntityQuery(
                ComponentType.ReadOnly<TutorialScenarioComponent>(),
                ComponentType.ReadOnly<TutorialObstacleBuffer>()
            );
            
            // Buffer per le operazioni di entità
            _endSimulationECBSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            
            // Richiedi aggiornamento solo quando esiste un giocatore attivo
            RequireForUpdate(_activePlayerQuery);
        }
        
        protected override void OnUpdate()
        {
            if (!SystemAPI.HasSingleton<TutorialLevelTag>())
                return;
                
            var commandBuffer = _endSimulationECBSystem.CreateCommandBuffer().AsParallelWriter();
            var playerEntities = _activePlayerQuery.ToEntityArray(Allocator.TempJob);
            
            if (playerEntities.Length == 0)
            {
                playerEntities.Dispose();
                return;
            }
            
            var playerEntity = playerEntities[0];
            var playerPosition = SystemAPI.GetComponent<TransformComponent>(playerEntity).Position;
            playerEntities.Dispose();
            
            float3 playerPos = playerPosition;
            
            // Controlla se il giocatore ha raggiunto nuovi scenari
            Entities
                .WithAll<TutorialScenarioComponent>()
                .WithNone<TriggeredTag>()
                .ForEach((Entity entity, int entityInQueryIndex, 
                          ref TutorialScenarioComponent scenario,
                          in DynamicBuffer<TutorialObstacleBuffer> obstacleBuffer) => 
                {
                    // Se il giocatore ha raggiunto la distanza di inizio scenario
                    if (playerPos.z >= scenario.DistanceFromStart)
                    {
                        // Marca questo scenario come attivato
                        commandBuffer.AddComponent<TriggeredTag>(entityInQueryIndex, entity);
                        
                        // Mostra messaggio di istruzione
                        if (!string.IsNullOrEmpty(scenario.InstructionMessage))
                        {
                            // Crea un'entità per il messaggio UI
                            Entity messageEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                            
                            // Aggiungi il componente di messaggio UI
                            commandBuffer.AddComponent(entityInQueryIndex, messageEntity, 
                                new RunawayHeroes.ECS.Components.UI.UIMessageComponent 
                                { 
                                    Message = scenario.InstructionMessage,
                                    Duration = scenario.MessageDuration,
                                    RemainingTime = scenario.MessageDuration,
                                    MessageType = 0, // Tipo tutorial
                                    IsPersistent = false,
                                    MessageId = entity.Index // Usa l'indice dell'entità scenario come ID
                                });
                                
                            // Aggiungi il tag per accodare il messaggio
                            commandBuffer.AddComponent(entityInQueryIndex, messageEntity,
                                new RunawayHeroes.ECS.Components.UI.QueuedMessageTag
                                {
                                    QueuePosition = 0
                                });
                            
                            UnityEngine.Debug.Log($"[Tutorial] Messaggio creato: {scenario.InstructionMessage}");
                        }
                        
                        // Genera gli ostacoli per questo scenario
                        SpawnObstaclesForScenario(commandBuffer, entityInQueryIndex, scenario, obstacleBuffer);
                    }
                })
                .ScheduleParallel();
            
            _endSimulationECBSystem.AddJobHandleForProducer(Dependency);
        }
        
        /// <summary>
        /// Genera gli ostacoli per uno scenario di tutorial
        /// </summary>
        private void SpawnObstaclesForScenario(
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int entityInQueryIndex,
            TutorialScenarioComponent scenario,
            DynamicBuffer<TutorialObstacleBuffer> obstacleBuffer)
        {
            // Genera ostacoli per ogni configurazione nel buffer
            for (int i = 0; i < obstacleBuffer.Length; i++)
            {
                var obstacleSetup = obstacleBuffer[i];
                SpawnTutorialObstaclesAdvanced(
                    commandBuffer,
                    entityInQueryIndex,
                    scenario.DistanceFromStart + obstacleSetup.StartOffset,
                    scenario.ObstacleSpacing,
                    scenario.RandomPlacement,
                    obstacleSetup
                );
            }
        }

        /// <summary>
        /// Genera ostacoli per un tutorial con impostazioni avanzate
        /// </summary>
        private void SpawnTutorialObstaclesAdvanced(
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int entityInQueryIndex,
            float startZ,
            float spacing,
            bool randomPlacement,
            TutorialObstacleBuffer obstacleSetup)
        {
            // Crea un nuovo seed per ogni chiamata per evitare pattern ripetuti
            uint localSeed = _seed + (uint)(obstacleSetup.ObstacleCode.GetHashCode() + entityInQueryIndex);
            var random = new Random(localSeed);
            
            for (int i = 0; i < obstacleSetup.Count; i++)
            {
                // Calcola la posizione Z (avanzamento)
                float zPos;
                if (randomPlacement)
                {
                    // Posizionamento casuale lungo un segmento di percorso
                    float segmentLength = spacing * obstacleSetup.Count;
                    zPos = startZ + random.NextFloat(0, segmentLength);
                }
                else
                {
                    // Posizionamento a intervalli regolari
                    zPos = startZ + (i * spacing);
                }
                
                // Calcola la posizione X (laterale) in base al tipo di posizionamento
                float xPos = 0;
                switch (obstacleSetup.Placement)
                {
                    case 0: // Center
                        xPos = CENTER_POSITION;
                        break;
                    case 1: // Left
                        xPos = LEFT_POSITION;
                        break;
                    case 2: // Right
                        xPos = RIGHT_POSITION;
                        break;
                    case 3: // Random
                        xPos = random.NextFloat(-LANE_WIDTH/2f, LANE_WIDTH/2f);
                        break;
                    case 4: // Pattern
                        // Distribuzione uniforme degli ostacoli in un pattern attraverso la corsia
                        float pattern = (float)i / (float)(obstacleSetup.Count - 1);
                        xPos = math.lerp(-LANE_WIDTH/2f, LANE_WIDTH/2f, pattern);
                        break;
                }
                
                // Calcola altezza e scala in base alle impostazioni
                float height = 1.0f; // Altezza predefinita
                if (obstacleSetup.RandomizeHeight)
                {
                    // Genera un'altezza casuale nel range definito
                    float minHeight = math.max(0.3f, obstacleSetup.HeightRange.x);
                    float maxHeight = math.max(minHeight, obstacleSetup.HeightRange.y);
                    height = random.NextFloat(minHeight, maxHeight);
                }
                
                float scale = 1.0f; // Scala predefinita
                if (obstacleSetup.RandomizeScale)
                {
                    // Genera una scala casuale nel range definito
                    float minScale = math.max(0.5f, obstacleSetup.ScaleRange.x);
                    float maxScale = math.max(minScale, obstacleSetup.ScaleRange.y);
                    scale = random.NextFloat(minScale, maxScale);
                }
                
                // Crea l'entità ostacolo tramite il factory
                Entity obstacleEntity = SpawnObstacle(
                    commandBuffer,
                    entityInQueryIndex,
                    obstacleSetup.ObstacleCode.ToString(),
                    new float3(xPos, 0, zPos),
                    height,
                    scale
                );
            }
        }
        
        /// <summary>
        /// Crea un'entità ostacolo utilizzando il factory
        /// </summary>
        private Entity SpawnObstacle(
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int entityInQueryIndex,
            string obstacleCode,
            float3 position,
            float height,
            float scale)
        {
            // Questo metodo dovrebbe richiamare l'ObstacleFactory per creare l'ostacolo appropriato
            // In questa implementazione di esempio, creiamo un'entità base con i componenti necessari
            
            var obstacleEntity = commandBuffer.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti di base dell'ostacolo
            commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new ObstacleComponent
            {
                Height = height,
                Width = 1.0f * scale,
                CollisionRadius = 0.5f * scale,
                Strength = 100.0f,
                DamageValue = 25.0f,
                IsDestructible = true
            });
            
            // Aggiungi il tag dell'ostacolo
            commandBuffer.AddComponent<ObstacleTag>(entityInQueryIndex, obstacleEntity);
            
            // Aggiungi la posizione
            commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new TransformComponent
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = scale
            });
            
            // Aggiungi un componente per tenere traccia del codice dell'ostacolo
            commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new ObstacleTypeComponent
            {
                TypeCode = new Unity.Collections.FixedString32Bytes(obstacleCode)
            });
            
            // Qui andrebbe aggiunta la logica per configurare ostacoli specifici in base al codice
            // Ad esempio, se il codice inizia con "L" potrebbe essere un ostacolo di lava
            if (obstacleCode.StartsWith("L", StringComparison.OrdinalIgnoreCase))
            {
                commandBuffer.AddComponent<LavaTag>(entityInQueryIndex, obstacleEntity);
                commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new ToxicGroundTag
                {
                    ToxicType = 1, // Tipo fuoco/lava
                    DamagePerSecond = 20.0f
                });
            }
            // Ghiaccio
            else if (obstacleCode.StartsWith("I", StringComparison.OrdinalIgnoreCase))
            {
                commandBuffer.AddComponent<IceObstacleTag>(entityInQueryIndex, obstacleEntity);
                commandBuffer.AddComponent(entityInQueryIndex, obstacleEntity, new IceIntegrityComponent
                {
                    MaxIntegrity = 100.0f,
                    CurrentIntegrity = 100.0f
                });
            }
            // Barriere digitali
            else if (obstacleCode.StartsWith("D", StringComparison.OrdinalIgnoreCase))
            {
                commandBuffer.AddComponent<DigitalBarrierTag>(entityInQueryIndex, obstacleEntity);
            }
            
            return obstacleEntity;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Componente che identifica uno scenario tutorial
    /// </summary>
    public struct TutorialScenarioComponent : IComponentData
    {
        public FixedString64Bytes Name;
        public float DistanceFromStart;
        public FixedString128Bytes InstructionMessage;
        public float MessageDuration;
        public bool RandomPlacement;
        public float ObstacleSpacing;
        public float StartOffset;
    }
    
    /// <summary>
    /// Buffer per gli ostacoli di un tutorial
    /// </summary>
    public struct TutorialObstacleBuffer : IBufferElementData
    {
        public FixedString32Bytes ObstacleCode;
        public int Count;
        public byte Placement; // 0=Center, 1=Left, 2=Right, 3=Random, 4=Pattern
        public bool RandomizeHeight;
        public float2 HeightRange;
        public bool RandomizeScale;
        public float2 ScaleRange;
        public float StartOffset;
    }
    
    /// <summary>
    /// Tag per gli scenari tutorial già attivati
    /// </summary>
    [System.Serializable]
    public struct TriggeredTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tag per identificare i livelli tutorial
    /// </summary>
    [System.Serializable]
    public struct TutorialLevelTag : IComponentData
    {
        public int CurrentSequence;
        public bool Completed;
    }
}