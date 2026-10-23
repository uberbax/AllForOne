#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public static class MobileJoystickFloatingCheck
{
    const string Flag = "Assets/System/MobileInput/Editor/.floating-test";
    [InitializeOnLoadMethod] static void Init()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && File.Exists(Flag)) {
                File.Delete(Flag); Run();
            }
        };
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static async void Run()
    {
        string result;
        try {
            await Task.Delay(1500);
            var j = UnityEngine.Object.FindFirstObjectByType<MobileJoystick>();
            var area = j.transform.parent.GetComponent<MobileJoystickArea>();
            var rect = (RectTransform)area.transform;
            var canvas = area.canvas.rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var group = j.GetComponent<CanvasGroup>();
            Check(group.alpha == 0f, "Initial visibility");
            var data = new PointerEventData(EventSystem.current) {pointerId = -1, button = PointerEventData.InputButton.Left};
            var hits = new List<RaycastResult>();
            data.position = RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
            EventSystem.current.RaycastAll(data,hits);
            Check(hits.Count > 0 && hits[0].gameObject == area.gameObject, "SafeArea raycast");
            data.pointerPressRaycast = hits[0];
            ExecuteEvents.Execute(area.gameObject,data,ExecuteEvents.pointerDownHandler);
            Check(j.IsPressed && j.Value.sqrMagnitude < 0.0001f && group.alpha == 1f,"Floating press/visibility");
            Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera,j.transform.position),data.position)<1f,"Origin at press");
            data.position = RectTransformUtility.WorldToScreenPoint(camera,j.transform.TransformPoint(Vector2.right*65));
            ExecuteEvents.Execute(area.gameObject,data,ExecuteEvents.dragHandler);
            Check(j.Horizontal > 0.5f,"Floating drag");
            var second = new PointerEventData(EventSystem.current){pointerId=2, position=data.position,button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(area.gameObject,second,ExecuteEvents.pointerUpHandler);
            Check(j.IsPressed,"Second finger ownership");
            ExecuteEvents.Execute(area.gameObject,data,ExecuteEvents.pointerUpHandler);
            Check(j.Value == Vector2.zero && !j.IsPressed && group.alpha > 0.9f,"Release reset/fade start");
            float oldTime = Time.timeScale;
            try {
                Time.timeScale=0;
                await Task.Delay(100);
                Check(group.alpha>0f && group.alpha<1f,"Intermediate fade");
                // A new press must interrupt the current fade.
                ExecuteEvents.Execute(area.gameObject,data,ExecuteEvents.pointerDownHandler);
                Check(j.IsPressed && group.alpha==1f,"Fade interruption");
                ExecuteEvents.Execute(area.gameObject,data,ExecuteEvents.pointerUpHandler);
                await Task.Delay(400);
                Check(group.alpha==0f && j.Value==Vector2.zero,"Unscaled fade completion");
            } finally { Time.timeScale=oldTime; j.ResetInput(); }
            // Place a real interactive Button above the surface and check that it gets priority.
            var button = new GameObject("Temporary joystick test button",typeof(RectTransform),typeof(Image),typeof(Button));
            try {
                button.transform.SetParent(rect,false);
                var br=(RectTransform)button.transform; br.sizeDelta=Vector2.one*120;
                Canvas.ForceUpdateCanvases();
                await Task.Delay(150);
                data.position=RectTransformUtility.WorldToScreenPoint(camera,br.position);
                hits.Clear(); EventSystem.current.RaycastAll(data,hits);
                Check(hits.Count>0 && hits[0].gameObject==button,"UI button priority: "+string.Join(",",hits.ConvertAll(h=>h.gameObject.name))+" buttonDepth="+button.GetComponent<Image>().depth+" cull="+button.GetComponent<Image>().canvasRenderer.cull);
            } finally { UnityEngine.Object.DestroyImmediate(button); }
            result="PASS: SafeArea raycast, floating origin, drag, multitouch ownership, immediate reset, intermediate fade, interruption, fade at timeScale 0, UI button priority";
        } catch(Exception e) { result="FAIL: "+e; Debug.LogException(e); }
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"joystick-floating-check.txt"),result);
    }
}
#endif
