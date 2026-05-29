using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotifyControl : MonoBehaviour
{
    public static NotifyControl instance;

    public List<UnoNotify> notifies = new List<UnoNotify>();
    //flags
    public DerFlags allFlags;


    public List<string> talents = new List<string>();
    private RObj player;
    
    private void Awake()
    {
        instance = this;
        player = MainStates.instance.mainPlayer;
    }
    
    private void OnDestroy()
    {
        instance = null;
    }

    public void Add(UnoNotify uno)
    {
        notifies.Add(uno);
    }

    public void CalculateFlags()
    {
        var c = MainStates.instance.GetItemsCount("gem");
        allFlags.hasMore100gems = c >= 100;

        bool s = false;
        foreach (var v in MainStates.instance.mainPlayer.inventory)
        {
            //if (v.pMonster == null) continue;
            var mon = v.owner;
            //levelup
            var y = MainStates.instance.IsItemLevelapable(v);
            //var price = ConfigConfig.GetExpPotionNeededToLevelup(mon.level);
            //var y = MainStates.instance.inventory.HaveAmount(price);
            if (y) s = y;
        }
        allFlags.canLevelupAny = s;
        
        s = false;
        foreach (var v in MainStates.instance.mainPlayer.inventory)
        {
            if (v.it != ItemType.item) continue;
            //levelup
            var y = MainStates.instance.IsItemLevelapable(v);

            if (y) s = y;
        }
        allFlags.canLevelUpAnyGear = s;
        
        s = false;
        foreach (var v in MainStates.instance.mainPlayer.inventory)
        {
            if (v.owner == null) continue;
            if (v.it != ItemType.monster) continue;
            //levelup
            var y = MainStates.instance.IsItemLevelapable(v);

            if (y) s = y;
        }
        allFlags.canLevelUpAnyMonster = s;

        var nn = MainStates.instance.GetItemsCount("summon_egg");
        allFlags.canSummonAny = nn > 0;
        
        s = false;
        foreach (var v in MainStates.instance.mainPlayer.inventory)
        {
            if (v.it != ItemType.item) continue;
            //levelup
            var y = MainStates.instance.IsItemMergable(v);

            if (y) s = y;
        }
        allFlags.canMergeAny = s;
        
        
        s = false;
        //stats list ? attack, health, def ?
        List<string> checkStats = new List<string> { "health","attack","def" };
        foreach (var v in checkStats)
        {
            var gg = UpgradeSystem.instance.GetPrice(MainStates.instance.mainPlayer, "upgrade", v);
            var q = MainStates.instance.HaveAmount(gg);
            if (q) s = true;
        }
        
        allFlags.canUpgradeStatAny = s;


        //what is talent ? passive skill ? player stat ? 
        /*
        s = false;
        foreach (var v in talents)
        {
            var c1 = player.GetMaxTalentTaken(v);
            var j1 = ConfigConfig.GetTalentLevelupCost(c1);
            if (MainStates.instance.HaveAmount(j1))
            {
                s = true;
                break;
            }
        }

        allFlags.canAnyTalentUpgrade = s;
        */
    }
    
    
    //public bool canLevelUpAnyGear;
    //public bool canMergeAny;
    //public bool canSummonAny;
    //public bool canLevelUpAnyMonster;
    //public bool canAnyTalentUpgrade;
    
    private void Update()
    {
        CalculateFlags();

        foreach (var v in notifies)
        {
            if (v == null) continue;
            
            var b = (v.flags.hasMore100gems && allFlags.hasMore100gems) ||
                    (v.flags.canLevelupAny && allFlags.canLevelupAny) ||
                    (v.flags.canUpgradeStatAny && allFlags.canUpgradeStatAny) ||
                    
                    (v.flags.canLevelUpAnyGear && allFlags.canLevelUpAnyGear) ||
                    (v.flags.canMergeAny && allFlags.canMergeAny) ||
                    (v.flags.canSummonAny && allFlags.canSummonAny) ||
                    (v.flags.canLevelUpAnyMonster && allFlags.canLevelUpAnyMonster) ||
                    (v.flags.canAnyTalentUpgrade && allFlags.canAnyTalentUpgrade)
                    
                    ;

            if (!b && v.flags.canLevelupSelf && MainStates.instance.curState == "nope")
            {
                RObj vv = null;
                vv = v.GetComponentInParent<ObjHolder>().obj;
                
                if (vv != null && vv.it == ItemType.monster)
                {
                    var y = MainStates.instance.IsItemLevelapable(vv);
                    b = y;
                }
            }
            //same for stat
            /*
            if (!b && v.flags.canUpgradeStateSelf && MainStates.instance.curState == "nope")
            {
                ViewItem vv = null;
                if (v.lookUp) vv = v.GetComponentInParent<ViewItem>(true);
                else vv = v.GetComponentInChildren<ViewItem>(true);
                
                if (vv.item != null && vv.item.pMonster != null)
                {
                    var mon = vv.item.pMonster;
                    //levelup
                    var price = ConfigConfig.GetMatsStatToLevelup(0, mon);
                    UpgradeSystem.instance.GetPrice(r, a.param);
                    var y = MainStates.instance.HaveAmount(price);
                    b = y;
                }
            }
            */
            
            //there can be options
            if (v.flags.isCustom)
            {
                v.DoCustom(v.CalculateCustom());
            }
            else
                v.gameObject.SetActive(b);
        }

        notifies.RemoveAll(x => x == null);
    }
}

[System.Serializable]
public class DerFlags
{
    public bool hasMore100gems;
    public bool canLevelupAny;
    
    public bool canLevelUpAnyGear;
    public bool canMergeAny;
    public bool canSummonAny;
    public bool canLevelUpAnyMonster;
    public bool canAnyTalentUpgrade;
    
    public bool canUpgradeStatAny;

    public bool canUpgradeStateSelf;
    public bool canLevelupSelf;

    public bool isCustom;
}