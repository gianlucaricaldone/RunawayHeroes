// Path: Assets/_Project/Runtime/Bridge/UnityPhysicsBridge.cs
using Unity.Entities;
using UnityEngine;
using RunawayHeroes.ECS.Components.Core;

namespace RunawayHeroes.Runtime.Bridge
{
    /// <summary>
    /// Bridge che collega il sistema di fisica di Unity con il sistema ECS.
    /// Permette di sincronizzare collisioni e forze tra i due sistemi.
    /// </summary>
    [AddComponentMenu("RunawayHeroes/Bridges/Physics Bridge")]
    [RequireComponent(typeof(Rigidbody))]
    public class UnityPhysicsBridge : MonoBehaviour
    {
        [Header("Impostazioni Fisica")]
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float mass = 1f;
        [SerializeField] private float friction = 0.5f;
        [SerializeField] private bool useGravity = true;
        [SerializeField] private LayerMask groundLayers;
        
        // Riferimenti interni
        private Rigidbody _rigidbody;
        private Entity _physicsEntity;
        private EntityManager _entityManager;
        private bool _isGrounded;
        
        // Classe Baker per la conversione
        public class PhysicsBridgeBaker : Baker<UnityPhysicsBridge>
        {
            public override void Bake(UnityPhysicsBridge authoring)
            {
                // Crea un'entità per la fisica
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Aggiunge il componente di trasformazione
                AddComponent(entity, new TransformComponent
                {
                    Position = authoring.transform.position,
                    Rotation = authoring.transform.rotation,
                    Scale = 1.0f
                });
                
                // Aggiunge il componente di fisica
                AddComponent(entity, new PhysicsComponent
                {
                    Velocity = Unity.Mathematics.float3.zero,
                    Acceleration = Unity.Mathematics.float3.zero,
                    Mass = authoring.mass,
                    Gravity = authoring.gravity,
                    Friction = authoring.friction,
                    UseGravity = authoring.useGravity,
                    IsGrounded = false
                });
                
                // Aggiunge un tag per identificare questa entità
                AddComponent<PhysicsTag>(entity);
            }
        }
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody == null)
            {
                Debug.LogError("UnityPhysicsBridge richiede un componente Rigidbody!");
                enabled = false;
                return;
            }
            
            // Configura il Rigidbody
            _rigidbody.mass = mass;
            _rigidbody.useGravity = useGravity;
            _rigidbody.freezeRotation = true; // Per evitare rotazioni indesiderate
        }
        
        private void Start()
        {
            // Ottieni il world di default e l'entity manager
            World defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null)
            {
                _entityManager = defaultWorld.EntityManager;
                
                // Trova l'entità fisica creata dal baker
                var query = _entityManager.CreateEntityQuery(typeof(PhysicsTag));
                if (query.CalculateEntityCount() > 0)
                {
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                    _physicsEntity = entities[0];
                    entities.Dispose();
                    
                    Debug.Log($"Physics Bridge trovato: Entity {_physicsEntity.Index}");
                }
                else
                {
                    Debug.LogWarning("Nessuna entità Physics trovata nel sistema ECS.");
                }
            }
        }
        
        private void FixedUpdate()
        {
            if (_entityManager == null || !_entityManager.Exists(_physicsEntity))
                return;
                
            // Controlla se l'oggetto è a terra
            CheckGrounded();
            
            // Sincronizza i dati dal sistema ECS al Rigidbody Unity
            if (_entityManager.HasComponent<PhysicsComponent>(_physicsEntity))
            {
                var physicsComponent = _entityManager.GetComponentData<PhysicsComponent>(_physicsEntity);
                
                // Applica la velocità dal sistema ECS al Rigidbody
                _rigidbody.linearVelocity = new Vector3(
                    physicsComponent.Velocity.x,
                    physicsComponent.Velocity.y,
                    physicsComponent.Velocity.z
                );
                
                // Aggiorna lo stato di IsGrounded nel componente ECS
                physicsComponent.IsGrounded = _isGrounded;
                _entityManager.SetComponentData(_physicsEntity, physicsComponent);
            }
            
            // Aggiorna la posizione nell'entità ECS in base alla posizione attuale del GameObject
            if (_entityManager.HasComponent<TransformComponent>(_physicsEntity))
            {
                var transformComponent = _entityManager.GetComponentData<TransformComponent>(_physicsEntity);
                transformComponent.Position = new Unity.Mathematics.float3(
                    transform.position.x,
                    transform.position.y,
                    transform.position.z
                );
                transformComponent.Rotation = new Unity.Mathematics.quaternion(
                    transform.rotation.x,
                    transform.rotation.y,
                    transform.rotation.z,
                    transform.rotation.w
                );
                _entityManager.SetComponentData(_physicsEntity, transformComponent);
            }
        }
        
        private void CheckGrounded()
        {
            // Esegue un raycast verso il basso per verificare se il personaggio è a terra
            float rayLength = 0.2f;
            Vector3 rayStart = transform.position + Vector3.up * 0.1f;
            
            _isGrounded = Physics.Raycast(rayStart, Vector3.down, rayLength, groundLayers);
            
            // Visualizzazione di debug
            if (Debug.isDebugBuild)
            {
                Debug.DrawRay(rayStart, Vector3.down * rayLength, _isGrounded ? Color.green : Color.red);
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (_entityManager == null || !_entityManager.Exists(_physicsEntity))
                return;
                
            // Gestisce le collisioni di Unity e le comunica al sistema ECS
            // Qui potresti creare eventi di collisione ECS per informare i sistemi
        }
    }
    
    /// <summary>
    /// Tag per identificare l'entità fisica
    /// </summary>
    public struct PhysicsTag : IComponentData { }
}