# RUNAWAY HEROES: STANDARD DI SVILUPPO ECS

Versione 1.0 - Allineato con standard ECS ufficiale (28 Aprile 2025)

**Versioni utilizzate:**
- Unity Editor Version: 6000.0.47f1
- Entities: 1.3.14

## 1. ARCHITETTURA ECS

- **Entità**: Identificatori univoci che rappresentano oggetti di gioco
- **Componenti**: Strutture dati semplici che contengono solo proprietà, no logica
- **Sistemi**: Contengono la logica di gioco, operano su entità con specifici componenti
- **Eventi**: Usati per comunicazioni tra sistemi disaccoppiati

### Principi chiave
- Separazione dati (Components) e logica (Systems)
- Organizzazione a strati: Presentazione, Gameplay, Core, Foundation
- Basso accoppiamento tra sistemi, alta coesione nei componenti
- Comunicazione attraverso eventi ben definiti

## 2. CONVENZIONI DI NAMING

### Entità
- Entità: PascalCase - `PlayerEntity`, `DroneEntity`
- ID Entità: UPPER_SNAKE_CASE - `PLAYER_ENTITY`, `DRONE_ENEMY_ENTITY`
- Factory: PascalCase + Factory - `PlayerEntityFactory`, `EnemyFactory`
- Archetype: PascalCase + Archetype - `PlayerArchetype`, `DroneArchetype`

### Componenti
- Componente: PascalCase + Component - `HealthComponent`, `MovementComponent`
- Proprietà: camelCase - `currentHealth`, `maxSpeed`
- Interfaccia: I + PascalCase + Component - `IHealthComponent`
- Tag: PascalCase + Tag - `PlayerTag`, `EnemyTag`

### Sistemi
- Sistema: PascalCase + System - `MovementSystem`, `CombatSystem`
- Metodi: PascalCase - `Initialize()`, `Update()`, `ProcessEntity()`
- Query: Get + PascalCase - `GetEntitiesWithHealth()`, `GetMovableEntities()`
- Job: PascalCase + Job - `HealthUpdateJob`, `MovementCalculationJob`

### Eventi
- Eventi: PascalCase + Event - `CollisionEvent`, `HealthChangedEvent`
- Handler: On + PascalCase - `OnCollision()`, `OnHealthChanged()`
- Payload: PascalCase + EventData - `DamageEventData`, `MovementEventData`

### Generali
- Classi/strutture/interfacce: PascalCase
- Variabili/parametri: camelCase
- Costanti: UPPER_SNAKE_CASE
- Evitare prefissi: m_, s_, g_

## 3. STRUTTURA DEL PROGETTO

```
Assets/
  _Project/
    ECS/
      Components/           # Definizioni dei componenti
        Core/               # Componenti base del sistema
        Gameplay/           # Componenti per meccaniche principali
        Characters/         # Componenti specifici personaggi
        Collectibles/       # Componenti per oggetti collezionabili
        World/              # Componenti per elementi del mondo
      Entities/             # Factory e definizioni di entità
        Archetypes/         # Template per entità comuni
        Blueprints/         # Configurazioni dettagliate
      Systems/              # Logica di gioco
        Core/               # Sistemi fondamentali
        Gameplay/           # Sistemi per meccaniche di gioco
        Character/          # Sistemi per gestione personaggi
        Environment/        # Sistemi per interazione ambiente
        Abilities/          # Sistemi per abilità speciali
      Events/               # Sistema di eventi ECS
      Worlds/               # Configurazioni dei mondi
      Utilities/            # Utility specifiche per ECS
    Data/                   # Dati e configurazioni
    Runtime/                # Codice di bootstrap e gestione
    Utilities/              # Utility generiche
    Editor/                 # Strumenti e editor custom
```

## 4. COMPONENTI E SISTEMI PRINCIPALI

### Componenti base
- `TransformComponent`: Posizione, rotazione, scala
- `PhysicsComponent`: Massa, collisioni, velocità
- `RenderComponent`: Riferimenti visivi, materiali
- `TagComponent`: Etichette per categorizzazione

### Componenti di gioco
- `HealthComponent`: Punti vita, vulnerabilità
- `MovementComponent`: Velocità, capacità di movimento
- `AbilityComponent`: Abilità speciali dei personaggi
- `FocusTimeComponent`: Meccanica di rallentamento
- `FragmentComponent`: Frammenti del Nucleo dell'Equilibrio

### Sistemi principali
- `TransformSystem`: Gestione trasformazioni
- `PhysicsSystem`: Simulazione fisica
- `InputSystem`: Gestione input utente
- `MovementSystem`: Gestione movimento personaggi
- `CollisionSystem`: Rilevamento e risposta collisioni
- `AbilitySystem`: Attivazione e gestione abilità
- `FocusTimeSystem`: Meccanica del tempo rallentato
- `FragmentResonanceSystem`: Sistema di cambio personaggio

## 5. CONVENZIONI DEL CODICE

### Implementazione componenti
```csharp
// Componente per il movimento dei personaggi
[Serializable]
public struct MovementComponent
{
    // Valori base
    public float BaseSpeed;        // Velocità base del personaggio
    public float JumpForce;        // Forza del salto
    public float SlideDuration;    // Durata della scivolata
    
    // Stato corrente
    public float CurrentSpeed;     // Velocità corrente
    public bool IsJumping;         // Flag per stato di salto
    public bool IsSliding;         // Flag per stato di scivolata
    
    // Limiti
    public float MaxSpeed;         // Velocità massima raggiungibile
    public int MaxJumps;           // Numero massimo di salti consecutivi
    public int RemainingJumps;     // Salti rimanenti
}
```

### Implementazione sistemi
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
        RequireSingletonForUpdate<GameActiveTag>();
    }
    
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        
        // Operazioni su entità
        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int entityInQueryIndex, ref MovementComponent movement) =>
            {
                // Logica movimento
            }).ScheduleParallel();
            
        _commandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### Documentazione
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
    
    // Altri campi...
}
```

### Regioni di codice
```csharp
public class GameplaySystem : SystemBase
{
    #region Initialization
    // Codice di inizializzazione
    #endregion
    
    #region Entity Queries
    // Definizioni di query
    #endregion
    
    // Altre regioni...
}
```

## 6. OTTIMIZZAZIONE E PERFORMANCE

### Target di performance
- **FPS**: 60 fps su dispositivi target
- **Entità simultanee**: max 1500
- **Componenti per entità**: max 12 (media 6-8)
- **Batch size**: 64-128 entità per batch
- **Memoria**: Max 400 MB di RAM utilizzata

### Best practices
- Chunk Iteration: Preferire iterazione per chunks
- Job System: Utilizzare Burst Compiler e C# Jobs
- Archetype Chunking: Raggruppare componenti comunemente usati
- Shared Components: Utilizzare per raggruppamento e rendering
- Query Caching: Precalcolare e riutilizzare EntityQuery
- Command Buffers: Utilizzare per modifiche strutturali differite

### Anti-pattern
- Evitare GetComponent durante l'aggiornamento
- Minimizzare allocazioni per frame
- Evitare boxing/unboxing e conversioni di tipo
- Non creare query ridondanti
- Limitare l'uso di singleton

### Strumenti di profiling
- Unity Profiler: Analizzare tempo CPU per sistema
- Frame Debugger: Verificare rendering e batch
- Memory Profiler: Monitorare allocazione memoria
- Burst Inspector: Verificare ottimizzazione codice

## 7. WORKFLOW DI SVILUPPO

### Approccio
1. Data-First: Definire componenti prima della logica
2. Sviluppo Incrementale: Un componente/sistema alla volta
3. Prototipazione Rapida: Usare archetipi semplici
4. Controllo Crescita Entità: Monitorare numero e complessità
5. Ciclo Revisione: Rifattorizzare regolarmente

### Creazione componenti
1. Definire scopo e dati necessari
2. Usare tipi appropriati (preferire struct blittable)
3. Limitare dimensione (<16 byte idealmente)
4. Documentare tutti i campi
5. Implementare IComponentData o ISharedComponentData

### Implementazione sistemi
1. Identificare entità target con EntityQuery
2. Definire dipendenze del sistema
3. Implementare logica con Entities.ForEach o IJobChunk
4. Gestire dipendenze dei job
5. Usare command buffer per modifiche strutturali

### Testing e debugging
1. Implementare unit test per componenti e sistemi
2. Usare Entity Debugger per ispezionare lo stato
3. Profilare performance regolarmente
4. Verificare uso memoria
5. Implementare log dedicati per ECS

## 8. STRUMENTI E UTILITY

### Utility Helper
```csharp
public static class ECSHelper
{
    /// <summary>
    /// Crea un'entità pre-configurata per un personaggio
    /// </summary>
    public static Entity CreateCharacterEntity(EntityManager entityManager, CharacterType type)
    {
        // Implementazione
    }
    
    /// <summary>
    /// Ottiene tutte le entità in un raggio specifico
    /// </summary>
    public static NativeArray<Entity> GetEntitiesInRadius(EntityManager entityManager, 
                                                          float3 position, float radius)
    {
        // Implementazione
    }
}
```

### Logging ECS
```csharp
public static class ECSLogger
{
    [Conditional("ENABLE_ECS_LOGGING")]
    public static void LogEntityCreation(Entity entity, EntityManager manager, string context)
    {
        var components = manager.GetComponentTypes(entity);
        Debug.Log($"[ECS] Entity created: {entity.Index}.{entity.Version} - Context: {context}");
        Debug.Log($"[ECS] Components: {string.Join(", ", 
                   components.Select(c => c.GetManagedType().Name))}");
    }
}
```

## 9. INTEGRAZIONE CON UNITY

### MonoBehaviour <-> ECS Bridge
```csharp
/// <summary>
/// Componente di authoring per configurare un personaggio nell'editor
/// </summary>
public class CharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Statistiche Base")]
    public float MaxHealth = 100f;
    public float MovementSpeed = 5f;
    
    public void Convert(Entity entity, EntityManager dstManager, 
                        GameObjectConversionSystem conversionSystem)
    {
        // Converte configurazioni in componenti ECS
        dstManager.AddComponentData(entity, new HealthComponent
        {
            MaxHealth = MaxHealth,
            CurrentHealth = MaxHealth
        });
        
        // Altre conversioni...
    }
}
```

## 10. TRANSIZIONE E CONFORMITÀ

### Piano di transizione da MonoBehaviour a ECS
1. **Fase 1**: Componenti Dati Puri (1 mese)
2. **Fase 2**: Sistemi Ibridi (2 mesi)
3. **Fase 3**: ECS Nativo (3 mesi)  
4. **Fase 4**: Ottimizzazione Continua

### Verifiche di conformità
- Code review regolari
- Analisi statica del codice
- Checklist pre-commit
- Meetup tecnici

### Risorse di apprendimento
- Documentazione ufficiale Unity DOTS
- Wiki interna del progetto
- Tutorial interni ECS
- Sessioni di formazione periodiche

## 11. NOTE SPECIFICHE PER BURST COMPILER

### Timestamp in sistemi Burst-compatibili

Per generare timestamp all'interno di sistemi Burst-compatibili, occorre usare la seguente tecnica:

```csharp
// NON USARE: Non compatibile con Burst Compiler
// long timestamp = DateTime.Now.Ticks;

// USARE INVECE: Approccio compatibile con Burst Compiler
long timestamp = (long)(SystemAPI.Time.ElapsedTime * 10000000);
```

Questo convertirà il tempo trascorso dall'avvio in un formato simile al formato ticks di DateTime.Now, moltiplicandolo per 10,000,000 (numero di ticks in un secondo).

### Tipi non supportati da Burst

Burst Compiler non supporta:
- `System.DateTime` e tipi simili dal namespace System
- Tipi di riferimento (classi) generici
- Tipi gestiti (managed types) come MonoBehaviour, ScriptableObject o altre classi Unity
- La classe `Unity.Entities.World` e molti altri tipi in Unity.Entities che sono classi anziché strutture
- Array di tipi managed come `ComponentType[]` (anche se ComponentType è una struct, l'array stesso è managed)
- Reflection
- Eccezioni a runtime
- `dynamic` o `RTTI`

Quando si definisce un sistema con l'attributo `[BurstCompile]`, assicurarsi che:
1. Tutti i tipi di dati utilizzati siano blittable o struct semplici
2. Non si utilizzi reflection o codice dinamico
3. Non si utilizzino API di Unity non supportate da Burst (come MonoBehaviour, GameObject, ecc.)
4. Non si dichiarino campi di tipo classe (reference type/managed type) all'interno del sistema

### Attributi utili

- `[BurstCompile]`: Applica compilazione Burst a un job o sistema
- `[BurstDiscard]`: Esclude una sezione di codice dalla compilazione Burst
- `[NativeDisableParallelForRestriction]`: Consente accesso in parallelo a una NativeContainer

### Gestire tipi gestiti con Burst Compiler

Quando è necessario utilizzare tipi gestiti (managed types) in un sistema che si desidera ottimizzare con Burst, ci sono diversi approcci:

1. **Rimuovere l'attributo `[BurstCompile]`**: Se il sistema richiede estensivamente tipi gestiti, potrebbe essere meglio rimuovere completamente Burst.

   ```csharp
   // Esempio reale: TutorialProgressionSystem che utilizza tipi gestiti, senza Burst
   // [BurstCompile] <-- Rimosso per consentire l'uso di tipi gestiti
   public partial struct TutorialProgressionSystem : ISystem
   {
       // Riferimenti a tipi gestiti - incompatibili con Burst
       private RunawayHeroes.Runtime.Levels.TutorialLevelInitializer _tutorialManager;
       private int _totalAvailableTutorials;
       
       public void OnStartRunning(ref SystemState state)
       {
           // Questo metodo accede a un tipo gestito (MonoBehaviour)
           _tutorialManager = UnityEngine.Object.FindFirstObjectByType<RunawayHeroes.Runtime.Levels.TutorialLevelInitializer>();
           if (_tutorialManager != null && _tutorialManager.tutorialSequence != null)
           {
               _totalAvailableTutorials = _tutorialManager.tutorialSequence.Length;
           }
       }
       
       // Implementazione...
   }
   ```

   ```csharp
   // Esempio reale: WorldProgressionSystem che utilizza World (tipo managed)
   // [BurstCompile] <-- Rimosso per consentire l'uso di World (tipo managed)
   public partial struct WorldProgressionSystem : ISystem
   {
       // Reference to World (tipo managed, non compatibile con Burst)
       private Unity.Entities.World _world;
       
       public void OnCreate(ref SystemState state)
       {
           // Salva riferimento al World corrente
           _world = state.World;
       }
       
       // In un metodo utilizziamo questo riferimento
       private Entity GetOrCreateWorldProgressionEntity(int worldIndex, ref SystemState state)
       {
           // ...
           
           if (worldEntity == Entity.Null)
           {
               // Recupera SystemState utilizzando _world (riferimento managed)
               var systemState = _world.Unmanaged.GetExistingSystemState<WorldProgressionSystem>();
               // ...
           }
           
           // ...
       }
   }
   ```

2. **Utilizzare componenti singleton**: Spostare i riferimenti gestiti in singleton IComponentData che possono essere interrogati quando necessario.

   ```csharp
   // Componente singleton per riferimenti gestiti
   public struct TutorialManagerReference : IComponentData
   {
       public Entity TutorialManagerEntity;
   }
   
   // Nel sistema [BurstCompile], usare il riferimento
   [BurstCompile]
   public partial struct TutorialSystem : ISystem
   {
       public void OnUpdate(ref SystemState state)
       {
           // Accedere al singleton solo quando necessario
           var tutorialRef = SystemAPI.GetSingleton<TutorialManagerReference>();
           // ...
       }
   }
   ```

3. **Usare `[BurstDiscard]` per sezioni specifiche**: Annotare solo i metodi che accedono a tipi gestiti.

   ```csharp
   [BurstCompile]
   public partial struct HybridSystem : ISystem
   {
       [BurstDiscard]
       private void ProcessManagedTypes()
       {
           // Codice che utilizza tipi gestiti
           var manager = UnityEngine.Object.FindObjectOfType<GameManager>();
           // ...
       }
       
       [BurstCompile]
       public void OnUpdate(ref SystemState state)
       {
           // Logica compatibile con Burst
           // ...
           
           // Chiamata a codice gestito
           ProcessManagedTypes();
       }
   }
   ```

### Utilizzare tipi di array compatibili con Burst

Per evitare problemi con gli array gestiti, utilizza alternative da Unity.Collections:

```csharp
// Utilizzo corretto di array in Burst
[BurstCompile]
public partial struct BurstFriendlyJob : IJobEntity
{
    // Array di tipo nativo compatibile con Burst
    [ReadOnly] public NativeArray<int> ValidIndices;
    [ReadOnly] public NativeArray<float3> Positions;
    
    // Buffer di scrittura
    public NativeList<Entity> ResultEntities;
    
    // Altre opzioni utili
    // public NativeHashMap<int, float> MappingValues;
    // public NativeQueue<Entity> ProcessQueue;
    
    public void Execute(Entity entity)
    {
        // Ora puoi accedere agli array in modo sicuro
        for (int i = 0; i < ValidIndices.Length; i++)
        {
            int index = ValidIndices[i];
            
            // Esempio di operazione con array
            float3 position = Positions[index];
            
            // Aggiungi all'output
            if (math.length(position) > 5f)
                ResultEntities.Add(entity);
        }
    }
}

// Preparazione per un job
void PrepareJob()
{
    // Sempre ricordare di inizializzare con Allocator
    var indices = new NativeArray<int>(10, Allocator.TempJob);
    var positions = new NativeArray<float3>(10, Allocator.TempJob);
    var results = new NativeList<Entity>(Allocator.TempJob);
    
    try
    {
        // Configura il job
        var job = new BurstFriendlyJob
        {
            ValidIndices = indices,
            Positions = positions,
            ResultEntities = results
        };
        
        // Esegui il job
        job.Schedule().Complete();
        
        // Usa i risultati
        foreach (var result in results)
        {
            // Fai qualcosa con result
        }
    }
    finally
    {
        // IMPORTANTE: Rilascia sempre le risorse
        indices.Dispose();
        positions.Dispose();
        results.Dispose();
    }
}
```

4. **Separare in sistemi diversi**: Dividere la logica in un sistema Burst-compatibile e uno per i tipi gestiti.

   ```csharp
   // Sistema per la gestione dei tipi gestiti
   public partial class ManagedReferencesSystem : SystemBase
   {
       protected override void OnUpdate()
       {
           // Logica che utilizza tipi gestiti
       }
   }
   
   // Sistema ottimizzato con Burst
   [BurstCompile]
   public partial struct BurstOptimizedSystem : ISystem
   {
       // Solo logica compatibile con Burst
   }
   ```

5. **Evitare la creazione di array gestiti**: Gli array classici in C# sono tipi managed, anche se contengono struct.

   ```csharp
   // Esempio problematico: creazione di array in job Burst-compiled
   [BurstCompile] // <-- Problematico!
   public partial struct ProcessLevelRequestsJob : IJobEntity
   {
       public EntityManager EntityManager;
   
       private void GenerateLevel()
       {
           // Errore: La creazione di questo array non è compatibile con Burst
           var difficultyQuery = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<WorldDifficultyConfigComponent>());
       }
   }
   
   // Soluzione: rimuovere BurstCompile o usare un approccio alternativo
   public partial struct ProcessLevelRequestsJob : IJobEntity // [BurstCompile] rimosso
   {
       public EntityManager EntityManager;
   
       private void GenerateLevel()
       {
           // Ancora non compatibile con Burst a causa di EntityManager,
           // ma evita la creazione di un array di ComponentType
           var difficultyQuery = EntityManager.CreateEntityQuery(typeof(WorldDifficultyConfigComponent));
       }
   }
   ```

---

Per il documento completo di standard ECS, fare riferimento a:
`/Users/gianlucaricaldone/Progetti/RunawayHeroes/Documentation/Standard/runaway-heroes-ecs-standard.md`

Per file di progettazione fai riferimento ai pdf nella cartella:
`/Users/gianlucaricaldone/Progetti/RunawayHeroes/Documentation/`

Per il GDD del gioco fare riferimento a:
`/Users/gianlucaricaldone/Progetti/RunawayHeroes/Documentation/Game Design Document.pdf`

Utilizza per capire la struttura delle classi il documento di skeleton a:
`/Users/gianlucaricaldone/Progetti/RunawayHeroes/Documentation/class_skeleton_complete.txt`

Utilizza per capire la struttura delle cartelle il file:
`/Users/gianlucaricaldone/Progetti/RunawayHeroes/Documentation/unity_folder_structure.txt`