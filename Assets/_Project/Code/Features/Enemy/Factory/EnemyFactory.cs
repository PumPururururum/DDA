using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Enemy.Facade;
using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Infrastructure.StaticData.Enemy;
using _ExampleProject.Code.Infrastructure.StaticData.Weapons;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Enemy.Factory
{
    public sealed class EnemyFactory : IEnemyFactory, IConstruct
    {
        private IMemoryPoolService _memoryPoolService;
        private EnemyStaticData _enemyStaticData;
        private WeaponsStaticData _weaponsStaticData;
        private EcsWorld _world;

        public void Construct()
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
            var staticData = ServiceLocator.Resolve<StaticDataService>();
            _enemyStaticData = staticData.EnemyStaticData;
            _weaponsStaticData = staticData.WeaponsStaticData;
            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
        }

        public int Create(EnemyId enemyId, Vector2 position)
        {
            var entity = _world.NewEntity();
            var enemyData = _enemyStaticData.GetEnemyConfig(enemyId);
            var enemyFacade = _memoryPoolService.SpawnGameObject<EnemyFacade>(MemoryPoolId.Enemy);

            var btState = _memoryPoolService.DequeueObject<BtState>();
            enemyData.Brain.BehaviourTree.FillInitialState(btState);
            var btContext = _memoryPoolService.DequeueObject<BtContext>();
            btContext.AgentIndex = entity;

            enemyFacade.EcsEntity.Construct(_world, entity);
            enemyFacade.Rigidbody.position = position;
            enemyFacade.BtMonoDebug.Construct(enemyData.Brain, btState);
            enemyFacade.NavMeshAgent.speed = enemyData.MoveSpeed;
            enemyFacade.NavMeshAgent.avoidancePriority = Random.Range(10, 90);

            _world.GetPool<EnemyTag>().Add(entity).Setup(enemyData.Id, enemyFacade.gameObject);
            _world.GetPool<UnityRigidbody>().Add(entity).Ref = enemyFacade.Rigidbody;
            _world.GetPool<UnityNavMeshAgent>().Add(entity).Ref = enemyFacade.NavMeshAgent;
            _world.GetPool<UnityTransform>().Add(entity).Ref = enemyFacade.transform;
            _world.GetPool<DifficultyWeight>().Add(entity).Value = enemyData.DifficultyWeight;
            _world.GetPool<Movement>().Add(entity).MoveSpeed = enemyData.MoveSpeed;
            _world.GetPool<AiBrain>().Add(entity).Setup(
                enemyData.Brain.BehaviourTree,
                btState,
                btContext,
                enemyData.BrainTickInterval,
                Random.Range(0f, enemyData.BrainTickInterval));
            _world.GetPool<PlayerVisibility>().Add(entity).Setup(
                enemyData.PlayerVisibilitySensorTickInterval,
                Random.Range(0f, enemyData.PlayerVisibilitySensorTickInterval),
                enemyData.DetectDistance * enemyData.DetectDistance,
                enemyData.HuntingDistance * enemyData.HuntingDistance);

            var weaponMagazineSize = _weaponsStaticData.GetWeaponData(enemyData.Weapon).MaxMagazineSize;
            var startMagazine = Mathf.Min(weaponMagazineSize, enemyData.InitAmmo);
            var totalAmmo = Mathf.Max(0, enemyData.InitAmmo - startMagazine);
            _world.GetPool<Weapon>().Add(entity).Setup(
                enemyFacade.FirePoint != null ? enemyFacade.FirePoint : enemyFacade.transform,
                enemyData.Weapon,
                totalAmmo,
                startMagazine);
            _world.GetPool<Health>().Add(entity) = new Health { MaxValue = enemyData.MaxHealth, CurrentValue = enemyData.MaxHealth };
            _world.GetPool<Armor>().Add(entity).Value = enemyData.Armour;
            _world.GetPool<CombatTeam>().Add(entity).Value = CombatTeamId.Enemy;

            return entity;
        }
    }
}
