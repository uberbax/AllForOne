#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MobileJoystickPrefabBuilder
{
    private const string Folder = "Assets/System/MobileInput";
    private const string PrefabPath = Folder + "/MobileJoystickCanvas.prefab";
    private const string Request = Folder + "/Editor/.build-request";
    private static string ReportPath => Path.Combine(Path.GetTempPath(), "AllForOne-MobileJoystick-validation.json");

    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if (File.Exists(Request)) EditorApplication.delayCall += Build;
    }

    [MenuItem("Tools/Mobile Input/Create Joystick Canvas")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) return;
        if (File.Exists(Request)) File.Delete(Request);
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) CreatePrefab();
            ConfigureFloatingPrefab();
            RepairRootLayout();
            ValidateAndPreview();
        }
        catch (Exception e)
        {
            File.WriteAllText(ReportPath, "FAIL: " + e);
            Debug.LogException(e);
        }
    }

    public static void ConfigureFloatingPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var joystick = root.GetComponentInChildren<MobileJoystick>(true);
            var area = (RectTransform)joystick.transform.parent;
            var surface = area.GetComponent<MobileJoystickArea>();
            if (surface == null) surface = area.gameObject.AddComponent<MobileJoystickArea>();
            surface.Configure(joystick);
            var group = joystick.GetComponent<CanvasGroup>();
            if (group == null) group = joystick.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            foreach (var graphic in joystick.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    // Edit the persistent asset: an overlay Canvas in a preview scene drives its scale to zero.
    public static void RepairRootLayout()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) return;
        var rect = (RectTransform)prefab.transform;
        var fields = new SerializedObject(rect);
        fields.FindProperty("m_LocalScale").vector3Value = Vector3.one;
        fields.FindProperty("m_AnchorMin").vector2Value = Vector2.zero;
        fields.FindProperty("m_AnchorMax").vector2Value = Vector2.one;
        fields.FindProperty("m_Pivot").vector2Value = Vector2.one * 0.5f;
        fields.FindProperty("m_AnchoredPosition").vector2Value = Vector2.zero;
        fields.FindProperty("m_SizeDelta").vector2Value = Vector2.zero;
        fields.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rect);
        AssetDatabase.SaveAssetIfDirty(prefab);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = size;
        return rect;
    }

    private static JoystickCircleGraphic Circle(string name, Transform parent, float size, Color color, float inner = 0f)
    {
        var rect = Rect(name, parent, Vector2.one * size);
        var graphic = rect.gameObject.AddComponent<JoystickCircleGraphic>();
        graphic.color = color;
        graphic.InnerRadius = inner;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static void CreatePrefab()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("MobileJoystickCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MobileInputCanvas));
            root.layer = 5;
            SceneManager.MoveGameObjectToScene(root, scene);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var safeArea = Rect("SafeArea", root.transform, Vector2.zero);
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            var canvasSettings = new SerializedObject(root.GetComponent<MobileInputCanvas>());
            canvasSettings.FindProperty("safeArea").objectReferenceValue = safeArea;
            canvasSettings.ApplyModifiedPropertiesWithoutUndo();

            var baseGraphic = Circle("Joystick", safeArea, 240, new Color(0.025f, 0.065f, 0.115f, 0.76f));
            baseGraphic.raycastTarget = true;
            var background = baseGraphic.rectTransform;
            background.anchorMin = background.anchorMax = Vector2.zero;
            background.anchoredPosition = new Vector2(180, 210);
            Circle("OuterRing", background, 240, new Color(0.43f, 0.75f, 1f, 0.6f), 0.975f);
            Circle("InnerRing", background, 184, new Color(0.65f, 0.83f, 1f, 0.13f), 0.985f);
            Circle("Center", background, 8, new Color(0.7f, 0.87f, 1f, 0.35f));
            var handle = Circle("Handle", background, 88, new Color(0.16f, 0.49f, 0.8f, 0.95f)).rectTransform;
            Circle("Border", handle, 88, new Color(0.68f, 0.89f, 1f, 0.95f), 0.95f);
            Circle("Highlight", handle, 56, new Color(0.4f, 0.73f, 1f, 0.35f));
            var joystick = background.gameObject.AddComponent<MobileJoystick>();
            var fields = new SerializedObject(joystick);
            fields.FindProperty("background").objectReferenceValue = background;
            fields.FindProperty("handle").objectReferenceValue = handle;
            fields.ApplyModifiedPropertiesWithoutUndo();
            root.transform.localScale = Vector3.one;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Could not save joystick prefab");
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void ValidateAndPreview()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        GameObject events = null;
        var oldEvents = EventSystem.current;
        try
        {
            var joystick = root.GetComponentInChildren<MobileJoystick>(true);
            Require(joystick != null && root.GetComponent<Canvas>() != null && root.GetComponent<GraphicRaycaster>() != null, "Prefab components missing");
            var background = (RectTransform)joystick.transform;
            var handle = (RectTransform)background.Find("Handle");
            Require(handle != null, "Handle reference missing");
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<CanvasScaler>().enabled = false;
            root.transform.localScale = Vector3.one;
            root.transform.position = Vector3.zero;
            ((RectTransform)root.transform).sizeDelta = new Vector2(1080, 1920);
            Canvas.ForceUpdateCanvases();
            events = new GameObject("Joystick validation EventSystem", typeof(EventSystem));
            SceneManager.MoveGameObjectToScene(events, root.scene);
            var first = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 1, button = PointerEventData.InputButton.Left };
            var second = new PointerEventData(events.GetComponent<EventSystem>()) { pointerId = 2, button = PointerEventData.InputButton.Left };
            Action<PointerEventData, Vector2> point = (data, local) => data.position = RectTransformUtility.WorldToScreenPoint(null, background.TransformPoint(local));
            point(first, Vector2.zero);
            joystick.OnPointerDown(first);
            point(first, Vector2.right * 1000);
            joystick.OnDrag(first);
            Require(joystick.IsPressed && Vector2.Distance(joystick.Value, Vector2.right) < 0.001f, "Right input failed: value="+joystick.Value+", pressed="+joystick.IsPressed+", active="+joystick.gameObject.activeInHierarchy+", enabled="+joystick.enabled+", scale="+background.lossyScale+", point="+first.position+", rect="+background.rect);
            point(second, Vector2.left * 1000);
            joystick.OnPointerDown(second); joystick.OnDrag(second); joystick.OnPointerUp(second);
            Require(joystick.IsPressed && joystick.Horizontal > 0.999f, "Second pointer took over input");
            joystick.OnPointerUp(first);
            Require(!joystick.IsPressed && joystick.Value == Vector2.zero && handle.anchoredPosition == Vector2.zero, "Release reset failed");
            point(first, Vector2.zero);
            joystick.OnPointerDown(first);
            Require(joystick.Value == Vector2.zero, "Dead-zone centre failed");
            float radius = (Mathf.Min(background.rect.width, background.rect.height) - Mathf.Min(handle.rect.width, handle.rect.height)) * 0.5f;
            point(first, Vector2.right * radius * 0.55f);
            joystick.OnDrag(first);
            Require(Mathf.Abs(joystick.Horizontal - 0.5f) < 0.002f, "Analog/dead-zone remapping failed");
            point(first, Vector2.one * 1000);
            joystick.OnDrag(first);
            Require(Mathf.Abs(joystick.Value.magnitude - 1f) < 0.001f, "Diagonal magnitude limit failed");
            joystick.SendMessage("OnApplicationFocus", false);
            Require(!joystick.IsPressed && joystick.Value == Vector2.zero, "Focus-loss reset failed");
            joystick.OnPointerDown(first);
            joystick.SendMessage("OnApplicationPause", true);
            Require(!joystick.IsPressed && joystick.Value == Vector2.zero, "Pause reset failed");
            joystick.OnPointerDown(first);
            joystick.OnCancel(new BaseEventData(events.GetComponent<EventSystem>()));
            Require(!joystick.IsPressed && joystick.Value == Vector2.zero, "Cancel reset failed");
            joystick.GetComponent<CanvasGroup>().alpha = 1f;
            RenderPreview(root, background);
            File.WriteAllText(ReportPath, Newtonsoft.Json.JsonConvert.SerializeObject(new {
                prefab = PrefabPath, passed = true,
                checks = new[] {"Prefab references", "Pointer movement", "Multitouch ownership", "Release reset", "Dead zone", "Analog input", "Diagonal magnitude", "Focus-loss reset", "Pause reset", "Cancel reset"},
                preview = Path.Combine(Path.GetTempPath(), "AllForOne-MobileJoystick-preview.png")
            }, Newtonsoft.Json.Formatting.Indented));
        }
        finally
        {
            if (events != null) UnityEngine.Object.DestroyImmediate(events);
            if (oldEvents != null) EventSystem.current = oldEvents;
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RenderPreview(GameObject root, RectTransform background)
    {
        var cameraGo = new GameObject("Joystick preview camera", typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraGo, root.scene);
        var camera = cameraGo.GetComponent<Camera>();
        camera.scene = root.scene;
        camera.orthographic = true;
        camera.orthographicSize = 152;
        camera.transform.position = background.position + new Vector3(0, 0, -1000);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.05f, 0.075f, 1);
        camera.nearClipPlane = 0.1f; camera.farClipPlane = 2000;
        var target = new RenderTexture(384, 384, 24);
        var previous = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture = new Texture2D(384, 384, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, 384, 384), 0, 0);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "AllForOne-MobileJoystick-preview.png"), texture.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(cameraGo);
        }
    }
}
#endif
