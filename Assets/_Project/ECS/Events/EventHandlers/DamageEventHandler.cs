using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Combat;

namespace RunawayHeroes.ECS.Events.Handlers
{
    /// <summary>
    /// Sistema che gestisce gli eventi di danno inflitti tra entità
    /// </summary>
    [BurstCompile]
    public partial struct DamageEventHandler : ISystem
    {
        private EntityQuery _damageEventsQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query necessarie
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query per le entità con DamageEvent
            _damageEventsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<DamageEvent>()
                .Build(ref state);
                
            state.RequireForUpdate(_damageEventsQuery);
        }

        /// <summary>
        /// Esegue la pulizia delle risorse allocate
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Cleanup di eventuali risorse
        }

        /// <summary>
        /// Elabora tutti gli eventi di danno presenti nel frame corrente
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottiene il buffer di comandi per eventuali modifiche strutturali
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Prepara i ComponentLookup
            var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>(true);
            var defenseLookup = SystemAPI.GetComponentLookup<DefenseComponent>(true);
            
            // Esegue il job di elaborazione eventi
            state.Dependency = new ProcessDamageEventsJob
            {
                ECB = ecb,
                HealthLookup = healthLookup,
                DefenseLookup = defenseLookup
            }.ScheduleParallel(state.Dependency);
        }
        
        /// <summary>
        /// Job che elabora gli eventi di danno
        /// </summary>
        [BurstCompile]
        private partial struct ProcessDamageEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public ComponentLookup<HealthComponent> HealthLookup;
            [ReadOnly] public ComponentLookup<DefenseComponent> DefenseLookup;
            
            // Definisce il metodo che verrà eseguito per ogni entità che soddisfa la query
            private void Execute(Entity entity, [EntityIndexInQuery] int sortKey, in DamageEvent damageEvent)
            {
                // Implementa la logica di gestione del danno
                // Esempi di azioni possibili:
                // - Ridurre la salute dell'entità danneggiata
                // - Applicare effetti di stato (avvelenamento, stordimento, ecc.)
                // - Creare effetti visivi per il danno
                // - Aggiornare statistiche di combattimento
                // - Verificare condizioni di morte
                
                // Se l'evento ha un target specifico e questo ha un componente di salute
                if (damageEvent.TargetEntity != Entity.Null && 
                    HealthLookup.HasComponent(damageEvent.TargetEntity))
                {
                    var health = HealthLookup[damageEvent.TargetEntity];
                    
                    // Calcola il danno effettivo considerando resistenze e moltiplicatori
                    float actualDamage = damageEvent.DamageAmount;
                    
                    // Se l'entità ha un componente di difesa, applica modificatori
                    if (DefenseLookup.HasComponent(damageEvent.TargetEntity))
                    {
                        var defense = DefenseLookup[damageEvent.TargetEntity];
                        actualDamage = CalculateDamageWithDefense(damageEvent.DamageAmount, 
                                                                 damageEvent.DamageType, 
                                                                 defense);
                    }
                    
                    // Aggiorna la salute
                    health.CurrentHealth -= actualDamage;
                    
                    // Garantisce che la salute non scenda sotto zero
                    health.CurrentHealth = math.max(0, health.CurrentHealth);
                    
                    // Aggiorna il componente
                    ECB.SetComponent(sortKey, damageEvent.TargetEntity, health);
                    
                    // Se l'entità ha raggiunto 0 salute, crea un evento di morte
                    if (health.CurrentHealth <= 0 && !health.IsDead)
                    {
                        health.IsDead = true;
                        ECB.SetComponent(sortKey, damageEvent.TargetEntity, health);
                        
                        // Crea un evento di morte
                        Entity deathEvent = ECB.CreateEntity(sortKey);
                        ECB.AddComponent(sortKey, deathEvent, new DeathEvent 
                        { 
                            DeadEntity = damageEvent.TargetEntity,
                            KillerEntity = damageEvent.SourceEntity
                        });
                    }
                    
                    // Crea evento di feedback danno per effetti visivi/audio
                    Entity feedbackEvent = ECB.CreateEntity(sortKey);
                    ECB.AddComponent(sortKey, feedbackEvent, new DamageFeedbackEvent
                    {
                        TargetEntity = damageEvent.TargetEntity,
                        DamageAmount = actualDamage,
                        DamageType = damageEvent.DamageType,
                        IsCritical = damageEvent.IsCritical,
                        HitPoint = damageEvent.HitPoint
                    });
                }
                
                // Elimina l'evento dopo l'elaborazione
                ECB.DestroyEntity(sortKey, entity);
            }
            
            /// <summary>
            /// Calcola il danno effettivo considerando le resistenze della difesa
            /// </summary>
            private float CalculateDamageWithDefense(float rawDamage, byte damageType, DefenseComponent defense)
            {
                float damageMultiplier = 1.0f;
                
                // Applica resistenze in base al tipo di danno
                switch (damageType)
                {
                    case 0: // Fisico
                        damageMultiplier = 1.0f - math.clamp(defense.PhysicalResistance / 100.0f, 0.0f, 0.75f);
                        break;
                    case 1: // Elementale
                        damageMultiplier = 1.0f - math.clamp(defense.ElementalResistance / 100.0f, 0.0f, 0.75f);
                        break;
                    case 2: // Energia
                        damageMultiplier = 1.0f - math.clamp(defense.EnergyResistance / 100.0f, 0.0f, 0.75f);
                        break;
                    default:
                        damageMultiplier = 1.0f;
                        break;
                }
                
                // Calcola danno finale
                return rawDamage * damageMultiplier;
            }
        }
    }
    
    /// <summary>
    /// Evento di feedback visivo per il danno
    /// </summary>
    public struct DamageFeedbackEvent : IComponentData
    {
        public Entity TargetEntity;    // Entità che ha subito danno
        public float DamageAmount;     // Quantità di danno
        public byte DamageType;        // Tipo di danno
        public bool IsCritical;        // Se è un colpo critico
        public float3 HitPoint;        // Punto di impatto
    }
    
    /// <summary>
    /// Evento di morte di un'entità
    /// </summary>
    public struct DeathEvent : IComponentData
    {
        public Entity DeadEntity;      // Entità morta
        public Entity KillerEntity;    // Entità che ha causato la morte
    }
}
