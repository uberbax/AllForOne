using UnityEngine;

[DisallowMultipleComponent]
public sealed class BRATMotionSpriteBreathing : MonoBehaviour
{
    [Header("Breathing")]
    [Min(1f)] public float breathsPerMinute = 14f;
    [Range(0f, 0.1f)] public float horizontalScale = 0.008f;
    [Range(0f, 0.1f)] public float verticalScale = 0.018f;

    private Vector3 baseScale;
    private float elapsed;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        var phase = elapsed * breathsPerMinute * Mathf.PI * 2f / 60f;
        var breath = Mathf.Sin(phase);
        transform.localScale = new Vector3(
            baseScale.x * (1f + breath * horizontalScale),
            baseScale.y * (1f + breath * verticalScale),
            baseScale.z);
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }

    private void OnValidate()
    {
        breathsPerMinute = Mathf.Max(1f, breathsPerMinute);
        horizontalScale = Mathf.Clamp(horizontalScale, 0f, 0.1f);
        verticalScale = Mathf.Clamp(verticalScale, 0f, 0.1f);
    }
}
