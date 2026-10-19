using UnityEngine;
using UnityEngine.UI;

// A curved sheet drawn over the existing spread. Its two faces use snapshots of the UI.
[AddComponentMenu(""), RequireComponent(typeof(CanvasRenderer))]
public sealed class BRATGUIBookLeaf : MaskableGraphic
{
    private Texture _front;
    private Texture _back;
    private float _progress;
    private int _direction = 1;
    public override Texture mainTexture => (_progress < 0.5f ? _front : _back) ?? Texture2D.whiteTexture;

    public void SetSheet(Texture front, Texture back, int direction)
    {
        _front = front; _back = back; _direction = direction; _progress = 0;
        SetAllDirty();
    }

    public void SetProgress(float progress)
    {
        bool previousFace = _progress < 0.5f;
        _progress = Mathf.Clamp01(progress);
        if (previousFace != (_progress < 0.5f)) SetMaterialDirty();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        const int strips = 32;
        Rect r = rectTransform.rect;
        float angle = _progress * Mathf.PI;
        float lift = Mathf.Sin(angle);
        bool front = _progress < 0.5f;
        float side = _direction * (front ? 1 : -1);
        float outerU = side > 0 ? 0.945f : 0.065f;
        float width = r.width * Mathf.Abs(outerU - 0.5f);
        for (int i = 0; i <= strips; i++)
        {
            float s = i / (float)strips;
            float bend = Mathf.Sin(s * Mathf.PI) * lift * 0.19f;
            float x = r.center.x + _direction * width * s * Mathf.Cos(angle + bend);
            float height = r.height * (0.86f + lift * 0.045f * Mathf.Sin(s * Mathf.PI));
            float shade = Mathf.Clamp01(1 - lift * (0.12f + 0.22f * Mathf.Sin(s * Mathf.PI)));
            Color tint = new Color(shade, shade, shade, 1) * color;
            float u = Mathf.Lerp(0.5f, outerU, s);
            vh.AddVert(new Vector3(x, r.yMin + r.height * 0.505f - height * 0.5f), tint, new Vector2(u, 0.075f));
            vh.AddVert(new Vector3(x, r.yMin + r.height * 0.505f + height * 0.5f), tint, new Vector2(u, 0.935f));
            if (i > 0)
            {
                int k = i * 2;
                vh.AddTriangle(k - 2, k - 1, k + 1);
                vh.AddTriangle(k - 2, k + 1, k);
            }
        }
    }
}

