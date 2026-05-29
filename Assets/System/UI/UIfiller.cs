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

    public GameObject spawn;

    public static List<UIfiller> instances = new List<UIfiller>();

    public UIfiller otherContext;

    public StringObjectDictionary context;
    public Button take;

    public bool deactivateOverCnt = false;
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
    public void OnEnable()
    {
        if (!ConfigLoader.parseEnded || !MainStates.instance.all.ContainsKey("main_player"))
        {
            Invoke("OnEnable", 0.1f);
            return;
        }
        
        //Debug.Log("~~~~~~" +nm);
        var res = MainStates.instance.GetCommandResult(command, param, transform);
        savedResult = res;
        if (slots.Count > 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var f = slots[i].GetComponentInChildren<ObjHolder>();
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
