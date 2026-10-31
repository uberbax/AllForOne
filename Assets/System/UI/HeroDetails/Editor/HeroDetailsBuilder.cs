#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HeroDetailsBuilder
{
    public const string Root = "Assets/System/UI/HeroDetails";
    public const string PrefabPath = Root + "/Resources/UI_HeroDetails.prefab";
    private const string Work = @"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\hero-details";
    private static readonly Color Ink = new Color(.035f,.045f,.065f), Card = new Color(.065f,.085f,.12f), Blue = new Color(.16f,.56f,1), Muted = new Color(.61f,.68f,.77f);
    private static TMP_FontAsset font;
    private static Sprite frame;
    [InitializeOnLoadMethod] private static void Init()
    { if (File.Exists(Work + "/build.request")) EditorApplication.delayCall += Build; }
    private static void Set(UnityEngine.Object obj, string name, UnityEngine.Object value)
    { var data = new SerializedObject(obj); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void Number(UnityEngine.Object obj, string name, int value)
    { var data = new SerializedObject(obj); data.FindProperty(name).intValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void String(UnityEngine.Object obj, string name, string value)
    { var data = new SerializedObject(obj); data.FindProperty(name).stringValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
        var rect = (RectTransform)go.transform; rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,1); rect.sizeDelta = new Vector2(w,h); rect.anchoredPosition = new Vector2(x,-y); return rect;
    }
    private static Image Box(string name, Transform parent, float x, float y, float w, float h, Color color, bool framed = false)
    {
        var image = Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false;
        if(framed) { image.sprite=frame; image.type=Image.Type.Sliced; }
        return image;
    }
    private static TMP_Text Label(string name,Transform parent,float x,float y,float w,float h,string text,float size,Color color,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft)
    {
        var label=Rect(name,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>(); label.font=font; label.fontSize=size; label.color=color;
        label.alignment=align; label.text=text; label.raycastTarget=false; label.overflowMode=TextOverflowModes.Ellipsis;
        label.enableAutoSizing=true; label.fontSizeMin=size*.75f; label.fontSizeMax=size; return label;
    }
    private static HeroDetailsPart Part(GameObject go,HeroDetailsPart.Content kind,TMP_Text title=null,TMP_Text value=null,TMP_Text detail=null,Image icon=null,Image background=null,int index=0)
    {
        var part=go.AddComponent<HeroDetailsPart>(); Number(part,"content",(int)kind); Number(part,"index",index);
        Set(part,"title",title); Set(part,"value",value); Set(part,"detail",detail); Set(part,"icon",icon); Set(part,"background",background);
        if(icon!=null&&(kind==HeroDetailsPart.Content.Skill||kind==HeroDetailsPart.Content.Milestone))
        {
            var placeholder=Label("Placeholder",icon.transform,0,0,icon.rectTransform.sizeDelta.x,icon.rectTransform.sizeDelta.y,(index+1).ToString("00"),kind==HeroDetailsPart.Content.Skill?38:23,Color.white,TextAlignmentOptions.Center);
            Set(part,"iconPlaceholder",placeholder);
        }
        return part;
    }
    private static Button Button(string name,Transform parent,float x,float y,float w,float h,string text,Color color)
    {
        var image=Box(name,parent,x,y,w,h,color,true); image.raycastTarget=true; var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.pressedColor=new Color(.65f,.75f,.9f); colors.disabledColor=new Color(.5f,.5f,.5f,.8f); button.colors=colors;
        Label("Label",image.transform,0,0,w-24,h,text,42,Color.white,TextAlignmentOptions.Center); return button;
    }
    [MenuItem("Tools/UI/Hero Details/Build Prefab")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        GameObject root=null; Scene preview=default;
        try
        {
            Directory.CreateDirectory(Work);
            foreach(var png in Directory.GetFiles(Root+"/Resources/HeroSkillIcons","*.png"))
            {
                AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.maxTextureSize=256;importer.SaveAndReimport();
            }
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Knights Quest SDF.asset");
            frame=AssetDatabase.LoadAllAssetsAtPath("Assets/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Frame/ItemFrame07_f_White2 1.png").OfType<Sprite>().First();
            if(font==null)throw new InvalidOperationException("Missing profile font");
            preview=EditorSceneManager.NewPreviewScene();
            root=new GameObject("UI_HeroDetails",typeof(RectTransform)); SceneManager.MoveGameObjectToScene(root,preview); root.layer=5; root.SetActive(false);
            var rect=(RectTransform)root.transform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.sizeDelta=new Vector2(1080,1920);
            var shade=root.AddComponent<Image>();shade.color=new Color(.005f,.01f,.02f,.94f);shade.raycastTarget=true;
            var holder=root.AddComponent<ObjHolder>();holder.noTrack=true;
            var controller=root.AddComponent<HeroDetailsController>();
            var layout=root.AddComponent<HeroDetailsLayout>();
            var design=Rect("Layout",root.transform,0,0,1080,1920);design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,.5f);design.anchoredPosition=Vector2.zero;Set(layout,"content",design);
            Label("Section",design,-280,95,390,55,"HERO DETAILS",32,Color.white);
            Label("Subtitle",design,-280,145,390,45,"LEVEL & SKILLS",23,Muted);
            var close=Button("Close",design,435,92,86,72,"X",Card); Set(controller,"closeButton",close);
            // A portrait above the modal mirrors the reference without duplicating hero assets.
            var splash=Box("HeroArtwork",design,0,125,430,345,Color.white);splash.preserveAspect=true;
            Part(splash.gameObject,HeroDetailsPart.Content.Header,icon:splash);
            var window=Box("Window",design,0,445,960,1410,Color.white,true);
            Box("Interior",window.transform,0,18,924,1374,Ink);
            Box("Accent",window.transform,-432,48,5,160,Blue);
            var portraitFrame=Box("PortraitFrame",window.transform,-328,48,176,176,Blue,true);
            var portrait=Box("Portrait",portraitFrame.transform,0,12,152,152,Color.white);portrait.preserveAspect=true;
            var name=Label("HeroName",window.transform,104,95,580,62,"Hero name",43,Color.white);
            var level=Label("Level",window.transform,80,47,530,43,"LEVEL —",29,Blue);
            var ascension=Label("Ascension",window.transform,80,166,530,35,"ASCENSION —",23,Muted);
            var header=Part(window.gameObject,HeroDetailsPart.Content.Header,name,level,ascension,portrait);
            var track=Box("LevelTrack",window.transform,96,212,552,7,new Color(.16f,.2f,.26f));
            var fill=Box("Fill",track.transform,0,0,552,7,Blue);fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillOrigin=0; Set(header,"fill",fill);
            string[] parameters={"battle_power","attack","max_health"};string[] captions={"POWER","ATK","HP"};
            for(int i=0;i<3;i++)
            {
                var stat=Box(captions[i],window.transform,(i-1)*287,253,268,101,Card,true);
                Label("Title",stat.transform,0,9,230,29,captions[i],22,Muted);
                var value=Label("Value",stat.transform,0,38,230,49,"—",38,Color.white);
                var part=Part(stat.gameObject,HeroDetailsPart.Content.Stat,value:value);String(part,"parameter",parameters[i]);
            }
            Label("ProgressionTitle",window.transform,-196,382,440,40,"SKILL PROGRESSION",26,Color.white);
            Label("ProgressionHint",window.transform,302,385,225,35,"EVERY 10 LEVELS",20,Blue,TextAlignmentOptions.Right);
            var viewport=Rect("Milestones",window.transform,0,437,866,442);var viewportImage=viewport.gameObject.AddComponent<Image>();viewportImage.color=Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=Rect("Content",viewport,0,0,866,20*76);content.anchorMin=content.anchorMax=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45;
            for(int i=0;i<20;i++)
            {
                var row=Box("Level_"+((i+1)*10),content,0,i*76,866,68,Card,true);
                Label("Number",row.transform,-376,0,82,68,((i+1)*10).ToString(),31,Blue,TextAlignmentOptions.Center);
                Box("Divider",row.transform,-321,14,2,40,new Color(.25f,.36f,.49f));
                var icon=Box("SkillIcon",row.transform,-277,10,48,48,Color.white);icon.preserveAspect=true;
                var title=Label("SkillName",row.transform,-27,7,424,53,"Skill name",27,Color.white);
                var status=Label("State",row.transform,300,5,221,55,"LOCKED",19,Muted,TextAlignmentOptions.Right);
                var part=Part(row.gameObject,HeroDetailsPart.Content.Milestone,title:title,detail:status,icon:icon,background:row,index:i%2);Number(part,"milestoneLevel",(i+1)*10);
            }
            Label("ScrollHint",window.transform,0,888,800,30,"SCROLL TO SEE MORE",18,Muted,TextAlignmentOptions.Center);
            for(int i=0;i<2;i++)
            {
                var card=Box("Skill_"+(i+1),window.transform,(i==0?-223:223),944,426,171,Card,true);
                var iconBg=Box("IconFrame",card.transform,-126,20,128,128,Blue,true);
                var icon=Box("Icon",iconBg.transform,0,13,102,102,Color.white);icon.preserveAspect=true;
                var title=Label("Name",card.transform,79,50,230,65,"Skill",28,Color.white);
                var badge=Label("Badge",card.transform,79,17,230,30,"SKILL "+(i+1),20,Muted);
                var rank=Label("Rank",card.transform,79,115,230,34,"LOCKED",23,Blue);
                Part(card.gameObject,HeroDetailsPart.Content.Skill,title,badge,rank,icon,card,i);
            }
            Label("CostTitle",window.transform,-212,1148,440,34,"NEXT LEVEL COST",22,Muted);
            for(int i=0;i<3;i++)
            {
                var cost=Box("Cost_"+i,window.transform,(i-1)*287,1190,268,70,Card,true);
                var icon=Box("Icon",cost.transform,-94,12,46,46,Color.white);icon.preserveAspect=true;
                var amount=Label("Amount",cost.transform,28,2,175,37,"—",29,Color.white);
                var balance=Label("Balance",cost.transform,28,38,175,26,"",17,Muted);
                Part(cost.gameObject,HeroDetailsPart.Content.Cost,value:amount,detail:balance,icon:icon,background:cost,index:i);
            }
            var upgrade=Button("Upgrade",window.transform,-70,1280,690,83,"UPGRADE",Blue);Set(controller,"upgradeButton",upgrade);
            var toggleBg=Box("Max",window.transform,350,1280,130,83,Card,true);toggleBg.raycastTarget=true;
            var toggle=toggleBg.gameObject.AddComponent<Toggle>();toggle.targetGraphic=toggleBg;
            var check=Box("Check",toggleBg.transform,0,8,44,22,Blue);toggle.graphic=check;toggle.isOn=false;Set(controller,"maxToggle",toggle);
            Label("MaxLabel",toggleBg.transform,0,31,110,42,"MAX",25,Color.white,TextAlignmentOptions.Center);
            var statusLabel=Label("UpgradeStatus",window.transform,0,1365,850,35,"Select a hero",20,Muted,TextAlignmentOptions.Center);
            Part(statusLabel.gameObject,HeroDetailsPart.Content.UpgradeStatus,value:statusLabel);
            if(!AssetDatabase.IsValidFolder(Root+"/Resources"))AssetDatabase.CreateFolder(Root,"Resources");
            // Stretch when instantiated below an existing Canvas. No additional Canvas/EventSystem is needed.
            rect.sizeDelta=Vector2.zero;
            root.SetActive(true);
            var asset=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);if(asset==null)throw new InvalidOperationException("Prefab save failed");
            Validate(asset);
            Render(root);
            File.WriteAllText(Work+"/result.txt","PASS: prefab saved with parent ObjHolder, 20 alternating milestones (10..200), two skill views, 3 stats, price icons, owned-only native upgrade, MAX toggle and close button. Milestone boundaries and hierarchy validated. Preview rendered. No live player or save changed.");
            File.Delete(Work+"/build.request");
        }
        catch(Exception error){File.WriteAllText(Work+"/result.txt","FAIL: "+error);Debug.LogException(error);}
        finally{if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);}
    }
    private static void Validate(GameObject asset)
    {
        for(int level=1;level<=210;level++)
        {
            int a=0,b=0;
            for(int l=10;l<=level;l+=10){if((l/10)%2==1)a++;else b++;}
            if(HeroDetailsController.UnlockedRank(level,0)!=a||HeroDetailsController.UnlockedRank(level,1)!=b)throw new Exception("Rank boundary "+level);
        }
        for(int level=10;level<=200;level+=10)
            if(HeroDetailsController.SkillIndex(level)!=(level/10-1)%2||HeroDetailsController.SkillRank(level)!=(level/10-1)/2+1)throw new Exception("Milestone mapping "+level);
        var parts=asset.GetComponentsInChildren<HeroDetailsPart>(true);
        if(parts.Length!=31)throw new Exception("Unexpected view count "+parts.Length);
        foreach(var part in parts)if(part.GetComponentInParent<ObjHolder>(true)!=asset.GetComponent<ObjHolder>())throw new Exception("Parent ObjHolder binding failed: "+part.name);
        foreach(var script in asset.GetComponentsInChildren<MonoBehaviour>(true))if(script==null)throw new Exception("Missing script");
        if(asset.GetComponentsInChildren<ScrollRect>(true).Length!=1)throw new Exception("Missing scroll");
    }
    private static void Render(GameObject root)
    {
        var rect=(RectTransform)root.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(1080,1920);rect.position=Vector3.zero;
        root.GetComponent<HeroDetailsLayout>().Fit();
        var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        var portrait=AssetDatabase.LoadAllAssetsAtPath("Assets/System/SkillS/Avatars/winter_ice.png").OfType<Sprite>().FirstOrDefault();
        foreach(var p in root.GetComponentsInChildren<HeroDetailsPart>(true))
        {
            var data=new SerializedObject(p);
            if(data.FindProperty("content").intValue==(int)HeroDetailsPart.Content.Header)Set(p,"fallbackIcon",portrait);
        }
        var hero=new RObj{dbID="winter_ice",it=ItemType.monster,dbObj=new Obj{ID="winter_ice"}};
        hero.dbObj.skills.Add("frost_nova");hero.dbObj.skills.Add("blizzard");
        hero.curPars["level"]=27;hero.curPars["attack"]=275;hero.curPars["max_health"]=1556;hero.curPars["def"]=44;hero.curPars["ascend"]=3;
        root.GetComponent<ObjHolder>().obj=hero;root.GetComponent<HeroDetailsController>().Refresh();
        var shownName=root.transform.Find("Layout/Window/HeroName").GetComponent<TMP_Text>();
        if(shownName.text!="Winter Ice")throw new Exception("Hero binding did not populate name");
        var levelLabel=root.transform.Find("Layout/Window/Level").GetComponent<TMP_Text>();
        if(levelLabel.text!="LEVEL 27")throw new Exception("Level binding failed");
        root.GetComponent<ObjHolder>().obj=null;root.GetComponent<HeroDetailsController>().Refresh();
        if(shownName.text!="Select a hero")throw new Exception("Stale hero data after clearing ObjHolder");
        root.GetComponent<ObjHolder>().obj=hero;root.GetComponent<HeroDetailsController>().Refresh();
        // Representative price text only on this preview instance, never on the saved prefab/player.
        var previewCost=root.transform.Find("Layout/Window/Cost_0");previewCost.GetComponent<Image>().enabled=true;
        previewCost.Find("Amount").GetComponent<TMP_Text>().text="2700 gold";previewCost.Find("Balance").GetComponent<TMP_Text>().text="NEXT LEVEL";
        root.transform.Find("Layout/Window/Upgrade").GetComponent<Button>().interactable=true;
        var camGo=new GameObject("Hero details preview",typeof(Camera));SceneManager.MoveGameObjectToScene(camGo,root.scene);
        var camera=camGo.GetComponent<Camera>();camera.scene=root.scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(root.scene);
        camera.orthographic=true;camera.orthographicSize=960;camera.transform.position=new Vector3(0,0,-1500);camera.farClipPlane=3000;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Ink;
        var rt=new RenderTexture(720,1280,24);var old=RenderTexture.active;Texture2D texture=null;
        try{camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;texture=new Texture2D(720,1280,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,720,1280),0,0);texture.Apply();File.WriteAllBytes(Work+"/preview.png",texture.EncodeToPNG());}
        finally{RenderTexture.active=old;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);}
    }
}
#endif
