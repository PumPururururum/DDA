using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Infrastructure.InputService;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class PlayerAimSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<PlayerTag, UnityTransform, Weapon>> _filter;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;

        private IInputService _inputService;

        public void Init(IEcsSystems systems)
        {
            _inputService = ServiceLocator.Resolve<IInputService>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var transformData = ref _transformPool.Value.Get(entity);
                ref var weapon = ref _weaponPool.Value.Get(entity);

                var aimPosition = _inputService.AimWorldPosition;
                var origin = transformData.Ref.position;
                var direction = (Vector2)(aimPosition - (Vector2)origin);
                if (direction.sqrMagnitude < 0.001f)
                    continue;

                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transformData.Ref.rotation = Quaternion.Euler(0f, 0f, angle);

                if (weapon.FirePointRef != null)
                    weapon.FirePointRef.right = direction.normalized;
            }
        }
    }
}
