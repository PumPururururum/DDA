// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
    [Serializable]
    public struct NodeState
    {
        [SerializeField] internal bool IsEntered;
        [SerializeField] internal int Cursor;
        [SerializeField] internal sbyte CachedStatus;

        public int StateIndex;

        internal void Reset()
        {
            IsEntered = false;
            Cursor = -1;
            CachedStatus = 0;
            StateIndex = -1;
        }
    }
}