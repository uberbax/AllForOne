#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using TMPro;

public static class PubRouletteValidation
{
    private const string Work=@"C:\Users\s4032\Documents\Codex\2026-09-30\d-unity-projects-allforone-allforone-assets\work\pub-roulette";
    private const BindingFlags Fields=BindingFlags.NonPublic|BindingFlags.Instance;
    [InitializeOnLoadMethod]private static void Init(){if(File.Exists(Work+"/validate.request"))EditorApplication.delayCall+=Run;}
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static object Get(object target,string name)=>target.GetType().GetField(name,Fields).GetValue(target);
    private static void Set(object target,string name,object value)=>target.GetType().GetField(name,Fields).SetValue(target,value);
    [MenuItem("Tools/UI/Pub/Validate single-roll roulette")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var oldState=MainStates.instance;var oldDb=DatabaseAll.instance;var oldConfig=ConfigLoader.Instance;var oldResources=ResourceHolder.instance;
        var oldFillers=UIfiller.instances;var oldRefresh=UIfiller.acts;
        var statics=typeof(ConfigLoader).GetFields(BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Where(f=>!f.IsLiteral&&!f.IsInitOnly).ToDictionary(f=>f,f=>f.GetValue(null));
        GameObject fixture=null;
        try
        {
            fixture=new GameObject("Isolated Pub roulette test");fixture.SetActive(false);
            var config=fixture.AddComponent<ConfigLoader>();ConfigLoader.Instance=config;
            using(var stream=new FileStream("Assets/StreamingAssets/Config_exp_rumble.xlsx",FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
            {
                var book=ExcelHelper.LoadExcel(stream);config.ParseHeroes(book.ToTSV("Heroes"));config.ParseSkills(book.ToTSV("SKILLS2"));config.ParseArtefacts(book.ToTSV("ITEMS"));
            }
            var db=fixture.AddComponent<DatabaseAll>();DatabaseAll.instance=db;
            typeof(DatabaseAll).GetMethod("OnSkillsParsed",Fields).Invoke(db,new object[]{config.skills});
            typeof(DatabaseAll).GetMethod("OnHeroesParsed",Fields).Invoke(db,new object[]{config.heroes});
            var items=Get(config,"artefacts");typeof(DatabaseAll).GetMethod("OnItemsParsed",Fields).Invoke(db,new[]{items});
            ResourceHolder.instance=UnityEngine.Object.FindFirstObjectByType<ResourceHolder>(FindObjectsInactive.Include);
            ConfigLoader.parseEnded=true;UIfiller.instances=new List<UIfiller>();UIfiller.acts=null;
            var state=fixture.AddComponent<MainStates>();MainStates.instance=state;state.useSave=false;
            var player=new RObj{RID="main_player",dbObj=db.heroes["toad_horn"]};
            player.upgradePars["level"]=0;player.upgradePars["rarity"]=0;player.upgradePars["registered_damage"]=0;player.RecalcPars();
            state.all["main_player"]=player;state.playerData=new PlayerData{mainPlayer=player};
            var gold=db.CreateItem("gold",10000,false,false);gold.index=0;gold.owner=player;player.inventory.Add(gold);
            var model=new PubRecruitment(47);model.BuildPool(config.heroes,db.heroes,new[]{"toad_horn","worm_poison","winter_ice"});model.RestoreOrRefresh(state.playerData,DateTime.UtcNow,30);
            var shardCounts=new Dictionary<string,int>();int mileage=0;
            for(int multiplier=1;multiplier<=3;multiplier++)
            {
                int balance=PubRecruitment.Currency(player,"gold"),heroes=player.inventory.Count(i=>i.it==ItemType.monster);
                Check(model.TryRecruit(state,multiplier,"gold",100,20,out var rewards,out int bonus,out string error),error);
                Check(rewards.Count==1&&bonus==0&&model.TotalRecruits==multiplier,"Extra draw or progress");
                var won=rewards[0];Check(won.Quantity+won.TotalMileageTokens==30*multiplier,"Scaled reward total");
                Check(PubRecruitment.Currency(player,"gold")==balance-100*multiplier,"Purchase price");
                Check(player.inventory.Count(i=>i.it==ItemType.monster)==heroes+(won.GrantedHero?1:0),"Hero multiplied");
                shardCounts.TryGetValue(won.Id,out int previous);shardCounts[won.Id]=previous+won.Quantity;mileage+=won.TotalMileageTokens;
                Check(player.inventory.Where(i=>i.shardID==won.Id).Sum(i=>i.GetPar("amount"))==shardCounts[won.Id],"Actual shard amount");
                Check(PubRecruitment.Currency(player,"mileage_token")==mileage,"Actual mileage amount");
            }
            state.playerData.playerStats[PubRecruitment.TotalKey].Value=19;
            Check(model.TryRecruit(state,3,"gold",100,20,out var final,out int milestone,out string finalError),finalError);
            Check(final.Count==1&&milestone==1&&final[0].Quantity+final[0].TotalMileageTokens==180,"Bonus must scale same hero");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PubBuilder.PrefabPath);
            var panel=UnityEngine.Object.Instantiate(prefab,fixture.transform,false);var controller=panel.GetComponent<PubController>();
            controller.EditorPreview(model.Offers.ToArray(),ResourceHolder.instance.items["gold"]);
            Set(controller,"model",model);Set(controller,"loadedData",state.playerData);Set(controller,"multiplier",3);Set(controller,"busy",true);
            var cards=(PubHeroCard[])Get(controller,"cards");var result=(GameObject)Get(controller,"resultPanel");
            controller.Refresh();
            Check(cards.All(c=>!c.GetComponent<CanvasGroup>().interactable&&!c.GetComponentInChildren<GoAscend>(true).enabled),"Card interactions/ascension should pause during roulette");
            var sequence=(IEnumerator)typeof(PubController).GetMethod("Reveal",Fields).Invoke(controller,new object[]{final,milestone});
            int steps=0,last=-1;
            while(sequence.MoveNext())
            {
                Check(!result.activeSelf,"Result shown before roulette finished");
                var selected=cards.Select((card,index)=>new{card,index}).Where(p=>p.card.highlight.color.a>.8f).ToArray();
                Check(selected.Length==1,"Roulette must highlight exactly one card");last=selected[0].index;
                if(++steps>30)throw new InvalidOperationException("Roulette did not finish");
            }
            Check(steps>=13&&cards[last].HeroId==final[0].Id&&result.activeSelf&&!controller.IsBusy,"Incorrect final roulette card/result");
            Check(cards.All(c=>c.GetComponent<CanvasGroup>().interactable&&c.GetComponentInChildren<GoAscend>(true).enabled),"Card controls not restored after reveal");
            var counts=(TMP_Text[])Get(controller,"resultCounts");Check(counts[0].text.Contains("Shards ×"+final[0].Quantity)&&counts[0].text.Contains("Mileage ×"+final[0].TotalMileageTokens),"Result totals do not match inventory reward");
            Set(controller,"busy",true);cards[0].SetRouletteHighlight(true);
            typeof(PubController).GetMethod("OnDisable",Fields).Invoke(controller,null);
            Check(!controller.IsBusy&&cards.All(c=>c.highlight.color.a==0),"Disable did not clear roulette");
            File.WriteAllText(Work+"/validation.txt","PASS: native config and RObj inventory transactions at x1/x2/x3; one hero selection/progress step, exact scaled shards/mileage/price, one newly owned hero, milestone scales same hero. Existing Pub prefab reveal iterator stepped through all highlight states: one card at a time, correct final hero and scaled result counts; disable clears highlights/busy. Isolated data, no saves modified. Real-time Play Mode animation was not run.");File.Delete(Work+"/validate.request");
        }
        catch(Exception e){File.WriteAllText(Work+"/validation.txt","FAIL: "+e);Debug.LogException(e);}
        finally
        {
            if(fixture!=null)UnityEngine.Object.DestroyImmediate(fixture);
            MainStates.instance=oldState;DatabaseAll.instance=oldDb;ResourceHolder.instance=oldResources;
            foreach(var field in statics)field.Key.SetValue(null,field.Value);ConfigLoader.Instance=oldConfig;
            UIfiller.instances=oldFillers;UIfiller.acts=oldRefresh;
        }
    }
}
#endif
