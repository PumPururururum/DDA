using System;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using Utils;
using VadimBurym.DodBehaviourTree;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class DistanceToPlayerThresholdLeaf : ILeaf
    {
        [SerializeField] private float _threshold;
#if ODIN_INSPECTOR
[HideLabel]
#endif
        [SerializeReference] private IComparison _comparison;
        
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            return _comparison.CompareFloat(
                _playerVisibilityPool.Get(context.AgentIndex).SqrDistanceToPlayer,
                _threshold * _threshold) ? NodeStatus.Success : NodeStatus.Failure;
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