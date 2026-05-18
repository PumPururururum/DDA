using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Enemy.Behaviours;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using Utils;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class PatrolStateSystem : IEcsRunSystem, IEcsInitSystem
    {
        private const float EPS = 0.5f;
        private const float NEAREST_RADIUS = 2f;
        private const float NEAREST_TIME_OUT = 4f;
        
        private readonly EcsFilterInject<Inc<PatrolState, AgentEntity>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<PatrolState> _statePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsPoolInject<UnityRigidbody> _rigidbodyPool;
        private readonly EcsWorldInject _world;

        private PatrolPointsProvider _patrolPointsProvider;
        
        public void Init(IEcsSystems systems)
        {
            _patrolPointsProvider = ServiceLocator.Resolve<PatrolPointsProvider>();
        } 
        
        public void Run(IEcsSystems systems)
        {
            if (_patrolPointsProvider == null || _patrolPointsProvider.PatrolPositions == null)
                return;

            foreach (var entity in _filter.Value)
            {
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;

                if (!EcsEntityUtils.IsAlive(_world.Value, agentIndex))
                    continue;

                if (!_navAgentPool.Value.Has(agentIndex) || !_rigidbodyPool.Value.Has(agentIndex))
                    continue;

                ref var patrolState = ref _statePool.Value.Get(entity);
                ref var navAgent = ref _navAgentPool.Value.Get(agentIndex).Ref;
                ref var rigidbody = ref _rigidbodyPool.Value.Get(agentIndex).Ref;

                if (navAgent == null || rigidbody == null)
                    continue;

                if (!patrolState.IsReached)
                {
                    if (navAgent.remainingDistance <= NEAREST_RADIUS)
                    {
                        patrolState.NearestTimer += Time.deltaTime;
                        if (patrolState.NearestTimer > NEAREST_TIME_OUT)
                        {
                            patrolState.NearestTimer = 0f;
                            patrolState.IsReached = true;
                        }
                    }
                    if (!navAgent.pathPending && navAgent.remainingDistance <= EPS)
                        patrolState.IsReached = true;
                    else
                        continue;
                }

                int targetPositionIndex = -1;
                var patrolPoints = _patrolPointsProvider.PatrolPositions;
                if (patrolPoints.Length == 0)
                    continue;

                if (patrolState.TargetPositionIndex == -1)
                {
                    var position = rigidbody.position;
                    var minValue = float.MaxValue;
                    for (int i = 0; i < patrolPoints.Length; i++)
                    {
                        var point = patrolPoints[i];
                        if (NavMeshUtils.TryGetNavMeshDistance(position, point.Position, out var distance)
                            && distance < minValue)
                        {
                            minValue = distance;
                            targetPositionIndex = i;
                        }
                    }
                }
                else if (patrolState.TargetPositionIndex >= 0 && patrolState.TargetPositionIndex < patrolPoints.Length)
                {
                    var neighbours = patrolPoints[patrolState.TargetPositionIndex].NeighbourPoints;
                    if (neighbours.Length > 0)
                        targetPositionIndex = neighbours[Random.Range(0, neighbours.Length)];
                }

                if (targetPositionIndex < 0 || targetPositionIndex >= patrolPoints.Length)
                    continue;

                navAgent.SetDestination(patrolPoints[targetPositionIndex].Position);
                patrolState.TargetPositionIndex = targetPositionIndex;
                patrolState.NearestTimer = 0f;
                patrolState.IsReached = false;
            }
        }
    }
}
