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
    
    [SerializeField] private GameObject disabled;
    [SerializeField] private GameObject locked;
    [SerializeField] private GameObject line;
    
    [SerializeField] private CharacterTraitNode depend;
    
    
    private CharacterTraitController controller;
    private int index;

    public void Bind(CharacterTraitController owner, int traitIndex)
    {
        controller = owner; index = traitIndex;
        if (button != null) { button.onClick.RemoveListener(Select); button.onClick.AddListener(Select); }
    }
    private void OnDestroy() { if (button != null) button.onClick.RemoveListener(Select); }
    private void Select() { if (controller != null) controller.RequestUpgrade(index); }

    public int lvl;
    public void Refresh(CharacterTraitDefinition trait, int level, bool ready)
    {
        this.lvl = level;
        if (label != null) label.text = trait?.Label ?? "";
        if (icon != null && trait?.Icon != null) icon.sprite = trait.Icon;
        if (levelText != null) levelText.text = level + "/" + CharacterTraitProgression.MaxLevel;
        if (button != null) button.interactable = ready && trait != null && trait.IsValid;
        if (normal != null) normal.SetActive(level == 0);
        if (upgraded != null) upgraded.SetActive(level > 0);
        if (upgradeArrow != null) upgradeArrow.SetActive(ready && level < CharacterTraitProgression.MaxLevel);
        
        Recalc();
    }

    void Recalc()
    {
        if (line != null)
        {
            if (lvl == CharacterTraitProgression.MaxLevel)
            {
                line.GetComponent<Image>().color = controller.colorCan;
            }
        }
        //

        if (depend != null)
        {
            if (depend.lvl == CharacterTraitProgression.MaxLevel)
            {
                disabled.SetActive(false);
                locked.SetActive(false);
                button.interactable = true;

            }
            else
            {
                disabled.SetActive(true);
                locked.SetActive(true);
                levelText.text = "";
                upgradeArrow.SetActive(false);
                button.interactable = false;
            }
        }
    }
}
