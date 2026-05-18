using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Infrastructure;
using _Project.Infrastructure;

namespace _ExampleProject.Code.Infrastructure.StaticData.BehaviourTree
{
    public sealed class BehaviourTreeConstructSystem : IConstruct, IInit
    {
        private BehaviourTreeStaticData _behaviourTreeStaticData;
        
        public void Construct()
        {
            _behaviourTreeStaticData = ServiceLocator.Resolve<StaticDataService>().BehaviourTreeStaticData;
        }

        public void Init()
        {
            _behaviourTreeStaticData.Construct();
        }
    }
}