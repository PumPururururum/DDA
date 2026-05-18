using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Infrastructure.StaticData.Resources
{
    [CreateAssetMenu(fileName = nameof(ResourcesStaticData), menuName = "_Project/StaticData/New ResourcesStaticData")]
    public sealed class ResourcesStaticData : ScriptableObject
    {
        public ResourceConfig[] Resources;

        public ResourceConfig Get(ResourceId id)
        {
            for (int i = 0; i < Resources.Length; i++)
                if (Resources[i].Id == id)
                    return Resources[i];

            return null;
        }
    }
}
