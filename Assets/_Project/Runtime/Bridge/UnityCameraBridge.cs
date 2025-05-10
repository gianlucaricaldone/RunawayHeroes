// Path: Assets/_Project/Runtime/Bridge/UnityCameraBridge.cs
using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using RunawayHeroes.ECS.Components.Core;
using RunawayHeroes.ECS.Components.Gameplay;
using RunawayHeroes.Utilities.ECSCompatibility; 

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// Bridge che collega la Camera di Unity con il sistema ECS.
    /// Permette al sistema ECS di controllare o essere informato sulla camera.
    /// </summary>
    [AddComponentMenu("RunawayHeroes/Bridges/Camera Bridge")]
    [RequireComponent(typeof(Camera))]
    public class UnityCameraBridge : MonoBehaviour
    {
        [Header("Impostazioni Camera")]
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private float lookAheadDistance = 3f;
        [SerializeField] private Vector3 offset = new Vector3(0, 2, -5);
        
        // Riferimenti interni
        private Camera _camera;
        private Entity _cameraEntity;
        private EntityManager _entityManager;
        
        // Classe Baker per la conversione
        public class CameraBridgeBaker : Baker<UnityCameraBridge>
        {
            public override void Bake(UnityCameraBridge authoring)
            {
                // Crea un'entità per la camera
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Aggiunge il componente di trasformazione
                AddComponent(entity, new TransformComponent
                {
                    Position = authoring.transform.position,
                    Rotation = authoring.transform.rotation,
                    Scale = 1.0f
                });
                
                // Aggiunge un componente con i parametri della camera
                AddComponent(entity, new CameraSettingsComponent
                {
                    FollowSpeed = authoring.followSpeed,
                    SmoothTime = authoring.smoothTime,
                    LookAheadDistance = authoring.lookAheadDistance,
                    Offset = new float3(authoring.offset.x, authoring.offset.y, authoring.offset.z)
                });
                
                // Aggiunge un componente che memorizza il target (sarà impostato in fase di runtime)
                AddComponent<CameraTargetComponent>(entity);
                
                // Aggiunge un tag per identificare questa entità come camera
                AddComponent<CameraTag>(entity);
            }
        }
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                Debug.LogError("UnityCameraBridge richiede un componente Camera!");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            // Ottieni il world di default e l'entity manager
            World defaultWorld = RunawayWorldExtensions.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                _entityManager = defaultWorld.EntityManager;
                
                // Trova l'entità della camera creata dal baker
                var query = _entityManager.CreateEntityQuery(typeof(CameraTag));
                if (query.CalculateEntityCount() > 0)
                {
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                    _cameraEntity = entities[0];
                    entities.Dispose();
                    
                    Debug.Log($"Camera Bridge trovato: Entity {_cameraEntity.Index}");
                    
                    // Trova un'entità giocatore e imposta come target della camera
                    var playerQuery = _entityManager.CreateEntityQuery(typeof(PlayerTag));
                    if (playerQuery.CalculateEntityCount() > 0)
                    {
                        var playerEntities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                        var targetComponent = new CameraTargetComponent
                        {
                            TargetEntity = playerEntities[0],
                            IsFollowing = true
                        };
                        _entityManager.SetComponentData(_cameraEntity, targetComponent);
                        
                        playerEntities.Dispose();
                    }
                }
                else
                {
                    Debug.LogWarning("Nessuna entità Camera trovata nel sistema ECS.");
                }
            }
        }
        
        private void LateUpdate()
        {
            if (_entityManager == null || !_entityManager.Exists(_cameraEntity))
                return;
                
            // Aggiorna la posizione dell'entità camera in base alla posizione reale della camera Unity
            var transformComponent = _entityManager.GetComponentData<TransformComponent>(_cameraEntity);
            transformComponent.Position = transform.position;
            transformComponent.Rotation = transform.rotation;
            _entityManager.SetComponentData(_cameraEntity, transformComponent);
            
            // Se c'è un target da seguire, gestisci il movimento della camera
            if (_entityManager.HasComponent<CameraTargetComponent>(_cameraEntity))
            {
                var targetComponent = _entityManager.GetComponentData<CameraTargetComponent>(_cameraEntity);
                if (targetComponent.IsFollowing && _entityManager.Exists(targetComponent.TargetEntity) &&
                    _entityManager.HasComponent<TransformComponent>(targetComponent.TargetEntity))
                {
                    var targetTransform = _entityManager.GetComponentData<TransformComponent>(targetComponent.TargetEntity);
                    var settings = _entityManager.GetComponentData<CameraSettingsComponent>(_cameraEntity);
                    
                    // Calcola la posizione target della camera
                    Vector3 targetPosition = new Vector3(
                        targetTransform.Position.x,
                        targetTransform.Position.y,
                        targetTransform.Position.z
                    ) + new Vector3(settings.Offset.x, settings.Offset.y, settings.Offset.z);
                    
                    // Aggiungi look ahead in base alla direzione di movimento
                    if (_entityManager.HasComponent<MovementComponent>(targetComponent.TargetEntity))
                    {
                        var movement = _entityManager.GetComponentData<MovementComponent>(targetComponent.TargetEntity);
                        if (movement.IsMoving)
                        {
                            targetPosition.x += movement.MoveDirection.x * settings.LookAheadDistance;
                        }
                    }
                    
                    // Applica smoothing al movimento della camera
                    transform.position = Vector3.Lerp(
                        transform.position,
                        targetPosition,
                        settings.FollowSpeed * Time.deltaTime
                    );
                    
                    // Guarda sempre verso il target
                    transform.LookAt(new Vector3(
                        targetTransform.Position.x,
                        targetTransform.Position.y + 1.0f, // Guarda leggermente sopra il personaggio
                        targetTransform.Position.z
                    ));
                }
            }
        }
    }
    
    /// <summary>
    /// Tag per identificare l'entità camera
    /// </summary>
    public struct CameraTag : IComponentData { }
    
    /// <summary>
    /// Componente che memorizza le impostazioni della camera
    /// </summary>
    public struct CameraSettingsComponent : IComponentData
    {
        public float FollowSpeed;
        public float SmoothTime;
        public float LookAheadDistance;
        public float3 Offset;
    }
    
    /// <summary>
    /// Componente che tiene traccia del target della camera
    /// </summary>
    public struct CameraTargetComponent : IComponentData
    {
        public Entity TargetEntity;
        public bool IsFollowing;
    }
}