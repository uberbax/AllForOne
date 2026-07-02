using System;
using System.Collections;
using System.Collections.Generic;
using GameDevWare.Dynamic.Expressions.CSharp;
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

        if (what == "upgrade")
        {
            List<Bon> res = new List<Bon>();
            var str = ConfigLoader.GetMetaParamValueString("upgrade_cost");
            var s1 = str.Split('#');
            foreach (var item in s1)
            {
                var s2 = item.Split(',');
                var s0 = s2[1].Replace("{level}", who.GetPar("level").ToString());
                var v1 = CSharpExpression.Evaluate<int>(s0);
                res.Add(new Bon{Key = s2[0], Value = v1});
            }
            
            return res;
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
