using System;
using TMPro;
using UnityEngine;

public class UIBattlePower : MonoBehaviour
{
    public TextMeshProUGUI cur;
    public TextMeshProUGUI add;
    CanvasGroup canvasGroup;
    
    private RObj mon;

    private int prevBP = -1;
    // Update is called once per frame
    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (mon == null)
            mon = MainStates.instance.mainPlayer;

        var f = (int)mon.GetPar("battle_power");
        
        if (prevBP < 0) prevBP = f;

        if (prevBP != f)
        {
            canvasGroup.alpha = 1;
            cur.text = prevBP.ToString();
            if (f > prevBP)
            {
                add.color = Color.green;
                add.text = "+" + (f-prevBP).ToString();
            }
            else
            {
                add.color = Color.red;
                add.text = (f-prevBP).ToString();
            }
            
            
            UtilsControl.Instance.ApplyCurve(cur.transform, AnimationCurve.Linear(0, 0, 1, 1), UtilsControl.CurveType.TextNum, 
                null, 1, 1, 1, 0, default, was:prevBP, now:f );
            prevBP = f;
            
            FunctionTimer.Create(() =>
            {
                canvasGroup.alpha = 0;
            }, 1);
            
        }
        
    }
}
