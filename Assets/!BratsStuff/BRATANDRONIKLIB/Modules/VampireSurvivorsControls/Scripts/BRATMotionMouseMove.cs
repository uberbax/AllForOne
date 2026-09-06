using System;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class BRATMotionMouseMove : MonoBehaviour
{
    public Camera controlledCamera;
    [Min(0.01f)] public float speed = 3f;
    [Range(0, 2)] public int mouseButton;
    public bool clickToMove = true;
    public bool ignorePointerOverUI = true;
    [Min(0.001f)] public float stopDistance = 0.05f;

    public event Action<Vector3> MovementStarted;
    public event Action MovementStopped;

    public bool IsMoving => isMoving;
    public Vector3 Destination => destination;

    private Vector3 destination;
    private bool isMoving;

    private void Update()
    {
        var inputRequested = clickToMove
            ? Input.GetMouseButtonDown(mouseButton)
            : Input.GetMouseButton(mouseButton);
        if (inputRequested && !IsPointerOverUI())
            SetDestination(ScreenToWorld(Input.mousePosition));

        if (!clickToMove && !Input.GetMouseButton(mouseButton))
            Stop();
        if (!isMoving)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position, destination, speed * Time.deltaTime);
        if ((transform.position - destination).sqrMagnitude <= stopDistance * stopDistance)
            Stop();
    }

    public void SetDestination(Vector3 worldPosition)
    {
        worldPosition.z = transform.position.z;
        destination = worldPosition;
        if ((transform.position - destination).sqrMagnitude <= stopDistance * stopDistance)
        {
            Stop();
            return;
        }

        isMoving = true;
        MovementStarted?.Invoke(destination - transform.position);
    }

    public void Stop()
    {
        if (!isMoving)
            return;
        isMoving = false;
        MovementStopped?.Invoke();
    }

    private Vector3 ScreenToWorld(Vector3 screenPosition)
    {
        var activeCamera = controlledCamera != null ? controlledCamera : Camera.main;
        return activeCamera != null ? activeCamera.ScreenToWorldPoint(screenPosition) : transform.position;
    }

    private bool IsPointerOverUI()
    {
        return ignorePointerOverUI && EventSystem.current != null &&
               EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDisable() => Stop();
}
