using System;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class IsPlayerDetectedLeaf : ILeaf
    {
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            return _playerVisibilityPool.Get(context.AgentIndex).IsPlayerDetected ? NodeStatus.Success : NodeStatus.Failure;
        }

        public void OnEnter(BtContext context, ref NodeState state)
        {
        }

        public void OnExit(BtContext context, ref NodeState state, NodeStatus exitStatus)
        {
        }

        public void OnAbort(BtContext context,ref NodeState state)
        {
        }
    }
}