
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(ParticleSystemRenderer))]
public class DimwoodGodRaysStaticArea : MonoBehaviour
{
    [Header("Area")]
    [Min(0.1f)]
    public float radius = 3f;

    [Header("Rays")]
    [Range(1, 40)]
    public int maxRays = 14;

    [Range(0.01f, 1f)]
    public float opacity = 0.18f;

    public Color color = Color.white;

    [Tooltip("Width of one ray")]
    public Vector2 widthRange = new Vector2(0.35f, 1.0f);

    [Tooltip("Length of one ray")]
    public Vector2 lengthRange = new Vector2(2.5f, 5.5f);

    [Header("Fade")]
    public Vector2 lifetimeRange = new Vector2(2.5f, 5f);

    [Tooltip("How long the particle spends fading in")]
    [Range(0.01f, 0.49f)]
    public float fadeInPart = 0.25f;

    [Tooltip("How long the particle spends fading out")]
    [Range(0.01f, 0.49f)]
    public float fadeOutPart = 0.30f;

    [Header("Rotation")]
    [Tooltip("Base ray angle. -45 = down-right")]
    public float angle = -42f;

    [Tooltip("Random angle variation")]
    [Range(0f, 45f)]
    public float angleRandomness = 8f;

    [Header("Spawn")]
    [Tooltip("How many rays appear per second")]
    [Min(0.01f)]
    public float spawnRate = 2.5f;

    private ParticleSystem ps;
    private ParticleSystemRenderer pr;

    private Material runtimeMaterial;
    private Texture2D runtimeTexture;

    private void OnEnable()
    {
        Setup();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)
            return;

        Setup();
    }

    private void Setup()
    {
        ps = GetComponent<ParticleSystem>();
        pr = GetComponent<ParticleSystemRenderer>();

        CreateTextureAndMaterial();

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = Mathf.Max(1, maxRays);

        // IMPORTANT: particles do not move.
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        main.startLifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.1f, lifetimeRange.x),
            Mathf.Max(lifetimeRange.x, lifetimeRange.y)
        );

        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(color.r, color.g, color.b, opacity * 0.8f),
            new Color(color.r, color.g, color.b, opacity)
        );

        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(widthRange.x, widthRange.y);
        main.startSizeY = new ParticleSystem.MinMaxCurve(lengthRange.x, lengthRange.y);
        main.startSizeZ = 1f;

        // Texture is vertical by default.
        float minAngle = -(angle + 90f) - angleRandomness;
        float maxAngle = -(angle + 90f) + angleRandomness;
        main.startRotation = new ParticleSystem.MinMaxCurve(
            Mathf.Deg2Rad * minAngle,
            Mathf.Deg2Rad * maxAngle
        );

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = spawnRate;

        // Spawn randomly inside a circular region.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 1f;
        shape.arc = 360f;

        // No movement/noise modules.
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        var force = ps.forceOverLifetime;
        force.enabled = false;

        var noise = ps.noise;
        noise.enabled = false;

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = false;

        // Fade in -> stay visible -> fade out.
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();

        float fadeOutStart = Mathf.Clamp01(1f - fadeOutPart);

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, fadeInPart),
                new GradientAlphaKey(1f, fadeOutStart),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLifetime.color = gradient;

        // Keep particle size fixed. No drifting/pulsing.
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = false;

        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = false;

        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.alignment = ParticleSystemRenderSpace.View;
        pr.sortMode = ParticleSystemSortMode.Distance;
        pr.material = runtimeMaterial;

        if (Application.isPlaying && !ps.isPlaying)
            ps.Play();
    }

    private void CreateTextureAndMaterial()
    {
        if (runtimeTexture == null)
        {
            const int W = 128;
            const int H = 512;

            runtimeTexture = new Texture2D(
                W,
                H,
                TextureFormat.RGBA32,
                false,
                true
            );

            runtimeTexture.name = "Dimwood_StaticGodRay_Runtime";
            runtimeTexture.wrapMode = TextureWrapMode.Clamp;
            runtimeTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[W * H];

            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);

                // Soft fade at both ends.
                float vertical =
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v * 8f)) *
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - v) * 3f));

                // Slightly stronger near the upper/source side.
                vertical *= Mathf.Lerp(0.55f, 1f, 1f - v);

                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1);
                    float dist = Mathf.Abs(u - 0.5f) * 2f;

                    float horizontal =
                        Mathf.Pow(
                            Mathf.Clamp01(1f - dist),
                            2.5f
                        );

                    float alpha = Mathf.Clamp01(vertical * horizontal);

                    pixels[y * W + x] =
                        new Color(1f, 1f, 1f, alpha);
                }
            }

            runtimeTexture.SetPixels(pixels);
            runtimeTexture.Apply(false, false);
        }

        if (runtimeMaterial == null)
        {
            Shader shader = Shader.Find("Dimwood/GodRaysAdditive");

            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");

            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            runtimeMaterial = new Material(shader);
            runtimeMaterial.name = "Dimwood_StaticGodRays_RuntimeMaterial";

            if (runtimeMaterial.HasProperty("_MainTex"))
                runtimeMaterial.SetTexture("_MainTex", runtimeTexture);

            if (runtimeMaterial.HasProperty("_BaseMap"))
                runtimeMaterial.SetTexture("_BaseMap", runtimeTexture);

            if (runtimeMaterial.HasProperty("_Color"))
                runtimeMaterial.SetColor("_Color", Color.white);

            if (runtimeMaterial.HasProperty("_BaseColor"))
                runtimeMaterial.SetColor("_BaseColor", Color.white);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeMaterial);
            else
                DestroyImmediate(runtimeMaterial);
        }

        if (runtimeTexture != null)
        {
            if (Application.isPlaying)
                Destroy(runtimeTexture);
            else
                DestroyImmediate(runtimeTexture);
        }
    }
}
