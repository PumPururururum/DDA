using _Project.Code.Core.Keys;
using _Project.Code.Features.Test;
using UnityEngine;

namespace _ExampleProject.Code.Features.Projectile.Factory
{
    public interface IProjectileFactory
    {
        int Create(ProjectileId id, Vector2 position, Vector2 direction);
        int Create(ProjectileId id, Vector2 position, Vector2 direction, CombatTeamId team);
    }
}
