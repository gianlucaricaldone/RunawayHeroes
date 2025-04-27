using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.ECS.Systems.Core
{
    /// <summary>
    /// Sistema che gestisce la simulazione fisica di base per tutte le entità con componenti fisici.
    /// Si occupa di applicare gravità, aggiornare velocità e posizioni e gestire collisioni semplici.
    /// </summary>
    public partial class PhysicsSystem : SystemBase
    {
        private EntityQuery _physicsQuery;
        
        /// <summary>
        /// Inizializza il sistema e definisce le query per le entità
        /// </summary>
        protected override void OnCreate()
        {
            // Definisce le entità da processare: quelle con TransformComponent e PhysicsComponent
            _physicsQuery = GetEntityQuery(
                ComponentType.ReadWrite<TransformComponent>(),
                ComponentType.ReadWrite<PhysicsComponent>()
            );
            
            // Assicura che questo sistema venga eseguito anche se non ci sono entità da elaborare
            RequireForUpdate(_physicsQuery);
        }
        
        /// <summary>
        /// Aggiorna la simulazione fisica per tutte le entità rilevanti ad ogni frame
        /// </summary>
        protected override void OnUpdate()
        {
            // Ottiene il delta time per questo frame
            float deltaTime = Time.DeltaTime;
            
            // Applica la simulazione fisica a tutte le entità corrispondenti
            Entities
                .WithName("PhysicsSimulation")
                .ForEach((ref TransformComponent transform, ref PhysicsComponent physics) => 
                {
                    // Applica gravità se necessario
                    if (physics.UseGravity && !physics.IsGrounded)
                    {
                        physics.Acceleration.y -= physics.Gravity;
                    }
                    
                    // Aggiorna la velocità in base all'accelerazione
                    physics.Velocity += physics.Acceleration * deltaTime;
                    
                    // Applica attrito (solo se a terra)
                    if (physics.IsGrounded)
                    {
                        float frictionFactor = 1.0f - math.min(1.0f, physics.Friction * deltaTime);
                        physics.Velocity.x *= frictionFactor;
                        physics.Velocity.z *= frictionFactor;
                    }
                    
                    // Aggiorna la posizione in base alla velocità
                    transform.Position += physics.Velocity * deltaTime;
                    
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
                    
                }).ScheduleParallel();
        }
    }
}