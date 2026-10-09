using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class StudioUIAntialiasing
    {
        private const string ResourcePath = "Assets/Animpic Studio/POLY-FantasyCharacter/UI/Studio/Resources/AnimpicStudio/UI/";
        public static void Upgrade()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ResourcePath + "StudioSmoothUI.shader");
            if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("The smooth UI shader is missing or has compilation errors.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(ResourcePath + "StudioSmoothUI.mat");
            if (!material) { material = new Material(shader) { name = "StudioSmoothUI" }; AssetDatabase.CreateAsset(material, ResourcePath + "StudioSmoothUI.mat"); }
            material.shader = shader;
            material.SetColor("_GradientStart", AnimpicStudioTheme.AccentBlue);
            material.SetColor("_GradientMiddle", AnimpicStudioTheme.GradientMiddle);
            material.SetColor("_GradientEnd", AnimpicStudioTheme.GradientEnd);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            StudioBranding.Apply();
            Capture("after");
        }
        public static void Capture(string label)
        {
            var ui = Object.FindObjectOfType<CharacterStudioUI>();
            if (!ui || !ui.viewCamera) throw new InvalidOperationException("The existing CharacterStudio scene must be open.");
            string female = ui.female.SaveJson(), male = ui.male.SaveJson();
            bool femaleActive = ui.female.gameObject.activeSelf, maleActive = ui.male.gameObject.activeSelf;
            Vector3 position = ui.viewCamera.transform.position; Quaternion rotation = ui.viewCamera.transform.rotation;
            int originalTab = Read<int>(ui, "tab"); string originalPart = Read<string>(ui, "paintedPart");
            ui.BuildUI();
            string output = Path.GetFullPath("Library/CharacterStudioValidation/smoothing/" + label); Directory.CreateDirectory(output);
            var report = new List<string> { "Completed: " + DateTime.Now.ToString("O"), "Mode: " + (Application.isPlaying ? "Play" : "Edit"), "MSAA: 1 sample (disabled). No supersampling or image postprocessing.", "UI render path uses the same Canvas shader and vertex channels as Screen Space Overlay." };
            try
            {
                SetTab(ui, 0);
                foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1366, 768), new Vector2Int(2560, 939) })
                    Render(ui, Path.Combine(output, "appearance-" + size.x + "x" + size.y + ".png"), size.x, size.y);
                SetTab(ui, 1); Render(ui, Path.Combine(output, "outfit-1920x1080.png"), 1920, 1080);
                SetTab(ui, 2); Render(ui, Path.Combine(output, "colour-1920x1080.png"), 1920, 1080);
                var shapes = ui.uiCanvas.GetComponentsInChildren<StudioRoundedImage>(false);
                report.Add("Shapes: " + shapes.Length);
                report.Add("Shape shaders: " + string.Join(", ", shapes.Select(s => s.materialForRendering.shader.name).Distinct()));
                report.Add("Shared base shape materials: " + shapes.Select(s => s.material.GetEntityId()).Distinct().Count());
                report.Add("Canvas channels: " + ui.uiCanvas.additionalShaderChannels);
                foreach (var shader in shapes.Select(s => s.materialForRendering.shader).Distinct())
                    if (!shader.isSupported || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Unsupported or failed shader: " + shader.name);
            }
            finally { Write(ui, "paintedPart", originalPart); SetTab(ui, originalTab); }
            if (female != ui.female.SaveJson() || male != ui.male.SaveJson() || femaleActive != ui.female.gameObject.activeSelf || maleActive != ui.male.gameObject.activeSelf)
                throw new InvalidOperationException("UI capture changed a character appearance or selection.");
            if (position != ui.viewCamera.transform.position || rotation != ui.viewCamera.transform.rotation)
                throw new InvalidOperationException("UI capture changed the scene camera.");
            report.Add("Character appearances, active character, and camera pose preserved.");
            Render(ui, Path.Combine(output, "preview.png"), 1920, 1080);
            File.WriteAllLines(Path.Combine(output, "report.txt"), report);
            if (label == "after" && !Application.isPlaying)
            {
                var scene = ui.gameObject.scene; EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the smoothed studio UI.");
            }
        }
        private static void Render(CharacterStudioUI ui, string path, int width, int height)
        {
            var camera = ui.viewCamera; var canvas = ui.uiCanvas;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 }; target.Create();
            var originalTarget = camera.targetTexture; var originalActive = RenderTexture.active;
            var mode = canvas.renderMode; var canvasCamera = canvas.worldCamera; var plane = canvas.planeDistance;
            var layers = canvas.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.gameObject.layer);
            var capture = new GameObject("Temporary single-sample UI capture camera"); var uiCamera = capture.AddComponent<Camera>();
            uiCamera.enabled = false; uiCamera.allowMSAA = false; uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.cullingMask = 1 << 5; uiCamera.targetTexture = target; uiCamera.transform.position = new Vector3(0, 0, -100);
            uiCamera.nearClipPlane = .1f; uiCamera.farClipPlane = 5; uiCamera.orthographic = true;
            Texture2D texture = null;
            try
            {
                foreach (var t in layers.Keys) t.gameObject.layer = 5;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = uiCamera; canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                foreach (var scroll in canvas.GetComponentsInChildren<ScrollRect>(true))
                {
                    scroll.Rebuild(CanvasUpdate.PostLayout);
                    if (scroll.verticalScrollbar && scroll.verticalScrollbarVisibility != ScrollRect.ScrollbarVisibility.Permanent)
                        scroll.verticalScrollbar.gameObject.SetActive(scroll.content.rect.height > scroll.viewport.rect.height + .1f);
                }
                camera.targetTexture = target; RenderTexture.active = target;
                GL.Clear(true, true, camera.backgroundColor); camera.Render(); uiCamera.Render();
                RenderTexture.active = target; texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = mode; canvas.worldCamera = canvasCamera; canvas.planeDistance = plane;
                camera.targetTexture = originalTarget; RenderTexture.active = originalActive;
                foreach (var entry in layers) if (entry.Key) entry.Key.gameObject.layer = entry.Value;
                Object.DestroyImmediate(capture); if (texture) Object.DestroyImmediate(texture);
                target.Release(); Object.DestroyImmediate(target); Canvas.ForceUpdateCanvases();
            }
        }
        private static void SetTab(CharacterStudioUI ui, int tab)
        {
            Write(ui, "tab", tab);
            typeof(CharacterStudioUI).GetMethod("RefreshContent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            typeof(CharacterStudioUI).GetMethod("UpdateTabs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            Canvas.ForceUpdateCanvases();
        }
        private static T Read<T>(CharacterStudioUI ui, string name) { return (T)typeof(CharacterStudioUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui); }
        private static void Write(CharacterStudioUI ui, string name, object value) { typeof(CharacterStudioUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, value); }
    }
}
