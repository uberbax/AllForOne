using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUICastleWindow : MonoBehaviour
{
    public WhoHeroesObjectRef building = new WhoHeroesObjectRef();
    public GUIInfoItem general;
    public Button back;
    public Button infoBut;
    public PopUpList highlight;
    public Transform shopHolder;
    private readonly List<GUISmallBuildingPref> shop = new List<GUISmallBuildingPref>();
    private RObj runtime;

    private void Awake()
    {
        if (shopHolder == null)
            return;
        for (var i = 0; i < shopHolder.childCount; i++)
        {
            var item = shopHolder.GetChild(i).GetComponent<GUISmallBuildingPref>();
            if (item != null) shop.Add(item);
        }
    }

    private void Start()
    {
        back?.onClick.AddListener(() => gameObject.SetActive(false));
        infoBut?.onClick.AddListener(() => GUILIB.Emit(WhoHeroesEvents.ViewBuilding, runtime, building.id));
        highlight?.SetUpNavigation();
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.SUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
    }

    private void OnRefresh(ArgPass _)
    {
        if (gameObject.activeInHierarchy) Fill();
    }

    private void OnObserveBuilding(ArgPass value)
    {
        if (gameObject.activeInHierarchy && value != null && highlight?[value.what] != null)
            highlight.SwitchTab(value.what);
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.UNSUB(WhoHeroesEvents.ObserveBuilding, OnObserveBuilding);
    }

    public void Fill(RObj value = null)
    {
        runtime = value ?? GUILIB.Resolve(building, gameObject);
        if (runtime == null)
            return;
        general?.Fill(runtime);
        foreach (var item in shop)
            item.Fill();
    }

    private void OnDisable()
    {
        highlight?.ToDefault();
    }
}
