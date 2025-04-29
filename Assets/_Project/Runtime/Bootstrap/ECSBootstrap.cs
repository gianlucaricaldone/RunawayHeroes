using Unity.Entities;
using UnityEngine;
using RunawayHeroes.ECS.Systems.Core;
using RunawayHeroes.ECS.Systems.Movement;
using RunawayHeroes.ECS.Systems.Abilities;
using RunawayHeroes.ECS.Systems.Combat;
using RunawayHeroes.ECS.Systems.Gameplay;
using RunawayHeroes.ECS.Systems.AI;
using RunawayHeroes.ECS.Systems.World;
using RunawayHeroes.ECS.Systems.UI;
using RunawayHeroes.ECS.Systems.Input;
using RunawayHeroes.ECS.Events.Handlers;

namespace RunawayHeroes.Runtime.Bootstrap
{
    /// <summary>
    /// Sistema di bootstrap per l'inizializzazione dell'Entity Component System.
    /// Questo componente crea e registra tutti i sistemi ECS necessari per il gioco.
    /// </summary>
    [DisableAutoCreation]
    public class ECSBootstrap : MonoBehaviour
    {
        /// <summary>
        /// Riferimento al World ECS principale
        /// </summary>
        private World _world;
        
        /// <summary>
        /// Flag che indica se l'inizializzazione è stata completata
        /// </summary>
        private bool _initialized = false;
        
        /// <summary>
        /// Inizializza il sistema ECS all'avvio del gioco
        /// </summary>
        private void Start()
        {
            if (!_initialized)
            {
                InitializeECS();
            }
        }
        
        /// <summary>
        /// Crea e registra tutti i sistemi ECS necessari
        /// </summary>
        private void InitializeECS()
        {
            Debug.Log("Inizializzazione del sistema ECS...");
            
            // Ottiene o crea il mondo predefinito
            _world = World.DefaultGameObjectInjectionWorld;
            
            // Registra i sistemi core
            RegisterCoreSystems();
            
            // Registra i sistemi di input
            RegisterInputSystems();
            
            // Registra i sistemi di movimento
            RegisterMovementSystems();
            
            // Registra i sistemi delle abilità
            RegisterAbilitySystems();
            
            // Registra i sistemi di combattimento
            RegisterCombatSystems();
            
            // Registra i sistemi di IA
            RegisterAISystems();
            
            // Registra i sistemi di gameplay
            RegisterGameplaySystems();
            
            // Registra i sistemi del mondo
            RegisterWorldSystems();
            
            // Registra i sistemi UI
            RegisterUISystems();
            
            // Registra i gestori eventi
            RegisterEventHandlers();
            
            _initialized = true;
            Debug.Log("Inizializzazione ECS completata con successo.");
        }
        
        /// <summary>
        /// Registra i sistemi core di base
        /// </summary>
        private void RegisterCoreSystems()
        {
            _world.GetOrCreateSystemManaged<EntityLifecycleSystem>();
            _world.GetOrCreateSystemManaged<TransformSystem>();
            _world.GetOrCreateSystemManaged<PhysicsSystem>();
            _world.GetOrCreateSystemManaged<RenderSystem>();
            _world.GetOrCreateSystemManaged<CollisionSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi di input
        /// </summary>
        private void RegisterInputSystems()
        {
            _world.GetOrCreateSystemManaged<InputSystem>();
            _world.GetOrCreateSystemManaged<TouchInputSystem>();
            _world.GetOrCreateSystemManaged<GestureRecognitionSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi di movimento
        /// </summary>
        private void RegisterMovementSystems()
        {
            _world.GetOrCreateSystemManaged<PlayerMovementSystem>();
            _world.GetOrCreateSystemManaged<JumpSystem>();
            _world.GetOrCreateSystemManaged<SlideSystem>();
            _world.GetOrCreateSystemManaged<ObstacleCollisionSystem>();
            _world.GetOrCreateSystemManaged<ObstacleInteractionSystem>(); // Nuovo sistema di interazione con ostacoli
            _world.GetOrCreateSystemManaged<NavigationSystem>();
            _world.GetOrCreateSystemManaged<ObstacleAvoidanceSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi delle abilità
        /// </summary>
        private void RegisterAbilitySystems()
        {
            _world.GetOrCreateSystemManaged<AbilitySystem>();
            _world.GetOrCreateSystemManaged<FocusTimeSystem>();
            _world.GetOrCreateSystemManaged<FragmentResonanceSystem>();
            _world.GetOrCreateSystemManaged<UrbanDashSystem>();
            _world.GetOrCreateSystemManaged<NatureCallSystem>();
            _world.GetOrCreateSystemManaged<HeatAuraSystem>();
            _world.GetOrCreateSystemManaged<FireproofBodySystem>();
            _world.GetOrCreateSystemManaged<AirBubbleSystem>();
            _world.GetOrCreateSystemManaged<ControlledGlitchSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi di combattimento
        /// </summary>
        private void RegisterCombatSystems()
        {
            _world.GetOrCreateSystemManaged<HealthSystem>();
            _world.GetOrCreateSystemManaged<DamageSystem>();
            _world.GetOrCreateSystemManaged<HitboxSystem>();
            _world.GetOrCreateSystemManaged<KnockbackSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi di IA
        /// </summary>
        private void RegisterAISystems()
        {
            _world.GetOrCreateSystemManaged<EnemyAISystem>();
            _world.GetOrCreateSystemManaged<PatrolSystem>();
            _world.GetOrCreateSystemManaged<AttackPatternSystem>();
            _world.GetOrCreateSystemManaged<BossPhasesSystem>();
            _world.GetOrCreateSystemManaged<PursuitSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi di gameplay
        /// </summary>
        private void RegisterGameplaySystems()
        {
            _world.GetOrCreateSystemManaged<ScoreSystem>();
            _world.GetOrCreateSystemManaged<ProgressionSystem>();
            _world.GetOrCreateSystemManaged<CollectibleSystem>();
            _world.GetOrCreateSystemManaged<PowerupSystem>();
            _world.GetOrCreateSystemManaged<DifficultySystem>();
            _world.GetOrCreateSystemManaged<FocusTimeItemDetectionSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi del mondo
        /// </summary>
        private void RegisterWorldSystems()
        {
            _world.GetOrCreateSystemManaged<LevelGenerationSystem>();
            _world.GetOrCreateSystemManaged<ObstacleSystem>();
            _world.GetOrCreateSystemManaged<CheckpointSystem>();
            _world.GetOrCreateSystemManaged<EnvironmentalEffectSystem>();
            _world.GetOrCreateSystemManaged<HazardSystem>();
        }
        
        /// <summary>
        /// Registra i sistemi UI
        /// </summary>
        private void RegisterUISystems()
        {
            _world.GetOrCreateSystemManaged<FocusTimeUISystem>();
            _world.GetOrCreateSystemManaged<ResonanceUISystem>();
        }
        
        /// <summary>
        /// Registra i gestori di eventi
        /// </summary>
        private void RegisterEventHandlers()
        {
            _world.GetOrCreateSystemManaged<GameplayEventHandler>();
            _world.GetOrCreateSystemManaged<CollisionEventHandler>();
            _world.GetOrCreateSystemManaged<DamageEventHandler>();
            _world.GetOrCreateSystemManaged<UIEventHandler>();
        }
        
        /// <summary>
        /// Pulisce le risorse quando il componente viene distrutto
        /// </summary>
        private void OnDestroy()
        {
            if (_initialized && _world != null && _world.IsCreated)
            {
                // La pulizia del World e dei sistemi viene gestita automaticamente
                Debug.Log("Pulizia del sistema ECS completata.");
            }
        }
    }
}