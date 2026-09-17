using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoNoninter : MonoBehaviour
{
    private RObj mon;
    ObjHolder holder;
    private void OnEnable()
    {
        holder = GetComponentInParent<ObjHolder>();
        holder.GetComponent<Button>().interactable = false;
    }


}
