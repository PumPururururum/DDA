using System;
using VadimBurym.DodBehaviourTree;

namespace _Project.Code.Features.Test
{
    [Serializable]
    public struct AiBrain
    {
        public BehaviourTree BehaviourTreeRef;
        public BtState StateRef;
        public BtContext ContextRef;
        public float TickInterval;
        public float TickTime;

        public void Setup(BehaviourTree behaviourTreeRef, BtState btState, BtContext btContext, float tickInterval, float tickTime)
        {
            BehaviourTreeRef = behaviourTreeRef;
            StateRef = btState;
            ContextRef = btContext;
            TickInterval = tickInterval;
            TickTime = tickTime;
        }
    }
}