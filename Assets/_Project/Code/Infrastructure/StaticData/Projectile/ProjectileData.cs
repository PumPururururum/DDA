using System;
using _Project.Code.Core.Keys;

namespace _ExampleProject.Code.Infrastructure.StaticData.Projectile
{
    [Serializable]
    public sealed class ProjectileData
    {
        public ProjectileId Id;
        public MemoryPoolId PrefabId;
        public float MoveSpeed = 20f;
        public int Damage = 10;
        public float LifeTime = 2f;
    }
}
