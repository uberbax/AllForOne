using UnityEngine;
using UnityEngine.UI;

// Keep the character's height and anchors fixed while wider clips extend sideways.
[RequireComponent(typeof(RawImage))]
public sealed class UiVideoAspect : BaseMeshEffect
{
    private RawImage view;

    protected override void Awake()
    {
        base.Awake();
        view = GetComponent<RawImage>();
    }

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || vertices.currentVertCount == 0) return;
        if (view == null) view = GetComponent<RawImage>();
        var texture = view.texture;
        var rect = view.rectTransform.rect;
        if (texture == null || texture.height <= 0 || rect.width <= 0 || rect.height <= 0) return;
        var uv = view.uvRect;
        if (Mathf.Abs(uv.height) < .00001f) return;
        float ratio = texture.width * Mathf.Abs(uv.width) / (texture.height * Mathf.Abs(uv.height));
        float scaleX = rect.height * ratio / rect.width;
        UIVertex vertex = default;
        for (int i = 0; i < vertices.currentVertCount; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            var position = vertex.position;
            position.x = rect.center.x + (position.x - rect.center.x) * scaleX;
            vertex.position = position;
            vertices.SetUIVertex(vertex, i);
        }
    }
}
