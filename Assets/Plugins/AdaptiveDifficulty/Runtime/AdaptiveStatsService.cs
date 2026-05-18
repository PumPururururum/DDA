using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    public sealed class AdaptiveStatsService
    {
        private readonly AdaptiveDifficultySettings _settings;

        private float _killsEma;
        private float _damageDealtEma;
        private float _damageTakenEma;
        private float _accuracyEma;
        private float _comboEma;
        private float _ammoPickupRateEma;
        private float _healthPickupRateEma;

        private int _totalShots;
        private int _totalHits;

        public AdaptiveStatsService(AdaptiveDifficultySettings settings)
        {
            _settings = settings;
        }

        public void OnFrameSamples(AdaptiveFrameTelemetry frame, float dt)
        {
            float safeDt = Mathf.Max(dt, 1e-6f);
            float killsRate = frame.Kills / safeDt;
            float damageDealtRate = frame.DamageDealt / safeDt;
            float damageTakenRate = frame.DamageTaken / safeDt;
            float ammoPickupRate = frame.AmmoPicked / safeDt;
            float healthPickupRate = frame.HealthPicked / safeDt;

            float killsAlpha = AlphaForTau(safeDt, _settings.TauKills);
            float damageDealtAlpha = AlphaForTau(safeDt, _settings.TauDamageDealt);
            float damageTakenAlpha = AlphaForTau(safeDt, _settings.TauDamageTaken);
            float accuracyAlpha = AlphaForTau(safeDt, _settings.TauAccuracy);
            float comboAlpha = AlphaForTau(safeDt, _settings.TauCombo);
            float pickupAlpha = AlphaForTau(safeDt, _settings.TauPickups);

            _killsEma = Mathf.Lerp(_killsEma, killsRate, killsAlpha);
            _damageDealtEma = Mathf.Lerp(_damageDealtEma, damageDealtRate, damageDealtAlpha);
            _damageTakenEma = Mathf.Lerp(_damageTakenEma, damageTakenRate, damageTakenAlpha);
            _comboEma = Mathf.Lerp(_comboEma, frame.Combo, comboAlpha);
            _ammoPickupRateEma = Mathf.Lerp(_ammoPickupRateEma, ammoPickupRate, pickupAlpha);
            _healthPickupRateEma = Mathf.Lerp(_healthPickupRateEma, healthPickupRate, pickupAlpha);

            _totalShots += Mathf.Max(0, frame.Shots);
            _totalHits += Mathf.Max(0, frame.Hits);

            if (_totalShots > 0)
            {
                float accuracySample = Mathf.Clamp01((float)_totalHits / _totalShots);
                _accuracyEma = Mathf.Lerp(_accuracyEma, accuracySample, accuracyAlpha);
            }
        }

        public AdaptiveStatsSnapshot GetSnapshot()
        {
            return new AdaptiveStatsSnapshot(
                killsPerSecond: _killsEma,
                damageDealtPerSecond: _damageDealtEma,
                damageTakenPerSecond: _damageTakenEma,
                accuracy: _accuracyEma,
                combo: _comboEma,
                ammoPickupRate: _ammoPickupRateEma,
                healthPickupRate: _healthPickupRateEma);
        }

        public float NormalizedKills() => Mathf.Clamp01(_killsEma / Mathf.Max(_settings.NormalizedKillsPerSecond, 1e-6f));

        public float NormalizedDamageDealt() => Mathf.Clamp01(_damageDealtEma / Mathf.Max(_settings.NormalizedDamageDealtPerSecond, 1e-6f));

        public float NormalizedDamageTaken() => Mathf.Clamp01(_damageTakenEma / Mathf.Max(_settings.NormalizedDamageTakenPerSecond, 1e-6f));

        public float NormalizedAccuracy() => Mathf.Clamp01(_accuracyEma);

        public float NormalizedCombo() => Mathf.Clamp01(_comboEma / Mathf.Max(_settings.NormalizedCombo, 1e-6f));

        public float NormalizedPickups()
        {
            float totalPickupRate = _ammoPickupRateEma + _healthPickupRateEma;
            return Mathf.Clamp01(totalPickupRate / (2f * Mathf.Max(_settings.NormalizedPickupsPerSecond, 1e-6f)));
        }

        private static float AlphaForTau(float dt, float tau)
        {
            if (dt <= 0f)
                return 1f;

            return dt / (tau + dt);
        }
    }
}
