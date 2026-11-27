using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace BRATANDRONIKLIB
{
    [CustomEditor(typeof(BRATGUITutorialSwitch))]
    public sealed class BRATGUITutorialSwitchEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var list = serializedObject.FindProperty("pageObjects");
            if (!Application.isPlaying && list.arraySize > 0)
                DrawPagePreview(list);
            DrawDefaultInspector();
        }

        private void DrawPagePreview(SerializedProperty list)
        {
            var pages = new List<GameObject>(list.arraySize);
            int active = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                var page = list.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                if (!page || pages.Contains(page)) continue;
                if (page.activeSelf) active = pages.Count;
                pages.Add(page);
            }
            if (pages.Count == 0) return;
            var names = pages.ConvertAll(page => page.name).ToArray();

            EditorGUI.BeginChangeCheck();
            int selected = EditorGUILayout.Popup("Preview page", active, names);
            if (!EditorGUI.EndChangeCheck() || selected < 0) return;

            Undo.IncrementCurrentGroup();
            int undo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Preview battle tutorial page");
            for (int i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                Undo.RecordObject(page, "Preview tutorial page");
                page.SetActive(i == selected);
                if (page.TryGetComponent<CanvasGroup>(out var group))
                {
                    Undo.RecordObject(group, "Preview tutorial page");
                    group.alpha = 1f;
                }
            }
            SetControlVisible("previousControl", selected > 0);
            SetControlVisible("nextControl", true);
            var forward = serializedObject.FindProperty("nextControl").objectReferenceValue as GameObject;
            if (forward && forward.GetComponentInChildren<TMP_Text>(true) is TMP_Text label)
            {
                Undo.RecordObject(label, "Preview tutorial navigation");
                label.text = BRATGUITutorialSwitch.ForwardCaption(selected, pages.Count);
            }
            Undo.CollapseUndoOperations(undo);
            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
        }

        private void SetControlVisible(string property, bool visible)
        {
            var control = serializedObject.FindProperty(property).objectReferenceValue as GameObject;
            if (!control) return;
            Undo.RecordObject(control, "Preview tutorial navigation");
            control.SetActive(visible);
        }
    }
}
