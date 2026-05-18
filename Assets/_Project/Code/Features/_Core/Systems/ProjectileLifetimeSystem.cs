using _ExampleProject.Code.Features._Core.Requests;
using _Project.Code.Features.Test;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class ProjectileLifetimeSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<ProjectileLifeTime>, Exc<DeathRequest>> _filter;
        private readonly EcsPoolInject<ProjectileLifeTime> _lifePool;
        private readonly EcsPoolInject<DeathRequest> _deathRequestPool;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var life = ref _lifePool.Value.Get(entity);
                life.Remaining -= Time.deltaTime;
                if (life.Remaining <= 0f)
                    _deathRequestPool.Value.Add(entity);
            }
        }
    }
}
