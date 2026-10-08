#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BattlePassPrefabSetup
{
    public const string PrefabPath = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Prefabs/Prefabs~DemoScenes/Progression_Pass1.prefab";
    private const string Work = "C:/Users/s4032/Documents/Codex/2026-09-30/d-unity-projects-allforone-allforone-assets/battlepass_work";

    [InitializeOnLoadMethod]
    private static void RunRequested()
    {
        if (File.Exists(Work + "/setup.request"))
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying || !File.Exists(Work + "/setup.request")) return;
                File.Delete(Work + "/setup.request");
                try { File.WriteAllText(Work + "/result.txt", Configure()); }
                catch (Exception e) { File.WriteAllText(Work + "/result.txt", "FAIL: " + e); }
            };
        }
        string request = Work + "/inspect.request";
        if (!File.Exists(request)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlaying || !File.Exists(request)) return;
            File.Delete(request);
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(PrefabPath);
                var text = new StringBuilder();
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = AnimationUtility.CalculateTransformPath(t, root.transform);
                    text.Append(path).Append(" | active=").Append(t.gameObject.activeSelf).Append(" | ");
                    foreach (var c in t.GetComponents<Component>()) text.Append(c == null ? "Missing" : c.GetType().Name).Append(',');
                    var label = t.GetComponent<TMP_Text>();
                    if (label != null) text.Append(" | text=").Append(label.text);
                    text.AppendLine();
                }
                File.WriteAllText(Work + "/hierarchy.txt", text.ToString());
            }
            catch (Exception e) { File.WriteAllText(Work + "/hierarchy.txt", "FAIL: " + e); }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
        };
    }

    [MenuItem("Tools/Battle Pass/Configure Progression_Pass1")]
    public static void ConfigureMenu() => Debug.Log(Configure());

    private static string Configure()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Configure outside Play mode.");
        var saved = SaveStatics();
        GameObject root = null;
        var temporary = new GameObject("BattlePassConfig") { hideFlags = HideFlags.HideAndDontSave };
        temporary.SetActive(false);
        try
        {
            var resource = UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(r => r.gameObject.scene.IsValid());
            ResourceHolder.instance = resource;
            var loader = temporary.AddComponent<ConfigLoader>();
            ConfigLoader.Instance = loader;
            loader.ParseTasks(ExcelHelper.LoadExcel(Path.Combine(Application.streamingAssetsPath, "Config_exp.xlsx")).ToTSV("TASKS"));
            var tasks = loader.allTasks.Values.Where(t => BattlePassController.IsStageId(t.id)).OrderBy(BattlePassController.RequiredPoints).ToList();
            Require(tasks.Count > 0 && tasks.All(t => BattlePassController.RequiredPoints(t) >= 0), "Invalid battle pass thresholds");
            root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var content = Find(root.transform, "Middle/ScrollRect/Viewport/Content");
            var controller = root.GetComponent<BattlePassController>() ?? root.AddComponent<BattlePassController>();
            var originals = new List<Transform>();
            foreach (Transform child in content) if (child.name.StartsWith("ListItem_Pass1")) originals.Add(child);
            Require(originals.Count > 0, "Reward row template is missing");
            while (originals.Count < tasks.Count)
                originals.Add(UnityEngine.Object.Instantiate(originals[0].gameObject, content).transform);
            BattlePassLevelView template = null;
            for (int i = 0; i < originals.Count; i++)
            {
                originals[i].gameObject.SetActive(i < tasks.Count);
                if (i >= tasks.Count) continue;
                var view = ConfigureRow(originals[i]);
                if (template == null) template = view;
                view.Bind(controller, tasks[i], i + 1);
                SetEditorIcon(view, "free", tasks[i].rewards, resource);
                SetEditorIcon(view, "premium", tasks[i].rewardsPremium, resource);
            }
            Set(controller, "content", content.GetComponent<RectTransform>());
            Set(controller, "rowTemplate", template);
            Set(controller, "claimAllButton", ButtonAt(Find(root.transform, "Middle/Button_02_Red")));
            Set(controller, "closeButton", ButtonAt(Find(root.transform, "Bottom/ArrowIconButton_03_Back")));
            Set(controller, "progressSlider", Find(root.transform, "Top2/Slider_Level_01/Slider_02_Orange").GetComponent<Slider>());
            Set(controller, "progressText", TextAt(root, "Top2/Slider_Level_01/Slider_02_Orange/Text (TMP)"));
            Set(controller, "levelText", TextAt(root, "Top2/Slider_Level_01/Level/Text (TMP)"));
            Set(controller, "pointsText", TextAt(root, "Top1/ResourceBar_Group/ResourceBar_Coin/Text (TMP)"));
            Set(controller, "premiumText", TextAt(root, "Top1/Button_02_Red/Text (TMP)"));
            TextAt(root, "Middle/Button_02_Red/Text (TMP)").text = "Claim";
            foreach (var path in new[]{"Top1/Timer_01", "Top1/ResourceBar_Group/ResourceBar_Gem", "Top1/ResourceBar_Group/ResourceBar_GemStone", "Bottom/Tab_03_BoxMenu_Text/Tab_03 (1)"})
                Find(root.transform, path).gameObject.SetActive(false);
            var pointsIcon = resource != null ? resource.GetIcon(BattlePassController.PointsStat) : null;
            if (pointsIcon == null && resource != null && resource.addResources.ContainsKey(BattlePassController.PointsStat)) pointsIcon = resource.addResources[BattlePassController.PointsStat];
            if (pointsIcon != null)
            {
                Find(root.transform, "Top1/ResourceBar_Group/ResourceBar_Coin/Icon").GetComponent<Image>().sprite = pointsIcon;
                Find(root.transform, "Top2/Slider_Level_01/Icon").GetComponent<Image>().sprite = pointsIcon;
            }
            controller.RefreshState();
            Require(root.GetComponentsInChildren<BattlePassLevelView>(true).Count(v => v.gameObject.activeSelf) == tasks.Count, "All stages must be visible in the prefab");
            var asset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
            Require(success && asset != null, "Prefab save failed");
            var validation = BattlePassValidation.Validate(root, loader, tasks);
            return "PASS: Progression_Pass1 configured with " + tasks.Count + " stages. " + validation;
        }
        finally
        {
            if (root != null) PrefabUtility.UnloadPrefabContents(root);
            UnityEngine.Object.DestroyImmediate(temporary);
            RestoreStatics(saved);
        }
    }

    private static BattlePassLevelView ConfigureRow(Transform row)
    {
        var view = row.GetComponent<BattlePassLevelView>() ?? row.gameObject.AddComponent<BattlePassLevelView>();
        Set(view, "free", ConfigureTrack(row, "PassFrame_01_Left", "01", "PanelDim_Left"));
        Set(view, "premium", ConfigureTrack(row, "PassFrame_01_Right", "02", "PanelDim_Right"));
        var step = Find(row, "PassFrame_Step_01/Text_Step").GetComponent<TMP_Text>();
        Set(view, "stageText", step);
        var threshold = row.Find("PassFrame_Step_01/Text_Requirement");
        if (threshold == null)
        {
            var label = new GameObject("Text_Requirement", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(step.transform.parent, false);
            threshold = label.transform;
            var rect = (RectTransform)threshold;
            rect.sizeDelta = new Vector2(170, 42); rect.anchoredPosition = new Vector2(0, -84);
            var text = label.GetComponent<TextMeshProUGUI>();
            text.font = step.font; text.fontSharedMaterial = step.fontSharedMaterial;
            text.fontSize = 24; text.alignment = TextAlignmentOptions.Center; text.color = step.color; text.raycastTarget = false;
        }
        Set(view, "thresholdText", threshold.GetComponent<TMP_Text>());
        Set(view, "reachedStep", Find(row, "PassFrame_Step_01/Normal").gameObject);
        Set(view, "lockedStep", Find(row, "PassFrame_Step_01/Disable").gameObject);
        Find(row, "DividedLine").gameObject.SetActive(false);
        return view;
    }
    private static BattlePassLevelView.RewardTrack ConfigureTrack(Transform row, string frameName, string style, string dimName)
    {
        var frame = Find(row, frameName);
        foreach (var other in new[]{"01", "02"})
            if (other != style) { Find(frame, "Bg_" + other + "_Normal").gameObject.SetActive(false); Find(frame, "Bg_" + other + "_Complete").gameObject.SetActive(false); }
        var track = new BattlePassLevelView.RewardTrack
        {
            button = ButtonAt(frame), icon = Find(frame, "Icon").GetComponent<Image>(), amount = Find(frame, "Text (TMP)").GetComponent<TMP_Text>(),
            normal = Find(frame, "Bg_" + style + "_Normal").gameObject, complete = Find(frame, "Bg_" + style + "_Complete").gameObject,
            check = Find(frame, "Check").gameObject, locked = Find(frame, "Lock").gameObject, dim = Find(row, dimName).gameObject
        };
        track.button.targetGraphic = Find(frame, "Bg_" + style + "_Normal/Bg").GetComponent<Image>();
        track.dim.GetComponent<Image>().raycastTarget = false;
        track.icon.raycastTarget = false; track.amount.raycastTarget = false;
        return track;
    }
    private static void SetEditorIcon(BattlePassLevelView view, string name, List<Bon> rewards, ResourceHolder resource)
    {
        if (resource == null || rewards.Count == 0) return;
        var track = (BattlePassLevelView.RewardTrack)view.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);
        var icon = resource.GetIcon(rewards[0].Key);
        if (icon == null && resource.addResources.ContainsKey(rewards[0].Key)) icon = resource.addResources[rewards[0].Key];
        track.icon.sprite = icon; track.icon.enabled = icon != null;
    }
    private static Button ButtonAt(Transform t)
    {
        var button = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.targetGraphic = t.GetComponent<Image>() ?? t.GetComponentInChildren<Image>(true);
        return button;
    }
    private static TMP_Text TextAt(GameObject root, string path) => Find(root.transform, path).GetComponent<TMP_Text>();
    private static Transform Find(Transform root, string path) => root.Find(path) ?? throw new InvalidOperationException("Missing prefab binding: " + path);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target, value);
    internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    internal static Dictionary<FieldInfo, object> SaveStatics()
    {
        var result = new Dictionary<FieldInfo, object>();
        foreach (var type in new[]{typeof(ConfigLoader), typeof(MainStates), typeof(DatabaseAll), typeof(ResourceHolder), typeof(ModelStatistics), typeof(EventManager)})
            foreach (var field in type.GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic))
                if (!field.IsLiteral && !field.IsInitOnly) result[field] = field.GetValue(null);
        return result;
    }
    internal static void RestoreStatics(Dictionary<FieldInfo, object> saved) { foreach (var entry in saved) entry.Key.SetValue(null, entry.Value); }
}
#endif
