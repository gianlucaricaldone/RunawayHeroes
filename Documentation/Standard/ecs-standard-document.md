# RUNAWAY HEROES: STANDARD DI SVILUPPO ECS

Versione 1.0 - Approvata il 28 Aprile 2025

Questo documento definisce lo standard ufficiale da seguire per lo sviluppo del gioco mobile "Runaway Heroes: Escape The Elements" utilizzando l'architettura Entity Component System (ECS). Tutte le parti coinvolte nello sviluppo sono tenute a rispettare queste linee guida per garantire coerenza e qualità.

## 1. PANORAMICA DELL'ARCHITETTURA ECS

L'Entity Component System (ECS) è un pattern architetturale che separa i dati (Componenti) dalla logica (Sistemi), con le Entità che fungono da contenitori per i Componenti. Questa architettura migliora la performance, la modularità e la manutenibilità del codice di gioco.

### 1.1 Elementi Fondamentali

**Entità (Entity)**: Identificatori univoci che rappresentano oggetti di gioco. Non contengono dati o comportamenti, ma solo un ID e riferimenti ai componenti associati.

**Componenti (Component)**: Strutture dati semplici che contengono solo proprietà e nessuna logica. Rappresentano caratteristiche o capacità delle entità.

**Sistemi (System)**: Contengono la logica di gioco e operano su entità che possiedono specifici insiemi di componenti.

## 2. CONVENZIONI DI NAMING

### 2.1 Entità

| Elemento | Convenzione | Esempio |
|----------|-------------|---------|
| Entità | PascalCase | `PlayerEntity`, `DroneEntity` |
| ID Entità | UPPER_SNAKE_CASE | `PLAYER_ENTITY`, `DRONE_ENEMY_ENTITY` |
| Factory di Entità | PascalCase + Factory | `PlayerEntityFactory`, `EnemyFactory` |
| Archetype di Entità | PascalCase + Archetype | `PlayerArchetype`, `DroneArchetype` |

### 2.2 Componenti

| Elemento | Convenzione | Esempio |
|----------|-------------|---------|
| Componente | PascalCase + Component | `HealthComponent`, `MovementComponent` |
| Proprietà | camelCase | `currentHealth`, `maxSpeed` |
| Interfaccia componente | I + PascalCase + Component | `IHealthComponent`, `IPositionComponent` |
| Tag componente | PascalCase + Tag | `PlayerTag`, `EnemyTag` |

### 2.3 Sistemi

| Elemento | Convenzione | Esempio |
|----------|-------------|---------|
| Sistema | PascalCase + System | `MovementSystem`, `CombatSystem` |
| Metodi principali | PascalCase | `Initialize()`, `Update()`, `ProcessEntity()` |
| Query di entità | Get + PascalCase | `GetEntitiesWithHealth()`, `GetMovableEntities()` |
| Job/Worker | PascalCase + Job | `HealthUpdateJob`, `MovementCalculationJob` |

### 2.4 Eventi ECS

| Elemento | Convenzione | Esempio |
|----------|-------------|---------|
| Eventi | PascalCase + Event | `CollisionEvent`, `HealthChangedEvent` |
| Handler di eventi | On + PascalCase | `OnCollision()`, `OnHealthChanged()` |
| Payload di eventi | PascalCase + EventData | `DamageEventData`, `MovementEventData` |

### 2.5 Convenzioni Generali

- Utilizzare PascalCase per nomi di classi, strutture e interfacce
- Utilizzare camelCase per variabili e parametri
- Utilizzare UPPER_SNAKE_CASE per costanti
- Prefissi da evitare: m_, _, s_, g_

## 3. STRUTTURA DEL PROGETTO

### 3.0 Principi di Architettura ECS

L'implementazione ECS in Runaway Heroes segue questi principi architetturali:

1. **Separazione delle Responsabilità**
   - Dati (Componenti)
   - Logica (Sistemi)
   - Identità (Entità)

2. **Organizzazione a Strati**
   ```
   [Presentazione] - UI, effetti visivi, feedback
         ↑
   [Gameplay] - Sistemi di gioco, meccaniche
         ↑
   [Core] - Movimento, fisica, input
         ↑
   [Foundation] - Strutture ECS di base
   ```

3. **Categorizzazione Funzionale**
   - Per dominio (Movement, Combat, Abilities)
   - Per tipo di gioco (Characters, Environment, UI)
   - Per livello di astrazione (Core, Gameplay, Presentation)

4. **Accoppiamento e Coesione**
   - Basso accoppiamento tra sistemi
   - Alta coesione all'interno dei componenti
   - Comunicazione attraverso eventi definiti

### 3.1 Organizzazione delle Cartelle

```
Assets/
  _Project/
    ECS/
      Components/           # Definizioni dei componenti
        Core/               # Componenti base del sistema
        Gameplay/           # Componenti per le meccaniche principali
        Characters/         # Componenti specifici per i personaggi
        Collectibles/       # Componenti per oggetti collezionabili
        World/              # Componenti per elementi del mondo
      Entities/             # Factory e definizioni di entità
        Archetypes/         # Template per la creazione di entità comuni
        Blueprints/         # Configurazioni dettagliate per diversi tipi di entità
      Systems/              # Logica di gioco
        Core/               # Sistemi fondamentali (movimento, fisica)
        Gameplay/           # Sistemi per le meccaniche di gioco
        Character/          # Sistemi per la gestione dei personaggi
        Environment/        # Sistemi per l'interazione con l'ambiente
        Abilities/          # Sistemi per le abilità speciali
      Events/               # Sistema di eventi ECS
      Worlds/               # Configurazioni dei mondi di gioco
      Utilities/            # Utility specifiche per ECS
    Data/                   # Dati e configurazioni
      ScriptableObjects/    # Definizioni di dati per componenti
      Config/               # File di configurazione del sistema ECS
    Runtime/                # Codice di bootstrap e gestione runtime
    Utilities/              # Utility generiche
    Editor/                 # Strumenti e editor custom
```

### 3.2 Struttura dei Componenti

I componenti sono strutture di sola-dati, preferibilmente immutabili:

```csharp
// Esempio di struttura componente
[Serializable]
public struct HealthComponent
{
    public float MaxHealth;     // Valore massimo della salute
    public float CurrentHealth; // Valore attuale della salute
    public bool IsInvulnerable; // Flag di invulnerabilità
    public float RegenRate;     // Tasso di rigenerazione (se presente)
}
```

### 3.3 Struttura dei Sistemi

I sistemi operano sulle entità in base ai componenti che possiedono:

```csharp
// Esempio di sistema
public class HealthSystem : SystemBase
{
    private EntityQuery _healthEntitiesQuery;

    protected override void OnCreate()
    {
        // Definisce quali entità processare (quelle con HealthComponent)
        _healthEntitiesQuery = GetEntityQuery(
            ComponentType.ReadWrite<HealthComponent>(),
            ComponentType.ReadOnly<CharacterStatsComponent>()
        );
    }

    protected override void OnUpdate()
    {
        // Logica da eseguire su tutte le entità rilevanti
        Entities.ForEach((ref HealthComponent health, in CharacterStatsComponent stats) => {
            // Logica da applicare a ogni entità
            if (health.CurrentHealth < health.MaxHealth && stats.CanRegenerate)
            {
                health.CurrentHealth += health.RegenRate * Time.DeltaTime;
                health.CurrentHealth = Math.Min(health.CurrentHealth, health.MaxHealth);
            }
        }).ScheduleParallel();
    }
}
```

## 4. IMPLEMENTAZIONE ECS PER RUNAWAY HEROES

### 4.0 Applicazione al Modello di Gioco

L'architettura ECS è particolarmente adatta per Runaway Heroes grazie a:

1. **Performance su Mobile**: L'ottimizzazione della cache e l'efficienza di memoria sono cruciali su dispositivi mobili
2. **Sistema di Personaggi Intercambiabili**: La meccanica "Risonanza dei Frammenti" si implementa naturalmente tramite manipolazione dei componenti
3. **Ambienti Diversificati**: I sei mondi di gioco possono condividere sistemi mantenendo componenti specifici per ambiente
4. **Scalabilità**: Il sistema supporta facilmente l'aggiunta di nuovi personaggi, abilità e meccaniche

Diagramma di alto livello dell'architettura ECS per Runaway Heroes:

```
[Personaggi Giocabili] -- hanno --> [Componenti Base + Specifici]
                                             |
                                             v
[Sistemi di Gioco] <-- operano su -- [Entità nel Mondo] -- interagiscono con --> [Ambiente/Nemici]
       |                                     ^
       v                                     |
[Eventi di Gioco] -- modificano -- [Stato dei Componenti]
```

### 4.1 Componenti Principali

#### Componenti Base
- `TransformComponent`: Posizione, rotazione, scala
- `PhysicsComponent`: Massa, collisioni, velocità
- `RenderComponent`: Riferimenti visivi, materiali
- `TagComponent`: Etichette per categorizzazione

#### Componenti Specifici del Gioco
- `HealthComponent`: Punti vita, vulnerabilità
- `MovementComponent`: Velocità, capacità di movimento
- `AbilityComponent`: Abilità speciali dei personaggi
- `CollectibleComponent`: Oggetti raccoglibili
- `FocusTimeComponent`: Meccanica di rallentamento
- `FragmentComponent`: Frammenti del Nucleo dell'Equilibrio

### 4.2 Sistemi Principali

#### Sistemi Base
- `TransformSystem`: Gestione delle trasformazioni
- `PhysicsSystem`: Simulazione fisica
- `RenderSystem`: Rendering degli elementi visivi
- `InputSystem`: Gestione degli input utente

#### Sistemi Specifici del Gioco
- `MovementSystem`: Gestione del movimento dei personaggi
- `CollisionSystem`: Rilevamento e risposta alle collisioni
- `AbilitySystem`: Attivazione e gestione delle abilità
- `FocusTimeSystem`: Meccanica del tempo rallentato
- `FragmentResonanceSystem`: Sistema di cambio personaggio
- `EnemyAISystem`: Intelligenza artificiale dei nemici

### 4.3 Eventi Principali

- `CollisionEvent`: Generato quando avviene una collisione
- `HealthChangedEvent`: Cambiamenti nei punti vita
- `AbilityActivatedEvent`: Attivazione di abilità
- `FocusTimeActivatedEvent`: Attivazione del Focus Time
- `FragmentCollectedEvent`: Raccolta di un frammento
- `LevelCompletedEvent`: Completamento di un livello

## 5. CONVENZIONI DEL CODICE

### 5.0 Principi Generali

1. **Separazione Dati e Logica**: Mantenere una rigorosa separazione tra dati (componenti) e logica (sistemi)
2. **Immutabilità dove possibile**: Preferire componenti immutabili quando ha senso
3. **Minimizzare le Dipendenze**: Ogni sistema dovrebbe dipendere solo dai componenti necessari
4. **Componenti Specializzati**: Creare componenti piccoli e specializzati anziché pochi componenti complessi
5. **Visibilità Appropriata**: Utilizzare modificatori di accesso appropriati (private, internal, public)
6. **Comprensibilità**: La chiarezza del codice è prioritaria rispetto a ottimizzazioni premature

### 5.1 Implementazione dei Componenti

```csharp
// Componente per il movimento dei personaggi
[Serializable]
public struct MovementComponent
{
    // Valori base
    public float BaseSpeed;         // Velocità base del personaggio
    public float JumpForce;         // Forza del salto
    public float SlideDuration;     // Durata della scivolata

    // Stato corrente
    public float CurrentSpeed;      // Velocità corrente (modificata da potenziamenti)
    public bool IsJumping;          // Flag per stato di salto
    public bool IsSliding;          // Flag per stato di scivolata
    public float SlideTimeRemaining; // Tempo rimanente nella scivolata

    // Limiti
    public float MaxSpeed;          // Velocità massima raggiungibile
    public int MaxJumps;            // Numero massimo di salti consecutivi
    public int RemainingJumps;      // Salti rimanenti prima di toccare terra
}
```

### 5.2 Implementazione dei Sistemi

```csharp
/// <summary>
/// Sistema che gestisce il movimento dei personaggi
/// </summary>
public class MovementSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem _commandBufferSystem;

    protected override void OnCreate()
    {
        _commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        
        // Definisce le entità su cui operare
        RequireSingletonForUpdate<GameActiveTag>();
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // Aggiorna la velocità in base ai potenziamenti
        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovementComponent movement, in SpeedBoostComponent boost) => 
            {
                if (boost.IsActive)
                {
                    movement.CurrentSpeed = math.min(
                        movement.BaseSpeed * boost.Multiplier, 
                        movement.MaxSpeed
                    );
                }
                else
                {
                    movement.CurrentSpeed = movement.BaseSpeed;
                }
            }).ScheduleParallel();

        // Gestisce il salto
        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovementComponent movement, ref PhysicsComponent physics, in JumpInputComponent jumpInput) => 
            {
                if (jumpInput.JumpPressed && movement.RemainingJumps > 0)
                {
                    physics.Velocity.y = movement.JumpForce;
                    movement.IsJumping = true;
                    movement.RemainingJumps--;
                    
                    // Crea un evento di salto
                    var eventEntity = commandBuffer.CreateEntity(entityInQueryIndex);
                    commandBuffer.AddComponent(entityInQueryIndex, eventEntity, new JumpEventComponent
                    {
                        CharacterEntity = entity,
                        JumpForce = movement.JumpForce
                    });
                }
            }).ScheduleParallel();

        // Altri aggiornamenti relativi al movimento...

        _commandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 5.3 Regioni di Codice

Utilizzare le regioni per organizzare sezioni di codice in file di grandi dimensioni:

```csharp
public class GameplaySystem : SystemBase
{
    #region Initialization
    // Codice di inizializzazione
    #endregion

    #region Entity Queries
    // Definizioni di query
    #endregion

    #region Update Methods
    // Metodi di aggiornamento principali
    #endregion
    
    #region Event Handlers
    // Gestori di eventi
    #endregion
    
    #region Utility Methods
    // Metodi di utilità
    #endregion
}
```

### 5.4 Documentazione del Codice

Ogni componente, sistema ed entità deve essere documentato:

```csharp
/// <summary>
/// Componente che rappresenta l'abilità speciale di un personaggio
/// </summary>
[Serializable]
public struct AbilityComponent
{
    /// <summary>
    /// Tipo di abilità (Scatto Urbano, Richiamo della Natura, ecc.)
    /// </summary>
    public AbilityType Type;
    
    /// <summary>
    /// Tempo di ricarica base in secondi
    /// </summary>
    public float BaseCooldown;
    
    /// <summary>
    /// Tempo di ricarica corrente (modificato da potenziamenti)
    /// </summary>
    public float CurrentCooldown;
    
    /// <summary>
    /// Tempo rimanente prima che l'abilità sia nuovamente disponibile
    /// </summary>
    public float CooldownRemaining;
    
    /// <summary>
    /// Se l'abilità è attualmente attiva
    /// </summary>
    public bool IsActive;
    
    /// <summary>
    /// Durata rimanente se l'abilità è attiva
    /// </summary>
    public float DurationRemaining;
}
```

## 6. OTTIMIZZAZIONE E PERFORMANCE

### 6.0 Strumenti di Profiling

I seguenti strumenti devono essere utilizzati regolarmente per monitorare le performance dell'implementazione ECS:

1. **Unity Profiler**: Analizzare il tempo di CPU per ogni sistema ECS
2. **Frame Debugger**: Verificare il rendering e i batch di entità
3. **Memory Profiler**: Monitorare l'allocazione di memoria
4. **Burst Inspector**: Verificare l'ottimizzazione del codice compilato con Burst
5. **ECS System Metrics**: Implementare metriche custom per monitorare:
   - Tempo di elaborazione per sistema
   - Entità elaborate per frame
   - Allocazioni per frame
   - Cache misses

Stabilire un processo di profiling settimanale per identificare e risolvere precocemente i problemi di performance.

### 6.1 Target di Performance

- **FPS**: 60 fps su dispositivi target
- **Entità simultanee**: max 1500
- **Componenti per entità**: max 12 (media 6-8)
- **Batch size**: 64-128 entità per batch
- **Memoria**: Max 400 MB di RAM utilizzata

### 6.2 Best Practices

1. **Chunk Iteration**: Preferire l'iterazione per chunks invece che per singole entità
2. **Job System**: Utilizzare Burst Compiler e C# Jobs per operazioni parallele
3. **Archetype Chunking**: Raggruppare componenti comunemente usati insieme in archetipi
4. **Shared Components**: Utilizzare componenti condivisi per raggruppamento e rendering
5. **Query Caching**: Precalcolare e riutilizzare le EntityQuery dove possibile
6. **Command Buffers**: Utilizzare EntityCommandBuffers per modifiche strutturali differite

### 6.3 Strategie Anti-Pattern

1. **Evitare GetComponent**: Mai usare GetComponent durante l'aggiornamento
2. **Minimizzare le Allocazioni**: Evitare allocazioni per frame
3. **No Conversioni Costose**: Evitare boxing/unboxing e conversioni di tipo
4. **Ottimizzare le Query**: Non creare query ridondanti 
5. **Limitare i Singleton**: Usare i singleton solo quando necessario

## 7. WORKFLOW DI SVILUPPO

### 7.0 Processi di Sviluppo ECS

1. **Approccio Data-First**: Iniziare definendo i dati (componenti) prima di implementare la logica (sistemi)
2. **Sviluppo Incrementale**: Aggiungere un componente/sistema alla volta e testare frequentemente
3. **Prototipazione Rapida**: Creare prototipi di gameplay con archetipi di entità semplici
4. **Controllo della Crescita delle Entità**: Monitorare il numero e la complessità delle entità
5. **Ciclo Revisione**: Rifattorizzare regolarmente per consolidare componenti e sistemi simili

Schema del processo di sviluppo:
```
Identificazione Requisiti → Modellazione Dati → Definizione Componenti → 
Implementazione Sistemi → Testing → Profiling → Ottimizzazione
```

### 7.1 Creazione di Nuovi Componenti

1. Definire chiaramente lo scopo e i dati necessari
2. Utilizzare tipi di dato appropriati (preferire struct blittable)
3. Limitare la dimensione del componente (idealmente <16 byte)
4. Documentare tutti i campi con commenti XML
5. Implementare IComponentData o ISharedComponentData

### 7.2 Implementazione di Nuovi Sistemi

1. Identificare le entità target attraverso EntityQuery
2. Definire chiaramente le dipendenze del sistema
3. Implementare la logica preferibilmente con Entities.ForEach o IJobChunk
4. Gestire correttamente le dipendenze dei job
5. Utilizzare command buffer per modifiche strutturali

### 7.3 Testing e Debugging

1. Implementare unit test per componenti e sistemi
2. Utilizzare Entity Debugger per ispezionare lo stato del mondo ECS
3. Profilare regolarmente le performance con il Profiler di Unity
4. Verificare l'uso della memoria con Memory Profiler
5. Implementare sistemi di logging dedicati per ECS

## 8. STRUMENTI E UTILITY

### 8.0 Gestione della Complessità

Per gestire la complessità in un sistema ECS su larga scala come Runaway Heroes:

1. **Visualizzazione delle Dipendenze**
   - Generare automaticamente grafici di dipendenza tra sistemi
   - Mappare componenti condivisi tra entità diverse
   - Visualizzare il flusso di dati attraverso il sistema

2. **Meccanismi di Logging ECS**
   ```csharp
   public static class ECSLogger
   {
       [Conditional("ENABLE_ECS_LOGGING")]
       public static void LogEntityCreation(Entity entity, EntityManager manager, string context)
       {
           var components = manager.GetComponentTypes(entity);
           Debug.Log($"[ECS] Entity created: {entity.Index}.{entity.Version} - Context: {context}");
           Debug.Log($"[ECS] Components: {string.Join(", ", components.Select(c => c.GetManagedType().Name))}");
       }
       
       // Altri metodi di logging...
   }
   ```

3. **Automazione e Tooling**
   - Script per generare componenti e sistemi scheletro
   - Validatori automatici di conformità agli standard
   - Sanity check per rilevare pattern problematici

### 8.1 Strumenti Consigliati

1. **Entity Debugger**: Visualizzazione e debug delle entità
2. **Burst Inspector**: Analisi del codice compilato con Burst
3. **Memory Profiler**: Analisi dell'utilizzo della memoria
4. **Custom Entity Gizmos**: Visualizzazione personalizzata per debug

### 8.2 Utility Helper

```csharp
public static class ECSHelper
{
    /// <summary>
    /// Crea un'entità pre-configurata per un personaggio
    /// </summary>
    public static Entity CreateCharacterEntity(EntityManager entityManager, CharacterType type, Vector3 position)
    {
        // Implementazione
    }
    
    /// <summary>
    /// Ottiene tutte le entità in un raggio specifico
    /// </summary>
    public static NativeArray<Entity> GetEntitiesInRadius(EntityManager entityManager, Vector3 center, float radius)
    {
        // Implementazione
    }
    
    // Altre utility...
}
```

## 9. INTEGRAZIONE CON UNITY

### 9.1 MonoBehaviour <-> ECS Bridge

Per componenti che devono interfacciarsi con sistemi legacy:

```csharp
/// <summary>
/// Componente ponte che connette un GameObject di Unity con un'entità ECS
/// </summary>
public class EntityBridge : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Aggiunge componenti ECS in base alle proprietà del GameObject
    }
}
```

### 9.2 Authoring Components

Per configurare componenti nell'editor di Unity:

```csharp
/// <summary>
/// Componente di authoring per configurare un personaggio nell'editor
/// </summary>
public class CharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Statistiche Base")]
    public float MaxHealth = 100f;
    public float MovementSpeed = 5f;
    public float JumpForce = 10f;
    
    [Header("Abilità")]
    public AbilityType SpecialAbility;
    public float AbilityCooldown = 15f;
    public float AbilityDuration = 5f;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Converte le impostazioni di authoring in componenti ECS
        dstManager.AddComponentData(entity, new HealthComponent
        {
            MaxHealth = MaxHealth,
            CurrentHealth = MaxHealth
        });
        
        dstManager.AddComponentData(entity, new MovementComponent
        {
            BaseSpeed = MovementSpeed,
            CurrentSpeed = MovementSpeed,
            JumpForce = JumpForce
        });
        
        // Altre conversioni...
    }
}
```

## 10. CONFORMITÀ E ADOZIONE

Questo documento rappresenta lo standard ufficiale per l'implementazione ECS in Runaway Heroes. Tutti i membri del team di sviluppo sono tenuti ad aderire a queste linee guida. Per proposte di modifica a questo standard, contattare il responsabile tecnico del progetto.

### 10.0 Transizione da MonoBehaviour a ECS

La migrazione dal codice esistente basato su MonoBehaviour all'architettura ECS seguirà questo piano:

1. **Fase 1: Componenti Dati Puri**
   - Identificare i dati nelle classi MonoBehaviour esistenti
   - Estrarli in strutture di componenti ECS puri
   - Mantenere la compatibilità con vecchi sistemi tramite componenti bridge

2. **Fase 2: Sistemi Ibridi**
   - Creare sistemi ECS che lavorano in parallelo ai MonoBehaviour
   - Implementare meccaniche nuove direttamente in ECS
   - Mantenere MonoBehaviour solo per interazione con sistemi Unity legacy

3. **Fase 3: ECS Nativo**
   - Sostituire tutti i MonoBehaviour core con pure implementazioni ECS
   - Convertire la logica di gioco in sistemi ECS
   - Utilizzare MonoBehaviour solo come layer di presentazione

4. **Fase 4: Ottimizzazione Completa**
   - Rivedere tutti i sistemi per massimizzare la performance
   - Implementare rendering ottimizzato per ECS
   - Eliminare i componenti bridge non più necessari

Pianificazione temporale:
- Fase 1: 1 mese
- Fase 2: 2 mesi
- Fase 3: 3 mesi
- Fase 4: Continua

### 10.1 Controllo di Conformità

La conformità a questi standard verrà verificata attraverso:
- Code review regolari
- Analisi statica del codice
- Checklist di verifica pre-commit
- Meetup di allineamento tecnico

### 10.2 Risorse di Apprendimento

- Documentazione ufficiale Unity DOTS
- Wiki interna del progetto
- Tutorial interni sull'implementazione ECS
- Sessioni di formazione periodiche

Fine del documento