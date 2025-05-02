# Configurazione del Prefab Tutorial

Per configurare correttamente il livello tutorial, segui questi passaggi nell'Editor Unity:

## 1. Crea un GameObject per il Tutorial

1. Nella scena del Tutorial, crea un nuovo GameObject vuoto
2. Rinominalo in "TutorialManager"
3. Aggiungi i seguenti componenti:
   - `TutorialSceneSetup`
   - `TutorialLevelInitializer`

## 2. Configura i Componenti

### TutorialLevelInitializer:
- **Tutorial Length**: 500 (lunghezza in metri)
- **Tutorial Theme**: City (tema urbano)
- **Seed**: 0 (sarà generato casualmente se lasciato a 0)
- **Player Start Position**: (0, 0, 0)
- **Checkpoint Distance**: 100

### TutorialSceneSetup:
- **Player Prefab**: Assegna il prefab del personaggio principale (es. "Alex")
- **Camera Prefab**: Assegna il prefab della telecamera
- **Player Start Position**: (0, 0, 0)
- **Default Character Id**: 0 (Alex)
- **Tutorial Initializer**: Lascia vuoto (si auto-assegnerà)
- **Load Main Menu On Completion**: true (se vuoi tornare al menu principale al termine)

## 3. Crea l'Interfaccia UI del Tutorial

1. Crea un Canvas nella scena
2. Rinominalo in "TutorialUI"
3. Aggiungi un oggetto Text come figlio:
   - Impostalo in alto al centro dello schermo
   - Modifica font e dimensione per renderlo ben visibile
   - Lascia il testo vuoto (sarà popolato dal sistema)

## 4. Prefab del Personaggio e della Telecamera

### Personaggio:
Assicurati che il prefab del personaggio abbia:
- Tag "Player"
- Un rigidbody
- Un collider
- Script di controllo del personaggio

### Telecamera:
Assicurati che il prefab della telecamera abbia:
- Componente CameraFollow (o script equivalente per seguire il personaggio)
- Impostazioni appropriate per una visuale in terza persona o dall'alto

## 5. Elementi aggiuntivi del Tutorial

Nella scena del tutorial potresti voler aggiungere:
- Un terreno di base o una piattaforma iniziale
- Elementi decorativi statici per dare contesto al percorso
- Punti luce per illuminare il percorso
- Effetti particellari per rendere l'ambiente più vivo

## 6. Configurazione della Scena

1. Assicurati che la scena "Tutorial" sia inclusa nel build settings
2. Verifica che possa essere caricata dal menu principale
3. Configura il completamento del tutorial per sbloccare il primo mondo