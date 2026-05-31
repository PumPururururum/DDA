using System;
using _Project.Code.Core.Keys;

namespace _ExampleProject.Code.Infrastructure.StaticData.Weapons
{
    [Serializable]
    public sealed class WeaponData
    {
        public WeaponId Id;
        public int MaxTotalAmmo;
        public int MaxMagazineSize;
        public ProjectileId ProjectileId;
        public int BurstsPerShot;
        public float BurstInterval;
        public int ProjectilesPerBurst;
        public int AmmoPerShot;
        public float ReloadTime;
        public float ShootSpeed;
        /// <summary>
        /// Минимальное время (сек) между двумя выстрелами.
        /// Новый выстрел блокируется, пока кулдаун не обнулится.
        /// </summary>
        public float FireRate;
    }
}