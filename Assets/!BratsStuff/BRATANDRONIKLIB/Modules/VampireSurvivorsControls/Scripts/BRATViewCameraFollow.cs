using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class BRATViewCameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);
    [Min(0f)] public float smoothSpeed = 10f;
    public bool followX = true;
    public bool followY = true;
    public bool followZ;

    private void LateUpdate()
    {
        if (target == null)
            return;

        var desired = target.position + offset;
        var current = transform.position;
        if (!followX)
            desired.x = current.x;
        if (!followY)
            desired.y = current.y;
        if (!followZ)
            desired.z = current.z;

        var factor = smoothSpeed <= 0f
            ? 1f
            : 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(current, desired, factor);
    }

    public void Snap()
    {
        if (target == null)
            return;

        var desired = target.position + offset;
        var current = transform.position;
        if (!followX)
            desired.x = current.x;
        if (!followY)
            desired.y = current.y;
        if (!followZ)
            desired.z = current.z;
        transform.position = desired;
    }
}
