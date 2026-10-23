using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Invisible SafeArea input surface. Interactive UI retains priority.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MobileJoystickArea : Graphic, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, IInitializePotentialDragHandler, ICancelHandler
{
    [SerializeField] private MobileJoystick joystick;
    private static bool checkingOtherUI;
    private readonly List<RaycastResult> otherHits = new List<RaycastResult>();
    private PointerEventData probe;
    private EventSystem probeSystem;
    public void Configure(MobileJoystick target) { joystick = target; raycastTarget = true; color = Color.clear; canvasRenderer.cullTransparentMesh = false; }
    protected override void OnPopulateMesh(VertexHelper mesh) => base.OnPopulateMesh(mesh);

    public override bool Raycast(Vector2 point, Camera eventCamera)
    {
        if (checkingOtherUI || joystick == null || !joystick.isActiveAndEnabled || !base.Raycast(point, eventCamera)) return false;
        var system = EventSystem.current;
        if (system == null) return true;
        if (probe == null || probeSystem != system) { probeSystem = system; probe = new PointerEventData(system); }
        probe.position = point;
        checkingOtherUI = true;
        try
        {
            otherHits.Clear();
            system.RaycastAll(probe, otherHits);
            foreach (var hit in otherHits)
            {
                if (hit.gameObject == gameObject || hit.gameObject.transform.IsChildOf(joystick.transform)) continue;
                if (ExecuteEvents.GetEventHandler<IPointerDownHandler>(hit.gameObject) != null
                    || ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) != null
                    || ExecuteEvents.GetEventHandler<IDragHandler>(hit.gameObject) != null) return false;
            }
            return true;
        }
        finally { checkingOtherUI = false; }
    }
    public void OnInitializePotentialDrag(PointerEventData data) => data.useDragThreshold = false;
    public void OnPointerDown(PointerEventData data) { if (joystick != null) joystick.OnPointerDown(data); }
    public void OnDrag(PointerEventData data) { if (joystick != null) joystick.OnDrag(data); }
    public void OnPointerUp(PointerEventData data) { if (joystick != null) joystick.OnPointerUp(data); }
    public void OnCancel(BaseEventData data) { if (joystick != null) joystick.OnCancel(data); }
    protected override void OnDisable() { if (joystick != null) joystick.ResetInput(); base.OnDisable(); }
}


