using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmallLoot : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private ObjHolder other;
    private RObj mon;

    public bool fillFull = false;
    
    private void OnEnable()
    {
        //if (other == null)
        //{
            other = GetComponentInParent<ObjHolder>();
            mon = other.obj;
        //}
        
        if (fillFull)
            FillFull();
        else Fill();
    }

    public void Fill()
    {
        List<string> all = ModelSet.GetMeItemsAll(mon.dbObj.drop);
        
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
        
        for (int i = 0; i < all.Count; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            var g = transform.GetChild(i);
            //check codex ?
            var icon = g.Find("icon").GetComponent<Image>();
            var txt = g.Find("icon/name").GetComponent<TextMeshProUGUI>();
            var c = ModelStatistics.instance.Codex_IsLootMet(mon.dbObj.ID, all[i]);
            if (c)
            {
                icon.color = Color.white;
                icon.sprite = ResourceHolder.instance.items[all[i]];
                txt.text = ConfigLoader.Instance.GetMeLocale(all[i]);
            }
            else
            {
                icon.color = Color.clear;
                txt.text = "???";
            }

        }
    }

    public void FillFull()
    {
        var all = MainStates.instance.dropTables["battle_reward"];
        
        for (int i = 0; i < transform.childCount; i++)
            transform.GetChild(i).gameObject.SetActive(false);
        
        for (int i = 0; i < all.Count; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            var g = transform.GetChild(i);
            //check codex ?
            var icon = g.Find("icon").GetComponent<Image>();
            var txt = g.Find("icon/name").GetComponent<TextMeshProUGUI>();

            icon.sprite = ResourceHolder.instance.items[all[i].Key];
            txt.text = (all[i].Value <= 1 ? "" : all[i].Value + " ") + ConfigLoader.Instance.GetMeLocale(all[i].Key);
            txt.color = ResourceHolder.instance.rareColors[all[i].Val3];

        }
    }
}
