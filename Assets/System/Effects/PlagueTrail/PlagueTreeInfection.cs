using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Withers nearby trees using a spatial grid and squared distances; preserves original bounds.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(PlagueTrail))]
public sealed class PlagueTreeInfection : MonoBehaviour
{
    [SerializeField] private PlagueTreeCatalog catalog;
    [SerializeField] private Material witheredMaterial;
    [SerializeField, Min(0f)] private float transitionDuration = 1.2f;
    [SerializeField, Min(.5f)] private float cellSize = 2f;
    [SerializeField, HideInInspector] private float trunkRadius = .10f; // Legacy prefab data; unused by distance checks.

    [SerializeField, Min(.01f), InspectorName("Wither Radius")] private float playerContactRadius = 1.5f;
    private float nextDistanceCheck;

    private sealed class Tree
    {
        public SpriteRenderer renderer;
        public Sprite living;
        public Sprite dead;
        public Vector3 root;
        public bool infected;
    }
    private sealed class Transition
    {
        public SpriteRenderer source;
        public SpriteRenderer ghost;
        public Sprite dead;
        public Color originalColor;
        public float elapsed;
        public Bounds originalBounds;
        public Bounds originalWorldBounds;
    }
    private PlagueTrail trail;
    private bool indexed;
    private readonly Dictionary<Sprite, Sprite> replacements = new Dictionary<Sprite, Sprite>();
    private readonly Dictionary<Vector2Int, List<Tree>> grid = new Dictionary<Vector2Int, List<Tree>>();
    private readonly List<Transition> transitions = new List<Transition>();
    public int InfectedTreeCount { get; private set; }

    private void OnEnable()
    {
        trail = GetComponent<PlagueTrail>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        indexed = false;
        nextDistanceCheck=0;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => indexed = false;

    /// <summary>Call after spawning or moving trees at runtime. Scene loads refresh automatically.</summary>
    public void RefreshTrees()
    {
        replacements.Clear();
        if (catalog != null)
            foreach (var pair in catalog.replacements)
                if (pair.living != null && pair.dead != null) replacements[pair.living] = pair.dead;
        grid.Clear();
        if (replacements.Count > 0)
        {
            foreach (var renderer in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.sprite == null || !replacements.TryGetValue(renderer.sprite, out var dead)) continue;
                // The sprite's lower centre is the foot of the trunk, independent of its pivot.
                Bounds bounds = renderer.sprite.bounds;
                Vector3 localRoot = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * .025f, 0);
                if (renderer.flipX) localRoot.x = -localRoot.x;
                if (renderer.flipY) localRoot.y = -localRoot.y;
                Vector3 root = renderer.transform.TransformPoint(localRoot);
                var cell = Cell(trail.GroundCoordinates(root));
                if (!grid.TryGetValue(cell, out var bucket)) grid[cell] = bucket = new List<Tree>();
                bucket.Add(new Tree { renderer = renderer, living = renderer.sprite, dead = dead, root = root,
                    infected = IsTransitioning(renderer) });
            }
        }
        indexed = true;
    }
    private bool IsTransitioning(SpriteRenderer renderer)
    {
        foreach (var transition in transitions) if (transition.source == renderer) return true;
        return false;
    }
    private Vector2Int Cell(Vector2 point)
    {
        float size = Mathf.Max(.5f, cellSize);
        return new Vector2Int(Mathf.FloorToInt(point.x / size), Mathf.FloorToInt(point.y / size));
    }
    private void Update()
    {
        var state=MainStates.instance;
        if(Time.time>=nextDistanceCheck && state!=null && state.HasMain())
        {
            var player=state.mainPlayer;
            if(player!=null && player.main!=null)
            {
                nextDistanceCheck=Time.time+.1f;
                CheckPlayerContact(player.main.transform.position);
            }
        }
        for (int i = transitions.Count - 1; i >= 0; i--)
        {
            var transition = transitions[i];
            if (transition.source == null)
            {
                if (transition.ghost != null) Destroy(transition.ghost.gameObject);
                transitions.RemoveAt(i);
                continue;
            }
            transition.elapsed += Time.deltaTime;
            float t = transitionDuration <= 0 ? 1 : Mathf.SmoothStep(0, 1, Mathf.Clamp01(transition.elapsed / transitionDuration));
            Color living = transition.originalColor; living.a *= 1 - t;
            Color dead = transition.originalColor; dead.a *= t;
            transition.source.color = living;
            if (transition.ghost != null) transition.ghost.color = dead;
            if (t >= 1)
            {
                Finish(transition);
                transitions.RemoveAt(i);
            }
        }
    }
    private void CheckPlayerContact(Vector3 position)
    {
        if(!indexed) RefreshTrees();
        Vector2 point=trail.GroundCoordinates(position);
        float radius=Mathf.Max(.01f,playerContactRadius);
        float radiusSquared=radius*radius;
        var lo=Cell(point-Vector2.one*radius);
        var hi=Cell(point+Vector2.one*radius);
        for(int x=lo.x;x<=hi.x;x++)
        for(int y=lo.y;y<=hi.y;y++)
        {
            if(!grid.TryGetValue(new Vector2Int(x,y),out var bucket)) continue;
            foreach(var tree in bucket)
            {
                if(tree.infected || tree.renderer==null || !tree.renderer.enabled || !tree.renderer.gameObject.activeInHierarchy
                    || tree.renderer.sprite!=tree.living) continue;
                Vector2 root=trail.GroundCoordinates(tree.root);
                if((root-point).sqrMagnitude>radiusSquared) continue;
                tree.infected=true;
                Infect(tree);
            }
        }
    }
    private void Infect(Tree tree)
    {
        InfectedTreeCount++;
        var source = tree.renderer;
        Bounds originalBounds=source.localBounds;
        Bounds originalWorldBounds=source.bounds;
        if (transitionDuration <= 0) { ApplyDead(source,tree.dead,originalBounds,originalWorldBounds); return; }
        var go = new GameObject("Plague dead-tree fade");
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);
        go.transform.localPosition = Vector3.zero;
        var ghost = go.AddComponent<SpriteRenderer>();
        ApplyDead(ghost,tree.dead,originalBounds,originalWorldBounds);
        ghost.flipX = source.flipX; ghost.flipY = source.flipY;
        ApplyDead(ghost,tree.dead,originalBounds,originalWorldBounds);
        ghost.localBounds=originalBounds;
        ghost.bounds=originalWorldBounds;
        ghost.sortingLayerID = source.sortingLayerID; ghost.sortingOrder = source.sortingOrder;
        ghost.maskInteraction = source.maskInteraction;
        ghost.spriteSortPoint = source.spriteSortPoint;
        var color = source.color; color.a = 0; ghost.color = color;
        transitions.Add(new Transition { source = source, ghost = ghost, dead = tree.dead, originalColor = source.color, originalBounds=originalBounds, originalWorldBounds=originalWorldBounds });
    }
    private void ApplyDead(SpriteRenderer renderer,Sprite dead,Bounds bounds,Bounds worldBounds)
    {
        renderer.sprite=dead;
        renderer.drawMode=SpriteDrawMode.Simple;
        var spriteBounds=dead.bounds;
        Vector2 size=new Vector2(bounds.size.x,bounds.size.y);
        Vector2 scale=new Vector2(size.x/Mathf.Max(.001f,spriteBounds.size.x),size.y/Mathf.Max(.001f,spriteBounds.size.y));
        Vector2 centre=new Vector2(renderer.flipX ? -bounds.center.x : bounds.center.x,renderer.flipY ? -bounds.center.y : bounds.center.y);
        Vector2 offset=centre-Vector2.Scale(new Vector2(spriteBounds.center.x,spriteBounds.center.y),scale);
        renderer.sharedMaterial=witheredMaterial;
        var properties=new MaterialPropertyBlock();
        renderer.GetPropertyBlock(properties);
        properties.SetVector("_PlagueTransform",new Vector4(scale.x,scale.y,offset.x,offset.y));
        renderer.SetPropertyBlock(properties);
        renderer.localBounds=bounds;
        // SpriteRenderer can refresh its local bounds from the intrinsic sprite on a later frame.
        // Keep the original world bounds explicitly for these static scenery renderers.
        renderer.bounds=worldBounds;
    }
    private void Finish(Transition transition)
    {
        if (transition.source != null) { ApplyDead(transition.source,transition.dead,transition.originalBounds,transition.originalWorldBounds); transition.source.color = transition.originalColor; }
        if (transition.ghost != null) Destroy(transition.ghost.gameObject);
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        foreach (var transition in transitions) Finish(transition);
        transitions.Clear(); grid.Clear(); indexed = false;
    }
}
