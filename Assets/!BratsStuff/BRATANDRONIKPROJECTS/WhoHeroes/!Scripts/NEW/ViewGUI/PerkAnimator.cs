using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PerkAnimator : MonoBehaviour, IPointerClickHandler
{
    public string type = "perk";

    public float appearDuration = 0.35f;
    public float bounceScale = 1.15f;
    public float overshootScale = 1.25f;
    public float selectScale = 1.3f;
    public float jumpHeight = 25f;

    private RectTransform rt;
    private bool isSelected = false;
    private CanvasGroup cg;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.zero;
        cg.alpha = 0f;
    }

    public IEnumerator PlayAppear()
    {
        // 
        float t = 0;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * overshootScale;

        Vector2 startPos = rt.anchoredPosition;
        Vector2 jumpPos = startPos + Vector2.up * jumpHeight;

        while (t < appearDuration)
        {
            t += Time.deltaTime;
            float k = t / appearDuration;

            // 
            rt.localScale = Vector3.Lerp(startScale, endScale, k);

            // 
            cg.alpha = k;

            yield return null;
        }

        //
        t = 0;
        while (t < 0.15f)
        {
            t += Time.deltaTime;
            float k = t / 0.15f;
            rt.localScale = Vector3.Lerp(Vector3.one * overshootScale, Vector3.one, k);
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

       
        Debug.Log("Perk selected: " + name);
        var window = GetComponent<GUIUnitFullInfo>();
        if (window != null)
            GUILIB.CoreAction(window.unitgui.unit, type == "perk" ? "take_skill" : "buy");

    }
}
