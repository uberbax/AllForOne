#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class MobileJoystickSceneAudit
{
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.delayCall += Requested;
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && File.Exists("Assets/System/MobileInput/Editor/.test-request")) {
                File.Delete("Assets/System/MobileInput/Editor/.test-request");
                double until = EditorApplication.timeSinceStartup + 2;
                EditorApplication.CallbackFunction run = null;
                run = () => { if (EditorApplication.timeSinceStartup < until) return; EditorApplication.update -= run; Audit(); };
                EditorApplication.update += run;
            }
        };
    }
    static void Requested()
    {
        const string request = "Assets/System/MobileInput/Editor/.repair-request";
        if (!File.Exists(request) || EditorApplication.isPlaying) return;
        File.Delete(request);
        MobileJoystickPrefabBuilder.RepairRootLayout();
        foreach (var canvas in UnityEngine.Object.FindObjectsByType<MobileInputCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var rect = (RectTransform)canvas.transform;
            if (rect.parent == null || rect.parent.GetComponentInParent<Canvas>() == null) continue;
            if (rect.localScale != Vector3.zero) continue;
            Undo.RecordObject(rect, "Repair joystick Canvas layout");
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rect.gameObject.scene);
        }
        EditorApplication.delayCall += Audit;
    }
    [MenuItem("Tools/Mobile Input/Audit Scene")]
    public static void Audit()
    {
        try {
        Canvas.ForceUpdateCanvases();
        var rows = new List<object>();
        foreach(var j in UnityEngine.Object.FindObjectsByType<MobileJoystick>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
            var r = (RectTransform)j.transform;
            var c = j.GetComponentInParent<Canvas>();
            var point = RectTransformUtility.WorldToScreenPoint(c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera, r.TransformPoint(r.rect.center));
            var hits = new List<RaycastResult>();
            var system = EventSystem.current ?? UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if(system != null) system.RaycastAll(new PointerEventData(system){position=point},hits);
            string inputCheck = "Not run outside Play Mode";
            if (EditorApplication.isPlaying && hits.Count > 0 && hits[0].gameObject == j.gameObject) {
                var camera = c.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.rootCanvas.worldCamera;
                var data = new PointerEventData(system) { pointerId = -1, button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(camera,r.TransformPoint(r.rect.center + Vector2.right * 60)), pointerPressRaycast = hits[0] };
                ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerDownHandler);
                bool moved = j.Horizontal > 0.5f;
                ExecuteEvents.Execute(hits[0].gameObject, data, ExecuteEvents.pointerUpHandler);
                inputCheck = moved && j.Value == Vector2.zero ? "PASS: raycast, pointer down, analog value, release reset" : "FAIL: input did not move/reset";
            }
            rows.Add(new { inputCheck, name=j.name, scene=j.gameObject.scene.path, active=j.isActiveAndEnabled, scale=r.lossyScale.ToString(), point=point.ToString(), canvas=c.name, root=c.rootCanvas.name, mode=c.renderMode.ToString(), order=c.sortingOrder, graphics=j.GetComponents<Graphic>().Select(g=>new{type=g.GetType().Name,g.raycastTarget,depth=g.depth,cull=g.canvasRenderer.cull}).ToArray(), hits=hits.Select(h=>h.gameObject.name).ToArray(), ancestors=j.GetComponentsInParent<CanvasGroup>().Select(g=>new{g.name,g.blocksRaycasts,g.interactable,g.alpha}).ToArray() });
        }
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"joystick-scene-audit.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new{playing=EditorApplication.isPlaying, joysticks=rows, systems=UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(e=> new{e.name, active=e.isActiveAndEnabled, current=e==EventSystem.current, modules=e.GetComponents<BaseInputModule>().Select(m=>new{type=m.GetType().FullName,active=m.isActiveAndEnabled}).ToArray()}).ToArray()},Newtonsoft.Json.Formatting.Indented));
        } catch(Exception e) { Debug.LogException(e); }
    }
}
#endif

