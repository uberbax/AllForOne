#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PlagueTreeBuilder
{
    const string Root="Assets/System/Effects/PlagueTrail";
    const string Flag="Assets/System/Effects/Editor/.plague-trees";
    static readonly string[] Folders={
        "Assets/Layer Lab/2D Minimal World/Pack/Forest/Nature/ResourcesData/Sprites/Tree",
        "Assets/Layer Lab/2D Minimal-Environment/Environment 1/ResourcesData/Sprites/Tree"
    };
    [InitializeOnLoadMethod] static void Init() { if(File.Exists(Flag)) EditorApplication.delayCall+=Requested; }
    static void Requested()
    {
        if(EditorApplication.isPlaying) return;
        File.Delete(Flag);
        try {
            Build();
        } catch(Exception e) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-trees-build.txt"),"FAIL: "+e); Debug.LogException(e); }
    }
    [MenuItem("Tools/Effects/Build Plague Tree Infection")]
    public static void Build()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<PlagueTreeCatalog>(Root+"/TreeCatalog.asset");
        if(catalog==null) { catalog=ScriptableObject.CreateInstance<PlagueTreeCatalog>(); AssetDatabase.CreateAsset(catalog,Root+"/TreeCatalog.asset"); }
        var previous=catalog.replacements.ToDictionary(p=>p.living,p=>p.dead);
        catalog.replacements.Clear();
        foreach(var folder in Folders)
        {
            var sprites=AssetDatabase.FindAssets("t:Sprite",new[]{folder}).Select(AssetDatabase.GUIDToAssetPath)
                .Distinct().SelectMany(path=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()).OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            var dead=sprites.Where(s=>s.name.StartsWith("Dead_",StringComparison.Ordinal)).ToArray();
            if(dead.Length==0) throw new Exception("No Dead_ sprites in "+folder);
            foreach(var living in sprites.Where(s=>!s.name.StartsWith("Dead_",StringComparison.Ordinal)))
            {
                bool white=living.name.IndexOf("Birch",StringComparison.OrdinalIgnoreCase)>=0 || living.name.IndexOf("White",StringComparison.OrdinalIgnoreCase)>=0;
                var variants=dead.Where(s=>s.name.Contains(white?"White":"Brown")).ToArray();
                if(variants.Length==0) variants=dead;
                uint hash=2166136261; foreach(char c in living.name) hash=unchecked((hash^c)*16777619);
                var deadSprite=variants[hash%(uint)variants.Length];
                // The withered-tree shader stretches this FullRect sprite to the original renderer size.
                // Its normalized pivot also preserves the original bounds centre, not only the foot.
                Bounds bounds=living.bounds;
                Vector2 pivot=new Vector2(.5f-bounds.center.x/bounds.size.x,.5f-bounds.center.y/bounds.size.y);
                var generated=Sprite.Create(deadSprite.texture,deadSprite.rect,pivot,deadSprite.pixelsPerUnit,0,SpriteMeshType.FullRect);
                generated.name=deadSprite.name;
                if (previous.TryGetValue(living,out var aligned) && AssetDatabase.GetAssetPath(aligned)==AssetDatabase.GetAssetPath(catalog))
                {
                    EditorUtility.CopySerialized(generated,aligned);
                    EditorUtility.SetDirty(aligned);
                    UnityEngine.Object.DestroyImmediate(generated);
                }
                else
                {
                    aligned=generated;
                    AssetDatabase.AddObjectToAsset(aligned,catalog);
                }
                catalog.replacements.Add(new PlagueTreeCatalog.Replacement{living=living,dead=aligned});
            }
        }
        EditorUtility.SetDirty(catalog); AssetDatabase.SaveAssetIfDirty(catalog);
        var root=PrefabUtility.LoadPrefabContents(Root+"/PlagueTrail.prefab");
        try { Configure(root,catalog); PrefabUtility.SaveAsPrefabAsset(root,Root+"/PlagueTrail.prefab"); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        int matched=0;
        var known=new HashSet<Sprite>(catalog.replacements.Select(p=>p.living));
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(renderer.sprite!=null && known.Contains(renderer.sprite)) matched++;
        var mappings=catalog.replacements.ToDictionary(p=>p.living,p=>p.dead);
        var samples=UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(r=>r.sprite!=null && known.Contains(r.sprite)).Take(10).Select(r=>new {r.name, sprite=r.sprite.name, replacement=mappings[r.sprite].name,
                pivot=r.sprite.pivot.ToString(), deadPivot=mappings[r.sprite].pivot.ToString(), bounds=r.sprite.bounds.ToString(), deadBounds=mappings[r.sprite].bounds.ToString()}).ToArray();
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-trees-build.txt"),"PASS: "+catalog.replacements.Count+" mappings; "+matched+" trees matched in loaded scenes; prefab updated.");
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"plague-trees-samples.json"),Newtonsoft.Json.JsonConvert.SerializeObject(samples,Newtonsoft.Json.Formatting.Indented));
    }
    public static void Configure(GameObject root,PlagueTreeCatalog catalog=null)
    {
        if(catalog==null) catalog=AssetDatabase.LoadAssetAtPath<PlagueTreeCatalog>(Root+"/TreeCatalog.asset");
        if(catalog==null) return;
        var infection=root.GetComponent<PlagueTreeInfection>();
        if(infection==null) infection=root.AddComponent<PlagueTreeInfection>();
        var shader=Shader.Find("AllForOne/Effects/WitheredTree");
        if(shader==null || ShaderUtil.ShaderHasError(shader)) throw new Exception("WitheredTree shader failed");
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/WitheredTree.mat");
        if(material==null) { material=new Material(shader); AssetDatabase.CreateAsset(material,Root+"/WitheredTree.mat"); }
        var fields=new SerializedObject(infection);
        fields.FindProperty("witheredMaterial").objectReferenceValue=material;
        fields.FindProperty("catalog").objectReferenceValue=catalog;
        var radius=fields.FindProperty("playerContactRadius");
        if(Mathf.Approximately(radius.floatValue,.3f)) radius.floatValue=1.5f;
        fields.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
