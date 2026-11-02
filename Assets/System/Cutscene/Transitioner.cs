using System;
using System.Collections;
using System.Collections.Generic;
using Colorful;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] private Slider loadingProgress;
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

    public void Simple2(GameObject go)
    {
        DoFade(1, 1, () =>
        {
            if (go)
                go.SetActive(false);
        });
    }
    
    public void Simple(GameObject go, List<GameObject> toActivate = null, Action end = null, List<GameObject> toDeactivate = null)
    {
        DoFade(1, 1, () =>
                {
                    if (go)
                        go.SetActive(false);
                    
                    if (toActivate != null)
                        foreach (var a in toActivate)
                            a.SetActive(true);
                    
                    if (toDeactivate != null)
                        foreach (var a in toDeactivate)
                            a.SetActive(false);
                    
                }, () =>
        {
            if (end != null)
                end();
        });
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
    public static float GLobalSpd = -1;
    public static float GLobalWait = -1;

    private void ResetLoadingProgress()
    {
        if (loadingProgress == null)
        {
            var bar = bg.transform.Find("LoadingProgress");
            if (bar != null) loadingProgress = bar.GetComponent<Slider>();
        }
        if (loadingProgress == null) return;
        loadingProgress.gameObject.SetActive(true);
        loadingProgress.interactable = false;
        loadingProgress.wholeNumbers = false;
        // The Slider changes the fill rect width. Do not also clip its Image to the old 65%.
        if (loadingProgress.fillRect != null)
        {
            var image = loadingProgress.fillRect.GetComponent<Image>();
            if (image != null) image.fillAmount = 1;
        }
        loadingProgress.SetValueWithoutNotify(loadingProgress.minValue);
    }

    private IEnumerator FillLoadingProgress(float duration, Action complete)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            if (loadingProgress != null)
                loadingProgress.SetValueWithoutNotify(Mathf.Lerp(loadingProgress.minValue, loadingProgress.maxValue, Mathf.Clamp01(elapsed / duration)));
        }
        if (loadingProgress != null) loadingProgress.SetValueWithoutNotify(loadingProgress.maxValue);
        // Render the full bar before the fade-out starts, including a zero-duration transition.
        yield return null;
        if (loadingProgress != null) loadingProgress.gameObject.SetActive(false);
        complete?.Invoke();
    }
    
    public void DoFade(float spd, float wait, Action midAct, Action endAct = null)
    {
        if (GLobalSpd > 0)
            spd = GLobalSpd;
        if (GLobalWait > 0)
            wait = GLobalWait;
        
        
        ClearSteps();
        steps.gameObject.SetActive(false);
        pic.gameObject.SetActive(false);
        // LOADING starts inactive in Game_Dark1; changing alpha alone cannot show it.
        var canvasGroup = bg.GetComponent<CanvasGroup>();
        bg.SetActive(true);
        canvasGroup.blocksRaycasts = true;
        ResetLoadingProgress();
        UtilsControl.Instance.FadeCanvasG(canvasGroup, spd, 1, () =>
        {
            if (midAct != null) midAct();
            steps.gameObject.SetActive(true);
            pic.gameObject.SetActive(true);
            StartCoroutine(EnableSteps());
            StartCoroutine(FillLoadingProgress(wait, () =>
            {
                pic.gameObject.SetActive(false);
                steps.gameObject.SetActive(false);
                UtilsControl.Instance.FadeCanvasG(canvasGroup, spd, 0, () =>
                {
                    canvasGroup.blocksRaycasts = false;
                    bg.SetActive(false);
                    endAct?.Invoke();
                });
            }));
        });
    }
    
}
