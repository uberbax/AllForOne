using UnityEngine;
using UnityEngine.EventSystems;

namespace Animpic.CharacterStudio
{
    [DisallowMultipleComponent]
    public sealed class CharacterStudioPresentation : MonoBehaviour
    {
        public Camera viewCamera;
        public Transform stageRoot;
        public float targetHeight = .98f;
        public float defaultDistance = 3.85f;
        public float minDistance = 2.1f;
        public float maxDistance = 5f;
        public float defaultYaw = 25f;
        public float defaultPitch = 7f;
        [Range(0.5f, 1f)] public float viewportWidth = 1f;
        public bool autoRotate;
        private float yaw, pitch, distance;
        private bool dragging;
        private Vector3 lastPointer;
        private bool initialized;

        public void Configure(Camera camera, Transform root)
        {
            viewCamera = camera;
            stageRoot = root;
            ResetView();
        }
        public void SetAutoRotate(bool value) { autoRotate = value; }
        public void ResetView()
        {
            yaw = defaultYaw; pitch = defaultPitch;
            distance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            initialized = true;
            MoveCamera(true);
        }
        private void Start() { if (!initialized) ResetView(); }
        private void LateUpdate()
        {
            if (!viewCamera) return;
            if (!initialized) ResetView();
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(0) && !overUI) { dragging = true; lastPointer = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (dragging && Input.GetMouseButton(0))
            {
                var pointer = Input.mousePosition;
                if (!overUI)
                {
                    var delta = pointer - lastPointer;
                    yaw += delta.x * 0.3f;
                    pitch = Mathf.Clamp(pitch - delta.y * 0.2f, -8f, 35f);
                }
                lastPointer = pointer;
            }
            if (!overUI) distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.22f, minDistance, maxDistance);
            if (autoRotate && !dragging) yaw += Time.unscaledDeltaTime * 12f;
            MoveCamera(false);
        }
        private void MoveCamera(bool instant)
        {
            if (!viewCamera) return;
            var target = (stageRoot ? stageRoot.position : Vector3.zero) + Vector3.up * targetHeight;
            var position = target + Quaternion.Euler(-pitch, yaw, 0) * (Vector3.forward * distance);
            float blend = instant ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f);
            viewCamera.transform.position = Vector3.Lerp(viewCamera.transform.position, position, blend);
            viewCamera.transform.rotation = Quaternion.Slerp(viewCamera.transform.rotation, Quaternion.LookRotation(target - position), blend);
            viewCamera.rect = new Rect(0, 0, viewportWidth, 1);
        }
    }
}
