# Sistema di Validazione dei Livelli

## Panoramica

Il sistema di validazione dei livelli permette di verificare che tutti i livelli e gli scenari tutorial creati siano effettivamente completabili dal giocatore. Analizza gli ostacoli, la loro disposizione e le capacità del personaggio per identificare eventuali problemi critici o potenziali difficoltà.

## Componenti Principali

### 1. LevelValidator

La classe principale che esegue l'analisi e la validazione:

```csharp
public class LevelValidator : MonoBehaviour
{
    // Valori configurabili
    public float characterRadius = 0.5f;
    public float characterHeight = 1.8f;
    public float characterSlideHeight = 0.6f;
    public float maxJumpHeight = 2.0f;
    public int maxConsecutiveJumps = 2;
    public float maxSideStepDistance = 3.0f;
    
    // Riferimenti
    public TutorialLevelInitializer tutorialInitializer;
    
    // Metodi principali
    public ValidationResult ValidateLevel();
}
```

### 2. LevelValidatorEditor

Un editor custom che offre un'interfaccia grafica per la validazione:
- Pulsante "Valida Livello"
- Visualizzazione dei risultati con dettagli sui problemi
- Possibilità di "focus" sulla posizione del problema nella scena

### 3. Tipi di Validazione

Il validatore segnala due tipi di problemi:
- **Critici**: Rendono il livello impossibile da completare
- **Avvisi**: Punti potenzialmente problematici o troppo difficili

## Come Usare il Validatore

1. **Setup**:
   - Aggiungi il componente `LevelValidator` a un GameObject nella scena
   - Assegna il `TutorialLevelInitializer` da validare
   - Configura i parametri in base alle capacità del personaggio

2. **Esecuzione**:
   - Nell'editor, clicca il pulsante "Valida Livello"
   - Osserva i risultati nella finestra di ispezione
   - Per i problemi individuati, puoi cliccare "Focus" per visualizzare la posizione nella scena

3. **Debug Visuale**:
   - Attiva `showDebugVisualization` per visualizzare i problemi nella scena
   - I problemi critici sono mostrati in rosso con una X
   - Gli avvisi sono mostrati in giallo con un punto esclamativo

## Cosa Verifica il Validatore

### 1. Verifica degli Scenari
- Sovrapposizioni tra scenari
- Messaggi di istruzione mancanti
- Distanziamento ostacoli non valido
- Lunghezza totale della sequenza

### 2. Verifica degli Ostacoli
- Altezze eccessive per gli ostacoli da saltare
- Ostacoli troppo alti anche per la scivolata
- Problemi di spazio per spostamenti laterali
- Collisioni tra diversi tipi di ostacoli

### 3. Verifica dei Pattern
- Passaggi completamente bloccati
- Troppe combinazioni difficili in successione
- Distanze troppo ravvicinate tra ostacoli complessi
- Requisiti di abilità speciali nel tutorial

### 4. Verifica dei Percorsi
- Simulazione di percorso attraverso gli ostacoli
- Analisi delle combinazioni critiche
- Verifica di spazi sufficienti per il personaggio

## Esempi di Problemi Rilevati

### Problemi Critici:
- "Ostacolo di tipo 'U03' ha un'altezza (2.5m) che supera l'altezza massima di salto (2.0m)"
- "Il pattern di ostacoli 'U01' blocca completamente il percorso senza lasciare spazi sufficienti"
- "Nessun percorso possibile trovato intorno alla posizione z=135m"

### Avvisi:
- "Possibile sovrapposizione tra scenario 'Jump_Training' e 'Slide_Training'"
- "Troppe ostacoli da saltare consecutivi (5) rispetto al massimo consentito (2)"
- "Ostacolo di ghiaccio 'I01' potrebbe richiedere l'abilità 'Aura di Calore' di Kai"

## Integrazione nel Workflow

### Durante lo Sviluppo
- Valida frequentemente i livelli mentre li costruisci
- Risolvi i problemi critici immediatamente
- Valuta gli avvisi in base al livello di difficoltà desiderato

### Prima della Pubblicazione
- Esegui la validazione completa di tutti i livelli
- Assicurati che non ci siano problemi critici
- Testa manualmente i punti segnalati come avvisi

## Personalizzazione

È possibile personalizzare il validatore per diversi tipi di giocatori o difficoltà:
- Modifica i parametri come `maxJumpHeight` e `maxConsecutiveJumps` 
- Aggiungi regole specifiche per diversi personaggi giocabili
- Imposta differenti soglie per i tutorial rispetto ai livelli avanzati

## Suggerimenti per la Creazione di Livelli Validi

1. **Spazio di Manovra**: Lascia sempre uno spazio sufficiente per il personaggio (almeno 2× il raggio)
2. **Altezze Raggiungibili**: Mantieni gli ostacoli da saltare sotto l'altezza massima di salto
3. **Distribuzione Equilibrata**: Evita concentrazioni eccessive di ostacoli complessi
4. **Curve di Difficoltà**: Aumenta gradualmente la difficoltà, specialmente nei tutorial
5. **Verifica le Combinazioni**: Presta attenzione quando combini diversi tipi di ostacoli ravvicinati