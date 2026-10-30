#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
// Preview exercises the actual built-in renderer as well as the controller.
public static class PartyCampValidation
{
    [InitializeOnLoadMethod]static void Init(){if(File.Exists(PartyCampInspector.Work+"/validate.request")&&!File.Exists(PartyCampInspector.Work+"/build.request"))EditorApplication.delayCall+=Run;}
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    static RObj Hero(string id,int index)
    {
        var result=new RObj{RID="camp_probe_"+index,dbID=id,dbObj=new Obj{ID=id},it=ItemType.monster};
        result.dbObj.pars["level"]=1;result.dbObj.pars["attack"]=12+index;result.dbObj.pars["health"]=100;
        result.upgradePars["level"]=index%5;result.upgradePars["amount"]=1;result.upgradePars["used_slot"]=-1;result.upgradePars["registered_damage"]=0;
        result.RecalcPars();return result;
    }
    [MenuItem("Tools/UI/Party Camp/Validate and Preview")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var previousState=MainStates.instance;var previousResources=ResourceHolder.instance;var previousParty=PartyController.instance;
        var previousFillers=UIfiller.instances;var previousRefresh=UIfiller.acts;float initialScale=Time.timeScale;
        Scene preview=default;var sourceScene=SceneManager.GetActiveScene();GameObject root=null;PartyCampController controller=null;var fixtures=new List<GameObject>();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PartyCampBuilder.PrefabPath);
            if(prefab==null)return; // The build callback may run after this callback during the same reload.
            ResourceHolder.instance=UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            Check(ResourceHolder.instance!=null,"ResourceHolder missing");
            var ids=ResourceHolder.instance.avas.Where(v=>v.Value!=null&&ResourceHolder.instance.monsters.ContainsKey(v.Key)&&PubRecruitment.IsEligibleId(v.Key)).Select(v=>v.Key).Take(20).ToArray();Check(ids.Length>=6,"Hero visuals missing");
            preview=EditorSceneManager.NewPreviewScene();
            root=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(root,sourceScene);
            controller=root.GetComponent<PartyCampController>();
            var fixture=new GameObject("Isolated test state");SceneManager.MoveGameObjectToScene(fixture,sourceScene);fixtures.Add(fixture);var state=fixture.AddComponent<MainStates>();state.useSave=false;
            MainStates.instance=state;PartyController.instance=null;UIfiller.instances=new List<UIfiller>();UIfiller.acts=null;
            var player=new RObj{RID="main_player"};state.all["main_player"]=player;
            for(int i=0;i<20;i++)player.inventory.Add(Hero(ids[i%ids.Length],i));
            for(int i=0;i<6;i++)state.EquipSkills(player,player.inventory[i]);
            var main=new GameObject("Test main camera",typeof(Camera));SceneManager.MoveGameObjectToScene(main,sourceScene);fixtures.Add(main);var mainCamera=main.GetComponent<Camera>();mainCamera.enabled=true;
            var dummy=new GameObject("Test gameplay canvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));SceneManager.MoveGameObjectToScene(dummy,sourceScene);fixtures.Add(dummy);var dummyCanvas=dummy.GetComponent<Canvas>();
            controller.Show();Check(controller.IsOpen&&!mainCamera.enabled&&controller.CampCamera.enabled&&!dummyCanvas.enabled&&Time.timeScale==0,"Camp transition flags: open="+controller.IsOpen+", main="+mainCamera.enabled+", camp="+controller.CampCamera.enabled+", HUD="+dummyCanvas.enabled+", time="+Time.timeScale);
            var populated = root.GetComponentsInChildren<PartyCampSlot>(true).Where(s => s.MonsterInstance != null).ToArray();
            Check(populated.Length == 6, "Expected six actual monster prefab instances");
            foreach (var slot in populated)
            {
                var hero = (RObj)typeof(PartyCampSlot).GetField("hero", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(slot);
                var source = ResourceHolder.instance.monsters[hero.dbObj.ID];
                var instance = slot.MonsterInstance;
                Check(instance != source && instance.GetComponentsInChildren<Transform>(true).Length == source.GetComponentsInChildren<Transform>(true).Length, "Monster prefab hierarchy was not preserved");
                Check(instance.GetComponentsInChildren<MonoBehaviour>(true).All(c => c == null || !c.enabled), "Camp gameplay script enabled");
                Check(instance.GetComponentsInChildren<Collider2D>(true).All(c => !c.enabled) && instance.GetComponentsInChildren<Rigidbody2D>(true).All(b => !b.simulated), "Camp physics enabled");
                Check(instance.GetComponentsInChildren<Animator>(true).All(a => a.updateMode == AnimatorUpdateMode.UnscaledTime && !a.fireEvents && !a.applyRootMotion), "Camp animation configuration incorrect");
            }
            var sample=player.inventory[0];controller.ToggleHero(sample);Check(sample.GetPar("used_slot")==-1,"Unequip failed");controller.ToggleHero(sample);Check(sample.GetPar("used_slot")>=state.petsLoIndex,"Equip failed");
            SceneManager.MoveGameObjectToScene(root,preview);
            Render(root,controller,preview);
            SceneManager.MoveGameObjectToScene(root,sourceScene);
            controller.AutoAssign();
            int capacity=state.petsHiIndex-state.petsLoIndex;var equipped=player.inventory.Where(h=>PartyCampRoster.IsAssigned(h,state.petsLoIndex,state.petsHiIndex)).ToArray();
            Check(equipped.Length==capacity&&equipped.Select(h=>h.GetPar("used_slot")).Distinct().Count()==capacity,"Auto-assign capacity or unique slots failed");
            var extra=player.inventory.First(h=>h.GetPar("used_slot")<0);controller.ToggleHero(extra);Check(extra.GetPar("used_slot")==-1,"Full party overflow");
            controller.ClearParty();Check(player.inventory.All(h=>h.GetPar("used_slot")==-1),"Clear failed");
            controller.Hide();Check(!controller.IsOpen&&mainCamera.enabled&&!controller.CampCamera.enabled&&dummyCanvas.enabled&&Time.timeScale==initialScale,"Exit failed to restore state");
            controller.Show();typeof(PartyCampController).GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(controller,null);Check(mainCamera.enabled&&dummyCanvas.enabled&&Time.timeScale==initialScale,"Disable failed to restore state");
            File.WriteAllText(PartyCampInspector.Work+"/validation.txt","PASS: full ResourceHolder monster prefab hierarchy, isolated scripts/physics, unscaled animation configuration, native isolated RObj equipment, enter/exit camera and HUD restoration, timeScale restoration, add/remove, auto-assign native capacity, unique slots, full-party rejection, clear, disable cleanup. Existing player/save untouched. Camp preview rendered.");File.Delete(PartyCampInspector.Work+"/validate.request");
        }
        catch(Exception e){File.WriteAllText(PartyCampInspector.Work+"/validation.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(controller!=null&&controller.IsOpen)controller.Hide();
            MainStates.instance=previousState;ResourceHolder.instance=previousResources;PartyController.instance=previousParty;UIfiller.instances=previousFillers;UIfiller.acts=previousRefresh;Time.timeScale=initialScale;
            foreach(var fixture in fixtures)if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
        }
    }
    static void Render(GameObject root,PartyCampController controller,Scene preview)
    {
        var camera=controller.CampCamera;var canvas=root.GetComponentInChildren<Canvas>(true);var rect=(RectTransform)canvas.transform;
        var previous=RenderTexture.active;RenderTexture target=null;Texture2D texture=null;
        try
        {
            camera.scene=preview;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=(1<<2)|(1<<5);
            canvas.renderMode=RenderMode.WorldSpace;canvas.GetComponent<CanvasScaler>().enabled=false;
            rect.sizeDelta=new Vector2(1080,2400);rect.localScale=Vector3.one*(21f/2400);
            rect.position=camera.transform.position+new Vector3(0,0,15);rect.rotation=Quaternion.identity;
            var safe=(RectTransform)rect.Find("SafeArea");safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;safe.Find("Design").localScale=Vector3.one;
            target=new RenderTexture(720,1600,24);camera.targetTexture=target;camera.aspect=.45f;
            var overlay=(RectTransform)rect.Find("Slot Buttons");foreach(var slot in root.GetComponentsInChildren<PartyCampSlot>(true))slot.Project(camera,overlay);
            Canvas.ForceUpdateCanvases();
            var planes=GeometryUtility.CalculateFrustumPlanes(camera);
            var renderers=root.GetComponentsInChildren<Renderer>(true);
            File.WriteAllText(PartyCampInspector.Work+"/render-debug.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{
                camera=camera.transform.position.ToString(),mask=camera.cullingMask,sceneMask=camera.overrideSceneCullingMask,
                renderers=renderers.Take(18).Select(r=>new{r.name,r.enabled,active=r.gameObject.activeInHierarchy,layer=r.gameObject.layer,bounds=r.bounds.ToString(),visible=GeometryUtility.TestPlanesAABB(planes,r.bounds),shader=r.sharedMaterial!=null?r.sharedMaterial.shader.name:null}).ToArray()
            },Newtonsoft.Json.Formatting.Indented));
            camera.Render();RenderTexture.active=target;
            texture=new Texture2D(720,1600,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,720,1600),0,0);texture.Apply();
            File.WriteAllBytes(PartyCampInspector.Work+"/camp-preview.png",texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=previous;
            if(target!=null){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
#endif
