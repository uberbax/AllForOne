using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject activate;
    public bool outlined;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DragObject.inDrag) return;
        
        if (activate) activate.SetActive(true);
        if (outlined)
        {
            var go = GetComponentInParent<SpriteRenderer>().gameObject;
            var hh = go.GetOrAddComponent<_2dxFX_Outline>();
            hh.enabled = true;
            hh._ColorX = Color.yellow;
        }
        PopupoManager.instance.ShowTooltip(transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (activate) activate.SetActive(false);
        if (outlined)
        {
            var go = GetComponentInParent<SpriteRenderer>().gameObject;
            var hh = go.GetOrAddComponent<_2dxFX_Outline>();
            hh.enabled = false;
        }
        PopupoManager.instance.HideTooltip();
    }

    public void OnDisable()
    {
        PopupoManager.instance.HideTooltip();
    }

    private void Update()
    {
        if (DragObject.inDrag)
        {
            PopupoManager.instance.HideTooltip();
        }
    }
}
