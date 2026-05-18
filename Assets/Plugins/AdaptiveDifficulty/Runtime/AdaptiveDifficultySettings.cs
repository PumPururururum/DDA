using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    [CreateAssetMenu(fileName = nameof(AdaptiveDifficultySettings), menuName = "AdaptiveDifficulty/New AdaptiveDifficultySettings")]
    public sealed class AdaptiveDifficultySettings : ScriptableObject
    {
        [Header("Stats smoothing (seconds)")]
        [Min(0.01f)] public float TauKills = 8f;
        [Min(0.01f)] public float TauDamageDealt = 8f;
        [Min(0.01f)] public float TauDamageTaken = 10f;
        [Min(0.01f)] public float TauAccuracy = 6f;
        [Min(0.01f)] public float TauCombo = 4f;
        [Min(0.01f)] public float TauPickups = 12f;

        [Header("Normalization")]
        [Min(0.001f)] public float NormalizedKillsPerSecond = 0.33f;
        [Min(0.001f)] public float NormalizedDamageDealtPerSecond = 50f;
        [Min(0.001f)] public float NormalizedDamageTakenPerSecond = 25f;
        [Min(0.001f)] public float NormalizedCombo = 10f;
        [Min(0.001f)] public float NormalizedPickupsPerSecond = 0.5f;

        [Header("Performance weights")]
        [Range(0f, 1f)] public float KillWeight = 0.45f;
        [Range(0f, 1f)] public float ComboWeight = 0.30f;
        [Range(0f, 1f)] public float DamageWeight = 0.25f;
        [Min(0.01f)] public float PerformanceGamma = 1.3f;

        [Header("Safety weights")]
        [Range(0f, 1f)] public float DamageTakenWeight = 0.50f;
        [Range(0f, 1f)] public float AccuracyPenaltyWeight = 0.30f;
        [Range(0f, 1f)] public float PickupsWeight = 0.20f;

        [Header("Difficulty update")]
        [Range(0f, 1f)] public float InitialDifficulty = 0.40f;
        [Min(0f)] public float BetaUp = 0.10f;
        [Min(0f)] public float BetaDown = 0.12f;
        [Range(0f, 1f)] public float MaxDifficultyDelta = 0.15f;

        [Header("Runtime intensity")]
        [Min(0.01f)] public float RuntimeTau = 2.5f;
        [Min(0.01f)] public float AudioTau = 0.6f;

        [Header("Combo")]
        [Min(0.01f)] public float ComboResetDelay = 3f;

        [Header("Project integration")]
        [Min(0)] public int DefaultPlayerMaxHealth = 100;
        [Min(0)] public int DefaultPlayerArmor = 0;
        [Min(1)] public int DefaultProjectileDamage = 10;
        [Min(0.01f)] public float ProjectileOwnerSearchRadius = 1f;

        [Tooltip("If enabled, any enemy health reduction will be interpreted as player damage. Keep disabled for the current project unless player shooting is added.")]
        public bool CountAnyEnemyHealthLossAsPlayerDamage = false;

        [Tooltip("If enabled, enemy despawn with DeathRequest is counted as a kill.")]
        public bool CountEnemyDespawnAsKill = true;

        [Header("Debug")]
        public bool EnableDebugLogs = true;
        [Min(0.1f)] public float DebugLogInterval = 2f;
    }
}