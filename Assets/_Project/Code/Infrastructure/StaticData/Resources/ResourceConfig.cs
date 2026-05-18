using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Infrastructure.StaticData.Resources
{
    [Serializable]
    public sealed class ResourceConfig
    {
        public ResourceId Id;
        public MemoryPoolId PrefabId;
        public int Amount = 10;
        public float PickupRadius = 1.25f;
    }
}
