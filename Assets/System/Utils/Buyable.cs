using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Buyable : ComponentBehavior
{
    [Header("RuntimeGen")] 
    public bool addToDB = false;
    public bool runtimeGen = false;
    public List<Bon> prices;
    public string pref = "";
    public string param = "";
    public Action extra = null;
    public bool autoClose = false;
    //
        
    [Header("Common")]
    public string dynamicID = "";
    public string calculated = "";
    
    public Button btn;
    public TextMeshProUGUI text;

    public FPrice price;
    
    //other
    public Image toColor;
    public GameObject toActivate;
    public GameObject blockCondition;
    public FormatDynamic curDynamic;

    private bool isMax = false;
    private bool taken = false;
    [Header("Another pars")] 
    public Button take;
    public TextMeshProUGUI timeLeft;
    public Image fillLeft;
    private float timeLeftF = 0;
    public Transform endItem;

    public void OnEnable()
    {
        if (!ConfigLoader.parseEnded)
        {
            Invoke("OnEnable", 0.1f);
            return;
        }
        CheckGeneration();
    }

    private void Start()
    {
        if (!ConfigLoader.parseEnded)
        {
            Invoke("Start", 0.1f);
            return;
        }
        Do();
        if (btn == null) btn = GetComponentInChildren<Button>();
        if (btn) btn.onClick.AddListener(() =>
            {
                MainStates.instance.Buy(curDynamic.price, null,() =>
                {
                    //can be time
                    if (curDynamic.time > 0)
                    {
                        btn.interactable = false;
                        timeLeftF = curDynamic.time;
                        UtilsControl.Instance.StartCoroutine(FillDt());
                    }
                    else Fulfill();
                });
            }
        );

        if (take)
        {
            take.onClick.AddListener(() =>
            {
                Fulfill();
            });
        }

        if (endItem)
        {
            if (curDynamic.itemsGet.Count > 0)
            {
                endItem.GetComponent<Image>().sprite = ResourceHolder.instance.items[curDynamic.itemsGet[0].Key];
                endItem.GetChild(0).GetComponent<TextMeshProUGUI>().text = curDynamic.itemsGet[0].Value.ToString();
            }
        }
    }

    public IEnumerator FillDt()
    {
        while (timeLeftF > 0)
        {
            timeLeftF -= Time.deltaTime;
            if (timeLeftF < 0) timeLeftF = 0;
            if (fillLeft) fillLeft.fillAmount = (curDynamic.time - timeLeftF) / curDynamic.time;
            if (timeLeft) timeLeft.text = MathF.Ceiling(timeLeftF).ToString();
            yield return null;
        }
        
        if (take) take.gameObject.SetActive(true);
        else Fulfill();
    }

    public void Fulfill()
    {
        buyed = true;
          if (!taken) ModelStatistics.instance.TakeDynamic(calculated);
          MainStates.instance.ExecuteDone(curDynamic, whoActivate:gameObject, par:transform.parent);
          if (extra != null) extra();
          Do();
          if (autoClose) gameObject.SetActive(false);  
          taken = true;

          if (curDynamic.multi > 0)
          {
              buyed = false;
              taken = false;
              btn.interactable = true;
              if (fillLeft) fillLeft.fillAmount = 0;
              if (take) take.gameObject.SetActive(false);
          }
    }

    public void CheckGeneration()
    {
        if (addToDB)
        {
            dynamicID = curDynamic.id;
            ConfigLoader.Instance.allDynamic.Add(curDynamic.id, curDynamic);
        }
        
        if (runtimeGen && dynamicID == "")
        {
            dynamicID = pref + "_" + RandomStringGenerator.GenerateRandomString(5);
            var gg = new FormatDynamic();
            gg.id = dynamicID;
            gg.price = prices;
            ConfigLoader.Instance.allDynamic.Add(gg.id, gg);
        }
    }

    public void SetParams(  bool runtimeGen,
    List<Bon> prices,
    string pref = "",
    string param = "",
    Action extra = null,
    bool autoClose = false)
    {
        dynamicID = "";
        this.runtimeGen = runtimeGen;
        this.prices = prices;
        this.pref = pref;
        this.param = param;
        this.extra = extra;
        this.autoClose = autoClose;
    }
    
    public void AfterSet(string par)
    {
        dynamicID = pars["val"];
    }

    public void Recalculate()
    {
        calculated = MainStates.instance.CalculateValue(dynamicID);

        if (!ConfigLoader.Instance.allDynamic.ContainsKey(calculated))
        {
            isMax = true;
            var ho = UtilsControl.GetPrev(calculated);
            if (ConfigLoader.Instance.allDynamic.ContainsKey(ho))
            {
                //??
                curDynamic = ConfigLoader.Instance.allDynamic[ho];
                var tx = GetComponentInParent<ObjHolder>();
                if (tx != null)
                    tx.obj.dynamic = curDynamic;
            }
            
            if (toColor) toColor.color = Color.green;
            if (toActivate) toActivate.SetActive(true);
            if (curDynamic.multi == 0) price.gameObject.SetActive(false);
            if (text) text.text = dynamicID + "MAX";
            return;
        }
        
        curDynamic = ConfigLoader.Instance.allDynamic[calculated];
        //?
        var tt = GetComponentInParent<ObjHolder>();
        if (tt != null) tt.obj.dynamic = curDynamic;
        if (price) price.req = curDynamic.price;
        if (text) text.text = ConfigLoader.Instance.GetMeLocale(calculated);

    }
    
    


    public void Do()
    {
        Recalculate();
        //redo price
        
        //redo name
    }

    private bool triggered = false;
    private bool buyed = false;
    private float lastClickTime = -1;
    public void CheckCondition()
    {
        if (taken && curDynamic.multi <= 0) return;
        
        bool done = true;
        foreach (var v in curDynamic.conds1)
        {
            if (v.Key == "dst")
            {
                RObj unit = MainStates.instance.lastAllySelected == null ? MainStates.instance.mainPlayer :  MainStates.instance.lastAllySelected;
                var gg = MainStates.instance.GetDistance(transform.position, unit, out var dst);
                if (gg > v.Value) done = false;
            }
            else if (v.Key == "trigger")
            {
                if (!triggered) done = false;
            }
            else if (v.Key == "click")
            {
                if (Time.time - lastClickTime > 0.2f) done = false;
            }
            
        }

        if (done && curDynamic.conds1.Count > 0 && (!taken || curDynamic.multi > 0))
        {
            Fulfill();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        triggered = true;
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        triggered = true;
    }

    public void OnMouseUp()
    {
        lastClickTime = Time.time;
    }

    private void Update()
    {
        if (dynamicID.IndexOf("{") >= 0)
            Recalculate();
        
        CheckCondition();
        
        if (MainStates.instance.HaveDynamic(calculated))
        {
            Do();
            if (toColor) toColor.color = Color.green;
            if (toActivate) toActivate.SetActive(true);
            if (price && curDynamic.multi == 0) price.gameObject.SetActive(false);
        }
        else if (!isMax)
        {
            if (toColor) toColor.color = Color.orange;
            if (toActivate) toActivate.SetActive(false);
            if (price) price.gameObject.SetActive(true);
        }

        if (blockCondition != null && price != null)
        {
            if (blockCondition.activeSelf) price.gameObject.SetActive(false);
        }      
        
    }
}
