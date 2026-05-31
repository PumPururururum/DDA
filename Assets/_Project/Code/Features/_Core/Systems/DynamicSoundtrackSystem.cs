using AdaptiveDifficulty.Runtime;
using _ExampleProject.Code.Features._Core.Behaviours;
using _Project.Code.Core.Abstractions.Contracts;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Systems
{
    /// <summary>
    /// ECS-система динамического саундтрека.
    ///
    /// Каждый кадр читает RuntimeIntensity из ProjectAdaptiveDifficultyBootstrap
    /// и передаёт его в DynamicSoundtrackView. Та самостоятельно решает, нужно ли
    /// переключать стадию musicState, и вызывает FMOD setParameterByName только
    /// при фактической смене стадии.
    ///
    /// Разделение ответственности:
    ///   - DynamicSoundtrackSystem — знает о системе сложности, не знает об FMOD.
    ///   - DynamicSoundtrackView   — знает об FMOD, не знает о системе сложности.
    /// </summary>
    public sealed class DynamicSoundtrackSystem : IConstruct, IInit, ITick, ICleanUp
    {
        private DynamicSoundtrackView _view;

        // ─── IConstruct ───────────────────────────────────────────────────────

        public void Construct()
        {
            _view = Object.FindFirstObjectByType<DynamicSoundtrackView>();

#if UNITY_EDITOR
            if (_view == null)
                Debug.LogWarning("[DynamicSoundtrackSystem] DynamicSoundtrackView не найден на сцене. " +
                                 "Добавьте компонент на GameObject.");
#endif
        }

        // ─── IInit ────────────────────────────────────────────────────────────

        /// <summary>
        /// Запустить музыкальное событие FMOD и установить стадию Intro в начале уровня.
        /// Вызывается GameContext при старте и при рестарте (Init → CleanUp → Init).
        /// </summary>
        public void Init()
        {
            if (_view == null)
                _view = Object.FindFirstObjectByType<DynamicSoundtrackView>();

            _view?.StartLevel();
        }

        // ─── ITick ────────────────────────────────────────────────────────────

        /// <summary>
        /// Каждый кадр: читаем RuntimeIntensity и обновляем стадию саундтрека.
        /// Вызов SetIntensity внутри View выполнит setParameterByName только при
        /// фактической смене стадии — лишних вызовов FMOD API нет.
        /// </summary>
        public void Tick()
        {
            if (_view == null)
            {
                _view = Object.FindFirstObjectByType<DynamicSoundtrackView>();
                if (_view == null)
                    return;
            }

            var bootstrap = ProjectAdaptiveDifficultyBootstrap.Instance;
            float intensity = bootstrap != null ? bootstrap.CurrentIntensity : 0f;
            _view.SetIntensity(intensity);
        }

        // ─── ICleanUp ─────────────────────────────────────────────────────────

        /// <summary>
        /// Сбросить интенсивность при выходе из уровня / рестарте.
        /// Возврат в Ambient через нулевую интенсивность.
        /// </summary>
        public void CleanUp()
        {
            if (_view != null)
                _view.SetIntensity(0f);
        }
    }
}
