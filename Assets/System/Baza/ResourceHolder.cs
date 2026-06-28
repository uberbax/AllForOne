using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHolder : MonoBehaviour
{
    public GameObject emptyProj;
    public static ResourceHolder instance;

    public StringObjectDictionary monsters;
    public StringSpriteDictionary misc;
    public StringSpriteDictionary avas;
    public StringSpriteDictionary skills;
    public StringSpriteDictionary pars;
    public StringSpriteDictionary diaAvas;
    public StringSpriteDictionary tasks;
    public StringSpriteDictionary dynamics;

    public IntColorDictionary rareColors;
    public IntStringDictionary rareString;
    public IntSpriteDictionary rareFrame;
    public IntSpriteDictionary slotFrame;
    
    public StringSpriteDictionary items;
    public StringSpriteDictionary buildings;
    
    public StringObjectDictionary itemsGO;
    public StringObjectDictionary buildingsGO;
    
    
    public List<SkillEtc> skillEffects = new List<SkillEtc>();
    
    public StringObjectDictionary XD;
    
    public StringObjectDictionary miscGO;
    public StringObjectDictionary uiGO;

    public StringObjectDictionary skillsWorld;
    public StringAudioclipDictionary sounds;
    public Sprite GetDiaAva(string what)
    {
        if (what == "") return null;
        return diaAvas[what];
    }
    public Sprite GetAva(string str)
    {
        return null;
    }
    public Sprite GetMisc(string str)
    {
        return null;
    }
    
    private void Awake()
    {
        instance = this;
    }
    
    public SkillEtc GetMeSkillEtc(Obj who, string sklName)
    {
        
        var tt = (who.ID + sklName).ToLower();
        //Debug.Log(tt);
        
        foreach (var v in skillEffects)
        {
            bool check = true;
            foreach (var b in v.conds)
            {
                if (tt.IndexOf(b.ToLower()) < 0)
                {
                    check = false;
                    break;
                }
            }

            if (check)
            {
                return v;
            }
        }
        
        tt = (who.ID + sklName + "_any").ToLower();
        
        foreach (var v in skillEffects)
        {
            bool check = true;
            foreach (var b in v.conds)
            {
                if (tt.IndexOf(b.ToLower()) < 0)
                {
                    check = false;
                    break;
                }
            }

            if (check)
            {
                return v;
            }
        }

        return null;
    }

    public GameObject GetGameobject(string id)
    {
        if (monsters.ContainsKey(id)) return  monsters[id];
        if (itemsGO.ContainsKey(id)) return  itemsGO[id];
        if (buildingsGO.ContainsKey(id)) return  buildingsGO[id];
        return null;
    }
    
    public Sprite GetIcon(RObj r, bool shard = false)
    {
        if (r.dynamic != null)
        {
            var g = UtilsControl.GetLowest(r.dynamic.id);
            if (dynamics.ContainsKey(g))
                return dynamics[g];
        }

        string ww = r.dbObj.ID;
        if (shard && r.shardID != "") ww = r.shardID;
        
        if (r.it == ItemType.item)
        {
            return items[ww];
        }
        else if (r.it == ItemType.monster)
        {
            return avas[ww];
        }
        else if (r.it == ItemType.projectile)
        {
            return skills[ww];
        }
        else if (r.it == ItemType.building)
        {
            return buildings[ww];
        }
        else if (r.it == ItemType.task)
        {
            return tasks[r.RID];
        }

        return null;
    }
    public void GetResult(UnoAll a, RObj r)
    {
        //prices ?
        if (a.param == "buy")
        {
            var gg = UpgradeSystem.instance.GetPrice(r, a.param);
            var oo = a.GetComponent<GBind>();
            var bb = MainStates.instance.HaveAmount(gg);
            oo.GetImage("icon").sprite = items[gg[0].Key];
            oo.GetText("price").text = (bb ? "" : "<color=red>") + gg[0].Value + (bb ? "" : "</color>");
            //a.transform.Find("icon").GetComponent<Image>().sprite = items[gg[0].Key];
            //a.transform.Find("icon/price").GetComponent<TextMeshProUGUI>().text = gg[0].Value.ToString();
            return;
        }
        else if (a.param == "sell")
        {
            var gg = UpgradeSystem.instance.GetPrice(r, a.param);
            var oo = a.GetComponent<GBind>();
            oo.GetImage("icon").sprite = items[gg[0].Key];
            oo.GetText("price").text = gg[0].Value.ToString();
            //a.transform.Find("icon").GetComponent<Image>().sprite = items[gg[0].Key];
            //a.transform.Find("icon/price").GetComponent<TextMeshProUGUI>().text = gg[0].Value.ToString();
            return;
        }
        else if (a.param == "upgrade")
        {
            var oo = a.GetComponent<GBind>();
            var gg = UpgradeSystem.instance.GetPrice(r, a.param);
            
            if (r.GetPar("max") == 1 || gg[0].Value < 0)
            {
                oo.GetComponent<CanvasGroup>().alpha = 0;
                oo.GetComponent<Button>().interactable = false;
                return;
            }
            else
            {
                oo.GetComponent<CanvasGroup>().alpha = 1;
                oo.GetComponent<Button>().interactable = true;
            }
            
            
            
            var bb = MainStates.instance.HaveAmount(gg);
            oo.GetImage("icon").sprite = items[gg[0].Key];
            oo.GetText("price").text = (bb ? "" : "<color=red>") + gg[0].Value + (bb ? "" : "</color>");        
            //a.transform.Find("icon").GetComponent<Image>().sprite = items[gg[0].Key];
            //a.transform.Find("icon/price").GetComponent<TextMeshProUGUI>().text = gg[0].Value.ToString();
            return;
        }

        if (a.isPar)
        {
            //fillstat
            var g = a.GetComponent<GBind>();
            if (g)
            {
                var rr = r.GetPar(a.param);
                if (g.HasKey("icon"))  g.GetImage("icon").sprite = pars[a.param];
                g.GetText("value").text = rr.ToString();
                if (a.hideEmpty)
                {
                    if (rr == 0)
                    {
                        a.GetComponent<CanvasGroup>().alpha = 0;
                        a.GetComponent<LayoutElement>().ignoreLayout = true;
                    }
                    else
                    {
                        a.GetComponent<CanvasGroup>().alpha = 1;
                        a.GetComponent<LayoutElement>().ignoreLayout = false;
                    }
                }
            }

            if (a.param == "health" || a.param == "mana" || a.param == "c_exp")
            {
                if (g)
                {
                    g.GetImage("fill").fillAmount = r.GetPar(a.param) / r.GetPar("max_" + a.param);
                }
                else
                {
                    a.GetComponent<Image>().fillAmount = r.GetPar(a.param) / r.GetPar("max_" + a.param);
                }
            }
            return;
        }
        
        if (a.GetComponent<Image>() != null)
        {
            var img = a.GetComponent<Image>();
            if (a.param == "id")
            {
                img.sprite = GetIcon(r);
            }
            if (a.param == "shard_id")
            {
                img.sprite = GetIcon(r, true);
                img.enabled = true;
            }
            else if (a.param == "cd")
            {
                img.fillAmount = r.GetPar("cd") / r.GetPar("cooldown");
            }
            else if (a.param == "rarity")
            {
                img.color = rareColors[(int)r.GetPar("rarity")];
            }
            else if (a.param == "rarity_frame")
            {
                img.sprite = rareFrame[(int)r.GetPar("rarity")];
            }
            else if (a.param == "slot")
            {
                if (!r.dbObj.pars.ContainsKey("slot") || r.dbObj.pars["slot"] < 0)
                {
                    img.color = Color.clear;
                }
                else
                {
                    img.color = Color.white;
                    img.sprite = slotFrame[(int)r.dbObj.pars["slot"]];
                }
            }
            else if (a.param == "exp")
            {
                MainStates.instance.GetMeExpPars(r, out float rat,out float cr, out float cm, out float lvl);
                img.fillAmount = rat;
            }
            else if (a.param2 != "")
            {
                img.fillAmount = r.GetPar(a.param) / r.GetPar(a.param2);
            }
        }
        else if (a.GetComponent<TextMeshProUGUI>() != null)
        {
            var txt = a.GetComponent<TextMeshProUGUI>();
            if (a.param == "id")
            {
                if (r.dynamic != null)
                {
                    txt.text = ConfigLoader.Instance.GetMeLocale(r.dynamic.id);
                }
                else if (r.dbObj != null)
                    txt.text = a.pref + ConfigLoader.Instance.GetMeLocale(r.dbObj.ID);
                else
                    txt.text = a.pref + ConfigLoader.Instance.GetMeLocale(r.RID);
            }
            else if (a.param == "description")
            {
                if (r.dynamic != null)
                {
                    txt.text = ConfigLoader.Instance.GetMeLocale(r.dynamic.id + "_descr");
                }
                else if (r.dbObj != null)
                {
                    txt.text = ConfigLoader.Instance.GetMeLocale(r.dbObj.ID + "_descr");
                }
                else
                    txt.text = ConfigLoader.Instance.GetMeLocale(r.RID + "_descr");
            }
            else if (a.param == "rarity")
            {
                txt.text = rareString[(int)r.GetPar("rarity")];
                txt.color = rareColors[(int)r.GetPar("rarity")];
            }
            else if (a.isStat)
            {
                if (a.param2 != "")
                {
                    txt.text = ModelStatistics.instance.GetStatValue(a.param) + "/" +
                               ModelStatistics.instance.GetStatValue(a.param2);
                }
                else
                    txt.text = ModelStatistics.instance.GetStatValue(a.param).ToString();
            }
            else if (a.param2 != "")
            {
                txt.text = a.pref + r.GetPar(a.param) + "/" + r.GetPar(a.param2);
            }
            else
            {
                var e = r.GetPar(a.param);
                if (!a.ignoreOnce && e == 1 && a.param == "amount") txt.text = "";
                else txt.text = a.pref + r.GetPar(a.param).ToString();
            }
        }

    }
}

[System.Serializable]
public class SkillEtc
{
    public GameObject effSelf;
    public List<string> conds = new List<string>();
    public GameObject projHit;
    public GameObject proj;
    public float projDelay;

    public string projHitMark = "";
    //public GameObject 
}