using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema che gestisce il ciclo di vita delle entità nel mondo di gioco.
    /// Responsabile della creazione, distruzione e gestione dello stato di attivazione
    /// delle entità in base a criteri come distanza dalla telecamera, tempo di vita,
    /// o eventi di gioco.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct EntityLifecycleSystem : ISystem
    {
        // Query necessarie per controllare il ciclo di vita delle entità
        private EntityQuery _timeBasedEntitiesQuery;
        private EntityQuery _distanceBasedEntitiesQuery;
        private EntityQuery _poolableEntitiesQuery;
        
        /// <summary>
        /// Inizializza il sistema di gestione del ciclo di vita delle entità e 
        /// configura le query necessarie.
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // TODO: Inizializzare query di entità 
            // Esempio di inizializzazione (da completare con componenti reali)
            // _timeBasedEntitiesQuery = state.GetEntityQuery(ComponentType.ReadWrite<LifetimeComponent>());
            // _distanceBasedEntitiesQuery = state.GetEntityQuery(ComponentType.ReadOnly<ActivationDistanceComponent>());
            // _poolableEntitiesQuery = state.GetEntityQuery(ComponentType.ReadOnly<PoolableTag>());
            
            // Richiedi singleton di EndSimulationEntityCommandBufferSystem per il CommandBuffer
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce eventuali risorse allocate dal sistema
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup di risorse se necessario
        }

        /// <summary>
        /// Gestisce la creazione, distruzione e lo stato di attivazione delle entità
        /// in base a vari criteri come distanza, tempo di vita, o eventi specifici.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Ottieni il deltaTime per aggiornamenti basati sul tempo
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // TODO: Implementare la logica di gestione del ciclo di vita
            // - Distruggere entità che hanno raggiunto il loro tempo di vita massimo
            // - Disattivare/attivare entità in base alla distanza dalla telecamera
            // - Gestire pool di entità per oggetti riutilizzabili
            
            // Esempio di implementazione con SystemAPI (da completare con componenti reali):
            /*
            // Gestione entità con tempo di vita
            foreach (var (entity, lifetime) in 
                SystemAPI.Query<RefRW<LifetimeComponent>>().WithEntityAccess())
            {
                // Decrementa il tempo di vita rimanente
                lifetime.ValueRW.RemainingTime -= deltaTime;
                
                // Se il tempo è scaduto, distruggi l'entità o rimettila nel pool
                if (lifetime.ValueRW.RemainingTime <= 0)
                {
                    if (SystemAPI.HasComponent<PoolableTag>(entity))
                    {
                        // Rimetti nel pool se l'entità è riutilizzabile
                        commandBuffer.AddComponent(entity, new InactiveTag());
                        lifetime.ValueRW.RemainingTime = lifetime.ValueRO.DefaultLifetime;
                    }
                    else
                    {
                        // Altrimenti distruggi l'entità
                        commandBuffer.DestroyEntity(entity);
                    }
                }
            }
            
            // Gestione attivazione/disattivazione basata su distanza
            float3 cameraPosition = SystemAPI.GetSingleton<CameraPositionComponent>().Position;
            foreach (var (entity, activation) in 
                SystemAPI.Query<RefRO<ActivationDistanceComponent>>()
                    .WithEntityAccess()
                    .WithAll<TransformComponent>())
            {
                float3 entityPosition = SystemAPI.GetComponent<TransformComponent>(entity).Position;
                float sqrDistance = math.distancesq(cameraPosition, entityPosition);
                
                bool isActive = SystemAPI.HasComponent<ActiveTag>(entity);
                bool shouldBeActive = sqrDistance <= activation.ValueRO.ActivationDistanceSq;
                
                // Attiva/disattiva in base alla distanza
                if (shouldBeActive && !isActive)
                {
                    commandBuffer.AddComponent(entity, new ActiveTag());
                    commandBuffer.RemoveComponent<InactiveTag>(entity);
                }
                else if (!shouldBeActive && isActive)
                {
                    commandBuffer.RemoveComponent<ActiveTag>(entity);
                    commandBuffer.AddComponent(entity, new InactiveTag());
                }
            }
            */
        }
    }
}
