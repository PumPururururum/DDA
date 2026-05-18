namespace AdaptiveDifficulty.Runtime
{
    public readonly struct AdaptiveDifficultySnapshot
    {
        public readonly float Difficulty;
        public readonly float RuntimeIntensity;
        public readonly float PerformanceScore;
        public readonly float SafetyScore;

        public AdaptiveDifficultySnapshot(
            float difficulty,
            float runtimeIntensity,
            float performanceScore,
            float safetyScore)
        {
            Difficulty = difficulty;
            RuntimeIntensity = runtimeIntensity;
            PerformanceScore = performanceScore;
            SafetyScore = safetyScore;
        }
    }
}