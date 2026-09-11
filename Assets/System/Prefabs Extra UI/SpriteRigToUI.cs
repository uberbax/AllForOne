using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpriteRigToUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Transform sourceRoot;

    [Header("UI")]
    [SerializeField] private RectTransform uiParent;

    [Tooltip("How many UI pixels correspond to 1 Unity world/local unit.")]
    [SerializeField] private float unitsToPixels = 100f;

    [Header("Options")]
    [SerializeField] private bool copyScale = true;
    [SerializeField] private bool copyActiveState = true;
    [SerializeField] private bool copySpriteEveryFrame = true;
    [SerializeField] private bool copyColorEveryFrame = true;

    private readonly List<Node> nodes = new();

    private RectTransform generatedRoot;

    private class Node
    {
        public Transform source;
        public RectTransform target;

        public SpriteRenderer sourceRenderer;
        public Image targetImage;
    }

    private void Start()
    {
        if (sourceRoot != null)
            Build(sourceRoot);
    }

    /// <summary>
    /// Creates a UI copy of the supplied transform hierarchy.
    /// </summary>
    public RectTransform Build(Transform source)
    {
        Clear();

        sourceRoot = source;

        if (sourceRoot == null)
        {
            Debug.LogError("Source root is null.");
            return null;
        }

        if (uiParent == null)
        {
            Debug.LogError("UI Parent is null.");
            return null;
        }

        generatedRoot = CreateRecursive(sourceRoot, uiParent);

        // Make the UI root itself start at zero.
        generatedRoot.anchoredPosition = Vector2.zero;

        SyncPose();

        return generatedRoot;
    }

    private RectTransform CreateRecursive(
        Transform source,
        RectTransform parent)
    {
        GameObject go = new GameObject(
            source.name,
            typeof(RectTransform)
        );

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        SpriteRenderer sr = source.GetComponent<SpriteRenderer>();

        Image image = null;

        if (sr != null)
        {
            image = go.AddComponent<Image>();

            image.sprite = sr.sprite;
            image.color = sr.color;
            image.raycastTarget = false;

            if (sr.sprite != null)
                image.SetNativeSize();

            image.enabled = sr.enabled;
        }

        Node node = new Node
        {
            source = source,
            target = rect,
            sourceRenderer = sr,
            targetImage = image
        };

        nodes.Add(node);

        for (int i = 0; i < source.childCount; i++)
        {
            CreateRecursive(
                source.GetChild(i),
                rect
            );
        }

        return rect;
    }

    private void LateUpdate()
    {
        SyncPose();
    }

    private void SyncPose()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];

            if (node.source == null || node.target == null)
                continue;

            SyncTransform(node);

            if (node.sourceRenderer != null &&
                node.targetImage != null)
            {
                SyncRenderer(node);
            }
        }
    }

    private void SyncTransform(Node node)
    {
        Transform source = node.source;
        RectTransform target = node.target;

        // World/local Unity units -> UI pixels
        target.anchoredPosition = new Vector2(
            source.localPosition.x * unitsToPixels,
            source.localPosition.y * unitsToPixels
        );

        // For 2D animation we generally only care about Z rotation.
        target.localRotation = Quaternion.Euler(
            0f,
            0f,
            source.localEulerAngles.z
        );

        if (copyScale)
        {
            Vector3 scale = source.localScale;

            // SpriteRenderer flip is not part of transform scale,
            // so reproduce it here.
            if (node.sourceRenderer != null)
            {
                if (node.sourceRenderer.flipX)
                    scale.x *= -1f;

                if (node.sourceRenderer.flipY)
                    scale.y *= -1f;
            }

            target.localScale = new Vector3(
                scale.x,
                scale.y,
                1f
            );
        }

        if (copyActiveState)
        {
            target.gameObject.SetActive(
                source.gameObject.activeSelf
            );
        }
    }

    private void SyncRenderer(Node node)
    {
        SpriteRenderer sr = node.sourceRenderer;
        Image image = node.targetImage;

        image.enabled = sr.enabled;

        if (copySpriteEveryFrame)
        {
            if (image.sprite != sr.sprite)
            {
                image.sprite = sr.sprite;

                if (sr.sprite != null)
                    image.SetNativeSize();
            }
        }

        if (copyColorEveryFrame)
            image.color = sr.color;
    }

    public void Clear()
    {
        nodes.Clear();

        if (generatedRoot != null)
        {
            if (Application.isPlaying)
                Destroy(generatedRoot.gameObject);
            else
                DestroyImmediate(generatedRoot.gameObject);
        }

        generatedRoot = null;
    }
}