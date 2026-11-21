using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BRATGUIHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Graphic icon;
    public Graphic label;
    public Graphic rim;
    public RectTransform visual;
    public Color hoverColor = new Color32(249, 248, 245, 255);
    [Range(0f, 1f)] public float brightness = 1f;
    [Min(0.01f)] public float fadeDuration = 0.1f;
    [Min(1f)] public float hoverScale = 1.04f;

    private Color iconColor, labelColor, rimColor;
    private Vector3 baseScale;
    private float progress;
    private Coroutine routine;

    private void OnEnable()
    {
        if (icon) iconColor = icon.color;
        if (label) labelColor = label.color;
        if (rim) rimColor = rim.color;
        if (visual) baseScale = visual.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => FadeTo(1f);
    public void OnPointerExit(PointerEventData eventData) => FadeTo(0f);

    private void FadeTo(float target)
    {
        if (routine != null) StopCoroutine(routine);
        routine = progress == target ? null : StartCoroutine(Fade(target));
    }

    private IEnumerator Fade(float target)
    {
        while (progress != target)
        {
            yield return null;
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeDuration));
            Apply();
        }
        routine = null;
    }

    private void Apply()
    {
        if (visual) visual.localScale = baseScale * Mathf.Lerp(1f, hoverScale, progress);
        if (icon) icon.color = Tint(iconColor);
        if (label) label.color = Tint(labelColor);
        if (rim) rim.color = Tint(rimColor);
    }

    private Color Tint(Color original)
    {
        Color target = hoverColor;
        target.a = original.a;
        return Color.Lerp(original, target, progress * brightness);
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        progress = 0f;
        Apply();
    }
}
