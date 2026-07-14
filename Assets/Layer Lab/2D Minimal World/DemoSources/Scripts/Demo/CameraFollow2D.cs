using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LayerLab
{
    /// <summary>
    /// 2D 카메라가 대상(플레이어)을 부드럽게 따라다니게 한다.
    /// SmoothDamp를 사용해 자연스러운 추적을 구현하고,
    /// 선택적으로 데드존(여유 영역)과 맵 경계 제한을 지원한다.
    /// 카메라 떨림을 막기 위해 LateUpdate에서 처리한다.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("추적 대상")]
        [Tooltip("따라갈 대상 (보통 플레이어). 비어 있으면 'Player' 태그를 자동 탐색)")]
        public Transform target;

        [Header("추적 설정")]
        [Tooltip("따라가는 부드러움. 값이 작을수록 빠르게 붙고, 클수록 느리게 따라온다.")]
        public float smoothTime = 0.2f;

        [Tooltip("대상으로부터의 오프셋 (카메라 위치 조정용)")]
        public Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("데드존 (선택)")]
        [Tooltip("대상이 이 영역을 벗어날 때만 카메라가 움직인다. 0이면 항상 추적")]
        public Vector2 deadZone = Vector2.zero;

        [Header("줌 설정")]
        [Tooltip("마우스 휠로 직교 카메라 크기를 조절한다.")]
        public bool enableMouseWheelZoom = true;

        [Tooltip("마우스 휠 줌 속도. 값이 클수록 한 번에 더 크게 확대/축소된다.")]
        [Min(0f)] public float zoomSpeed = 1f;

        [Tooltip("줌이 목표 크기에 도달하는 부드러움. 0이면 즉시 적용된다.")]
        [Min(0f)] public float zoomSmoothTime = 0.08f;

        [Tooltip("최대 확대 시 직교 카메라 크기")]
        [Min(0.1f)] public float minOrthographicSize = 3f;

        [Tooltip("최대 축소 시 직교 카메라 크기")]
        [Min(0.1f)] public float maxOrthographicSize = 12f;

        [Header("맵 경계 제한 (선택)")]
        [Tooltip("카메라가 맵 밖을 비추지 않도록 경계를 제한")]
        public bool useBounds = false;

        public Vector2 minBounds = new Vector2(-50f, -50f);
        public Vector2 maxBounds = new Vector2(50f, 50f);

        private Camera _cam;
        private Vector3 _velocity = Vector3.zero;
        private float _targetOrthographicSize;
        private float _zoomVelocity;
        private bool _followLocked;

        public bool FollowLocked => _followLocked;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null && _cam.orthographic)
            {
                _targetOrthographicSize = ClampOrthographicSize(_cam.orthographicSize);
                _cam.orthographicSize = _targetOrthographicSize;
            }
        }

        private void Start()
        {
            // 대상이 지정되지 않았으면 Player 태그로 자동 탐색
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }

            // 시작 시 즉시 대상 위치로 스냅 (첫 프레임 튐 방지)
            if (target != null)
            {
                transform.position = ClampToBounds(target.position + offset);
            }
        }

        private void LateUpdate()
        {
            HandleMouseWheelZoom();

            if (_followLocked) return;
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            // 데드존: 대상이 일정 범위 안에 있으면 카메라를 고정
            if (deadZone != Vector2.zero)
            {
                Vector3 current = transform.position;
                float dx = desiredPosition.x - current.x;
                float dy = desiredPosition.y - current.y;

                if (Mathf.Abs(dx) < deadZone.x * 0.5f) desiredPosition.x = current.x;
                if (Mathf.Abs(dy) < deadZone.y * 0.5f) desiredPosition.y = current.y;
            }

            // SmoothDamp로 부드럽게 추적
            Vector3 smoothed = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                smoothTime);

            // z는 오프셋 값 유지 (2D 카메라 깊이 고정)
            smoothed.z = target.position.z + offset.z;

            transform.position = ClampToBounds(smoothed);
        }

        public void LockAtCurrentPosition()
        {
            _followLocked = true;
            _velocity = Vector3.zero;
            transform.position = ClampToBounds(transform.position);
        }

        public void UnlockFollow()
        {
            _followLocked = false;
            _velocity = Vector3.zero;
        }

        private void HandleMouseWheelZoom()
        {
            if (!enableMouseWheelZoom || _cam == null || !_cam.orthographic) return;

            if (_targetOrthographicSize <= 0f)
            {
                _targetOrthographicSize = ClampOrthographicSize(_cam.orthographicSize);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                float scrollY = mouse.scroll.ReadValue().y;
                if (!Mathf.Approximately(scrollY, 0f))
                {
                    _targetOrthographicSize = ClampOrthographicSize(_targetOrthographicSize - scrollY * zoomSpeed * 0.01f);
                }
            }

            float nextSize = zoomSmoothTime <= 0f
                ? _targetOrthographicSize
                : Mathf.SmoothDamp(_cam.orthographicSize, _targetOrthographicSize, ref _zoomVelocity, zoomSmoothTime);

            nextSize = ClampOrthographicSize(nextSize);
            if (Mathf.Approximately(nextSize, _cam.orthographicSize)) return;

            _cam.orthographicSize = nextSize;
            transform.position = ClampToBounds(transform.position);
        }

        private float ClampOrthographicSize(float size)
        {
            float minSize = Mathf.Min(minOrthographicSize, maxOrthographicSize);
            float maxSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
            return Mathf.Clamp(size, minSize, maxSize);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (maxOrthographicSize < minOrthographicSize)
            {
                maxOrthographicSize = minOrthographicSize;
            }

            Camera cameraComponent = GetComponent<Camera>();
            if (cameraComponent != null && cameraComponent.orthographic)
            {
                cameraComponent.orthographicSize = ClampOrthographicSize(cameraComponent.orthographicSize);
                _targetOrthographicSize = cameraComponent.orthographicSize;
                _zoomVelocity = 0f;
            }

            SceneView.RepaintAll();
        }
#endif

        /// <summary>
        /// 맵 경계 안으로 카메라 위치를 제한한다.
        /// 직교 카메라의 화면 절반 크기를 고려해 경계 밖이 보이지 않게 한다.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!useBounds) return position;

            float halfHeight = 0f;
            float halfWidth = 0f;

            if (_cam != null && _cam.orthographic)
            {
                halfHeight = _cam.orthographicSize;
                halfWidth = halfHeight * _cam.aspect;
            }

            position.x = Mathf.Clamp(position.x, minBounds.x + halfWidth, maxBounds.x - halfWidth);
            position.y = Mathf.Clamp(position.y, minBounds.y + halfHeight, maxBounds.y - halfHeight);
            return position;
        }

        // 씬 뷰에서 데드존과 맵 경계를 시각화
        private void OnDrawGizmosSelected()
        {
            if (deadZone != Vector2.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, new Vector3(deadZone.x, deadZone.y, 0f));
            }

            if (useBounds)
            {
                Gizmos.color = Color.cyan;
                Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
                Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
