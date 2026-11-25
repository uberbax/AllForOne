using UnityEngine;

// World-space markers follow the painted surface through their parent transform.
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
public sealed class BRATGUISpriteMarker : MonoBehaviour
{
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private SpriteRenderer surface;
    [SerializeField] private Sprite paintedSprite;
    [SerializeField] private Vector2 spritePoint;
    [SerializeField] private GameObject battleView;

    private RectTransform marker;
    private RectTransform container;
    private Canvas canvas;
    private CanvasGroup visibility;

    private void Awake()
    {
        marker = (RectTransform)transform;
        container = (RectTransform)marker.parent;
        canvas = GetComponentInParent<Canvas>().rootCanvas;
        visibility = GetComponent<CanvasGroup>();
    }

    private void LateUpdate()
    {
        bool shown = sceneCamera && surface && surface.enabled && surface.gameObject.activeInHierarchy
            && surface.sprite == paintedSprite && (!battleView || battleView.activeInHierarchy);
        if (shown && canvas.renderMode != RenderMode.WorldSpace)
        {
            Vector3 point = sceneCamera.WorldToScreenPoint(surface.transform.TransformPoint(spritePoint));
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            shown = point.z > 0f && sceneCamera.pixelRect.Contains(point);
            if (shown && RectTransformUtility.ScreenPointToLocalPointInRectangle(container, point, uiCamera, out Vector2 position))
                marker.localPosition = new Vector3(position.x, position.y, marker.localPosition.z);
        }

        visibility.alpha = shown ? 1f : 0f;
        visibility.blocksRaycasts = shown;
        visibility.interactable = shown;
    }
}

