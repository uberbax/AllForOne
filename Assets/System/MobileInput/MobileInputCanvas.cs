using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MobileInputCanvas : MonoBehaviour
{
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private bool createEventSystemIfMissing = true;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void OnEnable()
    {
        // Repair instances created from the initial prefab with a zero-scale root.
        if (transform.parent != null && transform.parent.GetComponentInParent<Canvas>() != null
            )
        {
            var rect = (RectTransform)transform;
            if (rect.localScale == Vector3.zero) rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        ApplySafeArea();
        if (safeArea != null)
        {
            var joystick = safeArea.GetComponentInChildren<MobileJoystick>(true);
            if (joystick != null)
            {
                var area = safeArea.GetComponent<MobileJoystickArea>();
                if (area == null) area = safeArea.gameObject.AddComponent<MobileJoystickArea>();
                area.Configure(joystick);
            }
        }
    }

    private void Start()
    {
        if (!createEventSystemIfMissing) return;
        foreach (var system in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            if (system.isActiveAndEnabled) return;
        // The project enables both input backends. This module supports mouse and multitouch.
        var go = new GameObject("Mobile Input EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        go.transform.SetParent(transform, false);
    }

    private void LateUpdate()
    {
        if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height
            || lastSafeArea != Screen.safeArea) ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastSafeArea = Screen.safeArea;
        safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
}
