using UnityEngine;

namespace LayerLab
{
/// <summary>
/// Rotates a propeller or decorative object around a configurable local axis.
/// </summary>
[DisallowMultipleComponent]
public sealed class PropellerRotator : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 180f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool playOnAwake = true;

    private bool isPlaying;
    private Vector3 normalizedAxis = Vector3.forward;

    public float DegreesPerSecond
    {
        get { return degreesPerSecond; }
        set { degreesPerSecond = value; }
    }

    public bool IsPlaying
    {
        get { return isPlaying; }
        set { isPlaying = value; }
    }

    private void Awake()
    {
        RefreshAxis();
        isPlaying = playOnAwake;
    }

    private void OnValidate()
    {
        RefreshAxis();
    }

    private void Update()
    {
        if (!isPlaying || Mathf.Approximately(degreesPerSecond, 0f))
        {
            return;
        }

        // The axis is cached so Update only applies the frame rotation.
        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(normalizedAxis, degreesPerSecond * deltaTime, Space.Self);
    }

    /// <summary>
    /// Starts rotating without changing the configured speed or axis.
    /// </summary>
    public void Play()
    {
        isPlaying = true;
    }

    /// <summary>
    /// Stops rotation until Play is called again.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
    }

    private void RefreshAxis()
    {
        normalizedAxis = rotationAxis.sqrMagnitude > 0f ? rotationAxis.normalized : Vector3.forward;
    }
}
}
