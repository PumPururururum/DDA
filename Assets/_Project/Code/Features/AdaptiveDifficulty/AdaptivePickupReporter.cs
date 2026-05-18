using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features.Player.Facade;
using _Project.Code.Features.Resources.Facade;
using UnityEngine;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    public sealed class AdaptivePickupReporter : MonoBehaviour
    {
        [SerializeField] private AdaptivePickupType _pickupType = AdaptivePickupType.None;
        [SerializeField] private bool _destroyAfterReport = false;

        private bool _isReported;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isReported || _pickupType == AdaptivePickupType.None || other == null)
                return;

            if (other.GetComponentInParent<PlayerFacade>() == null)
                return;

            // ECS resources report pickups from ResourcePickupSystem so they are counted
            // exactly once and only after the gameplay pickup really succeeds.
            if (GetComponentInParent<ResourceFacade>() != null)
                return;

            ProjectAdaptiveDifficultyBootstrap bootstrap = ProjectAdaptiveDifficultyBootstrap.Instance;
            if (bootstrap == null)
                return;

            _isReported = true;
            bootstrap.ReportPickup(_pickupType);

            if (_destroyAfterReport)
                Destroy(gameObject);
        }
    }
}
