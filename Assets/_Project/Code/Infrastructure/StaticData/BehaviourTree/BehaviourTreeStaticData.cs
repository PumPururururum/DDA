using UnityEngine;
using VadimBurym.DodBehaviourTree;

namespace _ExampleProject.Code.Infrastructure.StaticData.BehaviourTree
{
    [CreateAssetMenu(fileName = nameof(BehaviourTreeStaticData), menuName ="_Project/StaticData/New BehaviourTreeStaticData")]
    public sealed class BehaviourTreeStaticData : ScriptableObject
    {
        public BehaviourTreeAsset[] Assets;

        public void Construct()
        {
            for (int i = 0; i < Assets.Length; i++)
                Assets[i].BehaviourTree.Construct();
        }
    }
}