using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Systems.Combat;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Sistema che gestisce l'effetto di knockback (spinta) applicato alle entità
    /// in seguito a colpi, esplosioni o altre forze. Applica una forza temporanea
    /// che spinge l'entità in una certa direzione, considerando peso, resistenza
    /// e altre caratteristiche dell'entità.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(HitboxSystem))]
    [BurstCompile]
    public partial struct KnockbackSystem : ISystem
    {
        // Query per entità con componente knockback
        private EntityQuery _knockbackQuery;
        
        // Query per gli eventi di hit che possono generare knockback
        private EntityQuery _hitEventsQuery;
        
        /// <summary>
        /// Inizializza il sistema di knockback e prepara le query per le entità
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità con knockback attivo
            _knockbackQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<KnockbackComponent, PhysicsComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per gli eventi di hit
            _hitEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HitEvent>()
                .Build(ref state);
                
            // Richiedi che ci siano eventi di hit o entità con knockback per l'aggiornamento
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna gli effetti di knockback ad ogni frame
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Processa gli eventi di hit per applicare nuovo knockback
            if (!_hitEventsQuery.IsEmpty)
            {
                // Ottieni gli eventi di hit
                var hitEvents = _hitEventsQuery.ToComponentDataArray<HitEvent>(Allocator.TempJob);
                var hitEntities = _hitEventsQuery.ToEntityArray(Allocator.TempJob);
                
                // Processa gli eventi di hit con un job
                state.Dependency = new ProcessHitEventsJob
                {
                    HitEvents = hitEvents,
                    HitEntities = hitEntities,
                    ECB = commandBuffer.AsParallelWriter(),
                    EntityManager = state.EntityManager,
                    PhysicsLookup = state.GetComponentLookup<PhysicsComponent>()
                }.Schedule(state.Dependency);
                
                // Pulisci le risorse allocate
                state.Dependency = hitEvents.Dispose(state.Dependency);
                state.Dependency = hitEntities.Dispose(state.Dependency);
            }
            
            // Aggiorna le entità con knockback attivo
            if (!_knockbackQuery.IsEmpty)
            {
                // Processa le entità con knockback usando IJobEntity
                state.Dependency = new UpdateKnockbackJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_knockbackQuery, state.Dependency);
            }
        }
    }
    
    /// <summary>
    /// Job che processa gli eventi di hit per applicare nuovo knockback
    /// </summary>
    [BurstCompile]
    public struct ProcessHitEventsJob : IJob
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeArray<HitEvent> HitEvents;
        [ReadOnly] public NativeArray<Entity> HitEntities;
        [NativeDisableParallelForRestriction] public EntityManager EntityManager;
        [NativeDisableParallelForRestriction] public ComponentLookup<PhysicsComponent> PhysicsLookup;
        
        [BurstCompile]
        public void Execute()
        {
            for (int i = 0; i < HitEvents.Length; i++)
            {
                var hitEvent = HitEvents[i];
                var hitEntity = HitEntities[i];
                
                // Indice per il comando parallelo
                int entityInQueryIndex = i;
                
                // Se c'è una forza di knockback e c'è un'entità vittima valida
                if (hitEvent.KnockbackForce > 0 && 
                    EntityManager.Exists(hitEvent.VictimEntity) &&
                    PhysicsLookup.HasComponent(hitEvent.VictimEntity))
                {
                    // Calcola la durata del knockback in base alla forza
                    float knockbackDuration = math.min(hitEvent.KnockbackForce * 0.05f, 0.5f);
                    
                    // Controlla se l'entità ha già un componente knockback
                    if (EntityManager.HasComponent<KnockbackComponent>(hitEvent.VictimEntity))
                    {
                        // Aggiorna il componente esistente
                        var knockback = EntityManager.GetComponentData<KnockbackComponent>(hitEvent.VictimEntity);
                        
                        // Somma le forze (versione semplificata)
                        knockback.Direction = math.normalize(knockback.Direction * knockback.RemainingForce + 
                                                      hitEvent.KnockbackDirection * hitEvent.KnockbackForce);
                        knockback.RemainingForce = math.max(knockback.RemainingForce, hitEvent.KnockbackForce);
                        knockback.RemainingTime = math.max(knockback.RemainingTime, knockbackDuration);
                        
                        // Aggiorna il componente
                        ECB.SetComponent(entityInQueryIndex, hitEvent.VictimEntity, knockback);
                    }
                    else
                    {
                        // Crea un nuovo componente knockback
                        ECB.AddComponent(entityInQueryIndex, hitEvent.VictimEntity, new KnockbackComponent
                        {
                            Direction = hitEvent.KnockbackDirection,
                            RemainingForce = hitEvent.KnockbackForce,
                            RemainingTime = knockbackDuration,
                            AttackerEntity = hitEvent.AttackerEntity
                        });
                    }
                    
                    // Crea un evento di inizio knockback
                    var knockbackStartEvent = ECB.CreateEntity(entityInQueryIndex);
                    ECB.AddComponent(entityInQueryIndex, knockbackStartEvent, new KnockbackStartedEvent
                    {
                        VictimEntity = hitEvent.VictimEntity,
                        AttackerEntity = hitEvent.AttackerEntity,
                        KnockbackDirection = hitEvent.KnockbackDirection,
                        KnockbackForce = hitEvent.KnockbackForce,
                        StartPosition = hitEvent.HitPoint
                    });
                }
                
                // Rimuovi l'evento di hit processato
                ECB.DestroyEntity(entityInQueryIndex, hitEntity);
            }
        }
    }
    
    /// <summary>
    /// Job che aggiorna le entità con knockback attivo
    /// </summary>
    [BurstCompile]
    public partial struct UpdateKnockbackJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [BurstCompile]
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex,
                          ref KnockbackComponent knockback,
                          ref PhysicsComponent physics)
        {
            // Aggiorna il tempo rimanente
            knockback.RemainingTime -= DeltaTime;
            
            // Se il knockback è ancora attivo
            if (knockback.RemainingTime > 0)
            {
                // Calcola la forza da applicare in questo frame
                float forceThisFrame = knockback.RemainingForce * 
                                      (knockback.RemainingTime / knockback.Duration);
                
                // Applica la forza alla velocità dell'entità
                physics.Velocity += knockback.Direction * forceThisFrame * DeltaTime;
                
                // Riduce gradualmente la forza rimanente
                knockback.RemainingForce *= (1.0f - DeltaTime);
            }
            else
            {
                // Crea un evento di fine knockback
                var knockbackEndEvent = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, knockbackEndEvent, new KnockbackEndedEvent
                {
                    VictimEntity = entity,
                    AttackerEntity = knockback.AttackerEntity,
                    TotalDistance = 0 // Andrebbe calcolata la distanza totale percorsa
                });
                
                // Rimuovi il componente knockback
                ECB.RemoveComponent<KnockbackComponent>(entityInQueryIndex, entity);
            }
        }
    }
    
    /// <summary>
    /// Componente che rappresenta un effetto di knockback attivo su un'entità
    /// </summary>
    public struct KnockbackComponent : IComponentData
    {
        public float3 Direction;         // Direzione del knockback
        public float RemainingForce;     // Forza rimanente
        public float RemainingTime;      // Tempo rimanente
        public float Duration;           // Durata totale (per calcoli di ratio)
        public Entity AttackerEntity;    // Entità che ha causato il knockback
    }
    
    /// <summary>
    /// Evento generato quando inizia un effetto di knockback
    /// </summary>
    public struct KnockbackStartedEvent : IComponentData
    {
        public Entity VictimEntity;         // Entità che subisce il knockback
        public Entity AttackerEntity;       // Entità che ha causato il knockback
        public float3 KnockbackDirection;   // Direzione del knockback
        public float KnockbackForce;        // Forza del knockback
        public float3 StartPosition;        // Posizione di partenza
    }
    
    /// <summary>
    /// Evento generato quando termina un effetto di knockback
    /// </summary>
    public struct KnockbackEndedEvent : IComponentData
    {
        public Entity VictimEntity;         // Entità che ha subito il knockback
        public Entity AttackerEntity;       // Entità che ha causato il knockback
        public float TotalDistance;         // Distanza totale percorsa
    }
}
