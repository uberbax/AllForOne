using UnityEngine;
using UnityEngine.UI;

namespace BRATANDRONIKLIB
{
    /// <summary>Occasional branching lightning in explicit image-space sky regions.</summary>
    [AddComponentMenu("BRATANDRONIKLIB/GUI/Lightning")]
    [DisallowMultipleComponent]
    public sealed class BRATGUILightning : MaskableGraphic
    {
        [SerializeField] private Image background;
        [Tooltip("Safe sky regions in background UV coordinates, bottom-left origin.")]
        [SerializeField] private Rect[] skyRegions = { new Rect(.445f, .935f, .06f, .065f), new Rect(.72f, .92f, .035f, .08f) };
        [SerializeField] private Vector2 interval = new Vector2(5, 9);
        [SerializeField, Range(.2f, 1)] private float duration = .42f;
        [SerializeField, Range(.4f, 3)] private float width = 1.2f;
        private readonly Vector2[] _bolt = new Vector2[13];
        private readonly Vector2[] _branch = new Vector2[6];
        private readonly Vector3[] _canvasCorners = new Vector3[4];
        private System.Random _random;
        private float _wait, _age, _strength;
        private Rect _region;

        protected override void Awake() { base.Awake(); raycastTarget = false; }
        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;
            if (_random == null) _random = new System.Random();
            _wait = 2f; _age = duration; _strength = 0;
        }
        protected override void OnDisable()
        {
            _strength = 0; _age = duration;
            base.OnDisable();
        }
        private float Random(float min, float max) => Mathf.Lerp(min, max, (float)_random.NextDouble());
        private void Update()
        {
            if (!Application.isPlaying) return;
            if (background == null || background.sprite == null || skyRegions == null || skyRegions.Length == 0)
            {
                if (_strength > 0) { _strength = 0; SetVerticesDirty(); }
                return;
            }
            Tick(Mathf.Min(Time.unscaledDeltaTime, .1f));
        }
        private void Tick(float dt)
        {
            if (_age >= duration)
            {
                _wait -= dt;
                if (_wait > 0) return;
                Strike();
            }
            _age += dt;
            float t = _age / Mathf.Max(.01f, duration);
            // First discharge, short pause, weaker return stroke, then fading cloud light.
            _strength = t < .2f ? 1f - t * 2f : t < .34f ? .06f : t < .52f ? .75f : Mathf.Max(0, (1 - t) * .5f);
            SetVerticesDirty();
        }
        private void Strike()
        {
            _region = skyRegions[_random.Next(skyRegions.Length)];
            _age = 0;
            _wait = Random(Mathf.Max(1, interval.x), Mathf.Max(1, interval.y));
            bool flip = _random.Next(2) == 0;
            for (int i = 0; i < _bolt.Length; i++)
            {
                float t = i / (float)(_bolt.Length - 1);
                float x = Mathf.Clamp01(Mathf.Lerp(.04f, .96f, t) + (i == 0 ? 0 : Random(-.06f, .06f)));
                float y = Mathf.Clamp01(Mathf.Lerp(.94f, .1f, t) + Random(-.13f, .13f));
                _bolt[i] = Point(flip ? 1 - x : x, y);
            }
            _branch[0] = _bolt[5];
            Vector2 end = Point(flip ? .16f : .84f, .83f);
            for (int i = 1; i < _branch.Length; i++)
            {
                float t = i / (float)(_branch.Length - 1);
                Vector2 p = Vector2.Lerp(_branch[0], end, t);
                p.y += Random(-.06f, .06f) * _region.height;
                _branch[i] = p;
            }
        }
        private Vector2 Point(float x, float y) => new Vector2(_region.x + x * _region.width, _region.y + y * _region.height);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_strength <= 0 || background == null || background.sprite == null || !background.isActiveAndEnabled) return;
            Vector2 size = background.sprite.rect.size;
            Rect r = background.GetPixelAdjustedRect();
            if (background.preserveAspect)
            {
                float ratio = size.x / size.y;
                if (ratio > r.width / r.height) { float h = r.width / ratio; r.y += (r.height - h) * background.rectTransform.pivot.y; r.height = h; }
                else { float w = r.height * ratio; r.x += (r.width - w) * background.rectTransform.pivot.x; r.width = w; }
            }
            // Extend the connected main bolt beyond the visible canvas top, not merely
            // the selected region. Re-evaluate on mesh rebuilds to follow background motion
            // and aspect-ratio changes without starting a new discharge.
            if (r.height <= 0 || size.y <= 0) return;
            float top = 1f;
            var root = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            if (root != null)
            {
                root.GetWorldCorners(_canvasCorners);
                for (int i = 0; i < _canvasCorners.Length; i++)
                {
                    float y = background.rectTransform.InverseTransformPoint(_canvasCorners[i]).y;
                    top = Mathf.Max(top, (y - r.y) / r.height);
                }
            }
            _bolt[0].y = top + Mathf.Max(32f, width * 8f) / size.y;
            Matrix4x4 m = rectTransform.worldToLocalMatrix * background.rectTransform.localToWorldMatrix *
                Matrix4x4.TRS(new Vector3(r.x, r.y), Quaternion.identity, new Vector3(r.width / size.x, r.height / size.y, 1));
            // Soft cloud illumination stays within the selected sky region.
            Vector2 center = Vector2.Scale(_region.center, size);
            Color glow = new Color(.62f, .78f, 1, .5f * _strength * color.a);
            int n = vh.currentVertCount;
            vh.AddVert(m.MultiplyPoint3x4(center), glow, Vector2.zero);
            glow.a = 0;
            const int sides = 24;
            for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                Vector2 p = center + Vector2.Scale(new Vector2(Mathf.Cos(a) * _region.width * .5f, Mathf.Sin(a) * _region.height * .5f), size);
                vh.AddVert(m.MultiplyPoint3x4(p), glow, Vector2.zero);
                if (i > 0) vh.AddTriangle(n, n + i, n + i + 1);
            }
            DrawPath(vh, m, size, _bolt, 1);
            DrawPath(vh, m, size, _branch, .55f);
        }
        private void DrawPath(VertexHelper vh, Matrix4x4 m, Vector2 size, Vector2[] path, float scale)
        {
            for (int i = 1; i < path.Length; i++)
            {
                Vector2 a = Vector2.Scale(path[i - 1], size), b = Vector2.Scale(path[i], size);
                Line(vh, m, a, b, width * scale * 8, new Color(.42f, .65f, 1, .14f * _strength * color.a));
                Line(vh, m, a, b, width * scale * 3.5f, new Color(.65f, .82f, 1, .4f * _strength * color.a));
                Line(vh, m, a, b, width * scale, new Color(.96f, .98f, 1, _strength * color.a));
            }
        }
        private static void Line(VertexHelper vh, Matrix4x4 m, Vector2 a, Vector2 b, float thickness, Color c)
        {
            Vector2 d = b - a, side = new Vector2(-d.y, d.x).normalized * thickness * .5f;
            int n = vh.currentVertCount;
            vh.AddVert(m.MultiplyPoint3x4(a - side), c, Vector2.zero); vh.AddVert(m.MultiplyPoint3x4(a + side), c, Vector2.zero);
            vh.AddVert(m.MultiplyPoint3x4(b + side), c, Vector2.zero); vh.AddVert(m.MultiplyPoint3x4(b - side), c, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
    }
}
