using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Vizualo : MonoBehaviour
{
    public string condID = "";
    public bool isNot = false;
    // Update is called once per frame
    private CanvasGroup c;
    private AbsHolder abs;
    
    public Button relButton;
    public GameObject relPrice;
    
    //other
    public string id;
    private RObj who;
    private ObjHolder wHolder;
    
    
    public bool readyNotTakenTask = false;
    public bool taken = false;
    public bool notTaken = false;
    public bool notReady;
    public bool noConditionMatch = false;

    public bool asHolderCond = false;
    
    //common case
    [Header("also IS NOT counted")]
    public GameObject asOther;
    public List<UnoReq> reqs = new List<UnoReq>();
    
    public GameObject forOther;
    public List<GameObject> alsoActive = new List<GameObject>();
    
    public bool activateScale = false; 
    
    private void OnEnable1()
    {
        if (asHolderCond)
        {
            wHolder = GetComponentInParent<ObjHolder>();
            id = wHolder.obj.dbObj.ID;
            who = wHolder.obj;
            if (ConfigLoader.Instance.allRelConditions.ContainsKey(id))
                condID = ConfigLoader.Instance.allRelConditions[id];
        }
    }

    private void Start()
    {
        c = GetComponent<CanvasGroup>();
        if (c == null) c = gameObject.AddComponent<CanvasGroup>();
        abs = GetComponentInParent<AbsHolder>();
        
        if (abs)
        {
            id = abs.id;
            condID = abs.condId;
        }
        else if (asHolderCond)
        {
            wHolder = GetComponentInParent<ObjHolder>();
            /*
            if (wHolder.obj == null)
            {
                Invoke("Start", 0.1f);
                return;
            }
            */

            if (wHolder.obj != null)
            {
                id = wHolder.obj.dbObj.ID;
                who = wHolder.obj;
                if (ConfigLoader.Instance.allRelConditions.ContainsKey(id))
                    condID = ConfigLoader.Instance.allRelConditions[id];
            }
        }
        
        MainStates.allVisuals.Add(this);

        var hu = transform.parent.GetComponentInChildren<FPrice>();
        if (hu != null) relPrice = hu.gameObject;
    }

    public void Updateo()
    {
        if (!ConfigLoader.parseEnded) return;

        if (asHolderCond)
        {
            who = wHolder.obj;
        }
        
        if (abs && abs.isTask)
        {

            if (readyNotTakenTask)
            {
                var task = MainStates.instance.playerData.playerTasks.Find(x => x.id == id);
                Activate(task.completed && !task.taken);
                return;
            }

            if (taken)
            {
                var task = MainStates.instance.playerData.playerTasks.Find(x => x.id == id);
                Activate(task.taken);
                return;
            }

            if (notReady)
            {
                var task = MainStates.instance.playerData.playerTasks.Find(x => x.id == id);
                Activate(!task.taken && !task.completed);
                return;
            }
        }
        else if (abs && abs.isSkill)
        {
            bool b1 = true;
            if (condID != "")
            {
                var a1 = ConfigLoader.Instance.allConditions[condID];
                b1 = ModelStatistics.instance.CheckCondition(a1);
            }

            if (b1 && taken)
            {
                var v = MainStates.instance.GetBuff(id);
                Activate(v != null);
                return;
            }
            
            if (b1 && notTaken)
            {
                var v = MainStates.instance.GetBuff(id);
                Activate(v == null);
                return;
            }

            if (!b1 && !noConditionMatch)
            {
                Activate(false);
                return;
            }

            if (noConditionMatch)
            {
                Activate(!b1);
                return;
            }
        }


        if (reqs.Count > 0)
        {
            foreach (var req in reqs)
            {
                if (req.what.IndexOf("{") >= 0)
                {
                    req.what = id;
                }
            }

            var g = ModelStatistics.instance.CheckCondition(reqs, who);
            Activate(g);
        }
        else if (asOther)
        {
            Activate(isNot ? !asOther.activeSelf :  asOther.activeSelf);
        }
        else if (condID != "")
        {
            var a = ConfigLoader.Instance.allConditions[condID];
            var b = ModelStatistics.instance.CheckCondition(a);
            if (b && !isNot) Activate(true);
            else if (!b && isNot) Activate(true);
            else Activate(false);
        }        
        else
        {
            Activate(true);
        }

    }

    public void Activate(bool val)
    {
         var ls = Vector3.one;
         if (!val) ls = Vector3.zero;
        
        if (forOther != null)
        {
            if (activateScale)
            {
                forOther.transform.localScale = ls;
            }
            else
                forOther.SetActive(val);
        }
        else
        {
            if (activateScale)
            {
                transform.localScale = ls;
            }
            else
                gameObject.SetActive(val);
        }
        
        if (relButton != null) relButton.interactable = isNot ? !val : val;

        foreach (var v in alsoActive)
        {
            if (activateScale)
            {
                forOther.transform.localScale = ls;
            }
            else
                v.SetActive(val);
        }
        //if (val) c.alpha = 1;
        //else c.alpha = 0;
    }
}
