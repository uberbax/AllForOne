#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class ButcherReferenceArt
{
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\reference-art";
    const string Art="Assets/System/Art/ButcherReference/";
    const string Ground="Assets/Scenes/EvergreenRPG/";
    [InitializeOnLoadMethod] static void Init(){if(File.Exists(Work+"/audit.request"))EditorApplication.delayCall+=Audit;if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;if(File.Exists(Work+"/plants.request"))EditorApplication.delayCall+=LightweightPlants;if(File.Exists(Work+"/refine.request"))EditorApplication.delayCall+=Refine;if(File.Exists(Work+"/final.request"))EditorApplication.delayCall+=FinalPalette;}
    static void Audit()
    {
        try
        {
            var s=EditorSceneManager.GetActiveScene();var roots=s.GetRootGameObjects();
            var holders=roots.SelectMany(g=>g.GetComponentsInChildren<ResourceHolder>(true)).ToArray();
            var models=holders.SelectMany(h=>h.monsters).Where(p=>p.Value!=null&&p.Value.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length>0).Select(p=>new{id=p.Key,path=AssetDatabase.GetAssetPath(p.Value),materials=p.Value.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Distinct().Select(m=>new{path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,keywords=m.shaderKeywords})});
            File.WriteAllText(Work+"/audit.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=s.path,models},Newtonsoft.Json.Formatting.Indented));File.Delete(Work+"/audit.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }
    static float Hash(int x,int y){float n=Mathf.Sin(((x%6+6)%6)*127.1f+((y%6+6)%6)*311.7f)*43758.5453f;return n-Mathf.Floor(n);}
    static Texture2D Texture(string name,bool stone)
    {
        const int size=512;var texture=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float u=(x+.5f)/size,v=(y+.5f)/size;Color c;
            if(stone)
            {
                var p=new Vector2(u*6,v*6);int cx=Mathf.FloorToInt(p.x),cy=Mathf.FloorToInt(p.y);float first=100,second=100,variation=0;Vector2 delta=default;
                for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)
                {
                    int ix=cx+i,iy=cy+j;var d=p-new Vector2(ix+.18f+.64f*Hash(ix,iy),iy+.18f+.64f*Hash(iy+7,ix+3));float distance=d.sqrMagnitude;
                    if(distance<first){second=first;first=distance;variation=Hash(ix+2,iy+1);delta=d;}else if(distance<second)second=distance;
                }
                float edge=Mathf.Sqrt(second)-Mathf.Sqrt(first);float fill=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.095f,edge));
                var surface=Color.Lerp(new Color(.57f,.43f,.25f),new Color(.76f,.61f,.38f),variation);surface*=1+.08f*Vector2.Dot(delta.normalized,new Vector2(-.6f,.8f));
                c=Color.Lerp(new Color(.33f,.255f,.15f),surface,fill);
            }
            else
            {
                float a=Mathf.Lerp(Mathf.PerlinNoise(u*8+10,v*8+10),Mathf.PerlinNoise((u-1)*8+10,v*8+10),u);
                float b=Mathf.Lerp(Mathf.PerlinNoise(u*8+10,(v-1)*8+10),Mathf.PerlinNoise((u-1)*8+10,(v-1)*8+10),u);
                c=Color.Lerp(new Color(.19f,.43f,.155f),new Color(.29f,.56f,.21f),Mathf.Lerp(a,b,v));
            }
            pixels[y*size+x]=c;
        }
        texture.SetPixels(pixels);texture.Apply();string path=Ground+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Set(Material m,string key,float v){if(m.HasProperty(key))m.SetFloat(key,v);}
    static void Set(Material m,string key,Color v){if(m.HasProperty(key))m.SetColor(key,v);}
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static void Render(Camera c,string file)
    {
        var old=c.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(900,2000,24);Texture2D image=null;
        try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Work+"/"+file,image.EncodeToPNG());}
        finally{c.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Build()
    {
        bool async=ShaderUtil.allowAsyncCompilation;GameObject preview=null;GameObject contents=null;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Butcher in Edit Mode");
            var roots=scene.GetRootGameObjects();var camera=roots.Single(g=>g.name=="Main Camera").GetComponent<Camera>();ShaderUtil.allowAsyncCompilation=false;
            var grass=Texture("ReferenceGrass",false);var stone=Texture("ReferenceStonePath",true);
            var material=AssetDatabase.LoadAssetAtPath<Material>(Ground+"ForestGround.mat");Undo.RecordObject(material,"Reference forest ground");material.SetTexture("_GrassTex",grass);material.SetTexture("_StoneTex",stone);material.SetFloat("_GrassScale",.15f);material.SetFloat("_StoneScale",.25f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            var generator=typeof(EvergreenRPGBuilder);var flags=BindingFlags.Static|BindingFlags.NonPublic;
            var height=(Func<float,float,float>)Delegate.CreateDelegate(typeof(Func<float,float,float>),generator.GetMethod("Height",flags));var road=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("RoadDistance",flags));var clearing=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("ClearingDistance",flags));var bank=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("BankDistance",flags));
            foreach(string file in new[]{"Forest terrain.asset","Stream bed - Not Walkable.asset"})
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Ground+file);var vertices=mesh.vertices;var triangles=mesh.triangles;var colors=mesh.colors;Undo.RegisterCompleteObjectUndo(mesh,"Paint stone paths");
                for(int i=0;i<vertices.Length;i++){var p=new Vector2(vertices[i].x,vertices[i].z);float noise=Mathf.PerlinNoise(p.x*.28f+70,p.y*.28f+70);float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,2.65f,road(p)+.4f*(noise-.5f)));float open=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.8f,5.7f,clearing(p)));colors[i].a=Mathf.Max(path,open*.32f);}
                mesh.colors=colors;if(!mesh.vertices.SequenceEqual(vertices)||!mesh.triangles.SequenceEqual(triangles))throw new Exception("Ground geometry changed");EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
            }
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Suriyun/RedKnight/Prefab/M05/M05.prefab");contents=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));var materials=new Dictionary<Material,Material>();
            foreach(var renderer in contents.GetComponentsInChildren<Renderer>(true))
            {
                var array=renderer.sharedMaterials;
                for(int i=0;i<array.Length;i++)
                {
                    var old=array[i];if(old==null)continue;
                    if(!materials.TryGetValue(old,out var m))
                    {
                        m=new Material(old){name=old.name+"_Reference"};
                        if(m.shader.name=="Toon/Toon")
                        {
                            m.EnableKeyword("UTS_RP_BUILTIN");m.EnableKeyword("_OUTLINE_NML");m.DisableKeyword("_OUTLINE_POS");
                            Set(m,"_Outline_Color",new Color(.045f,.055f,.04f));Set(m,"_Outline_Width",18);Set(m,"_Farthest_Distance",200);Set(m,"_Nearest_Distance",.1f);Set(m,"_Is_LightColor_Outline",0);Set(m,"_Is_BlendBaseColor",0);
                            Set(m,"_Is_Filter_LightColor",1);Set(m,"_RimLight",0);Set(m,"_MatCap",0);Set(m,"_HighColor",Color.black);Set(m,"_Emissive_Color",Color.black);Set(m,"_Unlit_Intensity",.65f);
                            Set(m,"_BaseColor",new Color(.82f,.84f,.84f));Set(m,"_Color",new Color(.82f,.84f,.84f));Set(m,"_1st_ShadeColor",new Color(.58f,.64f,.65f));Set(m,"_2nd_ShadeColor",new Color(.39f,.46f,.47f));Set(m,"_BaseColor_Step",.48f);Set(m,"_BaseShade_Feather",.12f);
                        }
                        AssetDatabase.CreateAsset(m,Art+m.name+".mat");materials.Add(old,m);
                    }
                    array[i]=m;
                }
                renderer.sharedMaterials=array;
            }
            string variantPath=Art+"RedKnight_Reference.prefab";var variant=PrefabUtility.SaveAsPrefabAsset(contents,variantPath);PrefabUtility.UnloadPrefabContents(contents);contents=null;
            int links=0;
            foreach(var holder in roots.SelectMany(g=>g.GetComponentsInChildren<ResourceHolder>(true)))
            {
                Undo.RecordObject(holder,"Use outlined hero in Butcher");
                foreach(var table in new[]{holder.monsters,holder.heroClasses})foreach(var pair in table.ToArray())if(pair.Value==source){table[pair.Key]=variant;links++;}
                PrefabUtility.RecordPrefabInstancePropertyModifications(holder);EditorUtility.SetDirty(holder);
            }
            var decor=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t=>t.name=="03 Undergrowth and bridges");if(decor==null)throw new Exception("Missing forest detail parent");
            if(decor.Find("Reference low vegetation")!=null)throw new Exception("Reference vegetation already exists");
            var low=new GameObject("Reference low vegetation");low.transform.SetParent(decor,false);Undo.RegisterCreatedObjectUndo(low,"Add low forest plants");var random=new System.Random(458);var batches=new Dictionary<Material,List<CombineInstance>>();int plants=0;
            for(float z=-38;z<39;z+=1.35f)for(float x=-38;x<39;x+=1.35f)
            {
                var p=new Vector2(x+((float)random.NextDouble()-.5f),z+((float)random.NextDouble()-.5f));if(road(p)<2.45f||clearing(p)<4.4f||bank(p)<2.9f||random.NextDouble()<.2)continue;
                string name;float scale;double choice=random.NextDouble();if(choice<.60){name="EF_Grasses_Small";scale=.6f+(float)random.NextDouble()*.35f;}else if(choice<.85){name="EF_Brush_Small_Summer";scale=.35f+(float)random.NextDouble()*.25f;}else{name=random.NextDouble()<.5?"EF_Flowers1_White":"EF_Flowers1_Pink";scale=.5f;}
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Soldium Studio/Evergreen Forest/Prefabs/"+name+".prefab");var lod=prefab.GetComponent<LODGroup>();var renderers=lod==null?prefab.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray():lod.GetLODs()[0].renderers;
                var b=BoundsOf(prefab);var placement=Matrix4x4.TRS(new Vector3(p.x,height(p.x,p.y)-b.min.y*scale-.02f,p.y),Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*scale);
                foreach(var r in renderers){var mf=r.GetComponent<MeshFilter>();if(mf==null)continue;for(int sm=0;sm<Mathf.Min(mf.sharedMesh.subMeshCount,r.sharedMaterials.Length);sm++){var mat=r.sharedMaterials[sm];if(!batches.TryGetValue(mat,out var list)){list=new List<CombineInstance>();batches.Add(mat,list);}list.Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=sm,transform=placement*mf.transform.localToWorldMatrix});}}
                plants++;
            }
            int batch=0;foreach(var pair in batches){var mesh=new Mesh{name="Low forest plants "+batch,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);AssetDatabase.CreateAsset(mesh,Art+"LowPlants"+batch+".asset");var go=new GameObject(mesh.name);go.transform.SetParent(low.transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);batch++;}
            // Temporary preview uses the actual configured hero prefab; no extra hero is saved in the scene.
            preview=(GameObject)PrefabUtility.InstantiatePrefab(variant);var target=camera.transform.position+camera.transform.forward*((.7f-camera.transform.position.y)/camera.transform.forward.y);preview.transform.position=target;preview.transform.rotation=Quaternion.Euler(0,155,0);
            var animator=preview.GetComponentInChildren<Animator>();if(animator!=null&&animator.runtimeAnimatorController!=null){var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);if(clip!=null)clip.SampleAnimation(animator.gameObject,.3f);}
            Render(camera,"scene-preview.png");var modelBounds=BoundsOf(preview);var savedPosition=camera.transform.position;float savedSize=camera.orthographicSize;camera.orthographicSize=Mathf.Max(.8f,modelBounds.size.y*.8f);camera.transform.position=modelBounds.center-camera.transform.forward*12;Render(camera,"hero-preview.png");camera.transform.position=savedPosition;camera.orthographicSize=savedSize;
            UnityEngine.Object.DestroyImmediate(preview);preview=null;EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save scene");
            var messages=ShaderUtil.GetShaderMessages(material.shader);if(messages.Any(m=>m.severity.ToString()=="Error"))throw new Exception("Ground shader compilation error");
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",plants,batches=batch,heroMaterials=materials.Count,heroLinks=links,variant=variantPath,geometryUnchanged=true},Newtonsoft.Json.Formatting.Indented));File.Delete(Work+"/build.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/build-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);if(contents!=null)PrefabUtility.UnloadPrefabContents(contents);}
    }
    static void Refine()
    {
        bool async=ShaderUtil.allowAsyncCompilation;GameObject preview=null;Camera camera=null;Vector3 savedPosition=default;float savedSize=0;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Butcher Edit Mode");
            var roots=scene.GetRootGameObjects();camera=roots.Single(g=>g.name=="Main Camera").GetComponent<Camera>();savedPosition=camera.transform.position;savedSize=camera.orthographicSize;
            var sun=roots.Single(g=>g.name=="Sun - late afternoon").GetComponent<Light>();Undo.RecordObject(sun,"Reference daylight colors");sun.color=new Color(1,.99f,.96f);sun.intensity=.95f;
            Undo.RecordObject(Unsupported.GetRenderSettings(),"Reference forest fill lighting");RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.38f,.47f,.45f);RenderSettings.ambientEquatorColor=new Color(.24f,.34f,.25f);RenderSettings.ambientGroundColor=new Color(.14f,.22f,.12f);
            ShaderUtil.allowAsyncCompilation=false;
            var variant=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"RedKnight_Reference.prefab");preview=(GameObject)PrefabUtility.InstantiatePrefab(variant);
            var animator=preview.GetComponentInChildren<Animator>();if(animator!=null&&animator.runtimeAnimatorController!=null){var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);if(clip!=null)clip.SampleAnimation(animator.gameObject,.3f);}
            var target=camera.transform.position+camera.transform.forward*((.7f-camera.transform.position.y)/camera.transform.forward.y);preview.transform.position=target;preview.transform.rotation=Quaternion.Euler(0,155,0);
            Render(camera,"soft-lighting.png");
            var importer=(TextureImporter)AssetImporter.GetAtPath(Ground+"ReferenceGrass.png");importer.sRGBTexture=false;importer.SaveAndReimport();Render(camera,"linear-grass.png");
            // Keep the gamma-correct texture while evaluating the brighter alternative.
            importer.sRGBTexture=true;importer.SaveAndReimport();
            var b=BoundsOf(preview);camera.orthographicSize=Mathf.Max(.8f,b.size.y*.8f);camera.transform.position=b.center-camera.transform.forward*12;Render(camera,"hero-final.png");camera.transform.position=savedPosition;camera.orthographicSize=savedSize;
            UnityEngine.Object.DestroyImmediate(preview);preview=null;
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"LowPlants0.asset");if(mesh==null||mesh.vertexCount==0)throw new Exception("Vegetation batch is empty");
            EditorUtility.SetDirty(sun);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Work+"/refine-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",plantVertices=mesh.vertexCount,shadowStrength=sun.shadowStrength,ambient=RenderSettings.ambientMode.ToString()},Newtonsoft.Json.Formatting.Indented));File.Delete(Work+"/refine.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/refine-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;if(camera!=null){camera.transform.position=savedPosition;camera.orthographicSize=savedSize;}if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
    static void LightweightPlants()
    {
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Butcher Edit Mode");
            var parent=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Reference low vegetation");
            var generator=typeof(EvergreenRPGBuilder);var flags=BindingFlags.NonPublic|BindingFlags.Static;
            var height=(Func<float,float,float>)Delegate.CreateDelegate(typeof(Func<float,float,float>),generator.GetMethod("Height",flags));var road=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("RoadDistance",flags));var clearing=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("ClearingDistance",flags));var bank=(Func<Vector2,float>)Delegate.CreateDelegate(typeof(Func<Vector2,float>),generator.GetMethod("BankDistance",flags));
            var vertices=new List<Vector3>();var indices=new List<int>();var bushes=new List<CombineInstance>();var random=new System.Random(6458);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Soldium Studio/Evergreen Forest/Prefabs/EF_Brush_Small_Summer.prefab");var groups=prefab.GetComponentsInChildren<LODGroup>(true);var renderers=groups.Length==0?prefab.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray():groups.SelectMany(l=>l.GetLODs().Last().renderers).Distinct().ToArray();
            int tufts=0,bushCount=0;
            for(float z=-38;z<39;z+=1.35f)for(float x=-38;x<39;x+=1.35f)
            {
                var p=new Vector2(x+(float)random.NextDouble()-.5f,z+(float)random.NextDouble()-.5f);if(road(p)<2.45f||clearing(p)<4.4f||bank(p)<2.9f||random.NextDouble()<.2)continue;
                var origin=new Vector3(p.x,height(p.x,p.y),p.y);
                if(random.NextDouble()<.09)
                {
                    float scale=.38f+(float)random.NextDouble()*.22f;var placement=Matrix4x4.TRS(origin,Quaternion.Euler(0,(float)random.NextDouble()*360,0),Vector3.one*scale);
                    foreach(var r in renderers){var mf=r.GetComponent<MeshFilter>();if(mf!=null)bushes.Add(new CombineInstance{mesh=mf.sharedMesh,transform=placement*mf.transform.localToWorldMatrix});}bushCount++;continue;
                }
                for(int blade=0;blade<3;blade++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,h=.25f+(float)random.NextDouble()*.4f,w=.035f+(float)random.NextDouble()*.045f;var side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var lean=new Vector3(-side.z,0,side.x)*(.07f+(float)random.NextDouble()*.08f);
                    int n=vertices.Count;vertices.AddRange(new[]{origin-side*w,origin+side*w,origin+Vector3.up*(h*.6f)+lean*.5f+side*(w*.35f),origin+Vector3.up*h+lean,origin+Vector3.up*(h*.6f)+lean*.5f-side*(w*.35f)});
                    int[] face={0,1,2,0,2,4,4,2,3};for(int i=0;i<face.Length;i+=3){indices.Add(n+face[i]);indices.Add(n+face[i+1]);indices.Add(n+face[i+2]);indices.Add(n+face[i+2]);indices.Add(n+face[i+1]);indices.Add(n+face[i]);}
                }
                tufts++;
            }
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(Art+"LowPlants0.asset");Undo.RegisterCompleteObjectUndo(old,"Use light forest bushes");old.Clear();old.CombineMeshes(bushes.ToArray(),true,true);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);
            var grassMesh=new Mesh{name="Small stylized grass tufts",indexFormat=IndexFormat.UInt32};grassMesh.SetVertices(vertices);grassMesh.SetTriangles(indices,0);grassMesh.SetNormals(Enumerable.Repeat(Vector3.up,vertices.Count).ToList());grassMesh.SetColors(Enumerable.Repeat(new Color(1,1,1,0),vertices.Count).ToList());grassMesh.RecalculateBounds();AssetDatabase.CreateAsset(grassMesh,Art+"GrassTufts.asset");
            var mat=new Material(AssetDatabase.LoadAssetAtPath<Material>(Ground+"ForestGround.mat")){name="Fresh grass tufts"};mat.SetColor("_Color",new Color(1.1f,1.55f,.92f));AssetDatabase.CreateAsset(mat,Art+"GrassTufts.mat");var go=new GameObject("Small grass tufts");go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=grassMesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;
            if(old.vertexCount>200000||grassMesh.vertexCount>100000)throw new Exception("Vegetation geometry budget exceeded");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Work+"/plants-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",tufts,bushes=bushCount,grassVertices=grassMesh.vertexCount,bushVertices=old.vertexCount},Newtonsoft.Json.Formatting.Indented));File.Delete(Work+"/plants.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/plants-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void FinalPalette()
    {
        try
        {
            foreach(string name in new[]{"ReferenceGrass","ReferenceStonePath"}){var importer=(TextureImporter)AssetImporter.GetAtPath(Ground+name+".png");importer.sRGBTexture=false;importer.SaveAndReimport();}
            var ground=AssetDatabase.LoadAssetAtPath<Material>(Ground+"ForestGround.mat");ground.SetColor("_Color",new Color(.92f,.94f,1.02f));EditorUtility.SetDirty(ground);AssetDatabase.SaveAssetIfDirty(ground);
            var tufts=AssetDatabase.LoadAssetAtPath<Material>(Art+"GrassTufts.mat");tufts.SetColor("_Color",new Color(.82f,1.10f,.88f));EditorUtility.SetDirty(tufts);AssetDatabase.SaveAssetIfDirty(tufts);
            File.Delete(Work+"/final.request");
            EditorApplication.delayCall+=()=>EditorApplication.delayCall+=FinalPreview;
        }
        catch(Exception e){File.WriteAllText(Work+"/final-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void FinalPreview()
    {
        bool async=ShaderUtil.allowAsyncCompilation;GameObject preview=null;
        try
        {
            var scene=EditorSceneManager.GetActiveScene();if(scene.name!="Game_Exp_butcher_3d"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Butcher Edit Mode");
            var camera=scene.GetRootGameObjects().Single(g=>g.name=="Main Camera").GetComponent<Camera>();ShaderUtil.allowAsyncCompilation=false;
            preview=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"RedKnight_Reference.prefab"));var animator=preview.GetComponentInChildren<Animator>();if(animator!=null&&animator.runtimeAnimatorController!=null){var clip=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0);if(clip!=null)clip.SampleAnimation(animator.gameObject,.3f);}
            preview.transform.position=camera.transform.position+camera.transform.forward*((.7f-camera.transform.position.y)/camera.transform.forward.y);preview.transform.rotation=Quaternion.Euler(0,155,0);Render(camera,"final-scene.png");UnityEngine.Object.DestroyImmediate(preview);preview=null;
            Render(camera,"final-environment.png");EditorSceneManager.SaveScene(scene);
            var resources=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ResourceHolder>(true)).First();var hero=resources.monsters["hero"];var meshes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>(true)).Select(m=>m.sharedMesh).Where(m=>m!=null&&(AssetDatabase.GetAssetPath(m)==Art+"LowPlants0.asset"||AssetDatabase.GetAssetPath(m)==Art+"GrassTufts.asset")).Distinct().ToArray();
            File.WriteAllText(Work+"/final-result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",hero=AssetDatabase.GetAssetPath(hero),groundShader=AssetDatabase.LoadAssetAtPath<Material>(Ground+"ForestGround.mat").shader.name,vegetationVertices=meshes.Sum(m=>m.vertexCount),vegetationMeshes=meshes.Length,scene=scene.path},Newtonsoft.Json.Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(Work+"/final-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=async;if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);}
    }
}
#endif
