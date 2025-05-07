using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Systems.Movement;
using RunawayHeroes.ECS.Events;
using RunawayHeroes.ECS.Systems.Combat;
using RunawayHeroes.ECS.Components.Combat;

namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Sistema che gestisce l'applicazione del danno alle entità.
    /// Elabora gli eventi di danno, applica modificatori di resistenza/vulnerabilità 
    /// e notifica gli altri sistemi attraverso eventi specifici.
    /// </summary>
    [BurstCompile]
    public partial struct DamageSystem : ISystem
    {
        private EntityQuery _damageEventsQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Query per gli eventi di danno
            _damageEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DamageEvent>()
                .Build(ref state);
                
            // Richiedi che ci siano eventi di danno per l'esecuzione
            state.RequireForUpdate(_damageEventsQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da liberare
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il command buffer dal singleton
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Elabora gli eventi di danno utilizzando un IJobEntity
            new ProcessDamageEventsJob 
            { 
                ECB = commandBuffer.AsParallelWriter(),
                EntityManager = state.EntityManager,
                HealthLookup = state.GetComponentLookup<HealthComponent>(),
                ArmorLookup = state.GetComponentLookup<ArmorComponent>(true)
            }.ScheduleParallel(state.Dependency).Complete();
        }
    }
    
    /// <summary>
    /// Job per elaborare gli eventi di danno
    /// </summary>
    [BurstCompile]
    public partial struct ProcessDamageEventsJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [NativeDisableParallelForRestriction] public ComponentLookup<HealthComponent> HealthLookup;
        [ReadOnly] public ComponentLookup<ArmorComponent> ArmorLookup;
        [NativeDisableParallelForRestriction] public EntityManager EntityManager;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex, in DamageEvent damageEvent)
        {
            // Verifica che l'entità target esista ancora
            if (!EntityManager.Exists(damageEvent.TargetEntity))
            {
                // Distruggi l'evento e salta questo danno
                ECB.DestroyEntity(entityInQueryIndex, entity);
                return;
            }
            
            // Verifica che l'entità target abbia un componente salute
            if (!HealthLookup.HasComponent(damageEvent.TargetEntity))
            {
                // Distruggi l'evento e salta questo danno
                ECB.DestroyEntity(entityInQueryIndex, entity);
                return;
            }
            
            // Calcola il danno finale considerando armatura e resistenze
            float finalDamage = CalculateFinalDamage(damageEvent);
            
            // Applica il danno alla salute
            var health = HealthLookup[damageEvent.TargetEntity];
            
            // Controlla se l'entità è invulnerabile
            if (health.IsInvulnerable)
            {
                // Genera evento di danno bloccato
                var blockedEntity = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, blockedEntity, new DamageBlockedEvent
                {
                    TargetEntity = damageEvent.TargetEntity,
                    SourceEntity = damageEvent.SourceEntity,
                    DamageAmount = finalDamage,
                    DamageType = damageEvent.DamageType,
                    BlockReason = BlockReason.Invulnerability
                });
                
                // Distruggi l'evento originale
                ECB.DestroyEntity(entityInQueryIndex, entity);
                return;
            }
            
            // Applica il danno e aggiorna il componente salute
            float actualDamage = health.ApplyDamage(finalDamage);
            HealthLookup[damageEvent.TargetEntity] = health;
            
            // Crea un evento di danno ricevuto
            var receivedEntity = ECB.CreateEntity(entityInQueryIndex);
            ECB.AddComponent(entityInQueryIndex, receivedEntity, new DamageReceivedEvent
            {
                TargetEntity = damageEvent.TargetEntity,
                SourceEntity = damageEvent.SourceEntity,
                DamageAmount = actualDamage,
                DamageType = damageEvent.DamageType,
                ImpactPosition = damageEvent.ImpactPosition,
                RemainingHealth = health.CurrentHealth,
                RemainingShield = health.CurrentShield
            });
            
            // Se la salute è zero o meno, genera evento di morte
            if (health.CurrentHealth <= 0)
            {
                var deathEntity = ECB.CreateEntity(entityInQueryIndex);
                ECB.AddComponent(entityInQueryIndex, deathEntity, new DeathEvent
                {
                    DeadEntity = damageEvent.TargetEntity,
                    KillerEntity = damageEvent.SourceEntity,
                    DamageType = damageEvent.DamageType,
                    DeathPosition = damageEvent.ImpactPosition
                });
            }
            
            // Distruggi l'evento originale
            ECB.DestroyEntity(entityInQueryIndex, entity);
        }
        
        /// <summary>
        /// Calcola il danno finale considerando armatura e resistenze
        /// </summary>
        private float CalculateFinalDamage(DamageEvent damageEvent)
        {
            float damageAmount = damageEvent.DamageAmount;
            
            // Considera armatura se presente
            if (ArmorLookup.HasComponent(damageEvent.TargetEntity))
            {
                var armor = ArmorLookup[damageEvent.TargetEntity];
                
                // Applica riduzione del danno in base all'armatura e tipo di danno
                switch (damageEvent.DamageType)
                {
                    case DamageType.Obstacle:
                        damageAmount *= (1.0f - armor.PhysicalDamageReduction);
                        break;
                    case DamageType.Fall:
                        damageAmount *= (1.0f - armor.FallDamageReduction);
                        break;
                    case DamageType.Enemy:
                        damageAmount *= (1.0f - armor.EnemyDamageReduction);
                        break;
                    case DamageType.Hazard:
                        damageAmount *= (1.0f - armor.HazardDamageReduction);
                        break;
                    case DamageType.StatusEffect:
                        damageAmount *= (1.0f - armor.StatusEffectResistance);
                        break;
                }
            }
            
            return math.max(0, damageAmount);
        }
    }
    
    // ArmorComponent è stato spostato in RunawayHeroes.ECS.Components.Combat.ArmorComponent
}
