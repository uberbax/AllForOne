using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GUIInventoryList : MonoBehaviour
{
    public InventoryUnit inventory;

    public void FillChoosen(string listType)
    {
        var units = GUILIB.PlayerInventory().Where(x => x != null && x.it == ItemType.monster).ToList();
        if (listType == "tavern" || string.IsNullOrEmpty(listType))
            units = units.Where(x => x.GetPar("used_slot") < 0).ToList();
        else if (listType == "expedition")
            units = MainCycle_WhoHeroes.GetSelectedUnits().Where(x => x != null && x.it == ItemType.monster)
                .ToList();
        else if (listType == "tower")
            units = units.Where(x => x.GetPar("used_slot") >= 20 && x.GetPar("used_slot") <= 23)
                .OrderBy(x => x.GetPar("used_slot")).ToList();
        inventory?.Fill(units);
    }
}

[Serializable]
public class InventoryUnit
{
    [SerializeField] private List<GUIUnitFrameBehav> items = new List<GUIUnitFrameBehav>();
    public Transform holder;
    public InventorySettings viewMask;

    public void SetUpInventory()
    {
        if (holder == null)
            return;
        items.Clear();
        for (var i = 0; i < holder.childCount; i++)
        {
            var slot = holder.GetChild(i).GetComponentInChildren<GUIUnitFrameBehav>(true);
            if (slot == null)
                continue;
            items.Add(slot);
            slot.SetUpSlot(viewMask.hasInfo, viewMask.hasAction, viewMask.actionType, viewMask.actionColor);
        }
    }

    public void Fill(List<RObj> units)
    {
        Fill(units, null);
    }

    public void Fill(List<RObj> units, Func<RObj, bool> actionState)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var active = i < units.Count;
            items[i].gameObject.SetActive(active);
            if (active)
            {
                items[i].Fill(units[i]);
                if (actionState != null)
                    items[i].ChangeActionState(actionState(units[i]), viewMask.actionColor,
                        viewMask.actionColorDis);
            }
        }
    }

    public void UpdateActionState(bool state)
    {
        foreach (var item in items)
            if (item.gameObject.activeSelf)
                item.ChangeActionState(state, viewMask.actionColor, viewMask.actionColorDis);
    }
}

[Serializable]
public class InventorySettings
{
    public bool hasInfo = true;
    public bool hasAction = true;
    public string actionType = "hire";
    public string actionColor = "butgreen";
    public string actionColorDis = "butred";
}
