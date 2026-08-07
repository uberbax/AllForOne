using UnityEngine;

namespace LayerLab
{
/// <summary>
/// Moves the sample character horizontally and flips its visual root to face the current movement direction.
/// </summary>
[DisallowMultipleComponent]
public sealed class SampleCharacterMover : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool moveOnUpdate = true;
    [SerializeField] private bool faceRightWhenSpeedPositive = true;

    private int directionSign = 1;

    public float Speed
    {
        get { return speed; }
        set
        {
            float limit = MaxSpeed;
            speed = Mathf.Clamp(value, -limit, limit);
            UpdateDirectionSign();
            ApplyFacing();
        }
    }

    public float SpeedMagnitude
    {
        get { return Mathf.Abs(speed); }
        set { SetSpeedMagnitude(value); }
    }

    public float MaxSpeed
    {
        get { return Mathf.Max(0f, maxSpeed); }
    }

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform;
        }

        Speed = speed;
    }

    private void OnValidate()
    {
        maxSpeed = Mathf.Max(0f, maxSpeed);
        speed = Mathf.Clamp(speed, -maxSpeed, maxSpeed);
        UpdateDirectionSign();
        ApplyFacing();
    }

    private void Update()
    {
        if (moveOnUpdate && !Mathf.Approximately(speed, 0f))
        {
            transform.position += Vector3.right * (speed * Time.deltaTime);
        }

        ApplyFacing();
    }

    /// <summary>
    /// Keeps the current speed magnitude and changes movement to the left.
    /// </summary>
    public void MoveLeft()
    {
        directionSign = -1;
        speed = -Mathf.Abs(speed);
        ApplyFacing();
    }

    /// <summary>
    /// Keeps the current speed magnitude and changes movement to the right.
    /// </summary>
    public void MoveRight()
    {
        directionSign = 1;
        speed = Mathf.Abs(speed);
        ApplyFacing();
    }

    /// <summary>
    /// Updates movement speed without changing the current left/right direction.
    /// </summary>
    public void SetSpeedMagnitude(float value)
    {
        float magnitude = Mathf.Clamp(value, 0f, MaxSpeed);
        speed = magnitude * directionSign;
        ApplyFacing();
    }

    /// <summary>
    /// Restores the character position while preserving speed and direction.
    /// </summary>
    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (visualRoot == null || Mathf.Approximately(speed, 0f))
        {
            return;
        }

        // Only the sign of localScale.x changes, so the original sprite size is preserved.
        float direction = speed > 0f ? 1f : -1f;
        if (!faceRightWhenSpeedPositive)
        {
            direction = -direction;
        }

        Vector3 scale = visualRoot.localScale;
        float width = Mathf.Abs(scale.x);
        scale.x = (Mathf.Approximately(width, 0f) ? 1f : width) * direction;
        visualRoot.localScale = scale;
    }

    private void UpdateDirectionSign()
    {
        if (speed > 0f)
        {
            directionSign = 1;
        }
        else if (speed < 0f)
        {
            directionSign = -1;
        }
    }
}
}
