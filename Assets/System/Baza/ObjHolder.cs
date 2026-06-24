using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjHolder : MonoBehaviour
{
    public RObj obj;
    public GameObject attachedVeh;

    public UIfiller filler;
    public bool noTrack = false;
    
    public bool inDrag = false;
    public void OnEnable()
    {
        UISystem.instance.FillItem(this);
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
    }

    private void Update()
    {
        if (obj == null || obj.RID == "") return;
        
        if (ConfigLoader.GetMetaParamValue("auto_track_pos") > 0 && !noTrack)
        {
            obj.Position = transform.position;    
        }
        else
        {
            
        }
        
        //basically ui ?
        if (noTrack && !inDrag && !filler.noScale)
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
        Debug.Log("TTTT");
    }
}
