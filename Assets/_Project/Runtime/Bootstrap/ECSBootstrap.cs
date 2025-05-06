// Path: Assets/_Project/Runtime/Bootstrap/ECSBootstrap.cs
using Unity.Entities;
using UnityEngine;
using System;
using System.Collections.Generic;
using RunawayHeroes.ECS.Core;
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
            try
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
                
                // Otteniamo i gruppi di sistema esistenti
                var simulationSystemGroup = _world.GetOrCreateSystemManaged<SimulationSystemGroup>();
                
                // Crea i gruppi di sistema necessari
                var transformSystemGroup = _world.CreateSystemManaged<RunawayHeroes.ECS.Core.TransformSystemGroup>();
                simulationSystemGroup.AddSystemToUpdateList(transformSystemGroup);
                
                var movementSystemGroup = _world.CreateSystemManaged<RunawayHeroes.ECS.Core.MovementSystemGroup>();
                transformSystemGroup.AddSystemToUpdateList(movementSystemGroup);
                
                // Metodo sicuro per aggiungere i sistemi
                AddSystemsSafely(systems, simulationSystemGroup);
                
                _initialized = true;
                Debug.Log("Inizializzazione ECS completata con successo.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Errore durante l'inizializzazione ECS: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Aggiunge i sistemi in modo sicuro, evitando dipendenze circolari
        /// </summary>
        private void AddSystemsSafely(List<Type> systems, SimulationSystemGroup simulationSystemGroup)
        {
            // Manteniamo un elenco di sistemi già aggiunti per evitare duplicati
            HashSet<Type> addedSystems = new HashSet<Type>();
            
            foreach (var systemType in systems)
            {
                if (addedSystems.Contains(systemType))
                    continue;
                
                try
                {
                    // Crea il sistema
                    ComponentSystemBase system = _world.GetOrCreateSystemManaged(systemType);
                    
                    // Verifica a quale gruppo appartiene il sistema
                    bool isInCustomGroup = false;
                    var attributes = systemType.GetCustomAttributes(typeof(UpdateInGroupAttribute), true);
                    
                    foreach (var attr in attributes)
                    {
                        if (attr is UpdateInGroupAttribute updateInGroup)
                        {
                            Type groupType = updateInGroup.GroupType;
                            
                            if (groupType == typeof(RunawayHeroes.ECS.Core.MovementSystemGroup) || 
                                groupType == typeof(RunawayHeroes.ECS.Systems.Movement.Group.MovementSystemGroup))
                            {
                                var movementGroup = _world.GetExistingSystemManaged<RunawayHeroes.ECS.Core.MovementSystemGroup>();
                                movementGroup.AddSystemToUpdateList(system);
                                isInCustomGroup = true;
                                break;
                            }
                            else if (groupType == typeof(RunawayHeroes.ECS.Core.TransformSystemGroup))
                            {
                                var transformGroup = _world.GetExistingSystemManaged<RunawayHeroes.ECS.Core.TransformSystemGroup>();
                                transformGroup.AddSystemToUpdateList(system);
                                isInCustomGroup = true;
                                break;
                            }
                        }
                    }
                    
                    // Aggiunge il sistema al gruppo di simulazione di default se non è in nessun gruppo personalizzato
                    if (!isInCustomGroup)
                    {
                        simulationSystemGroup.AddSystemToUpdateList(system);
                    }
                    
                    addedSystems.Add(systemType);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Impossibile creare il sistema {systemType.Name}: {e.Message}");
                }
            }
        }
        
        // I metodi AddXXXSystems rimangono invariati
        private void AddCoreSystems(List<Type> systems)
        {
            // Sistemi Core
            systems.Add(typeof(EntityLifecycleSystem));
            systems.Add(typeof(PhysicsSystem));
            systems.Add(typeof(TransformSystem));
            systems.Add(typeof(RenderSystem));
            systems.Add(typeof(CollisionSystem));
        }
        
        private void AddInputSystems(List<Type> systems)
        {
            systems.Add(typeof(InputSystem));
            systems.Add(typeof(TouchInputSystem));
            systems.Add(typeof(GestureRecognitionSystem));
        }
        
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
        
        private void AddCombatSystems(List<Type> systems)
        {
            systems.Add(typeof(HealthSystem));
            systems.Add(typeof(DamageSystem));
            systems.Add(typeof(HitboxSystem));
            systems.Add(typeof(KnockbackSystem));
        }
        
        private void AddAISystems(List<Type> systems)
        {
            systems.Add(typeof(EnemyAISystem));
            systems.Add(typeof(PatrolSystem));
            systems.Add(typeof(AttackPatternSystem));
            systems.Add(typeof(BossPhasesSystem));
            systems.Add(typeof(PursuitSystem));
        }
        
        private void AddGameplaySystems(List<Type> systems)
        {
            systems.Add(typeof(ScoreSystem));
            systems.Add(typeof(ProgressionSystem));
            systems.Add(typeof(CollectibleSystem));
            systems.Add(typeof(PowerupSystem));
            systems.Add(typeof(DifficultySystem));
            systems.Add(typeof(FocusTimeItemDetectionSystem));
        }
        
        private void AddWorldSystems(List<Type> systems)
        {
            systems.Add(typeof(LevelGenerationSystem));
            systems.Add(typeof(ObstacleSystem));
            systems.Add(typeof(CheckpointSystem));
            systems.Add(typeof(EnvironmentalEffectSystem));
            systems.Add(typeof(HazardSystem));
        }
        
        private void AddUISystems(List<Type> systems)
        {
            systems.Add(typeof(FocusTimeUISystem));
            systems.Add(typeof(ResonanceUISystem));
        }
        
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