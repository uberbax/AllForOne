using System.Collections.Generic;
using UnityEngine;

public class GUIButtUpgrade : MonoBehaviour
{
    public GUICostButtonItem upgrade;

    public void Fill(List<Bon> price, bool maxLevel = false, bool block = false, bool showRestriction = true,
        string head = "upgrade", string activeButtonColor = "butgreen", string disabledButtonColor = "butgrey",
        string activeTextColor = "textwhite", string disabledTextColor = "textred")
    {
        upgrade?.Fill(price, maxLevel, block, showRestriction, head, activeButtonColor, disabledButtonColor,
            activeTextColor, disabledTextColor);
    }
}
