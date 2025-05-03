using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema che gestisce la simulazione fisica di base per tutte le entità con componenti fisici.
    /// Si occupa di applicare gravità, aggiornare velocità e posizioni e gestire collisioni semplici.
    /// </summary>
    [BurstCompile]
    public partial struct PhysicsSystem : ISystem
    {
        private EntityQuery _physicsQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query per le entità
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Definisce le entità da processare: quelle con TransformComponent e PhysicsComponent
            _physicsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<TransformComponent, PhysicsComponent>()
                .Build(ref state);
            
            // Assicura che questo sistema venga eseguito solo se ci sono entità da elaborare
            state.RequireForUpdate(_physicsQuery);
        }
        
        /// <summary>
        /// Pulizia delle risorse quando il sistema viene distrutto
        /// </summary>
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            // Nessuna risorsa da pulire
        }
        
        /// <summary>
        /// Aggiorna la simulazione fisica per tutte le entità rilevanti ad ogni frame
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Ottiene il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            
            // Applica la simulazione fisica a tutte le entità corrispondenti usando IJobEntity
            state.Dependency = new PhysicsSimulationJob
            {
                DeltaTime = deltaTime
            }.ScheduleParallel(_physicsQuery, state.Dependency);
        }
    }
    
    /// <summary>
    /// Job che esegue la simulazione fisica per ciascuna entità
    /// </summary>
    [BurstCompile]
    public partial struct PhysicsSimulationJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref TransformComponent transform, ref PhysicsComponent physics)
        {
            // Applica gravità se necessario
            if (physics.UseGravity && !physics.IsGrounded)
            {
                physics.Acceleration.y -= physics.Gravity;
            }
            
            // Aggiorna la velocità in base all'accelerazione
            physics.Velocity += physics.Acceleration * DeltaTime;
            
            // Applica attrito (solo se a terra)
            if (physics.IsGrounded)
            {
                float frictionFactor = 1.0f - math.min(1.0f, physics.Friction * DeltaTime);
                physics.Velocity.x *= frictionFactor;
                physics.Velocity.z *= frictionFactor;
            }
            
            // Aggiorna la posizione in base alla velocità
            transform.Position += physics.Velocity * DeltaTime;
            
            // Controllo temporaneo del terreno (da sostituire con un sistema di collisione più avanzato)
            if (transform.Position.y <= 0.0f && physics.Velocity.y < 0)
            {
                transform.Position.y = 0.0f;
                physics.Velocity.y = 0.0f;
                physics.IsGrounded = true;
            }
            else if (physics.Velocity.y > 0)
            {
                physics.IsGrounded = false;
            }
            
            // Resetta l'accelerazione (le forze vengono riapplicate ogni frame)
            physics.Acceleration = float3.zero;
            
            // Se usciamo dai limiti dell'area di gioco, riporta il giocatore dentro (temporaneo)
            // Questo verrà sostituito da un sistema di collisione più avanzato
            float bounds = 50.0f;
            transform.Position.x = math.clamp(transform.Position.x, -bounds, bounds);
            transform.Position.z = math.clamp(transform.Position.z, -bounds, bounds);
        }
    }
}