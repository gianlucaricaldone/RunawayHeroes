# Struttura File ECS per Runaway Heroes

Ecco la struttura dei file per l'implementazione ECS di "Runaway Heroes: Escape The Elements" basata sul documento di design e sugli standard ECS definiti.

```
Assets/
  _Project/                                 # Directory principale del progetto
    ECS/                                    # Sistema ECS
      Components/                           # Componenti (strutture dati pure)
        Core/                               # Componenti base del sistema
          TransformComponent.cs             # Posizione, rotazione, scala dell'entità
          PhysicsComponent.cs               # Proprietà fisiche come velocità, massa, gravità
          IdentityComponent.cs              # Identifica univocamente l'entità (nome, ID, tipo)
          RenderComponent.cs                # Riferimenti ai dati visivi (mesh, materiali)
          TagComponent.cs                   # Tag per categorizzazione (player, nemico, collezionabile)
          
        Gameplay/                           # Componenti specifici delle meccaniche di gioco
          HealthComponent.cs                # Punti vita, invulnerabilità, rigenerazione
          MovementComponent.cs              # Velocità, salto, scivolata, forze
          FocusTimeComponent.cs             # Gestione del rallentamento temporale
          FragmentResonanceComponent.cs     # Cambio personaggio durante il gameplay
          CollectibleComponent.cs           # Proprietà oggetti raccoglibili
          ObstacleComponent.cs              # Proprietà degli ostacoli (danno, effetti)
          EnvironmentalEffectComponent.cs   # Effetti ambientali che influenzano il giocatore
          
        Characters/                         # Componenti specifici dei personaggi
          PlayerDataComponent.cs            # Dati generali del personaggio attivo
          AlexComponent.cs                  # Proprietà specifiche di Alex (Urbano)
          MayaComponent.cs                  # Proprietà specifiche di Maya (Foresta)
          KaiComponent.cs                   # Proprietà specifiche di Kai (Tundra)
          EmberComponent.cs                 # Proprietà specifiche di Ember (Vulcano)
          MarinaComponent.cs                # Proprietà specifiche di Marina (Abissi)
          NeoComponent.cs                   # Proprietà specifiche di Neo (Virtuale)
          
        Abilities/                          # Componenti abilità speciali
          AbilityComponent.cs               # Componente base abilità (cooldown, durata)
          UrbanDashAbilityComponent.cs      # Scatto Urbano - Alex
          NatureCallAbilityComponent.cs     # Richiamo della Natura - Maya
          HeatAuraAbilityComponent.cs       # Aura di Calore - Kai
          FireproofBodyAbilityComponent.cs  # Corpo Ignifugo - Ember
          AirBubbleAbilityComponent.cs      # Bolla d'Aria - Marina
          ControlledGlitchAbilityComponent.cs # Glitch Controllato - Neo
          
        Enemies/                            # Componenti nemici
          EnemyComponent.cs                 # Componente base nemico
          BossComponent.cs                  # Proprietà boss (fasi, pattern attacco)
          MidBossComponent.cs               # Proprietà mid-boss
          DroneComponent.cs                 # Proprietà droni e nemici volanti
          PatrolComponent.cs                # Pattern di pattugliamento
          AttackComponent.cs                # Pattern di attacco
          AIStateComponent.cs               # Stati IA dei nemici
          
        Input/                              # Componenti input
          InputComponent.cs                 # Input generico
          TouchInputComponent.cs            # Input touch per mobile
          JumpInputComponent.cs             # Input per salto
          SlideInputComponent.cs            # Input per scivolata
          FocusTimeInputComponent.cs        # Input per attivazione Focus Time
          AbilityInputComponent.cs          # Input per abilità speciali
          
        World/                              # Componenti mondo di gioco
          LevelComponent.cs                 # Proprietà del livello corrente
          WorldIdentifierComponent.cs       # Identifica il mondo (Città, Foresta, ecc.)
          CheckpointComponent.cs            # Punti di controllo nel livello
          SpawnPointComponent.cs            # Punti di spawn di nemici/oggetti
          HazardComponent.cs                # Zone pericolose con effetti specifici
          PathComponent.cs                  # Percorsi predefiniti per entità in movimento
          
      Entities/                             # Definizioni e factory di entità
        Archetypes/                         # Template per entità
          PlayerArchetypes.cs               # Archetipi per i diversi personaggi giocabili
          EnemyArchetypes.cs                # Archetipi per i diversi tipi di nemici
          CollectibleArchetypes.cs          # Archetipi per oggetti collezionabili
          ObstacleArchetypes.cs             # Archetipi per ostacoli
          
        Blueprints/                         # Blueprint dettagliati
          Alex.cs                           # Blueprint per Alex con tutti i componenti
          Maya.cs                           # Blueprint per Maya con tutti i componenti
          Kai.cs                            # Blueprint per Kai con tutti i componenti
          Ember.cs                          # Blueprint per Ember con tutti i componenti
          Marina.cs                         # Blueprint per Marina con tutti i componenti
          Neo.cs                            # Blueprint per Neo con tutti i componenti
          CyborgSecurity.cs                 # Blueprint per boss Cyborg di Sicurezza
          SpiritGuardian.cs                 # Blueprint per boss Spirito Guardiano
          ColosalYeti.cs                    # Blueprint per boss Yeti Colossale
          MagmaElemental.cs                 # Blueprint per boss Elementale di Magma
          MutantKraken.cs                   # Blueprint per boss Kraken Mutante
          CorruptedAI.cs                    # Blueprint per boss I.A. Corrotta
          
        Factory/                            # Factory per creazione entità
          PlayerFactory.cs                  # Crea entità giocatore
          EnemyFactory.cs                   # Crea entità nemici
          BossFactory.cs                    # Crea entità boss
          CollectibleFactory.cs             # Crea entità oggetti collezionabili
          WorldEntityFactory.cs             # Crea entità ambiente
          FXFactory.cs                      # Crea entità effetti visivi
          
      Systems/                              # Sistemi che implementano la logica
        Core/                               # Sistemi fondamentali
          TransformSystem.cs                # Aggiorna posizioni e rotazioni
          PhysicsSystem.cs                  # Simula fisica e movimento
          CollisionSystem.cs                # Rileva e gestisce collisioni
          RenderSystem.cs                   # Gestisce rendering delle entità
          EntityLifecycleSystem.cs          # Crea, inizializza e distrugge entità
          
        Input/                              # Sistemi di input
          InputSystem.cs                    # Processa input giocatore
          TouchInputSystem.cs               # Processa input touch specifici
          GestureRecognitionSystem.cs       # Riconosce swipe e gestures
          
        Movement/                           # Sistemi movimento
          PlayerMovementSystem.cs           # Gestisce movimento base del giocatore
          JumpSystem.cs                     # Gestisce salti e fisica correlata
          SlideSystem.cs                    # Gestisce scivolate
          NavigationSystem.cs               # Gestisce movimento su percorsi diversi
          ObstacleAvoidanceSystem.cs        # Logica per evitare ostacoli
          
        Combat/                             # Sistemi combattimento
          DamageSystem.cs                   # Applica e calcola danni
          HealthSystem.cs                   # Gestisce salute ed effetti correlati
          KnockbackSystem.cs                # Gestisce spinte ed effetti fisici
          HitboxSystem.cs                   # Gestisce hitbox e collisioni di combattimento
          
        Abilities/                          # Sistemi abilità
          AbilitySystem.cs                  # Sistema base abilità
          FocusTimeSystem.cs                # Sistema rallentamento tempo
          FragmentResonanceSystem.cs        # Sistema cambio personaggio
          UrbanDashSystem.cs                # Sistema abilità Scatto Urbano
          NatureCallSystem.cs               # Sistema abilità Richiamo della Natura
          HeatAuraSystem.cs                 # Sistema abilità Aura di Calore
          FireproofBodySystem.cs            # Sistema abilità Corpo Ignifugo
          AirBubbleSystem.cs                # Sistema abilità Bolla d'Aria
          ControlledGlitchSystem.cs         # Sistema abilità Glitch Controllato
          
        AI/                                 # Sistemi IA
          EnemyAISystem.cs                  # Logica base IA nemici
          PatrolSystem.cs                   # Sistema pattugliamento
          AttackPatternSystem.cs            # Sistema pattern di attacco
          BossPhasesSystem.cs               # Gestisce fasi dei boss
          PursuitSystem.cs                  # Logica inseguimento giocatore
          
        World/                              # Sistemi mondo
          LevelGenerationSystem.cs          # Genera parti di livello
          ObstacleSystem.cs                 # Gestisce comportamento ostacoli
          HazardSystem.cs                   # Gestisce zone pericolose
          CheckpointSystem.cs               # Gestisce punti di controllo
          EnvironmentalEffectSystem.cs      # Applica effetti ambientali
          
        Gameplay/                           # Sistemi gameplay
          ScoreSystem.cs                    # Calcola e gestisce punteggio
          CollectibleSystem.cs              # Gestisce raccolta oggetti
          PowerupSystem.cs                  # Gestisce potenziamenti temporanei
          ProgressionSystem.cs              # Gestisce avanzamento giocatore
          DifficultySystem.cs               # Regola difficoltà di gioco
          
      Events/                               # Sistema eventi
        EventDefinitions/                   # Definizioni eventi
          CollisionEvent.cs                 # Evento collisione
          DamageEvent.cs                    # Evento danno
          AbilityActivatedEvent.cs          # Evento attivazione abilità
          FocusTimeEvent.cs                 # Evento attivazione Focus Time
          FragmentCollectedEvent.cs         # Evento raccolta frammento
          CharacterSwitchEvent.cs           # Evento cambio personaggio
          LevelCompletedEvent.cs            # Evento completamento livello
          EnemyDefeatedEvent.cs             # Evento sconfitta nemico
          CheckpointReachedEvent.cs         # Evento raggiungimento checkpoint
          
        EventHandlers/                      # Gestori eventi
          CollisionEventHandler.cs          # Gestisce eventi collisione
          DamageEventHandler.cs             # Gestisce eventi danno
          GameplayEventHandler.cs           # Gestisce eventi gameplay
          UIEventHandler.cs                 # Gestisce eventi UI
          
      Utilities/                            # Utility ECS
        EntityQueries.cs                    # Query predefinite per entità
        ComponentExtensions.cs              # Estensioni per componenti
        SystemUtilities.cs                  # Utility per sistemi
        ECSLogger.cs                        # Logger specifico ECS
        EntityDebugger.cs                   # Strumenti debug entità
        
    Data/                                   # Configurazione e dati
      ScriptableObjects/                    # ScriptableObjects
        Characters/                         # Dati personaggi
          AlexData.asset                    # Dati Alex
          MayaData.asset                    # Dati Maya
          KaiData.asset                     # Dati Kai
          EmberData.asset                   # Dati Ember
          MarinaData.asset                  # Dati Marina
          NeoData.asset                     # Dati Neo
          
        Enemies/                            # Dati nemici
          DroneSecurity.asset               # Dati Droni di Sicurezza
          CorruptedSpiders.asset            # Dati Ragni Corrotti
          ArcticWolves.asset                # Dati Lupi Artici
          LavaSalamanders.asset             # Dati Salamandre di Fuoco
          AbyssalSquids.asset               # Dati Calamari Abissali
          CorruptedDataPackets.asset        # Dati Pacchetti Corrotti
          
        Bosses/                             # Dati boss
          CyborgSecurityData.asset          # Dati boss Cyborg
          SpiritGuardianData.asset          # Dati boss Spirito Guardiano
          YetiData.asset                    # Dati boss Yeti
          MagmaElementalData.asset          # Dati boss Elementale di Magma
          KrakenData.asset                  # Dati boss Kraken
          CorruptedAIData.asset             # Dati boss I.A. Corrotta
          
        Abilities/                          # Dati abilità
          UrbanDashData.asset               # Dati Scatto Urbano
          NatureCallData.asset              # Dati Richiamo della Natura
          HeatAuraData.asset                # Dati Aura di Calore
          FireproofBodyData.asset           # Dati Corpo Ignifugo
          AirBubbleData.asset               # Dati Bolla d'Aria
          ControlledGlitchData.asset        # Dati Glitch Controllato
          
        Worlds/                             # Dati mondi
          TutorialWorldData.asset           # Dati Tutorial
          UrbanWorldData.asset              # Dati Città in Caos
          ForestWorldData.asset             # Dati Foresta Primordiale
          TundraWorldData.asset             # Dati Tundra Eterna
          VolcanoWorldData.asset            # Dati Inferno di Lava
          AbyssWorldData.asset              # Dati Abissi Inesplorati
          VirtualWorldData.asset            # Dati Realtà Virtuale
          
        Items/                              # Dati oggetti
          CollectiblesData.asset            # Dati oggetti collezionabili
          PowerupsData.asset                # Dati potenziamenti
          ConsumablesData.asset             # Dati oggetti consumabili
          
      Config/                               # Configurazioni
        ECSConfig.asset                     # Configurazione sistema ECS
        GameplayConfig.asset                # Configurazioni gameplay
        DifficultySettings.asset            # Impostazioni difficoltà
        
    Runtime/                                # Codice bootstrap e runtime
      Bootstrap/                            # Inizializzazione
        ECSBootstrap.cs                     # Inizializza il sistema ECS
        GameBootstrap.cs                    # Inizializza il gioco
        WorldBootstrap.cs                   # Inizializza i mondi di gioco
        
      Managers/                             # Manager di alto livello
        GameManager.cs                      # Gestisce stato di gioco
        LevelManager.cs                     # Gestisce caricamento livelli
        UIManager.cs                        # Gestisce interfaccia utente
        AudioManager.cs                     # Gestisce audio
        
      Bridge/                               # Bridge tra MonoBehaviour e ECS
        PlayerBridge.cs                     # Collega MonoBehaviour player a ECS
        UnityCameraBridge.cs                # Collega camera Unity a ECS
        UnityPhysicsBridge.cs               # Collega fisica Unity a ECS
        InputBridge.cs                      # Collega sistema input Unity a ECS
        
    Utilities/                              # Utility generiche
      Extensions/                           # Estensioni classi
        VectorExtensions.cs                 # Estensioni per Vector2/3/4
        StringExtensions.cs                 # Estensioni per string
        
      Helpers/                              # Helper generici
        MathHelper.cs                       # Funzioni matematiche
        StringHelper.cs                     # Funzioni stringhe
        DebugHelper.cs                      # Funzioni debug
        
    Editor/                                 # Editor tools
      ECSDebugger/                          # Strumenti debug ECS
        EntityDebugWindow.cs                # Finestra debug entità
        SystemMonitorWindow.cs              # Monitoraggio performance sistemi
        
      CustomInspectors/                     # Inspector custom
        ComponentDataInspector.cs           # Inspector per ComponentData
        EntityInspector.cs                  # Inspector per Entity
        
      Wizards/                              # Wizard
        ECSComponentWizard.cs               # Crea nuovi componenti ECS
        ECSSystemWizard.cs                  # Crea nuovi sistemi ECS
        
  Art/                                      # Asset artistici
    Characters/                             # Modelli personaggi
      Alex/                                 # Asset Alex
      Maya/                                 # Asset Maya
      Kai/                                  # Asset Kai
      Ember/                                # Asset Ember
      Marina/                               # Asset Marina
      Neo/                                  # Asset Neo
      
    Enemies/                                # Modelli nemici
      Bosses/                               # Modelli boss
      MidBosses/                            # Modelli mid-boss
      Common/                               # Modelli nemici comuni
      
    Environments/                           # Asset ambienti
      Tutorial/                             # Asset Tutorial
      Urban/                                # Asset Città in Caos
      Forest/                               # Asset Foresta Primordiale
      Tundra/                               # Asset Tundra Eterna
      Volcano/                              # Asset Inferno di Lava
      Abyss/                                # Asset Abissi Inesplorati
      Virtual/                              # Asset Realtà Virtuale
      
    VFX/                                    # Effetti visivi
      Abilities/                            # VFX abilità
      Environmental/                        # VFX ambientali
      Combat/                               # VFX combattimento
      
    UI/                                     # Asset UI
      HUD/                                  # Elementi HUD
      Menus/                                # Elementi menu
      Icons/                                # Icone
      
  Audio/                                    # Asset audio
    Music/                                  # Musica
      Worlds/                               # Musica per mondo
      Menu/                                 # Musica menu
      Boss/                                 # Musica boss
      
    SFX/                                    # Effetti sonori
      Characters/                           # SFX personaggi
      Abilities/                            # SFX abilità
      Environments/                         # SFX ambienti
      UI/                                   # SFX interfaccia
      
  Scenes/                                   # Scene Unity
    Boot.unity                              # Scena boot
    MainMenu.unity                          # Menu principale
    Tutorial/                               # Scene tutorial
      Level1_FirstSteps.unity               # Livello 1 tutorial
      Level2_PerfectSlide.unity             # Livello 2 tutorial
      Level3_ReadyReflexes.unity            # Livello 3 tutorial
      Level4_ItemPower.unity                # Livello 4 tutorial
      Level5_EscapeTrainer.unity            # Livello 5 tutorial
      
    World1_City/                            # Scene Città in Caos
      Level1_CentralPark.unity              # Livello 1 città
      Level2_CommercialAvenues.unity        # Livello 2 città
      Level3_ResidentialDistrict.unity      # Livello 3 città (mid-boss)
      Level4_ConstructionArea.unity         # Livello 4 città
      Level5_IndustrialZone.unity           # Livello 5 città
      Level6_AbandonedSite.unity            # Livello 6 città (mid-boss)
      Level7_RundownPeriphery.unity         # Livello 7 città
      Level8_PollutedDistrict.unity         # Livello 8 città
      Level9_TechCenter.unity               # Livello 9 città (boss)
    
    World2_Forest/                          # Scene Foresta Primordiale
    World3_Tundra/                          # Scene Tundra Eterna
    World4_Volcano/                         # Scene Inferno di Lava
    World5_Abyss/                           # Scene Abissi Inesplorati
    World6_Virtual/                         # Scene Realtà Virtuale
    
  Plugins/                                  # Plugin di terze parti
    DOTween/                                # Libreria animazione
    TextMeshPro/                            # Libreria testo avanzato
```

## Note sui file principali

### Componenti Core del Player

1. **TransformComponent.cs** - Gestisce posizione, rotazione e scala dell'entità
2. **MovementComponent.cs** - Contiene dati movimento: velocità, jump force, slide duration
3. **HealthComponent.cs** - Contiene dati vita: current health, max health, invulnerability flag
4. **AbilityComponent.cs** - Struttura base per tutte le abilità speciali con cooldown e durata

### Sistemi Principali di Gameplay

1. **PlayerMovementSystem.cs** - Implementa la logica del movimento base: corsa, accelerazione
2. **JumpSystem.cs** - Gestisce la meccanica del salto con fisica realistica
3. **SlideSystem.cs** - Gestisce la meccanica della scivolata
4. **FocusTimeSystem.cs** - Implementa il rallentamento temporale per la selezione oggetti
5. **FragmentResonanceSystem.cs** - Gestisce il cambio di personaggio durante il gameplay

### Componenti/Sistemi per le Abilità Speciali

1. **UrbanDashSystem.cs** - Implementa l'abilità Scatto Urbano di Alex (invulnerabilità e accelerazione)
2. **NatureCallSystem.cs** - Implementa l'abilità Richiamo della Natura di Maya (animali alleati)
3. **HeatAuraSystem.cs** - Implementa l'abilità Aura di Calore di Kai (scioglimento ghiaccio)
4. **FireproofBodySystem.cs** - Implementa l'abilità Corpo Ignifugo di Ember (attraversamento lava)
5. **AirBubbleSystem.cs** - Implementa l'abilità Bolla d'Aria di Marina (ossigeno extra)
6. **ControlledGlitchSystem.cs** - Implementa l'abilità Glitch Controllato di Neo (attraversamento barriere)

### Sistema Nemici

1. **EnemyAISystem.cs** - Sistema base per l'intelligenza artificiale dei nemici
2. **BossPhasesSystem.cs** - Gestisce le fasi dei boss con pattern di attacco diversi
3. **PatrolSystem.cs** - Gestisce il movimento di pattuglia dei nemici
4. **AttackPatternSystem.cs** - Gestisce i diversi pattern di attacco dei nemici

### Sistema Eventi

1. **CollisionEventHandler.cs** - Gestisce le collisioni tra entità
2. **AbilityActivatedEvent.cs** - Evento emesso quando un'abilità viene attivata
3. **DamageEventHandler.cs** - Gestisce l'applicazione dei danni
4. **FragmentCollectedEvent.cs** - Evento emesso quando viene raccolto un frammento

### Bridge MonoBehaviour-ECS

1. **PlayerBridge.cs** - Collega i MonoBehaviour del giocatore al sistema ECS
2. **UnityCameraBridge.cs** - Collega la camera di Unity al sistema ECS
3. **InputBridge.cs** - Traduce gli input di Unity in eventi ECS
