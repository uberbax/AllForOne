using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PrisonMoveArrow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool IsHeld { get; private set; }
    private int pointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || IsHeld) return;
        pointerId = eventData.pointerId;
        IsHeld = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId) Release();
    }

    public void Release() => IsHeld = false;

    private void OnDisable() => Release();
}
