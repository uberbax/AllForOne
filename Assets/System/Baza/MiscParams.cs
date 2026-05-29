using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MiscParams : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public List<string> pars = new List<string>();
    public List<string> ini = new List<string>();

    public Dictionary<string, GameObject> gg = new Dictionary<string, GameObject>();

    public bool iniAdd = false;
    private void Start()
    {
        if (iniAdd)
        {
            foreach (var v in ini)
            {
                Add(v);
            }
        }
    }

    public static void Addso(GameObject who, string what)
    {
        var zz = who.GetOrAddComponent<MiscParams>();
        zz.Add(what);
    }
    
    public static void Removeso(GameObject who, string what)
    {
        var zz = who.GetOrAddComponent<MiscParams>();
        zz.Remove(what);
    }
    
    
    public void Add(string what)
    {
        if (pars.Contains(what)) return;
        pars.Add(what);
        
        if (gg.ContainsKey(what)) return;
        
        if (!ResourceHolder.instance.XD.ContainsKey(what)) return;
        
        var nn = Instantiate(ResourceHolder.instance.XD[what], transform);
        nn.transform.localPosition = Vector3.zero;
        gg.Add(what, nn);
    }

    public void Remove(string what)
    {
        pars.Remove(what);
        if (gg.ContainsKey(what))
        {
            Destroy(gg[what]);
            gg.Remove(what);
        }
        
    }

    public bool Has(string what)
    {
        return pars.Contains(what);
    }
}
