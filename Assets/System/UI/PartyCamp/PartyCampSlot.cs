using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.UI;

public sealed class PartyCampSlot : MonoBehaviour
{
    [SerializeField] private Transform previewRoot;
    [SerializeField] private Transform marker;
    [SerializeField] private Button hitButton;
    [SerializeField] private TMP_Text caption;
    [SerializeField] private Material spriteMaterial;
    private PartyCampController owner;
    private RObj hero;
    private GameObject visual;
    private GameObject monster;
    private readonly List<IdleFrames> spriteAnimations = new List<IdleFrames>();
    private sealed class IdleFrames
    {
        public SpriteRenderer renderer;
        public UnoAnim animation;
        public float frameTime;
    }
    public GameObject MonsterInstance => monster;
    private Vector3 markerScale;
    private bool capturedMarkerScale;
    private void Awake() { hitButton.onClick.AddListener(Clicked); }
    private void Clicked() { if (hero != null) owner.ToggleHero(hero); }
    public void Bind(PartyCampController controller, RObj value, int slotIndex)
    {
        owner = controller;
        if (!ReferenceEquals(hero, value))
        {
            ClearVisual(); hero = value;
            if (hero != null) CreateVisual();
        }
        hitButton.interactable = value != null;
        caption.text = value != null ? "-" : "+";
        caption.color = value != null ? Color.white : new Color(.65f,.85f,.76f,.6f);
        if (!capturedMarkerScale) { markerScale = marker.localScale; capturedMarkerScale = true; }
        marker.localScale = markerScale * (value != null ? 1f : .8f);
    }
    public void Project(Camera camera, RectTransform canvas)
    {
        var point = camera.WorldToViewportPoint(transform.position + new Vector3(0, -.35f, 0));
        var rect = (RectTransform)hitButton.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(point.x, point.y);
        rect.anchoredPosition = Vector2.zero;
    }
    private void ClearVisual()
    {
        if (visual != null) { visual.SetActive(false); if (Application.isPlaying) Destroy(visual); else DestroyImmediate(visual); }
        visual = null; monster = null; spriteAnimations.Clear();
    }
    private void CreateVisual()
    {
        var resources = ResourceHolder.instance;
        if (resources == null || !resources.monsters.TryGetValue(hero.dbObj.ID, out var prefab) || prefab == null) return;
        // Instantiate the complete prefab while inactive, before its scripts can enable gameplay behavior.
        visual = new GameObject("Camp Hero " + hero.dbObj.ID);
        visual.SetActive(false);
        visual.transform.SetParent(previewRoot, false);
        monster = Instantiate(prefab, visual.transform, false);
        monster.name = prefab.name;
        monster.transform.localPosition = Vector3.zero;
        foreach (var part in visual.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 2;
        foreach (var script in monster.GetComponentsInChildren<MonoBehaviour>(true))
            if (script != null) script.enabled = false;
        foreach (var collider in monster.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        foreach (var collider in monster.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (var body in monster.GetComponentsInChildren<Rigidbody2D>(true)) body.simulated = false;
        foreach (var body in monster.GetComponentsInChildren<Rigidbody>(true))
        { body.isKinematic = true; body.detectCollisions = false; }
        foreach (var audio in monster.GetComponentsInChildren<AudioSource>(true)) audio.enabled = false;
        foreach (var camera in monster.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        var sorting = visual.AddComponent<SortingGroup>();
        sorting.sortingOrder = 100 - (int)(transform.localPosition.y * 10);
        foreach (var renderer in monster.GetComponentsInChildren<SpriteRenderer>(true))
            renderer.sharedMaterial = spriteMaterial;
        foreach (var animator in monster.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false;
            animator.fireEvents = false;
        }
        // SpriterAnim uses scaled Time.time. Sample its own idle frames here while the world is paused.
        foreach (var spriter in monster.GetComponentsInChildren<SpriterAnim>(true))
        {
            var idle = spriter.anims.Find(a => a.nm == spriter.defaultAnim) ?? spriter.anims.Find(a => a.nm == "idle");
            var renderer = spriter.GetComponentInChildren<SpriteRenderer>(true);
            if (idle == null || idle.sprites == null || idle.sprites.Count == 0 || renderer == null) continue;
            renderer.sprite = idle.sprites[0];
            spriteAnimations.Add(new IdleFrames { renderer = renderer, animation = idle,
                frameTime = Mathf.Max(.01f, spriter.timeFrame / Mathf.Max(.01f, idle.speed)) });
        }
        monster.SetActive(true);
        visual.SetActive(true);
        foreach (var animator in monster.GetComponentsInChildren<Animator>(true))
        {
            if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            var convert = animator.GetComponent<AnimConvert>();
            string idle = convert != null && convert.anims != null && convert.anims.TryGetValue("idle", out var mapped) ? mapped : "idle";
            int hash = Animator.StringToHash(idle);
            if (animator.HasState(0, hash)) animator.Play(hash, 0, 0);
            animator.Update(0);
        }
        var renderers = monster.GetComponentsInChildren<SpriteRenderer>();
        bool hasBounds = false; Bounds bounds = default;
        foreach (var renderer in renderers)
        {
            if (!renderer.enabled || renderer.sprite == null) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds || bounds.size.y <= .001f) return;
        float scale = 1.35f / bounds.size.y;
        visual.transform.localScale = Vector3.one * scale;
        visual.transform.localPosition = new Vector3(previewRoot.position.x - bounds.center.x, previewRoot.position.y - bounds.min.y, 0) * scale;
    }
    private void Update()
    {
        foreach (var idle in spriteAnimations)
        {
            if (idle.renderer == null) continue;
            int frame = (int)(Time.unscaledTime / idle.frameTime) % idle.animation.sprites.Count;
            idle.renderer.sprite = idle.animation.sprites[frame];
        }
    }
    private void OnDestroy() { if (hitButton != null) hitButton.onClick.RemoveListener(Clicked); }
}
