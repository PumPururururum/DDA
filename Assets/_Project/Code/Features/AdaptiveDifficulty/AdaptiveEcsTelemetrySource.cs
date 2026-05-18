using AdaptiveDifficulty.Runtime;
using _Project.Code.Features.Test;
using UnityEngine;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    public sealed class AdaptiveEcsTelemetrySource : IAdaptiveTelemetrySource
    {
        private readonly AdaptiveDifficultySettings _settings;
        private readonly AdaptiveFrameTelemetry _frame = new();

        private float _comboTimer;
        private int _currentCombo;

        public AdaptiveEcsTelemetrySource(AdaptiveDifficultySettings settings)
        {
            _settings = settings;
        }

        public bool IsReady => true;

        public void Tick(float dt)
        {
            _comboTimer += Mathf.Max(0f, dt);

            if (_settings != null && _comboTimer >= _settings.ComboResetDelay)
                _currentCombo = 0;
        }

        public AdaptiveFrameTelemetry ConsumeFrameTelemetry()
        {
            AdaptiveFrameTelemetry output = new AdaptiveFrameTelemetry
            {
                Kills = _frame.Kills,
                DamageDealt = _frame.DamageDealt,
                DamageTaken = _frame.DamageTaken,
                Shots = _frame.Shots,
                Hits = _frame.Hits,
                Combo = _frame.Combo,
                AmmoPicked = _frame.AmmoPicked,
                HealthPicked = _frame.HealthPicked
            };

            _frame.Reset();
            return output;
        }

        public void ReportShot(CombatTeamId shooterTeam)
        {
            if (shooterTeam != CombatTeamId.Player)
                return;

            _frame.Shots += 1;
        }

        public void ReportDamage(CombatTeamId sourceTeam, CombatTeamId targetTeam, int damage)
        {
            if (damage <= 0)
                return;

            if (sourceTeam == CombatTeamId.Player && targetTeam == CombatTeamId.Enemy)
            {
                _frame.Hits += 1;
                _frame.DamageDealt += damage;
                return;
            }

            if (sourceTeam == CombatTeamId.Enemy && targetTeam == CombatTeamId.Player)
                _frame.DamageTaken += damage;
        }

        public void ReportEnemyKilled()
        {
            _frame.Kills += 1;
            RegisterComboKill();
        }

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

        public void RegisterProjectileCollision(int projectileEntity, GameObject collisionRef)
        {
            // Compatibility hook for old prefabs with AdaptiveProjectileCollisionRelay.
            // Damage and hit telemetry are reported from ProjectileCollisionSystem now,
            // so this method intentionally does not touch health or difficulty stats.
        }

        private void RegisterComboKill()
        {
            float comboResetDelay = _settings != null ? _settings.ComboResetDelay : 3f;

            if (_comboTimer <= comboResetDelay)
                _currentCombo += 1;
            else
                _currentCombo = 1;

            _comboTimer = 0f;
            _frame.Combo = Mathf.Max(_frame.Combo, _currentCombo);
        }
    }
}
