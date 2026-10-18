using UnityEngine;

/// <summary>One-shot slash/impact. Re-enable to replay; disable autoDestroy when using a pool.</summary>
public sealed class StylizedHitSlash : MonoBehaviour
{
    [SerializeField] private Transform streaks;
    [SerializeField] private Transform shockwave;
    [SerializeField] private MeshRenderer[] ribbons;
    [SerializeField] private ParticleSystem[] particles;
    [SerializeField] private float duration = 1.15f;
    [SerializeField] private bool autoDestroy = true;
    [SerializeField] private bool faceCamera = true;
    private MaterialPropertyBlock properties;
    private float age;
    private Camera viewingCamera;
    private static readonly int Fade = Shader.PropertyToID("_Fade");
    private static readonly int Erode = Shader.PropertyToID("_Erode");

    private void OnEnable() => Play();

    public void Play()
    {
        age = 0;
        viewingCamera = Camera.main;
        if (particles != null)
            foreach (var system in particles)
                if (system != null) { system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); system.Play(true); }
        ApplyFrame(0);
    }

    private void LateUpdate()
    {
        //if (faceCamera && viewingCamera != null) transform.rotation = viewingCamera.transform.rotation;
        age += Time.deltaTime;
        ApplyFrame(age);
        if (age >= duration)
        {
            if (autoDestroy) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    public void SetPreviewTime(float seconds)
    {
        ApplyFrame(seconds);
        if (particles != null)
            foreach (var system in particles)
                if (system != null) system.Simulate(seconds, true, true, true);
    }

    private void ApplyFrame(float seconds)
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        float opening = Mathf.Clamp01(seconds / .055f);
        float tail = Mathf.Clamp01((seconds - .10f) / .48f);
        if (streaks != null) streaks.localScale = new Vector3(Mathf.Lerp(.45f, 1, opening), Mathf.Lerp(.3f, 1, opening), 1);
        if (shockwave != null) shockwave.localScale = Vector3.one * Mathf.Lerp(.15f, 1.65f, Mathf.Clamp01(seconds / .4f));
        if (ribbons == null) return;
        foreach (var ribbon in ribbons)
        {
            if (ribbon == null) continue;
            properties.Clear();
            bool ring = shockwave != null && ribbon.transform == shockwave;
            properties.SetFloat(Fade, ring ? Mathf.Pow(1 - Mathf.Clamp01(seconds / .4f), 2) : opening * Mathf.Pow(1 - tail, 1.25f));
            properties.SetFloat(Erode, ring ? 0 : tail * .65f);
            ribbon.SetPropertyBlock(properties);
        }
    }
}
