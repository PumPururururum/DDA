using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Projectile.Factory;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class WeaponShootSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<WeaponShootRequest, Weapon>> _filter;
        private readonly EcsPoolInject<WeaponShootRequest> _requestPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        private readonly EcsPoolInject<CombatTeam> _teamPool;
        private readonly EcsPoolInject<UnityTransform> _transformPool;

        private WeaponsStaticData _weaponsStaticData;
        private IProjectileFactory _projectileFactory;

        public void Init(IEcsSystems systems)
        {
            var staticDataService = ServiceLocator.Resolve<StaticDataService>();
            _weaponsStaticData = staticDataService?.WeaponsStaticData;
            _projectileFactory = ServiceLocator.Resolve<IProjectileFactory>();
        }

        public void Run(IEcsSystems systems)
        {
            if (_weaponsStaticData == null || _projectileFactory == null)
                return;

            foreach (var entity in _filter.Value)
            {
                ref var requestData = ref _requestPool.Value.Get(entity);
                ref var weapon = ref _weaponPool.Value.Get(entity);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);

                if (weaponData == null)
                    continue;

                requestData.BurstTickTime += Time.deltaTime;
                if (requestData.BurstTickTime < weaponData.BurstInterval)
                    continue;

                requestData.BurstTickTime -= weaponData.BurstInterval;

                if (!TryGetShootPosition(entity, ref weapon, out var position))
                    continue;

                var baseDirection = GetShootDirection(position, requestData.ShootPosition, ref weapon);
                var team = _teamPool.Value.Has(entity) ? _teamPool.Value.Get(entity).Value : CombatTeamId.Neutral;

                int projectileCount = Mathf.Max(1, weaponData.ProjectilesPerBurst);
                float totalSpread = projectileCount > 1 ? 10f : 0f;
                float step = projectileCount > 1 ? totalSpread / (projectileCount - 1) : 0f;
                float start = -totalSpread * 0.5f;

                for (int i = 0; i < projectileCount; i++)
                {
                    var angle = start + step * i;
                    var direction = (Vector2)(Quaternion.Euler(0f, 0f, angle) * baseDirection);
                    _projectileFactory.Create(weaponData.ProjectileId, position, direction.normalized, team);
                    ProjectAdaptiveDifficultyBootstrap.Instance?.ReportShot(team);
                }

                requestData.BurstCount += 1;
                if (requestData.BurstCount >= weaponData.BurstsPerShot)
                    _requestPool.Value.Del(entity);
            }
        }

        private bool TryGetShootPosition(int entity, ref Weapon weapon, out Vector2 position)
        {
            if (weapon.FirePointRef != null)
            {
                position = weapon.FirePointRef.position;
                return true;
            }

            if (_transformPool.Value.Has(entity) && _transformPool.Value.Get(entity).Ref != null)
            {
                position = _transformPool.Value.Get(entity).Ref.position;
                return true;
            }

            position = default;
            return false;
        }

        private static Vector2 GetShootDirection(Vector2 shootPosition, Vector2 targetPosition, ref Weapon weapon)
        {
            var delta = targetPosition - shootPosition;
            if (delta.sqrMagnitude > 0.0001f)
                return delta.normalized;

            if (weapon.FirePointRef != null)
            {
                var firePointDirection = (Vector2)weapon.FirePointRef.right;
                if (firePointDirection.sqrMagnitude > 0.0001f)
                    return firePointDirection.normalized;
            }

            return Vector2.right;
        }
    }
}
