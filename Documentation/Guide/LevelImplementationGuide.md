# Guida all'Implementazione Grafica di un Livello in Runaway Heroes

## 1. Panoramica

Runaway Heroes utilizza un'architettura ECS (Entity Component System) per la gestione dei livelli, combinata con elementi Unity tradizionali per la rappresentazione grafica. Questa guida ti mostrerà come implementare un nuovo livello seguendo l'approccio utilizzato nel tutorial.

## 2. Struttura dei Livelli

I livelli in Runaway Heroes sono composti da:

- **Segmenti di Percorso**: Sezioni del livello con caratteristiche specifiche
- **Scenari**: Aree con obiettivi didattici o sfide specifiche
- **Ostacoli**: Elementi che il giocatore deve evitare o superare
- **Temi Visivi**: Determinano l'aspetto estetico del mondo

## 3. Strumenti e File Necessari

1. Unity Editor (versione 6000.0.47f1)
2. Progetto Runaway Heroes
3. Assets grafici per il tema del mondo scelto

## 4. Procedura di Implementazione

### Passo 1: Creazione della Scena di Base

1. Crea una nuova scena Unity in `Assets/Scenes/[NomeMondi]`
2. Aggiungi un GameObject vuoto come root e nominalo `LevelManager`
3. Aggiungi il componente `TutorialLevelInitializer` al GameObject

```csharp
// Configurazione base del TutorialLevelInitializer
- tutorialTheme: [Scegli il tema dal WorldTheme enum]
- tutorialLength: [Lunghezza in metri - suggerito 500-1000]
- seed: [Seed per generazione randomica - 0 per casuale]
```

> **Nota sul parametro seed**: Il valore del seed non è direttamente visibile nell'interfaccia di Unity per i componenti ECS. Viene applicato attraverso codice nel `TutorialTestHelper` che crea dietro le quinte entità ECS con il componente `RunnerLevelConfigComponent`, con il seed specificato nel `TutorialLevelInitializer`. Se desideri generare livelli con un seed specifico, puoi utilizzare lo strumento "Tutorial Test Tool" disponibile nel menu "Runaway Heroes", che permette di impostare il seed prima della generazione del livello.

### Passo 2: Definizione della Sequenza di Livello

1. Nell'Inspector, espandi la sezione "Livelli" del `TutorialLevelInitializer`
2. Configura la proprietà `tutorialSequence` definendo i livelli tutorial. Quando modifichi la "Sequence Size", si creeranno tutorial vuoti che potrai espandere facendo clic sul foldout.
3. Per ciascun tutorial espanso, imposta le seguenti proprietà:

```
- Description: Descrizione del livello
- Theme: Tema del livello (WorldTheme enum)
- Length: Lunghezza in metri (300-1000)
- Difficulty: Livello di difficoltà (1-10)
- Obstacle Density: Densità degli ostacoli (0-1)
- Enemy Density: Densità dei nemici (0-1)
- Teaching Scenarios: Scenari di insegnamento (espandi per configurare)
```

> **Nota sulla densità degli ostacoli e nemici**: I parametri `Obstacle Density` e `Enemy Density` sono disponibili nell'editor personalizzato quando espandi un tutorial nella sequenza. Questi parametri vengono utilizzati durante la generazione del livello per determinare quanti ostacoli e nemici generare. Un valore più alto significa più ostacoli/nemici.

### Passo 3: Creazione degli Scenari

Ogni livello contiene scenari che definiscono cosa il giocatore incontrerà e quando:

1. Crea scenari didattici utilizzando la classe `TutorialScenario`:

```csharp
// Esempio di creazione scenari
TutorialScenario[] scenarios = new TutorialScenario[] {
    // Scenario per insegnare il salto
    TutorialScenario.CreateJumpScenario(50f), // a 50 metri dall'inizio
    
    // Scenario per insegnare la scivolata
    TutorialScenario.CreateSlideScenario(150f), // a 150 metri dall'inizio
    
    // Scenario per insegnare i movimenti laterali
    TutorialScenario.CreateSideStepScenario(250f), // a 250 metri dall'inizio
    
    // Scenario con movimenti combinati
    TutorialScenario.CreateCombinedMovesScenario(350f) // a 350 metri dall'inizio
};
```

2. Personalizza ogni scenario aggiungendo il proprio messaggio di istruzione:

```csharp
TutorialScenario jumpScenario = TutorialScenario.CreateJumpScenario(50f);
jumpScenario.instructionMessage = "Premi SPAZIO per saltare!";
jumpScenario.messageDuration = 5f;
```

### Passo 4: Configurazione degli Ostacoli

Gli ostacoli sono definiti usando la struttura `ObstacleSetup`:

1. Definisci gli ostacoli per ogni scenario:

```csharp
// Esempio di configurazione ostacoli
ObstacleSetup[] obstacles = new ObstacleSetup[] {
    // Ostacolo basso da saltare
    ObstacleSetup.CreateJumpObstacles("U02", 3),
    
    // Ostacolo alto per scivolata
    ObstacleSetup.CreateSlideObstacles("U03", 2)
};

// Imposta placement per ostacoli
obstacles[0].placement = ObstaclePlacement.Center;
obstacles[1].placement = ObstaclePlacement.Pattern;
```

2. Utilizza i codici di ostacolo appropriati in base al tema:
   - `U##`: Universali
   - `C##`: City (Urbani)
   - `F##`: Forest (Foresta)
   - `T##`: Tundra
   - `V##`: Volcano (Vulcano)
   - `A##`: Abyss (Abissi)
   - `D##`: Digital (Realtà Virtuale)

### Passo 5: Creazione dei GameObject per gli Ostacoli

1. Crea un GameObject vuoto e chiamalo "Obstacles"
2. Per ogni tipo di ostacolo, crea un GameObject figlio e aggiungi il componente di authoring appropriato:

```
- Seleziona GameObject > Create Empty
- Rinomina in base al tipo (es. "JumpObstacle")
- Aggiungi il componente authoring appropriato:
  * StandardObstacleAuthoring
  * LavaObstacleAuthoring
  * IceObstacleAuthoring
  * SlipperyObstacleAuthoring
  * DigitalBarrierAuthoring
  * UnderwaterObstacleAuthoring
  * AirCurrentAuthoring
  * ToxicGasAuthoring
```

3. Configura le proprietà nell'Inspector:
   - Dimensioni (height, width)
   - Collisione (collisionRadius)
   - Resistenza (strength)
   - Preset (small, medium, large)
   - Proprietà speciali (in base al tipo)

4. Aggiungi modelli 3D come figli dei GameObject per rappresentare visivamente gli ostacoli:
   - Importa i modelli nella cartella `Assets/Art/Environments/[Tema]`
   - Trascina il modello come figlio del GameObject ostacolo
   - Configura materiali e texture appropriati

### Passo 6: Visualizzazione del Percorso

1. Crea un GameObject vuoto e chiamalo "Path"
2. Aggiungi un componente MeshRenderer e MeshFilter
3. Configura il materiale con la texture appropriata per il tema del mondo
4. Adatta la mesh al percorso definito nel `PathSegmentComponent`

### Passo 7: Configurazione dell'Ambiente

1. Aggiungi elementi di ambiente in base al tema:
   - Skybox appropriato
   - Illuminazione ambientale
   - Effetti particellari
   - Elementi decorativi

2. Per ogni tema, configura gli elementi caratteristici:
   - **City**: Edifici, strade, auto, cartelli
   - **Forest**: Alberi, vegetazione, rocce, acqua
   - **Tundra**: Neve, ghiaccio, rocce innevate, vento
   - **Volcano**: Lava, rocce vulcaniche, fumo, cenere
   - **Abyss**: Elementi subacquei, correnti, bolle, flora marina
   - **Virtual**: Elementi digitali, glitch, barriere digitali

### Passo 8: Integrazione con il Sistema ECS

Per integrare la visualizzazione con il sistema ECS, utilizza i componenti di authoring:

1. Aggiungi un GameObject vuoto e chiamalo "ECSBridge"
2. Aggiungi il componente `ECSBootstrap` a questo GameObject
3. Configura i riferimenti ai sistemi ECS necessari:
   - RuntimeTransformSystem
   - ObstacleSystem
   - PathSystem
   - Eventuali altri sistemi necessari

### Passo 9: Test e Debugging

1. Attiva la modalità di debug nel `TutorialLevelInitializer` (showDebugGizmos = true)
2. Entra in modalità Play per visualizzare i gizmo degli ostacoli e dei percorsi
3. Verifica il posizionamento degli ostacoli e la corretta visualizzazione degli scenari
4. Utilizza la finestra di debug ECS per monitorare le entità e i componenti

### Passo 10: Ottimizzazione Finale

1. Imposta LOD (Level of Detail) per gli oggetti distanti
2. Configura l'occlusione culling per migliorare le performance
3. Utilizza l'object pooling per elementi ripetitivi
4. Ottimizza le texture e i materiali

## 5. Esempi di Configurazione per Temi Specifici

### Tema Urban (City)

```csharp
// Configurazione per livello urbano
TutorialLevelData urbanLevel = new TutorialLevelData {
    description = "Livello Urbano - Centro Tecnologico",
    theme = WorldTheme.City,
    length = 600,
    difficulty = 3,
    obstacleDensity = 0.4f,
    enemyDensity = 0.3f,
    scenarios = new TutorialScenario[] {
        // All'inizio: ostacoli semplici
        TutorialScenario.CreateSideStepScenario(50f),
        
        // A metà: combinazione di ostacoli
        CreateUrbanScenario(300f)
    }
};

// Funzione helper per scenario urbano personalizzato
TutorialScenario CreateUrbanScenario(float distance) {
    return new TutorialScenario {
        name = "Urban_Complex",
        distanceFromStart = distance,
        obstacles = new ObstacleSetup[] {
            ObstacleSetup.CreateJumpObstacles("C02", 2),
            CreateCarObstacles()
        },
        instructionMessage = "Evita le auto e gli ostacoli!",
        messageDuration = 5f,
        randomPlacement = true,
        obstacleSpacing = 8f
    };
}

// Helper per creare ostacoli specifici urbani
ObstacleSetup CreateCarObstacles() {
    return new ObstacleSetup {
        obstacleCode = "C04",  // Codice per auto
        count = 3,
        placement = ObstaclePlacement.Random,
        randomizeHeight = false,
        heightRange = new Vector2(1f, 1f),
        randomizeScale = true,
        scaleRange = new Vector2(0.9f, 1.1f),
        startOffset = 3f
    };
}
```

### Tema Forest

```csharp
// Configurazione per livello forestale
TutorialLevelData forestLevel = new TutorialLevelData {
    description = "Livello Forestale - Sentieri Nascosti",
    theme = WorldTheme.Forest,
    length = 500,
    difficulty = 2,
    obstacleDensity = 0.5f,
    enemyDensity = 0.2f,
    scenarios = new TutorialScenario[] {
        CreateForestScenario(100f)
    }
};

// Funzione helper per scenario forestale
TutorialScenario CreateForestScenario(float distance) {
    return new TutorialScenario {
        name = "Forest_Paths",
        distanceFromStart = distance,
        obstacles = new ObstacleSetup[] {
            ObstacleSetup.CreateJumpObstacles("F01", 2),  // Tronchi
            CreateVineObstacles()
        },
        instructionMessage = "Salta i tronchi e scivola sotto le liane!",
        messageDuration = 5f,
        randomPlacement = false,
        obstacleSpacing = 10f
    };
}

// Helper per creare ostacoli di liane pendenti
ObstacleSetup CreateVineObstacles() {
    return new ObstacleSetup {
        obstacleCode = "F03",  // Codice per liane
        count = 3,
        placement = ObstaclePlacement.Pattern,
        randomizeHeight = true,
        heightRange = new Vector2(1.5f, 2.5f),
        randomizeScale = false,
        scaleRange = new Vector2(1f, 1f),
        startOffset = 5f
    };
}
```

## 6. Riferimenti e Asset Management

- I modelli 3D per gli ostacoli urbani si trovano in: `Assets/Art/Environments/Urban/`
- I modelli 3D per gli ostacoli forestali si trovano in: `Assets/Art/Environments/Forest/`
- I materiali condivisi si trovano in: `Assets/Art/Materials/`
- Gli script ECS sono in: `Assets/_Project/ECS/`
- Per riutilizzare scenari esistenti, fai riferimento a: `Assets/_Project/Runtime/Levels/`

## 7. Tips & Tricks

1. **Seed Consistente**: Utilizza lo stesso seed per generare livelli identici durante i test. Per applicare un seed specifico, usa lo strumento "Tutorial Test Tool" nel menu "Runaway Heroes".
2. **Combinazioni di Ostacoli**: Combina diversi tipi di ostacoli per creare sfide interessanti.
3. **Personalizzazione Visiva**: Adatta gli elementi visivi al tema specifico del mondo.
4. **Progettazione Progressiva**: Aumenta gradualmente la difficoltà lungo il livello.
5. **Debug con Gizmos**: Attiva i gizmos per visualizzare il posizionamento degli ostacoli.
6. **Prefabs Riutilizzabili**: Crea prefab per gli ostacoli comuni per facilitare il riutilizzo.
7. **Test Sistematico**: Testa ogni scenario individualmente prima di unirli.
8. **Documentazione**: Commenta i tuoi scenari personalizzati per riferimento futuro.
9. **Generazione ECS**: Ricorda che i parametri di configurazione come il seed vengono passati al sistema ECS tramite componenti come `RunnerLevelConfigComponent`, che non sono direttamente visibili nell'interfaccia di Unity.

## 8. Validazione del Livello

Utilizza lo strumento `LevelValidator` per verificare che il livello rispetti i requisiti:

1. Seleziona la scena del livello
2. Nel menu, seleziona "RunawayHeroes > Tools > Level Validator"
3. Esegui la validazione e correggi eventuali errori o avvisi

Criteri di validazione:
- Distanza minima tra ostacoli consecutivi
- Difficoltà bilanciata
- Distribuzione corretta degli elementi di gioco
- Performance e ottimizzazione