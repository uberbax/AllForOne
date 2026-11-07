using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class XDdeath_stat : ComponentBehavior
{
    private RObj mon;
    private Transform head;

    public Image stat;
    public TextMeshProUGUI statText;

    public float dx = 0;
    public float dy = 0;
    
    private void Start()
    {
        var t0 = GetComponentInParent<ObjHolder>();
        if (t0 == null) return;
        mon = t0.obj;
        head = mon.visMain.transform.Find("head");

        if (head == null) 
            Debug.Log(transform.parent.name + " " + mon.deathStat.Key);
        
        stat.sprite = ResourceHolder.instance.pars[mon.deathStat.Key];
        statText.text = mon.deathStat.Value.ToString();
        stat.transform.position = head.position;
        stat.transform.localPosition += new Vector3(dx, dy, 0);
        transform.forward = Camera.main.transform.forward;

    }

    private void Update()
    {
        if (head == null) return;
        stat.transform.position = head.position;
        transform.forward = Camera.main.transform.forward;
        stat.transform.localPosition += new Vector3(dx, dy, 0);
        
    }
}
