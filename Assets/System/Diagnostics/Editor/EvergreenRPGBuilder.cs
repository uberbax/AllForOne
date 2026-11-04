#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using NavMeshPlus.Components;

public static class EvergreenRPGBuilder
{
    const string Pack="Assets/Soldium Studio/Evergreen Forest";
    const string Out="Assets/Scenes/EvergreenRPG";
    const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\evergreen-rpg";
    [InitializeOnLoadMethod] static void Init(){if(File.Exists(Work+"/inspect.request"))EditorApplication.delayCall+=Inspect;if(File.Exists(Work+"/build.request"))EditorApplication.delayCall+=Build;if(File.Exists(Work+"/resume.request"))EditorApplication.delayCall+=Resume;if(File.Exists(Work+"/finish.request"))EditorApplication.delayCall+=Finish;}
    static Bounds BoundsOf(GameObject go){var rs=go.GetComponentsInChildren<Renderer>(true).Where(r=>!(r is ParticleSystemRenderer)&&!r.name.Contains("LOD1")&&!r.name.Contains("LOD2")&&!r.name.Contains("LOD3")).ToArray();if(rs.Length==0)return new Bounds(go.transform.position,Vector3.zero);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static void Inspect()
    {
        try
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=Pack+"/Scenes/Demo.unity"||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected Evergreen Demo in Edit Mode");
            var roots=scene.GetRootGameObjects();var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Pack+"/Prefabs"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>new{path=p,go=AssetDatabase.LoadAssetAtPath<GameObject>(p)}).ToArray();
            File.WriteAllText(Work+"/inspect.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,dirty=scene.isDirty,roots=roots.Select(g=>new{name=g.name,position=g.transform.position.ToString(),bounds=BoundsOf(g).ToString()}),prefabs=prefabs.Select(p=>new{path=p.path,bounds=BoundsOf(p.go).ToString(),colliders=p.go.GetComponentsInChildren<Collider>(true).Length,materials=p.go.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(m=>m.name+":"+m.shader.name).Distinct().ToArray()})},Newtonsoft.Json.Formatting.Indented));
            File.Delete(Work+"/inspect.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/error.txt",e.ToString());Debug.LogException(e);}
    }

    static System.Random random;
    static Scene map;
    static Transform landscape,woodland,details,ruins,markers;
    static readonly Dictionary<string,GameObject> prefabs=new Dictionary<string,GameObject>();
    static readonly List<Vector2> treePositions=new List<Vector2>();
    static readonly Vector2[] route={new Vector2(0,-35),new Vector2(0,-29),new Vector2(-8,-25),new Vector2(-18,-18),new Vector2(-19,-10),new Vector2(-18,-4),new Vector2(-18,3),new Vector2(-23,12),new Vector2(-19,20),new Vector2(-10,25),new Vector2(0,27),new Vector2(12,25),new Vector2(22,17),new Vector2(23,9),new Vector2(22,0),new Vector2(22,-8),new Vector2(23,-16),new Vector2(13,-23),new Vector2(0,-29)};
    static readonly Vector2[] clearings={new Vector2(-8,-25),new Vector2(-24,-13),new Vector2(-23,12),new Vector2(0,27),new Vector2(22,17),new Vector2(23,-16)};
    static float R(float min,float max){return min+(float)random.NextDouble()*(max-min);}
    static string Pick(params string[] choices){return choices[random.Next(choices.Length)];}
    static float River(float x){return -2+1.8f*Mathf.Sin(x*.095f);}
    static float BankDistance(Vector2 p){return Mathf.Abs(p.y-River(p.x));}
    static float Height(float x,float z)
    {
        float h=.55f+.18f*Mathf.Sin(x*.16f)*Mathf.Cos(z*.14f)+.15f*Mathf.PerlinNoise(x*.13f+50,z*.13f+50);
        h+=1.5f*Mathf.Exp(-((x*x)+(z-28)*(z-28))/260);
        h+=1.1f*Mathf.Pow(Mathf.Clamp01((Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))-31)/9),2);
        return Mathf.Lerp(-.75f,h,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,3.4f,Mathf.Abs(z-River(x)))));
    }
    static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b){var v=b-a;return Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude));}
    static float RoadDistance(Vector2 p){float d=999;for(int i=1;i<route.Length;i++)d=Mathf.Min(d,SegmentDistance(p,route[i-1],route[i]));d=Mathf.Min(d,SegmentDistance(p,new Vector2(-18,-18),clearings[1]));return d;}
    static float ClearingDistance(Vector2 p){return clearings.Min(c=>Vector2.Distance(c,p));}
    static Transform Group(string name,Transform parent=null){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,map);go.transform.SetParent(parent,false);return go.transform;}
    static GameObject Prefab(string name)
    {
        if(!prefabs.TryGetValue(name,out var p)){p=AssetDatabase.LoadAssetAtPath<GameObject>(Pack+"/Prefabs/"+name+".prefab");if(p==null)throw new Exception("Missing prefab "+name);prefabs.Add(name,p);}return p;
    }
    static GameObject Place(string name,Vector2 point,float yaw,float scale,Transform parent,bool collision=false)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(Prefab(name),map);go.transform.SetParent(parent,false);
        go.transform.localScale*=scale;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.position=new Vector3(point.x,Height(point.x,point.y),point.y);
        var b=BoundsOf(go);go.transform.position+=Vector3.up*(go.transform.position.y-b.min.y-.035f);
        if(collision)
        {
            var shape=new GameObject("Navigation obstacle");shape.transform.SetParent(go.transform,false);
            var original=BoundsOf(Prefab(name));
            if(name.Contains("Tree")||name.Contains("Pine"))
            {
                var c=shape.AddComponent<CapsuleCollider>();c.radius=.32f;c.height=2;c.center=new Vector3(0,1+original.min.y,0);
            }
            else
            {
                var c=shape.AddComponent<BoxCollider>();c.center=original.center;c.size=original.size;
                if(name.Contains("Rock")||name.Contains("Cliff"))c.size*=.8f;
            }
        }
        GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccludeeStatic);
        return go;
    }
    static Material Solid(string name,Color color,float smoothness=0)
    {
        var m=new Material(Shader.Find("Standard")){name=name,color=color,enableInstancing=true};m.SetFloat("_Glossiness",smoothness);AssetDatabase.CreateAsset(m,Out+"/"+name+".mat");return m;
    }
    static GameObject MeshObject(string name,List<Vector3> vertices,List<int> indices,List<Color> colors,Material material,bool collider)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);if(colors!=null)mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Out+"/"+name+".asset");
        var go=Group(name,landscape).gameObject;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;return go;
    }
    static void Ground()
    {
        var groundMaterial=new Material(Shader.Find("AllForOne/Evergreen RPG Ground")){name="Forest Ground"};AssetDatabase.CreateAsset(groundMaterial,Out+"/ForestGround.mat");
        var vertices=new[]{new List<Vector3>(),new List<Vector3>()};var indices=new[]{new List<int>(),new List<int>()};var colors=new[]{new List<Color>(),new List<Color>()};
        const int resolution=120;float step=80f/resolution;
        for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++)
        {
            var a=new Vector2(-40+x*step,-40+z*step);var b=a+new Vector2(step,0);var c=a+new Vector2(0,step);var d=a+new Vector2(step,step);
            foreach(var tri in new[]{new[]{a,c,b},new[]{b,c,d}})
            {
                var center=(tri[0]+tri[1]+tri[2])/3;int type=BankDistance(center)<1.8f?1:0;
                float noise=Mathf.PerlinNoise(center.x*.36f+64,center.y*.36f+64);
                float path=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.25f,2.6f,RoadDistance(center)+.3f*(noise-.5f)));
                float clearing=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3.8f,5.7f,ClearingDistance(center)+.5f*(noise-.5f)));
                var color=Color.Lerp(new Color(.19f,.32f,.105f),new Color(.31f,.43f,.16f),noise);
                color=Color.Lerp(color,new Color(.34f,.32f,.19f),Mathf.Max(path,clearing*.82f));
                if(BankDistance(center)<3.1f)color=Color.Lerp(new Color(.26f,.28f,.16f),color,Mathf.InverseLerp(1.7f,3.1f,BankDistance(center)));
                color*=R(.96f,1.04f);
                foreach(var p in tri){indices[type].Add(vertices[type].Count);vertices[type].Add(new Vector3(p.x,Height(p.x,p.y),p.y));colors[type].Add(color);}
            }
        }
        MeshObject("Forest terrain",vertices[0],indices[0],colors[0],groundMaterial,true);
        var riverbed=MeshObject("Stream bed - Not Walkable",vertices[1],indices[1],colors[1],groundMaterial,true);var modifier=riverbed.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;
        var waterVerts=new List<Vector3>();var waterIndices=new List<int>();
        for(int i=0;i<=160;i++){float x=-40+i*.5f;waterVerts.Add(new Vector3(x,-.12f,River(x)-1.55f));waterVerts.Add(new Vector3(x,-.12f,River(x)+1.55f));if(i<160){int n=i*2;waterIndices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});}}
        MeshObject("Winding forest stream",waterVerts,waterIndices,null,Solid("Stream water",new Color(.13f,.39f,.40f),.55f),false);
    }
    static void Bridge(float x)
    {
        float z=River(x);var group=Group(x<0?"West log bridge":"East log bridge",details);
        for(int i=0;i<7;i++)
        {
            var go=Place("EF_Log_Large1",new Vector2(x+(i-3)*.47f,z),0,1.4f,group);
            go.transform.localScale=new Vector3(1.4f,1.4f,2.4f);go.transform.position+=Vector3.up*(.25f-BoundsOf(go).min.y);
        }
        var floor=Group("Walkable bridge deck",group).gameObject;floor.transform.position=new Vector3(x,.35f,z);floor.AddComponent<BoxCollider>().size=new Vector3(3.45f,.28f,8.8f);
        for(int i=-1;i<=1;i+=2)for(int j=-1;j<=1;j+=2)
        {
            var post=Place("EF_Stump1",new Vector2(x+i*2.05f,z+j*2.1f),R(0,360),.9f,group);post.transform.position+=Vector3.up*(.23f-BoundsOf(post).min.y);
        }
    }
    static void DressForest()
    {
        for(int i=0;i<2200&&treePositions.Count<270;i++)
        {
            var p=new Vector2(R(-38.5f,38.5f),R(-38.5f,38.5f));bool border=Mathf.Max(Mathf.Abs(p.x),Mathf.Abs(p.y))>33;
            if(BankDistance(p)<3.6f||RoadDistance(p)<4.3f||ClearingDistance(p)<7.2f||treePositions.Any(t=>Vector2.Distance(t,p)<2.5f))continue;
            float density=Mathf.PerlinNoise(p.x*.11f+90,p.y*.11f+90);if(!border&&density<.4f)continue;
            string tree=Pick("EF_Tree_Medium_Summer","EF_Tree_Large_Summer","EF_Tree_High_Summer","EF_Pine_Small","EF_Pine_Medium");
            if(p.y<0&&p.y>-28&&R(0,1)<.75f)tree=Pick("EF_Tree_Medium_Summer","EF_Tree_Large_Summer");
            float scale=tree.Contains("Pine")?R(.48f,.67f):R(1.05f,1.65f);
            Place(tree,p,R(0,360),scale,woodland,true);treePositions.Add(p);
        }
        for(int i=0;i<92;i++)
        {
            var p=new Vector2(R(-38,38),R(-38,38));if(BankDistance(p)<2.8f||RoadDistance(p)<3.3f||ClearingDistance(p)<6.4f)continue;
            Place(Pick("EF_Rock_Medium_Moss1","EF_Rock_Large_Moss","EF_Rock_Medium_Moss2","EF_Stump2","EF_Log_Short"),p,R(0,360),R(.65f,1.2f),details,true);
        }
        for(int i=0;i<700;i++)
        {
            var p=new Vector2(R(-39,39),R(-39,39));if(BankDistance(p)<2.2f||RoadDistance(p)<1.9f||ClearingDistance(p)<4.7f)continue;
            string asset=Pick("EF_Grasses_Small","EF_Grass_Medium","EF_Brush_Small_Summer","EF_Brush_Berry_Blue_Summer","EF_Brush_Berry_Red_Summer","EF_Flowers1_White","EF_Flowers1_Blue","EF_Mushroom1_Orange");
            Place(asset,p,R(0,360),R(.65f,1.2f),details);
        }
        for(int i=0;i<88;i++)
        {
            float x=R(-38,38);if(Mathf.Abs(x+18)<3.5f||Mathf.Abs(x-22)<3.5f)continue;
            var p=new Vector2(x,River(x)+(R(0,1)>.5f?1:-1)*R(2,2.9f));
            Place(Pick("EF_Reeds","EF_Rock_Small","EF_Grass_Small"),p,R(0,360),R(.6f,1.1f),details);
        }
        for(int i=0;i<46;i++)
        {
            float angle=i*Mathf.PI*2/46;var p=new Vector2(Mathf.Sin(angle)*38,Mathf.Cos(angle)*38);
            if(p.y<-32&&Mathf.Abs(p.x)<7)continue;if(BankDistance(p)<4)continue;
            Place(Pick("EF_Cliff","EF_Rock_Large_Moss"),p,R(0,360),R(1,1.7f),woodland,true);
        }
    }
    static void Landmarks()
    {
        var camp=Group("01 - Abandoned woodcutters camp",ruins);
        Place("EF_Arc_Moss",new Vector2(0,-32),0,1.1f,camp);
        Place("EF_Sign",new Vector2(3,-30),-25,1,camp);
        Place("EF_Logs_Small",new Vector2(-13,-28),35,1.35f,camp,true);
        Place("EF_Log_Large3",new Vector2(-12,-21),75,1.2f,camp,true);
        Place("EF_Stump3",new Vector2(-4,-22),0,1.3f,camp,true);
        for(int i=0;i<3;i++)Place("EF_Fence_Moss1",new Vector2(-14+i*3,-31),0,1,camp,true);
        Place("EF_Lighter",new Vector2(-2.7f,-32),0,.8f,camp);Place("EF_Lighter",new Vector2(2.7f,-32),180,.8f,camp);
        var grove=Group("02 - Mushroom grove",ruins);
        for(int i=0;i<25;i++){float a=R(0,Mathf.PI*2),r=R(4.8f,6.4f);Place(Pick("EF_Mushroom1_Red","EF_Mushroom2_Orange","EF_Mushroom1_Brown"),clearings[2]+new Vector2(Mathf.Sin(a),Mathf.Cos(a))*r,R(0,360),R(1,2.1f),grove);}
        var shrine=Group("03 - Forgotten well sanctuary",ruins);
        Place("EF_Structure_Well",new Vector2(0,32),0,1.3f,shrine,true);
        Place("EF_Structure_Arc_Old",new Vector2(0,20.5f),0,1.4f,shrine);
        for(int i=0;i<7;i++)
        {
            float angle=Mathf.Lerp(-115,115,i/6f)*Mathf.Deg2Rad;var p=new Vector2(Mathf.Sin(angle)*6.5f,27+Mathf.Cos(angle)*6.5f);
            if(Mathf.Abs(p.x)<2&&p.y>30)continue;
            Place(Pick("EF_Structure_Collum_Old","EF_Structure_Collum_Old_Destroyed"),p,R(0,360),R(1.1f,1.5f),shrine,true);
        }
        var courtyard=Group("04 - Overgrown watchpost courtyard",ruins);
        for(int i=0;i<4;i++)Place("EF_Wall_Moss",new Vector2(17+i*3.7f,23.5f),0,.9f,courtyard,true);
        for(int i=0;i<2;i++)Place("EF_Wall_Destroyed",new Vector2(28,17+i*3),90,1,courtyard,true);
        Place("EF_Arc_Destroyed",new Vector2(19,11.5f),15,1,courtyard);
        Place("EF_Structure_Lighter_Old",new Vector2(26,12),0,1,courtyard,true);
        foreach(var c in new[]{clearings[3],clearings[4]})for(int i=0;i<20;i++)
        {
            var p=c+new Vector2(R(-4,4),R(-4,4));Place(Pick("EF_Structure_Pavement1","EF_Structure_Pavement2","EF_Structure_Pavement3"),p,R(0,360),R(.65f,1),courtyard);
        }
        for(int i=1;i<route.Length;i++)
        {
            var a=route[i-1];var b=route[i];int count=Mathf.FloorToInt(Vector2.Distance(a,b)/3.8f);
            for(int j=0;j<count;j++){var p=Vector2.Lerp(a,b,(j+.5f)/count);if(BankDistance(p)<3)continue;if(R(0,1)<.55f)Place("EF_Path_Rocks_Random1",p,R(0,360),R(.5f,.8f),details);}
        }
    }
    static Transform Mark(string name,Vector2 p){var t=Group(name,markers);t.position=new Vector3(p.x,Height(p.x,p.y)+.05f,p.y);return t;}
    static Camera CameraAt(string name,Vector3 target,float size,bool active)
    {
        var go=Group(name).gameObject;var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=size;camera.nearClipPlane=.3f;camera.farClipPlane=300;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.17f,.16f);camera.transform.rotation=Quaternion.Euler(60,0,0);camera.transform.position=target-camera.transform.forward*95;camera.enabled=active;return camera;
    }
    static void Render(Camera camera,string filename,int resolution)
    {
        var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(resolution,resolution,24);Texture2D image=null;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image=new Texture2D(resolution,resolution,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,resolution,resolution),0,0);image.Apply();File.WriteAllBytes(Work+"/"+filename,image.EncodeToPNG());}
        finally{camera.targetTexture=old;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Resume()
    {
        try
        {
            for(int i=0;i<SceneManager.sceneCount;i++){var s=SceneManager.GetSceneAt(i);if(s.GetRootGameObjects().Any(g=>g.name=="06 Navigation")){map=s;break;}}
            if(!map.IsValid())throw new Exception("No generated scene to inspect");
            SceneManager.SetActiveScene(map);
            var demo=SceneManager.GetSceneByPath(Pack+"/Scenes/Demo.unity");if(demo.IsValid()&&!demo.isDirty)EditorSceneManager.CloseScene(demo,true);
            var roots=map.GetRootGameObjects();markers=roots.Single(g=>g.name=="05 Gameplay placement markers").transform;
            var spawn=markers.Find("PlayerSpawn - southern gate");NavMesh.SamplePosition(spawn.position,out var start,2,NavMesh.AllAreas);
            var checks=new List<object>();
            foreach(Transform t in markers)
            {
                if(!t.name.StartsWith("Encounter")&&!t.name.StartsWith("Boss"))continue;
                bool sampled=NavMesh.SamplePosition(t.position,out var end,2,NavMesh.AllAreas);var path=new NavMeshPath();bool found=sampled&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path);
                checks.Add(new{name=t.name,sampled,status=path.status.ToString(),found,corners=path.corners.Select(v=>v.ToString()).ToArray()});
            }
            var nav=roots.Single(g=>g.name=="06 Navigation").GetComponent<NavMeshSurface>();
            File.WriteAllText(Work+"/routes.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks,settings=nav.GetBuildSettings(),decks=roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Where(c=>c.name=="Walkable bridge deck").Select(c=>new{pos=c.transform.position.ToString(),bounds=c.bounds.ToString()})},Newtonsoft.Json.Formatting.Indented));
            var overview=roots.Single(g=>g.name.StartsWith("Overview Camera")).GetComponent<Camera>();
            bool old=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            try{Render(overview,"overview.png",1536);Render(roots.Single(g=>g.name=="Main Camera - TopDown").GetComponent<Camera>(),"entrance.png",1024);}
            finally{ShaderUtil.allowAsyncCompilation=old;}
            EditorSceneManager.SaveScene(map,Out+"/Evergreen_Hollow.unity");
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,0),Quaternion.Euler(60,0,0),49);
            File.Delete(Work+"/resume.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/resume-error.txt",e.ToString());Debug.LogException(e);}
    }
    static void Finish()
    {
        bool asyncShaders=ShaderUtil.allowAsyncCompilation;
        try
        {
            map=SceneManager.GetSceneByPath(Out+"/Evergreen_Hollow.unity");
            if(!map.IsValid()||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Expected generated scene in Edit Mode");
            SceneManager.SetActiveScene(map);
            var roots=map.GetRootGameObjects();var nav=roots.Single(g=>g.name=="06 Navigation").GetComponent<NavMeshSurface>();
            Undo.RegisterFullObjectHierarchyUndo(nav.gameObject,"Extend forest bridge landings");
            foreach(var bridge in nav.GetComponentsInChildren<Transform>().Where(t=>t.name=="West log bridge"||t.name=="East log bridge"))
            {
                foreach(Transform t in bridge)
                {
                    if(t.name=="Walkable bridge deck")t.GetComponent<BoxCollider>().size=new Vector3(3.45f,.28f,8.8f);
                    else if(t.name=="EF_Log_Large1")t.localScale=new Vector3(1.4f,1.4f,2.4f);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                }
            }
            var previous=nav.navMeshData;nav.BuildNavMesh();var baked=nav.navMeshData;
            if(baked==null)throw new Exception("No NavMesh data");
            if(previous!=null){nav.RemoveData();EditorUtility.CopySerialized(baked,previous);nav.navMeshData=previous;nav.AddData();UnityEngine.Object.DestroyImmediate(baked);EditorUtility.SetDirty(previous);AssetDatabase.SaveAssetIfDirty(previous);}
            markers=roots.Single(g=>g.name=="05 Gameplay placement markers").transform;
            NavMesh.SamplePosition(markers.Find("PlayerSpawn - southern gate").position,out var start,2,NavMesh.AllAreas);
            var checks=new List<object>();bool valid=true;
            foreach(Transform t in markers)
            {
                if(!t.name.StartsWith("Encounter")&&!t.name.StartsWith("Boss"))continue;
                bool sampled=NavMesh.SamplePosition(t.position,out var end,2,NavMesh.AllAreas);var path=new NavMeshPath();bool reachable=sampled&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                checks.Add(new{name=t.name,sampled,reachable,status=path.status.ToString(),corners=path.corners.Select(c=>c.ToString()).ToArray()});valid&=reachable;
            }
            var tri=NavMesh.CalculateTriangulation();
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result=valid?"PASS":"FAIL",scene=map.path,size="80 x 80",prefabInstances=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)),navmeshTriangles=tri.indices.Length/3,encounters=checks},Newtonsoft.Json.Formatting.Indented));
            ShaderUtil.allowAsyncCompilation=false;
            var overview=roots.Single(g=>g.name.StartsWith("Overview Camera")).GetComponent<Camera>();Render(overview,"overview.png",1536);
            Render(roots.Single(g=>g.name=="Main Camera - TopDown").GetComponent<Camera>(),"entrance.png",1024);
            var detail=CameraAt("Temporary sanctuary preview",new Vector3(0,2,26),13,false);Render(detail,"sanctuary.png",1024);UnityEngine.Object.DestroyImmediate(detail.gameObject);
            EditorSceneManager.MarkSceneDirty(map);EditorSceneManager.SaveScene(map);
            File.Delete(Work+"/finish.request");
            if(!valid)throw new Exception("Some encounters remain unreachable; see result.json");
        }
        catch(Exception e){File.WriteAllText(Work+"/finish-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=asyncShaders;}
    }
    [MenuItem("Tools/Diagnostics/Build Evergreen RPG Scene")]
    public static void Build()
    {
        var original=SceneManager.GetActiveScene();bool success=false;bool asyncShaders=ShaderUtil.allowAsyncCompilation;
        try
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||original.path!=Pack+"/Scenes/Demo.unity")throw new Exception("Expected Evergreen Demo in Edit Mode");
            if(File.Exists(Out+"/Evergreen_Hollow.unity"))throw new Exception("Scene already exists; refusing overwrite");
            if(GraphicsSettings.currentRenderPipeline!=null)throw new Exception("Expected Built-in rendering");
            File.Delete(Work+"/build.request");random=new System.Random(9137);prefabs.Clear();treePositions.Clear();
            map=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(map);
            landscape=Group("01 Terrain and stream");woodland=Group("02 Forest canopy and boundary");details=Group("03 Undergrowth and bridges");ruins=Group("04 Landmarks");markers=Group("05 Gameplay placement markers");
            Ground();Bridge(-18);Bridge(22);DressForest();Landmarks();
            var spawn=Mark("PlayerSpawn - southern gate",new Vector2(0,-34));
            var targets=new List<Transform>{Mark("Encounter_01 - camp",clearings[0]),Mark("Encounter_02 - wolves clearing",clearings[1]),Mark("Encounter_03 - mushroom grove",clearings[2]),Mark("BossSpawn - forgotten sanctuary",clearings[3]),Mark("Encounter_04 - ruined courtyard",clearings[4]),Mark("Encounter_05 - eastern grove",clearings[5])};
            foreach(var c in clearings){Mark("Arena edge - north",c+Vector2.up*4);Mark("Arena edge - south",c-Vector2.up*4);}
            var light=Group("Sun - late afternoon").gameObject.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.91f,.72f);light.intensity=1.15f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(49,-35,0);
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.57f,.64f);RenderSettings.ambientEquatorColor=new Color(.32f,.41f,.30f);RenderSettings.ambientGroundColor=new Color(.18f,.22f,.14f);RenderSettings.fog=false;
            var gameCamera=CameraAt("Main Camera - TopDown",spawn.position+Vector3.forward*6,13,true);gameCamera.tag="MainCamera";gameCamera.gameObject.AddComponent<AudioListener>();
            var overview=CameraAt("Overview Camera - enable for whole map",new Vector3(0,1,0),44,false);
            var nav=Group("06 Navigation").gameObject.AddComponent<NavMeshSurface>();landscape.SetParent(nav.transform,true);woodland.SetParent(nav.transform,true);details.SetParent(nav.transform,true);ruins.SetParent(nav.transform,true);
            nav.collectObjects=CollectObjects.Children;nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;nav.BuildNavMesh();
            if(nav.navMeshData==null)throw new Exception("NavMesh bake returned null");
            AssetDatabase.CreateAsset(nav.navMeshData,Out+"/EvergreenNavMesh.asset");
            var triangulation=NavMesh.CalculateTriangulation();if(triangulation.indices.Length==0)throw new Exception("NavMesh is empty");
            if(!NavMesh.SamplePosition(spawn.position,out var start,2,NavMesh.AllAreas))throw new Exception("Spawn outside NavMesh");
            var checks=new List<object>();
            foreach(var t in targets)
            {
                bool sampled=NavMesh.SamplePosition(t.position,out var destination,2,NavMesh.AllAreas);var path=new NavMeshPath();bool found=sampled&&NavMesh.CalculatePath(start.position,destination.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete;
                checks.Add(new{name=t.name,sampled,reachable=found});if(!found)throw new Exception("Unreachable encounter: "+t.name);
            }
            if(!EditorSceneManager.SaveScene(map,Out+"/Evergreen_Hollow.unity"))throw new Exception("Scene save failed");
            ShaderUtil.allowAsyncCompilation=false;
            Render(overview,"overview.png",1536);Render(gameCamera,"entrance.png",1024);
            var detail=CameraAt("Temporary preview",new Vector3(0,2,26),13,false);Render(detail,"sanctuary.png",1024);UnityEngine.Object.DestroyImmediate(detail.gameObject);
            EditorSceneManager.SaveScene(map);
            File.WriteAllText(Work+"/result.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{result="PASS",scene=map.path,size="80 x 80",trees=treePositions.Count,prefabInstances=map.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Count(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)),navmeshTriangles=triangulation.indices.Length/3,encounters=checks,sourceUnchanged=!original.isDirty},Newtonsoft.Json.Formatting.Indented));
            success=true;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,0),Quaternion.Euler(60,0,0),49);
        }
        catch(Exception e){File.WriteAllText(Work+"/build-error.txt",e.ToString());Debug.LogException(e);}
        finally{ShaderUtil.allowAsyncCompilation=asyncShaders;if(success&&!original.isDirty)EditorSceneManager.CloseScene(original,true);else if(!success&&original.IsValid())SceneManager.SetActiveScene(original);}
    }
}
#endif
