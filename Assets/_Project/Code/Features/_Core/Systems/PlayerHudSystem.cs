using _ExampleProject.Code.Features._Core.Behaviours;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class PlayerHudSystem : IConstruct, ITick, ICleanUp
    {
        private EcsFilter _playerFilter;
        private EcsPool<Health> _healthPool;
        private EcsPool<Weapon> _weaponPool;
        private BasicGameplayHudView _view;

        public void Construct()
        {
            var world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _playerFilter = world.Filter<PlayerTag>().End();
            _healthPool = world.GetPool<Health>();
            _weaponPool = world.GetPool<Weapon>();
            CreateView();
        }

        public void Tick()
        {
            if (_view == null)
                CreateView();

            foreach (var entity in _playerFilter)
            {
                if (!_healthPool.Has(entity) || !_weaponPool.Has(entity))
                    continue;

                ref var health = ref _healthPool.Get(entity);
                ref var weapon = ref _weaponPool.Get(entity);
                _view.SetPlayerStats(health.CurrentValue, health.MaxValue, weapon.MagazineAmmo, weapon.TotalAmmo);
                return;
            }

            _view.SetNoPlayer();
        }

        public void CleanUp()
        {
            if (_view == null)
                return;

            Object.Destroy(_view.gameObject);
            _view = null;
        }

        private void CreateView()
        {
            var hudObject = new GameObject("Basic Gameplay HUD");
            _view = hudObject.AddComponent<BasicGameplayHudView>();
        }
    }
}
