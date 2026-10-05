using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorControl : MonoBehaviour
{
    public GameObject overlay;
    public GameObject hand;
    public GameObject bb;
    public TextMeshProUGUI header;
    public TextMeshProUGUI description;
    
    
    public static TutorControl instance;

    private void Awake()
    {
        instance = this;
    }

    public void DoTutor(List<UnoTutor> activate, string endStat, int cur = 0)
    {
        if (bb != null) DestroyImmediate(bb);
        
        
        //stat at start ?
        if (cur == 0)
        {
            ModelStatistics.instance.SetStatValue(endStat, 1);
        }
        
        if (cur >= activate.Count)
        {
            ModelStatistics.instance.SetStatValue(endStat, 1);
            overlay.SetActive(false);
            return;
        }

        if (activate[cur].header == "")
        {
            header.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            header.transform.parent.gameObject.SetActive(true);
            header.text = activate[cur].header;
            description.text = activate[cur].description;
        }
        
        overlay.SetActive(true);
        if (activate[cur].activate != null)
        {
            activate[cur].activate.gameObject.SetActive(true);
            overlay.GetComponent<Image>().color = Color.clear;
        }
        else
        {
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.9f);
        }

        if (activate[cur].copy != null)
        {
            hand.transform.position = activate[cur].copy.transform.position;
        }
        else
        {
            hand.transform.position = new Vector3(10000, 10000, 10000);
        }

        if (activate[cur].emptyHand)
        {
            hand.transform.GetChild(0).GetComponent<Image>().color = Color.clear;
        }
        else
        {
            hand.transform.GetChild(0).GetComponent<Image>().color = Color.white;
        }
        
        //ok time for copy

        if (activate[cur].copy != null)
        {
            var hh = activate[cur].copy.GetComponentInChildren<Button>();
            bb = Instantiate(activate[cur].copy, overlay.transform);
            hand.transform.SetAsLastSibling();
            bb.transform.position = activate[cur].copy.transform.position;
            var bn = bb.GetComponentInChildren<Button>();
            if (bn != null)
            {
                bn.onClick.RemoveAllListeners();
                bn.onClick.AddListener(() =>
                    {
                        if (hh != null) hh.onClick.Invoke();
                        DoTutor(activate, endStat, cur + 1);
                    }
                );
            }
        }
        //wait gone

        if (activate[cur].waitGone)
        {
            FunctionTimer.Create( () => DoTutor(activate, endStat, cur + 1), 0, () => !activate[cur].activate.activeSelf);
        }

    }
    
    
    
}

[System.Serializable]
public class UnoTutor
{
    public GameObject activate;
    public GameObject copy;
    public bool emptyHand = false;
    public bool waitGone = false;
    public bool justDeactivate = false;

    public string header = "";
    public string description = "";
}