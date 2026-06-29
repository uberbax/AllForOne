using System;
using System.Collections.Generic;
using UnityEngine;

public class UIDraw : MonoBehaviour
{
    public string whatDraw = "itm_draw1";
    public UIfiller filler;

    public bool convertShards = false;
    private void OnEnable()
    {
        filler.selfReward = new List<Bon>();
        filler.OnEnable();
    }

    void Start()
    {
        EventManager.SUB(whatDraw, (x) =>
        {
            var res = ModelSet.GetMeItemsBon(x.what, x.num);
            //we can prepare res if its shards

            if (convertShards)
            {
                for (int i = 0; i < res.Count; i++)
                {
                    var b = MainStates.instance.mainPlayer.inventory.Find(x => x.dbObj.ID == res[i].Key);
                    if (b != null)
                    {
                        res[i].Key = "shard_" + res[i].Key;
                        res[i].Value = MainStates.rarityShards[(int)b.GetPar("rarity")];
                    }
                    MainStates.instance.AddItems(new List<Bon>{res[i]});
                }
            }
            else
            {
                MainStates.instance.AddItems(res);                
            }

            
            filler.selfReward = res;
            filler.OnEnable();
            
        });
    }


}
