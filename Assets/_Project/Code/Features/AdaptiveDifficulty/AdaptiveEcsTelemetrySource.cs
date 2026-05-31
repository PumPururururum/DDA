using AdaptiveDifficulty.Runtime;
using _Project.Code.Features.Test;
using UnityEngine;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    /// <summary>
    /// ECS-источник телеметрии для системы адаптивной сложности.
    /// Накапливает события за кадр и предоставляет их контроллеру через IAdaptiveTelemetrySource.
    ///
    /// События поступают из:
    ///   - WeaponShootSystem → ReportShot
    ///   - ProjectileCollisionSystem → ReportDamage
    ///   - EnemyDeathSystem → ReportEnemyKilled
    ///   - ResourcePickupSystem → ReportPickup
    /// </summary>
    public sealed class AdaptiveEcsTelemetrySource : IAdaptiveTelemetrySource
    {
        private readonly AdaptiveDifficultySettings _settings;
        private readonly AdaptiveFrameTelemetry _frame = new();

        private float _comboTimer;
        private int   _currentCombo;
        private int   _totalKillsThisRun;

        public AdaptiveEcsTelemetrySource(AdaptiveDifficultySettings settings)
        {
            _settings = settings;
        }

        // ─── IAdaptiveTelemetrySource ────────────────────────────────────────

        public bool IsReady => true;

        public void Tick(float dt)
        {
            _comboTimer += Mathf.Max(0f, dt);

            if (_settings != null && _comboTimer >= _settings.ComboResetDelay)
                _currentCombo = 0;
        }

        public AdaptiveFrameTelemetry ConsumeFrameTelemetry()
        {
            var output = new AdaptiveFrameTelemetry
            {
                Kills        = _frame.Kills,
                DamageDealt  = _frame.DamageDealt,
                DamageTaken  = _frame.DamageTaken,
                Shots        = _frame.Shots,
                Hits         = _frame.Hits,
                Combo        = _frame.Combo,
                AmmoPicked   = _frame.AmmoPicked,
                HealthPicked = _frame.HealthPicked
            };

            _frame.Reset();
            return output;
        }

        // ─── Публичные свойства для дебаг-дисплея ────────────────────────────

        /// <summary>Текущая длина активного комбо (сбрасывается при паузе > ComboResetDelay).</summary>
        public int CurrentCombo => _currentCombo;

        /// <summary>Общее количество убийств с начала текущего запуска/рестарта.</summary>
        public int TotalKillsThisRun => _totalKillsThisRun;

        // ─── Точки вызова из ECS-систем ──────────────────────────────────────

        /// <summary>Вызывается из WeaponShootSystem при каждом выстреле.</summary>
        public void ReportShot(CombatTeamId shooterTeam)
        {
            if (shooterTeam != CombatTeamId.Player)
                return;

            _frame.Shots += 1;
        }

        /// <summary>Вызывается из ProjectileCollisionSystem при нанесении/получении урона.</summary>
        public void ReportDamage(CombatTeamId sourceTeam, CombatTeamId targetTeam, int damage)
        {
            if (damage <= 0)
                return;

            if (sourceTeam == CombatTeamId.Player && targetTeam == CombatTeamId.Enemy)
            {
                _frame.Hits       += 1;
                _frame.DamageDealt += damage;
                return;
            }

            if (sourceTeam == CombatTeamId.Enemy && targetTeam == CombatTeamId.Player)
                _frame.DamageTaken += damage;
        }

        /// <summary>Вызывается из EnemyDeathSystem при смерти врага.</summary>
        public void ReportEnemyKilled()
        {
            _frame.Kills += 1;
            _totalKillsThisRun += 1;
            RegisterComboKill();
        }

        /// <summary>Вызывается из ResourcePickupSystem при подборе ресурса.</summary>
        public void ReportPickup(AdaptivePickupType pickupType)
        {
            switch (pickupType)
            {
                case AdaptivePickupType.Ammo:
                    _frame.AmmoPicked += 1;
                    break;
                case AdaptivePickupType.Health:
                    _frame.HealthPicked += 1;
                    break;
            }
        }

        /// <summary>
        /// Совместимость с устаревшими префабами, использующими AdaptiveProjectileCollisionRelay.
        /// Урон и статистика попаданий теперь отслеживаются из ProjectileCollisionSystem,
        /// поэтому метод намеренно оставлен пустым.
        /// </summary>
        public void RegisterProjectileCollision(int projectileEntity, UnityEngine.GameObject collisionRef)
        {
        }

        /// <summary>Сбросить накопленную статистику (при рестарте игры).</summary>
        public void ResetRunStats()
        {
            _totalKillsThisRun = 0;
            _currentCombo      = 0;
            _comboTimer        = 0f;
            _frame.Reset();
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────

        private void RegisterComboKill()
        {
            float comboResetDelay = _settings != null ? _settings.ComboResetDelay : 3f;

            if (_comboTimer <= comboResetDelay)
                _currentCombo += 1;
            else
                _currentCombo = 1;

            _comboTimer    = 0f;
            _frame.Combo   = Mathf.Max(_frame.Combo, _currentCombo);
        }
    }
}
