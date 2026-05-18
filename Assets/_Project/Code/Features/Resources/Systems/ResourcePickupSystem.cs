using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Resources.Components;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _Project.Code.Features.Resources.Systems
{
    public sealed class ResourcePickupSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<PlayerTag, UnityTransform>> _playerFilter;
        private readonly EcsFilterInject<Inc<ResourceTag, UnityTransform>> _resourceFilter;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        private readonly EcsPoolInject<ResourceTag> _resourcePool;
        private readonly EcsPoolInject<Health> _healthPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        private readonly EcsWorldInject _world;

        private IMemoryPoolService _memoryPoolService;

        public void Init(IEcsSystems systems)
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }

        public void Run(IEcsSystems systems)
        {
            if (!TryGetPlayer(out var playerEntity, out var playerPosition))
                return;

            foreach (var entity in _resourceFilter.Value)
            {
                ref var resource = ref _resourcePool.Value.Get(entity);
                var resourceTransform = _transformPool.Value.Get(entity).Ref;

                if (resourceTransform == null)
                    continue;

                var resourcePosition = resourceTransform.position;
                float pickupRadius = Mathf.Max(0f, resource.PickupRadius);

                if ((resourcePosition - playerPosition).sqrMagnitude > pickupRadius * pickupRadius)
                    continue;

                if (!ApplyPickup(playerEntity, ref resource))
                    continue;

                UnspawnResource(ref resource);
                _world.Value.DelEntity(entity);
            }
        }

        private bool TryGetPlayer(out int playerEntity, out Vector3 playerPosition)
        {
            foreach (var entity in _playerFilter.Value)
            {
                var playerTransform = _transformPool.Value.Get(entity).Ref;
                if (playerTransform == null)
                    continue;

                playerEntity = entity;
                playerPosition = playerTransform.position;
                return true;
            }

            playerEntity = -1;
            playerPosition = default;
            return false;
        }

        private bool ApplyPickup(int playerEntity, ref ResourceTag resource)
        {
            switch (resource.ResourceId)
            {
                case ResourceId.HealthSmall:
                    if (!_healthPool.Value.Has(playerEntity))
                        return false;

                    ref var health = ref _healthPool.Value.Get(playerEntity);
                    int previousHealth = health.CurrentValue;
                    health.CurrentValue = Mathf.Min(health.MaxValue, health.CurrentValue + resource.Amount);

                    if (health.CurrentValue > previousHealth)
                        ProjectAdaptiveDifficultyBootstrap.Instance?.ReportPickup(AdaptivePickupType.Health);

                    return true;

                case ResourceId.AmmoSmall:
                    if (!_weaponPool.Value.Has(playerEntity))
                        return false;

                    ref var weapon = ref _weaponPool.Value.Get(playerEntity);
                    weapon.TotalAmmo += Mathf.Max(0, resource.Amount);
                    ProjectAdaptiveDifficultyBootstrap.Instance?.ReportPickup(AdaptivePickupType.Ammo);
                    return true;

                default:
                    return false;
            }
        }

        private void UnspawnResource(ref ResourceTag resource)
        {
            if (_memoryPoolService == null || resource.GameObjectRef == null)
                return;

            var poolId = resource.ResourceId == ResourceId.HealthSmall
                ? MemoryPoolId.HealthPickup
                : MemoryPoolId.AmmoPickup;

            _memoryPoolService.UnspawnGameObject(poolId, resource.GameObjectRef);
        }
    }
}
