using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SpriteRigToUI : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private Transform sourceRoot;

    [Header("UI")]
    [SerializeField] private RectTransform uiParent;

    [Tooltip("How many UI pixels correspond to 1 Unity unit.")]
    [SerializeField] private float unitsToPixels = 100f;

    [Header("Sync")]
    [SerializeField] private bool copyScale = true;
    [SerializeField] private bool copyActiveState = true;
    [SerializeField] private bool copySpriteEveryFrame = true;
    [SerializeField] private bool copyColorEveryFrame = true;
    [SerializeField] private bool syncSortingOrder = true;

    private RectTransform generatedRoot;

    private readonly List<Node> nodes = new();
    private readonly List<RendererNode> rendererNodes = new();

    private class Node
    {
        public Transform source;
        public RectTransform target;

        public SpriteRenderer sourceRenderer;
        public Image targetImage;
    }

    private class RendererNode
    {
        public Node node;

        // Used to keep sorting stable when orders are equal.
        public int creationIndex;
    }

    private void Start()
    {
        if (sourceRoot != null)
            Build(sourceRoot);
    }

    public RectTransform Build(Transform source)
    {
        Clear();

        sourceRoot = source;

        if (sourceRoot == null)
        {
            Debug.LogError("SpriteRigToUI: Source root is null.");
            return null;
        }

        if (uiParent == null)
        {
            Debug.LogError("SpriteRigToUI: UI parent is null.");
            return null;
        }

        generatedRoot = CreateRecursive(
            sourceRoot,
            uiParent
        );

        generatedRoot.anchoredPosition = Vector2.zero;

        SyncAll();

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

        SpriteRenderer spriteRenderer =
            source.GetComponent<SpriteRenderer>();

        Image image = null;

        if (spriteRenderer != null)
        {
            image = go.AddComponent<Image>();

            image.sprite = spriteRenderer.sprite;
            image.color = spriteRenderer.color;
            image.raycastTarget = false;
            image.enabled = spriteRenderer.enabled;

            if (spriteRenderer.sprite != null)
                image.SetNativeSize();
        }

        Node node = new Node
        {
            source = source,
            target = rect,
            sourceRenderer = spriteRenderer,
            targetImage = image
        };

        nodes.Add(node);

        if (spriteRenderer != null)
        {
            rendererNodes.Add(new RendererNode
            {
                node = node,
                creationIndex = rendererNodes.Count
            });
        }

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
        SyncAll();
    }

    private void SyncAll()
    {
        SyncPose();

        if (syncSortingOrder)
            SyncSorting();
    }

    private void SyncPose()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Node node = nodes[i];

            if (node.source == null ||
                node.target == null)
            {
                continue;
            }

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

        target.anchoredPosition = new Vector2(
            source.localPosition.x * unitsToPixels,
            source.localPosition.y * unitsToPixels
        );

        target.localRotation = Quaternion.Euler(
            0f,
            0f,
            source.localEulerAngles.z
        );

        if (copyScale)
        {
            Vector3 scale = source.localScale;

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
            if (target.gameObject.activeSelf !=
                source.gameObject.activeSelf)
            {
                target.gameObject.SetActive(
                    source.gameObject.activeSelf
                );
            }
        }
    }

    private void SyncRenderer(Node node)
    {
        SpriteRenderer sr = node.sourceRenderer;
        Image image = node.targetImage;

        image.enabled = sr.enabled;

        if (copySpriteEveryFrame &&
            image.sprite != sr.sprite)
        {
            image.sprite = sr.sprite;

            if (sr.sprite != null)
                image.SetNativeSize();
        }

        if (copyColorEveryFrame)
        {
            image.color = sr.color;
        }
    }

    private void SyncSorting()
    {
        /*
         * Important:
         *
         * We don't want to change bone hierarchy,
         * because bones depend on their parent transforms.
         *
         * So instead of changing the actual RectTransform
         * hierarchy, Image objects should ideally be rendered
         * using separate Canvas components.
         *
         * Each sprite gets its own Canvas, and Canvas.sortingOrder
         * reproduces SpriteRenderer.sortingOrder.
         */

        for (int i = 0; i < rendererNodes.Count; i++)
        {
            RendererNode rendererNode = rendererNodes[i];

            if (rendererNode.node.targetImage == null ||
                rendererNode.node.sourceRenderer == null)
            {
                continue;
            }

            EnsureCanvas(rendererNode);
        }
    }

    private void EnsureCanvas(RendererNode rendererNode)
    {
        Node node = rendererNode.node;

        Canvas canvas =
            node.target.GetComponent<Canvas>();

        if (canvas == null)
        {
            canvas = node.target.gameObject.AddComponent<Canvas>();

            // Required so nested Canvas controls its own order.
            canvas.overrideSorting = true;
        }

        SpriteRenderer sr = node.sourceRenderer;

        canvas.overrideSorting = true;

        // Map SpriteRenderer sorting layer/order to Canvas.
        canvas.sortingLayerID = sr.sortingLayerID;
        canvas.sortingOrder = sr.sortingOrder;
    }

    public void Clear()
    {
        nodes.Clear();
        rendererNodes.Clear();

        if (generatedRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(
                    generatedRoot.gameObject
                );
            }
            else
            {
                DestroyImmediate(
                    generatedRoot.gameObject
                );
            }
        }

        generatedRoot = null;
    }
}