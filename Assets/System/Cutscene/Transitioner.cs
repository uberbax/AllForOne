using System;
using System.Collections;
using System.Collections.Generic;
using Colorful;
using TMPro;
using UnityEngine;

public class Transitioner : MonoBehaviour
{
    // Start is called before the first frame update
    public static Transitioner instance;
    public GameObject bg;
    public GameObject tobeCOnt;

    public Transform tEnd;

    public TextMeshProUGUI phraseTxt;
    public GameObject pic;
    public Transform steps;
    private void Awake()
    {
        instance = this;
    }
    
    private void OnDestroy()
    {
        instance = null;
    }

#region meme_dungeon
    public void Dob()
    {
        //UtilsControl.Instance.FadeToColor(bg, new Color(0,0,0,1), 2f);
        UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 1, Bogi);
        
    }

    public void Bogi()
    {
        FunctionTimer.Create(() =>
        {
            var gg = GameObject.Find("MainCamera");
            var hh = GameObject.Find("camcam");
            gg.transform.position = hh.transform.position;
            
            UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 0, Bobi);
            
        }, 0.5f);        
    }

    public void Bobi()
    {
        Cutscener.instance.ExecuteCutscene("id_dung");
    }

    public void Tobe()
    {
        var gg = GameObject.Find("MainCamera");
        gg.GetComponent<ComicBook>().enabled = true;
        
        tobeCOnt.SetActive(true);
        UtilsControl.Instance.MoveTo(tobeCOnt.transform, 1000, tEnd.position, null, tEnd);        
    }
    #endregion
    
    public void Dob_2()
    {
        UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 1, null);
        //UtilsControl.Instance.FadeToColor(bg, new Color(0,0,0,1), 2f);
    }

#region vegan_meme
    
    public void DobWed()
    {
        UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 1, Mov);
    }

    public void Mov()
    {
        FunctionTimer.Create(() =>
        {
            var gg = GameObject.Find("MainCamera");
            var hh = GameObject.Find("camp1");
            gg.transform.position = hh.transform.position;
            gg.GetComponent<CameraFollow>().target = hh.transform;
            
            UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 0, Mov2);
            
        }, 0.5f);  
    }

    public void Mov2()
    {
        Cutscener.instance.ExecuteCutscene("id_vegan");
    }
    #endregion
    
    public void DobDrag()
    {
        phraseTxt.text = "";
        UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 1, MovD);
    }

    public void MovD()
    {
        FunctionTimer.Create(() =>
        {
            var gg = GameObject.Find("MainCamera");
            var hh = GameObject.Find("camp2");
            gg.transform.position = hh.transform.position;
            gg.GetComponent<CameraFollow>().target = hh.transform;
            
            UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), 2, 0, MovD2);
            
        }, 0.5f);  
    }

    public void MovD2()
    {
        Cutscener.instance.ExecuteCutscene("id_dragon");
    }


    public void ClearSteps()
    {
        for (int i = 0; i < steps.childCount; i++)
        {
            steps.GetChild(i).gameObject.SetActive(false);
        }
    }

    public IEnumerator EnableSteps()
    {
        for (int i = 0; i < steps.childCount; i++)
        {
            yield return new WaitForSeconds(0.2f);
            steps.GetChild(i).gameObject.SetActive(true);
        }
    }
    //
    public void DoFade(float spd, float wait, Action midAct, Action endAct = null)
    {
        ClearSteps();
        steps.gameObject.SetActive(false);
        pic.gameObject.SetActive(false);
        UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), spd, 1, () =>
        {
            if (midAct != null) midAct();
            steps.gameObject.SetActive(true);
            pic.gameObject.SetActive(true);
            StartCoroutine(EnableSteps());
            FunctionTimer.Create(() =>
            {
                pic.gameObject.SetActive(false);
                steps.gameObject.SetActive(false);
                UtilsControl.Instance.FadeCanvasG(bg.GetComponent<CanvasGroup>(), spd, 0, endAct);
            }, wait);
        });
    }
    
}
