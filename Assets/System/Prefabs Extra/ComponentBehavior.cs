using System.Collections.Generic;
using UnityEngine;

public class ComponentBehavior : MonoBehaviour
{
    public Dictionary<string, string> pars = new Dictionary<string, string>();
    public void Set(string par)
    {
        //a:v,b:c,v:n
        var t0 = par.Split(',');
        foreach (var t1 in t0)
        {
            var t2 = t1.Split(':');
            pars.Add(t2[0], t2[1]);
        }
    }
}
