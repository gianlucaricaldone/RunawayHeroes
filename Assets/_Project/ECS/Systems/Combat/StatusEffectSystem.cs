using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Enemies;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events;

namespace RunawayHeroes.ECS.Systems.Combat
{
    /// <summary>
    /// Sistema che gestisce gli effetti di stato sulle entità.
    /// Si occupa di applicare, monitorare e rimuovere effetti come stordimento,
    /// paralisi, vulnerabilità e altri effetti temporanei.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct StatusEffectSystem : ISystem
    {
        #region Fields
        // Query per le entità stordite
        private EntityQuery _stunnedEntitiesQuery;
        
        // Query per gli eventi di stordimento
        private EntityQuery _stunEventQuery;
        #endregion
        
        #region Lifecycle
        /// <summary>
        /// Inizializza il sistema di effetti di stato
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per le entità stordite
            _stunnedEntitiesQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StunnedTag>()
                .Build(ref state);
                
            // Configura la query per gli eventi di stordimento
            _stunEventQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StunEvent>()
                .Build(ref state);
                
            // Richiedi il command buffer per le modifiche strutturali
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il sistema viene distrutto
        /// </summary>
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        #endregion
        
        #region Update
        /// <summary>
        /// Aggiorna gli effetti di stato su tutte le entità
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // 1. Processa gli eventi di stordimento
            if (!_stunEventQuery.IsEmpty)
            {
                state.Dependency = new ProcessStunEventsJob
                {
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_stunEventQuery, state.Dependency);
            }
            
            // 2. Aggiorna e rimuovi gli effetti di stordimento scaduti
            if (!_stunnedEntitiesQuery.IsEmpty)
            {
                state.Dependency = new UpdateStunEffectsJob
                {
                    DeltaTime = deltaTime,
                    ECB = commandBuffer.AsParallelWriter()
                }.ScheduleParallel(_stunnedEntitiesQuery, state.Dependency);
            }
        }
        #endregion
        
        #region Jobs
        /// <summary>
        /// Job che processa gli eventi di stordimento
        /// </summary>
        [BurstCompile]
        private partial struct ProcessStunEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, 
                                 Entity entity, 
                                 in StunEvent stunEvent)
            {
                // Verifica se l'entità target esiste
                if (SystemAPI.Exists(stunEvent.TargetEntity))
                {
                    // Aggiungi il tag di stordimento
                    if (!SystemAPI.HasComponent<StunnedTag>(stunEvent.TargetEntity))
                    {
                        ECB.AddComponent<StunnedTag>(sortKey, stunEvent.TargetEntity);
                    }
                    
                    // Aggiungi o aggiorna il componente durata stordimento
                    if (SystemAPI.HasComponent<StunDurationComponent>(stunEvent.TargetEntity))
                    {
                        var duration = SystemAPI.GetComponent<StunDurationComponent>(stunEvent.TargetEntity);
                        
                        // Prendi la durata maggiore tra quella corrente e quella nuova
                        if (stunEvent.Duration > duration.RemainingTime)
                        {
                            duration.RemainingTime = stunEvent.Duration;
                            duration.OriginalDuration = stunEvent.Duration;
                            ECB.SetComponent(sortKey, stunEvent.TargetEntity, duration);
                        }
                    }
                    else
                    {
                        ECB.AddComponent(sortKey, stunEvent.TargetEntity, new StunDurationComponent
                        {
                            RemainingTime = stunEvent.Duration,
                            OriginalDuration = stunEvent.Duration
                        });
                    }
                }
                
                // Distruggi l'evento di stordimento
                ECB.DestroyEntity(sortKey, entity);
            }
        }
        
        /// <summary>
        /// Job che aggiorna lo stato di stordimento
        /// </summary>
        [BurstCompile]
        private partial struct UpdateStunEffectsJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [BurstCompile]
            private void Execute([EntityIndexInQuery] int sortKey, 
                                 Entity entity, 
                                 ref StunDurationComponent stunDuration)
            {
                // Aggiorna il tempo rimanente
                stunDuration.RemainingTime -= DeltaTime;
                
                // Se lo stordimento è terminato, rimuovi il tag e il componente durata
                if (stunDuration.RemainingTime <= 0)
                {
                    ECB.RemoveComponent<StunnedTag>(sortKey, entity);
                    ECB.RemoveComponent<StunDurationComponent>(sortKey, entity);
                }
            }
        }
        #endregion
    }
    
    #region Componenti e Eventi StatusEffect
    
    /// <summary>
    /// Componente che tiene traccia della durata rimanente di uno stordimento
    /// </summary>
    [System.Serializable]
    public struct StunDurationComponent : IComponentData
    {
        public float RemainingTime;     // Tempo rimanente dello stordimento
        public float OriginalDuration;  // Durata originale dello stordimento
    }
    
    /// <summary>
    /// Evento per stordire un'entità
    /// </summary>
    [System.Serializable]
    public struct StunEvent : IComponentData
    {
        public Entity TargetEntity;     // Entità da stordire
        public float Duration;          // Durata dello stordimento in secondi
        public StunType Type;           // Tipo di stordimento
    }
    
    /// <summary>
    /// Tipi di stordimento disponibili
    /// </summary>
    public enum StunType : byte
    {
        Light = 0,       // Stordimento leggero
        Medium = 1,      // Stordimento medio
        Heavy = 2,       // Stordimento pesante
        Freeze = 3,      // Congelamento
        Shock = 4,       // Shock elettrico
        Override = 5     // Override sistema (solo per nemici virtuali)
    }
    #endregion
}