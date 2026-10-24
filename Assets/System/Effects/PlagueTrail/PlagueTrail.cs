using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Distance-sampled, bounded ground corruption. Tracks mainPlayer unless a target is assigned.</summary>
[DisallowMultipleComponent]
public sealed class PlagueTrail : MonoBehaviour
{
    public enum GroundPlane { XY, XZ }
    [SerializeField] private Transform target;
    [SerializeField] private Material surfaceMaterial;
    [SerializeField] private GroundPlane plane = GroundPlane.XY;
    [SerializeField, Min(.1f)] private float radius = .85f;
    [SerializeField, Min(.05f)] private float spacing = .32f;
    [SerializeField, Min(.1f)] private float growDuration = .55f;
    [SerializeField, Min(0f)] private float lifetime = 90f;
    [SerializeField, Min(.1f)] private float fadeDuration = 8f;
    [SerializeField, Range(32, 2048)] private int maxPatches = 512;
    [SerializeField, Min(1f)] private float teleportDistance = 8f;
    [SerializeField] private float surfaceOffset = -.025f;
    [SerializeField] private int sortingOrder = -5;

    private Mesh mesh;
    private MeshRenderer surface;
    private GameObject surfaceObject;
    private MaterialPropertyBlock properties;
    private Vector3[] vertices;
    private List<Vector4> stampData;
    private Vector2[] groundUV;
    private int nextPatch;
    private int patches;
    private int capacity;
    private float clock;
    private bool hasPosition;
    private Vector3 lastObserved;
    private Vector3 lastStamp;
    private Transform trackedTarget;
    private static readonly int TrailTime = Shader.PropertyToID("_TrailTime");
    public int PatchCount => patches;

    private void LateUpdate()
    {
        if (!EnsureSurface()) return;
        clock += Time.deltaTime;
        ApplyProperties();
        Transform follow = target;
        if (follow == null && MainStates.instance != null && MainStates.instance.HasMain())
            follow = MainStates.instance.mainPlayer?.main?.transform;
        if (follow != trackedTarget)
        {
            hasPosition = false;
            trackedTarget = follow;
        }
        if (follow != null) RecordPosition(follow.position);
        else hasPosition = false;
    }

    private bool EnsureSurface()
    {
        if (mesh != null) return true;
        if (surfaceMaterial == null) return false;
        capacity = Mathf.Clamp(maxPatches, 32, 2048);
        vertices = new Vector3[capacity * 4];
        groundUV = new Vector2[capacity * 4];
        stampData = new List<Vector4>(capacity * 4);
        var triangles = new int[capacity * 6];
        for (int i = 0; i < capacity; i++)
        {
            int v = i * 4, t = i * 6;
            for (int j = 0; j < 4; j++) stampData.Add(new Vector4(0, 0, -10000f, 0));
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
            triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        mesh = new Mesh { name = "Plague trail (runtime)" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        mesh.SetUVs(0, stampData);
        mesh.uv2 = groundUV;
        mesh.triangles = triangles;
        surfaceObject = new GameObject("Plague surface", typeof(MeshFilter), typeof(MeshRenderer));
        surfaceObject.layer = gameObject.layer;
        surfaceObject.transform.SetParent(transform, false);
        surfaceObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        surface = surfaceObject.GetComponent<MeshRenderer>();
        surface.sharedMaterial = surfaceMaterial;
        surface.sortingOrder = sortingOrder;
        surface.shadowCastingMode = ShadowCastingMode.Off;
        surface.receiveShadows = false;
        surface.lightProbeUsage = LightProbeUsage.Off;
        surface.reflectionProbeUsage = ReflectionProbeUsage.Off;
        properties = new MaterialPropertyBlock();
        ApplyProperties();
        return true;
    }

    /// <summary>Record a world position; stationary points and teleport gaps add no patches.</summary>
    public void RecordPosition(Vector3 position)
    {
        if (!EnsureSurface()) return;
        Vector3 observed = position;
        if (!hasPosition)
        {
            hasPosition = true;
            lastObserved = lastStamp = position;
            return;
        }
        Vector3 delta = position - lastObserved;
        if (plane == GroundPlane.XY) delta.z = 0; else delta.y = 0;
        lastObserved = observed;
        if (delta.sqrMagnitude > teleportDistance * teleportDistance)
        {
            lastStamp = position;
            return;
        }
        Vector3 remaining = position - lastStamp;
        if (plane == GroundPlane.XY) remaining.z = 0; else remaining.y = 0;
        float distance = remaining.magnitude;
        float step = Mathf.Max(.05f, spacing);
        if (distance < step) return;
        Vector3 direction = remaining / distance;
        int count = Mathf.Min(capacity, Mathf.FloorToInt(distance / step));
        for (int i = 0; i < count; i++)
        {
            lastStamp += direction * step;
            if (plane == GroundPlane.XY) lastStamp.z = position.z; else lastStamp.y = position.y;
            AddPatch(lastStamp);
        }
        mesh.vertices = vertices;
        mesh.SetUVs(0, stampData);
        mesh.uv2 = groundUV;
        mesh.RecalculateBounds();
    }

    private void AddPatch(Vector3 centre)
    {
        // Local deterministic variation leaves UnityEngine.Random/gameplay randomness untouched.
        float seed = Mathf.Repeat((nextPatch + clock * .01f) * .61803399f, 1f);
        float size = Mathf.Max(.1f, radius) * Mathf.Lerp(.85f, 1.16f, seed);
        float angle = seed * Mathf.PI * 2f;
        float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
        int first = nextPatch * 4;
        for (int corner = 0; corner < 4; corner++)
        {
            float x = corner == 0 || corner == 3 ? -1f : 1f;
            float y = corner < 2 ? -1f : 1f;
            Vector2 offset = new Vector2(x * c - y * s, x * s + y * c) * size;
            Vector3 world = centre + (plane == GroundPlane.XY
                ? new Vector3(offset.x, offset.y, surfaceOffset)
                : new Vector3(offset.x, surfaceOffset, offset.y));
            vertices[first + corner] = transform.InverseTransformPoint(world);
            stampData[first + corner] = new Vector4(x, y, clock, seed);
            groundUV[first + corner] = plane == GroundPlane.XY ? new Vector2(world.x, world.y) : new Vector2(world.x, world.z);
        }
        nextPatch = (nextPatch + 1) % capacity;
        patches = Mathf.Min(patches + 1, capacity);
    }

    private void ApplyProperties()
    {
        properties.SetFloat(TrailTime, clock);
        properties.SetFloat("_GrowTime", Mathf.Max(.1f, growDuration));
        properties.SetFloat("_Lifetime", Mathf.Max(0, lifetime));
        properties.SetFloat("_FadeTime", Mathf.Max(.1f, fadeDuration));
        surface.SetPropertyBlock(properties);
    }

    public void ClearTrail()
    {
        hasPosition = false;
        nextPatch = patches = 0;
        if (mesh == null) return;
        System.Array.Clear(vertices, 0, vertices.Length);
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
    }

    public void SetPreviewTime(float seconds)
    {
        if (!EnsureSurface()) return;
        clock = Mathf.Max(0, seconds);
        ApplyProperties();
    }

    private void OnDisable()
    {
        hasPosition = false;
        trackedTarget = null;
        if (surfaceObject != null) surfaceObject.SetActive(false);
    }
    private void OnEnable() { if (surfaceObject != null) surfaceObject.SetActive(true); }
    private void OnDestroy()
    {
        if (mesh != null)
        {
            if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
        if (surfaceObject != null)
        {
            if (Application.isPlaying) Destroy(surfaceObject); else DestroyImmediate(surfaceObject);
        }
    }
}
