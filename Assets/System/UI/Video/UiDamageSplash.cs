using System;
using UnityEngine;
using UnityEngine.UI;

// Plays the supplied blood flipbooks in the same Canvas as the character.
public sealed class UiDamageSplash : MonoBehaviour
{
    [Serializable] private sealed class Splash { public Texture[] frames; }
    [SerializeField] private Splash[] splashes;
    [SerializeField, Min(1)] private float framesPerSecond = 24;
    [SerializeField, Min(.1f)] private float sizeRelativeToBody = .85f;
    private sealed class Layer
    {
        public RawImage image;
        public Texture[] frames;
        public float start;
        public Vector3 localCenter, scale;
        public Quaternion rotation;
    }
    private readonly Layer[] layers = new Layer[3];
    private Transform feet, head;
    private int nextLayer;

    private void Awake() { feet = transform.Find("legs"); head = transform.Find("head"); }

    public void Changed(float deltaHp)
    {
        if (deltaHp >= 0 || !isActiveAndEnabled || splashes == null || splashes.Length == 0) return;
        var frames = splashes[UnityEngine.Random.Range(0, splashes.Length)]?.frames;
        if (frames == null || frames.Length == 0) return;
        int index = nextLayer++ % layers.Length;
        var layer = layers[index];
        if (layer == null)
        {
            var go = new GameObject("Blood Splash " + index, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            layer = layers[index] = new Layer { image = go.GetComponent<RawImage>() };
            layer.image.raycastTarget = false;
            layer.image.color = new Color(1f / 1.5f, 1f / 1.5f, 1f / 1.5f, 1);
        }
        var rect = layer.image.rectTransform;
        var rootRect = transform as RectTransform;
        Vector3 bottom = feet != null ? transform.InverseTransformPoint(feet.position) : Vector3.zero;
        Vector3 top = head != null ? transform.InverseTransformPoint(head.position) : Vector3.up * (rootRect != null ? rootRect.rect.height : 1);
        float height = Mathf.Max(.01f, Vector3.Distance(bottom, top));
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        layer.localCenter = Vector3.Lerp(bottom, top, .6f);
        rect.sizeDelta = Vector2.one * height * sizeRelativeToBody / 1.5f;
        layer.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-25f, 25f));
        layer.scale = new Vector3(UnityEngine.Random.value < .5f ? -1 : 1, 1, 1);
        PlaceBehind(layer);
        layer.frames = frames;
        layer.start = Time.time;
        layer.image.texture = frames[0];
        layer.image.enabled = true;
    }

    private void LateUpdate()
    {
        foreach (var layer in layers)
        {
            if (layer == null || !layer.image.enabled) continue;
            int frame = Mathf.FloorToInt((Time.time - layer.start) * framesPerSecond);
            if (frame >= layer.frames.Length) { layer.image.enabled = false; continue; }
            layer.image.texture = layer.frames[frame];
            PlaceBehind(layer);
        }
    }

    private void PlaceBehind(Layer layer)
    {
        var rect = layer.image.rectTransform;
        if (rect.parent != transform.parent) rect.SetParent(transform.parent, false);
        rect.SetPositionAndRotation(transform.TransformPoint(layer.localCenter), transform.rotation * layer.rotation);
        rect.localScale = Vector3.Scale(transform.localScale, layer.scale);
        // Children of a RawImage always draw over it, so use an earlier sibling in the Canvas.
        if (rect.GetSiblingIndex() > transform.GetSiblingIndex()) rect.SetSiblingIndex(transform.GetSiblingIndex());
    }

    private void OnDestroy()
    {
        foreach (var layer in layers) if (layer != null && layer.image != null) Destroy(layer.image.gameObject);
    }

    private void OnDisable()
    {
        foreach (var layer in layers) if (layer != null && layer.image != null) layer.image.enabled = false;
    }
}
