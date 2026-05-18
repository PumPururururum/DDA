using _Project.Code.Features.Test;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class AiBrainTickSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<AiBrain>> _filter;
        private readonly EcsPoolInject<AiBrain> _aiBrainPool;
        
        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var aiBrain = ref _aiBrainPool.Value.Get(entity);
                aiBrain.TickTime += Time.deltaTime;
                if (aiBrain.TickTime >= aiBrain.TickInterval)
                {
                    aiBrain.TickTime -= aiBrain.TickInterval;
                    aiBrain.BehaviourTreeRef.Tick(aiBrain.ContextRef, aiBrain.StateRef);
                }
            }
        }
    }
}