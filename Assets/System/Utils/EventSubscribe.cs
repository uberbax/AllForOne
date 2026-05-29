using System;
using UnityEngine;

public class EventSubscribe : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public string eventName = "";
    public string wh = "";
    public string wh1 = "";
    
    
    public void Start()
    {
        EventManager.SUB(eventName, (x) =>
        {
            if (wh != "" && x.what != wh) return;
            if (wh1 != "" && x.what1 != wh1) return;
            
            gameObject.SetActive(false);
            gameObject.SetActive(true);
            
        });
    }
}
