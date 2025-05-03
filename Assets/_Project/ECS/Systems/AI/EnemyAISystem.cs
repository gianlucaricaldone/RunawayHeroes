using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Events.EventDefinitions;
using RunawayHeroes.ECS.Systems.AI;
using RunawayHeroes.ECS.Systems.Movement.Group;

namespace RunawayHeroes.ECS.Systems.AI
{
    /// <summary>
    /// Sistema principale che coordina l'intelligenza artificiale dei nemici.
    /// Gestisce il processo decisionale degli agenti nemici, la selezione degli
    /// stati di comportamento, e le transizioni tra gli stati.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(MovementSystemGroup))]
    public partial struct EnemyAISystem : ISystem
    {
        // Query per gli agenti IA
        private EntityQuery _aiAgentsQuery;
        
        // Query per le entità giocatore
        private EntityQuery _playerQuery;
        
        /// <summary>
        /// Inizializza il sistema di IA nemica
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            // Configura la query per gli agenti IA
            _aiAgentsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<EnemyAIComponent, TransformComponent>()
                .Build(ref state);
                
            // Configura la query per i giocatori (target potenziali)
            _playerQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PlayerTag, TransformComponent>()
                .Build(ref state);
                
            // Richiedi che ci siano entità con IA e giocatori per l'aggiornamento
            state.RequireForUpdate(_aiAgentsQuery);
            
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
        /// Aggiorna l'IA di tutti i nemici ad ogni frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            // Nota: Poiché alcune operazioni di AI richiedono logiche complesse e potrebbero
            // beneficiare di decisioni euristiche o non adatte a Burst, non utilizziamo
            // attributo [BurstCompile] su OnUpdate ma lo usiamo sui job di elaborazione specializzati.
            
            // Ottieni il delta time per questo frame
            float deltaTime = SystemAPI.Time.DeltaTime;
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Ottieni il buffer per i comandi
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            
            // Raccogli tutte le posizioni dei giocatori
            NativeArray<float3> playerPositions;
            
            if (!_playerQuery.IsEmpty)
            {
                var playerTransforms = _playerQuery.ToComponentDataArray<TransformComponent>(Allocator.Temp);
                playerPositions = new NativeArray<float3>(playerTransforms.Length, Allocator.TempJob);
                
                for (int i = 0; i < playerTransforms.Length; i++)
                {
                    playerPositions[i] = playerTransforms[i].Position;
                }
                
                playerTransforms.Dispose();
            }
            else
            {
                playerPositions = new NativeArray<float3>(0, Allocator.TempJob);
            }
            
            // Processa tutti gli agenti IA
            // Utilizziamo SystemAPI.Query per maggiore flessibilità
            foreach (var (entity, ai, transform, physics) in 
                    SystemAPI.Query<EntityRef<Entity>, RefRW<EnemyAIComponent>, RefRO<TransformComponent>, RefRW<PhysicsComponent>>()
                    .WithAll<EnemyAIComponent>())
            {
                // Aggiorna i timer di IA
                UpdateAITimers(ref ai.ValueRW, deltaTime);
                
                // Calcola la distanza dal giocatore più vicino
                float distanceToNearestPlayer = float.MaxValue;
                float3 nearestPlayerPosition = float3.zero;
                
                if (playerPositions.Length > 0)
                {
                    for (int i = 0; i < playerPositions.Length; i++)
                    {
                        float distance = math.distance(transform.ValueRO.Position, playerPositions[i]);
                        if (distance < distanceToNearestPlayer)
                        {
                            distanceToNearestPlayer = distance;
                            nearestPlayerPosition = playerPositions[i];
                        }
                    }
                }
                
                // Controlla se c'è un cambio di stato necessario
                AIState nextState = DetermineNextState(
                    ai.ValueRO, 
                    distanceToNearestPlayer,
                    transform.ValueRO.Position,
                    nearestPlayerPosition
                );
                
                // Se è necessario cambiare stato
                if (nextState != ai.ValueRO.CurrentState)
                {
                    // Gestisci l'uscita dallo stato corrente
                    ExitState(entity.Value, ai.ValueRO.CurrentState, ref state, commandBuffer);
                    
                    // Aggiorna lo stato
                    ai.ValueRW.CurrentState = nextState;
                    ai.ValueRW.StateEnterTime = elapsedTime;
                    
                    // Gestisci l'entrata nel nuovo stato
                    EnterState(entity.Value, nextState, ref state, commandBuffer, nearestPlayerPosition);
                }
                
                // Aggiorna il comportamento in base allo stato corrente
                UpdateState(
                    entity.Value, 
                    ai.ValueRO, 
                    transform.ValueRO.Position,
                    ref physics.ValueRW,
                    nearestPlayerPosition, 
                    distanceToNearestPlayer,
                    deltaTime,
                    ref state,
                    commandBuffer
                );
            }
            
            // Pulisci le risorse allocate
            playerPositions.Dispose();
        }
        
        /// <summary>
        /// Aggiorna i timer dell'IA
        /// </summary>
        private void UpdateAITimers(ref EnemyAIComponent ai, float deltaTime)
        {
            // Aggiorna i timer di recupero delle abilità
            if (ai.AbilityCooldown > 0)
            {
                ai.AbilityCooldown -= deltaTime;
                if (ai.AbilityCooldown < 0) ai.AbilityCooldown = 0;
            }
            
            // Aggiorna i timer di attesa generici
            if (ai.WaitTimer > 0)
            {
                ai.WaitTimer -= deltaTime;
                if (ai.WaitTimer < 0) ai.WaitTimer = 0;
            }
        }
        
        /// <summary>
        /// Determina il prossimo stato dell'IA in base a vari fattori
        /// </summary>
        private AIState DetermineNextState(
            EnemyAIComponent ai, 
            float distanceToPlayer,
            float3 position,
            float3 playerPosition)
        {
            // Gestisci gli stati speciali che hanno priorità
            if (ai.Health <= ai.FleeHealthThreshold && ai.CanFlee)
            {
                return AIState.Fleeing;
            }
            
            // Controlla se il giocatore è alla vista
            bool playerVisible = distanceToPlayer < ai.DetectionRange;
            
            // Transizioni di stato in base alla distanza e visibilità del giocatore
            switch (ai.CurrentState)
            {
                case AIState.Idle:
                    if (playerVisible)
                    {
                        return distanceToPlayer <= ai.AttackRange ? AIState.Attacking : AIState.Pursuing;
                    }
                    else if (ai.CanPatrol && ai.WaitTimer <= 0)
                    {
                        return AIState.Patrolling;
                    }
                    break;
                    
                case AIState.Patrolling:
                    if (playerVisible)
                    {
                        return distanceToPlayer <= ai.AttackRange ? AIState.Attacking : AIState.Pursuing;
                    }
                    // Continua a pattugliare
                    break;
                    
                case AIState.Pursuing:
                    if (distanceToPlayer <= ai.AttackRange)
                    {
                        return AIState.Attacking;
                    }
                    else if (!playerVisible && ai.LoseTargetTimer <= 0)
                    {
                        return ai.CanPatrol ? AIState.Patrolling : AIState.Idle;
                    }
                    break;
                    
                case AIState.Attacking:
                    if (distanceToPlayer > ai.AttackRange * 1.2f) // Buffer per evitare oscillazioni
                    {
                        return AIState.Pursuing;
                    }
                    break;
                    
                case AIState.Fleeing:
                    if (ai.Health > ai.FleeHealthThreshold * 1.5f) // Recuperato abbastanza salute
                    {
                        return playerVisible ? AIState.Pursuing : AIState.Idle;
                    }
                    break;
                    
                case AIState.Stunned:
                    if (ai.StunTime <= 0)
                    {
                        return playerVisible ? AIState.Pursuing : AIState.Idle;
                    }
                    break;
            }
            
            // Nessun cambio di stato necessario
            return ai.CurrentState;
        }
        
        /// <summary>
        /// Gestisce l'uscita da uno stato
        /// </summary>
        private void ExitState(Entity entity, AIState state, ref SystemState systemState, EntityCommandBuffer ecb)
        {
            // Implementazione specifica per ogni stato
            switch (state)
            {
                case AIState.Attacking:
                    // Annulla eventuali animazioni di attacco in corso
                    break;
                    
                case AIState.Pursuing:
                    // Annulla eventuali path following
                    break;
                    
                case AIState.Fleeing:
                    // Resetta l'obiettivo di fuga
                    break;
            }
            
            // Crea un evento di cambio stato
            var exitEvent = ecb.CreateEntity();
            ecb.AddComponent(exitEvent, new AIStateExitEvent
            {
                EntityID = entity,
                PreviousState = state
            });
        }
        
        /// <summary>
        /// Gestisce l'entrata in un nuovo stato
        /// </summary>
        private void EnterState(Entity entity, AIState state, ref SystemState systemState, EntityCommandBuffer ecb, float3 targetPosition)
        {
            // Implementazione specifica per ogni stato
            switch (state)
            {
                case AIState.Idle:
                    // Imposta animazione idle
                    break;
                    
                case AIState.Patrolling:
                    // Calcola il prossimo punto di pattugliamento
                    break;
                    
                case AIState.Pursuing:
                    // Imposta il target e inizia l'inseguimento
                    break;
                    
                case AIState.Attacking:
                    // Seleziona e prepara un attacco
                    break;
                    
                case AIState.Fleeing:
                    // Calcola la direzione di fuga
                    break;
            }
            
            // Crea un evento di cambio stato
            var enterEvent = ecb.CreateEntity();
            ecb.AddComponent(enterEvent, new AIStateEnterEvent
            {
                EntityID = entity,
                NewState = state,
                TargetPosition = targetPosition
            });
        }
        
        /// <summary>
        /// Aggiorna il comportamento in base allo stato corrente
        /// </summary>
        private void UpdateState(
            Entity entity, 
            EnemyAIComponent ai, 
            float3 position,
            ref PhysicsComponent physics,
            float3 targetPosition, 
            float distanceToTarget,
            float deltaTime,
            ref SystemState systemState,
            EntityCommandBuffer ecb)
        {
            // Implementazione del comportamento per ogni stato
            switch (ai.CurrentState)
            {
                case AIState.Idle:
                    // Comportamento di idle: piccoli movimenti casuali, animazioni, ecc.
                    physics.Velocity = float3.zero;
                    break;
                    
                case AIState.Patrolling:
                    // Implementa movimento di pattugliamento tra waypoint
                    // Questa è una versione semplificata
                    float3 patrolDirection = new float3(
                        math.sin((float)SystemAPI.Time.ElapsedTime * 0.5f),
                        0,
                        math.cos((float)SystemAPI.Time.ElapsedTime * 0.7f)
                    );
                    
                    physics.Velocity = patrolDirection * ai.MovementSpeed * 0.5f;
                    break;
                    
                case AIState.Pursuing:
                    // Calcola direzione verso il target
                    if (distanceToTarget > 0.1f)
                    {
                        float3 direction = math.normalize(targetPosition - position);
                        physics.Velocity = direction * ai.MovementSpeed;
                    }
                    break;
                    
                case AIState.Attacking:
                    // Gestisci il comportamento di attacco
                    // Fermati o muoviti lentamente verso il target
                    physics.Velocity = float3.zero;
                    
                    // Esegui attacchi se il cooldown è terminato
                    if (ai.AbilityCooldown <= 0)
                    {
                        // Crea un evento di attacco
                        var attackEvent = ecb.CreateEntity();
                        ecb.AddComponent(attackEvent, new EnemyAttackEvent
                        {
                            AttackerEntity = entity,
                            TargetPosition = targetPosition,
                            AttackType = (byte)(ai.AttackPattern % 3) // Semplice rotazione di attacchi
                        });
                        
                        // Imposta il cooldown
                        // Normalmente this sarebbe modificato direttamente in EnemyAIComponent, 
                        // ma per esempio qui utilizziamo un approccio indiretto
                    }
                    break;
                    
                case AIState.Fleeing:
                    // Calcola direzione di fuga (opposta al target)
                    if (distanceToTarget < ai.DetectionRange * 1.5f)
                    {
                        float3 fleeDirection = math.normalize(position - targetPosition);
                        physics.Velocity = fleeDirection * ai.MovementSpeed * 1.2f; // Fuga più veloce
                    }
                    break;
                    
                case AIState.Stunned:
                    // Nemico stordito, nessun movimento
                    physics.Velocity = float3.zero;
                    break;
            }
        }
    }
    
    /// <summary>
    /// Stati possibili dell'IA nemica
    /// </summary>
    public enum AIState : byte
    {
        Idle = 0,          // Nemico inattivo
        Patrolling = 1,    // Pattugliamento dell'area
        Pursuing = 2,      // Inseguimento del giocatore
        Attacking = 3,     // Attacco al giocatore
        Fleeing = 4,       // Fuga dal giocatore
        Stunned = 5        // Stordito, impossibilitato ad agire
    }
    
    /// <summary>
    /// Componente che contiene i dati di IA per un nemico
    /// </summary>
    public struct EnemyAIComponent : IComponentData
    {
        // Stato corrente
        public AIState CurrentState;     // Stato attuale dell'IA
        public float StateEnterTime;     // Momento in cui è stato attivato lo stato corrente
        
        // Parametri di comportamento
        public float DetectionRange;     // Raggio entro cui rileva il giocatore
        public float AttackRange;        // Raggio entro cui può attaccare
        public float MovementSpeed;      // Velocità di movimento
        
        // Capacità dell'IA
        public bool CanPatrol;           // Se può pattugliare quando idle
        public bool CanFlee;             // Se può fuggire quando in pericolo
        
        // Parametri di attacco
        public float AbilityCooldown;    // Tempo rimanente prima del prossimo attacco
        public int AttackPattern;        // Pattern di attacco da utilizzare
        
        // Parametri di fuga
        public float Health;             // Salute corrente
        public float FleeHealthThreshold;// Soglia di salute per la fuga
        
        // Timer vari
        public float WaitTimer;          // Timer generico di attesa
        public float LoseTargetTimer;    // Timer per perdere il target se non visibile
        public float StunTime;           // Tempo rimanente di stordimento
    }
    
    /// <summary>
    /// Evento generato quando un nemico esce da uno stato AI
    /// </summary>
    public struct AIStateExitEvent : IComponentData
    {
        public Entity EntityID;         // Entità che ha cambiato stato
        public AIState PreviousState;   // Stato precedente
    }
    
    /// <summary>
    /// Evento generato quando un nemico entra in un nuovo stato AI
    /// </summary>
    public struct AIStateEnterEvent : IComponentData
    {
        public Entity EntityID;         // Entità che ha cambiato stato
        public AIState NewState;        // Nuovo stato
        public float3 TargetPosition;   // Posizione target (se applicabile)
    }
    
    /// <summary>
    /// Evento generato quando un nemico esegue un attacco
    /// </summary>
    public struct EnemyAttackEvent : IComponentData
    {
        public Entity AttackerEntity;    // Entità che attacca
        public float3 TargetPosition;    // Posizione target dell'attacco
        public byte AttackType;          // Tipo di attacco da eseguire
    }
}
