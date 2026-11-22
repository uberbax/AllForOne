using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class BattleEnemyClick : MonoBehaviour, IPointerClickHandler, ICanvasRaycastFilter
{
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private RObj unit;
    private Transform head;
    private Transform legs;

    public static void Attach(RObj target)
    {
        var image = target.visMain.GetComponent<RawImage>();
        if (image == null) return;
        var canvas = image.GetComponentInParent<Canvas>();
        if (canvas == null) return;
        image.raycastTarget = true;
        canvas.worldCamera = Camera.main;
        if (canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        var click = image.GetComponent<BattleEnemyClick>() ?? image.gameObject.AddComponent<BattleEnemyClick>();
        click.unit = target;
        click.head = image.transform.Find("head");
        click.legs = image.transform.Find("legs");
    }

    private bool CanSelect() => isActiveAndEnabled && unit?.main != null && unit.main.activeInHierarchy &&
        unit.tags.Contains("enemy") && unit.GetPar("health") > 0;

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (MainStates.instance?.inBattle != true || !CanSelect()) return false;
        if (head == null || legs == null) return true;
        float distance = DistanceToBodySquared(screenPoint, eventCamera, out float height);
        float radius = Mathf.Max(12f, height * .26f);
        return distance <= radius * radius;
    }

    private float DistanceToBodySquared(Vector2 point, Camera camera, out float height)
    {
        height = 0;
        if (head == null || legs == null)
            return (point - RectTransformUtility.WorldToScreenPoint(camera, unit.main.transform.position)).sqrMagnitude;

        Vector2 top = RectTransformUtility.WorldToScreenPoint(camera, head.position);
        Vector2 bottom = RectTransformUtility.WorldToScreenPoint(camera, legs.position);
        Vector2 axis = top - bottom;
        height = axis.magnitude;
        float along = Mathf.Clamp01(Vector2.Dot(point - bottom, axis) / Mathf.Max(axis.sqrMagnitude, .001f));
        return (point - (bottom + axis * along)).sqrMagnitude;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var state = MainStates.instance;
        if (eventData.button != PointerEventData.InputButton.Left || state?.inBattle != true || !CanSelect()) return;

        // When two bodies overlap on screen, favor the one nearest the pointer over canvas sort order.
        var selected = this;
        var camera = eventData.pointerPressRaycast.module != null ? eventData.pressEventCamera : Camera.main;
        float bestDistance = DistanceToBodySquared(eventData.position, camera, out _);
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, hits);
            foreach (var hit in hits)
            {
                if (!hit.gameObject.TryGetComponent<BattleEnemyClick>(out var other) || other == this || !other.CanSelect()) continue;
                float distance = other.DistanceToBodySquared(eventData.position, hit.module.eventCamera, out _);
                if (distance >= bestDistance) continue;
                selected = other;
                bestDistance = distance;
            }
            hits.Clear();
        }
        state.lastTargetSelected = selected.unit;
    }
}
