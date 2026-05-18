namespace AdaptiveDifficulty.Runtime
{
    public sealed class AdaptiveFrameTelemetry
    {
        public int Kills;
        public float DamageDealt;
        public float DamageTaken;
        public int Shots;
        public int Hits;
        public int Combo;
        public int AmmoPicked;
        public int HealthPicked;

        public void Reset()
        {
            Kills = 0;
            DamageDealt = 0f;
            DamageTaken = 0f;
            Shots = 0;
            Hits = 0;
            Combo = 0;
            AmmoPicked = 0;
            HealthPicked = 0;
        }
    }
}