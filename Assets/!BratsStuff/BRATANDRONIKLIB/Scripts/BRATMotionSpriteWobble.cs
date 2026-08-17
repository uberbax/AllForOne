using UnityEngine;

[DisallowMultipleComponent]
public sealed class BRATMotionSpriteWobble : MonoBehaviour
{
    [Header("Target")]
    public bool includeChildSpriteRenderers;

    [Header("Position")]
    public Vector2 positionAmplitude = new(0.04f, 0.03f);

    [Header("Rotation")]
    [Min(0f)] public float rotationAmplitude = 0.75f;

    [Header("Scale")]
    [Range(0f, 0.05f)] public float scaleAmplitude = 0.0125f;

    [Header("Timing")]
    [Min(0.01f)] public float frequency = 0.35f;

    private Transform[] _targets;
    private Vector3[] _baseLocalPositions;
    private Quaternion[] _baseLocalRotations;
    private Vector3[] _baseLocalScales;
    private Vector3[] _phases;

    private void Awake()
    {
        CacheTargets();
    }

    private void Update()
    {
        float time = Time.time * frequency;

        for (int i = 0; i < _targets.Length; i++)
        {
            Vector3 phase = _phases[i];
            float x = SignedNoise(time, phase.x);
            float y = SignedNoise(time * 0.91f, phase.y);
            float rotation = SignedNoise(time * 0.73f, phase.z) * rotationAmplitude;
            float scale = 1f + SignedNoise(time * 0.67f, phase.x + phase.z) * scaleAmplitude;

            Transform target = _targets[i];
            target.localPosition = _baseLocalPositions[i] + new Vector3(
                x * positionAmplitude.x,
                y * positionAmplitude.y,
                0f);
            target.localRotation = _baseLocalRotations[i] * Quaternion.Euler(0f, 0f, rotation);
            target.localScale = _baseLocalScales[i] * scale;
        }
    }

    private void OnDisable()
    {
        if (_targets == null)
        {
            return;
        }

        for (int i = 0; i < _targets.Length; i++)
        {
            Transform target = _targets[i];
            if (target == null)
            {
                continue;
            }

            target.localPosition = _baseLocalPositions[i];
            target.localRotation = _baseLocalRotations[i];
            target.localScale = _baseLocalScales[i];
        }
    }

    private void OnValidate()
    {
        positionAmplitude.x = Mathf.Max(0f, positionAmplitude.x);
        positionAmplitude.y = Mathf.Max(0f, positionAmplitude.y);
        rotationAmplitude = Mathf.Max(0f, rotationAmplitude);
        scaleAmplitude = Mathf.Clamp(scaleAmplitude, 0f, 0.05f);
        frequency = Mathf.Max(0.01f, frequency);
    }

    private void CacheTargets()
    {
        if (includeChildSpriteRenderers)
        {
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _targets = new Transform[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                _targets[i] = renderers[i].transform;
            }
        }
        else
        {
            _targets = new[] { transform };
        }

        int targetCount = _targets.Length;
        _baseLocalPositions = new Vector3[targetCount];
        _baseLocalRotations = new Quaternion[targetCount];
        _baseLocalScales = new Vector3[targetCount];
        _phases = new Vector3[targetCount];

        for (int i = 0; i < targetCount; i++)
        {
            Transform target = _targets[i];
            _baseLocalPositions[i] = target.localPosition;
            _baseLocalRotations[i] = target.localRotation;
            _baseLocalScales[i] = target.localScale;

            Vector3 worldPosition = target.position;
            _phases[i] = new Vector3(
                Hash(worldPosition.x, worldPosition.y, 17.17f),
                Hash(worldPosition.y, worldPosition.x, 43.31f),
                Hash(worldPosition.x + worldPosition.y, worldPosition.z, 79.79f));
        }
    }

    private static float SignedNoise(float time, float phase)
    {
        return Mathf.PerlinNoise(time + phase, phase) * 2f - 1f;
    }

    private static float Hash(float a, float b, float salt)
    {
        float value = Mathf.Sin(a * 12.9898f + b * 78.233f + salt) * 43758.5453f;
        return Mathf.Repeat(value, 1f) * 100f;
    }
}
