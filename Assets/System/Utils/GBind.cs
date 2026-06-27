using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GBind : MonoBehaviour
{
    //we need amount actually
    public StringObjectDictionary m_buttons;
    public StringObjectDictionary m_texts;
    public StringObjectDictionary m_images;
    public StringObjectDictionary m_gameObjects;
    public StringObjectDictionary m_combRoots;
    //progressbars
    public StringObjectDictionary m_progressbars;
    public StringObjectDictionary m_binds;
    //toggles
    public StringObjectDictionary m_toggles;
    
    [Header("3xDDD")]
    public StringObjectDictionary m3_texts;
    public StringObjectDictionary m3_images;

    [Header("Generated assignments")] 
    [TextArea(10, 20)]
    public string geno;

    [ContextMenu("GetGenerated")]
    public void GetMeGenerated()
    {
        geno += "//buttons\n";
        foreach (var v in m_buttons)
        {
            geno += "bnd.GetButton(\"" + v.Key +
                    "\").onClick.AddListener( ()=> EventManager.XXXXXX.Invoke(new ArgPass{num = tmp}));\n";
        }
        //
        geno += "//texts\n";
        foreach (var v in m_texts)
        {
            geno += "bnd.GetText(\"" + v.Key +
                    "\").text = description;\n";
        }
        //
        geno += "//images\n";
        foreach (var v in m_images)
        {
            geno += "bnd.GetImage(\"" + v.Key +
                    "\").sprite = images;\n";
        }
        //
        geno += "//gameobjects\n";
        foreach (var v in m_gameObjects)
        {
            geno += "bnd.GetGameobject(\"" + v.Key +
                    "\").SetActive(condition);\n";
        }
        //
        geno += "//progressbars\n";
        foreach (var v in m_progressbars)
        {
            geno += "bnd.SetProgressbar(\"" + v.Key +
                    "\",1);\n";
        }
        //
        
    }
    //override for statistics
    public Button GetButton(string val)
    {
        if (m_buttons.ContainsKey(val))
            return m_buttons[val].GetComponent<Button>();
        else return null;
    }

    /*
    public CombRoot GetCombRoot(string val)
    {
        return m_combRoots[val].GetComponent<CombRoot>();
    }
    */

    public bool HasKey(string val)
    {
        return m_images.ContainsKey(val);
    }
    
    public Image GetImage(string val)
    {
        return m_images[val].GetComponent<Image>();
    }
    
    public TextMeshProUGUI GetText(string val)
    {
        if (!m_texts.ContainsKey(val)) return null;
        return m_texts[val].GetComponent<TextMeshProUGUI>();
    }

    public Toggle GetToggle(string val)
    {
        return m_toggles[val].GetComponent<Toggle>();
    }
    public TextMeshPro GetText3D(string val)
    {
        return m3_texts[val].GetComponent<TextMeshPro>();
    }

    public SpriteRenderer GetImage3D(string val)
    {
        if (m3_images.ContainsKey(val))
            return m3_images[val].GetComponent<SpriteRenderer>();
        else
        {
            return m_images[val].GetComponent<SpriteRenderer>();
        }
    }
    public GameObject GetGameobject(string val)
    {
        if (!m_gameObjects.ContainsKey(val)) return null;
        return m_gameObjects[val];
    }

    public GBind GetBind(string val)
    {
        return m_binds[val].GetComponent<GBind>();
    }

    public void SetProgressbar(string nm, float val)
    {
        m_progressbars[nm].GetComponent<Image>().fillAmount = val;
    }

    public void SetProgressbar3D(string nm, float val)
    {
        if (val < 0) val = 0;
        var tt = m_progressbars[nm].GetComponent<SpriteRenderer>().transform;
        
        /*
        if (tt.name.IndexOf("cradi") >= 0)
        {
            var ff = tt.GetComponent<SpriteRenderer>().material;
            ff.SetFloat("_Arc2", 360 - val * 360);
        }
        else
        {
        */
            tt.localScale = new Vector3(val, tt.localScale.y, tt.localScale.z);            
        /*}*/

    }
    
    public static void SetProgressbar3D(Transform tt, float val, float kf = 1)
    {
        if (val < 0) val = 0;
        //thwbbb
        if (val > 1) val = 1;

        tt.localScale = new Vector3(val * kf, tt.localScale.y, tt.localScale.z);

    }

    public Transform GetProgressBar3D(string nm)
    {
        return m_progressbars[nm].GetComponent<SpriteRenderer>().transform;
    }


    public long endDate = -1;
    private void Update()
    {
        if (endDate >= 0)
        {
            if (m_texts.ContainsKey("timer"))
            {
                m_texts["timer"].GetComponent<TextMeshProUGUI>().text = TimeManager.instance.GetStringTillEnd(endDate);
            }
            
            if (m_texts.ContainsKey("timer_back"))
            {
                m_texts["timer_back"].GetComponent<TextMeshProUGUI>().text = TimeManager.instance.GetStringTillEnd(endDate, -1);
            }
            
            if (m_texts.ContainsKey("timer_sec_back"))
            {
                m_texts["timer_sec_back"].GetComponent<TextMeshProUGUI>().text = TimeManager.instance.GetStringTillEnd(endDate, -1, true);
            }
        }
        
        if (m_texts.ContainsKey("timer_day"))
        {
            m_texts["timer_day"].GetComponent<TextMeshProUGUI>().text = TimeManager.instance.GetStringTillEndDay();
        }
    }
}
