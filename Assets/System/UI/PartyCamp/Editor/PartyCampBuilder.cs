#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
public static class PartyCampBuilder
{
    public const string Root="Assets/System/UI/PartyCamp";
    public const string PrefabPath=Root+"/Prefabs/PartyCamp.prefab";
    const string EnvironmentArt="Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/";
    const string Forest="Assets/Layer Lab/2D Minimal World/Pack/Forest/";
    const string FramePath="Assets/GUI Pro-CasualGame/ResourcesData/Sprites/Components/Frame/ItemFrame07_f_White2 1.png";
    static TMP_FontAsset font;static Sprite frame;static Material material;
    [InitializeOnLoadMethod]static void Init(){if(File.Exists(PartyCampInspector.Work+"/build.request"))EditorApplication.delayCall+=Build;}
    static Sprite Sprite(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    static void Folder(string name){if(!AssetDatabase.IsValidFolder(Root+"/"+name))AssetDatabase.CreateFolder(Root,name);}
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 pos)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.gameObject.layer=5;
        rect.SetParent(parent,false);rect.sizeDelta=size;rect.anchoredPosition=pos;return rect;
    }
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    static Image Image(string name,Transform parent,Vector2 size,Vector2 pos,Sprite sprite,Color color)
    {
        var rect=Rect(name,parent,size,pos);var image=rect.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=false;return image;
    }
    static TMP_Text Text(string name,Transform parent,string text,Vector2 size,Vector2 pos,float fontSize=28)
    {
        var rect=Rect(name,parent,size,pos);var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.fontSharedMaterial=font.material;label.text=text;
        label.fontSize=fontSize;label.enableAutoSizing=true;label.fontSizeMin=fontSize*.7f;label.fontSizeMax=fontSize;label.color=Color.white;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;return label;
    }
    static Button Button(string name,Transform parent,string label,Vector2 size,Vector2 pos,bool blue=false)
    {
        var image=Image(name,parent,size,pos,frame,blue?new Color(.16f,.56f,1):Color.white);image.raycastTarget=true;
        var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;
        var text=Text("Label",image.transform,label,size-new Vector2(12,8),Vector2.zero,28);return button;
    }
    static void Set(UnityEngine.Object target,string field,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetArray(UnityEngine.Object target,string field,IEnumerable<UnityEngine.Object> values)
    {
        var list=values.ToArray();var so=new SerializedObject(target);var prop=so.FindProperty(field);prop.arraySize=list.Length;
        for(int i=0;i<list.Length;i++)prop.GetArrayElementAtIndex(i).objectReferenceValue=list[i];so.ApplyModifiedPropertiesWithoutUndo();
    }
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles,List<Color> colors)
    {
        string path=Root+"/Materials/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);
        mesh.uv=vertices.Select(v=>Vector2.zero).ToArray();mesh.RecalculateBounds();mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);return mesh;
    }
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,int order)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.layer=2;go.transform.SetParent(parent,false);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return go;
    }
    static Mesh Disk(string name,Color center,Color edge,bool ring=false)
    {
        var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        if(!ring){vertices.Add(Vector3.zero);colors.Add(center);}
        for(int i=0;i<=48;i++)
        {
            float angle=i*Mathf.PI*2/48;var p=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
            vertices.Add(p);colors.Add(edge);
            if(ring){vertices.Add(p*.85f);colors.Add(center);if(i>0){int n=i*2;triangles.AddRange(new[]{n-2,n,n-1,n-1,n,n+1});}}
            else if(i>0)triangles.AddRange(new[]{0,i,i+1});
        }
        return SaveMesh(name,vertices,triangles,colors);
    }
    static SpriteRenderer Prop(string name,Transform parent,string path,Vector2 position,float height,int order,Color? tint=null)
    {
        var sprite=Sprite(path);if(sprite==null)return null;
        var go=new GameObject(name,typeof(SpriteRenderer));go.layer=2;go.transform.SetParent(parent,false);
        var renderer=go.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=material;renderer.sortingOrder=order;renderer.color=tint??Color.white;
        float scale=height/Mathf.Max(.01f,sprite.bounds.size.y);go.transform.localScale=Vector3.one*scale;
        go.transform.localPosition=new Vector3(position.x-sprite.bounds.center.x*scale,position.y-sprite.bounds.min.y*scale,0);return renderer;
    }
    static void Ground(Transform environment)
    {
        var random=new System.Random(4387);var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        const int xSteps=18,ySteps=28;
        for(int y=0;y<=ySteps;y++)for(int x=0;x<=xSteps;x++)
        {
            float px=-9+x,py=-14+y;vertices.Add(new Vector3(px,py,0));
            float light=Mathf.Clamp01(1-Vector2.Distance(new Vector2(px,py),new Vector2(0,3.5f))/12);
            var color=Color.Lerp(new Color(.003f,.025f,.018f),new Color(.018f,.12f,.065f),light);
            float noise=(float)random.NextDouble()*.006f;colors.Add(color+new Color(noise,noise,noise,0));
            if(x<xSteps&&y<ySteps){int a=y*(xSteps+1)+x;triangles.AddRange(new[]{a,a+1,a+xSteps+1,a+1,a+xSteps+2,a+xSteps+1});}
        }
        MeshObject("Forest Clearing",environment,SaveMesh("CampGround",vertices,triangles,colors),-1000);
        for(int i=0;i<80;i++)
        {
            float x=(float)random.NextDouble()*12-6,y=(float)random.NextDouble()*20-8;
            if(Mathf.Abs(x)<3.4f&&y>0&&y<7.8f)continue;
            Prop("Grass",environment,Forest+"Nature/ResourcesData/Sprites/Grass/Grass_10_Green.png",new Vector2(x,y),.18f+(float)random.NextDouble()*.18f,-800,new Color(.65f,.78f,.64f,.65f));
        }
        foreach(var p in new[]{new Vector2(-4.7f,6),new Vector2(4.8f,5.3f),new Vector2(-5.3f,.4f),new Vector2(5.4f,1.3f),new Vector2(-3.7f,9.2f),new Vector2(4.2f,9.5f)})
            Prop("Camp Tree",environment,EnvironmentArt+"Tree/Tree_Green_10.png",p,3.4f,-600,new Color(.65f,.80f,.71f));
        foreach(var p in new[]{new Vector2(-3.9f,1),new Vector2(3.9f,2.5f),new Vector2(-3.8f,7.3f)})
            Prop("Camp Bush",environment,Forest+"Nature/ResourcesData/Sprites/Plant_Bush_03_Green.png",p,.8f,-500);
        var glow=MeshObject("Campfire Glow",environment,Disk("CampGlow",new Color(1,.55f,.12f,.28f),new Color(1,.45f,.05f,0)),-400);
        glow.transform.localPosition=new Vector3(0,3.5f,0);glow.transform.localScale=new Vector3(2.2f,1.5f,1);
        var logs=Prop("Campfire",environment,Forest+"ForestVillage/ResourcesData/Sprites/Prop/Campfire_01.png",new Vector2(0,3.4f),1.1f,15);
        var flame=Prop("Flame",environment,"Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/UniqueIcon/256/Misc_Fire_01_Red.png",new Vector2(0,3.9f),.95f,16);
        if(flame!=null){var fire=flame.gameObject.AddComponent<PartyCampFire>();Set(fire,"flame",flame.transform);Set(fire,"glow",glow.transform);}
    }
    [MenuItem("Tools/UI/Party Camp/Build and Place")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        GameObject root=null;var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();UnityEngine.SceneManagement.Scene buildScene=default;
        try
        {
            if(scene.path!="Assets/Scenes/Game_Exp_Rumble.unity")throw new InvalidOperationException("Open Game_Exp_Rumble before building the camp");
            Folder("Materials");Folder("Prefabs");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Knights Quest SDF.asset");frame=Sprite(FramePath);
            material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CampUnlit.mat");
            var campShader=Shader.Find("AllForOne/PartyCampUnlit");if(campShader==null)throw new InvalidOperationException("Camp shader has not imported yet");
            if(material==null){material=new Material(campShader);AssetDatabase.CreateAsset(material,Root+"/Materials/CampUnlit.mat");}
            else if(material.shader!=campShader){material.shader=campShader;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
            var sourceState=UnityEngine.Object.FindFirstObjectByType<MainStates>(FindObjectsInactive.Include);
            int count=Mathf.Max(1,sourceState.petsHiIndex-sourceState.petsLoIndex);
            var sourceRenderers=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(v=>v.gameObject.scene==scene&&v.GetComponentInParent<Canvas>()==null&&v.GetComponentInParent<PartyCampController>()==null&&v.bounds.size.magnitude<5000).ToArray();
            float maxX=sourceRenderers.Length>0?sourceRenderers.Max(v=>v.bounds.max.x):300;
            float maxY=sourceRenderers.Length>0?sourceRenderers.Max(v=>v.bounds.max.y):300;
            root=new GameObject("PartyCamp");buildScene=EditorSceneManager.NewPreviewScene();UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,buildScene);var controller=root.AddComponent<PartyCampController>();
            var environment=new GameObject("Management Clearing");environment.layer=2;environment.transform.SetParent(root.transform,false);environment.transform.position=new Vector3(maxX+100,maxY+100,0);
            Ground(environment.transform);
            var cameraObject=new GameObject("Party Camp Camera",typeof(Camera));cameraObject.transform.SetParent(environment.transform,false);cameraObject.transform.localPosition=new Vector3(0,0,-20);
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=10.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.12f,.10f);camera.cullingMask=1<<2;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.enabled=false;
            var canvasObject=new GameObject("PartyCampCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.layer=5;canvasObject.transform.SetParent(root.transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=3000;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,2400);scaler.matchWidthOrHeight=.5f;
            var blocker=Image("Input Blocker",canvasObject.transform,Vector2.zero,Vector2.zero,null,Color.clear);Stretch(blocker.rectTransform);blocker.raycastTarget=true;
            var overlay=Rect("Slot Buttons",canvasObject.transform,Vector2.zero,Vector2.zero);Stretch(overlay);
            var safe=Rect("SafeArea",canvasObject.transform,Vector2.zero,Vector2.zero);Stretch(safe);
            var design=Rect("Design",safe,new Vector2(1080,2400),Vector2.zero);
            Text("Title",design,"SELECT HEROES",new Vector2(850,80),new Vector2(0,1080),44);
            var countLabel=Text("Party Count",design,"0 / "+count,new Vector2(700,60),new Vector2(0,1002),38);
            Text("Hint",design,"Tap a camp hero to remove",new Vector2(850,45),new Vector2(0,947),23).color=new Color(.7f,.84f,.8f);
            var slotList=new List<PartyCampSlot>();var ring=Disk("CampSlotRing",new Color(.68f,.9f,.85f,.35f),new Color(.65f,.88f,.8f,.6f),true);
            for(int i=0;i<count;i++)
            {
                float angle=(180-i*360f/count)*Mathf.Deg2Rad;
                var slotObject=new GameObject("Party Slot "+(sourceState.petsLoIndex+i));slotObject.layer=2;slotObject.transform.SetParent(environment.transform,false);slotObject.transform.localPosition=new Vector3(Mathf.Cos(angle)*3.1f,3.5f+Mathf.Sin(angle)*2.65f,0);
                var slot=slotObject.AddComponent<PartyCampSlot>();var preview=new GameObject("Display Hero").transform;preview.SetParent(slotObject.transform,false);
                var marker=MeshObject("Standing Ring",slotObject.transform,ring,-50);marker.transform.localScale=new Vector3(.36f,.13f,1);
                var hit=Button("Slot "+i,overlay,"+",new Vector2(110,80),Vector2.zero);hit.GetComponent<Image>().color=Color.clear;
                var slotCaption=hit.GetComponentInChildren<TMP_Text>();slotCaption.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Font/AfacadFlux-ExtraBold SDF.asset");slotCaption.fontSharedMaterial=slotCaption.font.material;slotCaption.fontSize=40;slotCaption.fontSizeMax=40;
                Set(slot,"previewRoot",preview);Set(slot,"marker",marker.transform);Set(slot,"hitButton",hit);Set(slot,"caption",hit.GetComponentInChildren<TMP_Text>());Set(slot,"spriteMaterial",material);slotList.Add(slot);
            }
            var panel=Image("Roster Panel",design,new Vector2(1000,840),new Vector2(0,-550),frame,Color.white);
            Text("Roster Title",panel.transform,"YOUR HEROES",new Vector2(440,68),new Vector2(-240,350),32);
            var auto=Button("AutoAssign",panel.transform,"AUTO ASSIGN",new Vector2(270,74),new Vector2(185,350),true);
            var clear=Button("Clear",panel.transform,"CLEAR",new Vector2(150,74),new Vector2(410,350));
            var viewport=Rect("Viewport",panel.transform,new Vector2(950,650),new Vector2(0,-18));viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage=viewport.gameObject.AddComponent<Image>();viewportImage.color=Color.clear;viewportImage.raycastTarget=true;
            var content=Rect("Hero List",viewport,new Vector2(950,0),Vector2.zero);content.anchorMin=content.anchorMax=content.pivot=new Vector2(0,1);content.anchoredPosition=Vector2.zero;
            var grid=content.gameObject.AddComponent<GridLayoutGroup>();grid.cellSize=new Vector2(208,204);grid.spacing=new Vector2(18,18);grid.padding=new RectOffset(22,22,16,16);grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=4;
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=panel.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=55;
            var templateImage=Image("Hero Card Template",content,new Vector2(208,204),Vector2.zero,frame,Color.white);templateImage.gameObject.SetActive(false);
            var template=templateImage.gameObject.AddComponent<PartyCampHeroCard>();var cardButton=templateImage.gameObject.AddComponent<Button>();cardButton.targetGraphic=templateImage;templateImage.raycastTarget=true;
            templateImage.GetComponent<ObjHolder>().noTrack=true;
            var portrait=Image("Portrait",templateImage.transform,new Vector2(136,132),new Vector2(0,24),null,Color.white);portrait.preserveAspect=true;
            var check=Image("Assigned",templateImage.transform,new Vector2(38,38),new Vector2(70,71),Sprite("Assets/GUI Pro-CasualGame/ResourcesData/Sprites/Components/UI_Etc/Toggle01_Check_Green.png"),Color.white);
            var name=Text("Name",templateImage.transform,"Hero",new Vector2(190,36),new Vector2(0,-58),22);
            var level=Text("Level",templateImage.transform,"Lv.1 A.0",new Vector2(190,30),new Vector2(0,-88),20);
            Set(template,"missingPortrait",Sprite("Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/friend.png"));Set(template,"button",cardButton);Set(template,"portrait",portrait);Set(template,"frame",templateImage);Set(template,"checkmark",check.gameObject);Set(template,"nameLabel",name);Set(template,"levelLabel",level);
            var empty=Text("Empty",viewport,"Recruit heroes at the Pub first",new Vector2(800,120),Vector2.zero,32);
            Text("Scroll Hint",panel.transform,"Swipe to see more heroes",new Vector2(850,40),new Vector2(0,-380),21).color=new Color(.65f,.72f,.81f);
            var back=Button("Back",design,"<",new Vector2(128,104),new Vector2(-422,-1090),true);
            var status=Text("Status",design,"Tap a portrait to assign or remove",new Vector2(730,100),new Vector2(80,-1090),25);
            Set(controller,"environment",environment);Set(controller,"campCamera",camera);Set(controller,"campCanvas",canvas);Set(controller,"safeArea",safe);Set(controller,"design",design);Set(controller,"slotOverlay",overlay);Set(controller,"rosterContent",content);Set(controller,"cardTemplate",template);Set(controller,"backButton",back);Set(controller,"autoAssignButton",auto);Set(controller,"clearButton",clear);Set(controller,"countLabel",countLabel);Set(controller,"statusLabel",status);Set(controller,"emptyLabel",empty);SetArray(controller,"slots",slotList);
            environment.SetActive(false);canvasObject.SetActive(false);
            var asset=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);if(asset==null)throw new InvalidOperationException("Cannot save camp prefab");
            var existing=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PartyCampController>(true)).FirstOrDefault();
            PartyCampController instance;
            if(existing==null){var placed=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);Undo.RegisterCreatedObjectUndo(placed,"Add party management camp");instance=placed.GetComponent<PartyCampController>();}else instance=existing;
            var mainCanvas=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Canvas>(true)).Single(c=>c.name=="Canvas"&&c.transform.parent==null);
            var oldButton=mainCanvas.transform.Find("Button_PartyCamp");Button opener;
            if(oldButton==null)
            {
                opener=Button("Button_PartyCamp",mainCanvas.transform,"PARTY",new Vector2(128,134),new Vector2(-90,-80),true);
                var rect=(RectTransform)opener.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);
                var label=opener.GetComponentInChildren<TMP_Text>();label.rectTransform.sizeDelta=new Vector2(112,42);label.rectTransform.anchoredPosition=new Vector2(0,-38);label.fontSize=25;label.fontSizeMax=25;
                var icon=Image("Icon",opener.transform,new Vector2(63,63),new Vector2(0,22),Sprite("Assets/Layer Lab/GUI Pro-MinimalGame/Shared/Icons/PictoIcon/128/friend.png"),Color.white);icon.preserveAspect=true;
                Undo.RegisterCreatedObjectUndo(opener.gameObject,"Add party camp button");
            }
            else opener=oldButton.GetComponent<Button>();
            if(opener.onClick.GetPersistentEventCount()==0)UnityEventTools.AddPersistentListener(opener.onClick,instance.Show);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Cannot save Game_Exp_Rumble");
            File.WriteAllText(PartyCampInspector.Work+"/build.txt",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",prefab=PrefabPath,scene=scene.path,slots=count,campPosition=environment.transform.position.ToString(),mapMaxX=maxX,mapMaxY=maxY,mainCameraUnchanged=true,button="Canvas/Button_PartyCamp"},Newtonsoft.Json.Formatting.Indented));File.Delete(PartyCampInspector.Work+"/build.request");
            if(File.Exists(PartyCampInspector.Work+"/validate.request")){PartyCampValidation.Run();EditorSceneManager.SaveScene(scene);}
        }
        catch(Exception e){File.WriteAllText(PartyCampInspector.Work+"/build.txt","FAIL: "+e);Debug.LogException(e);}
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(buildScene.IsValid())EditorSceneManager.ClosePreviewScene(buildScene);}
    }
}
#endif
