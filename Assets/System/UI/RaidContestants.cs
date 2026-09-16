using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RaidContestants : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject contestants;
    public Transform contHolder;
    private string raidID = "";
    private void OnEnable()
    {
        var b = GetComponentInParent<ObjHolder>();
        if (b.obj == null)
        {
            Invoke("OnEnable", 0.1f);
            return;
        }

        var c = b.obj.GetPar("raid_boss");
        if (c < 1)
        {
            raidID = "";
            contestants.SetActive(false);
            return;
        }
        contestants.SetActive(true);
        raidID = b.obj.RID;

        MainStatesServer.instance.GetRaidData(b.obj.RID, DataReceived);
        
    }

    public Button btn;
    public TextMeshProUGUI availableTimer;
    public static int minsCd = 15;
    
    public void DataReceived(string data)
    {
        Debug.Log(data);
        var str = data.Split(new string[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 1; i < contHolder.childCount; i++) contHolder.GetChild(i).gameObject.SetActive(false);
        
        for (int i = 0; i < str.Length; i++)
        {
            var ss = str[i].Split(',');
            if (i < contHolder.childCount-1)
            {
                contHolder.GetChild(i+1).gameObject.SetActive(true);
                contHolder.GetChild(i+1).Find("name").GetComponent<TextMeshProUGUI>().text = ss[0];
                contHolder.GetChild(i+1).Find("icon").GetComponent<Image>().sprite = ResourceHolder.instance.avas[ss[1]];
                contHolder.GetChild(i+1).Find("damage").GetComponent<TextMeshProUGUI>().text = ss[2];
            }
        }
    }
    
    private void Update()
    {
        var hh = MainStates.instance.mainPlayer.GetLongPar("timer_raid_" + raidID);
        var kk = TimeManager.instance.GetCurrentTimeLong();
        if (hh <= 0 || kk >= hh)
        {
            availableTimer.text = "";
            btn.interactable = true;
        }
        else
        {
            availableTimer.text = "Available in: " + TimeManager.instance.GetStringTillEnd((long)hh, 0,true);
            btn.interactable = false;
        }
    }
}
