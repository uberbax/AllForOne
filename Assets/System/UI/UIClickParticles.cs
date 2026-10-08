using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Attach to a Canvas to emit UI particles on mouse clicks and touches.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class UIClickParticles : MonoBehaviour
{
    [SerializeField] private bool emitOnClick = true;
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Color particleColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField, Range(1, 64)] private int particlesPerClick = 12;
    [SerializeField, Range(1, 512)] private int maxParticles = 128;
    [SerializeField] private Vector2 lifetime = new Vector2(0.3f, 0.6f);
    [SerializeField] private Vector2 speed = new Vector2(90f, 260f);
    [SerializeField] private Vector2 size = new Vector2(6f, 14f);
    [SerializeField] private Vector2 gravity = new Vector2(0f, -250f);
    [SerializeField] private bool useUnscaledTime = true;

    private Canvas canvas;
    private RectTransform layer;
    private readonly List<Particle> active = new List<Particle>();
    private readonly Queue<Particle> pool = new Queue<Particle>();

    private sealed class Particle
    {
        public Image image;
        public RectTransform rect;
        public Vector2 velocity;
        public float age, lifetime, size, rotation, angularSpeed;
        public Color color;
    }

    private void Awake() => canvas = GetComponent<Canvas>();

    private void Update()
    {
        if (canvas == null || !canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled)
        { Clear(); return; }
        float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var particle = active[i];
            particle.age += delta;
            if (particle.age >= particle.lifetime)
            {
                Recycle(particle);
                active.RemoveAt(i);
                continue;
            }
            particle.velocity += gravity * delta;
            particle.rect.anchoredPosition += particle.velocity * delta;
            particle.rotation += particle.angularSpeed * delta;
            particle.rect.localRotation = Quaternion.Euler(0f, 0f, particle.rotation);
            float remaining = 1f - particle.age / particle.lifetime;
            particle.rect.sizeDelta = Vector2.one * (particle.size * remaining);
            var color = particle.color;
            color.a *= remaining;
            particle.image.color = color;
        }
        if (!emitOnClick) return;
        // Touches can emulate mouse clicks: use only one input source per frame.
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began) Spawn(touch.position);
            }
        }
        else if (Input.GetMouseButtonDown(0)) Spawn(Input.mousePosition);
    }

    /// <summary>Emit at a screen position in pixels. Disable emitOnClick for custom input.</summary>
    public void Spawn(Vector2 screenPosition)
    {
        if (!isActiveAndEnabled) return;
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (!canvas.isActiveAndEnabled || !canvas.rootCanvas.isActiveAndEnabled) return;
        var rootCanvas = canvas.rootCanvas;
        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && camera == null) camera = Camera.main;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && camera == null) return;
        EnsureLayer();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screenPosition, camera, out var position) ||
            !layer.rect.Contains(position)) return;
        layer.SetAsLastSibling();
        int count = Mathf.Min(particlesPerClick, maxParticles - active.Count);
        for (int i = 0; i < count; i++)
        {
            var particle = pool.Count > 0 ? pool.Dequeue() : CreateParticle();
            float angle = Random.Range(0f, Mathf.PI * 2f);
            particle.velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(speed.x, speed.y);
            particle.age = 0f;
            particle.lifetime = Mathf.Max(0.01f, Random.Range(lifetime.x, lifetime.y));
            particle.size = Random.Range(size.x, size.y);
            particle.rotation = Random.Range(0f, 360f);
            particle.angularSpeed = Random.Range(-180f, 180f);
            particle.color = particleColor;
            particle.image.sprite = particleSprite;
            particle.image.color = particle.color;
            particle.rect.anchoredPosition = position;
            particle.rect.sizeDelta = Vector2.one * particle.size;
            particle.rect.localRotation = Quaternion.Euler(0f, 0f, particle.rotation);
            particle.image.gameObject.SetActive(true);
            active.Add(particle);
        }
    }

    private void EnsureLayer()
    {
        if (layer != null) return;
        var go = new GameObject("Click Particles", typeof(RectTransform));
        go.layer = gameObject.layer;
        layer = go.GetComponent<RectTransform>();
        layer.SetParent(canvas.transform, false);
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
    }

    private Particle CreateParticle()
    {
        var go = new GameObject("Particle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.layer = gameObject.layer;
        go.transform.SetParent(layer, false);
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        return new Particle { image = image, rect = image.rectTransform };
    }

    private void Recycle(Particle particle)
    {
        particle.image.gameObject.SetActive(false);
        pool.Enqueue(particle);
    }

    public void Clear()
    {
        foreach (var particle in active) Recycle(particle);
        active.Clear();
    }

    private void OnDisable() => Clear();

    private void OnDestroy()
    {
        if (layer != null) Destroy(layer.gameObject);
        active.Clear();
        pool.Clear();
    }

    private void OnValidate()
    {
        particlesPerClick = Mathf.Clamp(particlesPerClick, 1, 64);
        maxParticles = Mathf.Clamp(maxParticles, 1, 512);
        lifetime.x = Mathf.Max(0.01f, lifetime.x);
        lifetime.y = Mathf.Max(lifetime.x, lifetime.y);
        speed.x = Mathf.Max(0f, speed.x);
        speed.y = Mathf.Max(speed.x, speed.y);
        size.x = Mathf.Max(0.1f, size.x);
        size.y = Mathf.Max(size.x, size.y);
    }
}
