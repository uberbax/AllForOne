using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRATANDRONIKLIB
{
    // Owned by the battle controller. All geometry is temporary and expressed in world coordinates.
    internal sealed class BRATViewBattleVfx : IDisposable
    {
        private const int VertexCapacity = 1536;
        private const int IndexCapacity = 2304;
        private const int VignetteBands = 4;
        private const float MinimumScale = 0.0001f;

        private static readonly Color WarmWhite = new(1f, 0.94f, 0.76f, 1f);
        private static readonly Color SlashRed = new(0.88f, 0.075f, 0.025f, 1f);
        private static readonly Color HealingGold = new(1f, 0.68f, 0.14f, 1f);

        private readonly Transform _owner;
        private readonly GameObject _root;
        private readonly Transform _rootTransform;
        private readonly MeshRenderer _renderer;
        private readonly Mesh _mesh;
        private readonly Vector3[] _vertices = new Vector3[VertexCapacity];
        private readonly Color[] _colors = new Color[VertexCapacity];
        private readonly int[] _indices = new int[IndexCapacity];
        private int _vertexCount;
        private int _indexCount;
        private int _previousIndexCount;
        private Vector3 _boundsMin;
        private Vector3 _boundsMax;
        private bool _disposed;

        public BRATViewBattleVfx(Transform owner, Material material)
        {
            if (!owner) throw new ArgumentNullException(nameof(owner));
            if (!material) throw new ArgumentNullException(nameof(material));

            _owner = owner;
            _root = new GameObject("BRAT Battle VFX") { hideFlags = HideFlags.DontSave, layer = 0 };
            _rootTransform = _root.transform;
            _rootTransform.SetParent(owner, true);

            _mesh = new Mesh { name = "BRAT Battle VFX Mesh", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _root.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _root.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.sortingLayerID = 0;
            _renderer.sortingOrder = 1000;
            _renderer.shadowCastingMode = ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            _renderer.enabled = false;

            // Fixed buffer sizes also avoid resizing native mesh buffers between effect phases.
            // Unused triangles remain (0, 0, 0); their vertices have no visible area.
            var uv = new Vector2[VertexCapacity];
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(0.5f, 0.5f);
            _mesh.SetVertices(_vertices, 0, VertexCapacity);
            _mesh.SetColors(_colors, 0, VertexCapacity);
            _mesh.SetUVs(0, uv, 0, VertexCapacity);
            _mesh.SetTriangles(_indices, 0, IndexCapacity, 0, false);
        }

        public void Draw(Camera camera, Vector3 targetCenter, float focus, float impact, float healing, float elapsed, bool heavy)
        {
            if (_disposed) return;
            if (!camera || !_owner || !_root || !_mesh || !_renderer || !_renderer.sharedMaterial)
            {
                Clear();
                return;
            }

            focus = Weight(focus);
            impact = Weight(impact);
            healing = Weight(healing);
            if (focus <= 0f && impact <= 0f && healing <= 0f)
            {
                Clear();
                return;
            }

            Vector3 parentScale = _owner.lossyScale;
            if (Mathf.Abs(parentScale.x) < MinimumScale || Mathf.Abs(parentScale.y) < MinimumScale || Mathf.Abs(parentScale.z) < MinimumScale)
            {
                Clear();
                return;
            }
            _rootTransform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _rootTransform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);

            _vertexCount = 0;
            _indexCount = 0;
            if (float.IsNaN(elapsed) || float.IsInfinity(elapsed)) elapsed = 0f;

            if (focus > 0f) DrawVignette(camera, focus);
            if (impact > 0f) DrawImpact(targetCenter, impact, heavy);
            if (healing > 0f) DrawHealing(targetCenter, healing, elapsed);
            if (_vertexCount == 0)
            {
                Clear();
                return;
            }

            if (_indexCount < _previousIndexCount)
                Array.Clear(_indices, _indexCount, _previousIndexCount - _indexCount);
            _previousIndexCount = _indexCount;
            _mesh.SetVertices(_vertices, 0, VertexCapacity);
            _mesh.SetColors(_colors, 0, VertexCapacity);
            _mesh.SetTriangles(_indices, 0, IndexCapacity, 0, false);
            _mesh.bounds = new Bounds((_boundsMin + _boundsMax) * 0.5f, _boundsMax - _boundsMin + Vector3.one * 0.02f);
            _renderer.enabled = true;
        }

        public void Clear()
        {
            if (_disposed) return;
            if (_renderer) _renderer.enabled = false;
            _vertexCount = 0;
            _indexCount = 0;
        }

        public void Dispose()
        {
            if (_disposed) return;
            Clear();
            _disposed = true;
            DestroyOwned(_root);
            DestroyOwned(_mesh);
        }

        private void DrawVignette(Camera camera, float focus)
        {
            Transform cameraTransform = camera.transform;
            float forwardZ = cameraTransform.forward.z;
            if (Mathf.Abs(forwardZ) < MinimumScale) return;
            float depth = -cameraTransform.position.z / forwardZ;
            if (depth <= camera.nearClipPlane || depth >= camera.farClipPlane) return;

            for (int band = 0; band < VignetteBands; band++)
            {
                float t0 = band / (float)VignetteBands;
                float t1 = (band + 1f) / VignetteBands;
                float x0 = Mathf.Lerp(-0.015f, 0.2f, t0);
                float y0 = Mathf.Lerp(-0.015f, 0.18f, t0);
                float x1 = Mathf.Lerp(-0.015f, 0.2f, t1);
                float y1 = Mathf.Lerp(-0.015f, 0.18f, t1);
                Color outer = new(0.015f, 0.008f, 0.02f, 0.4f * focus * (1f - Mathf.SmoothStep(0f, 1f, t0)));
                Color inner = new(outer.r, outer.g, outer.b, 0.4f * focus * (1f - Mathf.SmoothStep(0f, 1f, t1)));

                Vector3 outerBl = camera.ViewportToWorldPoint(new Vector3(x0, y0, depth));
                Vector3 outerTl = camera.ViewportToWorldPoint(new Vector3(x0, 1f - y0, depth));
                Vector3 outerTr = camera.ViewportToWorldPoint(new Vector3(1f - x0, 1f - y0, depth));
                Vector3 outerBr = camera.ViewportToWorldPoint(new Vector3(1f - x0, y0, depth));
                Vector3 innerBl = camera.ViewportToWorldPoint(new Vector3(x1, y1, depth));
                Vector3 innerTl = camera.ViewportToWorldPoint(new Vector3(x1, 1f - y1, depth));
                Vector3 innerTr = camera.ViewportToWorldPoint(new Vector3(1f - x1, 1f - y1, depth));
                Vector3 innerBr = camera.ViewportToWorldPoint(new Vector3(1f - x1, y1, depth));

                AddQuad(outerBl, outerTl, innerTl, innerBl, outer, outer, inner, inner);
                AddQuad(outerTl, outerTr, innerTr, innerTl, outer, outer, inner, inner);
                AddQuad(outerTr, outerBr, innerBr, innerTr, outer, outer, inner, inner);
                AddQuad(outerBr, outerBl, innerBl, innerBr, outer, outer, inner, inner);
            }
        }

        private void DrawImpact(Vector3 center, float impact, bool heavy)
        {
            float spread = 1f - impact;
            float scale = (heavy ? 1.25f : 1f) * Mathf.Lerp(0.9f, 1.08f, spread);
            DrawSlash(center, impact, scale, false, 24);
            if (heavy) DrawSlash(center + new Vector3(0.04f, -0.06f, 0f), impact * 0.65f, scale * 0.68f, true, 16);

            int count = heavy ? 18 : 12;
            for (int i = 0; i < count; i++)
            {
                float phase = i * 2.39996323f;
                Vector3 direction = new(Mathf.Cos(phase), Mathf.Sin(phase), 0f);
                Vector3 side = new(-direction.y, direction.x, 0f);
                float variation = Mathf.Repeat(i * 0.61803399f, 1f);
                float distance = (0.06f + spread * (0.8f + variation * 0.55f)) * scale;
                float length = (0.18f + variation * 0.42f) * scale;
                float width = (0.018f + variation * 0.022f) * scale;
                Vector3 start = center + direction * distance;
                Color baseColor = i % 3 == 0 ? SlashRed : WarmWhite;
                baseColor.a = impact * 0.88f;
                Color tipColor = baseColor;
                tipColor.a = 0f;
                AddTriangle(start - side * width, start + side * width, start + direction * length, baseColor, baseColor, tipColor);
            }

            Color glow = new(1f, 0.78f, 0.39f, impact * impact * 0.6f);
            AddDiamond(center, 0.15f * scale, 0.28f * scale, glow);
            Color flash = new(1f, 0.97f, 0.84f, impact * impact * 0.9f);
            AddDiamond(center, 0.05f * scale, 0.13f * scale, flash);
        }

        private void DrawSlash(Vector3 center, float intensity, float scale, bool reverse, int segments)
        {
            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1f) / segments;
                Vector3 p0 = SlashPoint(t0, reverse) * scale + center;
                Vector3 p1 = SlashPoint(t1, reverse) * scale + center;
                Vector3 n0 = SlashNormal(t0, reverse);
                Vector3 n1 = SlashNormal(t1, reverse);
                float taper0 = Mathf.Sin(t0 * Mathf.PI);
                float taper1 = Mathf.Sin(t1 * Mathf.PI);
                float width0 = Mathf.Max(0f, taper0) * 0.15f * scale;
                float width1 = Mathf.Max(0f, taper1) * 0.15f * scale;
                Color red = SlashRed;
                red.a = intensity * 0.65f;
                AddSoftRibbon(p0, p1, n0, n1, width0, width1, red, red);
                Color light = WarmWhite;
                light.a = intensity * 0.95f;
                AddSoftRibbon(p0, p1, n0, n1, width0 * 0.34f, width1 * 0.34f, light, light);
            }
        }

        private static Vector3 SlashPoint(float t, bool reverse)
        {
            float bend = Mathf.Sin(t * Mathf.PI);
            float x = -1.1f + 2.2f * t + 0.33f * bend;
            return new Vector3(reverse ? -x : x, 1.05f - 2.1f * t + 0.42f * bend, 0f);
        }

        private static Vector3 SlashNormal(float t, bool reverse)
        {
            float bend = Mathf.Cos(t * Mathf.PI) * Mathf.PI;
            float dx = (2.2f + 0.33f * bend) * (reverse ? -1f : 1f);
            float dy = -2.1f + 0.42f * bend;
            return new Vector3(-dy, dx, 0f).normalized;
        }

        private void DrawHealing(Vector3 center, float healing, float elapsed)
        {
            for (int i = 0; i < 7; i++)
            {
                float phase = Mathf.Repeat(i * 0.61803399f, 1f);
                float rise = Mathf.Repeat(elapsed * 0.48f + phase, 1f);
                float x = (i - 3f) * 0.22f;
                float bottom = -0.95f + rise * 0.35f;
                float height = 1.55f + phase * 0.65f;
                float width = 0.025f + phase * 0.024f;
                Vector3 start = center + new Vector3(x, bottom, 0f);
                Vector3 middle = start + Vector3.up * (height * 0.58f);
                Vector3 end = start + Vector3.up * height;
                Color dim = HealingGold;
                dim.a = 0f;
                Color bright = HealingGold;
                bright.a = healing * 0.4f * Mathf.Sin(rise * Mathf.PI);
                AddSoftRibbon(start, middle, Vector3.right, Vector3.right, width, width * 0.8f, dim, bright);
                AddSoftRibbon(middle, end, Vector3.right, Vector3.right, width * 0.8f, width * 0.2f, bright, dim);
            }

            for (int i = 0; i < 12; i++)
            {
                float phase = Mathf.Repeat(i * 0.61803399f, 1f);
                float progress = Mathf.Repeat(elapsed * 0.6f + phase, 1f);
                float x = (Mathf.Repeat(i * 0.38196601f, 1f) - 0.5f) * 1.6f;
                x += Mathf.Sin(elapsed * 1.3f + i * 2.4f) * 0.08f;
                Vector3 position = center + new Vector3(x, -1f + progress * 2.5f, 0f);
                Color color = Color.Lerp(HealingGold, WarmWhite, phase * 0.6f);
                color.a = Mathf.Sin(progress * Mathf.PI) * healing * 0.9f;
                float size = 0.04f + phase * 0.035f;
                float thickness = size * 0.2f;
                AddQuad(position + new Vector3(-thickness, -size, 0f), position + new Vector3(-thickness, size, 0f),
                    position + new Vector3(thickness, size, 0f), position + new Vector3(thickness, -size, 0f), color, color, color, color);
                AddQuad(position + new Vector3(-size, -thickness, 0f), position + new Vector3(-size, thickness, 0f),
                    position + new Vector3(size, thickness, 0f), position + new Vector3(size, -thickness, 0f), color, color, color, color);
            }
        }

        private void AddSoftRibbon(Vector3 start, Vector3 end, Vector3 startNormal, Vector3 endNormal,
            float startWidth, float endWidth, Color startColor, Color endColor)
        {
            Color startEdge = startColor;
            Color endEdge = endColor;
            startEdge.a = 0f;
            endEdge.a = 0f;
            AddQuad(start - startNormal * startWidth, end - endNormal * endWidth, end, start, startEdge, endEdge, endColor, startColor);
            AddQuad(start, end, end + endNormal * endWidth, start + startNormal * startWidth, startColor, endColor, endEdge, startEdge);
        }

        private void AddDiamond(Vector3 center, float halfWidth, float halfHeight, Color color)
        {
            AddQuad(center + Vector3.left * halfWidth, center + Vector3.up * halfHeight,
                center + Vector3.right * halfWidth, center + Vector3.down * halfHeight, color, color, color, color);
        }

        private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
        {
            int first = _vertexCount;
            AddVertex(a, ca);
            AddVertex(b, cb);
            AddVertex(c, cc);
            AddVertex(d, cd);
            _indices[_indexCount++] = first;
            _indices[_indexCount++] = first + 1;
            _indices[_indexCount++] = first + 2;
            _indices[_indexCount++] = first;
            _indices[_indexCount++] = first + 2;
            _indices[_indexCount++] = first + 3;
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
        {
            int first = _vertexCount;
            AddVertex(a, ca);
            AddVertex(b, cb);
            AddVertex(c, cc);
            _indices[_indexCount++] = first;
            _indices[_indexCount++] = first + 1;
            _indices[_indexCount++] = first + 2;
        }

        private void AddVertex(Vector3 position, Color color)
        {
            _vertices[_vertexCount] = position;
            _colors[_vertexCount] = color;
            if (_vertexCount == 0) _boundsMin = _boundsMax = position;
            else
            {
                _boundsMin = Vector3.Min(_boundsMin, position);
                _boundsMax = Vector3.Max(_boundsMax, position);
            }
            _vertexCount++;
        }

        private static float Weight(float value)
        {
            return float.IsNaN(value) ? 0f : Mathf.Clamp01(value);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
