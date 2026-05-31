using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using _ExampleProject.Code.Features._Core.Audio;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    /// <summary>
    /// MonoBehaviour-фасад динамического саундтрека.
    ///
    /// Инкапсулирует работу с музыкальным событием FMOD: создаёт EventInstance,
    /// запускает его воспроизведение и управляет стадией саундтрека через
    /// параметр musicState. Игровой код не управляет громкостью отдельных слоёв
    /// напрямую — он лишь передаёт текущее значение RuntimeIntensity, а фасад
    /// сам выбирает дискретную стадию с гистерезисом и вызывает setParameterByName.
    ///
    /// Стадии и пороги перехода (задаются в инспекторе):
    ///   RuntimeIntensity в [0,   AmbientToLeadThreshold)  → Ambient
    ///   RuntimeIntensity в [A,   LeadToDrumsThreshold)    → Lead
    ///   RuntimeIntensity в [L,   1]                       → DrumsBass
    ///   При старте уровня принудительно устанавливается Intro.
    ///
    /// Гистерезис: переход на более высокую стадию происходит при достижении
    /// верхнего порога, возврат — при снижении ниже (порог − HysteresisMargin).
    /// Это исключает «дребезг» при колебаниях интенсивности вблизи границы.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DynamicSoundtrackView : MonoBehaviour
    {
        // ─── FMOD ─────────────────────────────────────────────────────────────

        [Header("FMOD")]
        [Tooltip("Путь к музыкальному событию FMOD, например: event:/Music/GameplayMusic")]


        [SerializeField] private string _musicStateParam = "musicState";

        // ─── Пороги переключения стадий ───────────────────────────────────────

        [Header("Пороги стадий")]
        [Tooltip("RuntimeIntensity, при котором происходит переход Ambient → Lead.\n" +
                 "Рекомендуется: 0.33")]
        [Range(0f, 1f)] public float AmbientToLeadThreshold  = 0.33f;

        [Tooltip("RuntimeIntensity, при котором происходит переход Lead → DrumsBass.\n" +
                 "Рекомендуется: 0.66")]
        [Range(0f, 1f)] public float LeadToDrumsThreshold    = 0.66f;

        [Tooltip("Ширина зоны гистерезиса. Возврат на предыдущую стадию происходит\n" +
                 "при снижении ниже (порог - HysteresisMargin).\n" +
                 "Рекомендуется: 0.05–0.10")]
        [Range(0f, 0.3f)] public float HysteresisMargin      = 0.07f;

        // ─── Приватные поля ───────────────────────────────────────────────────

        private EventInstance _eventInstance;
        private MusicState    _currentState = MusicState.Intro;
        private bool          _isStarted;

        // ─── MonoBehaviour ────────────────────────────────────────────────────

        private void Awake()
        {
            _eventInstance = RuntimeManager.CreateInstance("event:/Music");
        }

        private void OnDestroy()
        {
            if (_eventInstance.isValid())
            {
                _eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                _eventInstance.release();
            }
        }

        // ─── Публичный API ────────────────────────────────────────────────────

        /// <summary>Текущая активная стадия саундтрека. Используется в дебаг-оверлее.</summary>
        public MusicState CurrentState => _currentState;

        /// <summary>
        /// Запустить воспроизведение музыкального события и установить стадию Intro.
        /// Вызывается из DynamicSoundtrackSystem при инициализации уровня.
        /// </summary>
        public void StartLevel()
        {
            // Защита: если Awake не успел создать экземпляр — создаём здесь
            if (!_eventInstance.isValid())
                _eventInstance = RuntimeManager.CreateInstance("event:/Music");

            if (!_eventInstance.isValid())
            {
                Debug.LogError("[DynamicSoundtrackView] FMOD event:/Music не найден. Проверь имя события в FMOD Studio.");
                return;
            }

            _currentState = MusicState.Intro;
            ApplyState(_currentState);

            if (!_isStarted)
            {
                _eventInstance.start();
                _isStarted = true;
            }
        }

        /// <summary>
        /// Обновить музыкальное состояние на основе текущего значения RuntimeIntensity.
        /// Вызывается из DynamicSoundtrackSystem каждый кадр.
        ///
        /// Переключение выполняется только при фактическом изменении стадии,
        /// чтобы не вызывать setParameterByName на каждом кадре без нужды.
        /// Гистерезис предотвращает частые переключения при колебаниях вблизи порога.
        /// </summary>
        /// <param name="runtimeIntensity">Сглаженная интенсивность ∈ [0, 1].</param>
        public void SetIntensity(float runtimeIntensity)
        {
            if (!_eventInstance.isValid())
                return;

            MusicState target = ComputeTargetState(runtimeIntensity);

            if (target == _currentState)
                return;

            _currentState = target;
            ApplyState(_currentState);
        }

        /// <summary>
        /// Принудительно остановить музыку (например, при завершении уровня).
        /// </summary>
        public void Stop()
        {
            if (_eventInstance.isValid())
                _eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        // ─── Приватные методы ─────────────────────────────────────────────────

        /// <summary>
        /// Вычислить целевую стадию с учётом гистерезиса.
        ///
        /// Повышение стадии: при достижении верхнего порога.
        /// Понижение стадии: при снижении ниже (порог - HysteresisMargin).
        /// Стадия Intro сбрасывается в Ambient при первом ненулевом вызове SetIntensity.
        /// </summary>
        private MusicState ComputeTargetState(float intensity)
        {
            // Intro → Ambient при первом обновлении интенсивности
            if (_currentState == MusicState.Intro)
                return MusicState.Ambient;

            float hysteresis = HysteresisMargin;

            switch (_currentState)
            {
                case MusicState.Ambient:
                    if (intensity >= AmbientToLeadThreshold)
                        return MusicState.Lead;
                    break;

                case MusicState.Lead:
                    if (intensity >= LeadToDrumsThreshold)
                        return MusicState.DrumsBass;
                    if (intensity < AmbientToLeadThreshold - hysteresis)
                        return MusicState.Ambient;
                    break;

                case MusicState.DrumsBass:
                    if (intensity < LeadToDrumsThreshold - hysteresis)
                        return MusicState.Lead;
                    break;
            }

            return _currentState;
        }

        /// <summary>
        /// Передать числовое значение стадии в FMOD через setParameterByName.
        /// Плавность перехода между слоями обеспечивается средствами FMOD Studio
        /// (времена нарастания/затухания задаются в проекте FMOD, не в коде).
        /// </summary>
        private void ApplyState(MusicState state)
        {
            _eventInstance.setParameterByName(_musicStateParam, (float)state);
        }
    }
}

