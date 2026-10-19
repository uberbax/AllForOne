#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClassBranchSwitcherSetup
{
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/work";

    [InitializeOnLoadMethod]
    private static void Requested()
    {
        if (File.Exists(Work + "/class-branch-configure.request"))
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying) return;
                File.Delete(Work + "/class-branch-configure.request");
                try { Configure(); File.WriteAllText(Work + "/class-branch-result.txt", "PASS: all six arrows bound; thief/mage/warrior navigation, centered pages, boundary behavior and repeated enable verified; Game_Exp scene saved."); }
                catch (Exception e) { File.WriteAllText(Work + "/class-branch-result.txt", "FAIL: " + e); Debug.LogException(e); }
            };
        }
        if (!File.Exists(Work + "/class-branch-inspect.request")) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying) return;
            File.Delete(Work + "/class-branch-inspect.request");
            try { Inspect(); }
            catch (Exception e) { File.WriteAllText(Work + "/class-branch-hierarchy.txt", "FAIL: " + e); }
        };
    }

    private static GameObject FindRoot() => Resources.FindObjectsOfTypeAll<GameObject>()
        .Single(g => g.scene.IsValid() && g.scene.name == "Game_Exp" && g.name.Equals("UI_classSelect", StringComparison.OrdinalIgnoreCase));

    [MenuItem("Tools/Classes/Configure branch arrows")]
    public static void Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Configure outside Play mode");
        var root = FindRoot();
        var branches = new[]{ "OneLine_thief", "OneLine_mage", "OneLine_warrior" }
            .Select(name => root.transform.Find(name)?.gameObject ?? throw new InvalidOperationException("Missing branch " + name)).ToArray();
        var previous = branches.Select(b => b.transform.Find("Image_Prev").GetComponent<Button>()).ToArray();
        var next = branches.Select(b => b.transform.Find("Image_Next").GetComponent<Button>()).ToArray();
        Require(previous.All(b => b != null) && next.All(b => b != null), "Arrow buttons missing");
        Require(previous.Concat(next).All(b => b.onClick.GetPersistentEventCount() == 0), "Arrow already has a persistent action; inspect it before binding");
        int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Connect class branch arrows");
        var controller = root.GetComponent<ClassBranchSwitcher>() ?? Undo.AddComponent<ClassBranchSwitcher>(root);
        Undo.RecordObject(controller, "Bind class branches");
        var serialized = new SerializedObject(controller);
        SetArray(serialized.FindProperty("branches"), branches);
        SetArray(serialized.FindProperty("previousButtons"), previous);
        SetArray(serialized.FindProperty("nextButtons"), next);
        serialized.ApplyModifiedProperties();
        var center = branches[1].GetComponent<RectTransform>().anchoredPosition;
        foreach (var branch in branches)
        {
            Undo.RecordObject(branch, "Show one class branch");
            var rect = branch.GetComponent<RectTransform>(); Undo.RecordObject(rect, "Center class branch");
            rect.anchoredPosition = center; branch.SetActive(branch == branches[1]);
        }
        foreach (var button in previous.Concat(next))
        {
            Undo.RecordObject(button, "Configure class arrow");
            var image = button.GetComponent<Image>();
            Undo.RecordObject(image, "Enable class arrow raycasts");
            button.targetGraphic = image; image.raycastTarget = true;
        }

        // Exercise the actual scene buttons while the screen stays in its original hidden state.
        var enable = typeof(ClassBranchSwitcher).GetMethod("OnEnable", BindingFlags.Instance|BindingFlags.NonPublic);
        var disable = typeof(ClassBranchSwitcher).GetMethod("OnDisable", BindingFlags.Instance|BindingFlags.NonPublic);
        controller.ShowBranch(1);
        enable.Invoke(controller, null);
        try
        {
            AssertPage(controller, branches, 1);
            next[1].onClick.Invoke(); AssertPage(controller, branches, 2);
            next[2].onClick.Invoke(); AssertPage(controller, branches, 2);
            previous[2].onClick.Invoke(); AssertPage(controller, branches, 1);
            previous[1].onClick.Invoke(); AssertPage(controller, branches, 0);
            previous[0].onClick.Invoke(); AssertPage(controller, branches, 0);
            next[0].onClick.Invoke(); AssertPage(controller, branches, 1);
        }
        finally { disable.Invoke(controller, null); }
        enable.Invoke(controller, null);
        try { next[1].onClick.Invoke(); AssertPage(controller, branches, 2); controller.ShowBranch(1); }
        finally { disable.Invoke(controller, null); }
        Require(branches.All(b => b.GetComponent<RectTransform>().anchoredPosition == center), "All branches must display at the same position");
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Require(EditorSceneManager.SaveScene(root.scene), "Scene save failed");
        Undo.CollapseUndoOperations(undo);
        Debug.Log("CLASS_BRANCH_PASS: actual Prev/Next buttons verified; Game_Exp saved.");
    }

    private static void SetArray(SerializedProperty array, UnityEngine.Object[] values)
    {
        array.arraySize = values.Length;
        for (int i=0;i<values.Length;i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static void AssertPage(ClassBranchSwitcher controller, GameObject[] branches, int expected)
    {
        Require(controller.CurrentIndex == expected, "Wrong selected branch");
        for (int i=0;i<branches.Length;i++) Require(branches[i].activeSelf == (i == expected), "Only the selected branch must be visible");
    }
    private static void Require(bool condition, string message) { if(!condition) throw new InvalidOperationException(message); }

    [MenuItem("Tools/Classes/Inspect class branches")]
    public static void Inspect()
    {
        var root = FindRoot();
        var text = new StringBuilder();
        text.AppendLine("Scene=" + root.scene.path + " dirty=" + root.scene.isDirty + " prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root));
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            text.Append(AnimationUtility.CalculateTransformPath(t, root.transform)).Append(" | active=").Append(t.gameObject.activeSelf).Append(" | ");
            foreach (var c in t.GetComponents<Component>()) text.Append(c == null ? "Missing" : c.GetType().Name).Append(',');
            var label = t.GetComponent<TMP_Text>(); if (label != null) text.Append(" | text=").Append(label.text);
            if (t is RectTransform rect) text.Append(" | pos=").Append(rect.anchoredPosition).Append(" size=").Append(rect.sizeDelta);
            text.AppendLine();
        }
        File.WriteAllText(Work + "/class-branch-hierarchy.txt", text.ToString());
    }
}
#endif
