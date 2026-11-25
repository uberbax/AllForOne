using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image), typeof(Button))]
public sealed class BRATGUITextButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Color normalTextColor = new Color32(227, 178, 99, 255);
    [SerializeField] private Color hoverTextColor = new Color32(242, 232, 204, 255);

    private Image background;

    private void Awake() => background = GetComponent<Image>();

    private void OnEnable() => Apply(false);

    private void OnDisable() => Apply(false);

    public void OnPointerEnter(PointerEventData eventData) => Apply(true);

    public void OnPointerExit(PointerEventData eventData) => Apply(false);

    private void Apply(bool hovered)
    {
        if (label)
            label.color = hovered ? hoverTextColor : normalTextColor;
        if (background)
            background.overrideSprite = hovered ? hoverSprite : null;
    }
}

