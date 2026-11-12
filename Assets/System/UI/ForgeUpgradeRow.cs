using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ForgeUpgradeRow : MonoBehaviour
{
    [SerializeField] private ForgeController controller;
    [SerializeField] private string parameter;
    [SerializeField] private float increment;
    [SerializeField] private float displayScale = 1;
    [SerializeField] private float displayOffset;
    [SerializeField] private string suffix;
    [SerializeField] private string currency = "gold";
    [SerializeField, Min(1)] private int cost;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text valuePreview;
    [SerializeField] private TMP_Text costAmount;
    [SerializeField] private Image currencyIcon;

    public string Parameter => parameter;
    public float Increment => increment;
    public string Currency => currency;
    public int Cost => cost;
    public bool CanUpgrade => controller != null && controller.HasSelection && increment > 0 && cost > 0 &&
        !string.IsNullOrEmpty(parameter) && !string.IsNullOrEmpty(currency) &&
        MainStates.instance.GetItemsCount(currency) >= cost;

    public void Upgrade() => controller.Upgrade(this);

    public void Refresh()
    {
        costAmount.text = cost.ToString(CultureInfo.InvariantCulture);
        if (ResourceHolder.instance != null)
            currencyIcon.sprite = ResourceHolder.instance.GetIcon(currency);

        bool hasItem = controller != null && controller.HasSelection;
        valuePreview.text = hasItem
            ? Format(controller.Selected.GetPar(parameter)) + " > <color=green>" +
                Format(controller.Selected.GetPar(parameter) + increment) + "</color>"
            : "—";
        upgradeButton.interactable = CanUpgrade;
    }

    private string Format(float value) =>
        (value * displayScale + displayOffset).ToString("0.##", CultureInfo.InvariantCulture) + suffix;
}
