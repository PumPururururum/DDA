using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.States;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class RetreatEntityStateSystem : IEcsRunSystem
    {
        private const float EPS = 0.2f;
        private const string WALLS_MASK = "Walls";
        
        private readonly EcsFilterInject<Inc<RetreatEntityState, AgentEntity>> _filter = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<RetreatEntityState> _statePool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<AgentEntity> _agentPool = EcsWorlds.BT_STATES;
        private readonly EcsPoolInject<UnityTransform> _transformPool;
        private readonly EcsPoolInject<UnityNavMeshAgent> _navAgentPool;
        private readonly EcsWorldInject _world;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var stateData = ref _statePool.Value.Get(entity);
                var agentIndex = _agentPool.Value.Get(entity).AgentIndex;

                if (!EcsEntityUtils.IsAlive(_world.Value, agentIndex))
                    continue;

                if (!_navAgentPool.Value.Has(agentIndex) || !_transformPool.Value.Has(agentIndex))
                    continue;

                var navAgent = _navAgentPool.Value.Get(agentIndex).Ref;
                var selfTransform = _transformPool.Value.Get(agentIndex).Ref;

                if (navAgent == null || selfTransform == null)
                    continue;

                if (stateData.IsReached)
                {
                    if (!EcsEntityUtils.IsAlive(_world.Value, stateData.EntityIndex)
                        || !_transformPool.Value.Has(stateData.EntityIndex)
                        || _transformPool.Value.Get(stateData.EntityIndex).Ref == null)
                    {
                        stateData.StateStatus = NodeStatus.Failure;
                        continue;
                    }

                    var selfPosition = selfTransform.position;
                    var entityPosition = _transformPool.Value.Get(stateData.EntityIndex).Ref.position;
                    if (TryGetRetreatPosition(selfPosition, entityPosition, stateData.RetreatDistance, out var retreatPosition))
                        navAgent.SetDestination(retreatPosition);
                    else
                        stateData.StateStatus = NodeStatus.Failure;
                    stateData.IsReached = false;
                }
                else if (!navAgent.pathPending && navAgent.remainingDistance <= EPS)
                {
                    stateData.IsReached = true;
                    stateData.StateStatus = NodeStatus.Success;
                }
            }
        }

        private bool TryGetRetreatPosition(Vector2 selfPosition, Vector2 entityPosition, float retreatDistance, out Vector2 result)
        {
            result = default;
            var direction = selfPosition - entityPosition;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return false;

            direction.Normalize();
            
            for (int i = 0; i < Rotations.Length; i++)
            {
                Vector2 dir = Rotate(direction, Rotations[i]);
                Vector2 candidate = selfPosition + dir * retreatDistance;
                Vector2 delta = candidate - selfPosition;
                float dist = delta.magnitude;
                if (dist <= Mathf.Epsilon)
                    continue;

                var wallHit = Physics2D.Raycast(selfPosition, delta / dist, dist, LayerMask.GetMask(WALLS_MASK));
                if (wallHit.collider != null)
                    continue;
                result = candidate;
                return true;
            }
            
            return false;
        }
        
        private static Vector2 Rotate(Vector2 v, Rot r)
        {
            return new Vector2(
                v.x * r.Cos - v.y * r.Sin,
                v.x * r.Sin + v.y * r.Cos
            );
        }
        
        // Порядок попыток: 0, +20, -20, +40, -40, +60, -60, +90, -90
        private static readonly Rot[] Rotations =
        {
            new Rot( 1f,  0f),                         // 0
            new Rot( 0.9396926208f,  0.3420201433f),   // +20
            new Rot( 0.9396926208f, -0.3420201433f),   // -20
            new Rot( 0.7660444431f,  0.6427876097f),   // +40
            new Rot( 0.7660444431f, -0.6427876097f),   // -40
            new Rot( 0.5f,           0.8660254038f),   // +60
            new Rot( 0.5f,          -0.8660254038f),   // -60
            new Rot( 0f,            1f),               // +90
            new Rot( 0f,           -1f),               // -90
        };
        
        private readonly struct Rot
        {
            public readonly float Cos;
            public readonly float Sin;
            public Rot(float cos, float sin) { Cos = cos; Sin = sin; }
        }
    }
}
