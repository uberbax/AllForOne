using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATMotionCloudMove : MonoBehaviour
    {
        [Min(0f)] public float speed = 0.08f;

        // Kept for compatibility with already serialized scene components.
        [Min(0f)] public float distance = 0.35f;
        [Min(0.01f)] public float cycleDuration = 36f;
        [Min(0.01f)] public float smoothing = 0.18f;
        [Range(0f, 1f)] public float phase;
        public Vector2 direction = Vector2.right;

        private Vector3 _startLocalPosition;

        private void Awake()
        {
            _startLocalPosition = transform.localPosition;
        }

        private void OnEnable()
        {
            transform.localPosition = _startLocalPosition;
        }

        private void LateUpdate()
        {
            transform.localPosition += Vector3.right * (speed * Time.deltaTime);
        }

        private void OnDisable()
        {
            transform.localPosition = _startLocalPosition;
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            distance = Mathf.Max(0f, distance);
            cycleDuration = Mathf.Max(0.01f, cycleDuration);
            smoothing = Mathf.Max(0.01f, smoothing);
        }
    }
}
