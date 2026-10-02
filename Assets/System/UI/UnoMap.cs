using System;
using UnityEngine;
using UnityEngine.UI;

public class UnoMap : MonoBehaviour
{
    public GameObject portal;


    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => UIMap.instance.Travel(portal));
    }
    private void Update()
    {
        var vv = UIMap.instance.GetCalc(portal);

        var g = GetComponent<RectTransform>();
        g.anchorMin = vv;
        g.anchorMax = vv;
    }
}
