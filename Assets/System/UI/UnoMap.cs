using System;
using UnityEngine;
using UnityEngine.UI;

public class UnoMap : MonoBehaviour
{
    public GameObject portal;

    private Button btn;
    void Start()
    {
        btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => UIMap.instance.Travel(portal));
    }
    private void Update()
    {
        var vv = UIMap.instance.GetCalc(portal);

        var g = GetComponent<RectTransform>();
        g.anchorMin = vv;
        g.anchorMax = vv;
    }
}
