// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: Unity-DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
#if ODIN_INSPECTOR
    [HideMonoScript]
#endif
    [Icon("Assets/Plugins/VadimBurym-DODBT/Editor/Icons/dodbt-icon.png")]
    public sealed class BehaviourTreeAsset : ScriptableObject
    {
        public BehaviourTree BehaviourTree => _compiledTree;
        [SerializeField, HideInInspector] private BehaviourTree _compiledTree;

#if UNITY_EDITOR
        internal void SetupCompiledTree(BehaviourTree compiledTree)
        {
            _compiledTree = compiledTree;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        } 
#endif
    }
}