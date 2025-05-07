using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using UnityEngine.AI;
using UnityEngine;

namespace RunawayHeroes.ECS.Systems.Movement
{
    /// <summary>
    /// Sistema che gestisce la navigazione delle entità nel mondo di gioco,
    /// utilizzando il sistema di navigazione di Unity per calcolare percorsi
    /// evitando ostacoli e seguendo il mesh di navigazione.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NavigationSystem : ISystem
    {
        #region Fields

        // Query per entità che devono navigare
        private EntityQuery _navigatorQuery;
        
        #endregion
        
        #region Lifecycle

        /// <summary>
        /// Inizializza il sistema di navigazione, configurando le query e altre risorse necessarie
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità con navigazione
            _navigatorQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NavigatorComponent, TransformComponent>()
                .WithAllRW<PhysicsComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità naviganti per l'esecuzione dell'aggiornamento
            state.RequireForUpdate(_navigatorQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per generare eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna la navigazione di tutte le entità ad ogni frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            // Nota: Questo sistema non utilizza Burst perché interagisce con NavMesh di Unity
            
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Usa SystemAPI.Query invece di Entities.ForEach per maggiore flessibilità
            // quando si lavora con API di Unity che non sono compatibili con Burst
            foreach (var (entity, navigator, transform, physics) in 
                    SystemAPI.Query<EntityRef<Entity>, RefRW<NavigatorComponent>, RefRO<TransformComponent>, RefRW<PhysicsComponent>>()
                    .WithAll<NavigatorComponent>())
            {
                if (navigator.ValueRO.HasDestination)
                {
                    // Se il percorso è già stato calcolato
                    if (navigator.ValueRO.PathStatus == PathStatus.Ready && navigator.ValueRO.PathPointCount > 0)
                    {
                        // Ottieni il prossimo punto target
                        int currentPointIndex = navigator.ValueRO.CurrentPathIndex;
                        
                        if (currentPointIndex < navigator.ValueRO.PathPointCount)
                        {
                            float3 currentTarget = navigator.ValueRO.GetPathPoint(currentPointIndex);
                            float3 directionToTarget = currentTarget - transform.ValueRO.Position;
                            
                            // Ignora l'asse Y per movimento orizzontale
                            directionToTarget.y = 0;
                            
                            // Calcola la distanza al target
                            float distanceToTarget = math.length(directionToTarget);
                            
                            // Se siamo abbastanza vicini al punto corrente, passa al prossimo
                            if (distanceToTarget < navigator.ValueRO.WaypointReachedThreshold)
                            {
                                navigator.ValueRW.CurrentPathIndex++;
                                
                                // Se abbiamo raggiunto la destinazione finale
                                if (navigator.ValueRW.CurrentPathIndex >= navigator.ValueRO.PathPointCount)
                                {
                                    // Crea evento di destinazione raggiunta
                                    var destinationReachedEvent = commandBuffer.CreateEntity();
                                    commandBuffer.AddComponent(destinationReachedEvent, new DestinationReachedEvent
                                    {
                                        NavigatorEntity = entity.Value,
                                        FinalDestination = navigator.ValueRO.Destination,
                                        ActualPosition = transform.ValueRO.Position
                                    });
                                    
                                    // Resetta la navigazione
                                    navigator.ValueRW.HasDestination = false;
                                    navigator.ValueRW.PathStatus = PathStatus.None;
                                    
                                    // Ferma gradualmente l'entità
                                    physics.ValueRW.Velocity *= 0.8f;
                                }
                            }
                            else
                            {
                                // Normalizza la direzione e applica la velocità di movimento
                                float3 moveDirection = math.normalize(directionToTarget);
                                
                                // Smoothing del movimento (steering behavior semplificato)
                                float3 targetVelocity = moveDirection * navigator.ValueRO.MovementSpeed;
                                physics.ValueRW.Velocity = math.lerp(
                                    physics.ValueRO.Velocity,
                                    new float3(targetVelocity.x, physics.ValueRO.Velocity.y, targetVelocity.z),
                                    deltaTime * navigator.ValueRO.SteeringSpeed
                                );
                                
                                // Aggiorna la rotazione per guardare nella direzione del movimento
                                if (navigator.ValueRO.RotateTowardsTarget && math.lengthsq(moveDirection) > 0.01f)
                                {
                                    float targetAngle = math.atan2(moveDirection.x, moveDirection.z);
                                    quaternion targetRotation = quaternion.RotateY(targetAngle);
                                    
                                    // Smooth rotation
                                    // Nota: Questo richiederebbe anche l'aggiornamento del componente di rotazione 
                                    // che non è incluso nell'esempio
                                }
                            }
                        }
                    }
                    // Se il percorso non è ancora stato calcolato, calcolalo
                    else if (navigator.ValueRO.PathStatus == PathStatus.None || 
                            navigator.ValueRO.PathStatus == PathStatus.Failed)
                    {
                        // Calcola un nuovo percorso utilizzando NavMesh di Unity
                        CalculatePath(ref state, ref navigator.ValueRW, transform.ValueRO.Position);
                    }
                    // Se il percorso è in fase di calcolo, aspetta
                    else if (navigator.ValueRO.PathStatus == PathStatus.Calculating)
                    {
                        // In una implementazione reale, gestiremmo meglio async paths
                        // Ma per semplicità, assumiamo che il calcolo sia completato immediatamente
                        navigator.ValueRW.PathStatus = PathStatus.Ready;
                    }
                }
            }
        }
        
        #endregion

        #region Helpers
        
        /// <summary>
        /// Calcola un percorso utilizzando il NavMesh di Unity
        /// </summary>
        [BurstDiscard] // Non compatibile con Burst perché usa NavMesh di Unity
        private static void CalculatePath(ref SystemState state, ref NavigatorComponent navigator, float3 startPosition)
        {
            navigator.PathStatus = PathStatus.Calculating;
            
            // Crea un nuovo percorso
            NavMeshPath path = new NavMeshPath();
            
            // Calcola il percorso utilizzando NavMesh di Unity
            bool pathFound = NavMesh.CalculatePath(
                new Vector3(startPosition.x, startPosition.y, startPosition.z),
                new Vector3(navigator.Destination.x, navigator.Destination.y, navigator.Destination.z),
                NavMesh.AllAreas,
                path
            );
            
            if (pathFound && path.status == NavMeshPathStatus.PathComplete)
            {
                // Converti i corner del percorso in punti individuali per l'ECS
                Vector3[] corners = path.corners;
                int pointCount = math.min(corners.Length, NavigatorComponent.MaxPathPoints);
                
                // Imposta il conteggio dei punti del percorso
                navigator.PathPointCount = pointCount;
                
                // Salva i punti del percorso nelle proprietà individuali
                for (int i = 0; i < pointCount; i++)
                {
                    navigator.SetPathPoint(i, new float3(corners[i].x, corners[i].y, corners[i].z));
                }
                
                navigator.CurrentPathIndex = 0;
                navigator.PathStatus = PathStatus.Ready;
            }
            else
            {
                // Se il percorso non può essere completato, prova un percorso parziale o fallisci
                navigator.PathStatus = PathStatus.Failed;
                navigator.PathPointCount = 0;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Componente che rappresenta un'entità navigante
    /// </summary>
    public struct NavigatorComponent : IComponentData
    {
        public bool HasDestination;           // Indica se l'entità ha una destinazione
        public float3 Destination;            // Destinazione finale
        public float MovementSpeed;           // Velocità di movimento
        public float SteeringSpeed;           // Velocità di sterzata
        public bool RotateTowardsTarget;      // Se ruotare verso la direzione di movimento
        public PathStatus PathStatus;         // Stato attuale del calcolo del percorso
        
        // Definizione statica del numero massimo di punti nel percorso
        public const int MaxPathPoints = 20;
        
        // Punti del percorso calcolato (sostituito array con campi individuali)
        public float3 PathPoint1;
        public float3 PathPoint2;
        public float3 PathPoint3;
        public float3 PathPoint4;
        public float3 PathPoint5;
        public float3 PathPoint6;
        public float3 PathPoint7;
        public float3 PathPoint8;
        public float3 PathPoint9;
        public float3 PathPoint10;
        public float3 PathPoint11;
        public float3 PathPoint12;
        public float3 PathPoint13;
        public float3 PathPoint14;
        public float3 PathPoint15;
        public float3 PathPoint16;
        public float3 PathPoint17;
        public float3 PathPoint18;
        public float3 PathPoint19;
        public float3 PathPoint20;
        
        public int PathPointCount;            // Numero effettivo di punti nel percorso
        public int CurrentPathIndex;          // Indice corrente nel percorso
        public float WaypointReachedThreshold;// Distanza per considerare un waypoint raggiunto
        
        /// <summary>
        /// Ottiene il punto del percorso all'indice specificato
        /// </summary>
        public float3 GetPathPoint(int index)
        {
            if (index < 0 || index >= PathPointCount)
                return float3.zero;
                
            switch (index)
            {
                case 0: return PathPoint1;
                case 1: return PathPoint2;
                case 2: return PathPoint3;
                case 3: return PathPoint4;
                case 4: return PathPoint5;
                case 5: return PathPoint6;
                case 6: return PathPoint7;
                case 7: return PathPoint8;
                case 8: return PathPoint9;
                case 9: return PathPoint10;
                case 10: return PathPoint11;
                case 11: return PathPoint12;
                case 12: return PathPoint13;
                case 13: return PathPoint14;
                case 14: return PathPoint15;
                case 15: return PathPoint16;
                case 16: return PathPoint17;
                case 17: return PathPoint18;
                case 18: return PathPoint19;
                case 19: return PathPoint20;
                default: return float3.zero;
            }
        }
        
        /// <summary>
        /// Imposta il punto del percorso all'indice specificato
        /// </summary>
        public void SetPathPoint(int index, float3 point)
        {
            if (index < 0 || index >= MaxPathPoints)
                return;
                
            switch (index)
            {
                case 0: PathPoint1 = point; break;
                case 1: PathPoint2 = point; break;
                case 2: PathPoint3 = point; break;
                case 3: PathPoint4 = point; break;
                case 4: PathPoint5 = point; break;
                case 5: PathPoint6 = point; break;
                case 6: PathPoint7 = point; break;
                case 7: PathPoint8 = point; break;
                case 8: PathPoint9 = point; break;
                case 9: PathPoint10 = point; break;
                case 10: PathPoint11 = point; break;
                case 11: PathPoint12 = point; break;
                case 12: PathPoint13 = point; break;
                case 13: PathPoint14 = point; break;
                case 14: PathPoint15 = point; break;
                case 15: PathPoint16 = point; break;
                case 16: PathPoint17 = point; break;
                case 17: PathPoint18 = point; break;
                case 18: PathPoint19 = point; break;
                case 19: PathPoint20 = point; break;
            }
        }
    }
    
    /// <summary>
    /// Stato del calcolo del percorso
    /// </summary>
    public enum PathStatus : byte
    {
        None = 0,         // Nessun percorso
        Calculating = 1,  // Calcolo in corso
        Ready = 2,        // Percorso pronto
        Failed = 3        // Calcolo fallito
    }
    
    /// <summary>
    /// Evento generato quando un'entità raggiunge la sua destinazione
    /// </summary>
    public struct DestinationReachedEvent : IComponentData
    {
        public Entity NavigatorEntity;    // Entità che ha raggiunto la destinazione
        public float3 FinalDestination;   // Destinazione finale
        public float3 ActualPosition;     // Posizione effettiva di arrivo
    }
}