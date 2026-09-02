using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIbattleLog : MonoBehaviour
{
    private List<string> logs = new List<string>();
    public TextMeshProUGUI log;
    private int numsDisplay = 5;

    [Header("Other")] 
    public GameObject msg;
    public TextMeshProUGUI header;
    public TextMeshProUGUI description;
    
    private void Awake()
    {
        EventManager.SUB("battle_start", Clean);
        EventManager.SUB("skill_casted", Casted);
    }

    private void Casted(ArgPass obj)
    {
        //change sometimes in future

        FunctionTimer.Create(() =>
        {
            header.text = obj.who.dbObj.ID;
            description.text = "uses " + obj.who2.dbObj.ID;            
            
            
            if (!MainStates.instance.queTimes.ContainsKey("LOG"))
                MainStates.instance.queTimes.Add("LOG", new UnoQueTime { tm = Time.time });
            else
                MainStates.instance.queTimes["LOG"].tm = Time.time;
                
            UtilsControl.Instance.ApplyCurve(msg.transform, AnimationCurve.Linear(0, 0, 1, 1),
                UtilsControl.CurveType.CanvasFade, null,
                0.333f, 3, 1, 0, Color.white, repCount: 1, pong: true, waitBetween: 1);
        }, 0, () =>
        {
            if (!MainStates.instance.queTimes.ContainsKey("LOG")) return true;
            if (MainStates.instance.queTimes["LOG"].tm < Time.time - 2) return true;
            return false;

        });
        //
        
        string s = obj.who.dbObj.ID + " uses " + obj.who2.dbObj.ID;
        logs.Add(s);
        string result = "";
        if (logs.Count > numsDisplay)
        {
            for (int i = logs.Count - numsDisplay; i < logs.Count; i++)
            {
                result += logs[i] + "\n";
            }
        }
        else
        {
            for (int i = 0; i < logs.Count; i++)
            {
                result += logs[i] + "\n";
            }
        }
        log.text = result;
    }

    private void Clean(ArgPass obj)
    {
        logs.Clear();
        log.text = "";
    }
}
