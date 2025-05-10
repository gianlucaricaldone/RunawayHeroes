// Path: LevelValidator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.Gameplay;

namespace RunawayHeroes.Runtime.Levels
{
    /// <summary>
    /// Sistema di validazione che verifica se un livello generato è completabile
    /// </summary>
    public class LevelValidator : MonoBehaviour
    {
        [Header("Configurazione Validazione")]
        [Tooltip("Raggio di collisione del personaggio")]
        public float characterRadius = 0.5f;
        
        [Tooltip("Altezza del personaggio in piedi")]
        public float characterHeight = 1.8f;
        
        [Tooltip("Altezza del personaggio in scivolata")]
        public float characterSlideHeight = 0.6f;
        
        [Tooltip("Altezza massima di salto")]
        public float maxJumpHeight = 2.0f;
        
        [Tooltip("Numero di salti consecutivi permessi")]
        public int maxConsecutiveJumps = 2;
        
        [Tooltip("Distanza massima del side step")]
        public float maxSideStepDistance = 3.0f;
        
        [Tooltip("Se true, mostra debug visuale nel mondo")]
        public bool showDebugVisualization = true;
        
        [Tooltip("Se true, genera report dettagliato con tutti i problemi")]
        public bool generateDetailedReport = true;
        
        [Header("Riferimenti")]
        [Tooltip("Inizializzatore del livello da validare")]
        public TutorialLevelInitializer tutorialInitializer;
        
        // Risultati della validazione
        private List<ValidationIssue> _validationIssues = new List<ValidationIssue>();
        
        // Costanti per il posizionamento degli ostacoli
        private const float LANE_WIDTH = 9f; // Larghezza totale della corsia
        private const float LEFT_POSITION = -3f; // Posizione laterale sinistra
        private const float CENTER_POSITION = 0f; // Posizione centrale
        private const float RIGHT_POSITION = 3f; // Posizione laterale destra
        
        /// <summary>
        /// Esegue la validazione del livello tutorial configurato
        /// </summary>
        public ValidationResult ValidateLevel()
        {
            if (tutorialInitializer == null)
            {
                Debug.LogError("TutorialLevelInitializer non assegnato. Impossibile validare.");
                return new ValidationResult 
                {
                    IsValid = false,
                    Issues = new List<ValidationIssue> { 
                        new ValidationIssue 
                        { 
                            Type = IssueType.Critical, 
                            Message = "TutorialLevelInitializer non assegnato" 
                        } 
                    }
                };
            }
            
            _validationIssues.Clear();
            
            // Verifica sequenze
            if (tutorialInitializer.tutorialSequence != null)
            {
                TutorialLevelData[] dataSequences = tutorialInitializer.tutorialSequence;
                ValidateTutorialSequences(dataSequences);
            }
            
            // Determina il risultato
            bool isValid = !HasCriticalIssues();
            
            // Stampa risultato nel log
            LogValidationResult(isValid);
            
            return new ValidationResult
            {
                IsValid = isValid,
                Issues = _validationIssues
            };
        }
        
        /// <summary>
        /// Controlla se ci sono problemi critici che rendono il livello impossibile
        /// </summary>
        private bool HasCriticalIssues()
        {
            foreach (var issue in _validationIssues)
            {
                if (issue.Type == IssueType.Critical)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Stampa il risultato della validazione nel log
        /// </summary>
        private void LogValidationResult(bool isValid)
        {
            if (isValid)
            {
                Debug.Log("<color=green>[Level Validator] Il livello è completabile!</color>");
                
                if (_validationIssues.Count > 0)
                {
                    Debug.Log($"<color=yellow>Trovati {_validationIssues.Count} avvisi (non critici).</color>");
                    
                    if (generateDetailedReport)
                    {
                        foreach (var issue in _validationIssues)
                        {
                            Debug.Log($"<color=yellow>- {issue.Message} (Scenario: {issue.ScenarioName}, Posizione: {issue.Position})</color>");
                        }
                    }
                }
            }
            else
            {
                int criticalCount = 0;
                int warningCount = 0;
                
                foreach (var issue in _validationIssues)
                {
                    if (issue.Type == IssueType.Critical)
                        criticalCount++;
                    else
                        warningCount++;
                }
                
                Debug.LogError($"[Level Validator] Il livello NON è completabile! Trovati {criticalCount} problemi critici e {warningCount} avvisi.");
                
                if (generateDetailedReport)
                {
                    // Stampa prima i problemi critici
                    foreach (var issue in _validationIssues)
                    {
                        if (issue.Type == IssueType.Critical)
                        {
                            Debug.LogError($"- CRITICO: {issue.Message} (Scenario: {issue.ScenarioName}, Posizione: {issue.Position})");
                        }
                    }
                    
                    // Poi gli avvisi
                    foreach (var issue in _validationIssues)
                    {
                        if (issue.Type == IssueType.Warning)
                        {
                            Debug.LogWarning($"- AVVISO: {issue.Message} (Scenario: {issue.ScenarioName}, Posizione: {issue.Position})");
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Valida tutte le sequenze tutorial
        /// </summary>
        private void ValidateTutorialSequences(TutorialLevelData[] sequences)
        {
            if (sequences == null || sequences.Length == 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Critical,
                    Message = "Nessuna sequenza tutorial definita",
                    ScenarioName = "N/A",
                    Position = Vector3.zero
                });
                return;
            }
            
            for (int i = 0; i < sequences.Length; i++)
            {
                var sequence = sequences[i];
                ValidateTutorialSequence(sequence, i);
            }
        }
        
        /// <summary>
        /// Valida una sequenza tutorial specifica
        /// </summary>
        private void ValidateTutorialSequence(TutorialLevelData sequence, int sequenceIndex)
        {
            if (sequence.scenarios == null || sequence.scenarios.Length == 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"La sequenza '{sequence.description}' non ha scenari definiti",
                    ScenarioName = sequence.description,
                    Position = Vector3.zero
                });
                return;
            }
            
            // Verifica sovrapposizioni tra scenari
            CheckScenarioOverlaps(sequence);
            
            // Verifica tutti gli scenari
            for (int i = 0; i < sequence.scenarios.Length; i++)
            {
                var scenario = sequence.scenarios[i];
                ValidateScenario(scenario, sequence, i);
            }
            
            // Verifica lunghezza totale
            ValidateSequenceLength(sequence);
        }
        
        /// <summary>
        /// Verifica combinazioni impossibili di ostacoli
        /// </summary>
        private void ValidateObstacleCombinations(TutorialScenario scenario)
        {
            // Non procedere se non ci sono abbastanza ostacoli per creare una combinazione
            if (scenario.obstacles == null || scenario.obstacles.Length <= 1)
                return;
                
            // Raggruppa gli ostacoli per tipo
            var jumpObstacles = new List<ObstacleSetup>();
            var slideObstacles = new List<ObstacleSetup>();
            var sideObstacles = new List<ObstacleSetup>();
            var specialObstacles = new List<ObstacleSetup>();
            
            foreach (var setup in scenario.obstacles)
            {
                ObstacleType type = DetermineObstacleType(setup.obstacleCode);
                
                switch (type)
                {
                    case ObstacleType.JumpObstacle:
                        jumpObstacles.Add(setup);
                        break;
                    case ObstacleType.SlideObstacle:
                        slideObstacles.Add(setup);
                        break;
                    case ObstacleType.SideObstacle:
                        sideObstacles.Add(setup);
                        break;
                    case ObstacleType.SpecialObstacle:
                        specialObstacles.Add(setup);
                        break;
                }
            }
            
            // Verifica combinazioni critiche
            
            // 1. Combinazione salto + scivolata troppo ravvicinati (difficile da eseguire)
            if (jumpObstacles.Count > 0 && slideObstacles.Count > 0)
            {
                foreach (var jumpSetup in jumpObstacles)
                {
                    foreach (var slideSetup in slideObstacles)
                    {
                        float jumpZ = scenario.distanceFromStart + jumpSetup.startOffset;
                        float slideZ = scenario.distanceFromStart + slideSetup.startOffset;
                        
                        // Se gli ostacoli sono molto vicini tra loro
                        if (Math.Abs(jumpZ - slideZ) < 3.0f)
                        {
                            _validationIssues.Add(new ValidationIssue
                            {
                                Type = IssueType.Warning,
                                Message = $"Combinazione ravvicinata di salto + scivolata in '{scenario.name}' " +
                                         $"potrebbe essere difficile da eseguire (distanza: {Math.Abs(jumpZ - slideZ):F2}m)",
                                ScenarioName = scenario.name,
                                Position = new Vector3(0, 0, Math.Min(jumpZ, slideZ))
                            });
                        }
                    }
                }
            }
            
            // 2. Combinazione side step + salto/scivolata (richiede cambio di input veloce)
            if (sideObstacles.Count > 0 && (jumpObstacles.Count > 0 || slideObstacles.Count > 0))
            {
                // Esamina combinazioni con salto
                foreach (var sideSetup in sideObstacles)
                {
                    foreach (var jumpSetup in jumpObstacles)
                    {
                        float sideZ = scenario.distanceFromStart + sideSetup.startOffset;
                        float jumpZ = scenario.distanceFromStart + jumpSetup.startOffset;
                        
                        if (Math.Abs(sideZ - jumpZ) < 2.0f)
                        {
                            _validationIssues.Add(new ValidationIssue
                            {
                                Type = IssueType.Warning,
                                Message = $"Combinazione ravvicinata di spostamento laterale + salto in '{scenario.name}' " +
                                         $"potrebbe richiedere un cambio di input troppo veloce (distanza: {Math.Abs(sideZ - jumpZ):F2}m)",
                                ScenarioName = scenario.name,
                                Position = new Vector3(0, 0, Math.Min(sideZ, jumpZ))
                            });
                        }
                    }
                    
                    // Esamina combinazioni con scivolata
                    foreach (var slideSetup in slideObstacles)
                    {
                        float sideZ = scenario.distanceFromStart + sideSetup.startOffset;
                        float slideZ = scenario.distanceFromStart + slideSetup.startOffset;
                        
                        if (Math.Abs(sideZ - slideZ) < 2.0f)
                        {
                            _validationIssues.Add(new ValidationIssue
                            {
                                Type = IssueType.Warning,
                                Message = $"Combinazione ravvicinata di spostamento laterale + scivolata in '{scenario.name}' " +
                                         $"potrebbe richiedere un cambio di input troppo veloce (distanza: {Math.Abs(sideZ - slideZ):F2}m)",
                                ScenarioName = scenario.name,
                                Position = new Vector3(0, 0, Math.Min(sideZ, slideZ))
                            });
                        }
                    }
                }
            }
            
            // 3. Combinazione di ostacoli speciali che richiedono abilità diverse nello stesso scenario
            if (specialObstacles.Count > 1)
            {
                var lavaObstacles = specialObstacles.Where(o => o.obstacleCode.StartsWith("L", StringComparison.OrdinalIgnoreCase)).ToList();
                var iceObstacles = specialObstacles.Where(o => o.obstacleCode.StartsWith("I", StringComparison.OrdinalIgnoreCase)).ToList();
                var digitalObstacles = specialObstacles.Where(o => o.obstacleCode.StartsWith("D", StringComparison.OrdinalIgnoreCase)).ToList();
                
                // Controlla se ci sono più tipi diversi di ostacoli speciali
                int specialTypes = 0;
                if (lavaObstacles.Count > 0) specialTypes++;
                if (iceObstacles.Count > 0) specialTypes++;
                if (digitalObstacles.Count > 0) specialTypes++;
                
                if (specialTypes > 1)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Type = IssueType.Critical,
                        Message = $"Lo scenario '{scenario.name}' contiene {specialTypes} tipi diversi di ostacoli speciali " +
                                 $"che potrebbero richiedere personaggi diversi contemporaneamente",
                        ScenarioName = scenario.name,
                        Position = new Vector3(0, 0, scenario.distanceFromStart)
                    });
                }
            }
            
            // 4. Verifica pattern di ostacoli difficili
            if (scenario.obstacleSpacing < 5.0f && 
                (jumpObstacles.Sum(o => o.count) + slideObstacles.Sum(o => o.count) + sideObstacles.Sum(o => o.count)) > 5)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Lo scenario '{scenario.name}' ha una combinazione ad alta densità di ostacoli con " +
                             $"spaziatura ridotta ({scenario.obstacleSpacing}m) che potrebbe essere troppo difficile",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart)
                });
            }
            
            // 5. Se il posizionamento non è casuale, verifica la possibilità di passaggio
            if (!scenario.randomPlacement)
            {
                // Chiamata già implementata altrove nel codice
                // ScanObstacleCombination(scenario);
            }
        }

        /// <summary>
        /// Verifica che non ci siano sovrapposizioni tra scenari
        /// </summary>
        private void CheckScenarioOverlaps(TutorialLevelData sequence)
        {
            if (sequence.scenarios.Length <= 1)
                return;
                
            // Ordina scenari per distanza dall'inizio
            var orderedScenarios = new List<(TutorialScenario scenario, int index)>();
            for (int i = 0; i < sequence.scenarios.Length; i++)
            {
                orderedScenarios.Add((sequence.scenarios[i], i));
            }
            
            orderedScenarios.Sort((a, b) => a.scenario.distanceFromStart.CompareTo(b.scenario.distanceFromStart));
            
            // Controlla sovrapposizioni
            for (int i = 0; i < orderedScenarios.Count - 1; i++)
            {
                var current = orderedScenarios[i].scenario;
                var next = orderedScenarios[i + 1].scenario;
                
                // Stima la fine dello scenario corrente
                float currentEnd = EstimateScenarioEnd(current);
                
                if (currentEnd > next.distanceFromStart)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Type = IssueType.Warning,
                        Message = $"Possibile sovrapposizione tra scenario '{current.name}' e '{next.name}'",
                        ScenarioName = current.name,
                        Position = new Vector3(0, 0, next.distanceFromStart)
                    });
                }
            }
        }
        
        /// <summary>
        /// Stima la fine di uno scenario in base agli ostacoli presenti
        /// </summary>
        private float EstimateScenarioEnd(TutorialScenario scenario)
        {
            float maxEnd = scenario.distanceFromStart;
            
            if (scenario.obstacles == null || scenario.obstacles.Length == 0)
                return maxEnd + 10f; // Stima minima
                
            foreach (var obstacle in scenario.obstacles)
            {
                float obstacleEnd;
                
                if (scenario.randomPlacement)
                {
                    // Per posizionamento casuale, stima la lunghezza massima possibile
                    float segmentLength = scenario.obstacleSpacing * obstacle.count;
                    obstacleEnd = scenario.distanceFromStart + obstacle.startOffset + segmentLength;
                }
                else
                {
                    // Per posizionamento regolare, calcola l'esatta posizione dell'ultimo ostacolo
                    obstacleEnd = scenario.distanceFromStart + obstacle.startOffset + 
                                 (obstacle.count - 1) * scenario.obstacleSpacing + 5f; // +5 è un buffer di sicurezza
                }
                
                maxEnd = Math.Max(maxEnd, obstacleEnd);
            }
            
            return maxEnd;
        }
        
        /// <summary>
        /// Valida uno scenario specifico
        /// </summary>
        private void ValidateScenario(TutorialScenario scenario, TutorialLevelData sequence, int scenarioIndex)
        {
            if (scenario.obstacles == null || scenario.obstacles.Length == 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Lo scenario '{scenario.name}' non ha ostacoli definiti",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart)
                });
                return;
            }
            
            // Verifica messaggi di istruzione
            if (string.IsNullOrEmpty(scenario.instructionMessage))
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Lo scenario '{scenario.name}' non ha un messaggio di istruzione",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart)
                });
            }
            
            // Verifica valori negativi di distanziamento
            if (scenario.obstacleSpacing <= 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Lo scenario '{scenario.name}' ha un distanziamento ostacoli non valido ({scenario.obstacleSpacing})",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart)
                });
            }
            
            // Verifica tutti i tipi di ostacoli
            foreach (var obstacleSetup in scenario.obstacles)
            {
                ValidateObstacleSetup(obstacleSetup, scenario);
            }
            
            // Verifica combinazioni impossibili di ostacoli
            ValidateObstacleCombinations(scenario);
        }
        
        /// <summary>
        /// Verifica che la lunghezza della sequenza sia ragionevole
        /// </summary>
        private void ValidateSequenceLength(TutorialLevelData sequence)
        {
            if (sequence.length <= 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"La sequenza '{sequence.description}' ha una lunghezza non valida ({sequence.length})",
                    ScenarioName = sequence.description,
                    Position = Vector3.zero
                });
                return;
            }
            
            // Calcola l'ultimo punto dello scenario più lontano
            float furthestPoint = 0;
            
            foreach (var scenario in sequence.scenarios)
            {
                float scenarioEnd = EstimateScenarioEnd(scenario);
                furthestPoint = Math.Max(furthestPoint, scenarioEnd);
            }
            
            // Verifica che la lunghezza dichiarata sia sufficiente
            if (furthestPoint > sequence.length)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"La lunghezza della sequenza '{sequence.description}' ({sequence.length}m) potrebbe non essere sufficiente " +
                             $"per contenere tutti gli scenari (ultimo punto stimato: {furthestPoint}m)",
                    ScenarioName = sequence.description,
                    Position = new Vector3(0, 0, furthestPoint)
                });
            }
        }
        
        /// <summary>
        /// Valida una configurazione di ostacoli
        /// </summary>
        private void ValidateObstacleSetup(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Controllo codice ostacolo
            if (string.IsNullOrEmpty(obstacleSetup.obstacleCode))
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Codice ostacolo non specificato in '{scenario.name}'",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            // Controllo count
            if (obstacleSetup.count <= 0)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Numero di ostacoli non valido ({obstacleSetup.count}) per '{obstacleSetup.obstacleCode}' in '{scenario.name}'",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            // Controllo range di randomizzazione
            if (obstacleSetup.randomizeHeight &&
                (obstacleSetup.heightRange.x < 0 || obstacleSetup.heightRange.y <= obstacleSetup.heightRange.x))
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Range di altezza non valido ({obstacleSetup.heightRange}) per '{obstacleSetup.obstacleCode}' in '{scenario.name}'",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            if (obstacleSetup.randomizeScale &&
                (obstacleSetup.scaleRange.x <= 0 || obstacleSetup.scaleRange.y <= obstacleSetup.scaleRange.x))
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Range di scala non valido ({obstacleSetup.scaleRange}) per '{obstacleSetup.obstacleCode}' in '{scenario.name}'",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            // Verifica collisioni con altri ostacoli
            if (scenario.obstacles.Length > 1)
            {
                CheckForObstacleCollisions(obstacleSetup, scenario);
            }
            
            // Verifica percorribilità
            CheckIfObstaclesCanBeTraversed(obstacleSetup, scenario);
        }
        
        /// <summary>
        /// Verifica la presenza di collisioni tra diversi ostacoli nello stesso scenario
        /// </summary>
        private void CheckForObstacleCollisions(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Stima le posizioni degli ostacoli
            var obstaclePositions = EstimateObstaclePositions(obstacleSetup, scenario);
            
            // Confronta con altri set di ostacoli
            foreach (var otherSetup in scenario.obstacles)
            {
                // Non confrontare con se stesso
                if (AreObstacleSetupsEqual(otherSetup, obstacleSetup)) 
                    continue;
                    
                var otherPositions = EstimateObstaclePositions(otherSetup, scenario);
                
                // Controlla collisioni
                foreach (var pos1 in obstaclePositions)
                {
                    foreach (var pos2 in otherPositions)
                    {
                        float distance = Vector3.Distance(
                            new Vector3(pos1.x, 0, pos1.z),
                            new Vector3(pos2.x, 0, pos2.z));
                            
                        // Se la distanza è troppo piccola, segnala una possibile collisione
                        if (distance < characterRadius * 2)
                        {
                            _validationIssues.Add(new ValidationIssue
                            {
                                Type = IssueType.Warning,
                                Message = $"Possibile collisione tra ostacoli di tipo '{obstacleSetup.obstacleCode}' e '{otherSetup.obstacleCode}' " +
                                         $"in '{scenario.name}' (distanza: {distance:F2}m)",
                                ScenarioName = scenario.name,
                                Position = new Vector3(pos1.x, 0, pos1.z)
                            });
                            
                            // Segnala solo una collisione per coppia
                            return;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifica che gli ostacoli possano essere superati con le abilità disponibili
        /// </summary>
        private void CheckIfObstaclesCanBeTraversed(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Determina il tipo di ostacolo in base al codice
            ObstacleType obstacleType = DetermineObstacleType(obstacleSetup.obstacleCode);
            
            // Controlla in base al tipo
            switch (obstacleType)
            {
                case ObstacleType.JumpObstacle:
                    CheckJumpObstacles(obstacleSetup, scenario);
                    break;
                    
                case ObstacleType.SlideObstacle:
                    CheckSlideObstacles(obstacleSetup, scenario);
                    break;
                    
                case ObstacleType.SideObstacle:
                    CheckSideObstacles(obstacleSetup, scenario);
                    break;
                    
                case ObstacleType.SpecialObstacle:
                    // Gli ostacoli speciali richiedono analisi specifiche in base al tipo
                    // (lava, ghiaccio, barriere digitali, ecc.)
                    CheckSpecialObstacles(obstacleSetup, scenario);
                    break;
            }
            
            // Controlla anche scenari multi-obstacle con specifici test
            if (scenario.obstacles.Length > 1)
            {
                CheckComplexObstaclePatterns(obstacleSetup, scenario);
            }
        }
        
        /// <summary>
        /// Verifica gli ostacoli da saltare
        /// </summary>
        private void CheckJumpObstacles(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Determina l'altezza massima
            float maxHeight = obstacleSetup.randomizeHeight 
                ? obstacleSetup.heightRange.y 
                : DetermineDefaultObstacleHeight(obstacleSetup.obstacleCode);
                
            // Controlla se l'altezza è superabile con il salto
            if (maxHeight > maxJumpHeight)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Critical,
                    Message = $"Ostacolo di tipo '{obstacleSetup.obstacleCode}' in '{scenario.name}' ha un'altezza massima ({maxHeight}m) " +
                             $"che supera l'altezza massima di salto ({maxJumpHeight}m)",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            // Controlla salti consecutivi
            if (obstacleSetup.count > maxConsecutiveJumps && 
                scenario.obstacleSpacing < 5.0f) // Se gli ostacoli sono ravvicinati
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Troppe ostacoli da saltare consecutivi ({obstacleSetup.count}) in '{scenario.name}' " +
                             $"rispetto al massimo consentito ({maxConsecutiveJumps})",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
        }
        
        /// <summary>
        /// Verifica gli ostacoli da superare con scivolata
        /// </summary>
        private void CheckSlideObstacles(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Determina l'altezza minima
            float minHeight = obstacleSetup.randomizeHeight 
                ? obstacleSetup.heightRange.x
                : DetermineDefaultObstacleHeight(obstacleSetup.obstacleCode);
                
            // Controlla se l'altezza minima è sufficiente per richiedere scivolata
            if (minHeight <= characterSlideHeight)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Ostacolo di tipo '{obstacleSetup.obstacleCode}' in '{scenario.name}' ha un'altezza minima ({minHeight}m) " +
                             $"che potrebbe non richiedere scivolata (altezza scivolata: {characterSlideHeight}m)",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            
            // Controlla se l'ostacolo è troppo alto anche per la scivolata
            float maxHeight = obstacleSetup.randomizeHeight
                ? obstacleSetup.heightRange.y
                : DetermineDefaultObstacleHeight(obstacleSetup.obstacleCode);
                
            if (maxHeight < characterSlideHeight)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Critical,
                    Message = $"Ostacolo di tipo '{obstacleSetup.obstacleCode}' in '{scenario.name}' ha un'altezza massima ({maxHeight}m) " +
                             $"inferiore all'altezza di scivolata del personaggio ({characterSlideHeight}m) e non può essere superato",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
        }
        
        /// <summary>
        /// Verifica gli ostacoli da evitare con spostamenti laterali
        /// </summary>
        private void CheckSideObstacles(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Controlla se vengono bloccate tutte le corsie contemporaneamente
            if (obstacleSetup.placement == ObstaclePlacement.Pattern && 
                obstacleSetup.count >= 3) // Se ci sono abbastanza ostacoli per bloccare tutto
            {
                bool hasGap = false;
                
                // Simula posizionamento pattern per verificare se c'è uno spazio
                float patternStep = LANE_WIDTH / (obstacleSetup.count - 1);
                
                if (patternStep < characterRadius * 2)
                {
                    for (int i = 0; i < obstacleSetup.count - 1; i++)
                    {
                        float gapSize = patternStep;
                        if (gapSize > characterRadius * 2)
                        {
                            hasGap = true;
                            break;
                        }
                    }
                    
                    if (!hasGap)
                    {
                        _validationIssues.Add(new ValidationIssue
                        {
                            Type = IssueType.Critical,
                            Message = $"Il pattern di ostacoli '{obstacleSetup.obstacleCode}' in '{scenario.name}' " +
                                     $"blocca completamente il percorso senza lasciare spazi sufficienti (spazio richiesto: {characterRadius * 2}m)",
                            ScenarioName = scenario.name,
                            Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                        });
                    }
                }
            }
            
            // Verifica se gli ostacoli laterali lasciano spazio per passare
            if ((obstacleSetup.placement == ObstaclePlacement.Left || 
                 obstacleSetup.placement == ObstaclePlacement.Right) &&
                obstacleSetup.count > 1)
            {
                // Controlla se c'è spazio sufficiente sul lato opposto
                float width = DetermineDefaultObstacleWidth(obstacleSetup.obstacleCode);
                if (obstacleSetup.randomizeScale)
                {
                    width *= obstacleSetup.scaleRange.y; // Usa scala massima
                }
                
                float freeSpace = LANE_WIDTH - width;
                
                if (freeSpace < characterRadius * 2)
                {
                    _validationIssues.Add(new ValidationIssue
                    {
                        Type = IssueType.Critical,
                        Message = $"Gli ostacoli laterali '{obstacleSetup.obstacleCode}' in '{scenario.name}' " +
                                 $"non lasciano spazio sufficiente per passare (spazio disponibile: {freeSpace}m, richiesto: {characterRadius * 2}m)",
                        ScenarioName = scenario.name,
                        Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                    });
                }
            }
        }
        
        /// <summary>
        /// Verifica ostacoli speciali (lava, ghiaccio, barriere digitali, ecc.)
        /// </summary>
        private void CheckSpecialObstacles(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Controlla se si tratta di ostacoli speciali nel tutorial
            // Nel tutorial i personaggi potrebbero non avere ancora le abilità necessarie
            
            char firstChar = obstacleSetup.obstacleCode.Length > 0 ? obstacleSetup.obstacleCode[0] : ' ';
            
            if (char.ToUpper(firstChar) == 'L') // Lava
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Ostacolo di lava '{obstacleSetup.obstacleCode}' in '{scenario.name}' potrebbe richiedere " +
                             $"l'abilità 'Corpo Ignifugo' di Ember, che potrebbe non essere disponibile nel tutorial",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            else if (char.ToUpper(firstChar) == 'I') // Ghiaccio
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Ostacolo di ghiaccio '{obstacleSetup.obstacleCode}' in '{scenario.name}' potrebbe richiedere " +
                             $"l'abilità 'Aura di Calore' di Kai, che potrebbe non essere disponibile nel tutorial",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
            else if (char.ToUpper(firstChar) == 'D') // Barriere digitali
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Barriera digitale '{obstacleSetup.obstacleCode}' in '{scenario.name}' potrebbe richiedere " +
                             $"l'abilità 'Glitch Controllato' di Neo, che potrebbe non essere disponibile nel tutorial",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart + obstacleSetup.startOffset)
                });
            }
        }
        
        /// <summary>
        /// Verifica pattern complessi di ostacoli
        /// </summary>
        private void CheckComplexObstaclePatterns(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Analizza combinazioni critiche di ostacoli
            int jumpObstacles = 0;
            int slideObstacles = 0;
            int sideObstacles = 0;
            
            foreach (var setup in scenario.obstacles)
            {
                ObstacleType type = DetermineObstacleType(setup.obstacleCode);
                
                switch (type)
                {
                    case ObstacleType.JumpObstacle:
                        jumpObstacles += setup.count;
                        break;
                    case ObstacleType.SlideObstacle:
                        slideObstacles += setup.count;
                        break;
                    case ObstacleType.SideObstacle:
                        sideObstacles += setup.count;
                        break;
                }
            }
            
            // Troppe combinazioni potrebbero essere troppo difficili
            int totalObstacles = jumpObstacles + slideObstacles + sideObstacles;
            
            if (totalObstacles > 10 && scenario.obstacleSpacing < 8.0f)
            {
                _validationIssues.Add(new ValidationIssue
                {
                    Type = IssueType.Warning,
                    Message = $"Lo scenario '{scenario.name}' ha un numero elevato di ostacoli ({totalObstacles}) " +
                             $"che potrebbero essere troppo difficili da superare in successione",
                    ScenarioName = scenario.name,
                    Position = new Vector3(0, 0, scenario.distanceFromStart)
                });
            }
            
            // Verifica scansione della combinazione (se prevede la posizione precisa degli ostacoli)
            if (!scenario.randomPlacement)
            {
                ScanObstacleCombination(scenario);
            }
        }
        
        /// <summary>
        /// Esegue una simulazione semplificata del percorso per verificare se è possibile superare
        /// tutti gli ostacoli nella sequenza
        /// </summary>
        private void ScanObstacleCombination(TutorialScenario scenario)
        {
            // Crea una mappa semplificata degli ostacoli
            var obstacleMap = new Dictionary<float, List<(float x, float y, float width, float height)>>();
            
            // Posiziona tutti gli ostacoli sulla mappa
            foreach (var obstacleSetup in scenario.obstacles)
            {
                PopulateObstacleMap(obstacleMap, obstacleSetup, scenario);
            }
            
            // Verifica corridoi bloccati
            float lastZ = -1;
            foreach (var zPos in obstacleMap.Keys)
            {
                if (lastZ >= 0 && zPos - lastZ < 1.0f) // Ostacoli molto vicini
                {
                    // Verifica se c'è un percorso possibile
                    bool pathExists = CheckPathExistsAt(obstacleMap, lastZ, zPos);
                    
                    if (!pathExists)
                    {
                        _validationIssues.Add(new ValidationIssue
                        {
                            Type = IssueType.Critical,
                            Message = $"Nessun percorso possibile trovato intorno alla posizione z={zPos}m nello scenario '{scenario.name}'",
                            ScenarioName = scenario.name,
                            Position = new Vector3(0, 0, zPos)
                        });
                    }
                }
                
                lastZ = zPos;
            }
        }
        
        /// <summary>
        /// Verifica se esiste un percorso attraverso gli ostacoli presenti alle posizioni z specificate
        /// </summary>
        private bool CheckPathExistsAt(Dictionary<float, List<(float x, float y, float width, float height)>> obstacleMap, 
                                       float z1, float z2)
        {
            // Implementazione semplificata: controlla se esiste almeno uno spazio sufficientemente grande
            // per far passare il personaggio
            
            if (!obstacleMap.TryGetValue(z1, out var obstacles1) || 
                !obstacleMap.TryGetValue(z2, out var obstacles2))
            {
                return true; // Se non ci sono informazioni complete, assumiamo che sia possibile
            }
            
            // Trova gli ostacoli che bloccano il percorso a z1
            var blockedXRanges1 = new List<(float min, float max)>();
            foreach (var obstacle in obstacles1)
            {
                blockedXRanges1.Add((obstacle.x - obstacle.width/2, obstacle.x + obstacle.width/2));
            }
            
            // Trova gli ostacoli che bloccano il percorso a z2
            var blockedXRanges2 = new List<(float min, float max)>();
            foreach (var obstacle in obstacles2)
            {
                blockedXRanges2.Add((obstacle.x - obstacle.width/2, obstacle.x + obstacle.width/2));
            }
            
            // Controlla se esiste una posizione x che è libera in entrambe le posizioni z
            for (float x = -LANE_WIDTH/2; x <= LANE_WIDTH/2; x += characterRadius)
            {
                bool blockedAt1 = IsPositionBlocked(x, blockedXRanges1);
                bool blockedAt2 = IsPositionBlocked(x, blockedXRanges2);
                
                if (!blockedAt1 && !blockedAt2)
                {
                    return true; // Trovato un percorso
                }
            }
            
            return false; // Nessun percorso trovato
        }
        
        /// <summary>
        /// Verifica se una posizione x è bloccata dagli ostacoli
        /// </summary>
        private bool IsPositionBlocked(float x, List<(float min, float max)> blockedRanges)
        {
            foreach (var range in blockedRanges)
            {
                if (x >= range.min - characterRadius && x <= range.max + characterRadius)
                {
                    return true; // Posizione bloccata
                }
            }
            return false;
        }
        
        /// <summary>
        /// Confronta due ObstacleSetup per determinare se sono uguali
        /// </summary>
        private bool AreObstacleSetupsEqual(ObstacleSetup a, ObstacleSetup b)
        {
            return a.obstacleCode == b.obstacleCode
                && a.count == b.count
                && a.placement == b.placement
                && a.randomizeHeight == b.randomizeHeight
                && a.heightRange.Equals(b.heightRange)
                && a.randomizeScale == b.randomizeScale
                && a.scaleRange.Equals(b.scaleRange)
                && Math.Abs(a.startOffset - b.startOffset) < 0.001f;
        }
        
        /// <summary>
        /// Popola la mappa degli ostacoli per uno specifico setup
        /// </summary>
        private void PopulateObstacleMap(Dictionary<float, List<(float x, float y, float width, float height)>> map,
                                         ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            // Determina le dimensioni standard di questo tipo di ostacolo
            float height = DetermineDefaultObstacleHeight(obstacleSetup.obstacleCode);
            float width = DetermineDefaultObstacleWidth(obstacleSetup.obstacleCode);
            
            // Randomizzazione scala se abilitata
            if (obstacleSetup.randomizeScale)
            {
                // Usa il valore massimo per i controlli di sicurezza
                width *= obstacleSetup.scaleRange.y;
            }
            
            // Randomizzazione altezza se abilitata
            if (obstacleSetup.randomizeHeight)
            {
                height = obstacleSetup.heightRange.y; // Usa il massimo per i controlli
            }
            
            // Posiziona gli ostacoli in base al tipo di posizionamento
            for (int i = 0; i < obstacleSetup.count; i++)
            {
                float xPos = 0;
                
                switch (obstacleSetup.placement)
                {
                    case ObstaclePlacement.Center:
                        xPos = CENTER_POSITION;
                        break;
                    case ObstaclePlacement.Left:
                        xPos = LEFT_POSITION;
                        break;
                    case ObstaclePlacement.Right:
                        xPos = RIGHT_POSITION;
                        break;
                    case ObstaclePlacement.Pattern:
                        // Distribuzione uniforme
                        if (obstacleSetup.count > 1)
                        {
                            float pattern = (float)i / (float)(obstacleSetup.count - 1);
                            xPos = Mathf.Lerp(-LANE_WIDTH/2f, LANE_WIDTH/2f, pattern);
                        }
                        break;
                    case ObstaclePlacement.Random:
                        // Per l'analisi, assumiamo la distribuzione regolare
                        xPos = Mathf.Lerp(-LANE_WIDTH/2f, LANE_WIDTH/2f, (float)i / Mathf.Max(1, obstacleSetup.count));
                        break;
                }
                
                float zPos = scenario.distanceFromStart + obstacleSetup.startOffset + (i * scenario.obstacleSpacing);
                
                // Arrotonda la posizione Z al metro più vicino per semplificare l'analisi
                float roundedZ = Mathf.Round(zPos);
                
                // Aggiungi alla mappa
                if (!map.ContainsKey(roundedZ))
                {
                    map[roundedZ] = new List<(float, float, float, float)>();
                }
                
                map[roundedZ].Add((xPos, height, width, height));
            }
        }
        
        /// <summary>
        /// Determina il tipo di ostacolo in base al codice
        /// </summary>
        private ObstacleType DetermineObstacleType(string obstacleCode)
        {
            if (string.IsNullOrEmpty(obstacleCode) || obstacleCode.Length < 2)
                return ObstacleType.Generic;
                
            char prefix = char.ToUpper(obstacleCode[0]);
            
            // Controllo per prefissi speciali
            switch (prefix)
            {
                case 'L': // Lava
                case 'I': // Ice
                case 'D': // Digital barrier
                    return ObstacleType.SpecialObstacle;
            }
            
            // Controllo per tipo in base al numero (convenzione del progetto)
            if (obstacleCode.Length >= 3)
            {
                int typeNumber;
                if (int.TryParse(obstacleCode.Substring(1, 2), out typeNumber))
                {
                    // In base alla convenzione del progetto
                    if (typeNumber >= 1 && typeNumber <= 10)
                        return ObstacleType.Generic;
                    else if (typeNumber >= 11 && typeNumber <= 20) 
                        return ObstacleType.JumpObstacle;
                    else if (typeNumber >= 21 && typeNumber <= 30)
                        return ObstacleType.SlideObstacle;
                    else if (typeNumber >= 31 && typeNumber <= 40)
                        return ObstacleType.SideObstacle;
                    else
                        return ObstacleType.SpecialObstacle;
                }
            }
            
            // Default: verifica in base al codice specifico per U (Universal)
            if (prefix == 'U')
            {
                if (obstacleCode == "U01" || obstacleCode == "U04")
                    return ObstacleType.Generic;
                else if (obstacleCode == "U02" || obstacleCode == "U05")
                    return ObstacleType.JumpObstacle;
                else if (obstacleCode == "U03" || obstacleCode == "U06")
                    return ObstacleType.SlideObstacle;
                else if (obstacleCode == "U07" || obstacleCode == "U08")
                    return ObstacleType.SideObstacle;
            }
            
            return ObstacleType.Generic;
        }
        
        /// <summary>
        /// Determina l'altezza predefinita di un ostacolo in base al codice
        /// </summary>
        private float DetermineDefaultObstacleHeight(string obstacleCode)
        {
            ObstacleType type = DetermineObstacleType(obstacleCode);
            
            switch (type)
            {
                case ObstacleType.JumpObstacle:
                    return 0.5f;
                case ObstacleType.SlideObstacle:
                    return 1.5f;
                case ObstacleType.SideObstacle:
                    return 1.2f;
                case ObstacleType.SpecialObstacle:
                    return 1.0f;
                default:
                    return 1.0f;
            }
        }
        
        /// <summary>
        /// Determina la larghezza predefinita di un ostacolo in base al codice
        /// </summary>
        private float DetermineDefaultObstacleWidth(string obstacleCode)
        {
            ObstacleType type = DetermineObstacleType(obstacleCode);
            
            switch (type)
            {
                case ObstacleType.JumpObstacle:
                    return 1.0f;
                case ObstacleType.SlideObstacle:
                    return 1.0f;
                case ObstacleType.SideObstacle:
                    return 1.5f;
                case ObstacleType.SpecialObstacle:
                    return 2.0f;
                default:
                    return 1.0f;
            }
        }
        
        /// <summary>
        /// Stima le posizioni degli ostacoli in base alla configurazione
        /// </summary>
        private List<(float x, float y, float z)> EstimateObstaclePositions(ObstacleSetup obstacleSetup, TutorialScenario scenario)
        {
            var positions = new List<(float x, float y, float z)>();
            
            for (int i = 0; i < obstacleSetup.count; i++)
            {
                float xPos = 0;
                switch (obstacleSetup.placement)
                {
                    case ObstaclePlacement.Center:
                        xPos = CENTER_POSITION;
                        break;
                    case ObstaclePlacement.Left:
                        xPos = LEFT_POSITION;
                        break;
                    case ObstaclePlacement.Right:
                        xPos = RIGHT_POSITION;
                        break;
                    case ObstaclePlacement.Pattern:
                        // Distribuzione uniforme degli ostacoli in un pattern attraverso la corsia
                        if (obstacleSetup.count > 1)
                        {
                            float pattern = (float)i / (float)(obstacleSetup.count - 1);
                            xPos = Mathf.Lerp(-LANE_WIDTH/2f, LANE_WIDTH/2f, pattern);
                        }
                        break;
                    case ObstaclePlacement.Random:
                        // Rappresentazione deterministica per l'analisi
                        xPos = Mathf.Lerp(-LANE_WIDTH/2f, LANE_WIDTH/2f, (float)i / Mathf.Max(1, obstacleSetup.count));
                        break;
                }
                
                float zPos;
                if (scenario.randomPlacement)
                {
                    // Rappresentazione deterministica per l'analisi
                    zPos = scenario.distanceFromStart + obstacleSetup.startOffset + (i * (scenario.obstacleSpacing / 2));
                }
                else
                {
                    zPos = scenario.distanceFromStart + obstacleSetup.startOffset + (i * scenario.obstacleSpacing);
                }
                
                // Stima altezza standard (o dal range se randomizzato)
                float yPos = obstacleSetup.randomizeHeight
                    ? (obstacleSetup.heightRange.x + obstacleSetup.heightRange.y) / 2 // valore medio
                    : DetermineDefaultObstacleHeight(obstacleSetup.obstacleCode);
                
                positions.Add((xPos, yPos, zPos));
            }
            
            return positions;
        }
        
        /// <summary>
        /// Visualizza debug gizmo degli ostacoli e problemi
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showDebugVisualization || tutorialInitializer == null)
                return;
                
            // Ottieni i risultati attuali
            var result = ValidateLevel();
            
            // Posizione di partenza
            Vector3 startPos = tutorialInitializer.transform.position;
            
            // Disegna problemi
            foreach (var issue in result.Issues)
            {
                Vector3 issuePos = startPos + issue.Position;
                
                // Colore in base al tipo di problema
                if (issue.Type == IssueType.Critical)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(issuePos, 1.0f);
                    
                    // Disegna una X rossa
                    float size = 2.0f;
                    Gizmos.DrawLine(issuePos + new Vector3(-size, 0, -size), issuePos + new Vector3(size, 0, size));
                    Gizmos.DrawLine(issuePos + new Vector3(-size, 0, size), issuePos + new Vector3(size, 0, -size));
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(issuePos, 0.5f);
                    
                    // Disegna punto esclamativo
                    Gizmos.DrawLine(issuePos + new Vector3(0, 0, -0.5f), issuePos + new Vector3(0, 0, 0.5f));
                    Gizmos.DrawSphere(issuePos + new Vector3(0, 0, 1.0f), 0.2f);
                }
            }
        }
    }
    
    /// <summary>
    /// Rappresenta un problema di validazione
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// Tipo di problema (critico o avviso)
        /// </summary>
        public IssueType Type;
        
        /// <summary>
        /// Messaggio descrittivo del problema
        /// </summary>
        public string Message;
        
        /// <summary>
        /// Nome dello scenario in cui si trova il problema
        /// </summary>
        public string ScenarioName;
        
        /// <summary>
        /// Posizione del problema nel mondo
        /// </summary>
        public Vector3 Position;
    }
    
    
    /// <summary>
    /// Tipo di problema di validazione
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// Problema critico che rende il livello impossibile
        /// </summary>
        Critical,
        
        /// <summary>
        /// Avviso di possibile problema non bloccante
        /// </summary>
        Warning
    }
    
    /// <summary>
    /// Risultato della validazione
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Se true, il livello è completabile
        /// </summary>
        public bool IsValid;
        
        /// <summary>
        /// Lista di problemi e avvisi
        /// </summary>
        public List<ValidationIssue> Issues;
    }
    
    /// <summary>
    /// Tipi di ostacoli
    /// </summary>
    public enum ObstacleType
    {
        /// <summary>
        /// Ostacolo generico
        /// </summary>
        Generic,
        
        /// <summary>
        /// Ostacolo da saltare
        /// </summary>
        JumpObstacle,
        
        /// <summary>
        /// Ostacolo da attraversare con scivolata
        /// </summary>
        SlideObstacle,
        
        /// <summary>
        /// Ostacolo da evitare con spostamenti laterali
        /// </summary>
        SideObstacle,
        
        /// <summary>
        /// Ostacolo speciale (lava, ghiaccio, barriere digitali, ecc.)
        /// </summary>
        SpecialObstacle
    }
}