using System;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class IsPlayerRayCastLeaf : ILeaf
    {
        [SerializeField] private bool _isNot;
        
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            if (!_isNot)
                return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerRaycast ? NodeStatus.Success : NodeStatus.Failure;
            else
                return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerRaycast ? NodeStatus.Failure : NodeStatus.Success;
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