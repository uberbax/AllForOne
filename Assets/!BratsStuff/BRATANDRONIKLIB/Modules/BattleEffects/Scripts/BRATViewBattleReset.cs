using System.Collections.Generic;
using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATViewBattleReset : MonoBehaviour
    {
        public Camera targetCamera;
        public Transform[] sceneRoots;

        public event System.Action BeforeReset;

        private TransformState[] _transforms;
        private SpriteState[] _sprites;
        private TransformState _cameraTransform;
        private float _cameraSize;
        private float _cameraFieldOfView;
        private bool _resetting;

        private struct TransformState
        {
            public Transform target;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;

            public TransformState(Transform value)
            {
                target = value;
                position = value.localPosition;
                rotation = value.localRotation;
                scale = value.localScale;
            }

            public void Restore()
            {
                if (!target) return;
                target.localPosition = position;
                target.localRotation = rotation;
                target.localScale = scale;
            }
        }

        private struct SpriteState
        {
            public SpriteRenderer target;
            public Color color;
            public int sortingLayer;
            public int sortingOrder;
            public bool enabled;

            public SpriteState(SpriteRenderer value)
            {
                target = value;
                color = value.color;
                sortingLayer = value.sortingLayerID;
                sortingOrder = value.sortingOrder;
                enabled = value.enabled;
            }

            public void Restore()
            {
                if (!target) return;
                target.color = color;
                target.sortingLayerID = sortingLayer;
                target.sortingOrder = sortingOrder;
                target.enabled = enabled;
            }
        }

        private void Awake()
        {
            var transforms = new List<TransformState>();
            var sprites = new List<SpriteState>();
            var seen = new HashSet<Transform>();

            if (sceneRoots != null)
            {
                for (int i = 0; i < sceneRoots.Length; i++)
                {
                    Transform root = sceneRoots[i];
                    if (!root || !root.gameObject.activeInHierarchy) continue;

                    Transform[] children = root.GetComponentsInChildren<Transform>(false);
                    for (int j = 0; j < children.Length; j++)
                    {
                        Transform child = children[j];
                        if (!child.gameObject.activeInHierarchy || !seen.Add(child)) continue;
                        transforms.Add(new TransformState(child));
                        if (child.TryGetComponent(out SpriteRenderer sprite))
                            sprites.Add(new SpriteState(sprite));
                    }
                }
            }

            _transforms = transforms.ToArray();
            _sprites = sprites.ToArray();
            if (targetCamera)
            {
                _cameraTransform = new TransformState(targetCamera.transform);
                _cameraSize = targetCamera.orthographicSize;
                _cameraFieldOfView = targetCamera.fieldOfView;
            }
        }

        public void ResetScene()
        {
            if (_transforms == null || _resetting) return;
            _resetting = true;
            try
            {
                BeforeReset?.Invoke();
            }
            finally
            {
                RestoreScene();
                _resetting = false;
            }
        }

        private void RestoreScene()
        {

            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i].Restore();
            for (int i = 0; i < _sprites.Length; i++)
                _sprites[i].Restore();

            if (targetCamera && _cameraTransform.target == targetCamera.transform)
            {
                _cameraTransform.Restore();
                targetCamera.orthographicSize = _cameraSize;
                targetCamera.fieldOfView = _cameraFieldOfView;
            }
        }

        private void OnDisable()
        {
            ResetScene();
        }
    }
}
