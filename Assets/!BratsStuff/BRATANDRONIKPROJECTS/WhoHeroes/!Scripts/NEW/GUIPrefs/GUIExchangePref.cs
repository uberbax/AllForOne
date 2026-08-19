using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIExchangePref : MonoBehaviour
{
    public string id = "";
    public SimpleButtonItem sell;
    public SimpleButtonItem buy;
    private RObj item;

    private void Start()
    {
        sell?.but?.onClick.AddListener(() => GUILIB.CoreAction(FindItem(false), "sell"));
        buy?.but?.onClick.AddListener(() => GUILIB.CoreAction(FindItem(true), "buy"));
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Fill());
        Fill();
    }

    private RObj FindItem(bool create)
    {
        item = GUILIB.PlayerInventory().Find(x => GUILIB.IsId(x, id));
        if (item == null && create && DatabaseAll.instance != null && DatabaseAll.instance.items.ContainsKey(id))
            item = DatabaseAll.instance.CreateItem(id, 1, false, false);
        return item;
    }

    public void Fill()
    {
        var current = FindItem(false);
        var template = current ?? FindItem(true);
        var sellPrice = current == null ? 0 : Mathf.RoundToInt(current.GetPar("amount"));
        var buyPrice = template == null || GUILIB.Price(template, "buy").Count == 0 ? 0 : GUILIB.Price(template, "buy")[0].Value;
        sell?.Fill(current != null && sellPrice > 0, current != null ? "butred" : "butgrey", sellPrice);
        buy?.Fill(template != null && GUILIB.CanAfford(GUILIB.Price(template, "buy")), "butgreen", buyPrice);
    }

    private void OnEnable()
    {
        Fill();
    }
}

[Serializable]
public class SimpleButtonItem
{
    public Button but;
    public TextMeshProUGUI textValue;
    public string fillFormat = "int";
    public Image butImage;

    public void Fill(bool state = true, string color = "butgreen", float value = 0)
    {
        if (but != null) but.interactable = state;
        if (butImage != null) butImage.color = GUILIB.ColorFor(state ? color : "butgrey");
        if (textValue != null) textValue.text = GUILIB.Instance.FillNum(value, fillFormat);
    }
}
