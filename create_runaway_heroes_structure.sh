#!/bin/bash

# Script per creare la struttura di directory e file .cs vuoti per Runaway Heroes ECS
# Esegui questo script dalla directory principale del progetto Unity (dove si trova la cartella Assets)

# Colori per l'output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}Creazione struttura di directory per Runaway Heroes ECS...${NC}"

# Funzione per creare un file .cs vuoto con una classe base
create_cs_file() {
    local FILE_PATH=$1
    local CLASS_NAME=$(basename "$FILE_PATH" .cs)
    
    # Crea la directory se non esiste
    mkdir -p "$(dirname "$FILE_PATH")"
    
    # Determina il namespace in base al percorso
    local NAMESPACE="RunawayHeroes"
    
    if [[ $FILE_PATH == *"/Components/"* ]]; then
        if [[ $FILE_PATH == *"/Core/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Core"
        elif [[ $FILE_PATH == *"/Gameplay/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Gameplay"
        elif [[ $FILE_PATH == *"/Characters/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Characters"
        elif [[ $FILE_PATH == *"/Abilities/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Abilities"
        elif [[ $FILE_PATH == *"/Enemies/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Enemies"
        elif [[ $FILE_PATH == *"/Input/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.Input"
        elif [[ $FILE_PATH == *"/World/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Components.World"
        else
            NAMESPACE="RunawayHeroes.ECS.Components"
        fi
        
        # Crea una struttura per Component
        echo "using System;
using Unity.Entities;
using Unity.Mathematics;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct $CLASS_NAME : IComponentData
    {
        // Proprietà
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Systems/"* ]]; then
        if [[ $FILE_PATH == *"/Core/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Core"
        elif [[ $FILE_PATH == *"/Input/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Input"
        elif [[ $FILE_PATH == *"/Movement/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Movement"
        elif [[ $FILE_PATH == *"/Combat/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Combat"
        elif [[ $FILE_PATH == *"/Abilities/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Abilities"
        elif [[ $FILE_PATH == *"/AI/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.AI"
        elif [[ $FILE_PATH == *"/World/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.World"
        elif [[ $FILE_PATH == *"/Gameplay/"* ]]; then
            NAMESPACE="RunawayHeroes.ECS.Systems.Gameplay"
        else
            NAMESPACE="RunawayHeroes.ECS.Systems"
        fi
        
        # Crea una struttura per System
        echo "using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public partial class $CLASS_NAME : SystemBase
    {
        protected override void OnCreate()
        {
            
        }

        protected override void OnUpdate()
        {
            
        }
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Events/EventDefinitions/"* ]]; then
        NAMESPACE="RunawayHeroes.ECS.Events"
        
        # Crea una struttura per Event
        echo "using Unity.Entities;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public struct $CLASS_NAME : IComponentData
    {
        // Dati evento
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Events/EventHandlers/"* ]]; then
        NAMESPACE="RunawayHeroes.ECS.Events.Handlers"
        
        # Crea una struttura per Event Handler
        echo "using Unity.Entities;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public partial class $CLASS_NAME : SystemBase
    {
        protected override void OnCreate()
        {
            
        }

        protected override void OnUpdate()
        {
            
        }
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Entities/Archetypes/"* || $FILE_PATH == *"/Entities/Blueprints/"* ]]; then
        NAMESPACE="RunawayHeroes.ECS.Entities"
        
        # Crea una struttura per Archetype/Blueprint
        echo "using Unity.Entities;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public static class $CLASS_NAME
    {
        public static Entity Create(EntityManager entityManager)
        {
            Entity entity = entityManager.CreateEntity();
            // Aggiungi componenti
            return entity;
        }
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Entities/Factory/"* ]]; then
        NAMESPACE="RunawayHeroes.ECS.Entities.Factory"
        
        # Crea una struttura per Factory
        echo "using Unity.Entities;
using Unity.Mathematics;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public static class $CLASS_NAME
    {
        // Metodi factory
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Utilities/"* ]]; then
        NAMESPACE="RunawayHeroes.ECS.Utilities"
        
        # Crea una struttura per Utility
        echo "using Unity.Entities;
using Unity.Collections;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public static class $CLASS_NAME
    {
        // Metodi utility
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Runtime/Bootstrap/"* ]]; then
        NAMESPACE="RunawayHeroes.Runtime.Bootstrap"
        
        # Crea una struttura per Bootstrap
        echo "using Unity.Entities;
using UnityEngine;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public class $CLASS_NAME : MonoBehaviour
    {
        private void Start()
        {
            // Inizializzazione
        }
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Runtime/Managers/"* ]]; then
        NAMESPACE="RunawayHeroes.Runtime.Managers"
        
        # Crea una struttura per Manager
        echo "using UnityEngine;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public class $CLASS_NAME : MonoBehaviour
    {
        // Singleton pattern
        public static $CLASS_NAME Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}" > "$FILE_PATH"
    
    elif [[ $FILE_PATH == *"/Runtime/Bridge/"* ]]; then
        NAMESPACE="RunawayHeroes.Runtime.Bridge"
        
        # Crea una struttura per Bridge
        echo "using Unity.Entities;
using UnityEngine;

namespace $NAMESPACE
{
    /// <summary>
    /// 
    /// </summary>
    public class $CLASS_NAME : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            // Conversione da GameObject a Entity
        }
    }
}" > "$FILE_PATH"
    
    else
        # Struttura generica per altri file
        echo "namespace RunawayHeroes
{
    /// <summary>
    /// 
    /// </summary>
    public class $CLASS_NAME
    {
        
    }
}" > "$FILE_PATH"
    fi
    
    echo -e "${GREEN}Creato file: ${YELLOW}$FILE_PATH${NC}"
}

# Directory principale
mkdir -p "Assets/_Project"

# Crea struttura ECS
mkdir -p "Assets/_Project/ECS"

# Componenti
echo -e "${BLUE}Creazione componenti...${NC}"

# Core Components
create_cs_file "Assets/_Project/ECS/Components/Core/TransformComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Core/PhysicsComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Core/IdentityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Core/RenderComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Core/TagComponent.cs"

# Gameplay Components
create_cs_file "Assets/_Project/ECS/Components/Gameplay/HealthComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/MovementComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/FocusTimeComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/FragmentResonanceComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/CollectibleComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/ObstacleComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Gameplay/EnvironmentalEffectComponent.cs"

# Character Components
create_cs_file "Assets/_Project/ECS/Components/Characters/PlayerDataComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/AlexComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/MayaComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/KaiComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/EmberComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/MarinaComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Characters/NeoComponent.cs"

# Ability Components
create_cs_file "Assets/_Project/ECS/Components/Abilities/AbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/UrbanDashAbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/NatureCallAbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/HeatAuraAbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/FireproofBodyAbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/AirBubbleAbilityComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Abilities/ControlledGlitchAbilityComponent.cs"

# Enemy Components
create_cs_file "Assets/_Project/ECS/Components/Enemies/EnemyComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/BossComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/MidBossComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/DroneComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/PatrolComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/AttackComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Enemies/AIStateComponent.cs"

# Input Components
create_cs_file "Assets/_Project/ECS/Components/Input/InputComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Input/TouchInputComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Input/JumpInputComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Input/SlideInputComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Input/FocusTimeInputComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/Input/AbilityInputComponent.cs"

# World Components
create_cs_file "Assets/_Project/ECS/Components/World/LevelComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/World/WorldIdentifierComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/World/CheckpointComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/World/SpawnPointComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/World/HazardComponent.cs"
create_cs_file "Assets/_Project/ECS/Components/World/PathComponent.cs"

# Entities
echo -e "${BLUE}Creazione entità...${NC}"

# Archetypes
create_cs_file "Assets/_Project/ECS/Entities/Archetypes/PlayerArchetypes.cs"
create_cs_file "Assets/_Project/ECS/Entities/Archetypes/EnemyArchetypes.cs"
create_cs_file "Assets/_Project/ECS/Entities/Archetypes/CollectibleArchetypes.cs"
create_cs_file "Assets/_Project/ECS/Entities/Archetypes/ObstacleArchetypes.cs"

# Blueprints
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Alex.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Maya.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Kai.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Ember.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Marina.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/Neo.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/CyborgSecurity.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/SpiritGuardian.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/ColosalYeti.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/MagmaElemental.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/MutantKraken.cs"
create_cs_file "Assets/_Project/ECS/Entities/Blueprints/CorruptedAI.cs"

# Factory
create_cs_file "Assets/_Project/ECS/Entities/Factory/PlayerFactory.cs"
create_cs_file "Assets/_Project/ECS/Entities/Factory/EnemyFactory.cs"
create_cs_file "Assets/_Project/ECS/Entities/Factory/BossFactory.cs"
create_cs_file "Assets/_Project/ECS/Entities/Factory/CollectibleFactory.cs"
create_cs_file "Assets/_Project/ECS/Entities/Factory/WorldEntityFactory.cs"
create_cs_file "Assets/_Project/ECS/Entities/Factory/FXFactory.cs"

# Systems
echo -e "${BLUE}Creazione sistemi...${NC}"

# Core Systems
create_cs_file "Assets/_Project/ECS/Systems/Core/TransformSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Core/PhysicsSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Core/CollisionSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Core/RenderSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Core/EntityLifecycleSystem.cs"

# Input Systems
create_cs_file "Assets/_Project/ECS/Systems/Input/InputSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Input/TouchInputSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Input/GestureRecognitionSystem.cs"

# Movement Systems
create_cs_file "Assets/_Project/ECS/Systems/Movement/PlayerMovementSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Movement/JumpSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Movement/SlideSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Movement/NavigationSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Movement/ObstacleAvoidanceSystem.cs"

# Combat Systems
create_cs_file "Assets/_Project/ECS/Systems/Combat/DamageSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Combat/HealthSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Combat/KnockbackSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Combat/HitboxSystem.cs"

# Ability Systems
create_cs_file "Assets/_Project/ECS/Systems/Abilities/AbilitySystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/FocusTimeSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/FragmentResonanceSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/UrbanDashSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/NatureCallSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/HeatAuraSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/FireproofBodySystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/AirBubbleSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Abilities/ControlledGlitchSystem.cs"

# AI Systems
create_cs_file "Assets/_Project/ECS/Systems/AI/EnemyAISystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/AI/PatrolSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/AI/AttackPatternSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/AI/BossPhasesSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/AI/PursuitSystem.cs"

# World Systems
create_cs_file "Assets/_Project/ECS/Systems/World/LevelGenerationSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/World/ObstacleSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/World/HazardSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/World/CheckpointSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/World/EnvironmentalEffectSystem.cs"

# Gameplay Systems
create_cs_file "Assets/_Project/ECS/Systems/Gameplay/ScoreSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Gameplay/CollectibleSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Gameplay/PowerupSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Gameplay/ProgressionSystem.cs"
create_cs_file "Assets/_Project/ECS/Systems/Gameplay/DifficultySystem.cs"

# Events
echo -e "${BLUE}Creazione eventi...${NC}"

# Event Definitions
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/CollisionEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/DamageEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/AbilityActivatedEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/FocusTimeEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/FragmentCollectedEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/CharacterSwitchEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/LevelCompletedEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/EnemyDefeatedEvent.cs"
create_cs_file "Assets/_Project/ECS/Events/EventDefinitions/CheckpointReachedEvent.cs"

# Event Handlers
create_cs_file "Assets/_Project/ECS/Events/EventHandlers/CollisionEventHandler.cs"
create_cs_file "Assets/_Project/ECS/Events/EventHandlers/DamageEventHandler.cs"
create_cs_file "Assets/_Project/ECS/Events/EventHandlers/GameplayEventHandler.cs"
create_cs_file "Assets/_Project/ECS/Events/EventHandlers/UIEventHandler.cs"

# Utilities
echo -e "${BLUE}Creazione utility...${NC}"
create_cs_file "Assets/_Project/ECS/Utilities/EntityQueries.cs"
create_cs_file "Assets/_Project/ECS/Utilities/ComponentExtensions.cs"
create_cs_file "Assets/_Project/ECS/Utilities/SystemUtilities.cs"
create_cs_file "Assets/_Project/ECS/Utilities/ECSLogger.cs"
create_cs_file "Assets/_Project/ECS/Utilities/EntityDebugger.cs"

# Data
echo -e "${BLUE}Creazione cartelle per dati...${NC}"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Characters"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Enemies"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Bosses"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Abilities"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Worlds"
mkdir -p "Assets/_Project/Data/ScriptableObjects/Items"
mkdir -p "Assets/_Project/Data/Config"

# Runtime
echo -e "${BLUE}Creazione runtime...${NC}"

# Bootstrap
create_cs_file "Assets/_Project/Runtime/Bootstrap/ECSBootstrap.cs"
create_cs_file "Assets/_Project/Runtime/Bootstrap/GameBootstrap.cs"
create_cs_file "Assets/_Project/Runtime/Bootstrap/WorldBootstrap.cs"

# Managers
create_cs_file "Assets/_Project/Runtime/Managers/GameManager.cs"
create_cs_file "Assets/_Project/Runtime/Managers/LevelManager.cs"
create_cs_file "Assets/_Project/Runtime/Managers/UIManager.cs"
create_cs_file "Assets/_Project/Runtime/Managers/AudioManager.cs"

# Bridge
create_cs_file "Assets/_Project/Runtime/Bridge/PlayerBridge.cs"
create_cs_file "Assets/_Project/Runtime/Bridge/UnityCameraBridge.cs"
create_cs_file "Assets/_Project/Runtime/Bridge/UnityPhysicsBridge.cs"
create_cs_file "Assets/_Project/Runtime/Bridge/InputBridge.cs"

# Altre Utility
echo -e "${BLUE}Creazione utility generiche...${NC}"
create_cs_file "Assets/_Project/Utilities/Extensions/VectorExtensions.cs"
create_cs_file "Assets/_Project/Utilities/Extensions/StringExtensions.cs"
create_cs_file "Assets/_Project/Utilities/Helpers/MathHelper.cs"
create_cs_file "Assets/_Project/Utilities/Helpers/StringHelper.cs"
create_cs_file "Assets/_Project/Utilities/Helpers/DebugHelper.cs"

# Editor
echo -e "${BLUE}Creazione strumenti editor...${NC}"
create_cs_file "Assets/_Project/Editor/ECSDebugger/EntityDebugWindow.cs"
create_cs_file "Assets/_Project/Editor/ECSDebugger/SystemMonitorWindow.cs"
create_cs_file "Assets/_Project/Editor/CustomInspectors/ComponentDataInspector.cs"
create_cs_file "Assets/_Project/Editor/CustomInspectors/EntityInspector.cs"
create_cs_file "Assets/_Project/Editor/Wizards/ECSComponentWizard.cs"
create_cs_file "Assets/_Project/Editor/Wizards/ECSSystemWizard.cs"

# Cartelle per Asset artistici
echo -e "${BLUE}Creazione cartelle per asset artistici...${NC}"

# Characters
mkdir -p "Assets/Art/Characters/Alex"
mkdir -p "Assets/Art/Characters/Maya"
mkdir -p "Assets/Art/Characters/Kai"
mkdir -p "Assets/Art/Characters/Ember"
mkdir -p "Assets/Art/Characters/Marina"
mkdir -p "Assets/Art/Characters/Neo"

# Enemies
mkdir -p "Assets/Art/Enemies/Bosses"
mkdir -p "Assets/Art/Enemies/MidBosses"
mkdir -p "Assets/Art/Enemies/Common"

# Environments
mkdir -p "Assets/Art/Environments/Tutorial"
mkdir -p "Assets/Art/Environments/Urban"
mkdir -p "Assets/Art/Environments/Forest"
mkdir -p "Assets/Art/Environments/Tundra"
mkdir -p "Assets/Art/Environments/Volcano"
mkdir -p "Assets/Art/Environments/Abyss"
mkdir -p "Assets/Art/Environments/Virtual"

# VFX
mkdir -p "Assets/Art/VFX/Abilities"
mkdir -p "Assets/Art/VFX/Environmental"
mkdir -p "Assets/Art/VFX/Combat"

# UI
mkdir -p "Assets/Art/UI/HUD"
mkdir -p "Assets/Art/UI/Menus"
mkdir -p "Assets/Art/UI/Icons"

# Audio
mkdir -p "Assets/Audio/Music/Worlds"
mkdir -p "Assets/Audio/Music/Menu"
mkdir -p "Assets/Audio/Music/Boss"
mkdir -p "Assets/Audio/SFX/Characters"
mkdir -p "Assets/Audio/SFX/Abilities"
mkdir -p "Assets/Audio/SFX/Environments"
mkdir -p "Assets/Audio/SFX/UI"

# Scenes
mkdir -p "Assets/Scenes/Tutorial"
mkdir -p "Assets/Scenes/World1_City"
mkdir -p "Assets/Scenes/World2_Forest"
mkdir -p "Assets/Scenes/World3_Tundra"
mkdir -p "Assets/Scenes/World4_Volcano"
mkdir -p "Assets/Scenes/World5_Abyss"
mkdir -p "Assets/Scenes/World6_Virtual"

# Scene vuote
touch "Assets/Scenes/Boot.unity"
touch "Assets/Scenes/MainMenu.unity"

# Scene Tutorial
touch "Assets/Scenes/Tutorial/Level1_FirstSteps.unity"
touch "Assets/Scenes/Tutorial/Level2_PerfectSlide.unity"
touch "Assets/Scenes/Tutorial/Level3_ReadyReflexes.unity"
touch "Assets/Scenes/Tutorial/Level4_ItemPower.unity"
touch "Assets/Scenes/Tutorial/Level5_EscapeTrainer.unity"

# Scene Città in Caos
touch "Assets/Scenes/World1_City/Level1_CentralPark.unity"
touch "Assets/Scenes/World1_City/Level2_CommercialAvenues.unity"
touch "Assets/Scenes/World1_City/Level3_ResidentialDistrict.unity"
touch "Assets/Scenes/World1_City/Level4_ConstructionArea.unity"
touch "Assets/Scenes/World1_City/Level5_IndustrialZone.unity"
touch "Assets/Scenes/World1_City/Level6_AbandonedSite.unity"
touch "Assets/Scenes/World1_City/Level7_RundownPeriphery.unity"
touch "Assets/Scenes/World1_City/Level8_PollutedDistrict.unity"
touch "Assets/Scenes/World1_City/Level9_TechCenter.unity"

# Plugins
mkdir -p "Assets/Plugins/DOTween"
mkdir -p "Assets/Plugins/TextMeshPro"

echo -e "${GREEN}Creazione struttura completata con successo!${NC}"
echo -e "${YELLOW}Sono stati creati tutti i file .cs vuoti con classi/strutture base ECS.${NC}"