using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EventTrigger : MonoBehaviour
{
    public string keyCode = "";
    public string evtName = "";
    public ArgPass arg;

    public bool is3D = false;
    public bool isMouseDown = false;
    
    //dynamic
    public FormatDynamic dyno;
    public List<GameObject> reverts = new List<GameObject>();
    public List<GameObject> toActivate = new List<GameObject>();
    public List<GameObject> toDeactivate = new List<GameObject>();
    
    public string soundTrigger = "";

    public bool paramAsHolder = false;
    
    void Start()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                string bb = arg.what;
                if (paramAsHolder)
                    bb = GetComponentInParent<AbsHolder>().id;
                arg.what = bb;
                    
                EventManager.INV(evtName, arg);
                if (dyno.id != "")
                {
                    MainStates.instance.ExecuteDone(dyno);
                }
                HandleReverts();
            });
        }

        if (soundTrigger != "")
        {
            SoundManager.instance.PlayAny(soundTrigger);
        }
    }

    public void HandleReverts()
    {
        foreach (var revert in reverts) revert.SetActive(!revert.activeSelf);
        foreach (var revert in toActivate) revert.SetActive(true);
        foreach (var revert in toDeactivate) revert.SetActive(false);
    }

    private void OnMouseUp()
    {
        if (is3D || isMouseDown)
        {
            EventManager.INV(evtName, arg);
            if (dyno.id != "")
            {
                MainStates.instance.ExecuteDone(dyno);
            }
            HandleReverts();
        }
    }

    private void Update()
    {
        if (keyCode != "")
        {
            if (Input.GetKeyDown(keyCode))
            {
                EventManager.INV(evtName, arg);
                if (dyno.id != "")
                {
                    MainStates.instance.ExecuteDone(dyno);
                }
                HandleReverts();
            }
        }
    }
}
