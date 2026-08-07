using UnityEngine;

namespace LayerLab
{
/// <summary>
/// Follows only the target X position while preserving the camera's original Y/Z placement.
/// </summary>
[DisallowMultipleComponent]
public sealed class CameraFollowTargetX : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float xOffset;
    [SerializeField] private bool useSmoothing;
    [SerializeField] private float smoothTime = 0.15f;

    private float velocityX;

    public Transform Target
    {
        get { return target; }
        set { target = value; }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Keep the camera depth and height unchanged; only horizontal movement is applied.
        Vector3 position = transform.position;
        float targetX = target.position.x + xOffset;
        position.x = useSmoothing
            ? Mathf.SmoothDamp(position.x, targetX, ref velocityX, Mathf.Max(0.001f, smoothTime))
            : targetX;
        transform.position = position;
    }
}
}
