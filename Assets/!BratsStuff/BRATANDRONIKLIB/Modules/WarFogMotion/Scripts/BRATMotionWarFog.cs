using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATMotionWarFog : MonoBehaviour
    {
        [Tooltip("Maximum offset from each direct child RectTransform's starting position, in Canvas units.")]
        public Vector2 amplitude = new(6f, 3f);

        [Min(0.1f)] public float cycleDuration = 28f;
        public bool useUnscaledTime = true;

        private const float FadeInDuration = 2f;
        private const float PhaseStride = 0.618033989f;
        private RectTransform[] _clouds;
        private Vector3[] _basePositions;
        private float[] _phases;
        private float _cycleTime;
        private float _fadeTime;

        private void Awake()
        {
            Transform root = transform;
            int count = root.childCount;
            _clouds = new RectTransform[count];
            _basePositions = new Vector3[count];
            _phases = new float[count];

            for (int i = 0; i < count; i++)
            {
                _clouds[i] = root.GetChild(i) as RectTransform;
                if (_clouds[i] == null) continue;

                _basePositions[i] = _clouds[i].anchoredPosition3D;
                _phases[i] = Mathf.Repeat(i * PhaseStride, 1f) * Mathf.PI * 2f;
            }
        }

        private void OnEnable()
        {
            _cycleTime = 0f;
            _fadeTime = 0f;
            RestorePositions();
        }

        private void LateUpdate()
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float duration = Mathf.Max(0.1f, cycleDuration);
            _cycleTime = Mathf.Repeat(_cycleTime + deltaTime, duration);
            _fadeTime = Mathf.Min(_fadeTime + deltaTime, FadeInDuration);
            float angle = _cycleTime / duration * Mathf.PI * 2f;
            float blend = Mathf.SmoothStep(0f, 1f, _fadeTime / FadeInDuration);

            for (int i = 0; i < _clouds.Length; i++)
            {
                RectTransform cloud = _clouds[i];
                if (cloud == null) continue;

                float wave = Mathf.Sin(angle + _phases[i]) * blend;
                cloud.anchoredPosition3D = _basePositions[i] + new Vector3(
                    amplitude.x * wave,
                    amplitude.y * wave,
                    0f);
            }
        }

        private void OnDisable()
        {
            RestorePositions();
        }

        private void RestorePositions()
        {
            if (_clouds == null) return;

            for (int i = 0; i < _clouds.Length; i++)
                if (_clouds[i] != null)
                    _clouds[i].anchoredPosition3D = _basePositions[i];
        }

        private void OnValidate()
        {
            amplitude.x = Mathf.Max(0f, amplitude.x);
            amplitude.y = Mathf.Max(0f, amplitude.y);
            cycleDuration = Mathf.Max(0.1f, cycleDuration);
        }
    }
}
