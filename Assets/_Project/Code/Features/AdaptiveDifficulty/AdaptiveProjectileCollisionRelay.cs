using UnityEngine;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    public sealed class AdaptiveProjectileCollisionRelay : MonoBehaviour
    {
        private AdaptiveEcsTelemetrySource _telemetrySource;
        private int _projectileEntity;
        private bool _isConstructed;

        public void Construct(AdaptiveEcsTelemetrySource telemetrySource, int projectileEntity)
        {
            _telemetrySource = telemetrySource;
            _projectileEntity = projectileEntity;
            _isConstructed = true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_isConstructed == false || _telemetrySource == null)
                return;

            _telemetrySource.RegisterProjectileCollision(_projectileEntity, collision.collider.gameObject);
        }
    }
}