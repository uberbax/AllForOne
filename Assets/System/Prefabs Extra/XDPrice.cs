using TMPro;
using UnityEngine;

public class XDPrice : ComponentBehavior
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI txt;

    private RObj mon;

    void Start()
    {
        mon = GetComponentInParent<RObj>();
    }

    void Update()
    {
        txt.text = mon != null ? mon.GetPar("gold").ToString() : "0";
    }
}
