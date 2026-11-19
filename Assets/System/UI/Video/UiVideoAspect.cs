using UnityEngine;
using UnityEngine.UI;

// Keep the character's height and anchors fixed while wider clips extend sideways.
[RequireComponent(typeof(RawImage))]
public sealed class UiVideoAspect : BaseMeshEffect
{
    private RawImage view;
    private float visualScale = 1f;
    private Vector2 visualOffset;

    public void SetFraming(float scale, Vector2 offset)
    {
        scale = scale > 0 ? scale : 1f;
        if (Mathf.Approximately(visualScale, scale) && visualOffset == offset) return;
        visualScale = scale;
        visualOffset = offset;
        if (graphic != null) graphic.SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        view = GetComponent<RawImage>();
    }

    public bool TryGetWorldPoint(Vector2 videoUV, out Vector3 worldPoint)
    {
        worldPoint = default;
        if (!TryGetGeometry(out var rect, out float scaleX)) return false;
        var uv = view.uvRect;
        Vector2 point = new Vector2(
            rect.xMin + rect.width * (videoUV.x - uv.x) / uv.width,
            rect.yMin + rect.height * (videoUV.y - uv.y) / uv.height);
        worldPoint = transform.TransformPoint(ApplyFraming(point, rect, scaleX));
        return true;
    }

    private bool TryGetGeometry(out Rect rect, out float scaleX)
    {
        if (view == null) view = GetComponent<RawImage>();
        var texture = view.texture;
        rect = view.rectTransform.rect;
        scaleX = 0;
        if (texture == null || texture.height <= 0 || rect.width <= 0 || rect.height <= 0) return false;
        var uv = view.uvRect;
        if (Mathf.Abs(uv.height) < .00001f || Mathf.Abs(uv.width) < .00001f) return false;
        float ratio = texture.width * Mathf.Abs(uv.width) / (texture.height * Mathf.Abs(uv.height));
        scaleX = rect.height * ratio / rect.width;
        return true;
    }

    private Vector3 ApplyFraming(Vector3 position, Rect rect, float scaleX)
    {
        position.x = rect.center.x + (position.x - rect.center.x) * scaleX * visualScale + visualOffset.x * rect.width;
        position.y = rect.center.y + (position.y - rect.center.y) * visualScale + visualOffset.y * rect.height;
        return position;
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || vertices.currentVertCount == 0 || !TryGetGeometry(out var rect, out float scaleX)) return;
        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            vertex.position = ApplyFraming(vertex.position, rect, scaleX);
            vertices.SetUIVertex(vertex, i);
        }
    }
}
