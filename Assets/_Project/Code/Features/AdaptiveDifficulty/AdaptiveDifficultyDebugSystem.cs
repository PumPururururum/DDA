using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Behaviours;
using _Project.Code.Core.Abstractions.Contracts;
using UnityEngine;

namespace _Project.Code.Features.AdaptiveDifficulty
{
    /// <summary>
    /// Система управления отладочным оверлеем адаптивной сложности.
    ///
    /// Следует паттерну проекта: чистый C#-класс (IConstruct + ITick + ICleanUp),
    /// создаёт AdaptiveDifficultyDebugView как MonoBehaviour программно.
    ///
    /// Дополнительно читает текущую стадию динамического саундтрека (MusicState)
    /// из DynamicSoundtrackView и передаёт её в оверлей для отображения.
    ///
    /// Зарегистрирован в GameInstaller → BindUi().
    /// Переключение оверлея в игре: клавиша F1.
    /// </summary>
    public sealed class AdaptiveDifficultyDebugSystem : IConstruct, ITick, ICleanUp
    {
        private AdaptiveDifficultyDebugView _view;
        private DynamicSoundtrackView       _soundtrackView;

        // ─── IConstruct ───────────────────────────────────────────────────────

        public void Construct()
        {
            _soundtrackView = Object.FindFirstObjectByType<DynamicSoundtrackView>();
            TryCreateView();
        }

        // ─── ITick ────────────────────────────────────────────────────────────

        public void Tick()
        {
            if (_view == null)
                TryCreateView();

            if (_view == null)
                return;

            var bootstrap = ProjectAdaptiveDifficultyBootstrap.Instance;
            if (bootstrap == null)
                return;

            if (_soundtrackView == null)
                _soundtrackView = Object.FindFirstObjectByType<DynamicSoundtrackView>();

            _view.SetData(
                diffSnap:      bootstrap.GetDifficultySnapshot(),
                statsSnap:     bootstrap.GetStatsSnapshot(),
                currentCombo:  bootstrap.CurrentCombo,
                totalKills:    bootstrap.TotalKillsThisRun,
                musicStateName: _soundtrackView != null ? _soundtrackView.CurrentState.ToString() : "Ambient");
        }

        // ─── ICleanUp ─────────────────────────────────────────────────────────

        public void CleanUp()
        {
            if (_view == null)
                return;

            Object.Destroy(_view.gameObject);
            _view = null;
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────

        private void TryCreateView()
        {
            if (ProjectAdaptiveDifficultyBootstrap.Instance == null)
                return;

            var go = new GameObject("[Debug] Adaptive Difficulty HUD");
            _view  = go.AddComponent<AdaptiveDifficultyDebugView>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _view.Visible = true;
#else
            _view.Visible = false;
#endif

            _view.ToggleKey = KeyCode.F1;
            _view.UiScale   = 1f;
        }
    }
}
