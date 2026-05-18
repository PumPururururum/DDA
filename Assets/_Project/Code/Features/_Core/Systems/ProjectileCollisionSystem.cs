using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Behaviours;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Enemy.Facade;
using _ExampleProject.Code.Features.Player.Facade;
using _ExampleProject.Code.Features.Projectile.Components;
using _Project.Code.Features.Test;
using _Project.Infrastructure;
using Leopotam.EcsLite;
using Leopotam.EcsLite.Di;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class ProjectileCollisionSystem : IEcsRunSystem
    {
        private readonly EcsFilterInject<Inc<CollisionEvent>> _filter = EcsWorlds.EVENTS;
        private readonly EcsPoolInject<CollisionEvent> _eventPool = EcsWorlds.EVENTS;
        private readonly EcsPoolInject<ProjectileTag> _projPool;
        private readonly EcsPoolInject<ProjectileDamage> _damagePool;
        private readonly EcsPoolInject<CombatTeam> _teamPool;
        private readonly EcsPoolInject<Health> _healthPool;
        private readonly EcsPoolInject<Armor> _armorPool;
        private readonly EcsPoolInject<DeathRequest> _deathRequestPool;

        public void Run(IEcsSystems systems)
        {
            foreach (var entity in _filter.Value)
            {
                ref var eventData = ref _eventPool.Value.Get(entity);

                if (!_projPool.Value.Has(eventData.Entity))
                    continue;

                var projectileEntity = eventData.Entity;
                var projectileTeam = _teamPool.Value.Has(projectileEntity)
                    ? _teamPool.Value.Get(projectileEntity).Value
                    : CombatTeamId.Neutral;
                var projectileDamage = _damagePool.Value.Has(projectileEntity)
                    ? _damagePool.Value.Get(projectileEntity).Value
                    : 0;

                if (TryResolveTarget(eventData.CollisionRef, out var targetEntity, out var targetTeam))
                {
                    var isFriendlyFire = projectileTeam != CombatTeamId.Neutral && projectileTeam == targetTeam;

                    if (isFriendlyFire)
                        continue;

                    int appliedDamage = ApplyDamage(targetEntity, projectileDamage);
                    if (appliedDamage > 0)
                        ProjectAdaptiveDifficultyBootstrap.Instance?.ReportDamage(projectileTeam, targetTeam, appliedDamage);
                }

                AddDeathRequest(projectileEntity);
            }
        }

        private bool TryResolveTarget(GameObject collisionRef, out int entity, out CombatTeamId team)
        {
            entity = default;
            team = CombatTeamId.Neutral;

            if (collisionRef == null)
                return false;

            var playerFacade = collisionRef.GetComponent<PlayerFacade>();
            if (playerFacade == null)
                playerFacade = collisionRef.GetComponentInParent<PlayerFacade>();

            if (playerFacade != null && playerFacade.EcsEntity != null)
            {
                entity = playerFacade.EcsEntity.Index;
                team = CombatTeamId.Player;
                return true;
            }

            var enemyFacade = collisionRef.GetComponent<EnemyFacade>();
            if (enemyFacade == null)
                enemyFacade = collisionRef.GetComponentInParent<EnemyFacade>();

            if (enemyFacade != null && enemyFacade.EcsEntity != null)
            {
                entity = enemyFacade.EcsEntity.Index;
                team = CombatTeamId.Enemy;
                return true;
            }

            return false;
        }

        private int ApplyDamage(int entity, int rawDamage)
        {
            if (!_healthPool.Value.Has(entity))
                return 0;

            ref var health = ref _healthPool.Value.Get(entity);
            int previousHealth = health.CurrentValue;
            int armor = _armorPool.Value.Has(entity) ? _armorPool.Value.Get(entity).Value : 0;
            int finalDamage = Mathf.Max(1, rawDamage - armor);

            health.CurrentValue = Mathf.Max(0, health.CurrentValue - finalDamage);
            int appliedDamage = Mathf.Max(0, previousHealth - health.CurrentValue);

            if (health.CurrentValue <= 0)
                AddDeathRequest(entity);

            return appliedDamage;
        }

        private void AddDeathRequest(int entity)
        {
            if (!_deathRequestPool.Value.Has(entity))
                _deathRequestPool.Value.Add(entity);
        }
    }
}
