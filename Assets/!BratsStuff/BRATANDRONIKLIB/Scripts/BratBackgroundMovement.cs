using LayerLab;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BratBackgroundMovement : MonoBehaviour
{
    [SerializeField] private SampleCharacterMover movementSource;
    [SerializeField, Min(0f)] private float speedMultiplier = 1f;
    [SerializeField] private bool movementEnabled = true;

    private Vector3 startPosition;

    public SampleCharacterMover MovementSource
    {
        get { return movementSource; }
        set { movementSource = value; }
    }

    public float SpeedMultiplier
    {
        get { return speedMultiplier; }
        set { speedMultiplier = Mathf.Max(0f, value); }
    }

    private void Awake()
    {
        startPosition = transform.position;

        if (movementSource == null)
        {
            Debug.LogError(
                "BratBackgroundMovement requires a SampleCharacterMover reference.",
                this);
        }
    }

    private void OnValidate()
    {
        speedMultiplier = Mathf.Max(0f, speedMultiplier);
    }

    private void Update()
    {
        if (!movementEnabled || movementSource == null)
        {
            return;
        }

        float distance = movementSource.Speed * speedMultiplier * Time.deltaTime;
        transform.position += Vector3.left * distance;
    }

    public void SetMovementEnabled(bool value)
    {
        movementEnabled = value;
    }

    public void ResetPosition()
    {
        transform.position = startPosition;
    }
}
