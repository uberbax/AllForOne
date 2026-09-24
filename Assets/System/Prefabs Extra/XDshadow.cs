using UnityEngine;

public class XDshadow : ComponentBehavior
{
    private RObj mon;
    private Transform head;
    private void Start()
    {
        var t0 = GetComponentInParent<ObjHolder>();
        if (t0 == null) return;
        mon = t0.obj;
        head = mon.visMain.transform.Find("legs");

        var s1 = mon.visMain.transform.Find("Shadow");
        var s2 = mon.visMain.transform.Find("shadow");

        if (s1 != null || s2 != null)
        {
            Destroy(gameObject);
            return;
        }
        
        if (head != null)
        {
            transform.SetParent(head);
            transform.localPosition = Vector3.zero;
        }
    }
}
