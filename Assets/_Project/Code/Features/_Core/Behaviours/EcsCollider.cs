using _ExampleProject.Code.Features._Core.Requests;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class EcsCollider : MonoBehaviour
    {
        [SerializeField] private EcsEntity _entity;

        private EcsWorld _eventWorld;

        public void Construct(EcsWorld eventWorld)
        {
            _eventWorld = eventWorld;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CreateCollisionEvent(collision.collider.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CreateCollisionEvent(other.gameObject);
        }

        private void CreateCollisionEvent(GameObject collisionRef)
        {
            if (_eventWorld == null || _entity == null || collisionRef == null)
                return;

            var entity = _eventWorld.NewEntity();

            _eventWorld.GetPool<CollisionEvent>().Add(entity).Setup(
                entity: _entity.Index,
                collisionRef: collisionRef);

            _eventWorld.GetPool<OneFrameTag>().Add(entity);
        }
    }
}
