using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class BRATGUIArrowPulse : MonoBehaviour
{
    [Min(0f)] public float amplitude = 4f;
    [Min(0.1f)] public float period = 2.8f;
    public CanvasGroup edgeGlow;

    private RectTransform arrow;
    private Vector2 restPosition;
    private float restAlpha;
    private float phase;

    private void Awake()
    {
        arrow = (RectTransform)transform;
    }

    private void OnEnable()
    {
        restPosition = arrow.anchoredPosition;
        restAlpha = edgeGlow ? edgeGlow.alpha : 0f;
        phase = 0f;
    }

    private void Update()
    {
        phase = Mathf.Repeat(phase + Time.unscaledDeltaTime / Mathf.Max(period, 0.1f), 1f);
        float wave = Mathf.Sin(phase * (2f * Mathf.PI));
        arrow.anchoredPosition = restPosition + Vector2.up * (amplitude * wave);
        if (edgeGlow)
            edgeGlow.alpha = restAlpha * (1f + 0.18f * wave);
    }

    private void OnDisable()
    {
        arrow.anchoredPosition = restPosition;
        if (edgeGlow)
            edgeGlow.alpha = restAlpha;
    }
}

