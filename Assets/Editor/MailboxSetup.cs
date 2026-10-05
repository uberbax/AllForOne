using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MailboxSetup
{
    private const string Vendor = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Prefabs/";
    private const string Output = "Assets/System/UI/Prefabs/";
    private const string Request = "work/mailbox-setup.request";
    private const string Report = "work/mailbox-setup-result.txt";

    static MailboxSetup() { EditorApplication.delayCall += RunRequested; }

    private static void RunRequested()
    {
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += RunRequested; return; }
        File.Delete(Request);
        try { BuildAndPlace(); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Mailbox/Build and place in Game_Exp")]
    public static void BuildAndPlace()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Game_Exp.unity");
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Game_Exp must be open.");
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas" && g.GetComponent<Canvas>() != null);
        if (canvas.transform.Find("UI_Mailbox") != null) throw new InvalidOperationException("UI_Mailbox is already in Canvas.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Mailbox.prefab") != null)
            throw new InvalidOperationException("Mailbox.prefab already exists; do not overwrite it implicitly.");
        var rowSource = PrefabUtility.LoadPrefabContents(Vendor + "Prefabs~DemoLayout/ListItem_Mailbox.prefab");
        try
        {
            rowSource.name = "MailItem";
            var view = rowSource.AddComponent<MailItemView>();
            var data = new SerializedObject(view);
            Set(data, "headerText", rowSource.transform.Find("Text_Description").GetComponent<TMP_Text>());
            Set(data, "timerText", rowSource.transform.Find("Timer/Text_Timer").GetComponent<TMP_Text>());
            Set(data, "rewardAmountText", rowSource.transform.Find("Text (TMP)").GetComponent<TMP_Text>());
            Set(data, "rewardIcon", rowSource.transform.Find("Icon").GetComponent<Image>());
            Set(data, "claimButton", AddButton(rowSource.transform.Find("Button_02_Red")));
            Set(data, "claimedState", rowSource.transform.Find("Stemp").gameObject);
            Set(data, "contentGroup", rowSource.AddComponent<CanvasGroup>());
            data.ApplyModifiedPropertiesWithoutUndo();
            var amount = rowSource.transform.Find("Text (TMP)").GetComponent<TMP_Text>();
            amount.fontSize = 26;
            amount.fontSizeMax = 30;
            amount.fontSizeMin = 15;
            amount.enableAutoSizing = true;
            amount.rectTransform.sizeDelta = new Vector2(180, 68);
            rowSource.transform.Find("Stemp").gameObject.SetActive(false);
            var layout = rowSource.AddComponent<LayoutElement>();
            layout.preferredHeight = 190;
            layout.minHeight = 190;
            PrefabUtility.SaveAsPrefabAsset(rowSource, Output + "MailItem.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(rowSource); }

        var root = PrefabUtility.LoadPrefabContents(Vendor + "Prefabs~DemoScenes/Rewards_Mailbox.prefab");
        try
        {
            root.name = "Mailbox";
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && behaviour.GetType().Name == "PanelView") behaviour.enabled = false;
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            var popup = root.transform.Find("Popup");
            var content = popup.Find("ScrollRect/Viewport/Content");
            foreach (Transform child in content.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            popup.Find("Button_DeleteAll").gameObject.SetActive(false);
            // These example tabs have no corresponding category in FormatMail.
            foreach (var text in popup.GetComponentsInChildren<TMP_Text>(true))
                if (text.text == "News" || text.text == "Combat" || text.text == "Guild")
                {
                    var tab = text.transform;
                    while (tab.parent != null && tab.parent != popup) tab = tab.parent;
                    tab.gameObject.SetActive(false);
                }
            var empty = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Empty");
            if (empty == null) throw new InvalidOperationException("Empty state missing.");
            empty.gameObject.SetActive(true);
            var claimAll = AddButton(popup.Find("Button_ClaimAll/Button_02_Red"));
            var claimLabel = claimAll.GetComponentInChildren<TMP_Text>(true);
            claimLabel.text = "Claim All";
            var close = AddButton(popup.Find("Button_Close_02"));
            var controller = root.AddComponent<MailController>();
            var data = new SerializedObject(controller);
            Set(data, "content", content);
            Set(data, "rowPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(Output + "MailItem.prefab").GetComponent<MailItemView>());
            Set(data, "emptyState", empty.gameObject);
            Set(data, "claimAllButton", claimAll);
            Set(data, "closeButton", close);
            data.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, Output + "Mailbox.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Mailbox.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Place Mailbox in Canvas");
        instance.name = "UI_Mailbox";
        instance.transform.SetAsLastSibling();
        var ir = instance.GetComponent<RectTransform>();
        ir.anchorMin = Vector2.zero;
        ir.anchorMax = Vector2.one;
        ir.offsetMin = ir.offsetMax = Vector2.zero;
        ir.localScale = Vector3.one;
        Selection.activeGameObject = instance;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ir);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Report, "SUCCESS\nCanvas/UI_Mailbox\n" + Output + "Mailbox.prefab\n" + Output + "MailItem.prefab\nScene saved; UI references assigned.\n" + string.Join("\n", instance.GetComponentsInChildren<Transform>(true).Select(t => PathOf(t, instance.transform))));
        Debug.Log("Mailbox created and placed at Canvas/UI_Mailbox.", instance);
    }

    private static void Set(SerializedObject data, string name, UnityEngine.Object value)
    {
        var property = data.FindProperty(name);
        if (property == null) throw new InvalidOperationException("Missing serialized field " + name);
        property.objectReferenceValue = value;
    }

    private static Button AddButton(Transform target)
    {
        if (target == null) throw new InvalidOperationException("Button target missing.");
        var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        button.targetGraphic = target.GetComponentInChildren<Image>(true);
        if (button.targetGraphic != null) button.targetGraphic.raycastTarget = true;
        return button;
    }

    private static string PathOf(Transform target, Transform root)
    {
        if (target == root) return target.name;
        return PathOf(target.parent, root) + "/" + target.name;
    }
}
