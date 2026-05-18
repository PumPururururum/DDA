using System;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features.Player.AiLeafs
{
    [Serializable]
    public sealed class ChasePlayerLeaf : ILeaf
    {
        private EcsWorld _world;
        private EcsPool<AgentEntity> _agentPool;
        private EcsPool<ChaseEntityState> _chaseStatePool;
        private EcsPool<NavigationState> _navStatePool;
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);
            _agentPool = EcsWorlds.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
            _chaseStatePool = EcsWorlds.GetPool<ChaseEntityState>(EcsWorlds.BT_STATES);
            _navStatePool = EcsWorlds.GetPool<NavigationState>(EcsWorlds.BT_STATES);
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            return NodeStatus.Running;
        }

        public void OnEnter(BtContext context, ref NodeState state)
        {
            var playerIndex = _playerVisibilityPool.Get(context.AgentIndex).DetectedPlayer;
            state.StateIndex = _world.NewEntity();
            _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
            _navStatePool.Add(state.StateIndex);
            _chaseStatePool.Add(state.StateIndex).EntityIndex = playerIndex;
        }

        public void OnExit(BtContext context, ref NodeState state, NodeStatus exitStatus)
        {
            _world.DelEntity(state.StateIndex);
        }

        public void OnAbort(BtContext context, ref NodeState state)
        {
            _world.DelEntity(state.StateIndex);
        }
    }
}