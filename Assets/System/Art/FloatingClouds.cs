using System.Collections.Generic;
using UnityEngine;

public class FloatingClouds : MonoBehaviour
{
    public Transform cloudRoot;
    public Transform camLoX;
    public Transform camHiX;
    
    [Header("Cloud Sprites")]
    public List<Sprite> sprites = new List<Sprite>();

    [Header("Spawn")]
    [Min(1)]
    public int cloudCount = 15;

    public Vector2 areaSize = new Vector2(30f, 20f);

    [Header("Movement")]
    public Vector2 direction = new Vector2(1f, -0.2f);

    public Vector2 speedRange = new Vector2(0.5f, 1.5f);

    [Header("Appearance")]
    [Range(0f, 1f)]
    public float alpha = 0.25f;

    public Color cloudColor = Color.black;

    public Vector2 scaleRange = new Vector2(0.8f, 1.5f);

    [Header("Rendering")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    private readonly List<Cloud> clouds = new List<Cloud>();

    private class Cloud
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public float speed;
    }

    private void Start()
    {
        SpawnClouds();
    }

    private void Update()
    {
        MoveClouds();
    }

    private void SpawnClouds()
    {
        if (sprites == null || sprites.Count == 0)
        {
            Debug.LogWarning("FloatingClouds: No sprites assigned.");
            return;
        }

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject go = new GameObject("Cloud_" + i);
            go.transform.SetParent(transform);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            
            Cloud cloud = new Cloud
            {
                transform = go.transform,
                renderer = sr
            };

            SetupCloud(cloud, true);

            clouds.Add(cloud);
        }
    }

    private void SetupCloud(Cloud cloud, bool randomPosition)
    {
        // Random sprite
        cloud.renderer.sprite =
            sprites[Random.Range(0, sprites.Count)];

        // Semi-transparent shadow color
        Color color = cloudColor;
        color.a = alpha;

        cloud.renderer.color = color;

        // Sorting
        cloud.renderer.sortingLayerName = sortingLayerName;
        cloud.renderer.sortingOrder = sortingOrder;

        // Random scale
        float scale = Random.Range(
            scaleRange.x,
            scaleRange.y
        );

        cloud.transform.localScale =
            new Vector3(scale, scale, 1f);

        // Random speed
        cloud.speed = Random.Range(
            speedRange.x,
            speedRange.y
        );

        float halfX = areaSize.x * 0.5f;
        float halfY = areaSize.y * 0.5f;

        if (randomPosition)
        {
            cloud.transform.localPosition = new Vector3(
                Random.Range(-halfX, halfX),
                Random.Range(-halfY, halfY),
                1f
            );
        }
        
        cloud.transform.position = new  Vector3(camLoX.position.x,  cloud.transform.position.y, cloud.transform.position.z);
        
        cloud.transform.SetParent(cloudRoot);
    }

    private void MoveClouds()
    {
        Vector2 dir = direction.normalized;

        float halfX = areaSize.x * 0.5f;
        float halfY = areaSize.y * 0.5f;

        for (int i = 0; i < clouds.Count; i++)
        {
            Cloud cloud = clouds[i];

            cloud.transform.localPosition +=
                (Vector3)(dir * cloud.speed * Time.deltaTime);

            Vector3 p = cloud.transform.localPosition;

            bool outside = cloud.transform.position.x > camHiX.position.x;
            /*
            bool outside =
                p.x > halfX ||
                p.x < -halfX ||
                p.y > halfY ||
                p.y < -halfY;
    */
            if (outside)
            {
                RespawnCloud(cloud, dir);
            }
        }
    }

    private void RespawnCloud(Cloud cloud, Vector2 dir)
    {
        float halfX = areaSize.x * 0.5f;
        float halfY = areaSize.y * 0.5f;

        Vector3 position = cloud.transform.localPosition;

        // Spawn at the opposite edge from movement direction.

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            position.x =
                dir.x > 0
                    ? -halfX
                    : halfX;

            position.y =
                Random.Range(-halfY, halfY);
        }
        else
        {
            position.y =
                dir.y > 0
                    ? -halfY
                    : halfY;

            position.x =
                Random.Range(-halfX, halfX);
        }

        cloud.transform.localPosition = position;

        // Change appearance each time it respawns
        SetupCloud(cloud, false);
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            transform.position,
            new Vector3(
                areaSize.x,
                areaSize.y,
                0f
            )
        );
    }

#endif
}