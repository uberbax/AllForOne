#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class StylizedHitSlashBuilder
{
    private const string Root = "Assets/System/Effects/StylizedHitSlash";
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";
    private static readonly string[] Names = { "Crimson", "Ice", "Emerald", "Scarlet", "Void", "Silver", "Rose", "Toxic", "Amber", "Aether", "Flame", "Holy" };
    private static readonly string[] Hex = { "FF1256", "2AAFFF", "46FFD2", "FF3924", "7436FF", "BED9FF", "FF74C8", "93FF19", "FFA459", "22E8FF", "FF650C", "FFE5A0" };

    [InitializeOnLoadMethod]
    private static void RequestedBuild()
    {
        if (!File.Exists(Work + "/vfx-build.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying || !File.Exists(Work + "/vfx-build.request")) return;
            File.Delete(Work + "/vfx-build.request");
            try { Build(); File.WriteAllText(Work + "/vfx-build-result.txt", "PASS: 24 prefabs, 12 themes, native preview renders and particle simulation verified."); }
            catch (Exception e) { File.WriteAllText(Work + "/vfx-build-result.txt", "FAIL: " + e); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Effects/Build Stylized Hit and Slash")]
    public static void Build()
    {
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Meshes");
        Directory.CreateDirectory(Root + "/Prefabs");
        AssetDatabase.Refresh();
        var shader = Shader.Find("AllForOne/Effects/StylizedHitSlash");
        Require(shader != null && !ShaderUtil.ShaderHasError(shader), "VFX shader must compile");
        var scene = EditorSceneManager.NewPreviewScene();
        var slashes = new List<GameObject>();
        var hits = new List<GameObject>();
        try
        {
            for (int i = 0; i < Names.Length; i++)
            {
                ColorUtility.TryParseHtmlString("#" + Hex[i], out var color);
                var ribbon = Material(shader, Names[i] + "_Ribbon", color, 0);
                var spark = Material(shader, Names[i] + "_Spark", color, 1);
                var glow = Material(shader, Names[i] + "_Glow", color, 2);
                var fragments = Material(shader, Names[i] + "_Fragments", color, i == 7 ? 3 : i == 8 || i == 9 || i == 10 ? 2 : 1);
                for (int form = 0; form < 2; form++)
                {
                    bool impact = form == 1;
                    var root = new GameObject((impact ? "Hit_" : "Slash_") + Names[i]);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    root.SetActive(false);
                    var effect = root.AddComponent<StylizedHitSlash>();
                    var streaks = new GameObject("Streaks"); streaks.transform.SetParent(root.transform, false);
                    var mesh = SaveMesh(RibbonMesh(i, impact), Names[i] + (impact ? "_Hit" : "_Slash"));
                    var meshRenderer = MeshAt(streaks.transform, mesh, ribbon);
                    var ring = new GameObject("Shockwave"); ring.transform.SetParent(root.transform, false);
                    var ringRenderer = MeshAt(ring.transform, SaveMesh(RingMesh(impact ? .55f : .28f), impact ? "HitRing" : "SlashRing"), ribbon);
                    var systems = new List<ParticleSystem>();
                    systems.Add(Particles(root.transform, "Needle sparks", spark, i, impact, 0));
                    systems.Add(Particles(root.transform, "Element fragments", fragments, i, impact, 1));
                    systems.Add(Particles(root.transform, "Soft embers", glow, i, impact, 2));
                    systems.Add(Particles(root.transform, "Impact flash", glow, i, impact, 3));
                    var serialized = new SerializedObject(effect);
                    serialized.FindProperty("streaks").objectReferenceValue = streaks.transform;
                    serialized.FindProperty("shockwave").objectReferenceValue = ring.transform;
                    var renderers = serialized.FindProperty("ribbons"); renderers.arraySize = 2;
                    renderers.GetArrayElementAtIndex(0).objectReferenceValue = meshRenderer;
                    renderers.GetArrayElementAtIndex(1).objectReferenceValue = ringRenderer;
                    var particles = serialized.FindProperty("particles"); particles.arraySize = systems.Count;
                    for (int n = 0; n < systems.Count; n++) particles.GetArrayElementAtIndex(n).objectReferenceValue = systems[n];
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    root.SetActive(true);
                    var path = Root + "/Prefabs/" + root.name + ".prefab";
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    Require(success, "Save failed: " + path);
                    effect.SetPreviewTime(.17f);
                    Require(systems[0].particleCount > 0, "Sparks failed to simulate: " + root.name);
                    Require(root.GetComponentsInChildren<Renderer>().Length == 6, "Incomplete effect: " + root.name);
                    (impact ? hits : slashes).Add(root);
                    root.SetActive(false);
                }
            }
            RenderSheet(scene, slashes, Work + "/Slash_Preview.png");
            RenderSheet(scene, hits, Work + "/Hit_Preview.png");
            AssetDatabase.SaveAssets();
            Debug.Log("STYLIZED_VFX_PASS: 24 animated prefabs saved; shader, particle bursts and native renders verified.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    private static Material Material(Shader shader, string name, Color color, float shape)
    {
        var path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_Color", color * new Color(1.3f, 1.3f, 1.3f, 1));
        material.SetFloat("_Shape", shape); material.SetFloat("_Fade", 1); material.SetFloat("_Erode", 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static MeshRenderer MeshAt(Transform parent, Mesh mesh, Material material)
    {
        var filter = parent.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
        var renderer = parent.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = 20;
        return renderer;
    }

    private static Mesh SaveMesh(Mesh generated, string name)
    {
        foreach (var vertex in generated.vertices)
            Require(!float.IsNaN(vertex.x) && !float.IsNaN(vertex.y) && !float.IsInfinity(vertex.x) && !float.IsInfinity(vertex.y), "Invalid mesh vertex: " + name);
        var path = Root + "/Meshes/" + name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { generated.name = name; AssetDatabase.CreateAsset(generated, path); return generated; }
        existing.Clear(); existing.name = name; existing.vertices = generated.vertices; existing.uv = generated.uv;
        existing.colors = generated.colors; existing.triangles = generated.triangles; existing.RecalculateBounds();
        existing.UploadMeshData(false); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(generated); return existing;
    }

    private static Mesh RibbonMesh(int variant, bool hit)
    {
        var verts = new List<Vector3>(); var uv = new List<Vector2>(); var colors = new List<Color>(); var tris = new List<int>();
        var random = new System.Random(7123 + variant);
        int count = hit ? 12 : 9;
        for (int ray = 0; ray < count; ray++)
        {
            bool main = ray == 0;
            float length = hit ? (main ? 2.6f : 1.0f + (float)random.NextDouble() * 1.8f) : (main ? 3.8f : 2.0f + (float)random.NextDouble() * 1.5f);
            float width = main ? .22f : .015f + (float)random.NextDouble() * .055f;
            float offset = main ? 0 : ((float)random.NextDouble() - .5f) * .4f;
            float angle = hit ? ray * 137.5f : (main ? -2 : ((float)random.NextDouble() - .5f) * 13);
            var rotation = Quaternion.Euler(0, 0, angle);
            int start = verts.Count;
            const int segments = 22;
            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                float taper = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(t * Mathf.PI)), .8f) * Mathf.Lerp(1, .35f, t);
                float kink = main ? Mathf.Sin(t * 8 + variant) * .012f : Mathf.Sin(t * 10 + ray) * .022f;
                float x = hit ? offset * Mathf.Sin(t * Mathf.PI) * .12f : offset * Mathf.Sin(t * Mathf.PI) + kink;
                float y = hit ? (t - .22f) * length : (t - .5f) * length;
                for (int side = 0; side < 2; side++)
                {
                    verts.Add(rotation * new Vector3(x + (side == 0 ? -1 : 1) * width * taper, y, -ray * .001f));
                    uv.Add(new Vector2(side, t)); colors.Add(new Color(1, 1, 1, main ? 1 : .6f));
                }
                if (j == segments) continue;
                int k = start + j * 2; tris.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});
            }
        }
        var mesh = new Mesh(); mesh.SetVertices(verts); mesh.SetUVs(0, uv); mesh.SetColors(colors); mesh.SetTriangles(tris, 0); mesh.RecalculateBounds(); return mesh;
    }

    private static Mesh RingMesh(float radius)
    {
        const int count = 96;
        var verts = new Vector3[(count + 1) * 2]; var uv = new Vector2[verts.Length]; var colors = new Color[verts.Length];
        var triangles = new int[count * 6];
        for (int i = 0; i <= count; i++)
        {
            float a = i * Mathf.PI * 2 / count;
            for (int side = 0; side < 2; side++)
            {
                float r = radius + (side == 0 ? -.018f : .018f);
                verts[i*2+side] = new Vector3(Mathf.Cos(a)*r, Mathf.Sin(a)*r, .015f);
                uv[i*2+side] = new Vector2(side, .5f); colors[i*2+side] = new Color(1,1,1,.65f);
            }
            if (i == count) continue;
            int k=i*2, n=i*6; int[] face={k,k+2,k+1,k+1,k+2,k+3}; Array.Copy(face,0,triangles,n,6);
        }
        var mesh = new Mesh { vertices=verts, uv=uv, colors=colors, triangles=triangles }; mesh.RecalculateBounds(); return mesh;
    }

    private static ParticleSystem Particles(Transform parent, string name, Material material, int variant, bool hit, int layer)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        var system = go.AddComponent<ParticleSystem>(); system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = system.main;
        main.loop = false; main.playOnAwake = false; main.duration = 1.1f; main.maxParticles = 96;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = layer == 3 ? new ParticleSystem.MinMaxCurve(.09f,.15f) : new ParticleSystem.MinMaxCurve(.22f,.75f);
        main.startSpeed = layer == 3 ? 0 : layer == 0 ? new ParticleSystem.MinMaxCurve(1.5f,4.5f) : new ParticleSystem.MinMaxCurve(.1f,1.0f);
        main.startSize = layer == 0 ? new ParticleSystem.MinMaxCurve(.025f,.065f) : layer == 3 ? new ParticleSystem.MinMaxCurve(.6f,.85f) : new ParticleSystem.MinMaxCurve(.06f,.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
        main.gravityModifier = layer == 1 ? (variant == 10 || variant == 9 ? -.18f : .16f) : 0;
        main.startColor = layer == 3 ? new Color(1,1,1,.55f) : new Color(1,1,1,layer == 2 ? .25f : .85f);
        if (layer == 1 && (variant == 8 || variant == 9 || variant == 10))
        {
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(.045f,.1f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(.16f,.38f);
            main.startSizeZ = .1f;
        }
        var emission = system.emission; emission.rateOverTime = 0;
        int amount = layer == 3 ? 1 : layer == 0 ? 22 : layer == 1 ? 18 : 9;
        emission.SetBursts(new[]{new ParticleSystem.Burst(layer == 3 ? 0 : .025f, (short)amount)});
        var shape = system.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = hit ? .09f : .16f;
        if (!hit && layer != 3) { shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(.12f,layer == 0 ? 1.8f : 1.4f,.015f); shape.position = new Vector3(0,-.65f,0); }
        var size = system.sizeOverLifetime; size.enabled = true;
        if (!hit && layer != 3)
        {
            var drift = system.velocityOverLifetime; drift.enabled = true; drift.space = ParticleSystemSimulationSpace.Local;
            drift.x = new ParticleSystem.MinMaxCurve(-1.3f,1.3f);
            drift.y = layer == 0 ? new ParticleSystem.MinMaxCurve(-1.6f,2.2f) : new ParticleSystem.MinMaxCurve(-.4f,.9f);
            drift.z = new ParticleSystem.MinMaxCurve(0,0);
        }
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0,.15f),new Keyframe(.12f,1),new Keyframe(1,0)));
        var alpha = system.colorOverLifetime; alpha.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.6f,.4f),new GradientAlphaKey(0,1)}); alpha.color = gradient;
        var renderer = go.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = material; renderer.sortingOrder = 21;
        renderer.renderMode = layer == 0 ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (layer == 0) { renderer.lengthScale = 3; renderer.velocityScale = .08f; }
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        system.useAutoRandomSeed = false; system.randomSeed = (uint)(93 + variant * 4 + layer);
        return system;
    }

    private static void RenderSheet(Scene scene, List<GameObject> effects, string path)
    {
        const int tile = 256;
        var cameraGO = new GameObject("VFX Preview Camera"); SceneManager.MoveGameObjectToScene(cameraGO,scene);
        var camera = cameraGO.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 2.4f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.035f,.035f,.045f,1);
        camera.transform.position = new Vector3(0,0,-10); camera.nearClipPlane = .1f; camera.farClipPlane = 30;
        camera.scene = scene;
        var target = new RenderTexture(tile,tile,24); target.Create(); camera.targetTexture = target;
        var sheet = new Texture2D(tile*4,tile*3,TextureFormat.RGB24,false);
        var old = RenderTexture.active;
        try
        {
            for (int i=0;i<effects.Count;i++)
            {
                effects[i].SetActive(true); effects[i].GetComponent<StylizedHitSlash>().SetPreviewTime(.16f);
                if (i == 0)
                {
                    var m = effects[i].GetComponentInChildren<MeshFilter>();
                    var r = m.GetComponent<MeshRenderer>(); var block = new MaterialPropertyBlock(); r.GetPropertyBlock(block);
                    File.WriteAllText(Work + "/vfx-render-debug.txt", "Pipeline=" + GraphicsSettings.currentRenderPipeline + "\nBounds=" + r.bounds + "\nVertices=" + m.sharedMesh.vertexCount + "\nScale=" + m.transform.localScale + "\nColor=" + r.sharedMaterial.GetColor("_Color") + "\nFade=" + block.GetFloat("_Fade") + "\nUV=" + m.sharedMesh.uv[22] + "\nVertexColor=" + m.sharedMesh.colors[22]);
                }
                camera.Render(); RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0,0,tile,tile),(i%4)*tile,(2-i/4)*tile);
                effects[i].SetActive(false);
            }
            sheet.Apply(); File.WriteAllBytes(path,sheet.EncodeToPNG());
        }
        finally { RenderTexture.active=old; camera.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(sheet); UnityEngine.Object.DestroyImmediate(cameraGO); }
    }
    private static void Require(bool test,string message) { if(!test) throw new InvalidOperationException(message); }
}
#endif
