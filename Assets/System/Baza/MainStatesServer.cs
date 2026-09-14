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

}
