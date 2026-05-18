using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    public sealed class AdaptiveDifficultyService
    {
        private readonly AdaptiveDifficultySettings _settings;

        private float _difficulty;
        private float _runtimeIntensity;
        private float _audioIntensity;
        private float _lastPerformanceScore;
        private float _lastSafetyScore;

        public AdaptiveDifficultyService(AdaptiveDifficultySettings settings)
        {
            _settings = settings;
            _difficulty = settings.InitialDifficulty;
        }

        public float CurrentDifficulty => _difficulty;
        public float RuntimeIntensity => _audioIntensity;
        public float LastPerformanceScore => _lastPerformanceScore;
        public float LastSafetyScore => _lastSafetyScore;

        public void UpdateRuntime(AdaptiveStatsService stats, float dt)
        {
            float performanceScore = CalculatePerformance(stats);
            float runtimeAlpha = AlphaForTau(dt, _settings.RuntimeTau);
            float audioAlpha = AlphaForTau(dt, _settings.AudioTau);

            _runtimeIntensity = Mathf.Lerp(_runtimeIntensity, performanceScore, runtimeAlpha);
            _audioIntensity = Mathf.Lerp(_audioIntensity, _runtimeIntensity, audioAlpha);

            _lastPerformanceScore = performanceScore;
            _lastSafetyScore = CalculateSafety(stats);
        }

        public void UpdateBetweenLevels(AdaptiveStatsService stats)
        {
            float performanceScore = CalculatePerformance(stats);
            float safetyScore = CalculateSafety(stats);
            float delta = performanceScore - safetyScore;
            float beta = delta >= 0f ? _settings.BetaUp : _settings.BetaDown;

            float rawDifficulty = Mathf.Clamp01(_difficulty + beta * delta);
            _difficulty = Mathf.Clamp(
                rawDifficulty,
                _difficulty - _settings.MaxDifficultyDelta,
                _difficulty + _settings.MaxDifficultyDelta);

            _difficulty = Mathf.Clamp01(_difficulty);
            _lastPerformanceScore = performanceScore;
            _lastSafetyScore = safetyScore;
        }

        public AdaptiveDifficultySnapshot GetSnapshot()
        {
            return new AdaptiveDifficultySnapshot(
                difficulty: _difficulty,
                runtimeIntensity: _audioIntensity,
                performanceScore: _lastPerformanceScore,
                safetyScore: _lastSafetyScore);
        }

        private float CalculatePerformance(AdaptiveStatsService stats)
        {
            float kills = stats.NormalizedKills();
            float combo = stats.NormalizedCombo();
            float damage = stats.NormalizedDamageDealt();

            float score =
                _settings.KillWeight * kills +
                _settings.ComboWeight * combo +
                _settings.DamageWeight * damage;

            return Mathf.Clamp01(Mathf.Pow(Mathf.Clamp01(score), _settings.PerformanceGamma));
        }

        private float CalculateSafety(AdaptiveStatsService stats)
        {
            float damageTaken = stats.NormalizedDamageTaken();
            float accuracyPenalty = 1f - stats.NormalizedAccuracy();
            float pickups = stats.NormalizedPickups();

            float score =
                _settings.DamageTakenWeight * damageTaken +
                _settings.AccuracyPenaltyWeight * accuracyPenalty +
                _settings.PickupsWeight * pickups;

            return Mathf.Clamp01(score);
        }

        private static float AlphaForTau(float dt, float tau)
        {
            float safeDt = Mathf.Max(dt, 1e-6f);
            return safeDt / (Mathf.Max(tau, 1e-6f) + safeDt);
        }
    }
}