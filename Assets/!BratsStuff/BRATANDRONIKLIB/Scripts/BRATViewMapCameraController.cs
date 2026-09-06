using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class BRATViewMapCameraController : MonoBehaviour
{
    [Header("Zoom and Pan")]
    [Min(0.01f)] public float minOrthographicSize = 4f;
    [Min(0.01f)] public float maxOrthographicSize = 10f;
    [Min(0.01f)] public float zoomStep = 1f;
    [Min(0.001f)] public float panSpeed = 0.75f;
    [Range(0, 2)] public int panMouseButton = 1;

    [Header("Map bounds")]
    public Vector2 mapMin = new Vector2(-17f, -9f);
    public Vector2 mapMax = new Vector2(20f, 19f);

    private Camera controlledCamera;
    private bool isDragging;
    private bool inputBlocked;

    public bool InputBlocked => inputBlocked;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        if (!controlledCamera.orthographic)
        {
            Debug.LogError($"{nameof(BRATViewMapCameraController)} requires an orthographic Camera.", this);
            enabled = false;
            return;
        }

        controlledCamera.orthographicSize = Mathf.Clamp(
            controlledCamera.orthographicSize, minOrthographicSize, maxOrthographicSize);
        ClampPosition();
    }

    private void Update()
    {
        if (inputBlocked || UtilsControl.Instance == null)
            return;

        if (Application.isMobilePlatform)
        {
            UtilsControl.Instance.CameraZoom(controlledCamera, minOrthographicSize, maxOrthographicSize, zoomStep);
        }
        else if (!Mathf.Approximately(Input.mouseScrollDelta.y, 0f))
        {
            controlledCamera.orthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize - Input.mouseScrollDelta.y * zoomStep,
                minOrthographicSize,
                maxOrthographicSize);
        }
        UtilsControl.Instance.CameraGrab(
            controlledCamera, panSpeed, ref isDragging, null, null, 0f, panMouseButton);
        ClampPosition();
    }

    private void OnDisable() => isDragging = false;

    public void SetInputBlocked(bool blocked)
    {
        inputBlocked = blocked;
        if (blocked)
            isDragging = false;
    }

    private void ClampPosition()
    {
        var halfHeight = controlledCamera.orthographicSize;
        var halfWidth = halfHeight * controlledCamera.aspect;
        var position = transform.position;
        position.x = ClampAxis(position.x, mapMin.x, mapMax.x, halfWidth);
        position.y = ClampAxis(position.y, mapMin.y, mapMax.y, halfHeight);
        transform.position = position;
    }

    private static float ClampAxis(float value, float min, float max, float cameraExtent)
    {
        return max - min <= cameraExtent * 2f
            ? (min + max) * 0.5f
            : Mathf.Clamp(value, min + cameraExtent, max - cameraExtent);
    }

    private void OnValidate()
    {
        minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        zoomStep = Mathf.Max(0.01f, zoomStep);
        panSpeed = Mathf.Max(0.001f, panSpeed);
        mapMax = Vector2.Max(mapMin, mapMax);
    }
}
