using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler,
    IPointerUpHandler, IInitializePotentialDragHandler, ICancelHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField, Range(0f, 0.95f)] private float deadZone = 0.1f;

    [SerializeField, Min(0f)] private float fadeDuration = 0.25f;
    private CanvasGroup visibility;
    private float fadeTime;
    private float fadeStartAlpha;
    private bool fading;

    private const int NoPointer = int.MinValue;
    private int pointerId = NoPointer;

    /// <summary>Analog input in [-1, 1], with a circular magnitude limit and radial dead zone.</summary>
    public Vector2 Value { get; private set; }
    public float Horizontal => Value.x;
    public float Vertical => Value.y;
    public bool IsPressed => pointerId != NoPointer;

    private void Awake()
    {
        if (background == null) background = (RectTransform)transform;
        visibility = GetComponent<CanvasGroup>();
        if (visibility == null) visibility = gameObject.AddComponent<CanvasGroup>();
        visibility.blocksRaycasts = false;
        ResetInput();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        eventData.useDragThreshold = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!enabled || !gameObject.activeInHierarchy || IsPressed || eventData.button != PointerEventData.InputButton.Left
            || background == null || handle == null) return;
        var area = background.parent as RectTransform;
        if (area == null || !RectTransformUtility.RectangleContainsScreenPoint(area, eventData.position, eventData.pressEventCamera)
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out var local)) return;
        background.localPosition = new Vector3(local.x, local.y, background.localPosition.z);
        fading = false;
        if (visibility != null) visibility.alpha = 1f;
        pointerId = eventData.pointerId;
        UpdateInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (IsPressed && eventData.pointerId == pointerId) UpdateInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsPressed || eventData.pointerId != pointerId) return;
        ClearInput();
        fadeTime = 0f;
        fadeStartAlpha = visibility != null ? visibility.alpha : 0f;
        fading = fadeDuration > 0f && fadeStartAlpha > 0f;
        if (!fading && visibility != null) visibility.alpha = 0f;
    }

    public void OnCancel(BaseEventData eventData) => ResetInput();

    /// <summary>Release the captured pointer and return the handle and value to the centre.</summary>
    public void ResetInput()
    {
        ClearInput();
        fading = false;
        if (visibility != null) visibility.alpha = 0f;
    }

    private void Update()
    {
        if (!fading || visibility == null) return;
        fadeTime += Time.unscaledDeltaTime;
        float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeTime / fadeDuration);
        visibility.alpha = fadeStartAlpha * (1f - Mathf.SmoothStep(0f, 1f, t));
        if (t >= 1f) fading = false;
    }

    private void ClearInput()
    {
        pointerId = NoPointer;
        Value = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    private void UpdateInput(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background,
            eventData.position, eventData.pressEventCamera, out var point)) return;

        // Leave enough room for the complete handle to stay inside the base.
        float radius = Mathf.Max(1f, (Mathf.Min(background.rect.width, background.rect.height)
            - Mathf.Min(handle.rect.width, handle.rect.height)) * 0.5f);
        Vector2 raw = Vector2.ClampMagnitude((point - background.rect.center) / radius, 1f);
        handle.anchoredPosition = raw * radius;
        float magnitude = raw.magnitude;
        float zone = Mathf.Clamp(deadZone, 0f, 0.95f);
        Value = magnitude <= zone ? Vector2.zero
            : raw / magnitude * Mathf.InverseLerp(zone, 1f, magnitude);
    }

    private void OnDisable() => ResetInput();
    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) ResetInput(); }
    private void OnApplicationPause(bool paused) { if (paused) ResetInput(); }
}
