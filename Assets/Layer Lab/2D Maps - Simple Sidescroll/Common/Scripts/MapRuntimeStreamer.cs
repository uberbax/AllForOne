using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace LayerLab
{
/// <summary>
/// Streams repeating map blocks based on the camera's visible X range.
/// </summary>
[DisallowMultipleComponent]
public sealed class MapRuntimeStreamer : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Streaming")]
    [SerializeField] private bool initializeOnStart = true;
    [SerializeField] private float preloadViewWidth = 1f;
    [SerializeField] private float keepAliveViewWidth = 1.5f;
    [SerializeField] private bool usePool = true;

    [Header("Depth")]
    [FormerlySerializedAs("lockLastDepthToCameraX")]
    [SerializeField] private bool fixLastDepthToCameraX = true;

    private readonly List<LayerStream> layers = new List<LayerStream>();
    private bool initialized;

    public Camera TargetCamera
    {
        get { return targetCamera; }
        set { targetCamera = value; }
    }

    public bool InitializeOnStart
    {
        get { return initializeOnStart; }
        set { initializeOnStart = value; }
    }

    public float PreloadViewWidth
    {
        get { return preloadViewWidth; }
        set { preloadViewWidth = Mathf.Max(0f, value); }
    }

    public float KeepAliveViewWidth
    {
        get { return keepAliveViewWidth; }
        set { keepAliveViewWidth = Mathf.Max(0f, value); }
    }

    /// <summary>
    /// Builds layer data once and immediately fills the camera preload range.
    /// </summary>
    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        ResolveTargetCamera();
        BuildLayers();
        initialized = true;
        Refresh();
    }

    /// <summary>
    /// Spawns visible blocks, releases distant blocks, and locks the fixed depth to the camera when enabled.
    /// </summary>
    public void Refresh()
    {
        if (!initialized)
        {
            return;
        }

        ResolveTargetCamera();
        if (targetCamera == null)
        {
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            LayerStream layer = layers[i];
            if (layer.LockedToCameraX)
            {
                Vector3 position = layer.Root.position;
                position.x = targetCamera.transform.position.x;
                layer.Root.position = position;
                continue;
            }

            if (!TryGetCameraXRangeAtZ(layer.WorldPlaneZ, out float viewLeft, out float viewRight))
            {
                continue;
            }

            float viewWidth = Mathf.Abs(viewRight - viewLeft);
            float preload = viewWidth * preloadViewWidth;
            float keepAlive = viewWidth * keepAliveViewWidth;

            layer.Update(viewLeft - preload, viewRight + preload, viewLeft - keepAlive, viewRight + keepAlive, usePool);
        }
    }

    private void Reset()
    {
        ResolveTargetCamera();
    }

    private void OnValidate()
    {
        ResolveTargetCamera();
    }

    private void Awake()
    {
        ResolveTargetCamera();
    }

    private void Start()
    {
        if (initializeOnStart)
        {
            Initialize();
        }
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            Initialize();
        }

        Refresh();
    }

    private void ResolveTargetCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void BuildLayers()
    {
        layers.Clear();

        // Only numeric direct children are treated as depth roots.
        List<Transform> depthRoots = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (int.TryParse(child.name, out _))
            {
                depthRoots.Add(child);
            }
        }

        depthRoots.Sort((a, b) => int.Parse(a.name).CompareTo(int.Parse(b.name)));
        int lastDepth = depthRoots.Count == 0 ? int.MinValue : int.Parse(depthRoots[depthRoots.Count - 1].name);

        for (int i = 0; i < depthRoots.Count; i++)
        {
            Transform root = depthRoots[i];
            int depth = int.Parse(root.name);
            bool locked = fixLastDepthToCameraX && depth == lastDepth;
            LayerStream layer = new LayerStream(root, locked);
            layers.Add(layer);

            if (!locked)
            {
                layer.CapturePattern();
            }
        }
    }

    private bool TryGetCameraXRangeAtZ(float planeZ, out float left, out float right)
    {
        left = 0f;
        right = 0f;

        // Perspective cameras need ray-plane intersection to measure the visible width at each depth.
        Ray leftRay = targetCamera.ViewportPointToRay(new Vector3(0f, 0.5f, 0f));
        Ray rightRay = targetCamera.ViewportPointToRay(new Vector3(1f, 0.5f, 0f));
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));

        if (!plane.Raycast(leftRay, out float leftDistance) || !plane.Raycast(rightRay, out float rightDistance))
        {
            return false;
        }

        left = leftRay.GetPoint(leftDistance).x;
        right = rightRay.GetPoint(rightDistance).x;
        if (left > right)
        {
            float temp = left;
            left = right;
            right = temp;
        }

        return true;
    }

    private sealed class LayerStream
    {
        private readonly List<BlockTemplate> templates = new List<BlockTemplate>();
        private readonly Dictionary<BlockKey, Transform> activeBlocks = new Dictionary<BlockKey, Transform>();
        private readonly Dictionary<int, Stack<Transform>> pools = new Dictionary<int, Stack<Transform>>();
        private readonly List<BlockKey> removeBuffer = new List<BlockKey>();

        private bool captured;
        private float minX = float.PositiveInfinity;
        private float maxX = float.NegativeInfinity;
        private float cycleWidth = 1f;
        private float worldPlaneZ;

        public LayerStream(Transform root, bool lockedToCameraX)
        {
            Root = root;
            LockedToCameraX = lockedToCameraX;
        }

        public Transform Root { get; }
        public bool LockedToCameraX { get; }
        public float WorldPlaneZ { get { return worldPlaneZ; } }

        /// <summary>
        /// Captures the existing Block children as the repeating pattern for this depth.
        /// </summary>
        public void CapturePattern()
        {
            if (captured)
            {
                return;
            }

            List<Transform> sourceBlocks = new List<Transform>();
            for (int i = 0; i < Root.childCount; i++)
            {
                Transform child = Root.GetChild(i);
                if (child.name == "Block")
                {
                    sourceBlocks.Add(child);
                }
            }

            if (sourceBlocks.Count == 0)
            {
                return;
            }

            // Sorting by bounds keeps intentionally spaced blocks in their authored order.
            sourceBlocks.Sort((a, b) =>
            {
                GetLocalBoundsX(Root, a, out float aMin, out _);
                GetLocalBoundsX(Root, b, out float bMin, out _);
                return aMin.CompareTo(bMin);
            });

            float planeZSum = 0f;
            for (int i = 0; i < sourceBlocks.Count; i++)
            {
                Transform block = sourceBlocks[i];
                GetLocalBoundsX(Root, block, out float blockMinX, out float blockMaxX);
                minX = Mathf.Min(minX, blockMinX);
                maxX = Mathf.Max(maxX, blockMaxX);
                planeZSum += GetWorldPlaneZ(block);

                templates.Add(new BlockTemplate(
                    block.gameObject,
                    block.localPosition,
                    block.localRotation,
                    block.localScale,
                    block.gameObject.activeSelf,
                    blockMinX,
                    blockMaxX));
            }

            worldPlaneZ = planeZSum / sourceBlocks.Count;
            cycleWidth = CalculateCycleWidth(Root, sourceBlocks);

            // Authored blocks become cycle 0 instead of being duplicated at startup.
            for (int i = 0; i < sourceBlocks.Count; i++)
            {
                Transform block = sourceBlocks[i];
                block.gameObject.name = "Block";
                activeBlocks.Add(new BlockKey(i, 0), block);
            }

            captured = true;
        }

        /// <summary>
        /// Ensures blocks exist across the spawn range and releases blocks outside the keep-alive range.
        /// </summary>
        public void Update(float spawnLeft, float spawnRight, float keepLeft, float keepRight, bool usePool)
        {
            if (templates.Count == 0)
            {
                return;
            }

            float localSpawnLeft = WorldXToLocalX(spawnLeft);
            float localSpawnRight = WorldXToLocalX(spawnRight);
            float localKeepLeft = WorldXToLocalX(keepLeft);
            float localKeepRight = WorldXToLocalX(keepRight);
            SortRange(ref localSpawnLeft, ref localSpawnRight);
            SortRange(ref localKeepLeft, ref localKeepRight);

            for (int i = 0; i < templates.Count; i++)
            {
                BlockTemplate template = templates[i];
                int firstCycle = Mathf.FloorToInt((localSpawnLeft - template.MaxX) / cycleWidth) - 1;
                int lastCycle = Mathf.CeilToInt((localSpawnRight - template.MinX) / cycleWidth) + 1;

                for (int cycle = firstCycle; cycle <= lastCycle; cycle++)
                {
                    float blockMinX = template.MinX + cycleWidth * cycle;
                    float blockMaxX = template.MaxX + cycleWidth * cycle;
                    if (blockMaxX < localSpawnLeft || blockMinX > localSpawnRight)
                    {
                        continue;
                    }

                    EnsureBlock(new BlockKey(i, cycle), template);
                }
            }

            removeBuffer.Clear();
            foreach (KeyValuePair<BlockKey, Transform> pair in activeBlocks)
            {
                BlockTemplate template = templates[pair.Key.TemplateIndex];
                float blockMinX = template.MinX + cycleWidth * pair.Key.Cycle;
                float blockMaxX = template.MaxX + cycleWidth * pair.Key.Cycle;
                if (blockMaxX < localKeepLeft || blockMinX > localKeepRight)
                {
                    removeBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeBuffer.Count; i++)
            {
                ReleaseBlock(removeBuffer[i], usePool);
            }
        }

        private void EnsureBlock(BlockKey key, BlockTemplate template)
        {
            if (activeBlocks.ContainsKey(key))
            {
                return;
            }

            Transform block;
            if (pools.TryGetValue(key.TemplateIndex, out Stack<Transform> pool) && pool.Count > 0)
            {
                // Reuse inactive blocks to avoid allocation spikes while the camera scrolls.
                block = pool.Pop();
            }
            else
            {
                block = UnityEngine.Object.Instantiate(template.Source, Root).transform;
            }

            block.name = "Block";
            block.SetParent(Root, false);
            block.localPosition = template.LocalPosition + Vector3.right * (cycleWidth * key.Cycle);
            block.localRotation = template.LocalRotation;
            block.localScale = template.LocalScale;
            block.gameObject.SetActive(template.ActiveSelf);

            activeBlocks.Add(key, block);
        }

        private void ReleaseBlock(BlockKey key, bool usePool)
        {
            Transform block = activeBlocks[key];
            activeBlocks.Remove(key);

            if (usePool || block.gameObject == templates[key.TemplateIndex].Source)
            {
                block.gameObject.SetActive(false);
                if (!pools.TryGetValue(key.TemplateIndex, out Stack<Transform> pool))
                {
                    pool = new Stack<Transform>();
                    pools.Add(key.TemplateIndex, pool);
                }

                pool.Push(block);
            }
            else
            {
                UnityEngine.Object.Destroy(block.gameObject);
            }
        }

        private float WorldXToLocalX(float worldX)
        {
            return Root.InverseTransformPoint(new Vector3(worldX, Root.position.y, Root.position.z)).x;
        }

        private static void SortRange(ref float left, ref float right)
        {
            if (left <= right)
            {
                return;
            }

            float temp = left;
            left = right;
            right = temp;
        }

        private static float CalculateCycleWidth(Transform root, List<Transform> sortedBlocks)
        {
            if (sortedBlocks.Count == 1)
            {
                GetLocalBoundsX(root, sortedBlocks[0], out float min, out float max);
                return Mathf.Max(0.001f, max - min);
            }

            GetLocalBoundsX(root, sortedBlocks[0], out float firstMinX, out _);
            GetLocalBoundsX(root, sortedBlocks[sortedBlocks.Count - 2], out float previousMinX, out _);
            GetLocalBoundsX(root, sortedBlocks[sortedBlocks.Count - 1], out float lastMinX, out _);
            // Preserve the authored gap between the last two blocks when wrapping to the next cycle.
            float seamStep = lastMinX - previousMinX;
            return Mathf.Max(0.001f, lastMinX - firstMinX + seamStep);
        }

        private static float GetWorldPlaneZ(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return target.position.z;
            }

            float sum = 0f;
            for (int i = 0; i < renderers.Length; i++)
            {
                sum += renderers[i].bounds.center.z;
            }

            return sum / renderers.Length;
        }

        private static void GetLocalBoundsX(Transform root, Transform target, out float min, out float max)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                min = target.localPosition.x;
                max = target.localPosition.x;
                return;
            }

            min = float.PositiveInfinity;
            max = float.NegativeInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                Bounds bounds = renderers[i].bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;
                EncapsulateX(root, center + new Vector3(-extents.x, -extents.y, -extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(-extents.x, -extents.y, extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(-extents.x, extents.y, -extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(-extents.x, extents.y, extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(extents.x, -extents.y, -extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(extents.x, -extents.y, extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(extents.x, extents.y, -extents.z), ref min, ref max);
                EncapsulateX(root, center + new Vector3(extents.x, extents.y, extents.z), ref min, ref max);
            }
        }

        private static void EncapsulateX(Transform root, Vector3 worldPoint, ref float min, ref float max)
        {
            float x = root.InverseTransformPoint(worldPoint).x;
            min = Mathf.Min(min, x);
            max = Mathf.Max(max, x);
        }

        private readonly struct BlockKey : IEquatable<BlockKey>
        {
            public readonly int TemplateIndex;
            public readonly int Cycle;

            public BlockKey(int templateIndex, int cycle)
            {
                TemplateIndex = templateIndex;
                Cycle = cycle;
            }

            public bool Equals(BlockKey other)
            {
                return TemplateIndex == other.TemplateIndex && Cycle == other.Cycle;
            }

            public override bool Equals(object obj)
            {
                return obj is BlockKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (TemplateIndex * 397) ^ Cycle;
                }
            }
        }
    }

    private sealed class BlockTemplate
    {
        public BlockTemplate(GameObject source, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, bool activeSelf, float minX, float maxX)
        {
            Source = source;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            LocalScale = localScale;
            ActiveSelf = activeSelf;
            MinX = minX;
            MaxX = maxX;
            source.name = "Block";
        }

        public GameObject Source { get; }
        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }
        public Vector3 LocalScale { get; }
        public bool ActiveSelf { get; }
        public float MinX { get; }
        public float MaxX { get; }
    }
}
}
