using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Paperbound.Traveller
{
    /// <summary>Local camera-relative movement. Does not move the animated visual root or own network state.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class TravellerMotor : MonoBehaviour
    {
        public TravellerPresentation presentation;
        public Camera viewCamera;
        public bool readPlayerInput = true;
        [Min(.01f)] public float moveSpeed = 1.62f;
        [Min(.001f)] public float stoppingDistance = .04f;
        [Min(0)] public float gravity = 18f;
        [Min(.05f)] public float stepInterval = .31f;
        public LayerMask walkableLayers = 1;
        public TravellerClickMarker clickMarkerPrefab;
        [Tooltip("Optional world XZ rectangle. Bounds are not tied to any book scene.")]
        public bool constrainToRectangle;
        public Vector2 minXZ = new Vector2(-5.45f, -3.6f);
        public Vector2 maxXZ = new Vector2(5.45f, 3.75f);
        public bool BlockPointerInput { get; set; }
        public bool HasDestination => hasDestination;
        public Vector3 Velocity { get; private set; }
        public Vector3 Destination => destination;
        CharacterController capsule;
        TravellerClickMarker marker;
        Vector2 externalInput;
        Vector3 destination;
        bool hasDestination;
        float verticalVelocity, stepClock;

        void Awake()
        {
            capsule = GetComponent<CharacterController>();
            if (!presentation) presentation = GetComponent<TravellerPresentation>();
            if (!viewCamera) viewCamera = Camera.main;
        }
        void Update()
        {
            if (!viewCamera) viewCamera = Camera.main;
            Vector2 movement = readPlayerInput ? ReadMovement() : externalInput;
            if (readPlayerInput && !BlockPointerInput && PointerPressed(out var position))
                TryMoveToScreenPoint(position);
            Tick(movement, Time.deltaTime);
        }
        /// <summary>Use with Read Player Input off for touch, AI or an owning network controller.</summary>
        public void SetMoveInput(Vector2 value) { externalInput = Vector2.ClampMagnitude(value, 1); if (value.sqrMagnitude > .001f) hasDestination = false; }
        public void MoveTo(Vector3 point)
        {
            if (presentation && !presentation.CanMove) return;
            destination = point; hasDestination = true; externalInput = Vector2.zero;
        }
        public void Stop() { hasDestination = false; externalInput = Vector2.zero; Velocity = Vector3.zero; if (presentation) presentation.SetWalking(false); }
        public void Teleport(Vector3 point)
        {
            Stop(); verticalVelocity = 0;
            bool wasEnabled = capsule && capsule.enabled;
            if (wasEnabled) capsule.enabled = false;
            transform.position = point;
            if (wasEnabled) capsule.enabled = true;
        }
        public void TryMoveToScreenPoint(Vector2 position)
        {
            if (!viewCamera || (presentation && !presentation.CanMove)) return;
            if (!Physics.Raycast(viewCamera.ScreenPointToRay(position), out var hit, 200, walkableLayers, QueryTriggerInteraction.Ignore)) return;
            if (hit.normal.y < .45f) return;
            MoveTo(hit.point);
            if (clickMarkerPrefab)
            {
                if (!marker) marker = Instantiate(clickMarkerPrefab);
                marker.Show(hit.point + hit.normal * .015f, hit.normal);
            }
        }
        void Tick(Vector2 input, float dt)
        {
            if (!capsule || !capsule.enabled || dt <= 0) return;
            Vector3 before = transform.position;
            Vector3 direction = Vector3.zero;
            bool canMove = !presentation || presentation.CanMove;
            float distance = moveSpeed * dt;
            if (!canMove) { hasDestination = false; externalInput = Vector2.zero; }
            else if (input.sqrMagnitude > .001f)
            {
                hasDestination = false;
                Vector3 forward = viewCamera ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up) : Vector3.forward;
                if (forward.sqrMagnitude < .001f) forward = Vector3.forward;
                forward.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, forward);
                direction = Vector3.ClampMagnitude(right * input.x + forward * input.y, 1);
            }
            else if (hasDestination)
            {
                Vector3 delta = Vector3.ProjectOnPlane(destination - before, Vector3.up);
                if (delta.magnitude <= stoppingDistance) hasDestination = false;
                else { direction = delta.normalized; distance = Mathf.Min(distance, delta.magnitude); }
            }
            Vector3 motion = direction * distance;
            if (constrainToRectangle)
            {
                motion.x = Mathf.Clamp(before.x + motion.x, minXZ.x, maxXZ.x) - before.x;
                motion.z = Mathf.Clamp(before.z + motion.z, minXZ.y, maxXZ.y) - before.z;
            }
            if (capsule.isGrounded && verticalVelocity < 0) verticalVelocity = -2;
            verticalVelocity = Mathf.Max(verticalVelocity - gravity * dt, -30);
            CollisionFlags flags = capsule.Move(motion + Vector3.up * verticalVelocity * dt);
            if ((flags & CollisionFlags.Below) != 0) verticalVelocity = -2;
            Velocity = (transform.position - before) / dt;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(Velocity, Vector3.up);
            bool moving = canMove && planarVelocity.sqrMagnitude > .0001f;
            if (presentation)
            {
                presentation.SetWalking(moving);
                presentation.Face(planarVelocity.x);
                if (moving)
                {
                    stepClock += dt;
                    if (stepClock >= stepInterval) { stepClock %= stepInterval; presentation.PlayStep(); }
                }
                else stepClock = 0;
            }
        }
        static Vector2 ReadMovement()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2((k.dKey.isPressed || k.rightArrowKey.isPressed ? 1 : 0) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1 : 0), (k.wKey.isPressed || k.upArrowKey.isPressed ? 1 : 0) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1 : 0)), 1);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Vector2.ClampMagnitude(new Vector2((Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0), (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)), 1);
#else
            return Vector2.zero;
#endif
        }
        public static bool PointerPressed(out Vector2 position)
        {
            position = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) { position = Touchscreen.current.primaryTouch.position.ReadValue(); return true; }
            if (Mouse.current != null) { position = Mouse.current.position.ReadValue(); return Mouse.current.leftButton.wasPressedThisFrame; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) { position = Input.GetTouch(0).position; return true; }
            position = Input.mousePosition; if (Input.GetMouseButtonDown(0)) return true;
#endif
            return false;
        }
        void OnApplicationFocus(bool focused) { if (!focused) Stop(); }
        void OnDisable() { Stop(); verticalVelocity = 0; if (marker) marker.gameObject.SetActive(false); }
        void OnDestroy() { if (marker) Destroy(marker.gameObject); }
    }
}
