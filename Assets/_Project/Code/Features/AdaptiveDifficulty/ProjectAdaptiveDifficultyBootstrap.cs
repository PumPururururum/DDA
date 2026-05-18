using _Project.Code.Features.Test;
using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    [DefaultExecutionOrder(-90)]
    public sealed class ProjectAdaptiveDifficultyBootstrap : MonoBehaviour
    {
        public static ProjectAdaptiveDifficultyBootstrap Instance { get; private set; }

        [SerializeField] private AdaptiveDifficultySettings _settings;
        [SerializeField] private bool _dontDestroyOnLoad = true;

        private _Project.Code.Features.AdaptiveDifficulty.AdaptiveEcsTelemetrySource _telemetrySource;
        private AdaptiveDifficultyController _controller;

        public float CurrentDifficulty => _controller != null ? _controller.CurrentDifficulty : 0f;
        public float CurrentIntensity => _controller != null ? _controller.CurrentIntensity : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (_settings == null)
            {
                Debug.LogError("[AdaptiveDifficulty] Settings asset is not assigned.");
                enabled = false;
                return;
            }

            _telemetrySource = new _Project.Code.Features.AdaptiveDifficulty.AdaptiveEcsTelemetrySource(_settings);
            _controller = new AdaptiveDifficultyController(_settings, _telemetrySource);
        }

        private void Update()
        {
            if (_controller == null)
                return;

            _controller.Tick(Time.deltaTime);
        }

        public void CommitLevel()
        {
            _controller?.CommitLevel();
        }

        public void ReportShot(CombatTeamId shooterTeam)
        {
            _telemetrySource?.ReportShot(shooterTeam);
        }

        public void ReportDamage(CombatTeamId sourceTeam, CombatTeamId targetTeam, int damage)
        {
            _telemetrySource?.ReportDamage(sourceTeam, targetTeam, damage);
        }

        public void ReportEnemyKilled()
        {
            _telemetrySource?.ReportEnemyKilled();
        }

        public void ReportPickup(AdaptivePickupType pickupType)
        {
            _telemetrySource?.ReportPickup(pickupType);
        }

        public AdaptiveDifficultySnapshot GetDifficultySnapshot()
        {
            if (_controller == null)
                return default;

            return _controller.GetSnapshot();
        }

        public AdaptiveStatsSnapshot GetStatsSnapshot()
        {
            if (_controller == null)
                return default;

            return _controller.GetStatsSnapshot();
        }
    }
}
