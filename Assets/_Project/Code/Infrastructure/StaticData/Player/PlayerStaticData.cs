using _Project.Code.Core.Keys;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Player
{
    [CreateAssetMenu(fileName = nameof(PlayerStaticData), menuName = "_Project/StaticData/New PlayerStaticData")]
    public sealed class PlayerStaticData : ScriptableObject
    {
        public float MoveSpeed = 6f;
        public int MaxHealth = 100;
        public int Armor = 0;
        public WeaponId StartWeapon = WeaponId.Rifle;
        public int StartAmmo = 120;
    }
}
