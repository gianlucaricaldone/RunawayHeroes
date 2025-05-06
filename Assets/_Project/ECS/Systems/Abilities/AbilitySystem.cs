// Path: Assets/_Project/ECS/Systems/Abilities/AbilitySystem.cs
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Events.EventDefinitions;

namespace RunawayHeroes.ECS.Systems.Abilities
{
    /// <summary>
    /// Sistema base per la gestione delle abilità dei personaggi.
    /// Si occupa della ricezione degli input e del routing verso i sistemi
    /// specifici di ciascuna abilità.
    /// </summary>
    public partial struct AbilitySystem : ISystem
    {
        private EntityQuery _inputQuery;
        
        public void OnCreate(ref SystemState state)
        {
            // Definisci la query usando EntityQueryBuilder
            _inputQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<AbilityInputComponent, InputComponent, PlayerDataComponent>()
                .Build(ref state);
            
            // Richiedi entità corrispondenti per l'aggiornamento
            state.RequireForUpdate(_inputQuery);
            
            // Richiedi il singleton di EndSimulationEntityCommandBufferSystem per eventi
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var commandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            
            // Processa l'input di abilità e indirizza al sistema appropriato usando IJobEntity
            new AbilityInputProcessorJob().ScheduleParallel(_inputQuery, state.Dependency).Complete();
            
            // Non è più necessario chiamare AddJobHandleForProducer nella nuova API DOTS
        }
        
    }
    
    /// <summary>
    /// Enumerazione dei tipi di abilità disponibili
    /// </summary>
    public enum AbilityType
    {
        None = 0,
        UrbanDash = 1,        // Alex - Scatto Urbano
        NatureCall = 2,       // Maya - Richiamo della Natura
        HeatAura = 3,         // Kai - Aura di Calore
        FireproofBody = 4,    // Ember - Corpo Ignifugo
        AirBubble = 5,        // Marina - Bolla d'Aria
        ControlledGlitch = 6  // Neo - Glitch Controllato
    }
    
    
    /// <summary>
    /// Evento generato quando un'abilità termina
    /// </summary>
    [System.Serializable]
    public struct AbilityEndedEvent : IComponentData
    {
        public Entity EntityID;       // Entità che termina l'abilità
        public AbilityType AbilityType; // Tipo di abilità
    }
    
    /// <summary>
    /// Evento generato quando un'abilità è pronta (cooldown terminato)
    /// </summary>
    [System.Serializable]
    public struct AbilityReadyEvent : IComponentData
    {
        public Entity EntityID;       // Entità con abilità pronta
        public AbilityType AbilityType; // Tipo di abilità
    }
    
    /// <summary>
    /// Job per elaborare gli input di abilità
    /// </summary>
    public partial struct AbilityInputProcessorJob : IJobEntity
    {
        void Execute(Entity entity, 
                    ref AbilityInputComponent abilityInput,
                    in InputComponent input,
                    in PlayerDataComponent playerData)
        {
            // Rileva l'attivazione dell'abilità dall'input principale
            bool activateAbility = input.AbilityPressed;
            
            // Imposta l'input e resetta il flag di input una volta elaborato
            abilityInput.ActivateAbility = activateAbility;
            abilityInput.TargetPosition = input.TouchPosition;
            
            // Determina il tipo di abilità in base al personaggio
            AbilityType abilityType = GetAbilityTypeForCharacter(playerData.Type);
            abilityInput.CurrentAbilityType = abilityType;
        }
        
        /// <summary>
        /// Determina il tipo di abilità associata a un personaggio
        /// </summary>
        private AbilityType GetAbilityTypeForCharacter(CharacterType characterType)
        {
            switch (characterType)
            {
                case CharacterType.Alex:
                    return AbilityType.UrbanDash;
                case CharacterType.Maya:
                    return AbilityType.NatureCall;
                case CharacterType.Kai:
                    return AbilityType.HeatAura;
                case CharacterType.Ember:
                    return AbilityType.FireproofBody;
                case CharacterType.Marina:
                    return AbilityType.AirBubble;
                case CharacterType.Neo:
                    return AbilityType.ControlledGlitch;
                default:
                    return AbilityType.None;
            }
        }
        
    }
}