using _ExampleProject.Code.Features.Projectile.Components;
using _ExampleProject.Code.Features.Projectile.Facade;
using _ExampleProject.Code.Infrastructure.StaticData.Projectile;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Factory
{
    public sealed class ProjectileFactory : IProjectileFactory, IConstruct
    {
        private ProjectilesStaticData _projectileStaticData;
        private IMemoryPoolService _memoryPoolService;
        private EcsWorld _world;

        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _projectileStaticData = ServiceLocator.Resolve<StaticDataService>().ProjectilesStaticData;
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }

        public int Create(ProjectileId id, Vector2 position, Vector2 direction)
        {
            return Create(id, position, direction, CombatTeamId.Neutral);
        }

        public int Create(ProjectileId id, Vector2 position, Vector2 direction, CombatTeamId team)
        {
            var proj = _world.NewEntity();
            var projData = _projectileStaticData.GetProjectileData(id);
            var projFacade = _memoryPoolService.SpawnGameObject<ProjectileFacade>(projData.PrefabId);

            projFacade.EcsEntity.Construct(_world, proj);
            projFacade.EcsCollider.Construct(EcsWorlds.GetWorld(EcsWorlds.EVENTS));
            projFacade.transform.position = position;
            projFacade.Rigidbody.linearVelocity = Vector2.zero;

            _world.GetPool<UnityRigidbody>().Add(proj).Ref = projFacade.Rigidbody;
            _world.GetPool<Movement>().Add(proj).Setup(direction, projData.MoveSpeed);
            _world.GetPool<ProjectileTag>().Add(proj).Setup(id, projFacade.gameObject);
            _world.GetPool<CombatTeam>().Add(proj).Value = team;
            _world.GetPool<ProjectileDamage>().Add(proj).Value = projData.Damage;
            _world.GetPool<ProjectileLifeTime>().Add(proj).Remaining = projData.LifeTime;
            return proj;
        }
    }
}
