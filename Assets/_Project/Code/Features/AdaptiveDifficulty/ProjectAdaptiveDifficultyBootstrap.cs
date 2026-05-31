using _Project.Code.Features.AdaptiveDifficulty;
using _Project.Code.Features.Test;
using UnityEngine;

namespace AdaptiveDifficulty.Runtime
{
    /// <summary>
    /// Точка входа системы адаптивной сложности в проекте.
    /// MonoBehaviour-синглтон, связывающий plugin-контроллер с ECS-телеметрией.
    ///
    /// Размещается на отдельном GameObject в сцене.
    /// Не зависит от конкретных игровых объектов — получает данные через Report* методы.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class ProjectAdaptiveDifficultyBootstrap : MonoBehaviour
    {
        public static ProjectAdaptiveDifficultyBootstrap Instance { get; private set; }

        [SerializeField] private AdaptiveDifficultySettings _settings;
        [SerializeField] private bool _dontDestroyOnLoad = true;

        private AdaptiveEcsTelemetrySource _telemetrySource;
        private AdaptiveDifficultyController _controller;

        /// <summary>
        /// Флаг паузы сбора телеметрии.
        /// Устанавливается из GameLoopSystem когда все враги убиты и выход открыт,
        /// чтобы время ожидания перехода не влияло на расчёт сложности.
        /// Сбрасывается автоматически при CommitLevel() и ResetRunStats().
        /// </summary>
        private bool _collectionPaused;

        // ─── Публичные свойства ───────────────────────────────────────────────

        /// <summary>Текущее значение сложности D ∈ [0, 1]. Обновляется между уровнями.</summary>
        public float CurrentDifficulty => _controller != null ? _controller.CurrentDifficulty : 0f;

        /// <summary>
        /// Текущая интенсивность боя ∈ [0, 1]. Обновляется каждый кадр.
        /// Используется для управления слоями динамического саундтрека.
        /// </summary>
        public float CurrentIntensity => _controller != null ? _controller.CurrentIntensity : 0f;

        /// <summary>Текущая длина активного комбо (сбрасывается при паузе > ComboResetDelay).</summary>
        public int CurrentCombo => _telemetrySource?.CurrentCombo ?? 0;

        /// <summary>Суммарное количество убийств с начала текущего запуска.</summary>
        public int TotalKillsThisRun => _telemetrySource?.TotalKillsThisRun ?? 0;

        // ─── MonoBehaviour ────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (_settings == null)
            {
                Debug.LogError("[AdaptiveDifficulty] Settings asset is not assigned on " + name);
                enabled = false;
                return;
            }

            _telemetrySource = new AdaptiveEcsTelemetrySource(_settings);
            _controller      = new AdaptiveDifficultyController(_settings, _telemetrySource);
        }

        private void Update()
        {
            if (!_collectionPaused)
                _controller?.Tick(Time.deltaTime);
        }

        // ─── Публичный API ────────────────────────────────────────────────────

        /// <summary>
        /// Зафиксировать результаты уровня и обновить D.
        /// Вызывается из GameLoopSystem при переходе на следующий уровень.
        /// Автоматически снимает паузу сбора телеметрии.
        /// </summary>
        public void CommitLevel()
        {
            _controller?.CommitLevel();
            _collectionPaused = false; // начинаем сбор заново для следующего уровня
        }

        /// <summary>
        /// Остановить сбор телеметрии.
        /// Вызывается из GameLoopSystem при открытии выхода с уровня (все враги убиты).
        /// Гарантирует, что время ожидания перехода не искажает метрики сложности.
        /// </summary>
        public void PauseCollection()
        {
            _collectionPaused = true;
        }

        /// <summary>Возобновить сбор телеметрии (например, при рестарте).</summary>
        public void ResumeCollection()
        {
            _collectionPaused = false;
        }

        /// <summary>Сбросить статистику при рестарте игры (смерть и начало с 1 уровня).</summary>
        public void ResetRunStats()
        {
            _telemetrySource?.ResetRunStats();
            _collectionPaused = false; // при рестарте сбор всегда активен
        }

        // ─── Отчёты из ECS-систем ─────────────────────────────────────────────

        public void ReportShot(CombatTeamId shooterTeam)
            => _telemetrySource?.ReportShot(shooterTeam);

        public void ReportDamage(CombatTeamId sourceTeam, CombatTeamId targetTeam, int damage)
            => _telemetrySource?.ReportDamage(sourceTeam, targetTeam, damage);

        public void ReportEnemyKilled()
            => _telemetrySource?.ReportEnemyKilled();

        public void ReportPickup(AdaptivePickupType pickupType)
            => _telemetrySource?.ReportPickup(pickupType);

        // ─── Снапшоты для внешних систем ─────────────────────────────────────

        public AdaptiveDifficultySnapshot GetDifficultySnapshot()
            => _controller?.GetSnapshot() ?? default;

        public AdaptiveStatsSnapshot GetStatsSnapshot()
            => _controller?.GetStatsSnapshot() ?? default;
    }
}
