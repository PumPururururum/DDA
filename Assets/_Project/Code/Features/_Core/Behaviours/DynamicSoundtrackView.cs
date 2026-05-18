using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class DynamicSoundtrackView : MonoBehaviour
    {
        [Header("Layered soundtrack sources")]
        [SerializeField] private AudioSource _calmLayer;
        [SerializeField] private AudioSource _combatLayer;
        [SerializeField] private AudioSource _dangerLayer;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Min(0.01f)] private float _fadeSpeed = 2.5f;
        [SerializeField] private bool _playLayersOnStart = true;

        private float _targetCalm;
        private float _targetCombat;
        private float _targetDanger;

        private void Awake()
        {
            PrepareLayer(_calmLayer);
            PrepareLayer(_combatLayer);
            PrepareLayer(_dangerLayer);
        }

        private void Start()
        {
            if (!_playLayersOnStart)
                return;

            PlayIfReady(_calmLayer);
            PlayIfReady(_combatLayer);
            PlayIfReady(_dangerLayer);
        }

        private void Update()
        {
            ApplyVolume(_calmLayer, _targetCalm);
            ApplyVolume(_combatLayer, _targetCombat);
            ApplyVolume(_dangerLayer, _targetDanger);
        }

        public void SetIntensity(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);

            _targetCalm = (1f - Mathf.SmoothStep(0.15f, 0.75f, intensity)) * _masterVolume;
            _targetCombat = Mathf.SmoothStep(0.15f, 0.65f, intensity) * (1f - 0.35f * Mathf.SmoothStep(0.8f, 1f, intensity)) * _masterVolume;
            _targetDanger = Mathf.SmoothStep(0.55f, 1f, intensity) * _masterVolume;
        }

        private void ApplyVolume(AudioSource source, float target)
        {
            if (source == null)
                return;

            if (_playLayersOnStart && source.clip != null && !source.isPlaying)
                source.Play();

            source.volume = Mathf.MoveTowards(source.volume, target, _fadeSpeed * Time.deltaTime);
        }

        private static void PrepareLayer(AudioSource source)
        {
            if (source == null)
                return;

            source.loop = true;
            source.playOnAwake = false;
        }

        private static void PlayIfReady(AudioSource source)
        {
            if (source == null || source.clip == null || source.isPlaying)
                return;

            source.Play();
        }
    }
}
