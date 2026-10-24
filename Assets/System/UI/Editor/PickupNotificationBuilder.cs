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

public static class PickupNotificationBuilder
{
    const string Folder="Assets/System/UI/Resources";
    const string Flag="Assets/System/UI/Editor/.pickup-ui-build";
    [InitializeOnLoadMethod] static void Init() { if(File.Exists(Flag)) EditorApplication.delayCall+=Requested; }
    static void Requested()
    {
        if(EditorApplication.isPlaying) return;
        File.Delete(Flag);
        try { Build(); File.WriteAllText(Path.Combine(Path.GetTempPath(),"pickup-ui-build.txt"),"PASS: PickupNotifications prefab saved; theme font, layout, duplicate merge and queue verified; native preview rendered."); }
        catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"pickup-ui-build.txt"),"FAIL: "+e); Debug.LogException(e); }
    }
    static RectTransform Rect(string name,Transform parent)
    {
        var go=new GameObject(name,typeof(RectTransform)); go.layer=5;
        var rect=(RectTransform)go.transform; if(parent!=null) rect.SetParent(parent,false);
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
    static Image Image(string name,Transform parent,Color color)
    {
        var rect=Rect(name,parent); var image=rect.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image;
    }
    static TextMeshProUGUI Label(string name,Transform parent,TMP_FontAsset font,int size,TextAlignmentOptions alignment)
    {
        var rect=Rect(name,parent); var text=rect.gameObject.AddComponent<TextMeshProUGUI>(); text.font=font;
        text.fontSize=size; text.enableAutoSizing=true; text.fontSizeMin=14; text.fontSizeMax=size;
        text.color=new Color(.88f,.92f,.95f); text.alignment=alignment;
        text.textWrappingMode=TextWrappingModes.NoWrap; text.overflowMode=TextOverflowModes.Ellipsis;
        text.raycastTarget=false; text.richText=false; text.margin=Vector4.zero; return text;
    }
    [MenuItem("Tools/UI/Build Pickup Notifications")]
    public static void Build()
    {
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/System/UI","Resources");
        var scene=EditorSceneManager.NewPreviewScene(); GameObject root=null;
        try {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("8f586378b4e144a9851e7b34d9b748ee"));
            if(font==null) throw new Exception("Theme font missing");
            root=new GameObject("PickupNotifications",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(PickupNotificationStack));
            root.layer=5; SceneManager.MoveGameObjectToScene(root,scene);
            var canvas=root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=260;
            var scaler=root.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            var safe=Rect("SafeArea",root.transform);
            var rows=Rect("Rows",safe); rows.anchorMin=rows.anchorMax=Vector2.one; rows.pivot=Vector2.one;
            rows.sizeDelta=new Vector2(380,240); rows.anchoredPosition=new Vector2(-36,-180);
            var row=Image("RowTemplate",rows,new Color(.055f,.085f,.125f,.96f)).rectTransform;
            row.anchorMin=new Vector2(0,1); row.anchorMax=Vector2.one; row.pivot=new Vector2(.5f,1);
            row.sizeDelta=new Vector2(0,42); row.anchoredPosition=Vector2.zero;
            var group=row.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts=false; group.interactable=false;
            var accent=Image("Accent",row,new Color(.133f,.765f,.996f)).rectTransform;
            accent.anchorMax=new Vector2(0,1); accent.pivot=new Vector2(0,.5f); accent.sizeDelta=new Vector2(3,0);
            var line=Image("TopLine",row,new Color(.65f,.8f,.95f,.13f)).rectTransform;
            line.anchorMin=new Vector2(0,1); line.pivot=new Vector2(.5f,1); line.sizeDelta=new Vector2(0,1);
            var icon=Image("Icon",row,Color.white); icon.preserveAspect=true;
            icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,.5f);
            icon.rectTransform.sizeDelta=Vector2.one*28; icon.rectTransform.anchoredPosition=new Vector2(26,0);
            var label=Label("Name",row,font,20,TextAlignmentOptions.MidlineLeft);
            label.rectTransform.offsetMin=new Vector2(50,4); label.rectTransform.offsetMax=new Vector2(-94,-4);
            var amount=Label("Amount",row,font,21,TextAlignmentOptions.MidlineRight);
            amount.color=new Color(.133f,.765f,.996f); amount.rectTransform.offsetMin=new Vector2(288,4); amount.rectTransform.offsetMax=new Vector2(-12,-4);
            label.text="Предмет"; amount.text="+1";
            row.gameObject.SetActive(false);
            var stack=root.GetComponent<PickupNotificationStack>(); var fields=new SerializedObject(stack);
            fields.FindProperty("safeArea").objectReferenceValue=safe;
            fields.FindProperty("rows").objectReferenceValue=rows;
            fields.FindProperty("rowTemplate").objectReferenceValue=row;
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PickupNotifications.prefab");
            // Root overlay Canvases in preview scenes can be driven to a zero scale.
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PickupNotifications.prefab");
            var rectFields=new SerializedObject(prefab.transform); rectFields.FindProperty("m_LocalScale").vector3Value=Vector3.one;
            rectFields.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(prefab.transform); AssetDatabase.SaveAssetIfDirty(prefab);
            canvas.renderMode=RenderMode.WorldSpace; scaler.enabled=false;
            root.transform.localScale=Vector3.one; root.transform.position=Vector3.zero;
            ((RectTransform)root.transform).sizeDelta=new Vector2(1920,1080);
            var sprite=FindIcon();
            string[] names={"Золото","Древесина","Кристалл маны","Железная руда","Длинное название найденного предмета"};
            for(int i=0;i<8;i++) stack.Enqueue("preview_"+i,i<5?names[i]:"Ещё один предмет",i==0?125:i+1,sprite);
            stack.Enqueue("preview_0",names[0],25,sprite);
            if(stack.VisibleCount!=5 || stack.PendingCount!=3) throw new Exception("Stack/queue capacity failed");
            if(rows.GetChild(1).Find("Amount").GetComponent<TextMeshProUGUI>().text!="+150") throw new Exception("Pickup merge failed");
            foreach(var visibility in rows.GetComponentsInChildren<CanvasGroup>()) visibility.alpha=1;
            Canvas.ForceUpdateCanvases();
            var visible=rows.Cast<Transform>().Where(t=>t.gameObject.activeSelf).Select(t=>(RectTransform)t).ToArray();
            for(int i=1;i<visible.Length;i++) if(Mathf.Abs(visible[i].anchoredPosition.y-visible[i-1].anchoredPosition.y)<42) throw new Exception("Rows overlap");
            Render(root,scene,rows);
        }
        finally { if(root!=null) UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(scene); }
    }
    static Sprite FindIcon()
    {
        var holder=UnityEngine.Object.FindFirstObjectByType<ResourceHolder>();
        if(holder!=null) { var icon=holder.GetIcon("gold"); if(icon!=null) return icon; }
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Layer Lab/GUI Pro-MinimalGame/Theme_Dark/Sprites/HUD/ResourceBar_Icon_Gold.png");
    }
    static void Render(GameObject root,Scene scene,RectTransform rows)
    {
        var go=new GameObject("Pickup preview camera",typeof(Camera)); SceneManager.MoveGameObjectToScene(go,scene);
        var camera=go.GetComponent<Camera>(); camera.scene=scene; camera.orthographic=true; camera.orthographicSize=145; camera.farClipPlane=2000;
        camera.transform.position=rows.TransformPoint(new Vector3(-190,-117,-1000));
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.095f,.115f,.14f);
        var target=new RenderTexture(1000,650,24); var previous=RenderTexture.active; Texture2D texture=null;
        try {
            camera.targetTexture=target; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=target;
            texture=new Texture2D(1000,650,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1000,650),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"pickup-ui-preview.png"),texture.EncodeToPNG());
        } finally { camera.targetTexture=null; RenderTexture.active=previous; if(texture!=null) UnityEngine.Object.DestroyImmediate(texture);
            target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go); }
    }
}
#endif
