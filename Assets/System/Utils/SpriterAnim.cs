using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriterAnim : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float timeFrame = 0.1f;
    public List<UnoAnim> anims = new List<UnoAnim>();

    public string defaultAnim = "idle";


    private int curFrame = 0;
    private float lastTm = -1;
    public string curAnim;

    private UnoAnim curUanim;

    private SpriteRenderer spAnimator;
    private Image imAnimator;
    
    void Start()
    {
        spAnimator = GetComponent<SpriteRenderer>();
        imAnimator = GetComponent<Image>();

        CrossFade(defaultAnim, 0);
    }

    public void CrossFade(string anim, float tm)
    {
        //Debug.Log(anim);
        if (tm > 0)
        {
            FunctionTimer.Create(() => Inst(anim), tm);
        }
        else
        {
            Inst(anim);
        }

    }

    public void Inst(string anim)
    {
        //hack thwbbb
        if (curAnim == "Dead" && name.ToLower().IndexOf("player") < 0) 
            return;
        
        //Debug.Log("ANIM : " + anim);
        if (anim == "ult") anim = "Attack";
        
        curAnim = anim;
        curUanim = anims.Find(x => x.nm == anim);
        curFrame = 0;
        SetFrame(0);
        lastTm = Time.time;
    }

    public void SetFrame(int num)
    {
        if (spAnimator != null)
            spAnimator.sprite = curUanim.sprites[num];
        else if (imAnimator != null)
            imAnimator.sprite = curUanim.sprites[num];
    }

    private void Update()
    {
        if (Time.time - timeFrame > lastTm)
        {
            curFrame++;
            if (curFrame >= curUanim.sprites.Count)
            {
                if (curUanim.doEffect)
                {
                    GetComponent<UnoEffect>().enabled = true;
                }
                
                if (curUanim.loop)
                {
                    curFrame = 0;
                }
                else if (curUanim.endAnim != "")
                {
                    CrossFade(curUanim.endAnim, timeFrame);
                    return;
                }
                else
                {
                    return;
                }
            }

            lastTm = Time.time;
            SetFrame(curFrame);
        }
    }


    public string forceAnim = "idle";
    [ContextMenu("ForceAnim")]
    public void ForceAnim()
    {
        CrossFade(forceAnim, 0.1f);
    }
}

[System.Serializable]
public class UnoAnim
{
    public string nm = "";
    public bool loop = false;
    public List<Sprite> sprites = new List<Sprite>();

    public string endAnim = "";
    public bool doEffect = false;
}
