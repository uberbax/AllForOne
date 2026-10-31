using UnityEngine;

[ExecuteAlways]
public sealed class HeroDetailsLayout : MonoBehaviour
{
    [SerializeField] private RectTransform content;
    private void OnEnable() { Fit(); }
    private void OnRectTransformDimensionsChange() { Fit(); }
    public void Fit()
    {
        var parent = transform as RectTransform;
        if (content == null || parent == null || parent.rect.width <= 0 || parent.rect.height <= 0) return;
        content.localScale = Vector3.one * Mathf.Min(parent.rect.width / 1080f, parent.rect.height / 1920f);
    }
}
