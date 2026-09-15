using System;
using System.Collections;
using UnityEngine;

public class MainStatesServer : MonoBehaviour
{
    private string servUrl = "http://107.174.221.111/";

    public static MainStatesServer instance;
    private void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void DealRaidDamage(string raidName, string classN, string heroN, int damage)
    {
        if (heroN == "") heroN = "hero";
        string url = servUrl + "add_raid_damage.php?" +  "raid_name" + "=" + raidName + "&" 
                     +"class" + "=" + classN + "&" 
                     + "name" + "=" + heroN + "&" 
                     + "damage" + "=" + damage;

        Debug.Log(url);
        StartCoroutine(DealRaidDamageA(url));
    }
    
    public IEnumerator DealRaidDamageA(string url)
    {
        var w = new WWW(url);
        
        yield return w;
    }

    public void GetRaidData(string raidName, Action<string> act)
    {
        StartCoroutine(GetRaidDataA(raidName, act));
    }

    public IEnumerator GetRaidDataA(string raidName, Action<string> act)
    {
        var w = new WWW(servUrl + raidName + ".txt");
        
        yield return w;

        if (act != null)
        {
            act(w.text);
        }
        
    }

}
