// Path: Assets/_Project/Runtime/Bridge/PlayerBridge.cs
using Unity.Entities;
using UnityEngine;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Gameplay;

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// Bridge che collega i GameObject del giocatore con il sistema ECS.
    /// Gestisce la conversione delle proprietà del giocatore in componenti ECS.
    /// </summary>
    [AddComponentMenu("RunawayHeroes/Bridges/Player Bridge")]
    public class PlayerBridge : MonoBehaviour
    {
        [Header("Configurazione Giocatore")]
        [SerializeField] private CharacterType characterType = CharacterType.Alex;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float movementSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;
        
        // Classe Baker che si occupa della conversione GameObject -> Entity
        public class PlayerBridgeBaker : Baker<PlayerBridge>
        {
            public override void Bake(PlayerBridge authoring)
            {
                // Ottiene o crea l'entità associata a questo GameObject
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Aggiunge il componente di trasformazione
                AddComponent(entity, new TransformComponent
                {
                    Position = authoring.transform.position,
                    Rotation = authoring.transform.rotation,
                    Scale = authoring.transform.localScale.x
                });
                
                // Aggiunge il componente con i dati del giocatore
                AddComponent(entity, new PlayerDataComponent
                {
                    Type = authoring.characterType,
                    Name = authoring.characterType.ToString(),
                    IsUnlocked = true,
                    ExperienceLevel = 1,
                    CurrentExperience = 0,
                    ExperienceToNextLevel = 1000,
                    StatMultiplier = 1.0f,
                    FragmentID = (int)authoring.characterType,
                    FragmentPowerLevel = 1,
                    NativeWorldType = GetWorldTypeFromCharacter(authoring.characterType)
                });
                
                // Aggiunge il componente di salute
                AddComponent(entity, new HealthComponent
                {
                    CurrentHealth = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    IsInvulnerable = false,
                    InvulnerabilityTime = 0f,
                    RegenRate = 0f,
                    IsRegenerating = false
                });
                
                // Aggiunge il componente di movimento
                AddComponent(entity, new MovementComponent
                {
                    BaseSpeed = authoring.movementSpeed,
                    CurrentSpeed = authoring.movementSpeed,
                    MaxSpeed = authoring.movementSpeed * 1.5f,
                    Acceleration = 10f,
                    JumpForce = authoring.jumpForce,
                    MaxJumps = 1,
                    RemainingJumps = 1,
                    IsJumping = false,
                    SlideDuration = 1.0f,
                    SlideTimeRemaining = 0f,
                    IsSliding = false,
                    SlideSpeedMultiplier = 1.5f,
                    IsMoving = false,
                    MoveDirection = new Unity.Mathematics.float3(0, 0, 1)
                });
                
                // Aggiunge i componenti specifici in base al tipo di personaggio
                switch (authoring.characterType)
                {
                    case CharacterType.Alex:
                        AddComponent(entity, AlexComponent.Default());
                        break;
                    case CharacterType.Maya:
                        AddComponent(entity, MayaComponent.Default());
                        break;
                    case CharacterType.Kai:
                        AddComponent(entity, KaiComponent.Default());
                        break;
                    case CharacterType.Ember:
                        AddComponent(entity, EmberComponent.Default());
                        break;
                    case CharacterType.Marina:
                        AddComponent(entity, MarinaComponent.Default());
                        break;
                    case CharacterType.Neo:
                        AddComponent(entity, NeoComponent.Default());
                        break;
                }
                
                // Aggiunge un Tag per identificare facilmente questa entità
                AddComponent<PlayerTag>(entity);
            }
            
            // Metodo di utility per ottenere il tipo di mondo corrispondente al tipo di personaggio
            private static WorldType GetWorldTypeFromCharacter(CharacterType characterType)
            {
                switch (characterType)
                {
                    case CharacterType.Alex: return WorldType.Urban;
                    case CharacterType.Maya: return WorldType.Forest;
                    case CharacterType.Kai: return WorldType.Tundra;
                    case CharacterType.Ember: return WorldType.Volcano;
                    case CharacterType.Marina: return WorldType.Abyss;
                    case CharacterType.Neo: return WorldType.Virtual;
                    default: return WorldType.None;
                }
            }
        }
    }
}