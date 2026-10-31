#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PubPlayCheck
{
    static string Flag => PubInspector.Work + "/play.request";
    static bool running;
    static PubPlayCheck()
    {
        EditorApplication.playModeStateChanged += Changed;
        if (File.Exists(Flag)) EditorApplication.update += StartWhenReady;
        if (SessionState.GetBool("PubCheck.active",false)) EditorApplication.update += Pulse;
    }
    static void Pulse() { if(EditorApplication.isPlaying)EditorApplication.QueuePlayerLoopUpdate(); }
    static void StartWhenReady()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||File.Exists(PubInspector.Work+"/build.request"))return;
        if(!File.Exists(PubInspector.Work+"/build.txt")||!File.ReadAllText(PubInspector.Work+"/build.txt").Contains("PASS"))return;
        File.Delete(Flag);EditorApplication.update-=StartWhenReady;
        SessionState.SetBool("PubCheck.active",true);SessionState.SetBool("PubCheck.wasPlaying",EditorApplication.isPlaying);
        File.WriteAllText(PubInspector.Work+"/play.txt","STARTING");
        if(EditorApplication.isPlaying)Run();else EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("PubCheck.active",false))Run();
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static async void Run()
    {
        if(running)return;running=true;
        GameObject testCanvas=null;MainStates state=null;RObj originalPlayer=null;PlayerData originalData=null;
        bool originalUseSave=false;HashSet<string> registrations=null;
        try
        {
            var limit=DateTime.UtcNow.AddSeconds(75);
            while(DateTime.UtcNow<limit)
            {
                if(PubRecruitment.Ready(MainStates.instance)&&ConfigLoader.parseEnded)break;
                await Task.Delay(100);EditorApplication.QueuePlayerLoopUpdate();
            }
            state=MainStates.instance;Require(PubRecruitment.Ready(state)&&ConfigLoader.parseEnded,"Main player/config timeout");
            var model=new PubRecruitment(427);model.BuildPool(ConfigLoader.Instance.heroes,DatabaseAll.instance.heroes);
            Require(model.Pool.Count>=3,"Fewer than three eligible Heroes");
            Require(model.Pool.All(h=>PubRecruitment.IsEligibleId(h.Id)),"Excluded prefix in pool");
            int excluded=ConfigLoader.Instance.heroes.Count(h=>!PubRecruitment.IsEligibleId(h.monsterName));
            originalPlayer=state.mainPlayer;originalData=state.playerData;originalUseSave=state.useSave;
            registrations=new HashSet<string>(state.all.Keys,StringComparer.Ordinal);
            // Synchronous fixture: normal game updates never observe the temporary player.
            var player=new RObj{RID=originalPlayer.RID,dbObj=originalPlayer.dbObj};
            state.all["main_player"]=player;state.playerData=new PlayerData();state.playerData.mainPlayer=player;state.useSave=false;
            var gold=DatabaseAll.instance.CreateItem("gold",1000);gold.owner=player;gold.index=0;player.inventory.Add(gold);
            Require(model.RestoreOrRefresh(state.playerData,DateTime.UtcNow,30),"Offers unavailable");
            Require(model.Offers.Select(h=>h.Id).Distinct().Count()==3,"Duplicate offers");
            Require(model.TryRecruit(state,3,"gold",100,20,out var rewards,out int bonus,out string error),"Native hero creation: "+error);
            Require(PubRecruitment.Currency(player,"gold")==700&&rewards.Count==1&&rewards[0].RewardMultiplier==3,"Wrong 3x price/rewards");
            Require(player.inventory.Count(i=>i.it==ItemType.monster&&i.owner==player)==rewards.Select(h=>h.Id).Distinct().Count(),"Heroes not owned by player");
            Require(player.inventory.Where(i=>i.it==ItemType.monster).All(i=>i.main==null&&i.GetPar("used_slot")==-1&&i.index>=0),"Hero unexpectedly equipped/spawned");
            var oldOffers=model.Offers.Select(h=>h.Id).ToArray();
            Require(model.TryPaidRefresh(state,"gold",25,30,DateTime.UtcNow,out error),"Paid refresh: "+error);
            Require(PubRecruitment.Currency(player,"gold")==675,"Refresh price");
            var restored=new PubRecruitment(99);restored.BuildPool(ConfigLoader.Instance.heroes,DatabaseAll.instance.heroes);
            restored.RestoreOrRefresh(state.playerData,DateTime.UtcNow,30);
            Require(restored.Offers.Select(h=>h.Id).SequenceEqual(model.Offers.Select(h=>h.Id)),"Lineup changed on reopen");
            state.playerData.playerStats[PubRecruitment.TotalKey].Value=19;
            Require(model.TryRecruit(state,1,"gold",100,20,out rewards,out bonus,out error)&&bonus==1&&rewards.Count==1&&rewards[0].RewardMultiplier==2,"Bonus rewards at 20: "+error);
            testCanvas=new GameObject("Temporary Pub verification",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
            testCanvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;testCanvas.GetComponent<CanvasGroup>().alpha=0;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PubBuilder.PrefabPath);var panel=UnityEngine.Object.Instantiate(prefab,testCanvas.transform);var controller=panel.GetComponent<PubController>();controller.Show();Canvas.ForceUpdateCanvases();
            Require(controller.Offers.Count==3,"Live UI binding");Require(controller.GetComponentsInChildren<PubHeroCard>(true).All(c=>c.portrait.sprite!=null),"Missing hero portrait");
            var fields=new SerializedObject(controller);var button=(Button)fields.FindProperty("recruitButton").objectReferenceValue;
            int before=PubRecruitment.Currency(player,"gold");button.onClick.Invoke();button.onClick.Invoke();
            Require(controller.IsBusy&&PubRecruitment.Currency(player,"gold")==before-100,"Double click charged twice or recruit did not start");
            controller.Hide();Require(controller.IsBusy,"Close button interrupted roulette");
            panel.SetActive(false);Require(!controller.IsBusy,"Disabling mid-animation kept controller locked");
            File.WriteAllText(PubInspector.Work+"/play.txt",JsonConvert.SerializeObject(new{result="PASS",eligibleHeroes=model.Pool.Count,excludedRows=excluded,lineup=model.Offers.Select(h=>h.Id).ToArray(),native3x=true,bonus20=true,refresh25=true,persistedLineup=true,prefabBindings=true,portraits=true,doubleClickGuard=true,originalPlayerUntouched=true},Formatting.Indented));
        }
        catch(Exception e){File.WriteAllText(PubInspector.Work+"/play.txt","FAIL "+e);Debug.LogException(e);}
        finally
        {
            if(testCanvas!=null)UnityEngine.Object.Destroy(testCanvas);
            if(state!=null&&originalPlayer!=null)
            {
                state.all["main_player"]=originalPlayer;state.playerData=originalData;state.useSave=originalUseSave;
                if(registrations!=null)foreach(var id in state.all.Keys.Where(id=>!registrations.Contains(id)).ToArray())state.all.Remove(id);
            }
            SessionState.SetBool("PubCheck.active",false);EditorApplication.update-=Pulse;running=false;
            if(!SessionState.GetBool("PubCheck.wasPlaying",false))EditorApplication.isPlaying=false;
        }
    }
}
#endif
