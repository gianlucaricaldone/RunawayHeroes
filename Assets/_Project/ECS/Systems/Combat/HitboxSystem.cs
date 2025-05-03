using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Systems.Combat;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.Core;

namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Sistema responsabile della gestione delle hitbox di attacco e hurtbox di danno.
    /// Rileva collisioni tra hitbox offensive e hurtbox difensive, generando eventi di danno
    /// quando si verificano impatti validi. Gestisce anche la tempistica degli attacchi e
    /// i frame di invulnerabilità.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystem))]
    [BurstCompile]
    public partial struct HitboxSystem : ISystem
    {
        // Query per le entity con hitbox (attaccanti)
        private EntityQuery _hitboxQuery;
        
        // Query per le entity con hurtbox (potenziali target)
        private EntityQuery _hurtboxQuery;
        
        /// <summary>
        /// Inizializza il sistema di hitbox e prepara le query per le entità
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le hitbox (box di attacco)
            _hitboxQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HitboxComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per le hurtbox (box di vulnerabilità)
            _hurtboxQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HurtboxComponent, TransformComponent>()
                .WithNone<InvulnerableTag>() // Opzionalmente escludi entità invulnerabili
                .Build(ref state);
                
            // Richiedi che ci siano entity con hitbox per l'aggiornamento
            state.RequireForUpdate(_hitboxQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per generare eventi
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
        /// Aggiorna le hitbox e rileva le collisioni ad ogni frame
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Raccogli tutte le entity con hitbox attive
            var hitboxEntities = _hitboxQuery.ToEntityArray(Allocator.TempJob);
            var hitboxComponents = _hitboxQuery.ToComponentDataArray<HitboxComponent>(Allocator.TempJob);
            var hitboxTransforms = _hitboxQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Raccogli tutte le entity con hurtbox
            var hurtboxEntities = _hurtboxQuery.ToEntityArray(Allocator.TempJob);
            var hurtboxComponents = _hurtboxQuery.ToComponentDataArray<HurtboxComponent>(Allocator.TempJob);
            var hurtboxTransforms = _hurtboxQuery.ToComponentDataArray<TransformComponent>(Allocator.TempJob);
            
            // Processa le hitbox e rileva collisioni con job
            state.Dependency = new ProcessHitboxesJob
            {
                DeltaTime = deltaTime,
                HitboxEntities = hitboxEntities,
                HitboxComponents = hitboxComponents,
                HitboxTransforms = hitboxTransforms,
                HurtboxEntities = hurtboxEntities,
                HurtboxComponents = hurtboxComponents,
                HurtboxTransforms = hurtboxTransforms,
                ECB = commandBuffer.AsParallelWriter()
            }.Schedule(state.Dependency);
            
            // Pulisci le risorse allocate
            state.Dependency = hitboxEntities.Dispose(state.Dependency);
            state.Dependency = hitboxComponents.Dispose(state.Dependency);
            state.Dependency = hitboxTransforms.Dispose(state.Dependency);
            state.Dependency = hurtboxEntities.Dispose(state.Dependency);
            state.Dependency = hurtboxComponents.Dispose(state.Dependency);
            state.Dependency = hurtboxTransforms.Dispose(state.Dependency);
        }
    }
    
    /// <summary>
    /// Job che processa tutte le hitbox e rileva collisioni con hurtbox
    /// </summary>
    [BurstCompile]
    public struct ProcessHitboxesJob : IJob
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public NativeArray<Entity> HitboxEntities;
        [ReadOnly] public NativeArray<HitboxComponent> HitboxComponents;
        [ReadOnly] public NativeArray<TransformComponent> HitboxTransforms;
        
        [ReadOnly] public NativeArray<Entity> HurtboxEntities;
        [ReadOnly] public NativeArray<HurtboxComponent> HurtboxComponents;
        [ReadOnly] public NativeArray<TransformComponent> HurtboxTransforms;
        
        [BurstCompile]
        public void Execute()
        {
            // Controlla ogni hitbox contro ogni hurtbox
            for (int i = 0; i < HitboxEntities.Length; i++)
            {
                var hitboxEntity = HitboxEntities[i];
                var hitbox = HitboxComponents[i];
                var hitboxTransform = HitboxTransforms[i];
                
                // Aggiorna la durata della hitbox
                hitbox.RemainingActiveTime -= DeltaTime;
                
                // Se la hitbox è ancora attiva
                if (hitbox.RemainingActiveTime > 0)
                {
                    // Verifica collisioni con tutte le hurtbox
                    for (int j = 0; j < HurtboxEntities.Length; j++)
                    {
                        var hurtboxEntity = HurtboxEntities[j];
                        var hurtbox = HurtboxComponents[j];
                        var hurtboxTransform = HurtboxTransforms[j];
                        
                        // Evita auto-hit se appartengono alla stessa entità
                        if (hitbox.OwnerEntity == hurtbox.OwnerEntity)
                            continue;
                            
                        // Controlla se l'hurtbox ha già subito un colpo da questa hitbox
                        if (hitbox.HitEntities.Contains(hurtboxEntity))
                            continue;
                            
                        // Calcola la distanza tra i centri
                        float distanceSquared = math.distancesq(hitboxTransform.Position, hurtboxTransform.Position);
                        float radiusSum = hitbox.Radius + hurtbox.Radius;
                        
                        // Se c'è collisione
                        if (distanceSquared <= radiusSum * radiusSum)
                        {
                            // Aggiungi l'entità colpita alla lista per evitare colpi multipli
                            // Nota: nella realtà vogliamo utilizzare una NativeList, qui è semplificato
                            // hitbox.HitEntities.Add(hurtboxEntity);
                            
                            // Calcola il punto di impatto (semplificato)
                            float3 impactPoint = (hitboxTransform.Position + hurtboxTransform.Position) * 0.5f;
                            
                            // Genera un evento di danno
                            int entityInQueryIndex = i; // Indice usato per la parallelizzazione
                            
                            var hitEvent = ECB.CreateEntity(entityInQueryIndex);
                            ECB.AddComponent(entityInQueryIndex, hitEvent, new HitEvent
                            {
                                AttackerEntity = hitbox.OwnerEntity,
                                VictimEntity = hurtbox.OwnerEntity,
                                HitboxEntity = hitboxEntity,
                                HurtboxEntity = hurtboxEntity,
                                HitPoint = impactPoint,
                                Damage = hitbox.Damage,
                                AttackType = hitbox.AttackType,
                                KnockbackDirection = math.normalize(hurtboxTransform.Position - hitboxTransform.Position),
                                KnockbackForce = hitbox.KnockbackForce
                            });
                        }
                    }
                }
                else
                {
                    // La hitbox non è più attiva, si può distruggere
                    // o disabilitare a seconda dell'implementazione
                    int entityInQueryIndex = i;
                    
                    // Opzionalmente, distruggi la hitbox
                    ECB.DestroyEntity(entityInQueryIndex, hitboxEntity);
                    
                    // Oppure potresti solo disabilitarla aggiungendo un tag o modificando proprietà
                    // ECB.AddComponent(entityInQueryIndex, hitboxEntity, new InactiveTag());
                }
            }
        }
    }
    
    /// <summary>
    /// Componente che definisce una hitbox (box di attacco)
    /// </summary>
    public struct HitboxComponent : IComponentData
    {
        public Entity OwnerEntity;          // Entità che ha generato l'hitbox
        public float Radius;                // Raggio di collisione
        public float Damage;                // Quantità di danno
        public AttackType AttackType;       // Tipo di attacco
        public float KnockbackForce;        // Forza del knockback
        public float InitialActiveTime;     // Durata iniziale dell'attacco
        public float RemainingActiveTime;   // Tempo rimanente dell'attacco
        // La lista delle entità colpite andrebbe implementata con NativeList/NativeArray
        // ma è semplificata per l'esempio
        public Entity HitEntities;          // Entità già colpite da questa hitbox (evita colpi multipli)
    }
    
    /// <summary>
    /// Componente che definisce una hurtbox (box di vulnerabilità)
    /// </summary>
    public struct HurtboxComponent : IComponentData
    {
        public Entity OwnerEntity;          // Entità a cui appartiene l'hurtbox
        public float Radius;                // Raggio di collisione
        public float DamageMultiplier;      // Moltiplicatore del danno (per colpi critici, ecc.)
    }
    
    /// <summary>
    /// Tag per indicare entità temporaneamente invulnerabili
    /// </summary>
    public struct InvulnerableTag : IComponentData
    {
    }
    
    /// <summary>
    /// Tipi di attacco supportati
    /// </summary>
    public enum AttackType : byte
    {
        Melee = 0,        // Attacco corpo a corpo
        Ranged = 1,       // Attacco a distanza
        Special = 2,      // Attacco speciale
        Environmental = 3 // Danno ambientale
    }
    
    /// <summary>
    /// Evento generato quando una hitbox colpisce una hurtbox
    /// </summary>
    public struct HitEvent : IComponentData
    {
        public Entity AttackerEntity;     // Entità che ha eseguito l'attacco
        public Entity VictimEntity;       // Entità che ha subito l'attacco
        public Entity HitboxEntity;       // Hitbox che ha colpito
        public Entity HurtboxEntity;      // Hurtbox che è stata colpita
        public float3 HitPoint;           // Punto di impatto
        public float Damage;              // Danno potenziale
        public AttackType AttackType;     // Tipo di attacco
        public float3 KnockbackDirection; // Direzione del knockback
        public float KnockbackForce;      // Forza del knockback
    }
}
