#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PubBuilder
{
    public const string Root = "Assets/System/UI/Pub";
    public const string PrefabPath = Root + "/Prefabs/UI_Pub.prefab";
    const string Shared = "Assets/Layer Lab/GUI Pro-MinimalGame/Shared/";
    const string Dark = "Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/";
    static readonly Color Ink = new Color(.84f,.83f,.82f), Gold = new Color(.83f,.66f,.42f);
    static TMP_FontAsset body, title;
    static Sprite buttonBg, cardBg, cardBorder, cardInner;
    [InitializeOnLoadMethod] static void Init()
    {
        if (File.Exists(PubInspector.Work + "/build.request")) EditorApplication.delayCall += Requested;
    }
    static void Requested()
    {
        if (EditorApplication.isPlaying) { File.WriteAllText(PubInspector.Work+"/build.txt","NEED_EDIT_MODE"); return; }
        File.Delete(PubInspector.Work+"/build.request");
        try { Build(); } catch (Exception e) { File.WriteAllText(PubInspector.Work+"/build.txt","FAIL "+e);Debug.LogException(e); }
    }
    static Sprite Sprite(string path)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite==null)sprite=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if(sprite==null)throw new Exception("Missing UI sprite: "+path);return sprite;
    }
    static RectTransform Rect(string name,Transform parent)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;
        var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.localScale=Vector3.one;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;return rect;
    }
    static void Place(RectTransform rect,Vector2 size,Vector2 pos,Vector2? anchor=null)
    {
        rect.anchorMin=rect.anchorMax=anchor??new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
        rect.sizeDelta=size;rect.anchoredPosition=pos;
    }
    static Image Image(string name,Transform parent,Sprite sprite,Color color)
    {
        var rect=Rect(name,parent);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=false;
        if(sprite!=null&&sprite.border.sqrMagnitude>0)image.type=UnityEngine.UI.Image.Type.Sliced;return image;
    }
    static TMP_Text Text(string name,Transform parent,string value,float size,Color? color=null,TMP_FontAsset font=null)
    {
        var rect=Rect(name,parent);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font==null?body:font;
        label.text=value;label.fontSize=size;label.color=color??Ink;label.alignment=TextAlignmentOptions.Center;
        label.enableAutoSizing=true;label.fontSizeMin=size*.72f;label.fontSizeMax=size;label.textWrappingMode=TextWrappingModes.Normal;
        label.overflowMode=TextOverflowModes.Ellipsis;label.raycastTarget=false;label.margin=Vector4.zero;return label;
    }
    static Button Button(string name,Transform parent,Vector2 size,Vector2 pos,bool primary=false,Vector2? anchor=null)
    {
        var bg=Image(name,parent,buttonBg,primary?new Color(.45f,.173f,.173f):new Color(.176f,.173f,.176f));Place(bg.rectTransform,size,pos,anchor);bg.raycastTarget=true;
        var light=Image("InnerLight",bg.transform,Sprite(Shared+"Sprite_Common/Button/Button_02_White_Light.png"),primary?new Color(.53f,.22f,.224f):new Color(.224f,.235f,.224f));light.rectTransform.offsetMin=new Vector2(6,7);light.rectTransform.offsetMax=new Vector2(-6,-7);
        var button=bg.gameObject.AddComponent<Button>();button.targetGraphic=bg;
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.12f,1.12f,1.12f);colors.pressedColor=new Color(.75f,.75f,.75f);colors.disabledColor=new Color(.45f,.45f,.45f,.8f);button.colors=colors;
        return button;
    }
    static void CurrencyButton(Button button,string text,out Image icon,out TMP_Text amount,float titleSize=38)
    {
        var label=Text("Label",button.transform,text,titleSize);Place(label.rectTransform,new Vector2(660,55),new Vector2(0,23));
        var cost=Rect("Cost",button.transform);Place(cost,new Vector2(220,48),new Vector2(0,-34));
        icon=Image("Icon",cost,null,Color.white);icon.preserveAspect=true;Place(icon.rectTransform,Vector2.one*40,new Vector2(-62,0));
        amount=Text("Amount",cost,"100",36);Place(amount.rectTransform,new Vector2(135,46),new Vector2(30,0));
    }
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Array(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values)
    {
        var list=values.ToArray();var data=new SerializedObject(target);var p=data.FindProperty(field);p.arraySize=list.Length;
        for(int i=0;i<list.Length;i++)p.GetArrayElementAtIndex(i).objectReferenceValue=list[i];data.ApplyModifiedPropertiesWithoutUndo();
    }
    static PubHeroCard Card(Transform parent,int index)
    {
        float x=(index-1)*310;
        var beam=Image("SoftGlow_"+index,parent,Sprite(Shared+"Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_Glow.png"),new Color(.52f,.39f,.20f,.13f));Place(beam.rectTransform,new Vector2(290,850),new Vector2(x,130));
        var bg=Image("HeroCard_"+(index+1),parent,cardBg,new Color(.14f,.14f,.14f));Place(bg.rectTransform,new Vector2(286,620),new Vector2(x,0));
        Image("Border",bg.transform,cardBorder,new Color(.025f,.025f,.025f));
        var inner=Image("InnerBorder",bg.transform,cardInner,new Color(.36f,.286f,.247f));
        var card=bg.gameObject.AddComponent<PubHeroCard>();
        card.highlight=Image("RevealGlow",bg.transform,Sprite(Shared+"Sprite_Common/Frame/ItemFrame/ItemFrame_01_White_Glow.png"),new Color(.83f,.66f,.42f,0));
        var line=Image("RarityLine",bg.transform,null,Gold);Place(line.rectTransform,new Vector2(210,3),new Vector2(0,-184));card.rarityLine=line;
        var portrait=Image("Portrait",bg.transform,null,Color.white);portrait.preserveAspect=true;Place(portrait.rectTransform,new Vector2(245,350),new Vector2(0,37));card.portrait=portrait;
        card.quantity=Text("Quantity",bg.transform,"×1",36,Gold);Place(card.quantity.rectTransform,new Vector2(80,50),new Vector2(-77,261));
        card.rarityLabel=Text("Rarity",bg.transform,"COMPANION",23,new Color(.64f,.58f,.50f));Place(card.rarityLabel.rectTransform,new Vector2(240,42),new Vector2(0,206));
        card.heroName=Text("HeroName",bg.transform,"Companion",35);Place(card.heroName.rectTransform,new Vector2(242,85),new Vector2(0,-131));
        card.chance=Text("Chance",bg.transform,"33.3%",29,Gold);Place(card.chance.rectTransform,new Vector2(235,44),new Vector2(0,-218));
        card.owned=Text("Owned",bg.transform,"Owned: 0",24,new Color(.64f,.60f,.57f));Place(card.owned.rectTransform,new Vector2(240,44),new Vector2(0,-274));
        var badge=Image("NewBadge",bg.transform,Sprite(Shared+"Sprite_Common/Label/Label_Tail_04_White_Bg.png"),Gold);Place(badge.rectTransform,new Vector2(144,43),new Vector2(0,-274));
        Text("Label",badge.transform,"NEW",28,new Color(.16f,.13f,.10f));card.newBadge=badge.gameObject;return card;
    }
    [MenuItem("Tools/UI/Pub/Build and Place in Canvas")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new Exception("Build Pub in Edit Mode");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Game_Exp_Rumble.unity")throw new Exception("Expected Game_Exp_Rumble; current scene preserved");
        var canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(c=>c.name=="Canvas"&&c.transform.parent==null&&c.gameObject.scene==scene);
        bool wasDirty=scene.isDirty;
        body=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Shared+"Font/AfacadFlux-ExtraBold SDF.asset");title=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Shared+"Font/AfacadFlux-ExtraBold SDF_OutlineBlack.asset");
        if(body==null||title==null)throw new Exception("Current UI fonts missing");
        buttonBg=Sprite(Shared+"Sprite_Common/Button/Button_02_White_Bg.png");
        cardBg=Sprite(Shared+"Sprite_Common/Frame/ShopFrame/ShopFrame_01~02_White_Bg.png");cardBorder=Sprite(Shared+"Sprite_Common/Frame/ShopFrame/ShopFrame_01~02_White_Border.png");cardInner=Sprite(Shared+"Sprite_Common/Frame/ShopFrame/ShopFrame_01~02_White_InnerBorder.png");
        if(!AssetDatabase.IsValidFolder(Root+"/Prefabs"))AssetDatabase.CreateFolder(Root,"Prefabs");
        var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;var savedHolder=ResourceHolder.instance;
        try
        {
            root=new GameObject("UI_Pub",typeof(RectTransform));root.layer=5;SceneManager.MoveGameObjectToScene(root,preview);
            var rootRect=(RectTransform)root.transform;rootRect.anchorMin=Vector2.zero;rootRect.anchorMax=Vector2.one;rootRect.offsetMin=rootRect.offsetMax=Vector2.zero;rootRect.localScale=Vector3.one;
            var controller=root.AddComponent<PubController>();
            var backdrop=root.AddComponent<Image>();backdrop.color=new Color(.075f,.073f,.075f,.995f);backdrop.raycastTarget=true;
            var safe=Rect("SafeArea",root.transform);var content=Rect("Design",safe);Place(content,new Vector2(1080,2160),Vector2.zero);
            var header=Rect("Header",content);Place(header,new Vector2(1000,170),new Vector2(0,-100),new Vector2(.5f,1));
            var ornament=Image("TitleDecoration",header,Sprite(Dark+"Sprites/Title/Title_LineDeco_02_s_White.png"),new Color(.34f,.28f,.23f));Place(ornament.rectTransform,new Vector2(900,28),new Vector2(0,-61));
            var heading=Text("Title",header,"Pub",64,Ink,title);Place(heading.rectTransform,new Vector2(350,90),Vector2.zero);
            var subtitle=Text("Subtitle",header,"RECRUIT COMPANIONS",26,new Color(.64f,.59f,.53f));Place(subtitle.rectTransform,new Vector2(630,50),new Vector2(0,-112));
            var close=Button("Back",header,new Vector2(76,84),new Vector2(-455,0));var backIcon=Image("Icon",close.transform,Sprite(Dark+"Sprites/Control/ArrowIconButton_03_Back.png"),Ink);backIcon.rectTransform.offsetMin=new Vector2(19,16);backIcon.rectTransform.offsetMax=new Vector2(-19,-16);backIcon.preserveAspect=true;
            var balance=Image("Balance",content,Sprite(Dark+"Sprites/HUD/ResourceBar_Bg.png"),new Color(0,0,0,.65f));Place(balance.rectTransform,new Vector2(255,64),new Vector2(362,-239),new Vector2(.5f,1));
            var balanceIcon=Image("Icon",balance.transform,null,Color.white);balanceIcon.preserveAspect=true;Place(balanceIcon.rectTransform,Vector2.one*47,new Vector2(-88,0));
            var balanceLabel=Text("Amount",balance.transform,"1,250",36);Place(balanceLabel.rectTransform,new Vector2(160,58),new Vector2(27,0));
            var bonus=Rect("BonusProgress",content);Place(bonus,new Vector2(920,156),Vector2.zero,new Vector2(.5f,.81f));
            var bonusTitle=Text("Title",bonus,"NEXT BONUS HERO",29,Gold);Place(bonusTitle.rectTransform,new Vector2(480,50),new Vector2(-210,48));bonusTitle.alignment=TextAlignmentOptions.MidlineLeft;
            var progressLabel=Text("Progress",bonus,"4 / 20",32,Gold);Place(progressLabel.rectTransform,new Vector2(200,50),new Vector2(355,48));
            var track=Image("Track",bonus,buttonBg,new Color(.14f,.13f,.13f));Place(track.rectTransform,new Vector2(900,24),new Vector2(0,2));
            var fill=Image("Fill",track.transform,buttonBg,Gold);fill.type=UnityEngine.UI.Image.Type.Filled;fill.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;fill.fillOrigin=0;fill.fillAmount=.2f;
            var rules=Text("Rule",bonus,"Every 20 recruitments — one extra hero",25,new Color(.64f,.60f,.56f));Place(rules.rectTransform,new Vector2(900,48),new Vector2(0,-47));
            var offers=Rect("Offers",content);Place(offers,new Vector2(950,620),Vector2.zero,new Vector2(.5f,.555f));
            var cards=new List<PubHeroCard>();for(int i=0;i<3;i++)cards.Add(Card(offers,i));
            var actions=Rect("Actions",content);Place(actions,new Vector2(950,540),Vector2.zero,new Vector2(.5f,.235f));
            var buttons=new List<Button>();var backgrounds=new List<Image>();
            for(int i=0;i<3;i++){var b=Button("Multiplier_"+(i+1),actions,new Vector2(134,78),new Vector2((i-1)*145,220));Text("Label",b.transform,(i+1)+"×",42);buttons.Add(b);backgrounds.Add(b.GetComponent<Image>());}
            var status=Text("Status",actions,"One of these heroes is guaranteed",26,new Color(.65f,.59f,.52f));Place(status.rectTransform,new Vector2(940,56),new Vector2(0,140));
            var recruit=Button("Recruit",actions,new Vector2(760,154),new Vector2(0,29),true);CurrencyButton(recruit,"RECRUIT",out var recruitIcon,out var recruitCost);
            var refresh=Button("InstantRefresh",actions,new Vector2(760,136),new Vector2(0,-138));CurrencyButton(refresh,"INSTANT REFRESH",out var refreshIcon,out var refreshCost,33);
            var timer=Text("Timer",actions,"New heroes in 29:41",26,new Color(.65f,.59f,.52f));Place(timer.rectTransform,new Vector2(900,50),new Vector2(0,-241));
            var footer=Text("Footer",content,"Equal chances · Companions go to your inventory",24,new Color(.47f,.44f,.41f));Place(footer.rectTransform,new Vector2(950,70),new Vector2(0,68),new Vector2(.5f,0));
            var result=Image("RecruitResult",safe,null,new Color(0,0,0,.86f));result.raycastTarget=true;
            var resultWindow=Image("Window",result.transform,cardBg,new Color(.14f,.14f,.14f));Place(resultWindow.rectTransform,new Vector2(960,700),Vector2.zero);
            Image("Border",resultWindow.transform,cardInner,new Color(.52f,.40f,.27f));
            var resultTitle=Text("Title",resultWindow.transform,"RECRUITED",52,Gold,title);Place(resultTitle.rectTransform,new Vector2(850,90),new Vector2(0,250));
            var resultSubtitle=Text("Subtitle",resultWindow.transform,"3 new companions",29);Place(resultSubtitle.rectTransform,new Vector2(850,60),new Vector2(0,178));
            var icons=new List<Image>();var names=new List<TMP_Text>();var counts=new List<TMP_Text>();
            for(int i=0;i<3;i++){
                var row=Rect("Reward_"+i,resultWindow.transform);Place(row,new Vector2(270,300),new Vector2((i-1)*295,-8));
                var icon=Image("Icon",row,null,Color.white);icon.preserveAspect=true;Place(icon.rectTransform,new Vector2(210,195),new Vector2(0,42));
                var name=Text("Name",row,"Companion",28);Place(name.rectTransform,new Vector2(260,72),new Vector2(0,-98));
                var count=Text("Count",row,"×1",34,Gold);Place(count.rectTransform,new Vector2(230,50),new Vector2(0,-150));icons.Add(icon);names.Add(name);counts.Add(count);
            }
            var resultClose=Button("Continue",resultWindow.transform,new Vector2(560,92),new Vector2(0,-263),true);Text("Label",resultClose.transform,"CONTINUE",36);
            Set(controller,"safeArea",safe);Set(controller,"designContent",content);Array(controller,"cards",cards);
            Set(controller,"recruitButton",recruit);Set(controller,"refreshButton",refresh);Set(controller,"closeButton",close);Set(controller,"resultCloseButton",resultClose);
            Array(controller,"multiplierButtons",buttons);Array(controller,"multiplierBackgrounds",backgrounds);
            Set(controller,"balanceIcon",balanceIcon);Set(controller,"balanceLabel",balanceLabel);Set(controller,"recruitIcon",recruitIcon);Set(controller,"recruitCostLabel",recruitCost);Set(controller,"refreshIcon",refreshIcon);Set(controller,"refreshCostLabel",refreshCost);
            Set(controller,"timerLabel",timer);Set(controller,"progressLabel",progressLabel);Set(controller,"progressFill",fill);Set(controller,"statusLabel",status);
            Set(controller,"resultPanel",result.gameObject);Set(controller,"resultTitle",resultTitle);Set(controller,"resultSubtitle",resultSubtitle);Array(controller,"resultIcons",icons);Array(controller,"resultNames",names);Array(controller,"resultCounts",counts);
            var holder=UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();ResourceHolder.instance=holder;
            var portraits=PubPortraitBuilder.Build(holder);Set(controller,"portraits",portraits);foreach(var card in cards)card.portraits=portraits;
            var previewHeroes=holder.avas.Where(v=>PubRecruitment.IsEligibleId(v.Key)&&v.Value!=null).Where(v=>new[]{"goat_mushroom","orc","bird_red","cat_knight","bandit","priest"}.Contains(v.Key)).Take(3).Select(v=>new PubHero{Id=v.Key,Name=System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(v.Key.Replace('_',' ')),Definition=new Obj{ID=v.Key}}).ToArray();
            if(previewHeroes.Length<3)previewHeroes=holder.avas.Where(v=>PubRecruitment.IsEligibleId(v.Key)&&v.Value!=null).Take(3).Select(v=>new PubHero{Id=v.Key,Name=v.Key.Replace('_',' '),Definition=new Obj{ID=v.Key}}).ToArray();
            Sprite gold=null;holder.items.TryGetValue("gold",out gold);if(gold==null)gold=Sprite(Dark+"Sprites/HUD/ResourceBar_Icon_Gold.png");
            controller.EditorPreview(previewHeroes,gold);result.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            PubInspector.Render(root,"pub-preview.png");
            var existing=canvas.transform.Find("UI_Pub");GameObject instance;
            if(existing==null){instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),canvas.transform);Undo.RegisterCreatedObjectUndo(instance,"Place Pub");}
            else instance=existing.gameObject;
            var live=instance.GetComponent<PubController>();if(live==null)throw new Exception("Existing UI_Pub is not the generated Pub");
            var liveRect=(RectTransform)instance.transform;Undo.RecordObject(liveRect,"Fit Pub to Canvas");liveRect.anchorMin=Vector2.zero;liveRect.anchorMax=Vector2.one;liveRect.offsetMin=liveRect.offsetMax=Vector2.zero;liveRect.localScale=Vector3.one;
            var oldOpener=canvas.transform.Find("Button_Pub");Button opener;
            if(oldOpener==null){opener=Button("Button_Pub",canvas.transform,new Vector2(128,134),new Vector2(-90,80),false,new Vector2(1,.5f));Undo.RegisterCreatedObjectUndo(opener.gameObject,"Add Pub button");
                var icon=Image("Icon",opener.transform,Sprite(Shared+"Icons/PictoIcon/128/friend.png"),Gold);icon.preserveAspect=true;Place(icon.rectTransform,new Vector2(60,56),new Vector2(0,22));var label=Text("Label",opener.transform,"Pub",28);Place(label.rectTransform,new Vector2(110,44),new Vector2(0,-40));
            }else opener=oldOpener.GetComponent<Button>();
            if(opener==null)throw new Exception("Button_Pub is not a Button");
            if(opener.onClick.GetPersistentEventCount()==0)UnityEventTools.AddPersistentListener(opener.onClick,live.Show);
            Set(live,"opener",opener.gameObject);PrefabUtility.RecordPrefabInstancePropertyModifications(live);
            instance.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);bool saved=!wasDirty&&EditorSceneManager.SaveScene(scene);
            File.WriteAllText(PubInspector.Work+"/build.txt",JsonConvert.SerializeObject(new{result="PASS",prefab=PrefabPath,scene=scene.path,sceneSaved=saved,scenePreviouslyDirty=wasDirty,canvas=canvas.name,panel=instance.name,entry=opener.name,style="Existing Theme_Dark panels/buttons/fonts",previewHeroes=previewHeroes.Select(h=>h.Id).ToArray()},Formatting.Indented));
        }
        finally{ResourceHolder.instance=savedHolder;EditorSceneManager.ClosePreviewScene(preview);}
    }
}
#endif
