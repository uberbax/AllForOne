using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUITavernWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public GUIValueGrades grade;
    public GUIInventoryList guests;
    public GUIButtUpgrade upgrade;
    public TextMeshProUGUI dopLvl;
    private RObj runtime;

    private void Awake()
    {
        guests?.inventory?.SetUpInventory();
        back?.onClick.AddListener(Close);
        upgrade?.upgrade?.buy?.onClick.AddListener(Reroll);
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
        back?.onClick.RemoveListener(Close);
        upgrade?.upgrade?.buy?.onClick.RemoveListener(Reroll);
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null) return;
        var level = GUILIB.Level(runtime, building.level);
        general?.Fill(runtime);
        if (dopLvl != null) dopLvl.text = level.ToString();
        grade?.Fill(level, runtime.GetPar("max_stack"), Mathf.Max(1, runtime.GetPar("level_multiplier")));
        upgrade?.Fill(MainCycle_WhoHeroes.TavernRerollPrice(), false,
            !MainCycle_WhoHeroes.IsManagementActionAllowed("reroll"), false, "reroll");

        var offers = runtime.inventory.Where(MainCycle_WhoHeroes.IsActiveTavernOffer)
            .Take(MainCycle_WhoHeroes.TavernOfferCount).ToList();
        guests?.inventory?.Fill(offers, CanBuy);
    }

    private static bool CanBuy(RObj offer)
    {
        return MainCycle_WhoHeroes.IsManagementActionAllowed("buy") &&
               GUILIB.CanAfford(GUILIB.Price(offer, "buy"));
    }

    private void Reroll()
    {
        MainCycle_WhoHeroes.Instance?.TryRerollTavern();
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }

    private void OnRefresh(ArgPass _)
    {
        if (gameObject.activeInHierarchy)
            Fill();
    }

}
