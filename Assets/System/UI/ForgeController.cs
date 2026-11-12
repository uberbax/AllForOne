using System.Collections.Generic;
using UnityEngine;

public sealed class ForgeController : MonoBehaviour
{
    private const string SelectEvent = "evt_forge_select";

    [SerializeField] private ObjHolder selectedWeapon;
    [SerializeField] private GameObject emptyIcon;
    [SerializeField] private GameObject[] itemVisuals;
    [SerializeField] private ForgeUpgradeRow[] upgrades;

    private RObj selected;
    private bool busy;
    private float nextRefresh;

    public RObj Selected => selected;
    public bool HasSelection => IsOwnedEquipment(selected);

    private void OnEnable()
    {
        EventManager.SUB(SelectEvent, OnItemSelected);
        UIfiller.acts += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        UIfiller.acts -= Refresh;
        if (EventManager.dynActions.TryGetValue(SelectEvent, out var handlers))
        {
            handlers -= OnItemSelected;
            if (handlers == null) EventManager.dynActions.Remove(SelectEvent);
            else EventManager.dynActions[SelectEvent] = handlers;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + .25f;
        Refresh();
    }

    private void OnItemSelected(ArgPass args) => Select(args.who);

    public void Select(RObj item)
    {
        selected = IsOwnedEquipment(item) ? item : null;
        selectedWeapon.obj = selected;
        if (selected != null) selectedWeapon.OnEnable();
        Refresh();
    }

    public void Refresh()
    {
        if (selectedWeapon == null || emptyIcon == null || itemVisuals == null || upgrades == null) return;
        if (selected != null && !IsOwnedEquipment(selected)) selected = null;
        selectedWeapon.obj = selected;
        bool hasItem = selected != null;
        emptyIcon.SetActive(!hasItem);
        foreach (var visual in itemVisuals) visual.SetActive(hasItem);
        foreach (var row in upgrades) row.Refresh();
    }

    public void Upgrade(ForgeUpgradeRow row)
    {
        if (busy || !HasSelection || row == null || !row.CanUpgrade) return;
        var state = MainStates.instance;
        var item = selected;
        busy = true;
        try
        {
            var price = new List<Bon> { new Bon { Key = row.Currency, Value = row.Cost } };
            if (!state.HaveAmount(price)) return;
            state.DelItems(price);
            item.ChangePar(row.Parameter, row.Increment);
            item.owner.RecalcPars();
            EventManager.INV("evt_full_upgrade", new ArgPass { who = item });
            if (state.useSave) state.Save();
            selectedWeapon.OnEnable();
            UIfiller.GlobalRefresh();
        }
        finally
        {
            busy = false;
            Refresh();
        }
    }

    private static bool IsOwnedEquipment(RObj item)
    {
        var state = MainStates.instance;
        if (state == null || !state.HasMain() || item?.dbObj == null || item.it != ItemType.item ||
            item.GetPar("slot") < 0 || item.GetPar("amount") <= 0)
            return false;

        var owner = item.owner;
        return owner != null && owner.inventory.Contains(item) &&
            (owner == state.mainPlayer || owner == state.lastAllySelected || state.mainPlayer.inventory.Contains(owner));
    }
}
