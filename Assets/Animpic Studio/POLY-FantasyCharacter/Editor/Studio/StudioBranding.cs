using System;
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
    [InitializeOnLoad]
    public static class StudioBranding
    {
        private const string Pending = "Animpic.Studio.BrandingPending";
        private const string BrandPath = "Assets/Animpic Studio/POLY-FantasyCharacter/UI/Studio/Resources/AnimpicStudio/Brand/";
        static StudioBranding()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
                { SessionState.SetBool(Pending, false); EditorApplication.delayCall += Apply; }
            };
        }
        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Refresh Interface", false, 300)]
        public static void Apply()
        {
            var ui = Object.FindObjectOfType<CharacterStudioUI>();
            if (!ui || !ui.viewCamera) throw new InvalidOperationException("Open the existing CharacterStudio scene.");
            ImportLogo("animpic-wordmark-light.png"); ImportLogo("animpic-mark.png");
            if (!AnimpicStudioTheme.Wordmark) throw new InvalidOperationException("The official Animpic wordmark has not imported.");
            var beforeFemale = ui.female ? ui.female.SaveJson() : null;
            var beforeMale = ui.male ? ui.male.SaveJson() : null;
            var cameraPosition = ui.viewCamera.transform.position;
            var cameraRotation = ui.viewCamera.transform.rotation;
            var cameraRect = ui.viewCamera.rect;
            int category = Read<int>(ui, "tab"); string part = Read<string>(ui, "paintedPart");
            ui.BuildUI();
            Write(ui, "paintedPart", part); Refresh(ui);
            string output = Path.GetFullPath("Library/CharacterStudioValidation/branding"); Directory.CreateDirectory(output);
            try
            {
                var names = new[] { "appearance", "outfit", "colour" };
                for (int i = 0; i < names.Length; i++)
                {
                    Write(ui, "tab", i); Refresh(ui);
                    typeof(CharacterStudioUI).GetMethod("UpdateTabs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
                    CharacterStudioSetup.Render(ui, Path.Combine(output, names[i] + ".png"));
                }
            }
            finally
            {
                Write(ui, "tab", category); Write(ui, "paintedPart", part); Refresh(ui);
                typeof(CharacterStudioUI).GetMethod("UpdateTabs", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null);
            }
            if ((ui.female && ui.female.SaveJson() != beforeFemale) || (ui.male && ui.male.SaveJson() != beforeMale))
                throw new InvalidOperationException("The interface update changed a character appearance.");
            if (ui.viewCamera.transform.position != cameraPosition || ui.viewCamera.transform.rotation != cameraRotation || ui.viewCamera.rect != cameraRect)
                throw new InvalidOperationException("The interface update changed the camera.");
            CharacterStudioSetup.Render(ui, Path.Combine(output, "preview.png"));
            if (EditorApplication.isPlaying) SessionState.SetBool(Pending, true);
            else
            {
                var scene = ui.gameObject.scene; EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the updated studio interface.");
            }

            var rows = ui.uiCanvas.GetComponentsInChildren<HorizontalLayoutGroup>(false).Where(g => g.transform.Find("Selected value"));
            File.WriteAllLines(Path.Combine(output, "branding-report.txt"), new[] {
                "Completed: " + DateTime.Now.ToString("O"), "Play mode: " + Application.isPlaying,
                "Fonts: " + AnimpicStudioTheme.BodyFont.name + ", " + AnimpicStudioTheme.MediumFont.name + ", " + AnimpicStudioTheme.HeadingFont.name,
                "Logo: " + AnimpicStudioTheme.Wordmark.name,
                "Character appearances and camera preserved.",
                "Viewport: " + ui.viewCamera.rect,
                string.Join(Environment.NewLine, rows.Select(g => g.name + ": " + string.Join(", ", Enumerable.Range(0, g.transform.childCount).Select(i => {
                    var child = (RectTransform)g.transform.GetChild(i); return child.name + "=" + child.rect.width.ToString("F1");
                })))) });
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
        private static void ImportLogo(string file)
        {
            var importer = AssetImporter.GetAtPath(BrandPath + file) as TextureImporter;
            if (!importer) throw new FileNotFoundException("Missing brand asset: " + file);
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) return;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        private static T Read<T>(CharacterStudioUI ui, string name) { return (T)typeof(CharacterStudioUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui); }
        private static void Write(CharacterStudioUI ui, string name, object value) { typeof(CharacterStudioUI).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(ui, value); }
        private static void Refresh(CharacterStudioUI ui) { typeof(CharacterStudioUI).GetMethod("RefreshContent", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ui, null); Canvas.ForceUpdateCanvases(); }
    }
}
