using _ExampleProject.Code.Features._Core.Components;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class NavigationStateSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<NavigationState, AgentEntity>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsPoolInject<UnityRigidbody> _rigidbodyPool;
        private readonly EcsWorldInject _world;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;

                if (!EcsEntityUtils.IsAlive(_world.Value, agentIndex))
                    continue;

                if (!_navAgentPool.Value.Has(agentIndex) || !_rigidbodyPool.Value.Has(agentIndex))
                    continue;

                ref var navAgent = ref _navAgentPool.Value.Get(agentIndex).Ref;
                ref var rigidbody = ref _rigidbodyPool.Value.Get(agentIndex).Ref;

                if (navAgent == null || rigidbody == null)
                    continue;

                rigidbody.linearVelocity = navAgent.desiredVelocity;
                navAgent.nextPosition = rigidbody.position;
            }
        }
    }
}
