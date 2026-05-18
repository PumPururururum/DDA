using UnityEngine;

namespace _Project.Code.Infrastructure.StaticData.GameLoop
{
    [CreateAssetMenu(fileName = nameof(GameLoopStaticData), menuName = "_Project/StaticData/New GameLoopStaticData")]
    public sealed class GameLoopStaticData : ScriptableObject
    {
        [Header("Enemies")]
        public int BaseEnemyCount = 2;
        public int AdditionalEnemiesPerLevel = 1;
        public int MaxEnemiesPerLevel = 14;

        [Header("Adaptive enemy scaling")]
        public bool UseAdaptiveDifficulty = true;
        [Range(0f, 1f)] public float FallbackDifficulty = 0.4f;
        public int ExtraEnemiesAtMaxDifficulty = 5;
        public int MinWavesPerLevel = 1;
        public int ExtraWavesAtMaxDifficulty = 2;
        public int MaxWavesPerLevel = 4;
        public int MaxEnemiesPerWave = 5;
        public float MinWaveDelay = 1.25f;
        public float MaxWaveDelay = 4f;
        public int LowDifficultySpawnPointStride = 3;
        public int HighDifficultySpawnPointStride = 1;
        public float MinSpawnJitterRadius = 0f;
        public float MaxSpawnJitterRadius = 0.6f;
        public float EnemyTypeUnlockPerLevel = 0.2f;
        public float EnemyTypeUnlockTolerance = 0.35f;
        public float MaxEnemyDifficultyWeight = 3f;
        public int MinActiveEnemiesBeforeNextWave = 0;
        public int ExtraActiveEnemiesAtMaxDifficulty = 4;

        [Header("Resources")]
        public int BaseAmmoPickups = 1;
        public int BaseHealthPickups = 1;
        public int ResourceReductionEachTwoLevels = 1;
        public int BonusAmmoPickupsAtLowDifficulty = 2;
        public int BonusHealthPickupsAtLowDifficulty = 2;
        public int ResourceReductionAtMaxDifficulty = 1;
        public int MinAmmoPickups = 0;
        public int MaxAmmoPickups = 4;
        public int MinHealthPickups = 0;
        public int MaxHealthPickups = 3;

        [Header("Flow")]
        public float NextLevelDelay = 2f;

        [Header("Exit")]
        public Vector2 ExitOffsetFromPlayerSpawn = new Vector2(0f, 3f);
        public Vector2 ExitColliderSize = new Vector2(1.5f, 1.5f);
        public float ExitEnterRadius = 1f;
        public bool ShowGeneratedExitVisual = true;
    }
}
