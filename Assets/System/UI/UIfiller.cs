using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIfiller : MonoBehaviour
{
    public bool noScale = true;
    public bool ignoreInvAny = true;
    
    public string nm = "";
    // Start is called before the first frame update

    public string compatibility = "";
    public string command = "";
    public string param = "";
    
    public string clickFunc = "";
    public string fillFunc = "";
    public string replaceClick = "";
    

    public List<Transform> slots = new List<Transform>();
    public Transform root;
    public bool asGrid = false;
    
    public GameObject spawn;

    public static List<UIfiller> instances = new List<UIfiller>();

    public UIfiller otherContext;

    public StringObjectDictionary context;
    public Button take;

    public bool deactivateOverCnt = false;

    [Header("Subs for events")] 
    public string subParamChange = "";
    
    
    private void Awake()
    {
        instances.Add(this);
    }

    public static void GlobalRefresh()
    {
        foreach (var v in instances)
        {
            if (v.gameObject.activeInHierarchy)
                v.OnEnable();
        }
    }
    
    private List<RObj> savedResult = new List<RObj>();
    public List<Bon> selfReward = new List<Bon>();

    public bool findRobj = false;
    private void Start()
    {
        if (subParamChange != "")
        {
            EventManager.SUB(subParamChange, (x) =>
            {
                if (x.what != "")
                {
                    param = x.what;
                    OnEnable();
                }
            });
        }
    }

    public void OnEnable()
    {
        if (!ConfigLoader.parseEnded || !MainStates.instance.all.ContainsKey("main_player"))
        {
            Invoke("OnEnable", 0.1f);
            return;
        }
        
        //Debug.Log("~~~~~~" +nm);
        RObj rr = null;
        if (findRobj)
        {
            rr = GetComponentInParent<ObjHolder>().obj;
            var t0 = (int)rr.GetPar("adorn_count");
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(i < t0);
            }
        }

        var res = MainStates.instance.GetCommandResult(command, param, transform, rr:rr);
        savedResult = res;
        if (slots.Count > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var f = slots[i].GetComponentInChildren<ObjHolder>();
                if (i >= res.Count)
                {
                    f.gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    f.gameObject.SetActive(true);
                }
                
                if (f != null)
                {
                    f.obj = res[i];
                    //zdes
                    var cc = f.GetComponentsInChildren<UnoAll>();
                    for (int l = 0; l < cc.Length; l++) cc[l].mon = null;

                }
            }
        }

        if (root != null)
        {
            if (asGrid)
            {
                for (int i = res.Count; i < root.childCount; i++)
                {
                    root.GetChild(i).gameObject.SetActive(false);
                }

                var d = res.Count - root.childCount;
                for (int i = 0; i < d; i++)
                {
                    var a = GameObject.Instantiate(spawn, root);
                }
                
                for (int i = 0; i < res.Count; i++)
                {
                    var a = root.GetChild(i).gameObject;
                    var b1 = a.GetComponent<AbsHolder>();
                    if (b1 != null)
                    {
                        b1.id = res[i].RID;
                        a.transform.SetAsLastSibling();
                        continue;
                    }
                        
                    a.GetComponent<ObjHolder>().obj = res[i];
                    a.GetComponent<ObjHolder>().noTrack = true;
                    a.GetComponent<ObjHolder>().filler = this;
                    a.GetComponent<ObjHolder>().OnEnable();
                    if (res[i] == null)
                        a.GetComponentInChildren<CanvasGroup>().alpha = 0;
                    //a.transform.SetAsLastSibling();
                }
            }
            else
            {
                for (int i = 0; i < root.childCount; i++)
            {
                var jj = root.GetChild(i);
                if (deactivateOverCnt)
                {
                    jj.gameObject.SetActive(i < res.Count);
                }                
                
                if (i >= res.Count)
                {
                    var g = jj.GetComponentInChildren<ObjHolder>();
                    var g1 = jj.GetComponentInChildren<AbsHolder>();
                    if (g1 != null)
                    {
                        g1.id = res[i].RID;
                        g1.Start();
                        continue;
                    }
                    
                    if (g != null)
                    {
                        g.filler = this;
                        g.obj = null;
                        g.GetComponent<CanvasGroup>().alpha = 0;
                        g.GetComponent<CanvasGroup>().blocksRaycasts = false;
                        
                        //var kk = g.GetComponent<DragObject>();
                        //if (kk != null) kk.enabled = false;
                    }
                }
                else
                {
                    var g = jj.GetComponentInChildren<ObjHolder>();
                    var g1 = jj.GetComponentInChildren<AbsHolder>();
                    if (g1 != null)
                    {
                        g1.id = res[i].RID;
                        g1.Start();
                        continue;
                    }
                    
                    if (g != null)
                    {
                        g.filler = this;
                        g.GetComponent<CanvasGroup>().alpha = res[i] != null ? 1 : 0;
                        g.GetComponent<CanvasGroup>().blocksRaycasts = res[i] != null;
                        g.obj = res[i];
                        //zdes
                        var cc = g.GetComponentsInChildren<UnoAll>();
                        for (int l = 0; l < cc.Length; l++) cc[l].mon = null;                        
                    }
                    else
                    {
                        var a = GameObject.Instantiate(spawn, jj);

                        var b1 = a.GetComponent<AbsHolder>();
                        if (b1 != null)
                        {
                            b1.id = res[i].RID;
                            a.transform.SetAsLastSibling();
                            continue;
                        }
                        
                        a.GetComponent<ObjHolder>().obj = res[i];
                        a.GetComponent<ObjHolder>().noTrack = true;
                        a.GetComponent<ObjHolder>().filler = this;
                        a.GetComponent<ObjHolder>().OnEnable();
                        if (res[i] == null)
                            a.GetComponentInChildren<CanvasGroup>().alpha = 0;
                        a.transform.SetAsLastSibling();

                    }
                }
            }                
            }

        }

        if (take != null)
        {
            take.onClick.AddListener(() =>
            {
                MainStates.instance.AddItems(savedResult);
            });
        }
        ActivateContext();
        UISystem.instance.Fill(this);
    }

    public void ActivateContext()
    {
        
        if (otherContext == null) return;        
        
        foreach (var v in context)
        {
            v.Value.SetActive(false);
        }

        var g = otherContext.clickFunc.Split(',');
        for (int i = 0; i < g.Length; i++)
        {
            if (context.ContainsKey(g[i]))
                context[g[i]].SetActive(true);
        }

    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }
}
