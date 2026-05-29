using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AbsHolder : MonoBehaviour
{
    public string id;
    public string condId;
    public RObj b;
    public bool isRuntime;
    private bool done = false;
    public Button take;
    
    //
    public bool isTask = true;
    public bool isSkill = false;
    
    private void Start()
    {
        if (!ConfigLoader.parseEnded)
        {
            Invoke("Start", 0.1f);
            return;
        }

        if (isSkill)
        {
            DoSkill();
            return;
        }
        
        done = true;
        b = new RObj(id, ItemType.task);
        var a = gameObject.AddComponent<ObjHolder>();
        a.obj = b;
        Recalc();

        if (take)
        {
            var task = DatabaseAll.instance.allTasks[id];
            take.onClick.AddListener(() =>
            {
                MainStates.instance.AddItems(task.rewards);
                var p = MainStates.instance.playerData.playerTasks.Find(x => x.id == id);
                p.taken = true;
                ModelStatistics.instance.UpdateAllTasks();
            });
        }
        
    }

    public void DoSkill()
    {
        var s = MainStates.instance.GetSkill(id);
        if (s != null)
        {
            var a = gameObject.AddComponent<ObjHolder>();
            a.obj = s;
        }
        else
        {
            var f = DatabaseAll.instance.CreateProjectile(MainStates.instance.all["main_player"], id, Vector3.zero,
                false, false);
            var a = gameObject.AddComponent<ObjHolder>();
            a.obj = f;
        }
    }

    public void Recalc()
    {
        if (!DatabaseAll.instance.allTasks.ContainsKey(id)) return;
        var task = DatabaseAll.instance.allTasks[id];
        ModelStatistics.instance.GetMeProgress(task, out float me, out float all);
        b.SetPar("me", me);
        b.SetPar("all", all);
        b.RecalcPars();
        
    }
    public void Update()
    {
        if (!done) return;
        if (isRuntime)
        {
            Recalc();
        }
    }
}
