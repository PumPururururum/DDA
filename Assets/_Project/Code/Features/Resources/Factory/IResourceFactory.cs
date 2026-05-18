using _Project.Code.Core.Keys;
using UnityEngine;

namespace _Project.Code.Features.Resources.Factory
{
    public interface IResourceFactory
    {
        int Create(ResourceId id, Vector2 position);
    }
}
