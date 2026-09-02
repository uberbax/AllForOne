using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SmallResists : MonoBehaviour
{
    private ObjHolder other;
    private RObj mon;

    public Transform resistsRoot;
    public Transform immunitiesRoot;
    public Transform weaknessRoot;
    
    //
    public ObjHolder itemReward;
    public TakeReward takeReward;
    
    private void OnEnable()
    {
        //if (other == null)
        //{
            other = GetComponentInParent<ObjHolder>();
            mon = other.obj;
        //}
        
        Fill();
    }

    public void Fill()
    {
        int immuns = 0;
        int weaks = 0;
        int resists = 0;
        for (int i = 0; i < immunitiesRoot.childCount; i++) immunitiesRoot.GetChild(i).gameObject.SetActive(false);
        for (int i = 0; i < weaknessRoot.childCount; i++) weaknessRoot.GetChild(i).gameObject.SetActive(false);
        for (int i = 0; i < resistsRoot.childCount; i++) resistsRoot.GetChild(i).gameObject.SetActive(false);
        
        
        
        foreach (var t in MainStates.dmgTypes)
        {
            var g = mon.GetPar("res_" + t.Key);
            if (g < 0)
            {
                weaknessRoot.GetChild(weaks).gameObject.SetActive(true);
                bool b = ModelStatistics.instance.Codex_IsWeakMet(mon.dbObj.ID, t.Key);
                if (!b)
                {
                    weaknessRoot.GetChild(weaks).GetChild(0).GetComponent<TextMeshProUGUI>().text = "???";
                    weaknessRoot.GetChild(weaks).GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
                }
                else
                {
                    weaknessRoot.GetChild(weaks).GetChild(0).GetComponent<TextMeshProUGUI>().text = t.Key;
                    weaknessRoot.GetChild(weaks).GetChild(0).GetComponent<TextMeshProUGUI>().color =
                        ResourceHolder.instance.elemColors[t.Key];
                }

                weaks++;
            }
            else if (g >= 100)
            {
                immunitiesRoot.GetChild(immuns).gameObject.SetActive(true);
                bool b = ModelStatistics.instance.Codex_IsImmuneMet(mon.dbObj.ID, t.Key);
                if (!b)
                {
                    immunitiesRoot.GetChild(immuns).GetChild(0).GetComponent<TextMeshProUGUI>().text = "???";
                    immunitiesRoot.GetChild(immuns).GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
                }
                else
                {
                    immunitiesRoot.GetChild(immuns).GetChild(0).GetComponent<TextMeshProUGUI>().text = t.Key;
                    immunitiesRoot.GetChild(immuns).GetChild(0).GetComponent<TextMeshProUGUI>().color =
                        ResourceHolder.instance.elemColors[t.Key];
                }

                immuns++;
            }
            else if (g > 0)
            {
                resistsRoot.GetChild(resists).gameObject.SetActive(true);
                bool b = ModelStatistics.instance.Codex_IsResMet(mon.dbObj.ID, t.Key);
                if (!b)
                {
                    resistsRoot.GetChild(resists).GetChild(0).GetComponent<TextMeshProUGUI>().text = "???";
                    resistsRoot.GetChild(resists).GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
                }
                else
                {
                    resistsRoot.GetChild(resists).GetChild(0).GetComponent<TextMeshProUGUI>().text = t.Key;
                    resistsRoot.GetChild(resists).GetChild(0).GetComponent<TextMeshProUGUI>().color =
                        ResourceHolder.instance.elemColors[t.Key];
                }

                resists++;
            }
        }
        
        //
        Bon cc = new Bon();
        cc.Key = mon.dbObj.parsStr["codex_reward"];
        cc.Value = 1;
        cc.Val3 = 4;
        itemReward.obj = DatabaseAll.instance.CreateItem(cc.Key, 1, rarity:4 );
        takeReward.stat = "codex_" + mon.dbObj.ID + "_completed";
        takeReward.rewards.Clear();
        takeReward.rewards.Add(cc);
    }


}
