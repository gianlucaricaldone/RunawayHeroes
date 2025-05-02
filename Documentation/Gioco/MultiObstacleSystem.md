# Sistema Multi-Ostacoli per Livelli Tutorial

## Panoramica

Questo sistema permette di configurare e generare diversi tipi di ostacoli all'interno di uno stesso scenario didattico. Consente una maggiore varietà e flessibilità nella creazione di livelli tutorial, avvicinandosi all'esperienza di gioco completa.

## Componenti Principali

### 1. ObstacleSetup

Struttura che definisce come configurare un tipo di ostacolo in uno scenario:

```csharp
public struct ObstacleSetup
{
    // Codice identificativo dell'ostacolo (es. U01, C02, F03)
    public string obstacleCode;
    
    // Numero di ostacoli di questo tipo da spawnnare
    public int count;
    
    // Tipo di posizionamento (Center, Left, Right, Random, Pattern)
    public ObstaclePlacement placement;
    
    // Randomizzazione altezza e scala
    public bool randomizeHeight;
    public Vector2 heightRange;
    public bool randomizeScale;
    public Vector2 scaleRange;
    
    // Offset di partenza
    public float startOffset;
}
```

### 2. TutorialScenario

Struttura che definisce uno scenario di insegnamento all'interno di un tutorial:

```csharp
public struct TutorialScenario
{
    // Nome identificativo dello scenario
    public string name;
    
    // Distanza dall'inizio del livello
    public float distanceFromStart;
    
    // Array di configurazioni per i diversi tipi di ostacoli
    public ObstacleSetup[] obstacles;
    
    // Messaggio di istruzione e durata
    public string instructionMessage;
    public float messageDuration;
    
    // Configurazione posizionamento
    public bool randomPlacement;
    public float obstacleSpacing;
}
```

### 3. TutorialGuidanceSystem

Sistema ECS che gestisce l'avanzamento del tutorial e la generazione degli scenari didattici:

```csharp
public partial class TutorialGuidanceSystem : SystemBase
{
    // Metodi chiave:
    
    // 1. SpawnObstaclesForScenario - Genera ostacoli per uno scenario
    private void SpawnObstaclesForScenario(/* params */);
    
    // 2. SpawnTutorialObstaclesAdvanced - Implementa la logica di spawning avanzata
    private void SpawnTutorialObstaclesAdvanced(/* params */);
    
    // 3. SpawnObstacle - Factory per creare l'entità ostacolo
    private Entity SpawnObstacle(/* params */);
}
```

### 4. TutorialLevelInitializer

Classe MonoBehaviour che inizializza i livelli tutorial con le sequenze di scenari:

```csharp
public class TutorialLevelInitializer : MonoBehaviour
{
    // Livelli tutorial in sequenza
    public TutorialLevelSequence[] tutorialSequence;
    
    // Metodi principali:
    public void InitializeTutorialLevel();
    public void AdvanceToNextTutorialSequence();
    private void SetupTutorialScenarios(TutorialLevelSequence tutorial);
}
```

### 5. Editor Custom

`TutorialScenarioEditor` fornisce un'interfaccia editor avanzata per configurare facilmente gli scenari e gli ostacoli.

## Strategie di Posizionamento

Il sistema supporta cinque diverse strategie di posizionamento degli ostacoli:

1. **Center**: Posiziona gli ostacoli al centro della corsia
2. **Left**: Posiziona gli ostacoli sul lato sinistro della corsia
3. **Right**: Posiziona gli ostacoli sul lato destro della corsia
4. **Random**: Posiziona gli ostacoli in posizioni casuali nella corsia
5. **Pattern**: Distribuisce gli ostacoli in un pattern attraverso la corsia

## Randomizzazione

Per ogni tipo di ostacolo è possibile configurare:

1. **Randomizzazione dell'altezza**: Altezze casuali entro un range definito
2. **Randomizzazione della scala**: Dimensioni casuali entro un range definito
3. **Posizionamento casuale**: Distribuzione Z casuale invece che equidistante

## Esempio di Configurazione

```csharp
// Configurazione per uno scenario che insegna movimenti combinati
public static TutorialScenario CreateCombinedMovesScenario(float distance)
{
    var jumpObstacles = ObstacleSetup.CreateJumpObstacles("U02", 2);
    var slideObstacles = ObstacleSetup.CreateSlideObstacles("U03", 2);
    var patternObstacles = ObstacleSetup.CreateSideStepObstacles("U01", 3);
    patternObstacles.placement = ObstaclePlacement.Pattern;
    
    return new TutorialScenario
    {
        name = "Combined_Training",
        distanceFromStart = distance,
        obstacles = new ObstacleSetup[] 
        {
            jumpObstacles,
            slideObstacles,
            patternObstacles
        },
        instructionMessage = "Combina i movimenti per superare ostacoli diversi!",
        messageDuration = 5f,
        randomPlacement = true,
        obstacleSpacing = 5f
    };
}
```

## Codici degli Ostacoli

I codici degli ostacoli seguono una convenzione specifica:

- `U##`: Ostacoli universali (presenti in tutti i mondi)
- `C##`: Ostacoli della città
- `F##`: Ostacoli della foresta
- `T##`: Ostacoli della tundra
- `V##`: Ostacoli del vulcano
- `A##`: Ostacoli dell'abisso
- `D##`: Ostacoli del mondo digitale

## Come Usare il Sistema

1. **Configura le Sequenze Tutorial**:
   - Crea un GameObject con il componente `TutorialLevelInitializer`
   - Usa l'editor custom per configurare le sequenze e gli scenari

2. **Implementa Scenari Specifici**:
   - Crea metodi factory per scenari specifici come salto, scivolata, ecc.
   - Componi scenari complessi con molteplici tipi di ostacoli

3. **Gestione Avanzamento**:
   - Usa `AdvanceToNextTutorialSequence()` per passare al tutorial successivo
   - Controlla il completamento con il flag `TutorialLevelTag.Completed`

## Integrazione con ECS

Il sistema è completamente integrato con l'architettura ECS:

1. **Componenti**: `TutorialScenarioComponent`, `TutorialObstacleBuffer`, `TutorialLevelTag`
2. **Sistema**: `TutorialGuidanceSystem` gestisce l'avanzamento e lo spawning
3. **Eventi**: Possono essere aggiunti per notificare completamento scenari, attivazione messaggi, ecc.

## Debug Visuale

L'editor include funzionalità di debug visuale:
- Visualizzazione degli scenari nel world editor
- Gizmi colorati per differenti tipi di ostacoli e posizionamenti
- Controlli rapidi con preset predefiniti