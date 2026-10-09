using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Animpic.CharacterStudio.Editor
{
    public static class CharacterStudioSetup
    {
        public const string ScenePath = "Assets/Animpic Studio/POLY-FantasyCharacter/Scenes/CharacterStudio.unity";
        private const string Root = "Assets/Animpic Studio/POLY-FantasyCharacter/Runtime/Studio";
        public static void OpenStudio()
        {
            if (!File.Exists(ScenePath)) { BuildStudio(); return; }
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                PreserveOpenScenes(); EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            var character = Object.FindObjectsOfType<CharacterCustomizer>().FirstOrDefault();
            if (character) Selection.activeGameObject = character.gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        [MenuItem("Tools/Animpic Studio/Characters/Fantasy Character/Rebuild Studio", false, 300)]
        public static void BuildStudio()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build the studio outside Play mode.");
            StudioCatalogueBuilder.Build(false); StudioCatalogueBuilder.Build(true);
            PreserveOpenScenes();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.24f, .26f, .30f);
            RenderSettings.ambientEquatorColor = new Color(.14f, .15f, .18f);
            RenderSettings.ambientGroundColor = new Color(.09f, .08f, .10f);
            RenderSettings.ambientIntensity = 1; RenderSettings.fog = false; RenderSettings.reflectionIntensity = .35f;
            var stage = new GameObject("STUDIO - environment").transform;
            var backdrop = Material("Studio graphite", new Color(.065f, .073f, .095f), .08f, .3f);
            var plinth = Material("Podium - dark stone", new Color(.105f, .11f, .13f), .15f, .33f);
            var top = Material("Podium - soft slate", new Color(.21f, .225f, .25f), .14f, .26f);
            var brass = Material("Champagne metal", new Color(.49f, .36f, .18f), .7f, .65f);
            var glow = Material("Warm diffuser", new Color(.89f, .79f, .60f), 0, .2f, new Color(1.15f, .85f, .47f));
            var coolGlow = Material("Cool diffuser", new Color(.55f, .64f, .76f), 0, .2f, new Color(.55f, .75f, 1.08f));
            MeshObject("Infinite cyclorama", Cyclorama(), backdrop, stage);
            Primitive("Podium base", PrimitiveType.Cylinder, new Vector3(0,-.078f,0), new Vector3(2.18f,.065f,2.18f), plinth, stage);
            Primitive("Podium brass seam", PrimitiveType.Cylinder, new Vector3(0,-.014f,0), new Vector3(2.19f,.006f,2.19f), brass, stage);
            Primitive("Podium surface", PrimitiveType.Cylinder, new Vector3(0,-.006f,0), new Vector3(2.13f,.006f,2.13f), top, stage);
            MeshObject("Fine illuminated ring", Ring(1.071f,1.083f,.001f), glow, stage);
            // Narrow illuminated panels create a studio silhouette without obscuring the model.
            Softbox("Warm softbox", new Vector3(-2.25f,1.15f,-1.7f), Quaternion.Euler(0,-12,0), plinth,glow,stage);
            Softbox("Cool softbox", new Vector3(2.25f,1.3f,-1.85f), Quaternion.Euler(0,12,0), plinth,coolGlow,stage);
            Light("Key - warm", new Vector3(-3,4,4), new Vector3(0,1,0), 1.2f, new Color(1,.89f,.76f), LightType.Directional, true, stage);
            Light("Fill - cool", new Vector3(3,2.7f,3), new Vector3(0,1,0), .52f, new Color(.73f,.85f,1), LightType.Directional, false, stage);
            Light("Edge light", new Vector3(-1,3.2f,-2), new Vector3(0,1,0), 1.18f, new Color(.91f,.91f,1), LightType.Directional, false, stage);
            Light("Backdrop pool", new Vector3(.2f,2.1f,-.6f), new Vector3(0,1.1f,-2.8f), 2.2f, new Color(.46f,.58f,.78f), LightType.Spot, false, stage);
            var focus = new GameObject("CHARACTERS - stage origin").transform;
            var female = Instance(StudioCatalogueBuilder.FemalePrefab, focus);
            var male = Instance(StudioCatalogueBuilder.MalePrefab, focus);
            Pose(female.gameObject); Pose(male.gameObject);
            var backgroundCamera = new GameObject("Background clear").AddComponent<Camera>();
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor; backgroundCamera.backgroundColor = new Color(.072f,.07f,.09f);
            backgroundCamera.cullingMask = 0; backgroundCamera.depth = -10;
            var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.gameObject.AddComponent<AudioListener>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.08f,.10f);
            camera.nearClipPlane = .05f; camera.farClipPlane = 70; camera.fieldOfView = 38; camera.allowHDR = true; camera.allowMSAA = true;
            var app = new GameObject("CHARACTER STUDIO - runtime interface");
            var ui = app.AddComponent<CharacterStudioUI>(); ui.startWithMale = true;
            ui.Configure(female,male,camera,focus); ui.BuildUI(); ui.SetCharacter(true);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save CharacterStudio.unity.");
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = male.gameObject;
            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,1,0), Quaternion.Euler(8,205,0),3.2f);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("Character Studio ready: " + ScenePath);
        }
        private static void PreserveOpenScenes()
        {
            string backupDirectory = Path.GetFullPath("Library/CharacterStudioSceneBackups"); Directory.CreateDirectory(backupDirectory);
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i); if (!scene.isLoaded || !scene.isDirty) continue;
                string backup = backupDirectory.Replace('\\','/') + "/" + (string.IsNullOrEmpty(scene.name) ? "Untitled" : scene.name) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".unity";
                if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not preserve the open scene: " + backup);
                if (!string.IsNullOrEmpty(scene.path) && !EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the open scene: " + scene.path);
            }
        }
        private static CharacterCustomizer Instance(string path, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)); go.transform.SetParent(parent,false);
            go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
            foreach (var renderer in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) { renderer.updateWhenOffscreen = true; renderer.localBounds = new Bounds(Vector3.zero,Vector3.one*4); }
            return go.GetComponent<CharacterCustomizer>();
        }
        public static void Pose(GameObject root)
        {
            var bones = root.GetComponentsInChildren<Transform>(true).GroupBy(t=>t.name).ToDictionary(g=>g.Key,g=>g.First());
            foreach (string side in new[]{"L","R"})
            {
                Transform upper,lower,hand;
                if (!bones.TryGetValue("CC_Base_"+side+"_Upperarm",out upper) || !bones.TryGetValue("CC_Base_"+side+"_Forearm",out lower) || !bones.TryGetValue("CC_Base_"+side+"_Hand",out hand)) continue;
                float sign = Mathf.Sign(upper.position.x-root.transform.position.x);
                Vector3 direction = root.transform.TransformDirection(new Vector3(sign*.28f,-.96f,.035f)).normalized;
                upper.rotation = Quaternion.FromToRotation(lower.position-upper.position,direction) * upper.rotation;
                direction = root.transform.TransformDirection(new Vector3(sign*.13f,-.97f,.2f)).normalized;
                lower.rotation = Quaternion.FromToRotation(hand.position-lower.position,direction) * lower.rotation;
            }
        }
        public static AnimationClip CreateIdle(GameObject root,bool male)
        {
            Pose(root);
            var clip = new AnimationClip { name = male ? "Male studio idle" : "Female studio idle", frameRate = 30 };
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip,settings);
            foreach (var bone in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("Upperarm") || t.name.EndsWith("_Forearm") || t.name.EndsWith("Spine02")))
            {
                string path = AnimationUtility.CalculateTransformPath(bone,root.transform);
                var rotation = bone.localRotation;
                for(int k=0;k<4;k++) AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+"xyzw"[k]),AnimationCurve.Constant(0,4,rotation[k]));
            }
            clip.EnsureQuaternionContinuity();
            string assetPath = "Assets/Animpic Studio/POLY-FantasyCharacter/Animations/Studio/"+(male?"Male":"Female")+"StudioIdle.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
            if(existing) { EditorUtility.CopySerialized(clip,existing); Object.DestroyImmediate(clip); EditorUtility.SetDirty(existing); AssetDatabase.SaveAssetIfDirty(existing); return existing; }
            AssetDatabase.CreateAsset(clip,assetPath); return clip;
        }
        private static Material Material(string name,Color color,float metal,float gloss,Color? emission=null)
        {
            string path = "Assets/Animpic Studio/POLY-FantasyCharacter/Materials/Studio/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) {m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
            m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",gloss);
            if(emission.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);}
            EditorUtility.SetDirty(m);return m;
        }
        private static GameObject Primitive(string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        private static void Softbox(string name,Vector3 position,Quaternion rotation,Material frame,Material face,Transform parent)
        {
            var group=new GameObject(name).transform;group.SetParent(parent,false);group.localPosition=position;group.localRotation=rotation;
            Primitive("Frame",PrimitiveType.Cube,Vector3.zero,new Vector3(.10f,1.8f,.07f),frame,group);
            Primitive("Diffuser",PrimitiveType.Cube,new Vector3(0,0,.04f),new Vector3(.055f,1.7f,.008f),face,group);
            float floor = -.145f - position.y;
            Primitive("Light stand",PrimitiveType.Cylinder,new Vector3(0,(floor-.9f)*.5f,0),new Vector3(.023f,(-.9f-floor)*.5f,.023f),frame,group);
            Primitive("Weighted base",PrimitiveType.Cylinder,new Vector3(0,floor+.015f,0),new Vector3(.3f,.015f,.3f),frame,group);
        }
        private static void Light(string name,Vector3 position,Vector3 target,float intensity,Color color,LightType type,bool shadow,Transform parent)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;go.transform.LookAt(target);var light=go.AddComponent<Light>();
            light.type=type;light.intensity=intensity;light.color=color;light.range=10;light.spotAngle=80;
            light.shadows=shadow?LightShadows.Soft:LightShadows.None;light.shadowStrength=.65f;light.shadowBias=.025f;light.shadowNormalBias=.1f;light.shadowCustomResolution=2048;
        }
        private static void MeshObject(string name,Mesh mesh,Material material,Transform parent)
        {
            StudioCatalogueBuilder.Ensure("Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/Studio");string path="Assets/Animpic Studio/POLY-FantasyCharacter/Meshes/Studio/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){StudioSurround.CopyGeometry(mesh,old);Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<Renderer>().sharedMaterial=material; if(name=="Infinite cyclorama") go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        }
        private static Mesh Cyclorama() { return StudioSurround.CreateMesh(); }
        private static Mesh Ring(float inner,float outer,float height)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=192;i++){float a=i*Mathf.PI*2/192;vertices.Add(new Vector3(Mathf.Cos(a)*inner,height,Mathf.Sin(a)*inner));vertices.Add(new Vector3(Mathf.Cos(a)*outer,height,Mathf.Sin(a)*outer));}
            for(int i=0;i<192;i++){int a=i*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            var mesh=new Mesh{name="Podium luminous inlay"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        public static void RenderExamples()
        {
            var ui=Object.FindObjectOfType<CharacterStudioUI>();if(!ui)throw new InvalidOperationException("Open CharacterStudio scene first.");
            ui.Configure(ui.female,ui.male,ui.viewCamera,ui.stageRoot); ui.BuildUI();
            string directory=Path.GetFullPath("Library/CharacterStudioValidation");Directory.CreateDirectory(directory);
            foreach(bool male in new[]{true,false}){ui.SetCharacter(male);Canvas.ForceUpdateCanvases();Render(ui,Path.Combine(directory,male?"studio-male.png":"studio-female.png"));}
            ui.SetCharacter(true);EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
        public static void Render(CharacterStudioUI ui,string path)
        {
            var camera=ui.viewCamera;var canvas=ui.uiCanvas;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};target.Create();
            var originalTarget=camera.targetTexture;var originalActive=RenderTexture.active;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
            var uiGo=new GameObject("Temporary UI capture camera");var uiCamera=uiGo.AddComponent<Camera>();uiCamera.enabled=false;uiCamera.clearFlags=CameraClearFlags.Depth;uiCamera.cullingMask=1<<5;uiCamera.targetTexture=target;
            uiCamera.transform.position=new Vector3(0,0,-100);uiCamera.nearClipPlane=.1f;uiCamera.farClipPlane=5;uiCamera.orthographic=true;
            Texture2D texture=null;
            try
            {
                foreach(var t in canvas.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();foreach(var scroll in canvas.GetComponentsInChildren<UnityEngine.UI.ScrollRect>(true)){scroll.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);if(scroll.verticalScrollbar && scroll.verticalScrollbarVisibility!=UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent)scroll.verticalScrollbar.gameObject.SetActive(scroll.content.rect.height>scroll.viewport.rect.height+.1f);}camera.targetTexture=target;RenderTexture.active=target;GL.Clear(true,true,new Color(.072f,.07f,.09f));camera.Render();uiCamera.Render();
                RenderTexture.active=target;texture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;camera.targetTexture=originalTarget;RenderTexture.active=originalActive;Object.DestroyImmediate(uiGo);if(texture)Object.DestroyImmediate(texture);target.Release();Object.DestroyImmediate(target);Canvas.ForceUpdateCanvases();
            }
        }
    }
}
