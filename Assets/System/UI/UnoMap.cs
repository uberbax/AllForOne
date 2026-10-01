using System;
using UnityEngine;

public class UnoMap : MonoBehaviour
{
    public GameObject portal;

    private void Update()
    {
        var vv = UIMap.instance.GetCalc(portal);

        var g = GetComponent<RectTransform>();
        g.anchorMin = vv;
        g.anchorMax = vv;
    }
}
