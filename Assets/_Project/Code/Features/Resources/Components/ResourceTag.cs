using System;
using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Features.Resources.Components
{
    [Serializable]
    public struct ResourceTag
    {
        public ResourceId ResourceId;
        public int Amount;
        public float PickupRadius;
        public GameObject GameObjectRef;

        public void Setup(ResourceId resourceId, int amount, float pickupRadius, GameObject gameObjectRef)
        {
            ResourceId = resourceId;
            Amount = amount;
            PickupRadius = pickupRadius;
            GameObjectRef = gameObjectRef;
        }
    }
}
