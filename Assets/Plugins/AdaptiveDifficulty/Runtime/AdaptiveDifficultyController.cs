using System;
using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    public sealed class AdaptiveDifficultyController
    {
        private readonly AdaptiveDifficultySettings _settings;
        private readonly IAdaptiveTelemetrySource _telemetrySource;
        private readonly AdaptiveStatsService _statsService;
        private readonly AdaptiveDifficultyService _difficultyService;

        private float _debugLogTime;

        public AdaptiveDifficultyController(
            AdaptiveDifficultySettings settings,
            IAdaptiveTelemetrySource telemetrySource)
        {
            _settings = settings;
            _telemetrySource = telemetrySource;
            _statsService = new AdaptiveStatsService(settings);
            _difficultyService = new AdaptiveDifficultyService(settings);
        }

        public event Action<AdaptiveDifficultySnapshot> SnapshotUpdated;

        public AdaptiveStatsService StatsService => _statsService;
        public float CurrentDifficulty => _difficultyService.CurrentDifficulty;
        public float CurrentIntensity => _difficultyService.RuntimeIntensity;

        public void Tick(float dt)
        {
            if (_telemetrySource == null || _telemetrySource.IsReady == false)
                return;

            _telemetrySource.Tick(dt);

            AdaptiveFrameTelemetry frame = _telemetrySource.ConsumeFrameTelemetry();
            _statsService.OnFrameSamples(frame, dt);
            _difficultyService.UpdateRuntime(_statsService, dt);

            AdaptiveDifficultySnapshot snapshot = _difficultyService.GetSnapshot();
            SnapshotUpdated?.Invoke(snapshot);

            if (_settings.EnableDebugLogs == false)
                return;

            _debugLogTime += dt;
            if (_debugLogTime < _settings.DebugLogInterval)
                return;

            _debugLogTime = 0f;
            Debug.Log(
                $"[AdaptiveDifficulty] D={snapshot.Difficulty:F3}, I={snapshot.RuntimeIntensity:F3}, " +
                $"Perf={snapshot.PerformanceScore:F3}, Safety={snapshot.SafetyScore:F3}");
        }

        public void CommitLevel()
        {
            _difficultyService.UpdateBetweenLevels(_statsService);

            if (_settings.EnableDebugLogs)
            {
                AdaptiveDifficultySnapshot snapshot = _difficultyService.GetSnapshot();
                Debug.Log(
                    $"[AdaptiveDifficulty] Level committed. D={snapshot.Difficulty:F3}, " +
                    $"Perf={snapshot.PerformanceScore:F3}, Safety={snapshot.SafetyScore:F3}");
            }
        }

        public AdaptiveDifficultySnapshot GetSnapshot() =>
            _difficultyService.GetSnapshot();

        public AdaptiveStatsSnapshot GetStatsSnapshot() =>
            _statsService.GetSnapshot();
    }
}