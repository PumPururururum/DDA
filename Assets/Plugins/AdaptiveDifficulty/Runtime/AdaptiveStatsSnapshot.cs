namespace AdaptiveDifficulty.Runtime
{
    public readonly struct AdaptiveStatsSnapshot
    {
        public readonly float KillsPerSecond;
        public readonly float DamageDealtPerSecond;
        public readonly float DamageTakenPerSecond;
        public readonly float Accuracy;
        public readonly float Combo;
        public readonly float AmmoPickupRate;
        public readonly float HealthPickupRate;

        public AdaptiveStatsSnapshot(
            float killsPerSecond,
            float damageDealtPerSecond,
            float damageTakenPerSecond,
            float accuracy,
            float combo,
            float ammoPickupRate,
            float healthPickupRate)
        {
            KillsPerSecond = killsPerSecond;
            DamageDealtPerSecond = damageDealtPerSecond;
            DamageTakenPerSecond = damageTakenPerSecond;
            Accuracy = accuracy;
            Combo = combo;
            AmmoPickupRate = ammoPickupRate;
            HealthPickupRate = healthPickupRate;
        }
    }
}