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
using RunawayHeroes.ECS.Systems.Gameplay.Group;
using RunawayHeroes.ECS.Components.World.Obstacles;

namespace RunawayHeroes.ECS.Systems.Gameplay
{
    /// <summary>
    /// Sistema che gestisce l'avanzamento del tutorial e la generazione degli scenari didattici
    /// </summary>
    [UpdateInGroup(typeof(GameplaySystemGroup))]
    public partial struct TutorialGuidanceSystem : ISystem
    {
        private EntityQuery _activePlayerQuery;
        private EntityQuery _scenarioQuery;
        
        // Costanti per il posizionamento degli ostacoli
        private const float LANE_WIDTH = 9f; // Larghezza totale della corsia
        private const float LEFT_POSITION = -3f; // Posizione laterale sinistra
        private const float CENTER_POSITION = 0f; // Posizione centrale
        private const float RIGHT_POSITION = 3f; // Posizione laterale destra
        
        // Seed per generazione casuale
        private uint _seed;
        
        public void OnCreate(ref SystemState state)
        {
            // Inizializza il generatore di numeri casuali
            _seed = (uint)UnityEngine.Random.Range(1, 1000000);
            
            // Configura query per il giocatore attivo
            _activePlayerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, ActiveTag, TransformComponent>()
                .Build(ref state);
            
            // Configura query per gli scenari di tutorial
            _scenarioQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TutorialScenarioComponent, TutorialObstacleBuffer>()
                .Build(ref state);
            
            // Richiedi singleton per il command buffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            
            // Richiedi aggiornamento solo quando esiste un giocatore attivo
            state.RequireForUpdate(_activePlayerQuery);
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Risorse da ripulire, se necessario
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.HasSingleton<TutorialLevelTag>())
                return;
                
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            var playerEntities = _activePlayerQuery.ToEntityArray(Allocator.TempJob);
            
            if (playerEntities.Length == 0)
            {
                playerEntities.Dispose();
                return;
            }
            
            var playerEntity = playerEntities[0];
            var playerPosition = SystemAPI.GetComponent<TransformComponent>(playerEntity).Position;
            playerEntities.Dispose();
            
            // Costruiamo la query per gli scenari non ancora attivati
            var scenarioQuery = SystemAPI.QueryBuilder()
                .WithAll<TutorialScenarioComponent, DynamicBuffer<TutorialObstacleBuffer>>()
                .WithNone<TriggeredTag>()
                .Build();
                
            // Eseguiamo il job per controllare i nuovi scenari
            state.Dependency = new CheckScenarioTriggerJob 
            {
                PlayerPosition = playerPosition,
                ECB = commandBuffer,
                Seed = _seed,
                LaneWidth = LANE_WIDTH,
                LeftPosition = LEFT_POSITION,
                CenterPosition = CENTER_POSITION,
                RightPosition = RIGHT_POSITION
            }.ScheduleParallel(scenarioQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job per controllare e attivare scenari tutorial
    /// </summary>
    [BurstCompile]
    public partial struct CheckScenarioTriggerJob : IJobEntity
    {
        [ReadOnly] public float3 PlayerPosition;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public uint Seed;
        
        // Costanti per il posizionamento degli ostacoli
        [ReadOnly] public float LaneWidth;
        [ReadOnly] public float LeftPosition;
        [ReadOnly] public float CenterPosition;
        [ReadOnly] public float RightPosition;
        
        [BurstDiscard]
        public void Execute(Entity entity, 
                          [ChunkIndexInQuery] int entityInQueryIndex,
                          ref TutorialScenarioComponent scenario,
                          in DynamicBuffer<TutorialObstacleBuffer> obstacleBuffer)
        {
            // Se il giocatore ha raggiunto la distanza di inizio scenario
            if (PlayerPosition.z >= scenario.DistanceFromStart)
            {
                // Marca questo scenario come attivato
                ECB.AddComponent<TriggeredTag>(entityInQueryIndex, entity);
                
                // Mostra messaggio di istruzione
                if (!string.IsNullOrEmpty(scenario.InstructionMessage))
                {
                    // Crea un'entità per il messaggio UI
                    Entity messageEntity = ECB.CreateEntity(entityInQueryIndex);
                    
                    // Aggiungi il componente di messaggio UI
                    ECB.AddComponent(entityInQueryIndex, messageEntity, 
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
                    ECB.AddComponent(entityInQueryIndex, messageEntity,
                        new RunawayHeroes.ECS.Components.UI.QueuedMessageTag
                        {
                            QueuePosition = 0
                        });
                    
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.Log($"[Tutorial] Messaggio creato: {scenario.InstructionMessage}");
                #endif
                }
                
                // Genera gli ostacoli per questo scenario
                SpawnObstaclesForScenario(entityInQueryIndex, scenario, obstacleBuffer);
            }
        }
        
        /// <summary>
        /// Genera gli ostacoli per uno scenario di tutorial
        /// </summary>
        [BurstDiscard]
        private void SpawnObstaclesForScenario(
            int entityInQueryIndex,
            TutorialScenarioComponent scenario,
            DynamicBuffer<TutorialObstacleBuffer> obstacleBuffer)
        {
            // Genera ostacoli per ogni configurazione nel buffer
            for (int i = 0; i < obstacleBuffer.Length; i++)
            {
                var obstacleSetup = obstacleBuffer[i];
                SpawnTutorialObstaclesAdvanced(
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
        [BurstDiscard]
        private void SpawnTutorialObstaclesAdvanced(
            int entityInQueryIndex,
            float startZ,
            float spacing,
            bool randomPlacement,
            TutorialObstacleBuffer obstacleSetup)
        {
            // Crea un nuovo seed per ogni chiamata per evitare pattern ripetuti
            uint localSeed = Seed + (uint)(obstacleSetup.ObstacleCode.GetHashCode() + entityInQueryIndex);
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
                        xPos = CenterPosition;
                        break;
                    case 1: // Left
                        xPos = LeftPosition;
                        break;
                    case 2: // Right
                        xPos = RightPosition;
                        break;
                    case 3: // Random
                        xPos = random.NextFloat(-LaneWidth/2f, LaneWidth/2f);
                        break;
                    case 4: // Pattern
                        // Distribuzione uniforme degli ostacoli in un pattern attraverso la corsia
                        float pattern = (float)i / (float)(obstacleSetup.Count - 1);
                        xPos = math.lerp(-LaneWidth/2f, LaneWidth/2f, pattern);
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
        [BurstDiscard]
        private Entity SpawnObstacle(
            int entityInQueryIndex,
            string obstacleCode,
            float3 position,
            float height,
            float scale)
        {
            // Questo metodo dovrebbe richiamare l'ObstacleFactory per creare l'ostacolo appropriato
            // In questa implementazione di esempio, creiamo un'entità base con i componenti necessari
            
            var obstacleEntity = ECB.CreateEntity(entityInQueryIndex);
            
            // Aggiungi i componenti di base dell'ostacolo
            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new ObstacleComponent
            {
                Height = height,
                Width = 1.0f * scale,
                CollisionRadius = 0.5f * scale,
                Strength = 100.0f,
                DamageValue = 25.0f,
                IsDestructible = true
            });
            
            // Aggiungi il tag dell'ostacolo
            ECB.AddComponent<ObstacleTag>(entityInQueryIndex, obstacleEntity);
            
            // Aggiungi la posizione
            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new TransformComponent
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = new float3(scale, scale, scale)
            });
            
            // Analizza il codice dell'ostacolo per determinarne la categoria
            var obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.SmallBarrier;
            bool isUniversal = false;
            
            // Verifica se è un ostacolo universale (inizia con U)
            if (obstacleCode.Length > 0 && (obstacleCode[0] == 'U' || obstacleCode[0] == 'u'))
            {
                isUniversal = true;
            }
            
            // Determina la categoria in base al secondo carattere numerico del codice (se disponibile)
            if (obstacleCode.Length >= 3 && char.IsDigit(obstacleCode[2]))
            {
                int categoryNum = int.Parse(obstacleCode[2].ToString());
                switch (categoryNum)
                {
                    case 1: 
                    case 2: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.SmallBarrier; break;
                    case 3: 
                    case 4: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.MediumBarrier; break;
                    case 5: 
                    case 6: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.LargeBarrier; break;
                    case 7: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.AreaEffect; break;
                    case 8: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.SpecialEffect; break;
                    default: obstacleCategory = RunawayHeroes.ECS.Components.World.Obstacles.ObstacleCategory.SmallBarrier; break;
                }
            }
            
            // Crea un ID univoco basato sul codice completo
            ushort obstacleId = (ushort)obstacleCode.GetHashCode();
            
            // Aggiungi un componente per tenere traccia del codice dell'ostacolo
            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new RunawayHeroes.ECS.Components.World.Obstacles.ObstacleTypeComponent
            {
                ObstacleID = obstacleId,
                Category = obstacleCategory,
                IsUniversal = isUniversal
            });
            
            // Configura ostacoli specifici in base al prefisso del codice
            // I prefissi seguono la convenzione descritta nel catalogo ostacoli:
            // U: Universali, C: Città, F: Foresta, T: Tundra, V: Vulcano, A: Abisso, D: Digitale
            if (obstacleCode.Length > 0)
            {
                char prefix = obstacleCode[0];
                
                switch (prefix)
                {
                    // Ostacoli del Vulcano (V) - compatibili con l'abilità Corpo Ignifugo di Ember
                    case 'V':
                    case 'v':
                        if (obstacleCode.StartsWith("V01") || obstacleCode.StartsWith("V02") || 
                            obstacleCode.StartsWith("V04") || obstacleCode.StartsWith("V07"))
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new LavaTag
                            {
                                DamagePerSecond = 20.0f
                            });
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new ToxicGroundTag
                            {
                                ToxicType = 1, // Tipo fuoco/lava
                                DamagePerSecond = 20.0f
                            });
                        }
                        break;
                    
                    // Ostacoli della Tundra (T) - compatibili con l'abilità Aura di Calore di Kai
                    case 'T':
                    case 't':
                        if (obstacleCode.StartsWith("T01") || obstacleCode.StartsWith("T02") || obstacleCode.StartsWith("T03"))
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new IceObstacleTag
                            {
                                SlipperyFactor = 0.8f
                            });
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new IceIntegrityComponent
                            {
                                MaxIntegrity = 100.0f,
                                CurrentIntegrity = 100.0f
                            });
                        }
                        else if (obstacleCode.StartsWith("T08")) // Freezing Wind
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new CurrentTag
                            {
                                Direction = new Unity.Mathematics.float3(1, 0, 0), // Direzione orizzontale
                                Strength = 3.0f,
                                CurrentType = 2 // Tipo aria fredda
                            });
                        }
                        break;
                    
                    // Ostacoli Digitali (D) - compatibili con l'abilità Glitch Controllato di Neo
                    case 'D':
                    case 'd':
                        if (obstacleCode.StartsWith("D01") || obstacleCode.StartsWith("D02"))
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new DigitalBarrierTag
                            {
                                SecurityLevel = 1
                            });
                        }
                        break;
                    
                    // Ostacoli dell'Abisso (A) - compatibili con l'abilità Bolla d'Aria di Marina
                    case 'A':
                    case 'a':
                        if (obstacleCode.StartsWith("A01"))
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new UnderwaterTag
                            {
                                DepthPressure = 1.5f
                            });
                        }
                        else if (obstacleCode.StartsWith("A02"))
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new CurrentTag
                            {
                                Direction = new Unity.Mathematics.float3(0, 0, 1), // Direzione lungo il percorso
                                Strength = 3.0f,
                                CurrentType = 2 // Tipo acqua
                            });
                        }
                        break;
                        
                    // Ostacoli della Foresta (F) - alcuni compatibili con abilità speciali
                    case 'F':
                    case 'f':
                        if (obstacleCode.StartsWith("F07")) // Mud Pit
                        {
                            ECB.AddComponent(entityInQueryIndex, obstacleEntity, new SlipperyTag
                            {
                                SlipFactor = 0.5f
                            });
                        }
                        break;
                }
            }
            
            return obstacleEntity;
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
    
    // TutorialObstacleBuffer is now defined in RunawayHeroes.ECS.Components.Gameplay.TutorialComponents
    
    /// <summary>
    /// Tag per gli scenari tutorial già attivati
    /// </summary>
    [System.Serializable]
    public struct TriggeredTag : IComponentData
    {
    }
    
    // Nota: TutorialLevelTag è ora definito in RunawayHeroes.ECS.Components.Gameplay
    // per evitare duplicazione di definizioni di componenti
}