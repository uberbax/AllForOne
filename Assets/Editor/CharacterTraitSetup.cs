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
public static class CharacterTraitSetup
{
    private const string Vendor = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Prefabs/Prefabs~DemoScenes/";
    private const string Output = "Assets/System/UI/Prefabs/";
    private const string Request = "work/trait-setup.request";
    private const string Report = "work/trait-setup-result.txt";
    static CharacterTraitSetup() { EditorApplication.delayCall += RunRequested; }
    private static void RunRequested()
    {
        if (File.Exists("work/trait-cost-update.request"))
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RunRequested; return; }
            File.Delete("work/trait-cost-update.request");
            try { UpdateCostArea(); }
            catch (Exception e) { File.WriteAllText("work/trait-cost-update-result.txt", e.ToString()); Debug.LogException(e); }
        }
        if (File.Exists("work/trait-verify.request"))
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall += RunRequested; return; }
            File.Delete("work/trait-verify.request");
            try { CharacterTraitPreview.Verify(); }
            catch (Exception e) { File.WriteAllText("work/trait-verify-result.txt", e.ToString()); Debug.LogException(e); }
        }
        if (!File.Exists(Request)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += RunRequested; return; }
        File.Delete(Request);
        try { BuildAndPlace(); }
        catch (Exception e) { File.WriteAllText(Report, e.ToString()); Debug.LogException(e); }
    }

    [MenuItem("Tools/Character Traits/Build and place in Game_Exp")]
    public static void BuildAndPlace()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/Game_Exp.unity");
        if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Game_Exp must be open.");
        var canvas = scene.GetRootGameObjects().Single(g => g.name == "Canvas" && g.GetComponent<Canvas>() != null);
        if (canvas.transform.Find("UI_CharacterTrait") != null)
            throw new InvalidOperationException("UI_CharacterTrait already exists; do not replace it implicitly.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Character_Trait.prefab") != null ||
            AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Character_Trait_Enhance.prefab") != null)
            throw new InvalidOperationException("Configured trait assets already exist; do not overwrite them implicitly.");
        CharacterTraitChecks.Run();
        BuildEnhance();
        BuildTraits();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Character_Trait.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Place character traits in Canvas");
        instance.name = "UI_CharacterTrait";
        Stretch(instance.GetComponent<RectTransform>());
        instance.transform.SetAsLastSibling();
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        File.WriteAllText(Report, "SUCCESS\nCanvas/UI_CharacterTrait\n" + Output + "Character_Trait.prefab\n" +
            Output + "Character_Trait_Enhance.prefab\n10 nodes, confirmation popup and all references assigned.\nProgression checks passed; scene saved.\n");
        Debug.Log("Character traits created; progression checks passed.", instance);
    }

    private static void BuildEnhance()
    {
        var root = PrefabUtility.LoadPrefabContents(Vendor + "Character_Trait_Enhance.prefab");
        try
        {
            Prepare(root);
            var popup = root.transform.Find("Popup");
            var frame = popup.Find("TraitFrame_01");
            PrepareFrame(frame);
            var area = popup.Find("Button_Layout_TitleIconText");
            area.Find("Button_02_Red/Text (TMP)").gameObject.SetActive(false);
            var title = area.Find("Text_Title").GetComponent<TMP_Text>();
            title.text = "Enhance";
            title.enableAutoSizing = true; title.fontSizeMin = 20; title.fontSizeMax = 38;
            title.rectTransform.anchorMin = new Vector2(0, 0.42f);
            title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = new Vector2(10, 0);
            title.rectTransform.offsetMax = new Vector2(-10, -4);
            var view = root.AddComponent<CharacterTraitEnhance>();
            var data = new SerializedObject(view);
            Set(data, "levelText", frame.Find("Text (TMP)").GetComponent<TMP_Text>());
            Set(data, "icon", frame.Find("Icon").GetComponent<Image>());
            Set(data, "currentText", popup.Find("List_1/Text (TMP)").GetComponent<TMP_Text>());
            Set(data, "nextText", popup.Find("List_2/Text (TMP)").GetComponent<TMP_Text>());
            Set(data, "confirmText", title);
            Set(data, "confirmButton", AddButton(area));
            Set(data, "closeButton", AddButton(popup.Find("Button_Close_02")));
            data.ApplyModifiedPropertiesWithoutUndo();
            ConfigureCostArea(root);
            foreach (var path in new[] { "List_1/Text (TMP)", "List_2/Text (TMP)" })
            {
                var text = popup.Find(path).GetComponent<TMP_Text>();
                text.enableAutoSizing = true; text.fontSizeMin = 22; text.fontSizeMax = 35;
            }
            root.transform.Find("Dimmed").GetComponent<Image>().raycastTarget = true;
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, Output + "Character_Trait_Enhance.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    [MenuItem("Tools/Character Traits/Update cost GroupArea")]
    public static void UpdateCostArea()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var path = Output + "Character_Trait_Enhance.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ConfigureCostArea(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        File.WriteAllText("work/trait-cost-update-result.txt", "SUCCESS: GroupArea/Group (gold) and GroupArea/Res1 (res1); TraitCost disabled; runtime icons use ResourceHolder.GetIcon(itemId).\n");
    }

    private static void ConfigureCostArea(GameObject root)
    {
        var area = root.transform.Find("Popup/Button_Layout_TitleIconText");
        area.Find("TraitCost")?.gameObject.SetActive(false);
        var groupArea = (RectTransform)area.Find("GroupArea");
        groupArea.gameObject.SetActive(true);
        groupArea.anchorMin = Vector2.zero;
        groupArea.anchorMax = new Vector2(1, 0.45f);
        groupArea.offsetMin = new Vector2(8, 4);
        groupArea.offsetMax = new Vector2(-8, 0);
        var gold = groupArea.Find("Group");
        var res1 = groupArea.Find("Res1");
        if (res1 == null)
        {
            res1 = UnityEngine.Object.Instantiate(gold.gameObject, groupArea).transform;
            res1.name = "Res1";
        }
        var layout = groupArea.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18; layout.padding = new RectOffset();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var data = new SerializedObject(root.GetComponent<CharacterTraitEnhance>());
        Set(data, "groupArea", groupArea);
        ConfigureCostGroup(data.FindProperty("goldCost"), gold, "gold", "100");
        ConfigureCostGroup(data.FindProperty("res1Cost"), res1, "res1", "10");
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureCostGroup(SerializedProperty row, Transform group, string itemId, string amount)
    {
        group.gameObject.SetActive(true);
        var icon = group.Find("Icon").GetComponent<Image>();
        var text = group.Find("Text").GetComponent<TMP_Text>();
        var holder = UnityEngine.Object.FindAnyObjectByType<ResourceHolder>(FindObjectsInactive.Include);
        icon.sprite = holder != null ? holder.GetIcon(itemId) : null;
        icon.enabled = icon.sprite != null;
        icon.preserveAspect = true; icon.raycastTarget = false;
        icon.rectTransform.localScale = Vector3.one;
        var iconLayout = icon.GetComponent<LayoutElement>() ?? icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.minWidth = iconLayout.preferredWidth = 32;
        iconLayout.minHeight = iconLayout.preferredHeight = 32;
        iconLayout.flexibleWidth = iconLayout.flexibleHeight = 0;
        text.text = amount; text.fontSize = 28; text.fontSizeMax = 28; text.fontSizeMin = 16;
        text.enableAutoSizing = true; text.raycastTarget = false; text.alignment = TextAlignmentOptions.Center;
        var textLayout = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
        textLayout.minWidth = 38; textLayout.preferredWidth = 65; textLayout.flexibleWidth = 0;
        var layout = group.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 5; layout.padding = new RectOffset();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        row.FindPropertyRelative("group").objectReferenceValue = group.gameObject;
        row.FindPropertyRelative("icon").objectReferenceValue = icon;
        row.FindPropertyRelative("amountText").objectReferenceValue = text;
    }

    private static void BuildTraits()
    {
        var root = PrefabUtility.LoadPrefabContents(Vendor + "Character_Trait.prefab");
        try
        {
            Prepare(root);
            foreach (var path in new[] { "Bottom", "Tab_01_BottomFlushMenu", "ResourceBar_Group", "Top/Button_Info", "Top/Icon_Reset" })
                root.transform.Find(path)?.gameObject.SetActive(false);
            var middle = (RectTransform)root.transform.Find("Middle");
            middle.offsetMin = new Vector2(0, 50);
            middle.offsetMax = new Vector2(0, -300);
            var title = root.transform.Find("Top/Title_LineDeco_02_s/Group/Text (TMP)").GetComponent<TMP_Text>();
            title.text = "Traits";
            var balance = NewText("TraitBalance", root.transform, title, 32);
            balance.rectTransform.anchorMin = new Vector2(0, 1);
            balance.rectTransform.anchorMax = Vector2.one;
            balance.rectTransform.pivot = new Vector2(0.5f, 1);
            balance.rectTransform.anchoredPosition = new Vector2(0, -25);
            balance.rectTransform.sizeDelta = new Vector2(-80, 60);
            balance.text = "gold     res1";
            var closeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Vendor + "Character_Trait_Enhance.prefab");
            var close = UnityEngine.Object.Instantiate(closeAsset.transform.Find("Popup/Button_Close_02").gameObject, root.transform.Find("Top"));
            close.name = "CloseTraits";
            var closeRect = close.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-90, 0);
            closeRect.sizeDelta = new Vector2(80, 80);
            closeRect.localScale = Vector3.one;
            var content = root.transform.Find("Middle/ScrollRect/Viewport/Content");
            var frames = content.GetComponentsInChildren<Transform>(true).Where(t => t.name == "TraitFrame_01").ToArray();
            var definitions = CharacterTraitDefinition.Defaults();
            if (frames.Length != definitions.Length) throw new InvalidOperationException("Expected ten trait frames, found " + frames.Length);
            var nodes = new CharacterTraitNode[frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];
                frame.parent.gameObject.SetActive(true); frame.gameObject.SetActive(true);
                PrepareFrame(frame);
                var level = frame.Find("Text (TMP)").GetComponent<TMP_Text>();
                level.text = "0/10";
                var label = NewText("TraitLabel", frame, level, 24);
                label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0);
                label.rectTransform.pivot = new Vector2(0.5f, 1);
                label.rectTransform.anchoredPosition = new Vector2(0, -22);
                label.rectTransform.sizeDelta = new Vector2(300, 38);
                label.text = definitions[i].Label;
                nodes[i] = frame.gameObject.AddComponent<CharacterTraitNode>();
                var data = new SerializedObject(nodes[i]);
                Set(data, "button", AddButton(frame));
                Set(data, "label", label); Set(data, "levelText", level);
                Set(data, "icon", frame.Find("Icon").GetComponent<Image>());
                Set(data, "normal", frame.Find("Bg_Normal").gameObject);
                Set(data, "upgraded", frame.Find("Bg_FocusYellow").gameObject);
                Set(data, "upgradeArrow", frame.Find("Icon_Up").gameObject);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var t in content.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Alert_")) t.gameObject.SetActive(false);
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 80; layout.padding.bottom = 90;
            var popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Output + "Character_Trait_Enhance.prefab");
            var popup = (GameObject)PrefabUtility.InstantiatePrefab(popupPrefab, root.transform);
            Stretch(popup.GetComponent<RectTransform>()); popup.SetActive(false);
            var controller = root.AddComponent<CharacterTraitController>();
            var so = new SerializedObject(controller);
            Set(so, "enhance", popup.GetComponent<CharacterTraitEnhance>());
            Set(so, "closeButton", AddButton(close.transform));
            Set(so, "balanceText", balance);
            var array = so.FindProperty("nodes"); array.arraySize = nodes.Length;
            for (int i = 0; i < nodes.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            string[] icons = { "Gear_Weapons_Sword_01", "Economy_Heart_Red", "Consumable_Potion_01_Blue",
                "Consumable_Potion_01_Purple", "Gear_Shield_03_Blue", "Gear_Shield_03_Gold", "Gear_Weapons_Sword_02",
                "Gear_Weapons_Sword_01", "Gear_Boots_01", "Gear_Weapons_Sword_02" };
            var traits = so.FindProperty("traits");
            for (int i = 0; i < icons.Length; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/256/" + icons[i] + ".png");
                if (sprite == null) throw new InvalidOperationException("Missing trait icon " + icons[i]);
                traits.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = sprite;
                frames[i].Find("Icon").GetComponent<Image>().sprite = sprite;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); root.SetActive(true);
            // The opaque background and viewport must receive clicks/drags outside the nodes too.
            root.transform.Find("Background").GetComponent<Image>().raycastTarget = true;
            root.transform.Find("Middle/ScrollRect/Viewport").GetComponent<Image>().raycastTarget = true;
            PrefabUtility.SaveAsPrefabAsset(root, Output + "Character_Trait.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Prepare(GameObject root)
    {
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour != null && behaviour.GetType().Name == "PanelView") behaviour.enabled = false;
        foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        Stretch(root.GetComponent<RectTransform>());
    }
    private static void PrepareFrame(Transform frame)
    {
        foreach (Transform child in frame)
            if (child.name.StartsWith("Bg_")) child.gameObject.SetActive(child.name == "Bg_Normal");
        frame.Find("Icon_Lock").gameObject.SetActive(false);
        frame.Find("Icon_Up").gameObject.SetActive(false);
        frame.Find("Text (TMP)").gameObject.SetActive(true);
        frame.Find("Icon").gameObject.SetActive(true);
        frame.Find("Icon").GetComponent<Image>().color = Color.white;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero; rect.localScale = Vector3.one;
    }
    private static TMP_Text NewText(string name, Transform parent, TMP_Text template, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.layer = parent.gameObject.layer; go.transform.SetParent(parent, false);
        var text = go.GetComponent<TMP_Text>();
        text.font = template.font; text.fontSharedMaterial = template.fontSharedMaterial;
        text.fontSize = size; text.fontSizeMax = size; text.fontSizeMin = 16; text.enableAutoSizing = true;
        text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.raycastTarget = false;
        return text;
    }
    private static Button AddButton(Transform target)
    {
        var button = target.GetComponent<Button>() ?? target.gameObject.AddComponent<Button>();
        // The root hit target remains active when a node switches background.
        var hit = target.GetComponent<Image>() ?? target.gameObject.AddComponent<Image>();
        hit.color = Color.clear; hit.raycastTarget = true;
        button.targetGraphic = target.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i != hit);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }
    private static void Set(SerializedObject data, string name, UnityEngine.Object value)
    {
        var property = data.FindProperty(name);
        if (property == null || value == null) throw new InvalidOperationException("Missing trait reference " + name);
        property.objectReferenceValue = value;
    }
}
