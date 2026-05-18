// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public sealed class BehaviourTree
    {
        private const sbyte Unknown = 0;
        private const sbyte Failure = -1;
        private const sbyte Success = 1;
        private const sbyte Running = 2;
        
        [SerializeField] private int _rootIndex;
        [SerializeField] private Node[] _nodes;
        [SerializeField] private SelectorNode[] _selectorNodes;
        [SerializeField] private SequenceNode[] _sequenceNodes;
        [SerializeField] private MemorySelectorNode[] _memorySelectorNodes;
        [SerializeField] private MemorySequenceNode[] _memorySequenceNodes;
        [SerializeField] private ParallelNode[] _parallelNodes;
        [SerializeReference] private ILeaf[]  _leafs;
        [SerializeField] private int[] _buffer;
        
        internal BehaviourTree(
            Node[] nodes,
            int rootIndex,
            SelectorNode[] selectorNodes,
            SequenceNode[] sequenceNodes,
            MemorySelectorNode[] memorySelectorNodes,
            MemorySequenceNode[] memorySequenceNodes,
            ParallelNode[] parallelNodes,
            ILeaf[] leafs,
            int childBufferSize)
        {
            _nodes = nodes;
            _rootIndex = rootIndex;
            _selectorNodes = selectorNodes;
            _sequenceNodes = sequenceNodes;
            _memorySelectorNodes = memorySelectorNodes;
            _memorySequenceNodes = memorySequenceNodes;
            _parallelNodes = parallelNodes;
            _leafs = leafs;
            _buffer = new int[childBufferSize];
        }
        
        public void Construct()
        {
            for (int i = 0; i < _leafs.Length; i++)
                _leafs[i].Construct();
        }

        public void FillInitialState(BtState btState)
        {
            if (btState.NodeStates.Length < _nodes.Length)
                btState.NodeStates = new NodeState[Mathf.NextPowerOfTwo(_nodes.Length)];
            var nodeStates = btState.NodeStates;
            for (int i = 0; i < _nodes.Length; i++)
                nodeStates[i].Reset();
#if UNITY_EDITOR
            if (btState.DebugStatus.Length < _nodes.Length)
                btState.DebugStatus = new NodeStatus[Mathf.NextPowerOfTwo(_nodes.Length)];
            var debugStatus = btState.DebugStatus;
            for (int i = 0; i < _nodes.Length; i++)
                debugStatus[i] = NodeStatus.None;
#endif
        }
        
        public void Tick(BtContext context, BtState state)
        {
#if UNITY_EDITOR
            state.DebugRunningLeafs.Clear();
            for (int i = 0; i < state.DebugStatus.Length; i++)
                state.DebugStatus[i] = NodeStatus.None;
            var status = TickNode(context, state, _rootIndex);
            state.DebugStatus[_rootIndex] = status;
#else
            TickNode(context, state, _rootIndex);
#endif
        }

        public void Abort(BtContext context, BtState state)
        {
            AbortNode(context, state, _rootIndex);
        }
        
        private NodeStatus TickNode(BtContext context, BtState state, int index)
        {
            ref var node = ref _nodes[index];
            ref var nodeState = ref state.NodeStates[index];
            switch (node.Id)
            {
                case NodeId.Selector:
                    ref var selectorNode = ref _selectorNodes[node.DataIndex];
                    for (int i = 0; i < selectorNode.ChildCount; i++)
                    {
                        var status = TickNode(context, state, selectorNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[selectorNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Failure) continue;
                        if (nodeState.Cursor != i && nodeState.Cursor != -1)
                            AbortNode(context, state, selectorNode.FirstChild + nodeState.Cursor);
                        nodeState.Cursor = status == NodeStatus.Running ? i : -1;
                        return status;
                    }
                    nodeState.Cursor = -1;
                    return NodeStatus.Failure;
                
                case NodeId.Sequence:
                    ref var sequenceNode = ref _sequenceNodes[node.DataIndex];
                    for (int i = 0; i < sequenceNode.ChildCount; i++)
                    {
                        var status = TickNode(context, state, sequenceNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[sequenceNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Success) continue;
                        if (nodeState.Cursor != i && nodeState.Cursor != -1)
                            AbortNode(context, state, sequenceNode.FirstChild + nodeState.Cursor);
                        nodeState.Cursor = status == NodeStatus.Running ? i : -1;
                        return status;
                    }
                    nodeState.Cursor = -1;
                    return NodeStatus.Success;
                
                case NodeId.MemorySequence:
                    ref var memorySequenceNode = ref _memorySequenceNodes[node.DataIndex];
                    var cursor = nodeState.Cursor;
                    if (cursor != -1) cursor--;
                    for (; ++cursor < memorySequenceNode.ChildCount;)
                    {
                        var status = TickNode(context, state, memorySequenceNode.FirstChild + cursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySequenceNode.FirstChild + cursor] = status;
#endif
                        if (status == NodeStatus.Success) continue;
                        nodeState.Cursor = status == NodeStatus.Failure && memorySequenceNode.ResetOnFailure ? -1 : cursor;
                        return status;
                    }
                    nodeState.Cursor = -1;
                    return NodeStatus.Success;
                
                case NodeId.MemorySelector:
                    ref var memorySelectorNode = ref _memorySelectorNodes[node.DataIndex];
                    if (nodeState.Cursor != -1)
                    {
                        var status = TickNode(context, state, memorySelectorNode.FirstChild + nodeState.Cursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySelectorNode.FirstChild + nodeState.Cursor] = status;
#endif
                        if (status != NodeStatus.Failure)
                        {
                            if (status == NodeStatus.Success) nodeState.Cursor = -1;
                            return status;
                        }
                    }
                    WarmUpBuffer(memorySelectorNode.PickRandom, memorySelectorNode.ChildCount);
                    for (int i = 0; i < memorySelectorNode.ChildCount; i++)
                    {
                        var bufferCursor = _buffer[i];
                        var status = TickNode(context, state, memorySelectorNode.FirstChild + bufferCursor);
#if UNITY_EDITOR
                        state.DebugStatus[memorySelectorNode.FirstChild + bufferCursor] = status;
#endif
                        if (status == NodeStatus.Failure) continue;
                        nodeState.Cursor = status == NodeStatus.Running ? bufferCursor : -1;
                        return status;
                    }
                    nodeState.Cursor = -1;
                    return NodeStatus.Failure;
                
                case NodeId.Leaf:
                    var leaf = _leafs[node.DataIndex];
                    if (!nodeState.IsEntered)
                    {
                        leaf.OnEnter(context, ref nodeState);
                        nodeState.IsEntered = true;
                    }
                    var leafStatus = leaf.OnTick(context, ref nodeState);
#if UNITY_EDITOR
                    state.DebugStatus[index] = leafStatus;
                    if (leafStatus == NodeStatus.Running)
                        state.DebugRunningLeafs.Add(DebugUtils.GetLeafName(leaf));
#endif
                    if (leafStatus != NodeStatus.Running)
                    {
                        leaf.OnExit(context, ref nodeState, leafStatus);
                        nodeState.IsEntered = false;
                    }
                    return leafStatus;
                
                case NodeId.Parallel:
                    ref var parallelNode = ref _parallelNodes[node.DataIndex];
                    int success = 0; int fails = 0;
                    for (int i = 0; i < parallelNode.ChildCount; i++)
                    {
                        ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                        if (childState.CachedStatus == Failure) {fails++; continue;}
                        if (childState.CachedStatus == Success) {success++; continue;}
                        var status = TickNode(context, state, parallelNode.FirstChild + i);
#if UNITY_EDITOR
                        state.DebugStatus[parallelNode.FirstChild + i] = status;
#endif
                        if (status == NodeStatus.Failure)
                        {
                            fails++;
                            if (parallelNode.CacheChildStatus) childState.CachedStatus = Failure;
                        }
                        else if (status == NodeStatus.Success)
                        {
                            success++;
                            if (parallelNode.CacheChildStatus) childState.CachedStatus = Success;
                        }
                        else
                        {
                            childState.CachedStatus = Running;
                        }
                    }
                    if (success >= parallelNode.SuccessThreshold || fails >= parallelNode.FailsThreshold)
                    {
                        for (int i = 0; i < parallelNode.ChildCount; i++)
                        {
                            ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                            if (childState.CachedStatus == Running) AbortNode(context, state, parallelNode.FirstChild + i);
                            childState.CachedStatus = Unknown;
                        }
                        return fails >= parallelNode.FailsThreshold ? NodeStatus.Failure : NodeStatus.Success;
                    }
                    return NodeStatus.Running;

                default:
                    return NodeStatus.Failure;
            }
        }

        private void AbortNode(BtContext context, BtState state, int index)
        {
            ref var node = ref _nodes[index];
            ref var nodeState = ref state.NodeStates[index];
            switch (node.Id)
            {
                case NodeId.Selector:
                    ref var selectorNode = ref _selectorNodes[node.DataIndex];
                    if (nodeState.Cursor == -1) return;
                    AbortNode(context, state, selectorNode.FirstChild + nodeState.Cursor);
                    nodeState.Cursor = -1;
                    return;
                
                case NodeId.Sequence:
                    ref var sequenceNode = ref _sequenceNodes[node.DataIndex];
                    if (nodeState.Cursor == -1) return;
                    AbortNode(context, state, sequenceNode.FirstChild + nodeState.Cursor);
                    nodeState.Cursor = -1;
                    return;
                
                case NodeId.MemorySequence:
                    ref var memorySequenceNode = ref _memorySequenceNodes[node.DataIndex];
                    if (nodeState.Cursor == -1) return;
                    AbortNode(context, state, memorySequenceNode.FirstChild + nodeState.Cursor);
                    if (memorySequenceNode.ResetOnAbort) nodeState.Cursor = -1;
                    return;
                
                case NodeId.MemorySelector:
                    ref var memorySelectorNode = ref _memorySelectorNodes[node.DataIndex];
                    if (nodeState.Cursor == -1) return;
                    AbortNode(context, state, memorySelectorNode.FirstChild + nodeState.Cursor);
                    if (memorySelectorNode.ResetOnAbort) nodeState.Cursor = -1;
                    return;
                
                case NodeId.Leaf:
                    if (!nodeState.IsEntered) return;
                    var leaf = _leafs[node.DataIndex];
                    nodeState.IsEntered = false;
                    leaf.OnAbort(context, ref nodeState);
                    return;
                
                case NodeId.Parallel:
                    ref var parallelNode = ref _parallelNodes[node.DataIndex];
                    for (int i = 0; i < parallelNode.ChildCount; i++)
                    {
                        ref var childState = ref state.NodeStates[parallelNode.FirstChild + i];
                        if (childState.CachedStatus == Running) AbortNode(context, state, parallelNode.FirstChild + i);
                        childState.CachedStatus = Unknown;
                    }
                    return;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void WarmUpBuffer(bool isRandom, int count)
        {
            for (int i = 0; i < count; i++) 
                _buffer[i] = i;
            if (!isRandom) return;
            for (int i = count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_buffer[i], _buffer[j]) = (_buffer[j], _buffer[i]);
            }
        }
    }
}