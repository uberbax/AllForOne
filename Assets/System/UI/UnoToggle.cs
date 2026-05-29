using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnoToggle : MonoBehaviour
{
    private static Dictionary<string, List<UnoToggle>> toggles = new Dictionary<string, List<UnoToggle>>(); 
    //true false
    public bool state = false;
    public string groupName = "group1";
    
    public string toggleKey = "0";
    public int toggleVal = 0;

    public GameObject view;
    public List<GameObject> additionalObjs;

    public GameObject deactState;
    public GameObject actState;

    [Header("Size change")] 
    public Sprite toChangeEnabled;
    public Sprite toChangeDisabled;
    public Color enabledColor = Color.white;
    public Color disabledColor = Color.white;
    public Vector2 disabledSize;
    public Vector2 enabledSize;
    
    private void Awake()
    {
        if (toggles.ContainsKey(groupName))
        {
            toggles[groupName].Add(this);
        }
        else
        {
            toggles.Add(groupName, new List<UnoToggle>{this});
        }
        
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Activate(true);
        });

        if (state)
        {
            Activate(true);
        }
    }

    public void Activate(bool val)
    {
        
        if (this == null)
            return;
            
        if (!val)
        {
            if (actState)
            {
                actState.SetActive(false);
            }
            if (deactState) 
                deactState.SetActive(true);
            
            if (view)
                view.SetActive(false);
            
            foreach (var v in additionalObjs)
                v.SetActive(false);

            state = false;

            if (disabledSize != Vector2.zero)
            {
                GetComponent<RectTransform>().sizeDelta = disabledSize;
            }

            if (toChangeDisabled != null)
            {
                GetComponent<Image>().sprite = toChangeDisabled;
                GetComponent<Image>().color = disabledColor;
            }
        }
        else
        {
            EventManager.INV(toggleKey, new ArgPass { num = toggleVal});            
            var gg = toggles[groupName];
            foreach (var v in gg)
            {
                v.Activate(false);
            }
            //
            if (actState) actState.SetActive(true);
            if (deactState) deactState.SetActive(false);
            
            if (view) 
                view.SetActive(true);
            
            foreach (var v in additionalObjs)
                v.SetActive(true);

            state = true;
            
            if (enabledSize != Vector2.zero)
            {
                GetComponent<RectTransform>().sizeDelta = enabledSize;
            }

            if (toChangeEnabled != null)
            {
                GetComponent<Image>().sprite = toChangeEnabled;
                GetComponent<Image>().color = enabledColor;
            }
        }
    }

    public void OnDestroy()
    {
        toggles.Clear();
    }
}
