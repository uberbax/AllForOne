using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjHolder : MonoBehaviour, IReceive
{
    public GameObject deactIfNotNull;
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

    public CanvasGroup equipBtn;
    public CanvasGroup slotBtn;
    public CanvasGroup level;

    private bool isRectTransform;
    Transform _cachedTransform;
    private RectTransform _cachedRectTransform;


    public bool miscShit = false;
    public void OnEnable()
    {
        _cachedTransform = transform;
        _cachedRectTransform = _cachedTransform as RectTransform;
        isRectTransform = _cachedRectTransform != null;
        if (UISystem.instance == null) return;
        UISystem.instance.FillItem(this);
        foreach (var a in alsoEnables)
        {
            a.SendMessage("OnEnable", SendMessageOptions.DontRequireReceiver);
        }
        //?
        UpdateState(true);

        var hh = GetComponentsInChildren<Vizualo>(true);
        for (int i = 0; i < hh.Length; i++)
        {
            hh[i].Start();
            hh[i].Updateo();
        }
        
        //miscshit
        if (redirect || copyTo != null || asCurObj != "" || asObj != "" || asDBObj != "" || asMain ||
            deactIfNotNull != null)
        {
            miscShit = true;
        }
    }

    [ContextMenu("ShowViz")]
    public void ShowViz()
    {
        Debug.Log(obj);
        Debug.Log(obj.dbObj.ID);
        Debug.Log(obj.RID);
        if (obj.owner != null) Debug.Log(obj.owner.RID);
        
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

    private void Update() => UpdateState(false);

    private void UpdateState(bool refreshBindings)
    {
        if (miscShit || isRectTransform)
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
                if (!MainStates.instance.curObjs.TryGetValue(asCurObj, out var selected)) return;
                obj = selected;
            }

            if (asObj != "")
            {
                if (!MainStates.instance.all.TryGetValue(asObj, out var selected)) return;
                obj = selected;
            }

            if (asDBObj != "" && obj == null)
            {
                if (!DatabaseAll.instance.heroes.ContainsKey(asDBObj)) return;
                obj = DatabaseAll.instance.CreateAny(asDBObj, false, 1, new GameObject());
            }

            if (asMain)
            {
                if (MainStates.instance.HasMain() && obj == null)
                {
                    obj = MainStates.instance.mainPlayer;
                }
            }

            if (deactIfNotNull != null)
            {
                bool active = obj == null || obj.RID == "";
                if (deactIfNotNull.activeSelf != active) deactIfNotNull.SetActive(active);
            }
        }

        if (obj == null || obj.RID == "") return;

        if (isRectTransform &&  (equipBtn != null || slotBtn != null || level != null))
        {
            float subtype = obj.dbObj.pars["subtype"];
            float alpha = subtype != MainStates.subtypes["adorn"] &&
                subtype != MainStates.subtypes["skill"] &&
                subtype != MainStates.subtypes["potion"] &&
                subtype != MainStates.subtypes["none"] ? 1f : 0f;
            SetAlpha(equipBtn, alpha);
            SetAlpha(slotBtn, alpha);
            SetAlpha(level, alpha);
        }

        if (!noTrack && ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.auto_track_pos) > 0)
        {
            obj.Position = _cachedTransform.position;    
        }

        //basically ui ?
        if (noTrack && !inDrag && filler && !filler.noScale && _cachedRectTransform != null)
        {
            var sx = obj.dbObj.sizeX;
            var sy = obj.dbObj.sizeY;

            var offsetMax = new Vector2(0, 100 * (sx - 1));
            var offsetMin = new Vector2(100 * (sy - 1), 0);
            if (_cachedRectTransform.offsetMax != offsetMax) _cachedRectTransform.offsetMax = offsetMax;
            if (_cachedRectTransform.offsetMin != offsetMin) _cachedRectTransform.offsetMin = offsetMin;
            
        }
        
        // UnoAll has its own Unity Update. Refresh synchronously only after OnEnable binds the object.
        if (isRectTransform && refreshBindings && _cachedRectTransform != null)
        {
            var bindings = GetComponentsInChildren<UnoAll>();
            for (int i = 0; i < bindings.Length; i++)
                if (bindings[i].isActiveAndEnabled) bindings[i].Update();
        }
    }

    private static void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null && group.alpha != alpha) group.alpha = alpha;
    }

    public Action<RObj> onDestroy;
    public void OnDestroy()
    {
        if (noTrack) return;
        if (obj == null) return;
        if (onDestroy != null)
        {
            onDestroy(obj);
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
