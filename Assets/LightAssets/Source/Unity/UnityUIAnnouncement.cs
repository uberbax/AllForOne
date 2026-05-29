using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UnityUIAnnouncement : MonoBehaviour
{
    public static UnityUIAnnouncement i;
    public TextMeshProUGUI txt;
    public CanvasGroup cg;
    
    Sequence seq;

    void Awake()
    {
        i = this;
        cg.alpha = 0;
    }

    public void Show(string text, float speedNudge = 1f)
    {
        txt.text = text;
        txt.rectTransform.anchoredPosition = new Vector2(0, 400);
        
        seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 1.25f / speedNudge));
        seq.AppendInterval(1f / speedNudge);
        seq.Append(cg.DOFade(0f, 0.5f / speedNudge));
    }

    public void Hide()
    {
        seq?.Kill();
        cg.DOFade(0f, 0.1f);
    }

    public void ShowBoss(string text)
    {
        txt.text = text;
        txt.rectTransform.anchoredPosition = new Vector2(0, 400);
        
        seq = DOTween.Sequence();
        seq.Append(cg.DOFade(1f, 2f));
        seq.AppendInterval(1.5f);
        seq.Append(cg.DOFade(0f, 1f));
    }
}
