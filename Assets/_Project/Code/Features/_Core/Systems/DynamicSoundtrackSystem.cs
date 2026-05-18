using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Behaviours;
using _Project.Code.Core.Abstractions.Contracts;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class DynamicSoundtrackSystem : IConstruct, ITick, ICleanUp
    {
        private DynamicSoundtrackView _view;

        public void Construct()
        {
            _view = Object.FindFirstObjectByType<DynamicSoundtrackView>();
        }

        public void Tick()
        {
            if (_view == null)
                _view = Object.FindFirstObjectByType<DynamicSoundtrackView>();

            if (_view == null)
                return;

            var bootstrap = ProjectAdaptiveDifficultyBootstrap.Instance;
            float intensity = bootstrap != null ? bootstrap.CurrentIntensity : 0f;
            _view.SetIntensity(intensity);
        }

        public void CleanUp()
        {
            if (_view != null)
                _view.SetIntensity(0f);
        }
    }
}
