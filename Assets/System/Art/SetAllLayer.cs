using System;
using UnityEngine;

public class SetAllLayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform lo;
    public Transform high;

    public bool doBreathing = false;
    
    [ContextMenu("Do set")]
    public void SetAll()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (!transform.GetChild(i).gameObject.activeInHierarchy) continue;
            UtilsControl.CalculateLayer(transform.GetChild(i).gameObject, lo, high);
        }
    }

    private void OnEnable()
    {
        /*
        if (doBreathing)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (!transform.GetChild(i).gameObject.activeInHierarchy) continue;
                var cc =   transform.GetChild(i);
                //cc.gameObject.AddComponent<Brat>()
            }
        }
        */
    }
    
    
}
