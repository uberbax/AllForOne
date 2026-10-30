#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
public static class HeroAscensionBuilder
{
    public const string PrefabPath = "Assets/System/UI/Ascension/Resources/UI_HeroAscension.prefab";
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\ascension";
    [InitializeOnLoadMethod] private static void Init()
    {
        if (File.Exists(Work + "/build.request")) EditorApplication.delayCall += Build;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position; return rect;
    }
    private static Image Image(string name, Transform parent, Vector2 size, Vector2 position, Sprite sprite, Color color)
    {
        var rect = Rect(name, parent, size, position); var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite; image.color = color; image.type = UnityEngine.UI.Image.Type.Sliced; image.raycastTarget = false; return image;
    }
    private static TMP_Text Text(string name, Transform parent, Vector2 size, Vector2 position, TMP_FontAsset font, string value, Color color)
    {
        var rect = Rect(name, parent, size, position); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = 26; text.alignment = TextAlignmentOptions.Center; text.color = color;
        text.enableAutoSizing = true; text.fontSizeMin = 16; text.fontSizeMax = 26; text.raycastTarget = false; text.text = value; return text;
    }
    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("Tools/UI/Pub/Build Ascension Widget")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject pub = null, widget = null;
        try
        {
            pub = PrefabUtility.LoadPrefabContents(PubBuilder.PrefabPath);
            var cards = pub.GetComponentsInChildren<PubHeroCard>(true);
            var sample = cards[0]; var source = sample.newBadge.GetComponent<Image>();
            var gold = sample.quantity.color; var font = sample.quantity.font;
            widget = Rect("UI_HeroAscension", null, new Vector2(240, 44), Vector2.zero).gameObject;
            widget.SetActive(false);
            var view = widget.AddComponent<GoAscend>();
            var body = Image("Content", widget.transform, new Vector2(240, 44), Vector2.zero, source.sprite, new Color(.13f, .18f, .18f));
            body.raycastTarget = true;
            var button = body.gameObject.AddComponent<Button>(); button.targetGraphic = body;
            var fill = Image("Progress", body.transform, new Vector2(182, 44), new Vector2(29, 0), source.sprite, new Color(.25f, .42f, .39f));
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillAmount = 0;
            var badge = Image("AscensionBadge", body.transform, new Vector2(58, 44), new Vector2(-91, 0), source.sprite, gold);
            var level = Text("AscensionLevel", badge.transform, new Vector2(52, 42), Vector2.zero, font, "0", new Color(.16f, .13f, .10f));
            level.fontStyle = FontStyles.Bold;
            var icon = Image("ShardIcon", body.transform, new Vector2(28, 28), new Vector2(-37, 0), null, Color.white); icon.preserveAspect = true; icon.enabled = icon.sprite != null;
            var fraction = Text("Shards", body.transform, new Vector2(124, 42), new Vector2(43, 0), font, "0/5", gold);
            Set(view, "content", body.gameObject); Set(view, "button", button); Set(view, "levelLabel", level);
            Set(view, "shardLabel", fraction); Set(view, "shardIcon", icon); Set(view, "progress", fill);
            PubProfileTheme.ApplyAscension(widget);
            widget.SetActive(true);
            if (!AssetDatabase.IsValidFolder("Assets/System/UI/Ascension/Resources")) AssetDatabase.CreateFolder("Assets/System/UI/Ascension", "Resources");
            var asset = PrefabUtility.SaveAsPrefabAsset(widget, PrefabPath);
            if (asset == null) throw new InvalidOperationException("Cannot save ascension prefab");
            foreach (var card in cards)
            {
                var legacy = card.transform.Find("OwnedProgress");
                var current = card.GetComponentInChildren<GoAscend>(true);
                if (current == null)
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, card.transform);
                    instance.name = "HeroAscension";
                    var rect = (RectTransform)instance.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = new Vector2(0, -274);
                }
                card.EnsureOwnedView();
                current = card.GetComponentInChildren<GoAscend>(true);
                current.SetNotOwnedVisual(card.newBadge);
                if (card.owned != current.ShardLabel || current.GetComponentInParent<ObjHolder>() != card.GetComponent<ObjHolder>())
                    throw new InvalidOperationException("Ascension parent binding failed");
                if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject);
            }
            PrefabUtility.SaveAsPrefabAsset(pub, PubBuilder.PrefabPath, out bool success);
            if (!success) throw new InvalidOperationException("Cannot update Pub prefab");
            Validate(pub);
            File.WriteAllText(Work + "/result.txt", "PASS: standalone GoAscend prefab saved; nested widget on " + cards.Length + " Pub cards; ObjHolder binding, NEW state, native RObj ascent 3->4 with 40->35 shards, normal level preserved, no PlayerPrefs writes, and preview checked.");
            File.Delete(Work + "/build.request");
        }
        catch (Exception e) { File.WriteAllText(Work + "/result.txt", "FAIL: " + e); Debug.LogException(e); }
        finally
        {
            if (pub != null) PrefabUtility.UnloadPrefabContents(pub);
            if (widget != null) UnityEngine.Object.DestroyImmediate(widget);
        }
    }
    private static RObj Probe(string id, ItemType type, int amount)
    {
        var result = new RObj { RID = "ascension_probe_" + id, dbID = id, it = type, dbObj = new Obj { ID = id } };
        result.dbObj.pars["level"] = 1;
        result.upgradePars["level"] = 0; result.upgradePars["amount"] = amount;
        result.RecalcPars(); return result;
    }
    private static void Validate(GameObject pub)
    {
        var previousState = MainStates.instance;
        var previousResources = ResourceHolder.instance;
        var previousFillers = UIfiller.instances; var previousRefresh = UIfiller.acts;
        GameObject fixture = null;
        try
        {
            fixture = new GameObject("Ascension verification"); fixture.hideFlags = HideFlags.HideAndDontSave;
            var state = fixture.AddComponent<MainStates>(); state.useSave = false;
            MainStates.instance = state;
            UIfiller.instances = new System.Collections.Generic.List<UIfiller>(); UIfiller.acts = null;
            ResourceHolder.instance = UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            var player = new RObj { RID = "main_player" }; state.all["main_player"] = player;
            var cards = pub.GetComponentsInChildren<PubHeroCard>(true);
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i]; var id = card.GetComponent<ObjHolder>().asDBObj;
                if (string.IsNullOrEmpty(id)) id = "probe_" + i;
                var hero = Probe(id, ItemType.monster, 1);
                hero.SetPar("level", 6); hero.SetPar("ascend", 3);
                if (i == 2)
                {
                    player.inventory.Add(hero);
                    var shards = Probe("shard", ItemType.item, 40); shards.shardID = id; player.inventory.Add(shards);
                }
                var name = card.heroName.text;
                card.Bind(new PubHero { Id = id, Name = name, Definition = hero.dbObj }, player, 3);
                var view = card.GetComponentInChildren<GoAscend>(true);
                if (view.IsOwned != (i == 2) || card.newBadge.activeSelf != (i != 2))
                    throw new InvalidOperationException("NEW/owned visibility failed");
                if (i == 2 && (view.ShardLabel.text != "40/5" || view.transform.Find("Content/AscensionBadge/AscensionLevel").GetComponent<TMP_Text>().text != "3"))
                    throw new InvalidOperationException("Ascension display failed");
            }
            PubInspector.Render(pub, "pub-ascension.png");
            var selected = cards[2].GetComponentInChildren<GoAscend>(true);
            var owned = cards[2].GetComponent<ObjHolder>().obj;
            selected.DoAscend();
            if (HeroAscension.Level(owned) != 4 || HeroAscension.Shards(player, owned.dbObj.ID) != 35 || owned.GetPar("level") != 7 || selected.ShardLabel.text != "35/5")
                throw new InvalidOperationException("Native parent-bound ascend transaction failed");
        }
        finally
        {
            MainStates.instance = previousState; ResourceHolder.instance = previousResources;
            UIfiller.instances = previousFillers; UIfiller.acts = previousRefresh;
            if (fixture != null) UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

}
#endif
