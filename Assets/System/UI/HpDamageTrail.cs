using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class HpDamageTrail : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Image trail;
    [SerializeField, Min(.01f)] private float catchUpTime = .3f;

    private float previousAmount;
    private float velocity;
    private bool initialized;

    private void OnEnable()
    {
        initialized = false;
        velocity = 0;
        if (trail != null) trail.enabled = false;
    }

    private void LateUpdate()
    {
        if (fill == null || trail == null) return;
        if (!initialized) MatchFillGeometry();
        float amount = fill.fillAmount;
        if (!initialized || amount > previousAmount || trail.fillAmount <= amount + .001f)
        {
            initialized = true;
            velocity = 0;
            trail.fillAmount = amount;
        }
        else if (Time.unscaledDeltaTime > 0)
        {
            trail.fillAmount = Mathf.SmoothDamp(trail.fillAmount, amount, ref velocity,
                catchUpTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        trail.enabled = trail.fillAmount > amount + .001f;
        previousAmount = amount;
    }

    private void MatchFillGeometry()
    {
        // Scene instances can override the red fill's layout independently of the prefab.
        var source = fill.rectTransform;
        var destination = trail.rectTransform;
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.sizeDelta = source.sizeDelta;
        destination.anchoredPosition3D = source.anchoredPosition3D;
        destination.localRotation = source.localRotation;
        destination.localScale = source.localScale;

        trail.overrideSprite = fill.overrideSprite;
        trail.preserveAspect = fill.preserveAspect;
        trail.fillMethod = fill.fillMethod;
        trail.fillOrigin = fill.fillOrigin;
    }
}
