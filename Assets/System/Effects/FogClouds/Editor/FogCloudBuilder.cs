#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class FogCloudBuilder
{
    const string Root="Assets/System/Effects/FogClouds";
    const string Flag=Root+"/Editor/.build-area-fog";
    [InitializeOnLoadMethod] static void Init(){if(File.Exists(Flag))EditorApplication.delayCall+=Requested;}
    static void Requested()
    {
        if(EditorApplication.isPlaying)return;File.Delete(Flag);
        try{Build();}catch(Exception e){File.WriteAllText(Path.Combine(Path.GetTempPath(),"area-fog-build.txt"),"FAIL: "+e);Debug.LogException(e);}
    }
    [MenuItem("Tools/Effects/Build FogAreaActivator1 Clouds")]
    public static void Build()
    {
        var target=UnityEngine.Object.FindObjectsByType<FogActivator>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Single(f=>f.transform.parent!=null&&f.transform.parent.name=="FogAreaActivator1");
        if(target.lo==null||target.hi==null||target.cloudRoot==null)throw new Exception("Fog markers/root are not assigned");
        bool wasDirty=target.gameObject.scene.isDirty;
        Vector3 a=target.cloudRoot.InverseTransformPoint(target.lo.position),b=target.cloudRoot.InverseTransformPoint(target.hi.position);
        var min=new Vector2(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y));var size=new Vector2(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y));
        if(size.x<.1f||size.y<.1f)throw new Exception("Fog area is empty");
        var sprites=AssetDatabase.LoadAllAssetsAtPath("Assets/!Images/mapcloud.png").OfType<Sprite>().OrderBy(s=>s.name).ToArray();
        if(sprites.Length==0)throw new Exception("Cloud atlas sprites missing");
        var shader=Shader.Find("AllForOne/Effects/AreaCloudFog");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("Cloud shader failed");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/AreaCloudFog.mat");
        if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"/AreaCloudFog.mat");}
        material.SetTexture("_MainTex",sprites[0].texture);material.SetVector("_Area",new Vector4(min.x,min.y,size.x,size.y));
        EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        var mesh=CreateMesh(sprites,min,size);
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/FogArea1Mesh.asset");
        if(existing==null){AssetDatabase.CreateAsset(mesh,Root+"/FogArea1Mesh.asset");existing=mesh;}
        else{EditorUtility.CopySerialized(mesh,existing);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);}
        Transform child=target.cloudRoot.Find("AnimatedClouds");
        if(child==null){var go=new GameObject("AnimatedClouds");Undo.RegisterCreatedObjectUndo(go,"Add animated area fog");child=go.transform;child.SetParent(target.cloudRoot,false);}
        child.gameObject.layer=0;
        var filter=child.GetComponent<MeshFilter>();if(filter==null)filter=Undo.AddComponent<MeshFilter>(child.gameObject);
        var renderer=child.GetComponent<MeshRenderer>();if(renderer==null)renderer=Undo.AddComponent<MeshRenderer>(child.gameObject);
        Undo.RecordObjects(new UnityEngine.Object[]{filter,renderer,target},"Configure area clouds");
        filter.sharedMesh=existing;renderer.sharedMaterial=material;renderer.sortingOrder=3000;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        var fields=new SerializedObject(target);fields.FindProperty("cloudSurface").objectReferenceValue=renderer;fields.ApplyModifiedProperties();
        if(PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
        bool saved=!wasDirty&&EditorSceneManager.SaveScene(target.gameObject.scene);
        Preview(existing,material,min,size);
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"area-fog-build.txt"),Newtonsoft.Json.JsonConvert.SerializeObject(new{
            result="PASS",target=target.name,parent=target.transform.parent.name,areaSize=size.ToString(),sprites=sprites.Length,
            clouds=existing.vertexCount/4,renderers=1,sceneSaved=saved,scenePreviouslyDirty=wasDirty,
            boundsLo=target.lo.position.ToString(),boundsHi=target.hi.position.ToString(),root=target.cloudRoot.name
        },Newtonsoft.Json.Formatting.Indented));
    }
    [MenuItem("Tools/Effects/Preview Area Fog")]
    public static void PreviewExisting()
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/AreaCloudFog.mat");
        if(material==null||ShaderUtil.ShaderHasError(material.shader))throw new Exception("Cloud shader failed");
        var area=material.GetVector("_Area");
        Preview(AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/FogArea1Mesh.asset"),material,
            new Vector2(area.x,area.y),new Vector2(area.z,area.w));
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"area-fog-edge-check.txt"),"PASS: shader compiled; native covered/dispersing/clear previews rendered; animation changes pixels.");
    }
    static Mesh CreateMesh(Sprite[] sprites,Vector2 min,Vector2 size)
    {
        int nx=Mathf.Clamp(Mathf.CeilToInt(size.x/2.4f),2,10),ny=Mathf.Clamp(Mathf.CeilToInt(size.y/2.6f),2,10);
        int count=nx*ny;var vertices=new Vector3[count*4];var uv=new Vector2[count*4];var cloud=new System.Collections.Generic.List<Vector4>(count*4);var indices=new int[count*6];
        var random=new System.Random(7314);float dx=size.x/nx,dy=size.y/ny;
        for(int n=0;n<count;n++){
            var sprite=sprites[random.Next(sprites.Length)];var rect=sprite.rect;var texture=sprite.texture;
            float x=min.x+(n%nx+.5f)*dx+(float)(random.NextDouble()-.5)*dx*.4f;
            float y=min.y+(n/nx+.5f)*dy+(float)(random.NextDouble()-.5)*dy*.4f;
            float h=dy*1.85f*Mathf.Lerp(.9f,1.12f,(float)random.NextDouble()),w=h*Mathf.Clamp(rect.width/rect.height,.8f,2.6f);
            float phase=(float)random.NextDouble()*6.28f,speed=Mathf.Lerp(.8f,1.2f,(float)random.NextDouble());
            for(int k=0;k<4;k++){
                bool right=k==1||k==2,top=k>=2;
                vertices[n*4+k]=new Vector3(x+(right?.5f:-.5f)*w,y+(top?.5f:-.5f)*h,-.05f);
                uv[n*4+k]=new Vector2((right?rect.xMax:rect.xMin)/texture.width,(top?rect.yMax:rect.yMin)/texture.height);
                cloud.Add(new Vector4(x,y,phase,speed));
            }
            int v=n*4,t=n*6;indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;
        }
        var mesh=new Mesh{name="FogArea1 cloud cards"};mesh.vertices=vertices;mesh.uv=uv;mesh.SetUVs(1,cloud);mesh.triangles=indices;mesh.RecalculateBounds();return mesh;
    }
    static void Preview(Mesh mesh,Material material,Vector2 min,Vector2 size)
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try{
            var root=new GameObject("Fog preview",typeof(MeshFilter),typeof(MeshRenderer));SceneManager.MoveGameObjectToScene(root,scene);
            root.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=3000;
            var ground=GameObject.CreatePrimitive(PrimitiveType.Quad);SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.position=new Vector3(min.x+size.x*.5f,min.y+size.y*.5f,.1f);ground.transform.localScale=new Vector3(size.x,size.y,1);
            var groundMaterial=new Material(Shader.Find("Unlit/Color"));groundMaterial.color=new Color(.27f,.36f,.18f);ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
            var go=new GameObject("Fog preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(go,scene);var camera=go.GetComponent<Camera>();camera.scene=scene;
            camera.orthographic=true;camera.orthographicSize=size.y*.55f;camera.transform.position=new Vector3(min.x+size.x*.5f,min.y+size.y*.5f,-20);
            camera.backgroundColor=new Color(.07f,.1f,.12f);camera.clearFlags=CameraClearFlags.SolidColor;
            var rt=new RenderTexture(700,850,24);var previous=RenderTexture.active;Texture2D texture=null;
            try{
                camera.targetTexture=rt;var block=new MaterialPropertyBlock();Color32[] first=null;
                for(int pass=0;pass<4;pass++){
                    block.SetFloat("_PreviewTime",pass==0?0:8);block.SetFloat("_ClearProgress",pass<2?0:pass==2?.5f:1);block.SetVector("_RevealOrigin",new Vector4(.5f,.5f,0,0));renderer.SetPropertyBlock(block);
                    camera.Render();RenderTexture.active=rt;texture=new Texture2D(700,850,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,700,850),0,0);texture.Apply();
                    if(pass==0)first=texture.GetPixels32();
                    if(pass==1){var second=texture.GetPixels32();int changed=0;for(int i=0;i<first.Length;i++)if(Mathf.Abs(first[i].r-second[i].r)+Mathf.Abs(first[i].g-second[i].g)+Mathf.Abs(first[i].b-second[i].b)>12)changed++;if(changed<100)throw new Exception("Clouds are not animated");}
                    string label=pass<2?"covered":pass==2?"dispersing":"clear";
                    File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"area-fog-"+label+".png"),texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);texture=null;
                }
            }finally{camera.targetTexture=null;RenderTexture.active=previous;if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(groundMaterial);}
        }finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif

