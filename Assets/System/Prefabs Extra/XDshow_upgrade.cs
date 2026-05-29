using System;
using System.Collections.Generic;
using UnityEngine;

public class XDshow_upgrade : ComponentBehavior
{
    public List<GameObject> upgrades;
    private Buyable town;
    
    private void Start()
    {
        town = transform.parent.GetComponentInChildren<Buyable>();
    }

    private void Update()
    {
        var t = UtilsControl.GetNum(town.calculated);
        if (t > 0)
        {
            for (int k = 1; k <= t; k++)
                upgrades[k-1].SetActive(true);
        }
    }
}
