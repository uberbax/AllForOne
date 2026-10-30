#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class PubProfileTheme
{
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\pub-profile-theme";
    [InitializeOnLoadMethod] private static void Init()
    {
        if (File.Exists(Work + "/inspect.request")) EditorApplication.delayCall += Inspect;
        if (File.Exists(Work + "/apply.request")) EditorApplication.delayCall += ApplySaved;
    }
    [MenuItem("Tools/UI/Pub/Inspect Profile Theme")]
    public static void Inspect()
    {
        try
        {
            var profile = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(t => string.Equals(t.name, "UI_InsideProfile", StringComparison.OrdinalIgnoreCase));
            var data = new {
                images=profile.GetComponentsInChildren<Image>(true).Select(i=>new {path=AnimationUtility.CalculateTransformPath(i.transform,profile),sprite=AssetDatabase.GetAssetPath(i.sprite),color=new[]{i.color.r,i.color.g,i.color.b,i.color.a},type=i.type.ToString(),size=new[]{i.rectTransform.rect.width,i.rectTransform.rect.height}}).ToArray(),
                texts=profile.GetComponentsInChildren<TMP_Text>(true).Take(50).Select(t=>new{path=AnimationUtility.CalculateTransformPath(t.transform,profile),text=t.text,font=AssetDatabase.GetAssetPath(t.font),color=new[]{t.color.r,t.color.g,t.color.b,t.color.a},size=t.fontSize}).ToArray()
            };
            File.WriteAllText(Work + "/profile.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));
            PubInspector.Render(profile.gameObject,"inside-profile-theme.png");
            File.WriteAllText(Work + "/inspect.txt","PASS");File.Delete(Work+"/inspect.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/inspect.txt", "FAIL: "+e);Debug.LogException(e);}
    }
    private const string FramePath = "Assets/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Frame/ItemFrame07_f_White2 1.png";
    private static TMP_FontAsset ProfileFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Knights Quest SDF.asset");
    private static Sprite ProfileFrame => AssetDatabase.LoadAllAssetsAtPath(FramePath).OfType<Sprite>().First();
    private static void Frame(Image image, Color color)
    {
        image.sprite = ProfileFrame; image.type = Image.Type.Sliced; image.color = color;
    }
    private static void Typography(GameObject root)
    {
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = ProfileFont; text.fontSharedMaterial = text.font.material; text.color = PubUiTheme.White;
            if (new[]{"Subtitle","Status","Timer","Footer","Rule","Owned","Rarity"}.Contains(text.name)) text.color = PubUiTheme.Muted;
        }
    }
    public static void ApplyAscension(GameObject root)
    {
        Typography(root);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Content") Frame(image, Color.white);
            else if (image.name == "AscensionBadge") image.color = PubUiTheme.Blue;
            else if (image.name == "Progress") image.color = new Color(.16f, .56f, 1f, .35f);
            else if (image.name == "ShardIcon")
            {
                image.color = Color.white;
                var resources = UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
                if (resources != null && resources.items.TryGetValue("shard", out var icon)) image.sprite = icon;
                image.enabled = image.sprite != null;
            }
        }
    }
    public static void Apply(GameObject root)
    {
        Typography(root);
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            if (image.gameObject == root) { image.color = new Color(0,0,0,.945f); continue; }
            if (name.StartsWith("HeroCard_") || name == "Balance" || name == "Window") Frame(image, Color.white);
            else if (name == "Border" || name == "InnerBorder" || name == "InnerLight") image.color = Color.clear;
            else if (name.StartsWith("SoftGlow_")) image.color = new Color(.16f,.56f,1f,.10f);
            else if (name == "RevealGlow") image.color = new Color(.16f,.56f,1f,0);
            else if (name == "NewBadge" || name == "Fill" || name == "TitleDecoration") image.color = PubUiTheme.Blue;
            else if (name == "Track") image.color = new Color(.10f,.14f,.20f);
            else if (name == "RarityLine") { } // Rarity remains an independent visual identifier.
            else if (name == "RecruitResult") image.color = new Color(0,0,0,.86f);
            else image.color = Color.white;
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.GetComponentInParent<GoAscend>() != null) continue;
            if (button.targetGraphic is Image image)
                Frame(image, button.name == "Recruit" || button.name == "Continue" || button.name == "Multiplier_1" ? PubUiTheme.Blue : Color.white);
        }
        foreach (var card in root.GetComponentsInChildren<PubHeroCard>(true))
        {
            card.quantity.color = PubUiTheme.Blue;
            var mileage = card.transform.Find("MileageReward/Amount");
            if (mileage != null) mileage.GetComponent<TMP_Text>().color = PubUiTheme.White;
            if (card.newBadge != null)
                foreach(var text in card.newBadge.GetComponentsInChildren<TMP_Text>(true)) text.color=PubUiTheme.White;
        }
        foreach (var ascend in root.GetComponentsInChildren<GoAscend>(true)) ApplyAscension(ascend.gameObject);
    }
    [MenuItem("Tools/UI/Pub/Apply Profile Theme")]
    public static void ApplySaved()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject pub=null,ascension=null;
        try
        {
            ascension=PrefabUtility.LoadPrefabContents(HeroAscensionBuilder.PrefabPath);
            ApplyAscension(ascension);
            PrefabUtility.SaveAsPrefabAsset(ascension,HeroAscensionBuilder.PrefabPath,out bool ascendSaved);
            if(!ascendSaved)throw new InvalidOperationException("Cannot save ascension theme");
            pub=PrefabUtility.LoadPrefabContents(PubBuilder.PrefabPath);
            Apply(pub);
            PrefabUtility.SaveAsPrefabAsset(pub,PubBuilder.PrefabPath,out bool pubSaved);
            if(!pubSaved)throw new InvalidOperationException("Cannot save Pub theme");
            var cards = pub.GetComponentsInChildren<PubHeroCard>(true);
            for (int i=0;i<cards.Length;i++)
            {
                var card=cards[i];var view=card.GetComponentInChildren<GoAscend>(true);
                if(view==null)continue;
                card.newBadge.SetActive(i<2);
                view.transform.Find("Content").gameObject.SetActive(i==2);
                if(i==2)
                {
                    view.ShardLabel.text="40/5";
                    view.transform.Find("Content/AscensionBadge/AscensionLevel").GetComponent<TMP_Text>().text="3";
                    view.transform.Find("Content/Progress").GetComponent<Image>().fillAmount=1;
                }
            }
            PubInspector.Render(pub,"pub-profile-theme.png");
            File.WriteAllText(Work+"/apply.txt","PASS: UI_Pub and nested ascension widget styled from UI_InsideProfile: Knights Quest, white rounded frames, dark surfaces and blue accents. Existing hierarchy and recruitment settings preserved. Preview rendered.");
            File.Delete(Work+"/apply.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/apply.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(pub!=null)PrefabUtility.UnloadPrefabContents(pub);
            if(ascension!=null)PrefabUtility.UnloadPrefabContents(ascension);
        }
    }

}
#endif
