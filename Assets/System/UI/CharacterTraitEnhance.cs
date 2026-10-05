using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterTraitEnhance : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text currentText;
    [SerializeField] private TMP_Text nextText;
    // Retain the old reference so existing instances can hide the retired text safely.
    [SerializeField, HideInInspector] private TMP_Text costText;
    [SerializeField] private RectTransform groupArea;
    [SerializeField] private CostView goldCost = new CostView();
    [SerializeField] private CostView res1Cost = new CostView();
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Image icon;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button closeButton;
    private CharacterTraitController controller;

    public void Bind(CharacterTraitController owner)
    {
        controller = owner;
        if (confirmButton != null) { confirmButton.onClick.RemoveListener(Confirm); confirmButton.onClick.AddListener(Confirm); }
        if (closeButton != null) { closeButton.onClick.RemoveListener(Cancel); closeButton.onClick.AddListener(Cancel); }
    }
    private void OnDestroy()
    {
        if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
        if (closeButton != null) closeButton.onClick.RemoveListener(Cancel);
    }
    private void Confirm() { if (controller != null) controller.ConfirmUpgrade(); }
    private void Cancel() { if (controller != null) controller.CancelUpgrade(); }

    public void Refresh(CharacterTraitDefinition trait, int level, RObj player, bool affordable)
    {
        bool maxed = level >= CharacterTraitProgression.MaxLevel;
        if (icon != null && trait.Icon != null) icon.sprite = trait.Icon;
        if (levelText != null) levelText.text = level + "/" + CharacterTraitProgression.MaxLevel;
        if (currentText != null) currentText.text = trait.Label + "  +" + trait.Format(level * trait.Increment);
        if (nextText != null) nextText.text = maxed ? "Maximum level" : trait.Label + "  +" + trait.Format((level + 1) * trait.Increment);
        if (costText != null) costText.gameObject.SetActive(false);
        if (groupArea != null) groupArea.gameObject.SetActive(!maxed);
        goldCost.Refresh("gold", trait.GoldCost, player, maxed);
        res1Cost.Refresh("res1", trait.Res1Cost, player, maxed);
        if (confirmText != null) confirmText.text = maxed ? "MAX" : affordable ? "Enhance" : "Not enough resources";
        if (confirmButton != null) confirmButton.interactable = !maxed && affordable;
    }

    [System.Serializable]
    private sealed class CostView
    {
        [SerializeField] private GameObject group;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        public void Refresh(string itemId, int amount, RObj player, bool maxed)
        {
            if (group != null) group.SetActive(!maxed && amount > 0);
            if (maxed || amount <= 0) return;
            var sprite = ResourceHolder.instance != null ? ResourceHolder.instance.GetIcon(itemId) : null;
            if (icon != null) { icon.sprite = sprite; icon.enabled = sprite != null; }
            if (amountText == null) return;
            amountText.text = sprite != null ? amount.ToString() : amount + " " + itemId;
            amountText.color = CharacterTraitProgression.GetAmount(player, itemId) >= amount
                ? Color.white : new Color(1f, 0.45f, 0.4f);
        }
    }
}
