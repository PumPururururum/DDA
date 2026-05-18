// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public sealed class BtState
    {
        public NodeState[] NodeStates = Array.Empty<NodeState>();
#if UNITY_EDITOR
        internal NodeStatus[] DebugStatus = Array.Empty<NodeStatus>();
        internal List<string> DebugRunningLeafs = new();
#endif
    }
}