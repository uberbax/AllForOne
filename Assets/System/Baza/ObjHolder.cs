using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjHolder : MonoBehaviour, IReceive
{
    public RObj obj;
    public GameObject attachedVeh;

    public UIfiller filler;
    public bool noTrack = false;
    
    public bool inDrag = false;
    
    public bool asMain = false;
    public string asCurObj = "";
    public string asObj = "";
    public string asDBObj = "";
    
    
    public ObjHolder redirect;
    public ObjHolder copyTo;
    
    public List<GameObject> alsoEnables = new List<GameObject>();
    public void OnEnable()
    {
        UISystem.instance.FillItem(this);
        foreach (var a in alsoEnables)
        {
            a.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        }
    }

    [ContextMenu("ShowViz")]
    public void ShowViz()
    {
        Debug.Log(obj);
        Debug.Log(obj.dbObj.ID);
        foreach (var v in obj.visuals)
        {
            Debug.Log(v);
        }
    }
    
    [ContextMenu("ShowPars")]
    public void ShowPars()
    {
        Debug.Log(obj);
        Debug.Log(obj.dbObj.ID);
        foreach (var v in obj.curPars)
        {
            Debug.Log(v);
        }
        Debug.Log("index: " + obj.index);
        Debug.Log("used slot: " + obj.GetPar("used_slot"));
    }

    private void Update()
    {
        if (obj == null && redirect)
        {
            obj = redirect.obj;
            return;
        }

        if (copyTo != null)
        {
            copyTo.obj = obj;
        }

        if (asCurObj != "")
        {
            if (!MainStates.instance.curObjs.ContainsKey(asCurObj)) return;
            obj = MainStates.instance.curObjs[asCurObj];
        }
        
        if (asObj != "")
        {
            if (!MainStates.instance.all.ContainsKey(asObj)) return;
            obj = MainStates.instance.all[asObj];
        }
        
        if (asDBObj != "" && obj == null)
        {
            if (!DatabaseAll.instance.heroes.ContainsKey(asDBObj)) return;
            obj = DatabaseAll.instance.CreateAny(asDBObj, false, 1, new GameObject());
        }
        
        if (asMain)
        {
            if (MainStates.instance.mainPlayer != null && obj == null)
            {
                obj = MainStates.instance.mainPlayer;
            }
        }
        
        if (obj == null || obj.RID == "") return;
        
        if (ConfigLoader.GetMetaParamValue("auto_track_pos") > 0 && !noTrack)
        {
            obj.Position = transform.position;    
        }
        else
        {
            
        }
        
        //basically ui ?
        if (noTrack && !inDrag && filler && !filler.noScale)
        {
            var sx = obj.dbObj.sizeX;
            var sy = obj.dbObj.sizeY;

            GetComponent<RectTransform>().offsetMax = new Vector2(0, 100 * (sx - 1));
            GetComponent<RectTransform>().offsetMin = new Vector2(100 * (sy - 1), 0);
            
        }

    }
    
    
    
    
    
    
    
    
    
    //trash
    public bool ignoreScale = false;
    public void DoAnim(string anim)
    {
        
    }

    public void SetFlipScale(float val)
    {
        
    }

    public void OnDisable()
    {
        //Debug.Log("TTTT " + gameObject.name);
    }

    public void Receive(ArgPass arg)
    {
        obj = arg.who;
    }
}
