using AdaptiveDifficulty.Runtime;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    public readonly struct AdaptiveProjectileOwnerInfo
    {
        public readonly int Entity;
        public readonly AdaptiveProjectileOwnerKind Kind;

        public AdaptiveProjectileOwnerInfo(int entity, AdaptiveProjectileOwnerKind kind)
        {
            Entity = entity;
            Kind = kind;
        }
    }
}