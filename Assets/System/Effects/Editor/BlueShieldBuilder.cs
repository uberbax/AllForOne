#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BlueShieldBuilder
{
    private const string Folder = "Assets/System/Effects";
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";

    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if (!File.Exists(Work + "/blue-shield.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying || !File.Exists(Work + "/blue-shield.request")) return;
            File.Delete(Work + "/blue-shield.request");
            try { Build(); File.WriteAllText(Work + "/blue-shield-result.txt", "PASS: Shield_Blue prefab saved; shader, transparent material, fade visibility and native render verified."); }
            catch (Exception e) { File.WriteAllText(Work + "/blue-shield-result.txt", "FAIL: " + e); Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/Effects/Build Blue Shield")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder + "/BlueShield"); AssetDatabase.Refresh();
        var shader = Shader.Find("AllForOne/Effects/BlueShield");
        Require(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "Shield shader failed to compile");
        string materialPath = Folder + "/BlueShield/Shield_Blue.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
        material.shader = shader;
        material.SetColor("_Color", new Color(.025f,.3f,.8f,1));
        material.SetColor("_EdgeColor", new Color(.12f,.85f,2,1));
        material.SetFloat("_Opacity", .14f); material.SetFloat("_HexScale", 9);
        material.SetFloat("_Visibility", 1); material.SetFloat("_ShieldTime", 0);
        EditorUtility.SetDirty(material);
        var scene = EditorSceneManager.NewPreviewScene();
        Material subjectMaterial = null;
        try
        {
            var root = new GameObject("Shield_Blue"); SceneManager.MoveGameObjectToScene(root, scene); root.SetActive(false);
            var effect = root.AddComponent<BlueShieldVFX>();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); SceneManager.MoveGameObjectToScene(sphere, scene);
            sphere.name = "Energy shell"; sphere.transform.SetParent(root.transform, false);
            UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
            var renderer = sphere.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            var data = new SerializedObject(effect); data.FindProperty("shell").objectReferenceValue = renderer; data.ApplyModifiedPropertiesWithoutUndo();
            effect.SetPreviewTime(1); root.SetActive(true);
            Require(root.GetComponentsInChildren<Collider>().Length == 0, "Visual shield must not create physics collisions");
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Shield_Blue.prefab", out bool saved);
            Require(saved, "Shield prefab save failed");
            Require(material.GetTag("RenderType", false) == "Transparent" && material.GetFloat("_Opacity") < .5f, "Shield must be semitransparent");

            // Validate visibility without entering Play mode or touching the user's scene.
            var block = new MaterialPropertyBlock(); effect.Play(); renderer.GetPropertyBlock(block);
            Require(block.GetFloat("_Visibility") == 0, "Shield must start transparent for fade-in");
            effect.SetPreviewTime(1); renderer.GetPropertyBlock(block);
            Require(block.GetFloat("_Visibility") == 1 && renderer.transform.localScale.x > 2, "Shield radius and visible state");

            var subject = GameObject.CreatePrimitive(PrimitiveType.Capsule); SceneManager.MoveGameObjectToScene(subject, scene);
            subject.name = "Preview object (not part of prefab)"; subject.transform.localScale = new Vector3(.55f,.7f,.55f);
            subjectMaterial = new Material(Shader.Find("Standard")); subjectMaterial.color = new Color(.75f,.23f,.045f);
            subjectMaterial.SetFloat("_Metallic", .15f); subjectMaterial.SetFloat("_Glossiness", .4f);
            subject.GetComponent<MeshRenderer>().sharedMaterial = subjectMaterial;
            var lightGO = new GameObject("Preview light"); SceneManager.MoveGameObjectToScene(lightGO, scene);
            var light = lightGO.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.5f;
            lightGO.transform.rotation = Quaternion.Euler(35,-35,0);
            Render(scene, Folder + "/BlueShield/Shield_Preview.png");
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("BLUE_SHIELD_PASS: semitransparent blue shield prefab and preview saved.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); if(subjectMaterial != null) UnityEngine.Object.DestroyImmediate(subjectMaterial); }
    }

    private static void Render(Scene scene, string path)
    {
        var go = new GameObject("Shield Preview Camera"); SceneManager.MoveGameObjectToScene(go, scene);
        var camera = go.AddComponent<Camera>(); camera.scene = scene;
        camera.transform.position = new Vector3(2,.7f,-6); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1.6f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.06f);
        var target = new RenderTexture(640,640,24); target.Create(); camera.targetTexture = target;
        var image = new Texture2D(640,640,TextureFormat.RGB24,false); var old = RenderTexture.active;
        try
        {
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,640,640),0,0); image.Apply();
            var center = image.GetPixel(320,320);
            Require(center.r > .08f, "The warm-colored object inside must remain visible through the shield");
            File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally { RenderTexture.active = old; camera.targetTexture = null; target.Release(); UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); }
    }
    private static void Require(bool valid, string message) { if(!valid) throw new InvalidOperationException(message); }
}
#endif
