using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Infrastructure.InputService;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class PlayerShootInputSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<PlayerTag, Weapon>> _filter;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        private readonly EcsPoolInject<WeaponShootRequest> _shootRequestPool;
        private readonly EcsPoolInject<WeaponReloadRequest> _reloadRequestPool;

        private IInputService _inputService;
        private WeaponsStaticData _weaponsStaticData;

        public void Init(IEcsSystems systems)
        {
            _inputService = ServiceLocator.Resolve<IInputService>();
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var weapon = ref _weaponPool.Value.Get(entity);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);
                if (weaponData == null)
                    continue;

                if (_inputService.IsReloadPressed)
                {
                    if (!_reloadRequestPool.Value.Has(entity) && weapon.TotalAmmo > 0 && weapon.MagazineAmmo < weaponData.MaxMagazineSize)
                        _reloadRequestPool.Value.Add(entity);
                    continue;
                }

                if (!_inputService.IsShootHeld || _shootRequestPool.Value.Has(entity) || _reloadRequestPool.Value.Has(entity))
                    continue;

                if (weapon.MagazineAmmo < weaponData.AmmoPerShot)
                {
                    if (weapon.TotalAmmo > 0 && !_reloadRequestPool.Value.Has(entity))
                        _reloadRequestPool.Value.Add(entity);
                    continue;
                }

                weapon.MagazineAmmo -= weaponData.AmmoPerShot;
                _shootRequestPool.Value.Add(entity).Setup(weaponData.BurstInterval, _inputService.AimWorldPosition);
            }
        }
    }
}
