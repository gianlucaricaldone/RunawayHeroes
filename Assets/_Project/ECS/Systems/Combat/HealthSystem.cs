using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Sistema che gestisce la salute delle entità, il recupero della salute,
    /// la gestione delle invulnerabilità temporanee e lo stato di morte.
    /// </summary>
    [BurstCompile]
    public partial struct HealthSystem : ISystem
    {
        private EntityQuery _healthEntitiesQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Query per entità con componente salute
            _healthEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<HealthComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità con salute per l'esecuzione
            state.RequireForUpdate(_healthEntitiesQuery);
            
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
            
            // Ottieni delta time da utilizzare nel job
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Processa le entità con salute
            new ProcessHealthJob 
            { 
                DeltaTime = deltaTime,
                ECB = commandBuffer.AsParallelWriter()
            }.ScheduleParallel(_healthEntitiesQuery, state.Dependency).Complete();
        }
    }
    
    /// <summary>
    /// Job per gestire il componente salute delle entità
    /// </summary>
    [BurstCompile]
    public partial struct ProcessHealthJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        public void Execute(Entity entity, [ChunkIndexInQuery] int entityInQueryIndex, ref HealthComponent health)
        {
            // Gestione invulnerabilità temporanea
            if (health.InvulnerabilityTimer > 0)
            {
                health.InvulnerabilityTimer -= DeltaTime;
                
                if (health.InvulnerabilityTimer <= 0)
                {
                    health.InvulnerabilityTimer = 0;
                    health.IsInvulnerable = false;
                }
            }
            
            // Rigenerazione automatica della salute (se abilitata)
            if (health.HasAutoRegen && health.CurrentHealth < health.MaxHealth && health.CurrentHealth > 0)
            {
                // Controlla se è trascorso il tempo di attesa per rigenerazione
                if (health.TimeSinceLastDamage >= health.RegenDelay)
                {
                    // Applica rigenerazione
                    float regenAmount = health.RegenRate * DeltaTime;
                    health.CurrentHealth = math.min(health.CurrentHealth + regenAmount, health.MaxHealth);
                    
                    // Se la rigenerazione è significativa, genera evento
                    if (regenAmount >= 1.0f)
                    {
                        var regenEntity = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, regenEntity, new HealthRegeneratedEvent
                        {
                            TargetEntity = entity,
                            RegenAmount = regenAmount,
                            CurrentHealth = health.CurrentHealth,
                            CurrentShield = health.CurrentShield
                        });
                    }
                }
                else
                {
                    // Aggiorna il timer dall'ultimo danno
                    health.TimeSinceLastDamage += DeltaTime;
                }
            }
            
            // Rigenerazione automatica dello scudo (se presente e abilitata)
            if (health.HasShield && health.HasShieldAutoRegen && health.CurrentShield < health.MaxShield)
            {
                // Controlla se è trascorso il tempo di attesa per rigenerazione scudo
                if (health.TimeSinceLastShieldDamage >= health.ShieldRegenDelay)
                {
                    // Applica rigenerazione scudo
                    float shieldRegenAmount = health.ShieldRegenRate * DeltaTime;
                    health.CurrentShield = math.min(health.CurrentShield + shieldRegenAmount, health.MaxShield);
                    
                    // Se la rigenerazione è significativa, genera evento
                    if (shieldRegenAmount >= 1.0f)
                    {
                        var shieldRegenEntity = ECB.CreateEntity(entityInQueryIndex);
                        ECB.AddComponent(entityInQueryIndex, shieldRegenEntity, new ShieldRegeneratedEvent
                        {
                            TargetEntity = entity,
                            RegenAmount = shieldRegenAmount,
                            CurrentHealth = health.CurrentHealth,
                            CurrentShield = health.CurrentShield
                        });
                    }
                }
                else
                {
                    // Aggiorna il timer dall'ultimo danno allo scudo
                    health.TimeSinceLastShieldDamage += DeltaTime;
                }
            }
            
            // Controlla condizioni di morte (non fa nulla qui, gli eventi di morte sono generati dal DamageSystem)
        }
    }
    
    /// <summary>
    /// Evento generato quando un'entità rigenera salute
    /// </summary>
    public struct HealthRegeneratedEvent : IComponentData
    {
        public Entity TargetEntity;     // Entità che ha rigenerato salute
        public float RegenAmount;       // Quantità di salute rigenerata
        public float CurrentHealth;     // Salute attuale dopo rigenerazione
        public float CurrentShield;     // Scudo attuale
    }
    
    /// <summary>
    /// Evento generato quando un'entità rigenera scudo
    /// </summary>
    public struct ShieldRegeneratedEvent : IComponentData
    {
        public Entity TargetEntity;     // Entità che ha rigenerato scudo
        public float RegenAmount;       // Quantità di scudo rigenerato
        public float CurrentHealth;     // Salute attuale
        public float CurrentShield;     // Scudo attuale dopo rigenerazione
    }
}
