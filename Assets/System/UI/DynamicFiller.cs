using System;
using UnityEngine;

public class DynamicFiller : MonoBehaviour
{
    public Transform holder;

    public RObj mon;
    private void OnEnable()
    {
        mon = MainStates.instance.lastAllySelected;
        Fill();
    }

    public void Fill()
    {
        for (int i = 0; i < holder.childCount; i++)
        {
            holder.GetChild(i).gameObject.SetActive(false);
        }
        
        for (int i = 0; i < mon.dynamic.dynList.Count; i++)
        {
            holder.GetChild(i).gameObject.SetActive(true);
            holder.GetChild(i).GetComponent<Buyable>().dynamicID = mon.dynamic.dynList[i];
        }
    }
}
