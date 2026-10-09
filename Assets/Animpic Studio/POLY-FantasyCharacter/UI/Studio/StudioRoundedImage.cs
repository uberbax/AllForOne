using UnityEngine;
using UnityEngine.UI;

namespace Animpic.CharacterStudio
{
    // Four vertices carry each rectangle's shape; the shared shader resolves a one-pixel AA edge.
    [AddComponentMenu("")]
    public sealed class StudioRoundedImage : Image
    {
        public Vector4 corners = new Vector4(15, 9, 16, 10); // TL, TR, BR, BL
        public float borderWidth = 1;
        public Color borderColor = new Color32(38, 59, 71, 255);
        public bool accentGradient;
        public bool drawShadow;
        public Color shadowColor = new Color(0, 0, 0, .16f);
        public Vector2 shadowOffset = new Vector2(0, -5);

        private static Material smoothMaterial;
        private const AdditionalCanvasShaderChannels ShapeChannels = AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;

        public static Material SmoothMaterial
        {
            get
            {
                if (!smoothMaterial) smoothMaterial = Resources.Load<Material>("AnimpicStudio/UI/StudioSmoothUI");
                return smoothMaterial;
            }
        }
        public override Material defaultMaterial { get { var shared = SmoothMaterial; return shared ? shared : base.defaultMaterial; } }

        protected override void OnEnable() { base.OnEnable(); EnsureChannels(); }
        protected override void OnCanvasHierarchyChanged() { base.OnCanvasHierarchyChanged(); EnsureChannels(); }
        private void EnsureChannels()
        {
            var owner = canvas;
            if (!owner) return;
            if ((owner.additionalShaderChannels & ShapeChannels) != ShapeChannels) owner.additionalShaderChannels |= ShapeChannels;
            var root = owner.rootCanvas;
            if (root && (root.additionalShaderChannels & ShapeChannels) != ShapeChannels) root.additionalShaderChannels |= ShapeChannels;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // An uninstalled resource must still render an ordinary Image, rather than interpreting shape data as UVs.
            if (!SmoothMaterial) { base.OnPopulateMesh(vh); return; }
            EnsureChannels();
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            if (rect.width <= 0 || rect.height <= 0) return;
            float maximum = Mathf.Min(rect.width, rect.height) * .5f;
            var radii = new Vector4(Mathf.Clamp(corners.x, 0, maximum), Mathf.Clamp(corners.y, 0, maximum),
                Mathf.Clamp(corners.z, 0, maximum), Mathf.Clamp(corners.w, 0, maximum));
            Vector2 padding = PixelPadding(rect.center);
            if (drawShadow && shadowColor.a > 0)
            {
                var shadow = shadowColor; shadow.a *= color.a;
                Quad(vh, new Rect(rect.position + shadowOffset, rect.size), radii, padding, shadow, Color.clear, 0, false);
            }
            Quad(vh, rect, radii, padding, color, borderColor,
                borderColor.a > 0 ? Mathf.Clamp(borderWidth, 0, maximum) : 0, accentGradient);
        }

        private Vector2 PixelPadding(Vector2 center)
        {
            var owner = canvas;
            Camera camera = owner && owner.renderMode != RenderMode.ScreenSpaceOverlay ? owner.worldCamera : null;
            Vector2 origin = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(center));
            Vector2 dx = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(center + Vector2.right)) - origin;
            Vector2 dy = RectTransformUtility.WorldToScreenPoint(camera, rectTransform.TransformPoint(center + Vector2.up)) - origin;
            float determinant = Mathf.Abs(dx.x * dy.y - dx.y * dy.x);
            if (determinant < .000001f) return Vector2.one * (2 / Mathf.Max(owner ? owner.scaleFactor : 1, .001f));
            // Inverse screen transform: leave two pixels around every edge, including scaled/rotated canvases.
            return new Vector2(Mathf.Abs(dy.x) + Mathf.Abs(dy.y), Mathf.Abs(dx.x) + Mathf.Abs(dx.y)) * (2 / determinant);
        }

        private static void Quad(VertexHelper vh, Rect rect, Vector4 radii, Vector2 padding,
            Color fill, Color border, float width, bool gradient)
        {
            int first = vh.currentVertCount;
            Vector2 half = rect.size * .5f;
            Color32 rgb = fill;
            var vertex = UIVertex.simpleVert;
            // RGB remains white so CanvasRenderer/Button tint affects fill and border equally.
            vertex.color = new Color(1, 1, 1, fill.a);
            vertex.uv1 = radii;
            vertex.uv2 = new Vector4(border.r, border.g, border.b, border.a);
            // RG16 and B8 avoid large packed integers and use the same colour precision as normal UGUI vertices.
            vertex.uv3 = new Vector4(width, gradient ? 1 : 0, rgb.r * 256 + rgb.g, rgb.b);
            AddVertex(vh, vertex, rect.center, new Vector2(-half.x - padding.x, -half.y - padding.y), half);
            AddVertex(vh, vertex, rect.center, new Vector2(-half.x - padding.x, half.y + padding.y), half);
            AddVertex(vh, vertex, rect.center, new Vector2(half.x + padding.x, half.y + padding.y), half);
            AddVertex(vh, vertex, rect.center, new Vector2(half.x + padding.x, -half.y - padding.y), half);
            vh.AddTriangle(first, first + 1, first + 2); vh.AddTriangle(first + 2, first + 3, first);
        }
        private static void AddVertex(VertexHelper vh, UIVertex vertex, Vector2 center, Vector2 local, Vector2 half)
        {
            vertex.position = center + local;
            vertex.uv0 = new Vector4(local.x, local.y, half.x, half.y);
            vh.AddVert(vertex);
        }
    }
}