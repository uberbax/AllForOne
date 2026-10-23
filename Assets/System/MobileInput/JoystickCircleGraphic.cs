using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent joystick discs and rings; no texture dependencies.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class JoystickCircleGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 0.99f)] private float innerRadius;
    [SerializeField, Range(16, 128)] private int segments = 64;

    public float InnerRadius
    {
        get => innerRadius;
        set { innerRadius = Mathf.Clamp(value, 0f, 0.99f); SetVerticesDirty(); }
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        Vector2 radius = rect.size * 0.5f;
        int count = Mathf.Clamp(segments, 16, 128);
        float inner = Mathf.Clamp(innerRadius, 0f, 0.99f);
        if (inner <= 0f)
        {
            mesh.AddVert(center, color, new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + Vector2.Scale(direction, radius), color, direction * 0.5f + Vector2.one * 0.5f);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
        else
        {
            for (int i = 0; i <= count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(center + Vector2.Scale(direction, radius), color, Vector2.zero);
                mesh.AddVert(center + Vector2.Scale(direction, radius) * inner, color, Vector2.zero);
                if (i == 0) continue;
                int vertex = i * 2;
                mesh.AddTriangle(vertex - 2, vertex, vertex - 1);
                mesh.AddTriangle(vertex, vertex + 1, vertex - 1);
            }
        }
    }

    public override bool Raycast(Vector2 screenPoint, Camera eventCamera)
    {
        if (!base.Raycast(screenPoint, eventCamera)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var point)) return false;
        Vector2 halfSize = rectTransform.rect.size * 0.5f;
        if (halfSize.x <= 0f || halfSize.y <= 0f) return false;
        point -= rectTransform.rect.center;
        return new Vector2(point.x / halfSize.x, point.y / halfSize.y).sqrMagnitude <= 1f;
    }
}
