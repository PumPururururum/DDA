using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features._Core.States;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class ShootEntityStateSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<ShootEntityState, AgentEntity>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<ShootEntityState> _shootStatePool = EcsWorlds.BT_STATES;

        private readonly EcsPoolInject<WeaponShootRequest> _shootRequestPool;
        private readonly EcsPoolInject<WeaponReloadRequest> _reloadRequestPool;
        private readonly EcsPoolInject<Weapon> _weaponPool;
        private readonly EcsPoolInject<UnityTransform> _transformPool;

        private WeaponsStaticData _weaponsStaticData;

        public void Init(IEcsSystems systems)
        {
            var staticDataService = ServiceLocator.Resolve<StaticDataService>();
            _weaponsStaticData = staticDataService?.WeaponsStaticData;
        }

        public void Run(IEcsSystems systems)
        {
            if (_weaponsStaticData == null)
                return;

            foreach (var entity in _filter.Value)
            {
                ref var stateData = ref _shootStatePool.Value.Get(entity);
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;

                if (stateData.StateStatus == NodeStatus.Success || stateData.StateStatus == NodeStatus.Failure)
                    continue;

                if (stateData.EntityIndex < 0 || !_transformPool.Value.Has(stateData.EntityIndex) || _transformPool.Value.Get(stateData.EntityIndex).Ref == null)
                {
                    stateData.StateStatus = NodeStatus.Failure;
                    continue;
                }

                if (!_weaponPool.Value.Has(agentIndex))
                {
                    stateData.StateStatus = NodeStatus.Failure;
                    continue;
                }

                ref var weapon = ref _weaponPool.Value.Get(agentIndex);
                var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);

                if (weaponData == null)
                {
                    stateData.StateStatus = NodeStatus.Failure;
                    continue;
                }

                stateData.TickTime += Time.deltaTime;
                if (stateData.TickTime < weaponData.ShootSpeed)
                    continue;

                if (weapon.MagazineAmmo < weaponData.AmmoPerShot)
                {
                    if (weapon.TotalAmmo < weaponData.AmmoPerShot)
                        stateData.StateStatus = NodeStatus.Failure;
                    else if (!_reloadRequestPool.Value.Has(agentIndex))
                        _reloadRequestPool.Value.Add(agentIndex);

                    continue;
                }

                stateData.TickTime = 0;
                if (_shootRequestPool.Value.Has(agentIndex))
                    continue;

                weapon.MagazineAmmo -= weaponData.AmmoPerShot;
                _shootRequestPool.Value.Add(agentIndex).Setup(
                    burstTickTime: weaponData.BurstInterval,
                    shootPosition: _transformPool.Value.Get(stateData.EntityIndex).Ref.position);

                stateData.CurrentShots += 1;
                if (stateData.CurrentShots >= stateData.TargetShots && stateData.TargetShots != -1)
                    stateData.StateStatus = NodeStatus.Success;
            }
        }
    }
}
