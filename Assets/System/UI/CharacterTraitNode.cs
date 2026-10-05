using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterTraitNode : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject normal;
    [SerializeField] private GameObject upgraded;
    [SerializeField] private GameObject upgradeArrow;
    private CharacterTraitController controller;
    private int index;

    public void Bind(CharacterTraitController owner, int traitIndex)
    {
        controller = owner; index = traitIndex;
        if (button != null) { button.onClick.RemoveListener(Select); button.onClick.AddListener(Select); }
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Select); }
    private void Select() { if (controller != null) controller.RequestUpgrade(index); }

    public void Refresh(CharacterTraitDefinition trait, int level, bool ready)
    {
        if (label != null) label.text = trait?.Label ?? "";
        if (icon != null && trait?.Icon != null) icon.sprite = trait.Icon;
        if (levelText != null) levelText.text = level + "/" + CharacterTraitProgression.MaxLevel;
        if (button != null) button.interactable = ready && trait != null && trait.IsValid;
        if (normal != null) normal.SetActive(level == 0);
        if (upgraded != null) upgraded.SetActive(level > 0);
        if (upgradeArrow != null) upgradeArrow.SetActive(ready && level < CharacterTraitProgression.MaxLevel);
    }
}
