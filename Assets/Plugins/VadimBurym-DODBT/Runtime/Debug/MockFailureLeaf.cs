// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public sealed class MockFailureLeaf : ILeaf
    {
        public void Construct()
        {
        }

        public NodeStatus OnTick(BtContext context, ref NodeState state)
        {
            return NodeStatus.Failure;
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