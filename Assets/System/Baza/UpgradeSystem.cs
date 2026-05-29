using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeSystem : MonoBehaviour
{
    // Start is called before the first frame update
    public static UpgradeSystem instance;

    private void Awake()
    {
        instance = this;
    }
    //
    public List<Bon> GetPrice(RObj who, string what, string what2 = "")
    {
        if (who.dynamic != null)
        {
            return who.dynamic.price;
        }

        return who.dbObj.price;
        return new List<Bon> { new Bon{Key = "gold", Value = 100} };
    }

    //UNIVERSAL FUNCTION
    public void UpgradeSomething(RObj who, string what, Action onSuccess, Action onFail)
    {
        var p = GetPrice(who, what);
        var g = MainStates.instance.HaveAmount(p);

        if (g)
        {
            
        }
        else
        {
            if (onFail != null)
                onFail();
        }
    }
    
}
