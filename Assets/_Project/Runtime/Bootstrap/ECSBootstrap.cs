// Path: Assets/_Project/Runtime/Bootstrap/ECSBootstrap.cs
using Unity.Entities;
using UnityEngine;
using System;
using System.Collections.Generic;
using RunawayHeroes.ECS.Systems.Core;
using RunawayHeroes.ECS.Systems.Movement;
using RunawayHeroes.ECS.Systems.Movement.Group;
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
            
            // Applica la configurazione dei sistemi
            var systems = new List<Type>(); 
            
            // Aggiunge tutti i sistemi necessari
            AddCoreSystems(systems);
            AddInputSystems(systems);
            AddMovementSystems(systems);
            AddAbilitySystems(systems);
            AddCombatSystems(systems);
            AddAISystems(systems);
            AddGameplaySystems(systems);
            AddWorldSystems(systems);
            AddUISystems(systems);
            AddEventHandlers(systems);
            
            // Crea un gruppo per i sistemi principali del gioco
            var simulationSystemGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
            
            // Crea il MovementSystemGroup per organizzare i sistemi di movimento
            var movementSystemGroup = _world.CreateSystem<MovementSystemGroup>();
            simulationSystemGroup.AddSystemToUpdateList(movementSystemGroup);
            
            // Aggiunge i sistemi al gruppo appropriato
            foreach (var systemType in systems)
            {
                var system = _world.CreateSystem(systemType);
                
                // Se è un sistema di movimento, lo aggiunge al gruppo di movimento
                // altrimenti lo aggiunge al gruppo di simulazione
                var attributes = systemType.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                
                if (attributes.Length > 0)
                {
                    var updateInGroupAttr = attributes[0] as UpdateInGroupAttribute;
                    if (updateInGroupAttr != null && updateInGroupAttr.GroupType == typeof(MovementSystemGroup))
                    {
                        // Non è necessario aggiungere qui, l'attributo gestirà
                        continue;
                    }
                }
                
                simulationSystemGroup.AddSystemToUpdateList(system);
            }
            
            _initialized = true;
            Debug.Log("Inizializzazione ECS completata con successo.");
        }
        
        /// <summary>
        /// Aggiunge i sistemi core di base
        /// </summary>
        private void AddCoreSystems(List<Type> systems)
        {
            // Sistemi Core
            systems.Add(typeof(EntityLifecycleSystem));
            systems.Add(typeof(PhysicsSystem));
            systems.Add(typeof(TransformSystem));
            systems.Add(typeof(RenderSystem));
            systems.Add(typeof(CollisionSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi di input
        /// </summary>
        private void AddInputSystems(List<Type> systems)
        {
            systems.Add(typeof(InputSystem));
            systems.Add(typeof(TouchInputSystem));
            systems.Add(typeof(GestureRecognitionSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi di movimento
        /// </summary>
        private void AddMovementSystems(List<Type> systems)
        {
            // L'ordine in questa lista determinerà l'ordine di esecuzione nel gruppo
            systems.Add(typeof(PlayerMovementSystem));     // Prima esegui movimento base
            systems.Add(typeof(JumpSystem));               // Poi il salto
            systems.Add(typeof(SlideSystem));              // Poi la scivolata
            systems.Add(typeof(ObstacleCollisionSystem));  // Poi collisioni
            systems.Add(typeof(ObstacleInteractionSystem)); // Poi interazioni con ostacoli
            systems.Add(typeof(NavigationSystem));         // Poi navigazione
            systems.Add(typeof(ObstacleAvoidanceSystem));  // Poi evitamento ostacoli
        }
        
        /// <summary>
        /// Aggiunge i sistemi delle abilità
        /// </summary>
        private void AddAbilitySystems(List<Type> systems)
        {
            systems.Add(typeof(AbilitySystem));
            systems.Add(typeof(FocusTimeSystem));
            systems.Add(typeof(FragmentResonanceSystem));
            systems.Add(typeof(UrbanDashSystem));
            systems.Add(typeof(NatureCallSystem));
            systems.Add(typeof(HeatAuraSystem));
            systems.Add(typeof(FireproofBodySystem));
            systems.Add(typeof(AirBubbleSystem));
            systems.Add(typeof(ControlledGlitchSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi di combattimento
        /// </summary>
        private void AddCombatSystems(List<Type> systems)
        {
            systems.Add(typeof(HealthSystem));
            systems.Add(typeof(DamageSystem));
            systems.Add(typeof(HitboxSystem));
            systems.Add(typeof(KnockbackSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi di IA
        /// </summary>
        private void AddAISystems(List<Type> systems)
        {
            systems.Add(typeof(EnemyAISystem));
            systems.Add(typeof(PatrolSystem));
            systems.Add(typeof(AttackPatternSystem));
            systems.Add(typeof(BossPhasesSystem));
            systems.Add(typeof(PursuitSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi di gameplay
        /// </summary>
        private void AddGameplaySystems(List<Type> systems)
        {
            systems.Add(typeof(ScoreSystem));
            systems.Add(typeof(ProgressionSystem));
            systems.Add(typeof(CollectibleSystem));
            systems.Add(typeof(PowerupSystem));
            systems.Add(typeof(DifficultySystem));
            systems.Add(typeof(FocusTimeItemDetectionSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi del mondo
        /// </summary>
        private void AddWorldSystems(List<Type> systems)
        {
            systems.Add(typeof(LevelGenerationSystem));
            systems.Add(typeof(ObstacleSystem));
            systems.Add(typeof(CheckpointSystem));
            systems.Add(typeof(EnvironmentalEffectSystem));
            systems.Add(typeof(HazardSystem));
        }
        
        /// <summary>
        /// Aggiunge i sistemi UI
        /// </summary>
        private void AddUISystems(List<Type> systems)
        {
            systems.Add(typeof(FocusTimeUISystem));
            systems.Add(typeof(ResonanceUISystem));
        }
        
        /// <summary>
        /// Aggiunge i gestori di eventi
        /// </summary>
        private void AddEventHandlers(List<Type> systems)
        {
            systems.Add(typeof(GameplayEventHandler));
            systems.Add(typeof(CollisionEventHandler));
            systems.Add(typeof(DamageEventHandler));
            systems.Add(typeof(UIEventHandler));
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