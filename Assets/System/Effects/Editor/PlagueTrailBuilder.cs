#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public static class PlagueTrailBuilder
{
    const string Folder = "Assets/System/Effects/PlagueTrail";
    const string Flag = "Assets/System/Effects/Editor/.plague-build";
    const string ScenePath = "Assets/Scenes/Game_Exp_Rumble.unity";
    [InitializeOnLoadMethod] static void Init()
    {
        if (File.Exists(Flag)) EditorApplication.delayCall += Requested;
    }
    static void Requested()
    {
        if (EditorApplication.isPlaying) return;
        File.Delete(Flag);
        try { Build(); }
        catch (Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-build.txt"),"FAIL: "+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Effects/Build Plague Trail")]
    public static void Build()
    {
        var shader = Shader.Find("AllForOne/Effects/PlagueGround");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Plague shader failed to compile");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PlagueGround.mat");
        if (material == null) { material=new Material(shader); AssetDatabase.CreateAsset(material,Folder+"/PlagueGround.mat"); }
        material.shader=shader;
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
        var previewScene = EditorSceneManager.NewPreviewScene();
        GameObject root = null;
        try
        {
            root = new GameObject("PlagueTrail");
            SceneManager.MoveGameObjectToScene(root,previewScene);
            var trail=root.AddComponent<PlagueTrail>();
            var fields=new SerializedObject(trail);
            fields.FindProperty("surfaceMaterial").objectReferenceValue=material;
            fields.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root,Folder+"/PlagueTrail.prefab");
            Validate(trail);
            Render(trail,previewScene);
        }
        finally { if(root!=null) UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(previewScene); }

        var scene=SceneManager.GetActiveScene();
        bool saved=false;
        string placement="Scene unchanged";
        if (scene.path == ScenePath)
        {
            bool wasDirty=scene.isDirty;
            var existing=UnityEngine.Object.FindObjectsByType<PlagueTrail>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(t=>t.gameObject.scene==scene);
            if(existing==null)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/PlagueTrail.prefab");
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                Undo.RegisterCreatedObjectUndo(instance,"Add plague trail");
                EditorSceneManager.MarkSceneDirty(scene);
                placement="Added PlagueTrail prefab to "+ScenePath;
                if (!wasDirty) saved=EditorSceneManager.SaveScene(scene);
            }
            else placement="Existing PlagueTrail retained";
        }
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-build.txt"),"PASS: shader compiled; sampling, stationary, teleport, bounded mesh, clear and native preview checked. "+placement+"; saved="+saved);
    }
    static void Require(bool value,string message) { if(!value) throw new Exception(message); }
    static void Validate(PlagueTrail trail)
    {
        trail.RecordPosition(Vector3.zero);
        Require(trail.PatchCount==0,"Stationary start emitted a patch");
        for(int i=0;i<30;i++) trail.RecordPosition(Vector3.zero);
        Require(trail.PatchCount==0,"Stationary emission");
        trail.RecordPosition(new Vector3(3,0,0));
        Require(trail.PatchCount>=8 && trail.PatchCount<=10,"Distance sampling");
        int count=trail.PatchCount;
        trail.RecordPosition(new Vector3(100,0,0));
        Require(trail.PatchCount==count,"Teleport gap painted");
        for(int i=1;i<=900;i++) trail.RecordPosition(new Vector3(100+i*.4f,0,0));
        Require(trail.PatchCount==512,"Ring capacity");
        Require(trail.GetComponentsInChildren<MeshRenderer>().Length==1,"Multiple surface renderers");
        Require(trail.GetComponentInChildren<MeshFilter>().sharedMesh.vertexCount==2048,"Mesh capacity");
        trail.ClearTrail();
        Require(trail.PatchCount==0,"Clear failed");
    }
    static void Render(PlagueTrail trail,Scene scene)
    {
        trail.RecordPosition(new Vector3(-5,-1,0));
        for(int i=1;i<=80;i++) {
            float x=-5+i*.125f;
            trail.RecordPosition(new Vector3(x,Mathf.Sin(x*.85f)*1.1f,0));
        }
        trail.SetPreviewTime(3);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Quad);
        SceneManager.MoveGameObjectToScene(ground,scene);
        ground.transform.position=new Vector3(0,0,.2f); ground.transform.localScale=new Vector3(30,30,1);
        var groundMaterial=new Material(Shader.Find("Unlit/Color")); groundMaterial.color=new Color(.24f,.30f,.16f);
        ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
        var cameraObject=new GameObject("Plague preview camera",typeof(Camera));
        SceneManager.MoveGameObjectToScene(cameraObject,scene);
        var camera=cameraObject.GetComponent<Camera>(); camera.scene=scene;
        camera.orthographic=true; camera.orthographicSize=4.1f;
        camera.transform.position=new Vector3(0,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.11f,.13f,.10f);
        var target=new RenderTexture(1200,720,24);
        var previous=RenderTexture.active;
        Texture2D texture=null;
        try {
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture=new Texture2D(1200,720,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1200,720),0,0); texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(),"plague-preview.png"),texture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous; camera.targetTexture=null;
            if(texture!=null) UnityEngine.Object.DestroyImmediate(texture);
            target.Release(); UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(ground);
            UnityEngine.Object.DestroyImmediate(groundMaterial);
        }
    }
}
#endif
