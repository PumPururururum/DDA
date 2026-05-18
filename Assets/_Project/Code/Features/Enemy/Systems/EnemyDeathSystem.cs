using System.Collections.Generic;
using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features.Enemy.Systems
{
    public sealed class EnemyDeathSystem : IEcsRunSystem, IEcsInitSystem
    {
        private readonly EcsFilterInject<Inc<DeathRequest, EnemyTag>> _filter;
        private readonly EcsPoolInject<EnemyTag> _enemyPool;
        private readonly EcsWorldInject _world;

        private readonly EcsWorldInject _btStateWorld = EcsWorlds.BT_STATES;
        private readonly EcsFilterInject<Inc<AgentEntity>> _btStateFilter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _btAgentPool = EcsWorlds.BT_STATES;
        private readonly List<int> _btStatesToDelete = new();

        private IMemoryPoolService _memoryPoolService;

        public void Init(IEcsSystems systems)
        {
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();
        }

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ProjectAdaptiveDifficultyBootstrap.Instance?.ReportEnemyKilled();
                RemoveBehaviourTreeStatesForAgent(entity);

                ref var enemy = ref _enemyPool.Value.Get(entity);

                if (_memoryPoolService != null && enemy.GameObjectRef != null)
                    _memoryPoolService.UnspawnGameObject(MemoryPoolId.Enemy, enemy.GameObjectRef);

                _world.Value.DelEntity(entity);
            }
        }

        private void RemoveBehaviourTreeStatesForAgent(int agentEntity)
        {
            _btStatesToDelete.Clear();

            foreach (var stateEntity in _btStateFilter.Value)
            {
                if (_btAgentPool.Value.Get(stateEntity).AgentIndex == agentEntity)
                    _btStatesToDelete.Add(stateEntity);
            }

            for (int i = 0; i < _btStatesToDelete.Count; i++)
            {
                var stateEntity = _btStatesToDelete[i];
                if (EcsEntityUtils.IsAlive(_btStateWorld.Value, stateEntity))
                    _btStateWorld.Value.DelEntity(stateEntity);
            }

            _btStatesToDelete.Clear();
        }
    }
}
