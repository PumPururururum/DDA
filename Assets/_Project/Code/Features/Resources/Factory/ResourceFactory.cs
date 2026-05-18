using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Resources.Components;
using _Project.Code.Features.Resources.Facade;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Code.Infrastructure.StaticData.Resources;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;

namespace _Project.Code.Features.Resources.Factory
{
    public sealed class ResourceFactory : IResourceFactory, IConstruct
    {
        private EcsWorld _world;
        private IMemoryPoolService _memoryPoolService;
        private ResourcesStaticData _resourcesStaticData;

        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            _resourcesStaticData = ServiceLocator.Resolve<StaticDataService>().ResourcesStaticData;
        }

        public int Create(ResourceId id, Vector2 position)
        {
            var entity = _world.NewEntity();
            var config = _resourcesStaticData.Get(id);
            var facade = _memoryPoolService.SpawnGameObject<ResourceFacade>(config.PrefabId);
            facade.EcsEntity.Construct(_world, entity);
            facade.transform.position = position;

            _world.GetPool<UnityTransform>().Add(entity).Ref = facade.transform;
            _world.GetPool<ResourceTag>().Add(entity).Setup(id, config.Amount, config.PickupRadius, facade.gameObject);
            _world.GetPool<CombatTeam>().Add(entity).Value = CombatTeamId.Neutral;
            return entity;
        }
    }
}
