using System;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.States;
using _ExampleProject.Code.Features.Player.Components;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Features._Core.AiLeafs
{
    [Serializable]
    public sealed class ShootPlayerLeaf : ILeaf
    {
        [SerializeField] private int _targetShots;
        
        private EcsWorld _world;
        private EcsPool<AgentEntity> _agentPool;
        private EcsPool<ShootEntityState> _shootStatePool;
        private EcsPool<PlayerVisibility> _playerVisibilityPool;
        
        public void Construct()
        {
            _world = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);
            _agentPool = EcsWorlds.GetPool<AgentEntity>(EcsWorlds.BT_STATES);
            _shootStatePool = EcsWorlds.GetPool<ShootEntityState>(EcsWorlds.BT_STATES);
            _playerVisibilityPool = EcsWorlds.GetPool<PlayerVisibility>(EcsWorlds.DEFAULT);
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            var status = _shootStatePool.Get(state.StateIndex).StateStatus;
            return status == NodeStatus.None ? NodeStatus.Running : status;
        }

        public void OnEnter(BtContext context, ref NodeState state)
        {
            var playerIndex = _playerVisibilityPool.Get(context.AgentIndex).DetectedPlayer;
            state.StateIndex = _world.NewEntity();
            _agentPool.Add(state.StateIndex).AgentIndex = context.AgentIndex;
            _shootStatePool.Add(state.StateIndex).Setup(
                targetShots: _targetShots,
                entityIndex: playerIndex);
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