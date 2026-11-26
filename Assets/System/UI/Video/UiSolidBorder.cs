using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public sealed class UiSolidBorder : BaseMeshEffect
{
    [SerializeField, Min(0f)] private float width = 1.5f;

    public override void ModifyMesh(VertexHelper vertices)
    {
        if (!IsActive() || vertices.currentVertCount != 4) return;

        var vertex = UIVertex.simpleVert;
        vertices.PopulateUIVertex(ref vertex, 0);
        var materialVertex = vertex;
        Vector2 min = vertex.position;
        Vector2 max = vertex.position;
        for (int i = 1; i < 4; i++)
        {
            vertices.PopulateUIVertex(ref vertex, i);
            min = Vector2.Min(min, vertex.position);
            max = Vector2.Max(max, vertex.position);
        }

        float thickness = Mathf.Min(width, .5f * Mathf.Min(max.x - min.x, max.y - min.y));
        if (thickness <= 0f) return;

        vertices.Clear();
        AddStrip(min.x, min.y, max.x, min.y + thickness);
        AddStrip(min.x, max.y - thickness, max.x, max.y);
        AddStrip(min.x, min.y + thickness, min.x + thickness, max.y - thickness);
        AddStrip(max.x - thickness, min.y + thickness, max.x, max.y - thickness);

        void AddStrip(float left, float bottom, float right, float top)
        {
            int index = vertices.currentVertCount;
            var corner = materialVertex;
            float z = corner.position.z;
            corner.position = new Vector3(left, bottom, z);
            vertices.AddVert(corner);
            corner.position = new Vector3(left, top, z);
            vertices.AddVert(corner);
            corner.position = new Vector3(right, top, z);
            vertices.AddVert(corner);
            corner.position = new Vector3(right, bottom, z);
            vertices.AddVert(corner);
            vertices.AddTriangle(index, index + 1, index + 2);
            vertices.AddTriangle(index + 2, index + 3, index);
        }
    }
}
