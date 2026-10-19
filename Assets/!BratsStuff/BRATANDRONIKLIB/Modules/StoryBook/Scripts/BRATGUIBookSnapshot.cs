using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Renders only the supplied page graphics; source UI objects and their hierarchy stay untouched.
internal sealed class BRATGUIBookSnapshot : IDisposable
{
    private readonly Dictionary<Graphic, Material> _materials = new Dictionary<Graphic, Material>();
    private readonly Dictionary<Graphic, Mesh> _meshes = new Dictionary<Graphic, Mesh>();
    private readonly List<Vector3> _vertices = new List<Vector3>();
    private readonly List<Vector3> _normals = new List<Vector3>();
    private readonly List<Vector4> _uv = new List<Vector4>();
    private readonly List<Color32> _colors = new List<Color32>();
    private readonly List<int> _triangles = new List<int>();
    private readonly CommandBuffer _commands = new CommandBuffer { name = "Story page snapshot" };

    public void Capture(RectTransform book, Graphic background, Graphic[] content, RenderTexture target)
    {
        Rect r = book.rect;
        Vector4 screen = Shader.GetGlobalVector("_ScreenParams");
        _commands.Clear();
        _commands.SetRenderTarget(target);
        _commands.SetGlobalVector("_ScreenParams", new Vector4(target.width, target.height, 1f + 1f / target.width, 1f + 1f / target.height));
        _commands.ClearRenderTarget(true, true, Color.clear);
        _commands.SetViewProjectionMatrices(Matrix4x4.identity,
            GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(r.xMin, r.xMax, r.yMin, r.yMax, -1000, 1000), false));
        Draw(background, book);
        foreach (Graphic graphic in content) Draw(graphic, book);
        _commands.SetGlobalVector("_ScreenParams", screen);
        Graphics.ExecuteCommandBuffer(_commands);
    }

    private void Draw(Graphic graphic, RectTransform book)
    {
        if (graphic == null || !graphic.isActiveAndEnabled) return;
        Mesh sourceMesh = graphic.canvasRenderer.GetMesh();
        if (sourceMesh == null || sourceMesh.vertexCount == 0) return;
        if (!_meshes.TryGetValue(graphic, out Mesh mesh))
        {
            mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            _meshes.Add(graphic, mesh);
        }
        // CanvasRenderer meshes have CPU data but cannot be drawn directly by DrawMesh.
        mesh.Clear();
        sourceMesh.GetVertices(_vertices); mesh.SetVertices(_vertices);
        sourceMesh.GetColors(_colors); mesh.SetColors(_colors);
        sourceMesh.GetNormals(_normals); mesh.SetNormals(_normals);
        for (int channel = 0; channel < 4; channel++)
        { sourceMesh.GetUVs(channel, _uv); mesh.SetUVs(channel, _uv); }
        sourceMesh.GetTriangles(_triangles, 0); mesh.SetTriangles(_triangles, 0);
        Material source = graphic.materialForRendering;
        if (!_materials.TryGetValue(graphic, out Material material))
        {
            material = new Material(source) { hideFlags = HideFlags.HideAndDontSave };
            _materials.Add(graphic, material);
        }
        material.CopyPropertiesFromMaterial(source);
        material.SetInt("_UIVertexColorAlwaysGammaSpace", 1);
        if (!(graphic is TMPro.TMP_Text) && !(graphic is TMPro.TMP_SubMeshUI)) material.mainTexture = graphic.mainTexture;
        material.DisableKeyword("UNITY_UI_CLIP_RECT");
        if (material.HasProperty("_StencilComp")) material.SetInt("_StencilComp", (int)CompareFunction.Always);
        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        _commands.DrawMesh(mesh, book.worldToLocalMatrix * graphic.transform.localToWorldMatrix, material);
    }

    public void Dispose()
    {
        _commands.Release();
        foreach (var material in _materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
        foreach (var mesh in _meshes.Values) if (mesh != null) UnityEngine.Object.Destroy(mesh);
        _materials.Clear(); _meshes.Clear();
    }
}


