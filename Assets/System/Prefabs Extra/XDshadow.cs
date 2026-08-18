using UnityEngine;

public class XDshadow : ComponentBehavior
{
    private RObj mon;
    private Transform head;
    private void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        head = mon.visMain.transform.Find("legs");
        if (head != null)
        {
            transform.SetParent(head);
            transform.localPosition = Vector3.zero;
        }
    }
}
