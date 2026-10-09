using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    [InitializeOnLoad]
    public static class StudioChoiceLayout
    {
        private const string Pending = "Animpic.Studio.ChoiceLayoutPending";
        static StudioChoiceLayout()
        {
            EditorApplication.playModeStateChanged += state => {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
                { SessionState.SetBool(Pending, false); EditorApplication.delayCall += Apply; }
            };
        }
        public static void Apply()
        {
            var ui = Object.FindObjectOfType<CharacterStudioUI>();
            if (!ui || !ui.uiCanvas) throw new InvalidOperationException("Open the existing CharacterStudio scene.");
            int count = 0;
            foreach (var group in ui.uiCanvas.GetComponentsInChildren<HorizontalLayoutGroup>(true))
            {
                var value = group.transform.Find("Selected value"); if (!value) continue;
                group.childForceExpandWidth = false;
                var middle = value.GetComponent<LayoutElement>(); middle.minWidth = 0; middle.preferredWidth = 0; middle.flexibleWidth = 1;
                value.GetComponentInChildren<Text>().fontSize = 16;
                foreach (var button in group.GetComponentsInChildren<Button>(true))
                {
                    var size = button.GetComponent<LayoutElement>(); size.minWidth = 44; size.preferredWidth = 44; size.flexibleWidth = 0;
                    button.GetComponentInChildren<Text>().fontSize = 20;
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform); count++;
            }
            Canvas.ForceUpdateCanvases();
            if (EditorApplication.isPlaying) SessionState.SetBool(Pending, true);
            else
            {
                var scene = ui.gameObject.scene; EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save updated UI layout.");
            }
            string output = Path.GetFullPath("Library/CharacterStudioValidation/choice-layout"); Directory.CreateDirectory(output);
            CharacterStudioSetup.Render(ui, Path.Combine(output, "preview.png"));
            var rows = ui.uiCanvas.GetComponentsInChildren<HorizontalLayoutGroup>(false).Where(g => g.transform.Find("Selected value"));
            File.WriteAllLines(Path.Combine(output, "dimensions.txt"), rows.Select(g => g.name + ": " + string.Join(", ", Enumerable.Range(0, g.transform.childCount).Select(i => {
                var child = (RectTransform)g.transform.GetChild(i); return child.name + "=" + child.rect.width.ToString("F1");
            }))));
            Debug.Log("Updated " + count + " choice rows: compact arrows and expanding option labels.");
        }
    }
}
