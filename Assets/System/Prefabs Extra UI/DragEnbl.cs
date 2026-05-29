using System;
using UnityEngine;

public class DragEnbl : MonoBehaviour
{
    public GameObject root;

    private void OnEnable()
    {
        root.GetComponent<DragObject>().enabled = true;
    }

    private void Update()
    {
        var h = root.GetComponent<ObjHolder>();
        root.GetComponent<DragObject>().enabled = (h.obj != null);
    }
}
