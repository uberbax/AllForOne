using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnoNotify : MonoBehaviour
{
    // Start is called before the first frame update
    public DerFlags flags;
    public bool lookUp;
    public List<GameObject> states = new List<GameObject>();

    [Header("Custom params")] 
    public bool haveItemCountOver = false;
    public int itemCountOver = 0;
    public string itemID = "";
    
    //---------------------
    public bool haveTaskCompletedNoTaken = false;
    public string taskId = "";
    
    //how can we deal with or
    public bool haveUncollectedDaily;

    public bool haveUncollectedWeeky;
    
    public bool haveUncollectedDailyOrWeekly;
    
    public bool haveUncollectedMail;
    
    public bool haveAnyTaskCompletedNoTaken = false;
   
    public bool haveAutoreward = false;
    
    
    /*
    //have any daily chest uncollected
    public bool haveDailyChestUncollected = false; 
    
    //have daily points < 100 && daily task uncollected
    public bool haveItemCountLess = false;  
    
    public bool haveDailyTaskUncollected = false; 
    */
    
    //--override action
    [Header("Override action")]
    public bool haveTaskCompletedAndTaken = false;
    public string taskId1 = "";

    private TasksProg task = null;
    
    public int CalculateCustom()
    {
        //2 - override
        //1 - active
        //0 - disabled

        //2
        if (haveAnyTaskCompletedNoTaken)
        {
            var f = MainStates.instance.playerData.playerTasks.Find(x => x.completed && !x.taken);
            if (f != null) return 1;
            else return 0;
        }

        if (haveAutoreward)
        {
            var gf = TimeManager.instance.GetCurrentTimeLong() - MainStates.instance.lastAutoRewardTime;
            
            gf /= (int)ConfigLoader.GetMetaParamValue("autoreward_refresh");
            if (gf > 0) return 1;
            else return 0;
        }
        
        if (haveTaskCompletedAndTaken)
        {
            if (task == null)
            {
                task = MainStates.instance.playerData.playerTasks.Find(x => x.id == taskId1);
            }

            if (task.completed && task.taken)
            {
                return 2;
            }
            
        }
        
        //1
        bool check = true;
        if (haveItemCountOver)
        {
            int cnt = MainStates.instance.GetItemsCount(itemID);
            if (cnt < itemCountOver)
                check = false;
        }

        if (haveTaskCompletedNoTaken)
        {
            if (task == null)
            {
                task = MainStates.instance.playerData.playerTasks.Find(x => x.id == taskId1);
            }

            if (!task.completed)
                check = false;
        }

        bool hud = false;
        bool hud_w = false;
        if (haveUncollectedDaily)
        {
            var yy = MainStates.instance.playerData.playerTasks.FindAll(x =>
                x.id.IndexOf("chestdai") >= 0 && x.completed && !x.taken).Count;

            if (yy > 0) hud = true;
            
            //
            itemCountOver = MainStates.instance.GetItemsCount("daily_point");
            
            var pp = MainStates.instance.playerData.playerTasks.FindAll(x =>
                x.id.IndexOf("daily") >= 0 && x.completed && !x.taken).Count;

            if (itemCountOver < 100 && pp > 0)
                hud = true;
        }

        
        int cntM = 0;
        if (haveUncollectedMail)
        {
            cntM = MainStates.instance.HaveUncollectedMail();
        }
        

        if (haveUncollectedDailyOrWeekly)
        {
            check = check && (hud || hud_w);
        }
        else if (haveUncollectedDaily)
        {
            check = check && hud;
        }
        else if (haveUncollectedWeeky)
        {
            check = check && hud_w;
        }
        else if (haveUncollectedMail)
        {
            check = check && (cntM > 0);
        }

        if (check)
            return 1;

        return 0;
    }

    public void DoCustom(int state)
    {
        //states can be 2 (enable or disable)
        
        //or 3 - enable - disable - collectged state

        if (states.Count == 1)
        {
            states[0].SetActive(state == 1);
            return;
        }
        
        for (int i = 0; i < states.Count; i++)
        {
            states[i].SetActive(i == state);
        }
    }
    //itspossibly can be changed
    private void Start()
    {
        NotifyControl.instance.Add(this);
        if (states.Count == 0)
        {
            states.Add(gameObject);
        }
    }
}
