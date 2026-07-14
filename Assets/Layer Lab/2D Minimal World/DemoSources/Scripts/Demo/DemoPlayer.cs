using UnityEngine;
using UnityEngine.InputSystem;

namespace LayerLab
{
    /// <summary>
    /// Demo player controller that only handles movement (no pathfinding, harvesting, or attacks).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DemoPlayer : MonoBehaviour
    {
        private const float InputEpsilonSqr = 0.0001f;
        private const float FacingEpsilon = 0.01f;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 3.5f;
        [SerializeField] private float runSpeed = 5.5f;
        [SerializeField] private float acceleration = 18f;

        [Header("Mouse Movement")]
        [SerializeField] private bool enableMouseMove = true;
        [SerializeField] private DemoController demoController;

        [Header("Facing")]
        [SerializeField] private bool flipSprite = true;
        [SerializeField] private bool defaultFacingRight = true;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string idleState = "Idle";
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string runState = "Run";
        [SerializeField] private float animCrossFade = 0.08f;

        private Rigidbody2D _rb;
        private Camera _camera;
        private Vector2 _moveInput;
        private bool _isRunning;
        private int _currentAnimHash;

        private void Awake()
        {
            Application.runInBackground = true;

            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (animator == null) animator = GetComponentInChildren<Animator>();
            _camera = Camera.main;
            if (demoController == null) demoController = Object.FindFirstObjectByType<DemoController>();
        }

        private void Update()
        {
            ReadInput();
            UpdateAnimation();
        }

        private void FixedUpdate()
        {
            Move();
        }

        // Reads movement input, preferring keyboard over mouse when both are active.
        private void ReadInput()
        {
            Vector2 keyboardMove = ReadKeyboardMove();
            _isRunning = IsRunPressed();

            if (keyboardMove.sqrMagnitude > InputEpsilonSqr)
            {
                _moveInput = keyboardMove;
                return;
            }

            _moveInput = GetMouseMoveDirection();
        }

        private static Vector2 ReadKeyboardMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero;

            Vector2 input = Vector2.zero;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;

            return input.sqrMagnitude > 1f ? input.normalized : input;
        }

        private static bool IsRunPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private bool TryGetMouseWorldPosition(Vector2 screenPosition, out Vector2 worldPosition)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    worldPosition = default;
                    return false;
                }
            }

            float zDistance = -_camera.transform.position.z;
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, zDistance);
            worldPosition = _camera.ScreenToWorldPoint(screenPoint);
            return true;
        }

        // Returns a normalized direction toward the mouse world position while a mouse button is held.
        private Vector2 GetMouseMoveDirection()
        {
            if (!enableMouseMove) return Vector2.zero;

            Mouse mouse = Mouse.current;
            if (mouse == null) return Vector2.zero;
            if (!mouse.leftButton.isPressed && !mouse.rightButton.isPressed) return Vector2.zero;

            Vector2 screenPosition = mouse.position.ReadValue();
            if (demoController != null && demoController.IsScreenPositionOverDemoUi(screenPosition)) return Vector2.zero;
            if (!TryGetMouseWorldPosition(screenPosition, out Vector2 worldPosition)) return Vector2.zero;

            Vector2 direction = worldPosition - _rb.position;
            return direction.sqrMagnitude > InputEpsilonSqr ? direction.normalized : Vector2.zero;
        }

        // Smoothly accelerates the rigidbody toward the target velocity and updates facing.
        private void Move()
        {
            if (_moveInput == Vector2.zero)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            float speed = _isRunning ? runSpeed : walkSpeed;
            Vector2 targetVelocity = _moveInput * Mathf.Max(0f, speed);
            _rb.linearVelocity = Vector2.Lerp(
                _rb.linearVelocity,
                targetVelocity,
                Mathf.Max(0f, acceleration) * Time.fixedDeltaTime);

            ApplyFacing(_moveInput.x);
        }

        // Flips the sprite horizontally to match the movement direction.
        private void ApplyFacing(float horizontalDirection)
        {
            if (!flipSprite || Mathf.Abs(horizontalDirection) <= FacingEpsilon) return;

            Vector3 scale = transform.localScale;
            float absX = Mathf.Abs(scale.x);
            if (absX <= Mathf.Epsilon) absX = 1f;

            bool faceRight = horizontalDirection > 0f;
            scale.x = faceRight == defaultFacingRight ? absX : -absX;
            transform.localScale = scale;
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            string stateName = idleState;
            if (_moveInput.sqrMagnitude > InputEpsilonSqr)
            {
                stateName = _isRunning ? runState : walkState;
            }

            PlayAnimationState(stateName);
        }

        private void PlayAnimationState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName)) return;

            int stateHash = Animator.StringToHash(stateName);
            if (_currentAnimHash == stateHash) return;

            _currentAnimHash = stateHash;
            animator.CrossFadeInFixedTime(stateHash, Mathf.Max(0f, animCrossFade), 0, 0f);
        }
    }
}
