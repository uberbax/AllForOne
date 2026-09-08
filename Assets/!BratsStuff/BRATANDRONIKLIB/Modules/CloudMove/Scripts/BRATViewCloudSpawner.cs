using System.Collections.Generic;
using UnityEngine;

namespace BRATANDRONIKLIB
{
    [DisallowMultipleComponent]
    public sealed class BRATViewCloudSpawner : MonoBehaviour
    {
        public Camera targetCamera;
        public SpriteRenderer[] cloudTemplates;

        [Min(0.01f)] public float minSpawnInterval = 18f;
        [Min(0.01f)] public float maxSpawnInterval = 30f;
        [Min(0f)] public float leftSpawnPadding = 0.35f;
        [Min(0f)] public float rightDestroyPadding = 1f;
        [Min(1)] public int maxSpawnedClouds = 5;

        public Vector2 localYRange = new Vector2(1.65f, 2.20f);
        public Vector2 scaleRange = new Vector2(0.85f, 1.25f);
        public Vector2 speedRange = new Vector2(0.065f, 0.10f);

        private readonly List<SpriteRenderer> _spawnedClouds = new List<SpriteRenderer>();
        private float _spawnTimer;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void OnEnable()
        {
            _spawnTimer = 0f;
        }

        private void Update()
        {
            RemoveCloudsPastRightEdge();

            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f || _spawnedClouds.Count >= maxSpawnedClouds)
                return;

            SpawnCloud();
            _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        private void OnDisable()
        {
            for (int i = _spawnedClouds.Count - 1; i >= 0; i--)
            {
                if (_spawnedClouds[i] != null)
                    Destroy(_spawnedClouds[i].gameObject);
            }

            _spawnedClouds.Clear();
        }

        private void SpawnCloud()
        {
            if (targetCamera == null || cloudTemplates == null || cloudTemplates.Length == 0)
                return;

            SpriteRenderer template = cloudTemplates[Random.Range(0, cloudTemplates.Length)];
            if (template == null || template.sprite == null)
                return;

            var cloud = new GameObject("Runtime Cloud");
            Transform cloudTransform = cloud.transform;
            cloudTransform.SetParent(transform, false);
            cloudTransform.localPosition = new Vector3(0f, Random.Range(localYRange.x, localYRange.y), 0f);

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            cloudTransform.localScale = new Vector3(scale, scale, 1f);

            var renderer = cloud.AddComponent<SpriteRenderer>();
            renderer.sprite = template.sprite;
            renderer.color = template.color;
            renderer.sharedMaterial = template.sharedMaterial;
            renderer.sortingLayerID = template.sortingLayerID;
            renderer.sortingOrder = template.sortingOrder;
            renderer.flipX = Random.value > 0.5f;

            float cameraLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, 0f)).x;
            float halfWidth = renderer.bounds.extents.x;
            Vector3 worldPosition = cloudTransform.position;
            worldPosition.x = cameraLeft - halfWidth - leftSpawnPadding;
            cloudTransform.position = worldPosition;

            var mover = cloud.AddComponent<BRATMotionCloudMove>();
            mover.speed = Random.Range(speedRange.x, speedRange.y);

            _spawnedClouds.Add(renderer);
        }

        private void RemoveCloudsPastRightEdge()
        {
            if (targetCamera == null)
                return;

            float cameraRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, 0f)).x;
            for (int i = _spawnedClouds.Count - 1; i >= 0; i--)
            {
                SpriteRenderer cloud = _spawnedClouds[i];
                if (cloud == null)
                {
                    _spawnedClouds.RemoveAt(i);
                    continue;
                }

                if (cloud.bounds.min.x <= cameraRight + rightDestroyPadding)
                    continue;

                Destroy(cloud.gameObject);
                _spawnedClouds.RemoveAt(i);
            }
        }

        private void OnValidate()
        {
            minSpawnInterval = Mathf.Max(0.01f, minSpawnInterval);
            maxSpawnInterval = Mathf.Max(minSpawnInterval, maxSpawnInterval);
            leftSpawnPadding = Mathf.Max(0f, leftSpawnPadding);
            rightDestroyPadding = Mathf.Max(0f, rightDestroyPadding);
            maxSpawnedClouds = Mathf.Max(1, maxSpawnedClouds);

            if (localYRange.x > localYRange.y)
                localYRange = new Vector2(localYRange.y, localYRange.x);
            if (scaleRange.x > scaleRange.y)
                scaleRange = new Vector2(scaleRange.y, scaleRange.x);
            if (speedRange.x > speedRange.y)
                speedRange = new Vector2(speedRange.y, speedRange.x);

            scaleRange.x = Mathf.Max(0.01f, scaleRange.x);
            scaleRange.y = Mathf.Max(scaleRange.x, scaleRange.y);
            speedRange.x = Mathf.Max(0f, speedRange.x);
            speedRange.y = Mathf.Max(speedRange.x, speedRange.y);
        }
    }
}
