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
            var f = GetComponentInParent<ObjHolder>();
            id = f.obj.dbObj.ID;
            if (ConfigLoader.Instance.allRelConditions.ContainsKey(id))
                condID = ConfigLoader.Instance.allRelConditions[id];
        }
        
        MainStates.allVisuals.Add(this);

        var hu = transform.parent.GetComponentInChildren<FPrice>();
        if (hu != null) relPrice = hu.gameObject;
    }

    public void Updateo()
    {
        if (!ConfigLoader.parseEnded) return;

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

            var g = ModelStatistics.instance.CheckCondition(reqs);
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
        gameObject.SetActive(val);
        if (relButton != null) relButton.interactable = isNot ? !val : val;

        //if (val) c.alpha = 1;
        //else c.alpha = 0;
    }
}
