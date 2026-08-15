using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIlevelUp : MonoBehaviour
{
    public GameObject activate;
    public List<GameObject> waitWhat = new List<GameObject>();

    public static bool wasLevelup = false;
    public static int levelWas = -1;
    // Update is called once per frame
    public Transform statHolder;
    
    
    void Update()
    {
        if (!wasLevelup) return;
        
        bool check = false;
        foreach (var v in waitWhat)
        {
            if (v.activeSelf) check = true;
        }
        if (check) return;
        
        Activate();
    }

    public void Activate()
    {
        activate.SetActive(true);
        wasLevelup = false;

        List<string> stats = new List<string> { "health", "attack", "res", "def" };
        int levelNow = (int)MainStates.instance.mainPlayer.GetPar("level");
        
        var e1 = Mathf.Pow(1.1f, levelNow - levelWas);
        
        for (int i = 0; i < stats.Count; i++)
        {
            var fg = statHolder.GetChild(i);
            fg.Find("statName").GetComponent<TextMeshProUGUI>().text = stats[i];
            var t1 = MainStates.instance.mainPlayer.GetPar(stats[i]);
            fg.Find("statEnd").GetComponent<TextMeshProUGUI>().text = ((int)t1).ToString();
            var dlt = (int)(t1 - t1 / e1);
            fg.Find("statDlt").GetComponent<TextMeshProUGUI>().text = "+" + (dlt).ToString();
        }
        
    }
}
