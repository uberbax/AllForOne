using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static Dictionary<string, Action<ArgPass>> dynActions = new Dictionary<string, Action<ArgPass>>();
    public static Dictionary<string, int> eventCount = new Dictionary<string, int>();
    
    
    public static void SUB(string evt, Action<ArgPass> b)
    {
        if (dynActions.ContainsKey(evt))
        {
            dynActions[evt] += b;
        }
        else
        {
            dynActions.Add(evt, b);
        }
    }
    
    public static void INV(string evt, ArgPass e)
    {
        Debug.Log(evt + " INVOKED " + (e == null ? "null" : e.what1));
        if (dynActions.ContainsKey(evt))
        {
            dynActions[evt].Invoke(e);
            if (eventCount.ContainsKey(evt))
                eventCount[evt]++;
            else
            {
                eventCount.Add(evt, 1);
            }
        }
    }

    public static int HasEvent(string evt)
    {
        if (eventCount.ContainsKey(evt)) return eventCount[evt];
        else return 0;
    }

    private void OnDisable()
    {
        dynActions.Clear();
        eventCount.Clear();
    }
}
