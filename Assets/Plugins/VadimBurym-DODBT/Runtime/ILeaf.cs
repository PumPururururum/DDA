// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

namespace VadimBurym.DodBehaviourTree
{
    public interface ILeaf
    {
        void Construct();
        NodeStatus OnTick(BtContext context, ref NodeState state);
        void OnEnter(BtContext context, ref NodeState state);
        void OnExit(BtContext context, ref NodeState state, NodeStatus exitStatus);
        void OnAbort(BtContext context, ref NodeState state);
    }
}