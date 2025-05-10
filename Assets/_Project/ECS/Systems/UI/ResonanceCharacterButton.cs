// Path: Assets/_Project/ECS/Systems/UI/ResonanceCharacterButton.cs
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using RunawayHeroes.ECS.Components.Input;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.Utilities.ECSCompatibility;

namespace RunawayHeroes.ECS.Systems.UI
{
    /// <summary>
    /// Classe MonoBehaviour che gestisce il click sul pulsante di un personaggio.
    /// Utilizzata per evitare il problema delle lambda in struct.
    /// </summary>
    public class ResonanceCharacterButton : MonoBehaviour
    {
        [Tooltip("Indice del personaggio associato a questo pulsante")]
        public int CharacterIndex;
        
        [Tooltip("Riferimento all'effetto onda")]
        public GameObject WaveEffect;
        
        // Riferimento al mondo
        private Unity.Entities.World _world;
        // Nota: Il timer per l'effetto onda è gestito in ResonanceUISystem
        
        private void Start()
        {
            // Ottiene il mondo corrente
            _world = RunawayWorldExtensions.DefaultGameObjectInjectionWorld;
            
            // Ottiene il pulsante
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }
        
        /// <summary>
        /// Gestisce il click sul pulsante del personaggio
        /// </summary>
        public void OnButtonClick()
        {
            if (_world != null)
            {
                // Ottieni un riferimento alla SystemState
                var resonanceSystem = _world.GetExistingSystemManaged<ResonanceUISystemGroup>();
                if (resonanceSystem == null)
                {
                    Debug.LogError("ResonanceUISystemGroup non trovata!");
                    return;
                }
                
                var systemState = _world.Unmanaged.ResolveSystemStateRef(resonanceSystem.SystemHandle);
                
                // Ottieni l'entità del giocatore
                var entityQuery = systemState.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<FragmentResonanceComponent>(),
                    ComponentType.ReadOnly<ResonanceInputComponent>());
                    
                if (entityQuery.IsEmpty) 
                {
                    Debug.LogError("Nessuna entità di risonanza trovata!");
                    return;
                }
                
                Entity playerEntity = entityQuery.GetSingletonEntity();
                
                // Ottieni il command buffer usando il metodo normale
                var ecbSystem = _world.GetExistingSystemManaged<EndSimulationEntityCommandBufferSystem>();
                var commandBuffer = ecbSystem.CreateCommandBuffer();
                
                // Aggiorna il componente di input della Risonanza
                commandBuffer.SetComponent(playerEntity, new ResonanceInputComponent
                {
                    SwitchToCharacterIndex = CharacterIndex,
                    NewCharacterUnlocked = false,
                    NewCharacterEntity = Entity.Null,
                    ResonanceLevelUp = false,
                    NewResonanceLevel = 0
                });
            }
            
            // Mostra l'effetto onda
            if (WaveEffect != null)
            {
                WaveEffect.SetActive(true);
                
                // Avvia l'animazione
                Animator waveAnimator = WaveEffect.GetComponent<Animator>();
                if (waveAnimator != null)
                {
                    waveAnimator.SetTrigger("Play");
                }
            }
        }
    }
    
    /// <summary>
    /// Classe helper per accedere alla SystemState
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ResonanceUISystemGroup : ComponentSystemGroup
    {
        // Proprietà per accedere a SystemState
        public SystemState SystemState => this.GetCheckedState();
    }
}