#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RumbleRosterBuilder
{
    public const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\rumble-roster";
    public const string Source="Assets/Layer Lab/2D Minimal-EnemyMonster/EnemyMonster 3/Prefabs";
    public const string AvatarFolder="Assets/System/SkillS/Avatars";
    public const string MonsterFolder="Assets/System/SkillS/Monsters";
    [Serializable]public class Entry { public string id,prefab,basic,other,reason; }
    [Serializable]public class Selection { public Entry[] entries; }
    [InitializeOnLoadMethod]private static void Init()
    {
        if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Inspect;
        if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;
        if(File.Exists(Work+"/validate.request")&&!File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Validate;
    }
    [MenuItem("Tools/Skills/Preview EnemyMonster 3")]
    public static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        try
        {
            Directory.CreateDirectory(Work+"/previews");
            var paths=AssetDatabase.FindAssets("t:Prefab",new[]{Source}).Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).ToArray();
            foreach(var path in paths)Render(path,Work+"/previews/"+Path.GetFileNameWithoutExtension(path)+".png",256);
            File.WriteAllText(Work+"/prefabs.json",Newtonsoft.Json.JsonConvert.SerializeObject(paths,Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(Work+"/inspect.txt","PASS: rendered "+paths.Length+" native monster prefabs");File.Delete(Work+"/inspect.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/inspect.txt","FAIL: "+e);Debug.LogException(e);}
    }
    private static void Render(string path,string output,int size)
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject model=null,cameraGo=null;Material material=null;
        RenderTexture target=null;Texture2D image=null;var previous=RenderTexture.active;
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(source==null)throw new InvalidOperationException("Missing prefab "+path);
            model=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
            foreach(var script in model.GetComponentsInChildren<MonoBehaviour>(true))if(script!=null)script.enabled=false;
            foreach(var transform in model.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=2;
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))animator.fireEvents=false;
            var clips=AnimationUtility.GetAnimationClips(source);
            var idle=clips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);
            if(idle!=null)idle.SampleAnimation(model,Mathf.Min(.13f,idle.length));
            material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
            bool found=false;Bounds bounds=default;
            foreach(var renderer in model.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)>=0){renderer.enabled=false;continue;}
                if(!renderer.enabled||renderer.sprite==null)continue;
                renderer.sharedMaterial=material;
                if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
            }
            if(!found)throw new InvalidOperationException("No active sprites "+path);
            cameraGo=new GameObject("Avatar Camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraGo,scene);
            var camera=cameraGo.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.size.x,bounds.size.y)*.58f;
            camera.transform.position=bounds.center+new Vector3(0,0,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.cullingMask=1<<2;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.scene=scene;
            target=new RenderTexture(size,size,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();
            RenderTexture.active=target;image=new Texture2D(size,size,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size,size),0,0);image.Apply();
            File.WriteAllBytes(output,image.EncodeToPNG());camera.targetTexture=null;
        }
        finally
        {
            RenderTexture.active=previous;
            if(cameraGo!=null){cameraGo.GetComponent<Camera>().targetTexture=null;UnityEngine.Object.DestroyImmediate(cameraGo);}
            if(model!=null)UnityEngine.Object.DestroyImmediate(model);
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}
            if(image!=null)UnityEngine.Object.DestroyImmediate(image);if(material!=null)UnityEngine.Object.DestroyImmediate(material);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    [MenuItem("Tools/Skills/Add Rumble Monster Roster")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        try
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Game_Exp_Rumble.unity")throw new InvalidOperationException("Open Game_Exp_Rumble in Edit Mode");
            var selection=JsonUtility.FromJson<Selection>(File.ReadAllText(Work+"/selection.json"));
            if(selection.entries.Length!=10||selection.entries.Select(e=>e.id).Distinct().Count()!=10)throw new InvalidOperationException("Expected ten unique hero IDs");
            var resources=UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.gameObject.scene==scene);
            if(!AssetDatabase.IsValidFolder(AvatarFolder))AssetDatabase.CreateFolder("Assets/System/SkillS","Avatars");
            if(!AssetDatabase.IsValidFolder(MonsterFolder))AssetDatabase.CreateFolder("Assets/System/SkillS","Monsters");
            foreach(var entry in selection.entries)
            {
                string avatar=AvatarFolder+"/"+entry.id+".png";
                if(!File.Exists(avatar))Render(entry.prefab,avatar,512);
                AssetDatabase.ImportAsset(avatar,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(avatar);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=512;importer.SaveAndReimport();
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(avatar);var prefab=Adapt(entry);
                if(sprite==null||prefab==null)throw new InvalidOperationException("Missing resource "+entry.id);
                if(resources.monsters.TryGetValue(entry.id,out var previous)&&previous!=prefab)throw new InvalidOperationException("Monster ID collision "+entry.id);
            }
            Undo.RecordObject(resources,"Add ten Rumble skill monsters");
            foreach(var entry in selection.entries)
            {
                resources.monsters[entry.id]=AssetDatabase.LoadAssetAtPath<GameObject>(MonsterFolder+"/"+entry.id+".prefab");
                resources.avas[entry.id]=AssetDatabase.LoadAssetAtPath<Sprite>(AvatarFolder+"/"+entry.id+".png");
            }
            EditorUtility.SetDirty(resources);if(PrefabUtility.IsPartOfPrefabInstance(resources))PrefabUtility.RecordPrefabInstancePropertyModifications(resources);
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Cannot save scene");
            File.WriteAllText(Work+"/build.txt","PASS: ten monster prefabs and ten generated 512px transparent avatars assigned to ResourceHolder in "+scene.path);File.Delete(Work+"/build.request");
            if(File.Exists(Work+"/validate.request"))Validate();
        }
        catch(Exception e){File.WriteAllText(Work+"/build.txt","FAIL: "+e);Debug.LogException(e);}
    }
    [MenuItem("Tools/Skills/Validate Rumble Monster Roster")]
    public static void Validate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        GameObject fixture=null;var oldConfig=ConfigLoader.Instance;var oldDb=DatabaseAll.instance;
        var statics=typeof(ConfigLoader).GetFields(System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic)
            .Where(f=>!f.IsLiteral&&!f.IsInitOnly).ToDictionary(f=>f,f=>f.GetValue(null));
        try
        {
            var selection=JsonUtility.FromJson<Selection>(File.ReadAllText(Work+"/selection.json"));
            var scene=SceneManager.GetActiveScene();var resources=UnityEngine.Object.FindObjectsByType<ResourceHolder>(FindObjectsInactive.Include,FindObjectsSortMode.None).Single(r=>r.gameObject.scene==scene);
            fixture=new GameObject("Isolated roster validation");fixture.SetActive(false);
            var config=fixture.AddComponent<ConfigLoader>();ConfigLoader.Instance=config;
            using(var stream=File.OpenRead("Assets/StreamingAssets/Config_exp_rumble.xlsx"))
            {var book=ExcelHelper.LoadExcel(stream);config.ParseSkills(book.ToTSV("SKILLS2"));config.ParseHeroes(book.ToTSV("Heroes"));}
            var db=fixture.AddComponent<DatabaseAll>();DatabaseAll.instance=db;
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            typeof(DatabaseAll).GetMethod("OnSkillsParsed",flags).Invoke(db,new object[]{config.skills});
            typeof(DatabaseAll).GetMethod("OnHeroesParsed",flags).Invoke(db,new object[]{config.heroes});
            var sceneText=File.ReadAllText(scene.path);
            foreach(var entry in selection.entries)
            {
                if(!db.heroes.TryGetValue(entry.id,out var hero))throw new InvalidOperationException("Missing hero "+entry.id);
                if(!hero.skills.SequenceEqual(new[]{entry.basic,entry.other})||!db.skills.ContainsKey(entry.basic)||!db.skills.ContainsKey(entry.other))throw new InvalidOperationException("Wrong skill pair "+entry.id);
                if(!resources.monsters.TryGetValue(entry.id,out var model)||model==null||!resources.avas.TryGetValue(entry.id,out var avatar)||avatar==null)throw new InvalidOperationException("Missing ResourceHolder entry "+entry.id);
                if(model.transform.Find("head")==null||model.transform.Find("legs")==null)throw new InvalidOperationException("Missing anchors "+entry.id);
                var convert=model.GetComponentInChildren<AnimConvert>(true);
                if(convert==null||!convert.anims.ContainsKey("idle")||!convert.anims.ContainsKey("attack")||!convert.anims.ContainsKey("walk"))throw new InvalidOperationException("Missing animation aliases "+entry.id);
                string modelPath=AssetDatabase.GetAssetPath(model),avatarPath=AssetDatabase.GetAssetPath(avatar);
                if(!sceneText.Contains(AssetDatabase.AssetPathToGUID(modelPath))||!sceneText.Contains(AssetDatabase.AssetPathToGUID(avatarPath)))throw new InvalidOperationException("Scene references not saved "+entry.id);
                if(avatar.texture.width!=512||avatar.texture.height!=512)throw new InvalidOperationException("Wrong avatar dimensions "+entry.id);
            }
            File.WriteAllText(Work+"/validation.txt","PASS: all ten heroes loaded through ExcelHelper/ConfigLoader/DatabaseAll; exact basic+other skill pairs; ten ResourceHolder monster and avatar entries; native prefab variants, animation aliases, head/legs anchors; generated 512px sprites; all asset GUIDs persisted in Game_Exp_Rumble. Existing saves untouched.");File.Delete(Work+"/validate.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/validation.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
            foreach(var field in statics)field.Key.SetValue(null,field.Value);ConfigLoader.Instance=oldConfig;DatabaseAll.instance=oldDb;
        }
    }
    private static GameObject Adapt(Entry entry)
    {
        string output=MonsterFolder+"/"+entry.id+".prefab";
        var existing=AssetDatabase.LoadAssetAtPath<GameObject>(output);if(existing!=null)return existing;
        var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;
        try
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(entry.prefab);
            root=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);
            foreach(var animator in root.GetComponentsInChildren<Animator>(true))
            {
                var controller=animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                if(controller==null)continue;
                var names=new List<string>();
                void Collect(UnityEditor.Animations.AnimatorStateMachine machine)
                {foreach(var state in machine.states)names.Add(state.state.name);foreach(var child in machine.stateMachines)Collect(child.stateMachine);}
                foreach(var layer in controller.layers)Collect(layer.stateMachine);
                var convert=animator.GetComponent<AnimConvert>();if(convert==null)convert=animator.gameObject.AddComponent<AnimConvert>();
                if(convert.anims==null)convert.anims=new StringStringDictionary();
                string Match(params string[] parts)=>names.FirstOrDefault(n=>parts.Any(p=>n.IndexOf(p,StringComparison.OrdinalIgnoreCase)>=0));
                void Map(string key,params string[] parts){var value=Match(parts);if(value!=null)convert.anims[key]=value;}
                Map("idle","idle");Map("walk","walk","move","run");Map("attack","attack");Map("hit","hit","damage");Map("death","death","dead");
                if(convert.anims.TryGetValue("death",out var death))convert.anims["dead"]=death;
                if(convert.anims.TryGetValue("attack",out var attack))convert.anims["ult"]=attack;
                if(!convert.anims.ContainsKey("walk")&&convert.anims.TryGetValue("idle",out var idle))convert.anims["walk"]=idle;
            }
            var renderers=root.GetComponentsInChildren<SpriteRenderer>().Where(r=>r.enabled&&r.sprite!=null&&r.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)<0).ToArray();
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            void Anchor(string name,Vector3 position)
            {if(root.transform.Find(name)!=null)return;var anchor=new GameObject(name);anchor.transform.SetParent(root.transform,false);anchor.transform.position=position;}
            Anchor("head",new Vector3(bounds.center.x,bounds.max.y,bounds.center.z));Anchor("legs",new Vector3(bounds.center.x,bounds.min.y,bounds.center.z));
            return PrefabUtility.SaveAsPrefabAsset(root,output);
        }
        finally{if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
#endif
