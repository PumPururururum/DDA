using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    /// <summary>
    /// Сервис EMA-сглаживания игровых метрик.
    /// Принимает сырые данные за кадр, применяет экспоненциальное скользящее среднее
    /// и предоставляет нормализованные значения для расчёта I и S_safety.
    /// </summary>
    public sealed class AdaptiveStatsService
    {
        private readonly AdaptiveDifficultySettings _settings;

        // EMA-сглаженные значения (в единицах в секунду или абсолютных)
        private float _killsEma;
        private float _damageDealtEma;
        private float _damageTakenEma;
        private float _accuracyEma;
        private float _comboEma;
        private float _ammoPickupRateEma;
        private float _healthPickupRateEma;

        // Для расчёта точности через сглаженные ставки попаданий/выстрелов.
        // FIX: заменяем накопительные _totalShots/_totalHits на EMA-ставки.
        // Прежний подход (накопительный) приводил к тому, что точность «деревенела»
        // по мере роста _totalShots и переставала реагировать на изменения поведения.
        private float _shotsRateEma;
        private float _hitsRateEma;

        public AdaptiveStatsService(AdaptiveDifficultySettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Обработать метрики за текущий кадр.
        /// Вызывается каждый кадр из AdaptiveDifficultyController.Tick().
        /// </summary>
        public void OnFrameSamples(AdaptiveFrameTelemetry frame, float dt)
        {
            float safeDt = Mathf.Max(dt, 1e-6f);

            // Перевод в ставки (единицы в секунду)
            float killsRate        = frame.Kills       / safeDt;
            float damageDealtRate  = frame.DamageDealt / safeDt;
            float damageTakenRate  = frame.DamageTaken / safeDt;
            float shotsRate        = frame.Shots        / safeDt;
            float hitsRate         = frame.Hits         / safeDt;
            float ammoPickupRate   = frame.AmmoPicked   / safeDt;
            float healthPickupRate = frame.HealthPicked / safeDt;

            // Коэффициенты сглаживания (alpha = dt / (tau + dt))
            float killsAlpha      = AlphaForTau(safeDt, _settings.TauKills);
            float damageDealtAlpha = AlphaForTau(safeDt, _settings.TauDamageDealt);
            float damageTakenAlpha = AlphaForTau(safeDt, _settings.TauDamageTaken);
            float accuracyAlpha   = AlphaForTau(safeDt, _settings.TauAccuracy);
            float comboAlpha      = AlphaForTau(safeDt, _settings.TauCombo);
            float pickupAlpha     = AlphaForTau(safeDt, _settings.TauPickups);

            // EMA-обновление основных метрик
            _killsEma          = Mathf.Lerp(_killsEma,          killsRate,        killsAlpha);
            _damageDealtEma    = Mathf.Lerp(_damageDealtEma,    damageDealtRate,  damageDealtAlpha);
            _damageTakenEma    = Mathf.Lerp(_damageTakenEma,    damageTakenRate,  damageTakenAlpha);
            _comboEma          = Mathf.Lerp(_comboEma,          frame.Combo,      comboAlpha);
            _ammoPickupRateEma = Mathf.Lerp(_ammoPickupRateEma, ammoPickupRate,   pickupAlpha);
            _healthPickupRateEma = Mathf.Lerp(_healthPickupRateEma, healthPickupRate, pickupAlpha);

            // FIX: EMA-обновление точности через сглаженные ставки выстрелов/попаданий.
            // Это устраняет «деревенение» точности при накоплении большого _totalShots.
            // Алгоритм: сглаживаем shots/s и hits/s отдельно, затем берём их соотношение.
            _shotsRateEma = Mathf.Lerp(_shotsRateEma, shotsRate, accuracyAlpha);
            _hitsRateEma  = Mathf.Lerp(_hitsRateEma,  hitsRate,  accuracyAlpha);

            // Если игрок стрелял в этом кадре — обновляем мгновенную точность
            // и подмешиваем её в EMA. Если не стрелял — EMA точности не меняется.
            if (frame.Shots > 0)
            {
                float frameAccuracy = Mathf.Clamp01((float)frame.Hits / frame.Shots);
                _accuracyEma = Mathf.Lerp(_accuracyEma, frameAccuracy, accuracyAlpha);
            }
        }

        /// <summary>Снапшот текущих EMA-значений для дебаг-вывода.</summary>
        public AdaptiveStatsSnapshot GetSnapshot()
        {
            return new AdaptiveStatsSnapshot(
                killsPerSecond:       _killsEma,
                damageDealtPerSecond: _damageDealtEma,
                damageTakenPerSecond: _damageTakenEma,
                accuracy:             NormalizedAccuracy(),
                combo:                _comboEma,
                ammoPickupRate:       _ammoPickupRateEma,
                healthPickupRate:     _healthPickupRateEma);
        }

        // ─── Нормализованные значения ─────────────────────────────────────────

        public float NormalizedKills() =>
            Mathf.Clamp01(_killsEma / Mathf.Max(_settings.NormalizedKillsPerSecond, 1e-6f));

        public float NormalizedDamageDealt() =>
            Mathf.Clamp01(_damageDealtEma / Mathf.Max(_settings.NormalizedDamageDealtPerSecond, 1e-6f));

        public float NormalizedDamageTaken() =>
            Mathf.Clamp01(_damageTakenEma / Mathf.Max(_settings.NormalizedDamageTakenPerSecond, 1e-6f));

        /// <summary>
        /// Нормализованная точность [0, 1].
        /// FIX: вычисляется как отношение EMA-ставок попаданий к выстрелам,
        /// что обеспечивает актуальную точность вне зависимости от общей истории стрельбы.
        /// </summary>
        public float NormalizedAccuracy()
        {
            // Если игрок давно не стрелял (_shotsRateEma ≈ 0), возвращаем последнюю известную точность
            if (_shotsRateEma < 1e-4f)
                return Mathf.Clamp01(_accuracyEma);

            // Иначе — соотношение EMA-ставок попаданий к выстрелам
            return Mathf.Clamp01(_hitsRateEma / _shotsRateEma);
        }

        public float NormalizedCombo() =>
            Mathf.Clamp01(_comboEma / Mathf.Max(_settings.NormalizedCombo, 1e-6f));

        public float NormalizedPickups()
        {
            float totalPickupRate = _ammoPickupRateEma + _healthPickupRateEma;
            return Mathf.Clamp01(totalPickupRate / (2f * Mathf.Max(_settings.NormalizedPickupsPerSecond, 1e-6f)));
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────

        /// <summary>
        /// Коэффициент α для EMA: α = dt / (τ + dt).
        /// При dt→0: α→0 (новые данные почти не влияют).
        /// При dt→∞: α→1 (новые данные полностью заменяют старые).
        /// </summary>
        private static float AlphaForTau(float dt, float tau)
        {
            if (dt <= 0f) return 1f;
            return dt / (tau + dt);
        }
    }
}
