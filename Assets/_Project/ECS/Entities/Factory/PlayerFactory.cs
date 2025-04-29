// Path: Assets/_Project/ECS/Entities/Factory/PlayerFactory.cs
using Unity.Entities;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Characters;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Abilities;
using RunawayHeroes.ECS.Components.Input;

namespace RunawayHeroes.ECS.Entities.Factory
{
    /// <summary>
    /// Factory che crea entità per i personaggi giocabili.
    /// Implementa metodi per creare i 6 protagonisti con configurazioni appropriate.
    /// </summary>
    public static class PlayerFactory
    {
        /// <summary>
        /// Crea un'entità giocatore con componenti base
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale del giocatore</param>
        /// <returns>Entità player creata</returns>
        public static Entity CreatePlayer(EntityManager entityManager, float3 position)
        {
            // Per default creiamo Alex come personaggio iniziale
            return CreateAlex(entityManager, position);
        }
        
        /// <summary>
        /// Crea un'entità per Alex, il corriere urbano
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Alex creata</returns>
        public static Entity CreateAlex(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(100f));
            entityManager.AddComponentData(entity, MovementComponent.DefaultPlayer());
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Alex, 
                "Alex",
                WorldType.Urban
            ));
            
            // Aggiungi componente specifico di Alex
            entityManager.AddComponentData(entity, AlexComponent.Default());
            
            // Aggiungi abilità speciale (Scatto Urbano)
            entityManager.AddComponentData(entity, new UrbanDashAbilityComponent
            {
                Duration = 2.0f,
                RemainingTime = 0,
                Cooldown = 15.0f,
                CooldownRemaining = 0,
                SpeedMultiplier = 1.5f,
                InitialBoost = 2.0f,
                IsActive = false,
                BreakThroughForce = 500.0f
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi Risonanza dei Frammenti (con solo Alex sbloccato inizialmente)
            entityManager.AddComponentData(entity, FragmentResonanceComponent.Default(entity));
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea un'entità per Maya, l'esploratrice della foresta
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Maya creata</returns>
        public static Entity CreateMaya(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(90f)); // Leggermente meno vita di Alex
            
            // MovementComponent con agilità maggiore
            var movement = MovementComponent.DefaultPlayer();
            movement.JumpForce *= 1.1f; // +10% potenza di salto
            entityManager.AddComponentData(entity, movement);
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Maya, 
                "Maya",
                WorldType.Forest
            ));
            
            // Aggiungi componente specifico di Maya
            entityManager.AddComponentData(entity, new MayaComponent
            {
                NaturalRegeneration = 0.5f, // Rigenerazione salute in ambienti naturali
                PoisonResistance = 0.5f,    // Resistenza ai veleni
                ForestVision = 1.0f,        // Visibilità migliorata nella foresta
                WildlifeAffinity = 0.8f     // Reazione più mite degli animali selvatici
            });
            
            // Aggiungi abilità speciale (Richiamo della Natura)
            entityManager.AddComponentData(entity, new NatureCallAbilityComponent
            {
                Duration = 5.0f,            // Durata dell'abilità
                RemainingTime = 0,
                Cooldown = 20.0f,
                CooldownRemaining = 0,
                MaxAllies = 3,              // Numero massimo di alleati evocabili
                AllySummonRadius = 10.0f,   // Raggio di evocazione
                AllyDistractDuration = 5.0f, // Durata della distrazione
                IsActive = false
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea un'entità per Kai, l'alpinista della tundra
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Kai creata</returns>
        public static Entity CreateKai(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(110f)); // Più vita di Alex
            
            // MovementComponent con forza maggiore ma meno agilità
            var movement = MovementComponent.DefaultPlayer();
            movement.JumpForce *= 1.15f; // +15% potenza di salto
            movement.SlideDuration *= 0.9f; // -10% durata scivolata
            entityManager.AddComponentData(entity, movement);
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Kai, 
                "Kai",
                WorldType.Tundra
            ));
            
            // Aggiungi componente specifico di Kai
            entityManager.AddComponentData(entity, new KaiComponent
            {
                ColdResistance = 0.8f,     // Alta resistenza al freddo
                IceGrip = 0.7f,            // Migliore presa sul ghiaccio
                StaminaBonus = 0.2f,       // +20% stamina
                ClimbingAbility = 0.6f     // Abilità di arrampicata
            });
            
            // Aggiungi abilità speciale (Aura di Calore)
            entityManager.AddComponentData(entity, new HeatAuraAbilityComponent
            {
                Duration = 8.0f,           // Durata dell'abilità
                RemainingTime = 0,
                Cooldown = 25.0f,
                CooldownRemaining = 0,
                AuraRadius = 5.0f,         // Raggio dell'aura
                MeltIceRate = 0.5f,        // Velocità scioglimento ghiaccio
                IsActive = false
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea un'entità per Ember, la sopravvissuta del vulcano
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Ember creata</returns>
        public static Entity CreateEmber(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(95f));
            
            // MovementComponent con maggiore resistenza
            var movement = MovementComponent.DefaultPlayer();
            movement.BaseSpeed *= 0.95f; // -5% velocità base
            movement.SlideSpeedMultiplier *= 1.1f; // +10% velocità scivolata
            entityManager.AddComponentData(entity, movement);
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Ember, 
                "Ember",
                WorldType.Volcano
            ));
            
            // Aggiungi componente specifico di Ember
            entityManager.AddComponentData(entity, new EmberComponent
            {
                HeatResistance = 0.8f,     // Alta resistenza al calore
                FireDamageReduction = 0.6f, // Riduzione danni da fuoco
                ToxicGasResistance = 0.4f,  // Resistenza a gas tossici
                ExplosionResistance = 0.3f  // Resistenza a esplosioni
            });
            
            // Aggiungi abilità speciale (Corpo Ignifugo)
            entityManager.AddComponentData(entity, new FireproofBodyAbilityComponent
            {
                Duration = 3.0f,            // Durata dell'abilità
                RemainingTime = 0,
                Cooldown = 30.0f,
                CooldownRemaining = 0,
                HeatAura = 2.0f,            // Aura di calore che danneggia nemici vicini
                LavaWalkingActive = true,        // Immunità alla lava quando attivo
                IsActive = false
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea un'entità per Marina, la biologa degli abissi
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Marina creata</returns>
        public static Entity CreateMarina(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(90f));
            
            // MovementComponent con movimento in acqua migliorato
            var movement = MovementComponent.DefaultPlayer();
            movement.BaseSpeed *= 0.9f; // -10% velocità base sulla terra
            entityManager.AddComponentData(entity, movement);
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Marina, 
                "Marina",
                WorldType.Abyss
            ));
            
            // Aggiungi componente specifico di Marina
            entityManager.AddComponentData(entity, new MarinaComponent
            {
                SwimSpeed = 1.5f,          // +50% velocità nuoto rispetto al movimento terrestre
                WaterBreathing = 0.2f,      // Consumo di ossigeno ridotto del 80%
                PressureResistance = 0.7f,  // Alta resistenza alla pressione
                UnderwaterVision = 0.9f,    // Visibilità migliorata sott'acqua
                ElectricResistance = 0.5f   // Resistenza agli shock elettrici
            });
            
            // Aggiungi abilità speciale (Bolla d'Aria)
            entityManager.AddComponentData(entity, new AirBubbleAbilityComponent
            {
                Duration = 20.0f,           // Durata dell'abilità
                RemainingTime = 0,
                Cooldown = 20.0f,
                CooldownRemaining = 0,
                BubbleRadius = 3.0f,        // Raggio della bolla
                RepelForce = 5.0f,          // Forza di repulsione contro nemici
                IsActive = false
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea un'entità per Neo, l'hacker della realtà virtuale
        /// </summary>
        /// <param name="entityManager">EntityManager per creare l'entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Entità Neo creata</returns>
        public static Entity CreateNeo(EntityManager entityManager, float3 position)
        {
            // Crea l'entità base
            Entity entity = entityManager.CreateEntity();
            
            // Aggiungi componenti di base
            entityManager.AddComponentData(entity, new TagComponent { Tag = "Player" });
            entityManager.AddComponentData(entity, TransformComponent.WithPosition(position));
            entityManager.AddComponentData(entity, PhysicsComponent.Default());
            entityManager.AddComponentData(entity, HealthComponent.Default(85f)); // Meno vita di tutti
            
            // MovementComponent più imprevedibile
            var movement = MovementComponent.DefaultPlayer();
            movement.BaseSpeed *= 1.05f; // +5% velocità base
            entityManager.AddComponentData(entity, movement);
            
            // Aggiungi componente PlayerData specifico
            entityManager.AddComponentData(entity, PlayerDataComponent.Create(
                CharacterType.Neo, 
                "Neo",
                WorldType.Virtual
            ));
            
            // Aggiungi componente specifico di Neo
            entityManager.AddComponentData(entity, new NeoComponent
            {
                CodeSight = 0.9f,           // Capacità di vedere codice e glitch
                DataCorruptionResistance = 0.7f, // Resistenza alla corruzione di dati
                GlitchManipulation = 0.6f,   // Capacità di manipolare glitch
                HackingEfficiency = 0.8f     // Efficienza nei mini-giochi hacking
            });
            
            // Aggiungi abilità speciale (Glitch Controllato)
            entityManager.AddComponentData(entity, new ControlledGlitchAbilityComponent
            {
                Duration = 1.5f,            // Durata dell'abilità
                RemainingTime = 0,
                Cooldown = 25.0f,
                CooldownRemaining = 0,
                GlitchDistance = 10.0f,     // Distanza massima del glitch
                BarrierPenetration = true,  // Capacità di attraversare barriere
                IsActive = false
            });
            
            // Aggiungi Focus Time
            entityManager.AddComponentData(entity, FocusTimeComponent.Default());
            
            // Aggiungi componenti di input
            entityManager.AddComponentData(entity, new InputComponent());
            entityManager.AddComponentData(entity, new JumpInputComponent());
            entityManager.AddComponentData(entity, new SlideInputComponent());
            entityManager.AddComponentData(entity, new FocusTimeInputComponent());
            entityManager.AddComponentData(entity, new AbilityInputComponent());
            
            return entity;
        }
        
        /// <summary>
        /// Crea tutti i personaggi per testing
        /// </summary>
        /// <param name="entityManager">EntityManager per creare le entità</param>
        /// <param name="position">Posizione iniziale</param>
        /// <returns>Array delle entità create</returns>
        public static Entity[] CreateAllCharacters(EntityManager entityManager, float3 position)
        {
            return new Entity[]
            {
                CreateAlex(entityManager, position),
                CreateMaya(entityManager, position),
                CreateKai(entityManager, position),
                CreateEmber(entityManager, position),
                CreateMarina(entityManager, position),
                CreateNeo(entityManager, position)
            };
        }
    }
}