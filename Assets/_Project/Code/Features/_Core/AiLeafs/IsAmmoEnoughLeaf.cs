using System;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.AiLeafs
{
    [Serializable]
    public sealed class IsAmmoEnoughLeaf : ILeaf
    {
        [SerializeField] private int _shotCount;

        private WeaponsStaticData _weaponsStaticData;
        private EcsPool<Weapon> _weaponPool;
        
        public void Construct()
        {
            _weaponsStaticData = ServiceLocator.Resolve<StaticDataService>().WeaponsStaticData;
            _weaponPool = EcsWorlds.GetPool<Weapon>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            ref var weapon = ref _weaponPool.Get(context.AgentIndex);
            var weaponData = _weaponsStaticData.GetWeaponData(weapon.WeaponId);
            var targetAmmo = _shotCount * weaponData.AmmoPerShot;
            return weapon.TotalAmmo + weapon.MagazineAmmo >= targetAmmo ? NodeStatus.Success : NodeStatus.Failure;
        }

        public void OnEnter(BtContext context, ref NodeState state)
        {
        }

        public void OnExit(BtContext context, ref NodeState state, NodeStatus exitStatus)
        {
        }

        public void OnAbort(BtContext context, ref NodeState state)
        {
        }
    }
}