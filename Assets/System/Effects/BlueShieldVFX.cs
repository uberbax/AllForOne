using UnityEngine;

/// <summary>Reusable shield bubble. A zero lifetime keeps it active until FadeOut is called.</summary>
public sealed class BlueShieldVFX : MonoBehaviour
{
    [SerializeField] private MeshRenderer shell;
    [SerializeField, Min(.05f)] private float radius = 1.2f;
    [SerializeField, Range(0, 1)] private float opacity = .14f;
    [SerializeField, Min(0)] private float lifetime;
    [SerializeField, Min(.01f)] private float fadeInDuration = .22f;
    [SerializeField, Min(.01f)] private float fadeOutDuration = .3f;
    [SerializeField] private bool destroyOnFinish = true;
    private MaterialPropertyBlock properties;
    private float elapsed;
    private float fadeStarted;
    private float fadeStartVisibility;
    private bool fading;
    private static readonly int Visibility = Shader.PropertyToID("_Visibility");
    private static readonly int ShieldTime = Shader.PropertyToID("_ShieldTime");
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");

    private void OnEnable() => Play();

    public void Play()
    {
        elapsed = 0; fading = false;
        Apply(0, 0);
    }

    public void FadeOut()
    {
        if (fading) return;
        fading = true;
        fadeStarted = elapsed;
        fadeStartVisibility = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeInDuration)));
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (!fading && lifetime > 0 && elapsed >= lifetime) FadeOut();
        float visibility = fading
            ? fadeStartVisibility * (1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01((elapsed - fadeStarted) / Mathf.Max(.01f, fadeOutDuration))))
            : Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeInDuration)));
        Apply(elapsed, visibility);
        if (fading && elapsed - fadeStarted >= fadeOutDuration)
        {
            if (destroyOnFinish) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    public void SetPreviewTime(float seconds) => Apply(seconds, 1);

    private void Apply(float seconds, float visibility)
    {
        if (shell == null) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        shell.transform.localScale = Vector3.one * (Mathf.Max(.05f, radius) * 2 * Mathf.Lerp(.93f, 1, visibility));
        properties.Clear();
        properties.SetFloat(Visibility, visibility);
        properties.SetFloat(ShieldTime, seconds);
        properties.SetFloat(Opacity, opacity);
        shell.SetPropertyBlock(properties);
    }
}
