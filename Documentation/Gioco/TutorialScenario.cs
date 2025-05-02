// Path: TutorialScenario.cs
using System;
using UnityEngine;

namespace RunawayHeroes.Gameplay
{
    /// <summary>
    /// Rappresenta uno scenario di insegnamento nel tutorial
    /// </summary>
    [Serializable]
    public struct TutorialScenario
    {
        /// <summary>
        /// Nome identificativo dello scenario
        /// </summary>
        [Tooltip("Nome identificativo dello scenario")]
        public string name;
        
        /// <summary>
        /// Distanza dall'inizio del livello in cui inizia questo scenario
        /// </summary>
        [Tooltip("Distanza dall'inizio del livello (metri)")]
        public float distanceFromStart;
        
        /// <summary>
        /// Array di configurazioni per i diversi tipi di ostacoli in questo scenario
        /// </summary>
        [Tooltip("Configurazioni per i diversi tipi di ostacoli")]
        public ObstacleSetup[] obstacles;
        
        /// <summary>
        /// Messaggio di istruzione da mostrare al giocatore
        /// </summary>
        [Tooltip("Messaggio di istruzione per il giocatore")]
        [TextArea(2, 5)]
        public string instructionMessage;
        
        /// <summary>
        /// Durata per cui visualizzare il messaggio (secondi)
        /// </summary>
        [Tooltip("Durata del messaggio (secondi)")]
        public float messageDuration;
        
        /// <summary>
        /// Se true, gli ostacoli saranno posizionati casualmente nell'area dello scenario
        /// invece che ad intervalli regolari
        /// </summary>
        [Tooltip("Posizionamento casuale degli ostacoli nell'area")]
        public bool randomPlacement;
        
        /// <summary>
        /// Distanza tra gli ostacoli se randomPlacement è false
        /// </summary>
        [Tooltip("Distanza tra gli ostacoli (metri)")]
        public float obstacleSpacing;
        
        /// <summary>
        /// Crea uno scenario di base per il tutorial
        /// </summary>
        public static TutorialScenario CreateBasic(string name, float distance, string message)
        {
            return new TutorialScenario
            {
                name = name,
                distanceFromStart = distance,
                obstacles = new ObstacleSetup[] 
                {
                    ObstacleSetup.CreateDefault()
                },
                instructionMessage = message,
                messageDuration = 5f,
                randomPlacement = false,
                obstacleSpacing = 10f
            };
        }
        
        /// <summary>
        /// Crea uno scenario per insegnare il salto
        /// </summary>
        public static TutorialScenario CreateJumpScenario(float distance)
        {
            return new TutorialScenario
            {
                name = "Jump_Training",
                distanceFromStart = distance,
                obstacles = new ObstacleSetup[] 
                {
                    ObstacleSetup.CreateJumpObstacles("U02", 3),
                    ObstacleSetup.CreateJumpObstacles("U05", 2) // Aggiunge un secondo tipo di ostacolo
                },
                instructionMessage = "Premi SPAZIO per saltare gli ostacoli bassi!",
                messageDuration = 5f,
                randomPlacement = false,
                obstacleSpacing = 8f
            };
        }
        
        /// <summary>
        /// Crea uno scenario per insegnare la scivolata
        /// </summary>
        public static TutorialScenario CreateSlideScenario(float distance)
        {
            return new TutorialScenario
            {
                name = "Slide_Training",
                distanceFromStart = distance,
                obstacles = new ObstacleSetup[] 
                {
                    ObstacleSetup.CreateSlideObstacles("U03", 3)
                },
                instructionMessage = "Premi C per scivolare sotto gli ostacoli alti!",
                messageDuration = 5f,
                randomPlacement = false,
                obstacleSpacing = 10f
            };
        }
        
        /// <summary>
        /// Crea uno scenario per insegnare i movimenti laterali
        /// </summary>
        public static TutorialScenario CreateSideStepScenario(float distance)
        {
            var leftObstacles = ObstacleSetup.CreateSideStepObstacles("U01", 3);
            leftObstacles.placement = ObstaclePlacement.Left;
            
            var rightObstacles = ObstacleSetup.CreateSideStepObstacles("U01", 3);
            rightObstacles.placement = ObstaclePlacement.Right;
            
            return new TutorialScenario
            {
                name = "SideStep_Training",
                distanceFromStart = distance,
                obstacles = new ObstacleSetup[] 
                {
                    leftObstacles,
                    rightObstacles
                },
                instructionMessage = "Usa A e D per spostarti lateralmente ed evitare gli ostacoli!",
                messageDuration = 5f,
                randomPlacement = true,
                obstacleSpacing = 12f
            };
        }
        
        /// <summary>
        /// Crea uno scenario con diversi tipi di ostacoli per movimenti combinati
        /// </summary>
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
    }
}