using System;
using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB
{
    /// <summary>UI rain with image-space collision contours. No world physics or per-drop objects.</summary>
    [AddComponentMenu("BRATANDRONIKLIB/GUI/Rain")]
    [DisallowMultipleComponent]
    public sealed class BRATGUIRain : MaskableGraphic
    {
        [Serializable]
        public sealed class Surface
        {
            public string name = "Surface";
            [Tooltip("Image UV coordinates, bottom-left = (0,0). An open polyline along the exposed surface.")]
            public Vector2[] points = Array.Empty<Vector2>();
            [Range(0.2f, 1.5f)] public float bounce = 1f;
            [Tooltip("Enable for the floor: foreground drops pass characters but still splash on this surface.")]
            [SerializeField] private bool collideWithForeground;
            internal bool CollideWithForeground => collideWithForeground;
            [Tooltip("Fraction of drops in this depth layer. Keep 1 for solid silhouettes; lower for perspective steps.")]
            [SerializeField, Range(0, 1)] private float hitFraction = 1f;
            internal float HitFraction => hitFraction;
        }

        [Header("Image and collision contours")]
        [SerializeField] private Image background;
        [SerializeField] private Surface[] surfaces = Array.Empty<Surface>();
        [Header("Rain (in source-image pixels)")]
        [SerializeField, Range(0, 400)] private float dropsPerSecond = 170f;
        [SerializeField] private Vector2 fallSpeed = new Vector2(720, 1050);
        [SerializeField] private float wind = -155f;
        [SerializeField] private Vector2 streakLength = new Vector2(13, 26);
        [SerializeField, Range(0.3f, 3)] private float streakWidth = 1.1f;
        [SerializeField, Range(0, 1)] private float foregroundFraction = 0.18f;
        [Header("Splashes")]
        [SerializeField, Range(2, 6)] private int splashCount = 4;
        [SerializeField, Range(20, 220)] private float splashSpeed = 105f;
        [SerializeField, Range(0.1f, 0.6f)] private float splashLifetime = 0.28f;
        [SerializeField, Min(1)] private float gravity = 580f;
        [SerializeField, Range(1, 3)] private float splashSize = 1f;
        [SerializeField, Range(1, 2)] private float splashBrightness = 1f;

        private const int MaxDrops = 640;
        private const int MaxSplashes = 1024;
        private struct Drop
        {
            public Vector2 position, velocity;
            public float length, opacity;
            public bool foreground;
        }
        private struct Splash
        {
            public Vector2 position, velocity;
            public float age, lifetime, opacity, size;
        }
        private Drop[] _drops;
        private Splash[] _splashes;
        private int _dropCount, _splashCount;
        private float _emission;
        private Vector2 _imageSize;
        private RectTransform _backgroundRect;
        private Image _cachedBackground;
        private System.Random _random;
        private bool _initialized;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            _initialized = false;
        }

        protected override void OnDisable()
        {
            _dropCount = _splashCount = 0;
            _emission = 0;
            _initialized = false;
            base.OnDisable();
        }

        private bool Initialize()
        {
            if (background == null || background.sprite == null) return false;
            if (_drops == null) _drops = new Drop[MaxDrops];
            if (_splashes == null) _splashes = new Splash[MaxSplashes];
            _random = new System.Random(7937);
            _backgroundRect = background.rectTransform;
            _cachedBackground = background;
            _imageSize = background.sprite.rect.size;
            _dropCount = _splashCount = 0;
            _emission = 0;
            _initialized = true;
            // Start with rain already spread across the image; never spawn inside a character.
            for (int i = 0; i < 100; i++) Simulate(1f / 60f);
            return true;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (background == null || background.sprite == null)
            {
                if (_initialized) { _initialized = false; SetVerticesDirty(); }
                return;
            }
            if ((!_initialized || _cachedBackground != background || _imageSize != background.sprite.rect.size) && !Initialize()) return;
            // Unscaled time keeps loading/menu rain alive at timeScale = 0.
            float remaining = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            while (remaining > 0f)
            {
                float step = Mathf.Min(remaining, 1f / 60f);
                Simulate(step);
                remaining -= step;
            }
            SetVerticesDirty();
        }

        private float Random(float min, float max) => Mathf.Lerp(min, max, (float)_random.NextDouble());

        private void Simulate(float dt)
        {
            for (int i = _splashCount - 1; i >= 0; i--)
            {
                Splash s = _splashes[i];
                s.age += dt;
                if (s.age >= s.lifetime) { _splashes[i] = _splashes[--_splashCount]; continue; }
                s.velocity.y -= gravity * dt;
                s.position += s.velocity * dt;
                _splashes[i] = s;
            }
            for (int i = _dropCount - 1; i >= 0; i--)
            {
                Drop d = _drops[i];
                Vector2 next = d.position + d.velocity * dt;
                if (Hit(d.position, next, d.foreground, out Vector2 hit, out Vector2 normal, out float bounce))
                {
                    EmitSplash(hit, normal, bounce, d.opacity);
                    _drops[i] = _drops[--_dropCount];
                    continue;
                }
                if (next.y < -40 || next.x < -300 || next.x > _imageSize.x + 300)
                { _drops[i] = _drops[--_dropCount]; continue; }
                d.position = next;
                _drops[i] = d;
            }
            _emission += Mathf.Clamp(dropsPerSecond, 0, 400) * dt;
            int count = Mathf.FloorToInt(_emission);
            _emission -= count;
            float extra = Mathf.Abs(wind) * _imageSize.y / Mathf.Max(100, fallSpeed.x);
            for (int i = 0; i < count && _dropCount < MaxDrops; i++)
            {
                bool front = Random(0, 1) < foregroundFraction;
                _drops[_dropCount++] = new Drop
                {
                    position = new Vector2(Random(wind > 0 ? -extra : 0, _imageSize.x + (wind < 0 ? extra : 0)), _imageSize.y + 30),
                    velocity = new Vector2(wind * Random(.85f, 1.15f), -Random(Mathf.Max(100, fallSpeed.x), Mathf.Max(100, fallSpeed.y))),
                    length = Random(streakLength.x, streakLength.y) * (front ? 1.25f : 1f),
                    opacity = Random(.45f, 1f) * (front ? .6f : 1f), foreground = front
                };
            }
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private bool Hit(Vector2 from, Vector2 to, bool foreground, out Vector2 hit, out Vector2 normal, out float bounce)
        {
            hit = normal = Vector2.zero; bounce = 1;
            float nearest = float.PositiveInfinity;
            Vector2 travel = to - from;
            if (surfaces == null) return false;
            for (int s = 0; s < surfaces.Length; s++)
            {
                Surface surface = surfaces[s];
                if (surface == null || surface.points == null) continue;
                if (foreground && !surface.CollideWithForeground) continue;
                for (int i = 1; i < surface.points.Length; i++)
                {
                    Vector2 a = Vector2.Scale(surface.points[i - 1], _imageSize);
                    Vector2 edge = Vector2.Scale(surface.points[i], _imageSize) - a;
                    float denominator = Cross(travel, edge);
                    if (Mathf.Abs(denominator) < .00001f) continue;
                    float t = Cross(a - from, edge) / denominator;
                    float u = Cross(a - from, travel) / denominator;
                    if (t < 0 || t > 1 || u < 0 || u > 1 || t >= nearest) continue;
                    Vector2 n = new Vector2(-edge.y, edge.x).normalized;
                    if (n.y < 0) n = -n;
                    if (Vector2.Dot(travel, n) >= 0) continue;
                    if (surface.HitFraction < 1f && Random(0, 1) >= surface.HitFraction) continue;
                    nearest = t; hit = from + travel * t; normal = n; bounce = surface.bounce;
                }
            }
            return !float.IsPositiveInfinity(nearest);
        }

        private void EmitSplash(Vector2 hit, Vector2 normal, float bounce, float opacity)
        {
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            for (int i = 0; i < splashCount && _splashCount < MaxSplashes; i++)
            {
                // Uneven fan of slender droplets instead of an equally sized round spray.
                float lateral = Mathf.Lerp(-1.1f, 1.1f, (i + Random(.1f, .9f)) / splashCount);
                Vector2 velocity = normal * Random(.4f, 1.1f) + tangent * lateral;
                velocity *= splashSpeed * bounce;
                velocity.y = Mathf.Max(25, velocity.y);
                _splashes[_splashCount++] = new Splash { position = hit + normal, velocity = velocity,
                    lifetime = splashLifetime * Random(.7f, 1.3f), opacity = opacity, age = 0, size = Random(.65f, 1.25f) };
            }
        }

        private Rect ImageRect()
        {
            Rect r = background.GetPixelAdjustedRect();
            if (background.preserveAspect && _imageSize.y > 0)
            {
                float ratio = _imageSize.x / _imageSize.y;
                if (ratio > r.width / r.height)
                { float h = r.width / ratio; r.y += (r.height - h) * _backgroundRect.pivot.y; r.height = h; }
                else { float w = r.height * ratio; r.x += (r.width - w) * _backgroundRect.pivot.x; r.width = w; }
            }
            return r;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!_initialized || background == null || _backgroundRect == null || !background.isActiveAndEnabled) return;
            Rect r = ImageRect();
            Matrix4x4 matrix = rectTransform.worldToLocalMatrix * _backgroundRect.localToWorldMatrix;
            // Compose image-pixel -> own UI-local coordinates, including preserveAspect and background motion.
            matrix = matrix * Matrix4x4.TRS(new Vector3(r.x, r.y), Quaternion.identity,
                new Vector3(r.width / _imageSize.x, r.height / _imageSize.y, 1));
            for (int i = 0; i < _dropCount; i++)
            {
                Drop d = _drops[i];
                if (d.position.x < 0 || d.position.x > _imageSize.x || d.position.y > _imageSize.y) continue;
                Streak(vh, matrix, d.position - d.velocity.normalized * d.length, d.position, streakWidth, d.opacity);
            }
            for (int i = 0; i < _splashCount; i++)
            {
                Splash s = _splashes[i];
                float fade = 1 - s.age / s.lifetime;
                float length = Mathf.Clamp(s.velocity.magnitude * .045f, 3f, 10f) * splashSize * s.size;
                Vector2 tail = s.position - s.velocity.normalized * length;
                float width = streakWidth * .48f * Mathf.Sqrt(splashSize) * s.size;
                Streak(vh, matrix, tail, s.position, width, s.opacity * fade * 1.5f, splashBrightness);
            }
        }

        private void Streak(VertexHelper vh, Matrix4x4 matrix, Vector2 tail, Vector2 head, float width, float opacity, float brightness = 1f)
        {
            Vector2 d = head - tail;
            if (d.sqrMagnitude < .00001f) return;
            Vector2 side = new Vector2(-d.y, d.x).normalized * width;
            Color c = color; c.a = Mathf.Clamp01(c.a * Mathf.Clamp01(opacity) * brightness);
            Color clear = c; clear.a = 0;
            Color faint = c; faint.a *= .12f;
            int n = vh.currentVertCount;
            vh.AddVert(matrix.MultiplyPoint3x4(tail - side * .45f), clear, Vector2.zero);
            vh.AddVert(matrix.MultiplyPoint3x4(tail), faint, Vector2.zero);
            vh.AddVert(matrix.MultiplyPoint3x4(tail + side * .45f), clear, Vector2.zero);
            vh.AddVert(matrix.MultiplyPoint3x4(head - side), clear, Vector2.zero);
            vh.AddVert(matrix.MultiplyPoint3x4(head), c, Vector2.zero);
            vh.AddVert(matrix.MultiplyPoint3x4(head + side), clear, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 4); vh.AddTriangle(n, n + 4, n + 3);
            vh.AddTriangle(n + 1, n + 2, n + 5); vh.AddTriangle(n + 1, n + 5, n + 4);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            raycastTarget = false;
            fallSpeed.x = Mathf.Max(100, fallSpeed.x); fallSpeed.y = Mathf.Max(fallSpeed.x, fallSpeed.y);
            streakLength.x = Mathf.Max(1, streakLength.x); streakLength.y = Mathf.Max(streakLength.x, streakLength.y);
            _initialized = false;
        }
#endif
    }
}
