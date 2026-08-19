using System;
using UnityEngine;

public class GUIUnitFullInfo : MonoBehaviour
{
    public GUIUnit unitgui;
    public ButtonActionItem actionBut;
    public UnitDrowSettings viewType;

    private void Start()
    {
        SetUpButtons();
        if (viewType.showUprgade && unitgui?.hire?.upgrade?.buy != null)
            unitgui.hire.upgrade.buy.onClick.AddListener(() => GUILIB.CoreAction(unitgui.unit, "buy"));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ =>
        {
            if (gameObject.activeInHierarchy)
                Fill(unitgui?.unit);
        });
    }

    public void Fill(RObj value = null)
    {
        if (value != null)
            unitgui?.Fill(value);
    }

    private void SetUpButtons()
    {
        unitgui?.hire?.gameObject.SetActive(viewType.showUprgade);
        if (actionBut?.but == null)
            return;
        actionBut.but.gameObject.SetActive(viewType.showAction);
        if (!viewType.showAction)
            return;
        actionBut.Fill(true, viewType.actionType, viewType.actionType == "add" ? "butgreen" : "butred");
        actionBut.but.onClick.AddListener(() =>
            GUILIB.CoreAction(unitgui?.unit, viewType.actionType == "add" ? "equip_exp" : "unequip_exp"));
    }
}

[Serializable]
public class UnitDrowSettings
{
    public bool showUprgade = true;
    public bool showAction;
    public string actionType = "add";
}
