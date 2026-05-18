using System.Collections.Generic;
using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Behaviours;
using _ExampleProject.Code.Features._Core.Components;
using _ExampleProject.Code.Features._Core.Requests;
using _ExampleProject.Code.Features.Enemy.Factory;
using _ExampleProject.Code.Infrastructure.StaticData.Enemy;
using _ExampleProject.Code.Features.Player.Components;
using _ExampleProject.Code.Features.Player.Factory;
using _ExampleProject.Code.Features.Projectile.Components;
using _ExampleProject.Code.Scene.SpawnPoints;
using _Project.Code.Core.Abstractions.Contracts;
using _Project.Code.Core.Keys;
using _Project.Code.Features.Resources.Components;
using _Project.Code.Features.Resources.Factory;
using _Project.Code.Features.Test;
using _Project.Code.Infrastructure;
using _Project.Code.Infrastructure.StaticData.GameLoop;
using _Project.Code.Scene.SpawnPoints;
using _Project.Infrastructure;
using Infrastructure.MemoryPool.Service;
using Leopotam.EcsLite;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    public sealed class GameLoopSystem : IConstruct, ITick, ICleanUp
    {
        private readonly List<int> _entitiesToDelete = new();
        private readonly List<int> _btStatesToDelete = new();

        private IEnemyFactory _enemyFactory;
        private IResourceFactory _resourceFactory;
        private IPlayerFactory _playerFactory;
        private PlayerSpawnPosition _playerSpawnPosition;
        private EnemySpawnPointsProvider _enemySpawnPoints;
        private ResourceSpawnPointsProvider _resourceSpawnPoints;
        private GameLoopStaticData _settings;
        private IMemoryPoolService _memoryPoolService;
        private StaticDataService _staticData;

        private EcsWorld _world;
        private EcsWorld _btStateWorld;

        private EcsFilter _playerFilter;
        private EcsFilter _enemyFilter;
        private EcsFilter _resourceFilter;
        private EcsFilter _projectileFilter;
        private EcsFilter _btStateFilter;

        private EcsPool<EnemyTag> _enemyPool;
        private EcsPool<ResourceTag> _resourcePool;
        private EcsPool<ProjectileTag> _projectilePool;
        private EcsPool<UnityTransform> _transformPool;
        private EcsPool<UnityRigidbody> _rigidbodyPool;
        private EcsPool<Movement> _movementPool;
        private EcsPool<WeaponShootRequest> _shootRequestPool;
        private EcsPool<WeaponReloadRequest> _reloadRequestPool;
        private EcsPool<AgentEntity> _btAgentPool;

        private GameObject _exitObject;
        private BoxCollider2D _exitCollider;

        private int _currentLevel;
        private bool _initialWaveSpawned;
        private bool _isWaitingRespawn;
        private float _respawnTick;
        private bool _isExitOpen;
        private bool _exitRequested;

        private LevelSpawnPlan _spawnPlan;
        private bool _isSpawningLevel;
        private int _spawnPointCursor;

        public void Construct()
        {
            _enemyFactory = ServiceLocator.Resolve<IEnemyFactory>();
            _resourceFactory = ServiceLocator.Resolve<IResourceFactory>();
            _playerFactory = ServiceLocator.Resolve<IPlayerFactory>();
            _staticData = ServiceLocator.Resolve<StaticDataService>();
            _settings = _staticData.GameLoopStaticData;
            _memoryPoolService = ServiceLocator.Resolve<IMemoryPoolService>();

            _world = EcsWorlds.GetWorld(EcsWorlds.DEFAULT);
            _btStateWorld = EcsWorlds.GetWorld(EcsWorlds.BT_STATES);

            _playerFilter = _world.Filter<PlayerTag>().End();
            _enemyFilter = _world.Filter<EnemyTag>().End();
            _resourceFilter = _world.Filter<ResourceTag>().End();
            _projectileFilter = _world.Filter<ProjectileTag>().End();
            _btStateFilter = _btStateWorld.Filter<AgentEntity>().End();

            _enemyPool = _world.GetPool<EnemyTag>();
            _resourcePool = _world.GetPool<ResourceTag>();
            _projectilePool = _world.GetPool<ProjectileTag>();
            _transformPool = _world.GetPool<UnityTransform>();
            _rigidbodyPool = _world.GetPool<UnityRigidbody>();
            _movementPool = _world.GetPool<Movement>();
            _shootRequestPool = _world.GetPool<WeaponShootRequest>();
            _reloadRequestPool = _world.GetPool<WeaponReloadRequest>();
            _btAgentPool = _btStateWorld.GetPool<AgentEntity>();

            _playerSpawnPosition = Object.FindFirstObjectByType<PlayerSpawnPosition>();
            _enemySpawnPoints = Object.FindFirstObjectByType<EnemySpawnPointsProvider>();
            _resourceSpawnPoints = Object.FindFirstObjectByType<ResourceSpawnPointsProvider>();
        }

        public void Tick()
        {
            if (!_initialWaveSpawned)
            {
                SpawnCurrentLevel();
                _initialWaveSpawned = true;
                return;
            }

            if (!HasPlayer())
            {
                HandlePlayerRespawn();
                return;
            }

            _isWaitingRespawn = false;

            UpdateWaveSpawning();

            if (_isSpawningLevel || _enemyFilter.GetEntitiesCount() > 0)
                return;

            if (!_isExitOpen)
            {
                OpenExit();
                return;
            }

            if (_exitRequested || IsPlayerInsideExit())
                LoadNextLevel();
        }

        public void CleanUp()
        {
            CloseExit();
            _initialWaveSpawned = false;
            _isWaitingRespawn = false;
            _isSpawningLevel = false;
            _exitRequested = false;
        }

        private bool HasPlayer()
        {
            return _playerFilter.GetEntitiesCount() > 0;
        }

        private void HandlePlayerRespawn()
        {
            CloseExit();
            _isSpawningLevel = false;

            if (!_isWaitingRespawn)
            {
                _isWaitingRespawn = true;
                _respawnTick = 2f;
                return;
            }

            _respawnTick -= Time.deltaTime;
            if (_respawnTick > 0f)
                return;

            _isWaitingRespawn = false;
            ResetRun();
        }

        private void ResetRun()
        {
            CloseExit();
            ClearEnemies();
            ClearResources();
            ClearProjectiles();
            _currentLevel = 0;
            _isSpawningLevel = false;

            if (!HasPlayer() && _playerSpawnPosition != null)
                _playerFactory.Create(_playerSpawnPosition.SpawnPosition);
            else
                MovePlayerToSpawn();

            SpawnCurrentLevel();
            _initialWaveSpawned = true;
        }

        private void LoadNextLevel()
        {
            _exitRequested = false;
            CloseExit();
            ClearResources();
            ClearProjectiles();
            CommitAdaptiveDifficultyForClearedLevel();
            _currentLevel += 1;
            MovePlayerToSpawn();
            SpawnCurrentLevel();
        }

        private void CommitAdaptiveDifficultyForClearedLevel()
        {
            if (_settings != null && !_settings.UseAdaptiveDifficulty)
                return;

            ProjectAdaptiveDifficultyBootstrap.Instance?.CommitLevel();
        }

        private void SpawnCurrentLevel()
        {
            BuildSpawnPlan();
            SpawnNextEnemyWave();
            SpawnResources();
        }

        private void BuildSpawnPlan()
        {
            float difficulty = GetCurrentDifficulty();
            int baseCount = _settings.BaseEnemyCount + _currentLevel * _settings.AdditionalEnemiesPerLevel;
            int difficultyBonus = Mathf.RoundToInt(Mathf.Max(0, _settings.ExtraEnemiesAtMaxDifficulty) * difficulty);
            int maxEnemies = Mathf.Max(0, _settings.MaxEnemiesPerLevel);
            int totalEnemies = Mathf.Clamp(baseCount + difficultyBonus, 0, maxEnemies);

            int waves = _settings.MinWavesPerLevel + Mathf.RoundToInt(Mathf.Max(0, _settings.ExtraWavesAtMaxDifficulty) * difficulty);
            waves = Mathf.Clamp(waves, 1, Mathf.Max(1, _settings.MaxWavesPerLevel));
            waves = Mathf.Min(waves, Mathf.Max(1, totalEnemies));

            int lowStride = Mathf.Max(1, _settings.LowDifficultySpawnPointStride);
            int highStride = Mathf.Max(1, _settings.HighDifficultySpawnPointStride);
            int spawnPointStride = Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(lowStride, highStride, difficulty)));

            _spawnPlan = new LevelSpawnPlan
            {
                Difficulty = difficulty,
                TotalEnemies = totalEnemies,
                TotalWaves = waves,
                SpawnedEnemies = 0,
                SpawnedWaves = 0,
                NextWaveTick = 0f,
                SpawnPointStride = spawnPointStride
            };

            _isSpawningLevel = totalEnemies > 0;
        }

        private void UpdateWaveSpawning()
        {
            if (!_isSpawningLevel)
                return;

            if (_spawnPlan.SpawnedEnemies >= _spawnPlan.TotalEnemies || _spawnPlan.SpawnedWaves >= _spawnPlan.TotalWaves)
            {
                _isSpawningLevel = false;
                return;
            }

            _spawnPlan.NextWaveTick -= Time.deltaTime;
            if (_spawnPlan.NextWaveTick > 0f)
                return;

            int maxActiveEnemies = GetMaxActiveEnemiesBeforeNextWave(_spawnPlan.Difficulty);
            if (_spawnPlan.SpawnedWaves > 0 && _enemyFilter.GetEntitiesCount() > maxActiveEnemies)
                return;

            SpawnNextEnemyWave();
        }

        private int GetMaxActiveEnemiesBeforeNextWave(float difficulty)
        {
            int minActive = Mathf.Max(0, _settings.MinActiveEnemiesBeforeNextWave);
            int extraActive = Mathf.Max(0, _settings.ExtraActiveEnemiesAtMaxDifficulty);
            return minActive + Mathf.RoundToInt(extraActive * difficulty);
        }

        private void SpawnNextEnemyWave()
        {
            if (_enemySpawnPoints == null || _enemySpawnPoints.SpawnPoints.Count == 0)
            {
                _isSpawningLevel = false;
                return;
            }

            if (_staticData == null || _staticData.EnemyStaticData == null || _staticData.EnemyStaticData.Enemies == null)
            {
                _isSpawningLevel = false;
                return;
            }

            int remainingEnemies = _spawnPlan.TotalEnemies - _spawnPlan.SpawnedEnemies;
            int remainingWaves = Mathf.Max(1, _spawnPlan.TotalWaves - _spawnPlan.SpawnedWaves);
            int waveSize = Mathf.CeilToInt((float)remainingEnemies / remainingWaves);

            if (_settings.MaxEnemiesPerWave > 0)
                waveSize = Mathf.Min(waveSize, _settings.MaxEnemiesPerWave);

            waveSize = Mathf.Clamp(waveSize, 0, remainingEnemies);

            for (int i = 0; i < waveSize; i++)
            {
                var enemyConfig = SelectEnemyConfig(_spawnPlan.Difficulty);
                if (enemyConfig == null)
                    continue;

                Vector2 position = GetEnemySpawnPosition(_spawnPlan.Difficulty);
                _enemyFactory.Create(enemyConfig.Id, position);
                _spawnPlan.SpawnedEnemies += 1;
            }

            _spawnPlan.SpawnedWaves += 1;

            if (_spawnPlan.SpawnedEnemies >= _spawnPlan.TotalEnemies || _spawnPlan.SpawnedWaves >= _spawnPlan.TotalWaves)
            {
                _isSpawningLevel = false;
                return;
            }

            _spawnPlan.NextWaveTick = Mathf.Lerp(_settings.MaxWaveDelay, _settings.MinWaveDelay, _spawnPlan.Difficulty);
        }

        private EnemyConfig SelectEnemyConfig(float difficulty)
        {
            var enemies = _staticData.EnemyStaticData.Enemies;
            if (enemies == null || enemies.Length == 0)
                return null;

            float maxWeight = Mathf.Max(0.001f, GetMaxEnemyDifficultyWeight(enemies));
            float targetWeight = difficulty * maxWeight + _currentLevel * Mathf.Max(0f, _settings.EnemyTypeUnlockPerLevel);
            float unlockTolerance = Mathf.Max(0f, _settings.EnemyTypeUnlockTolerance);
            float totalWeight = 0f;

            for (int i = 0; i < enemies.Length; i++)
            {
                var config = enemies[i];
                if (config == null)
                    continue;

                if (config.DifficultyWeight > targetWeight + unlockTolerance)
                    continue;

                totalWeight += GetEnemySelectionWeight(config, targetWeight, maxWeight, difficulty);
            }

            if (totalWeight <= 0f)
                return enemies[0];

            float roll = Random.value * totalWeight;
            float cursor = 0f;

            for (int i = 0; i < enemies.Length; i++)
            {
                var config = enemies[i];
                if (config == null)
                    continue;

                if (config.DifficultyWeight > targetWeight + unlockTolerance)
                    continue;

                cursor += GetEnemySelectionWeight(config, targetWeight, maxWeight, difficulty);
                if (roll <= cursor)
                    return config;
            }

            return enemies[0];
        }

        private float GetMaxEnemyDifficultyWeight(EnemyConfig[] enemies)
        {
            float configuredMax = _settings != null ? _settings.MaxEnemyDifficultyWeight : 0f;
            float maxWeight = configuredMax > 0f ? configuredMax : 0f;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                    maxWeight = Mathf.Max(maxWeight, enemies[i].DifficultyWeight);
            }

            return Mathf.Max(0.001f, maxWeight);
        }

        private static float GetEnemySelectionWeight(EnemyConfig config, float targetWeight, float maxWeight, float difficulty)
        {
            float normalizedWeight = Mathf.Clamp01(config.DifficultyWeight / maxWeight);
            float closeness = 1f / (1f + Mathf.Abs(config.DifficultyWeight - targetWeight));

            if (config.DifficultyWeight <= 0.001f)
                return Mathf.Lerp(1.6f, 0.45f, difficulty);

            return Mathf.Max(0.05f, closeness * Mathf.Lerp(0.35f, 1.4f, difficulty) + normalizedWeight * difficulty);
        }

        private Vector2 GetEnemySpawnPosition(float difficulty)
        {
            int spawnPointCount = _enemySpawnPoints.SpawnPoints.Count;
            int stride = Mathf.Max(1, _spawnPlan.SpawnPointStride);
            int index = (_currentLevel + _spawnPointCursor * stride + _spawnPlan.SpawnedWaves) % spawnPointCount;
            _spawnPointCursor += 1;

            var spawnPoint = _enemySpawnPoints.SpawnPoints[index];
            Vector2 position = spawnPoint.position;

            float jitterRadius = Mathf.Lerp(_settings.MinSpawnJitterRadius, _settings.MaxSpawnJitterRadius, difficulty);
            if (jitterRadius > 0.001f)
                position += Random.insideUnitCircle * jitterRadius;

            return position;
        }

        private void SpawnResources()
        {
            if (_resourceSpawnPoints == null || _resourceSpawnPoints.SpawnPoints.Count == 0)
                return;

            float difficulty = _spawnPlan.Difficulty;
            float assist = 1f - difficulty;
            int levelReduction = (_currentLevel / 2) * Mathf.Max(0, _settings.ResourceReductionEachTwoLevels);
            int highDifficultyReduction = Mathf.RoundToInt(Mathf.Max(0, _settings.ResourceReductionAtMaxDifficulty) * difficulty);

            int ammoCount = _settings.BaseAmmoPickups - levelReduction - highDifficultyReduction;
            ammoCount += Mathf.RoundToInt(Mathf.Max(0, _settings.BonusAmmoPickupsAtLowDifficulty) * assist);
            ammoCount = Mathf.Clamp(ammoCount, _settings.MinAmmoPickups, _settings.MaxAmmoPickups);

            int healthCount = _settings.BaseHealthPickups - levelReduction - highDifficultyReduction;
            healthCount += Mathf.RoundToInt(Mathf.Max(0, _settings.BonusHealthPickupsAtLowDifficulty) * assist);
            healthCount = Mathf.Clamp(healthCount, _settings.MinHealthPickups, _settings.MaxHealthPickups);

            int cursor = 0;
            for (int i = 0; i < ammoCount && cursor < _resourceSpawnPoints.SpawnPoints.Count; i++, cursor++)
                _resourceFactory.Create(ResourceId.AmmoSmall, _resourceSpawnPoints.SpawnPoints[cursor].position);

            for (int i = 0; i < healthCount && cursor < _resourceSpawnPoints.SpawnPoints.Count; i++, cursor++)
                _resourceFactory.Create(ResourceId.HealthSmall, _resourceSpawnPoints.SpawnPoints[cursor].position);
        }

        private float GetCurrentDifficulty()
        {
            if (_settings == null || !_settings.UseAdaptiveDifficulty)
                return _settings != null ? Mathf.Clamp01(_settings.FallbackDifficulty) : 0.4f;

            var bootstrap = ProjectAdaptiveDifficultyBootstrap.Instance;
            if (bootstrap == null)
                return Mathf.Clamp01(_settings.FallbackDifficulty);

            return Mathf.Clamp01(bootstrap.GetDifficultySnapshot().Difficulty);
        }

        private void OpenExit()
        {
            _isExitOpen = true;
            _exitRequested = false;

            if (_exitObject != null)
                return;

            var position = GetExitSpawnPosition();
            var size = GetExitSize();

            _exitObject = new GameObject($"Level Exit {_currentLevel + 1}");
            _exitObject.transform.position = position;

            _exitCollider = _exitObject.AddComponent<BoxCollider2D>();
            _exitCollider.isTrigger = true;
            _exitCollider.size = size;

            var trigger = _exitObject.AddComponent<LevelExitTrigger>();
            trigger.Construct(RequestLevelTransition);

            if (_settings == null || _settings.ShowGeneratedExitVisual)
                CreateExitVisual(_exitObject.transform, size);

#if UNITY_EDITOR
            Debug.Log($"Level cleared. Exit opened at {position}", _exitObject);
#endif
        }

        private void CloseExit()
        {
            _isExitOpen = false;
            _exitRequested = false;
            _exitCollider = null;

            if (_exitObject == null)
                return;

            Object.Destroy(_exitObject);
            _exitObject = null;
        }

        private void RequestLevelTransition()
        {
            _exitRequested = true;
        }

        private Vector2 GetExitSpawnPosition()
        {
            if (_playerSpawnPosition != null)
                return _playerSpawnPosition.SpawnPosition + GetExitOffset();

            if (_resourceSpawnPoints != null && _resourceSpawnPoints.SpawnPoints.Count > 0)
                return _resourceSpawnPoints.SpawnPoints[_resourceSpawnPoints.SpawnPoints.Count - 1].position;

            if (_enemySpawnPoints != null && _enemySpawnPoints.SpawnPoints.Count > 0)
                return _enemySpawnPoints.SpawnPoints[_enemySpawnPoints.SpawnPoints.Count - 1].position;

            return Vector2.zero;
        }

        private Vector2 GetExitOffset()
        {
            return _settings != null ? _settings.ExitOffsetFromPlayerSpawn : new Vector2(0f, 3f);
        }

        private Vector2 GetExitSize()
        {
            var size = _settings != null ? _settings.ExitColliderSize : new Vector2(1.5f, 1.5f);
            size.x = Mathf.Max(0.1f, size.x);
            size.y = Mathf.Max(0.1f, size.y);
            return size;
        }

        private bool IsPlayerInsideExit()
        {
            if (!_isExitOpen || _exitObject == null || !TryGetPlayerPosition(out var playerPosition))
                return false;

            if (_exitCollider != null && _exitCollider.OverlapPoint(playerPosition))
                return true;

            var exitPosition = (Vector2)_exitObject.transform.position;
            float radius = Mathf.Max(0.1f, _settings != null ? _settings.ExitEnterRadius : 1f);
            return (playerPosition - exitPosition).sqrMagnitude <= radius * radius;
        }

        private bool TryGetPlayerEntity(out int playerEntity)
        {
            foreach (var entity in _playerFilter)
            {
                playerEntity = entity;
                return true;
            }

            playerEntity = -1;
            return false;
        }

        private bool TryGetPlayerPosition(out Vector2 playerPosition)
        {
            if (!TryGetPlayerEntity(out var playerEntity))
            {
                playerPosition = default;
                return false;
            }

            if (_transformPool.Has(playerEntity) && _transformPool.Get(playerEntity).Ref != null)
            {
                playerPosition = _transformPool.Get(playerEntity).Ref.position;
                return true;
            }

            if (_rigidbodyPool.Has(playerEntity) && _rigidbodyPool.Get(playerEntity).Ref != null)
            {
                playerPosition = _rigidbodyPool.Get(playerEntity).Ref.position;
                return true;
            }

            playerPosition = default;
            return false;
        }

        private void MovePlayerToSpawn()
        {
            if (_playerSpawnPosition == null || !TryGetPlayerEntity(out var playerEntity))
                return;

            var spawnPosition = _playerSpawnPosition.SpawnPosition;

            if (_rigidbodyPool.Has(playerEntity) && _rigidbodyPool.Get(playerEntity).Ref != null)
            {
                var rigidbody = _rigidbodyPool.Get(playerEntity).Ref;
                rigidbody.linearVelocity = Vector2.zero;
                rigidbody.angularVelocity = 0f;
                rigidbody.position = spawnPosition;
            }

            if (_transformPool.Has(playerEntity) && _transformPool.Get(playerEntity).Ref != null)
                _transformPool.Get(playerEntity).Ref.position = spawnPosition;

            if (_movementPool.Has(playerEntity))
                _movementPool.Get(playerEntity).Direction = Vector2.zero;

            if (_shootRequestPool.Has(playerEntity))
                _shootRequestPool.Del(playerEntity);

            if (_reloadRequestPool.Has(playerEntity))
                _reloadRequestPool.Del(playerEntity);
        }

        private void ClearEnemies()
        {
            CopyFilterToBuffer(_enemyFilter);

            for (int i = 0; i < _entitiesToDelete.Count; i++)
            {
                int entity = _entitiesToDelete[i];
                if (!EcsEntityUtils.IsAlive(_world, entity) || !_enemyPool.Has(entity))
                    continue;

                RemoveBehaviourTreeStatesForAgent(entity);

                ref var enemy = ref _enemyPool.Get(entity);
                if (enemy.GameObjectRef != null)
                    _memoryPoolService.UnspawnGameObject(MemoryPoolId.Enemy, enemy.GameObjectRef);

                _world.DelEntity(entity);
            }

            _entitiesToDelete.Clear();
        }

        private void ClearResources()
        {
            CopyFilterToBuffer(_resourceFilter);

            for (int i = 0; i < _entitiesToDelete.Count; i++)
            {
                int entity = _entitiesToDelete[i];
                if (!EcsEntityUtils.IsAlive(_world, entity) || !_resourcePool.Has(entity))
                    continue;

                ref var resource = ref _resourcePool.Get(entity);
                var poolId = resource.ResourceId == ResourceId.HealthSmall
                    ? MemoryPoolId.HealthPickup
                    : MemoryPoolId.AmmoPickup;

                if (resource.GameObjectRef != null)
                    _memoryPoolService.UnspawnGameObject(poolId, resource.GameObjectRef);

                _world.DelEntity(entity);
            }

            _entitiesToDelete.Clear();
        }

        private void ClearProjectiles()
        {
            CopyFilterToBuffer(_projectileFilter);

            for (int i = 0; i < _entitiesToDelete.Count; i++)
            {
                int entity = _entitiesToDelete[i];
                if (!EcsEntityUtils.IsAlive(_world, entity) || !_projectilePool.Has(entity))
                    continue;

                ref var projectile = ref _projectilePool.Get(entity);
                var projectileData = _staticData?.ProjectilesStaticData?.GetProjectileData(projectile.ProjectileId);

                if (projectile.GameObjectRef != null && projectileData != null)
                    _memoryPoolService.UnspawnGameObject(projectileData.PrefabId, projectile.GameObjectRef);
                else if (projectile.GameObjectRef != null)
                    Object.Destroy(projectile.GameObjectRef);

                _world.DelEntity(entity);
            }

            _entitiesToDelete.Clear();
        }

        private void RemoveBehaviourTreeStatesForAgent(int agentEntity)
        {
            _btStatesToDelete.Clear();

            foreach (var stateEntity in _btStateFilter)
            {
                if (_btAgentPool.Get(stateEntity).AgentIndex == agentEntity)
                    _btStatesToDelete.Add(stateEntity);
            }

            for (int i = 0; i < _btStatesToDelete.Count; i++)
            {
                int stateEntity = _btStatesToDelete[i];
                if (EcsEntityUtils.IsAlive(_btStateWorld, stateEntity))
                    _btStateWorld.DelEntity(stateEntity);
            }

            _btStatesToDelete.Clear();
        }

        private void CopyFilterToBuffer(EcsFilter filter)
        {
            _entitiesToDelete.Clear();
            foreach (var entity in filter)
                _entitiesToDelete.Add(entity);
        }

        private static void CreateExitVisual(Transform parent, Vector2 size)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(parent, false);
            visual.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = LevelExitTrigger.ExitSprite;
            renderer.color = new Color(0.1f, 0.9f, 0.35f, 0.45f);
            renderer.sortingOrder = 100;
        }

        private struct LevelSpawnPlan
        {
            public float Difficulty;
            public int TotalEnemies;
            public int TotalWaves;
            public int SpawnedEnemies;
            public int SpawnedWaves;
            public float NextWaveTick;
            public int SpawnPointStride;
        }
    }
}
