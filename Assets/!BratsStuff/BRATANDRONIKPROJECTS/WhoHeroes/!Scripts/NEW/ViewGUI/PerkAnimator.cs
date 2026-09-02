using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class PerkAnimator : MonoBehaviour, IPointerClickHandler
{
    public string type = "perk";

    public float appearDuration = 0.35f;
    public float bounceScale = 1.15f;
    public float overshootScale = 1.25f;
    public float selectScale = 1.3f;
    public float jumpHeight = 25f;

    private RectTransform rt;
    private Vector2 basePosition;
    private bool isSelected = false;

    void Awake()
    {
        EnsureReferences();
        basePosition = rt.anchoredPosition;
        rt.localScale = Vector3.zero;
    }

    public IEnumerator PlayAppear()
    {
        EnsureReferences();
        // 
        float t = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * overshootScale;

        Vector2 startPos = basePosition;
        Vector2 jumpPos = startPos + Vector2.up * jumpHeight;

        while (t < appearDuration)
        {
            t += Time.deltaTime;
            float k = t / appearDuration;

            // 
            rt.localScale = Vector3.Lerp(startScale, endScale, k);

            yield return null;
        }

        //
        t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;
            rt.localScale = Vector3.Lerp(Vector3.one * overshootScale, Vector3.one * bounceScale, k);
            yield return null;
        }

        // 
        t = 0;
        while (t < 0.18f)
        {
            t += Time.deltaTime;
            float k = t / 0.18f;
            rt.anchoredPosition = Vector2.Lerp(startPos, jumpPos, Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        rt.anchoredPosition = startPos;
        rt.localScale = Vector3.one;
    }

    public void ResetState()
    {
        EnsureReferences();
        isSelected = false;
        if (rt != null)
        {
            rt.localScale = Vector3.zero;
            rt.anchoredPosition = basePosition;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        isSelected = false;
        if (rt == null)
            return;
        rt.anchoredPosition = basePosition;
        rt.localScale = Vector3.zero;
    }

    private void EnsureReferences()
    {
        if (rt == null)
            rt = GetComponent<RectTransform>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(type != "perk")
            return;

        if (isSelected) return;
        isSelected = true;
            StartCoroutine(SelectAnimation());
    }

    IEnumerator SelectAnimation()
    {
        
        float t = 0;
        Vector3 startScale = rt.localScale;
        Vector3 targetScale = Vector3.one * selectScale;

        Vector2 startPos = rt.anchoredPosition;
        Vector2 jumpPos = startPos + Vector2.up * (jumpHeight * 1.5f);

        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float k = t / 0.25f;

            rt.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Sin(k * Mathf.PI));
            rt.anchoredPosition = Vector2.Lerp(startPos, jumpPos, Mathf.Sin(k * Mathf.PI));

            yield return null;
        }

       
        var window = GetComponent<GUIUnitFullInfo>();
        var perkWindow = GetComponentInParent<GUIPerkWindow>();
        if (type == "perk")
        {
            if (window == null || perkWindow == null || !perkWindow.SelectPermanentPerk(window.unitgui.unit))
                isSelected = false;
            yield break;
        }
        if (window != null)
            GUILIB.CoreAction(window.unitgui.unit, "buy");

    }
}
