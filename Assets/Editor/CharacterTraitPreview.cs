using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CharacterTraitPreview
{
    [MenuItem("Tools/Character Traits/Verify prefab and render preview")]
    public static void Verify()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var previous = RenderTexture.active;
        var target = new RenderTexture(1080, 1920, 24);
        Texture2D pixels = null;
        var previousHolder = ResourceHolder.instance;
        try
        {
            if (ResourceHolder.instance == null)
                ResourceHolder.instance = UnityEngine.Object.FindAnyObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            var cameraObject = new GameObject("Trait Preview Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.orthographic = true; camera.orthographicSize = 960;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 2000;
            camera.targetTexture = target; camera.scene = scene;
            var canvasObject = new GameObject("Trait Preview Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/System/UI/Prefabs/Character_Trait.prefab");
            var root = UnityEngine.Object.Instantiate(prefab, canvasObject.transform);
            var controller = root.GetComponent<CharacterTraitController>();
            Require(controller != null, "Controller missing");
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                Require(behaviour != null, "Missing script");
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true).Where(b =>
                         b is CharacterTraitController || b is CharacterTraitNode || b is CharacterTraitEnhance))
            {
                var serialized = new SerializedObject(behaviour);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.name != "m_Script")
                        Require(property.objectReferenceValue != null, "Missing reference " + behaviour.name + ": " + property.propertyPath);
            }
            var nodes = root.GetComponentsInChildren<CharacterTraitNode>(true);
            Require(nodes.Length == 10 && nodes.All(n => n.gameObject.activeInHierarchy), "Expected ten active nodes");
            var definitions = (CharacterTraitDefinition[])typeof(CharacterTraitController)
                .GetField("traits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(controller);
            Require(definitions.Length == 10 && definitions.All(d => d.IsValid && d.GoldCost == 100 && d.Res1Cost == 10), "Incorrect defaults");
            var popup = root.GetComponentInChildren<CharacterTraitEnhance>(true);
            Require(!popup.gameObject.activeSelf, "Popup should start hidden");
            pixels = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            Render(root, camera, target, pixels, "work/trait-panel-preview.png");
            popup.gameObject.SetActive(true);
            popup.Refresh(definitions[0], 3, null, false);
            Render(root, camera, target, pixels, "work/trait-popup-preview.png");
            File.WriteAllText("work/trait-verify-result.txt", "PASS: all serialized references; ten visible nodes; 100 gold + 10 res1 defaults; hidden nested popup; panel and popup rendered at 1080x1920.\n");
        }
        finally
        {
            ResourceHolder.instance = previousHolder;
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
    private static void Render(GameObject root, Camera camera, RenderTexture target, Texture2D pixels, string path)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(root.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        pixels.Apply();
        File.WriteAllBytes(path, pixels.EncodeToPNG());
    }
    private static void Require(bool value, string message)
    { if (!value) throw new InvalidOperationException("Trait prefab verification failed: " + message); }
}
