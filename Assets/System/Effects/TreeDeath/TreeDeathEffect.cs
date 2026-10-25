using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>A detached visual death; only the non-interactive stump persists.</summary>
public sealed class TreeDeathEffect : MonoBehaviour
{
    [SerializeField] private Transform fallingPivot;
    [SerializeField] private SpriteRenderer stump;
    [SerializeField, Min(.1f)] private float fallDuration=1.15f;
    [SerializeField, Range(40,100)] private float fallAngle=88;
    [SerializeField, Range(0,.25f)] private float footHeightFraction=.1f;
    private sealed class Visual { public SpriteRenderer renderer; public Bounds bounds; public Color color; }
    private readonly List<Visual> visuals=new List<Visual>();
    private Vector3 axis;
    private float direction,elapsed;
    private bool playing;
    private static GameObject prefab;
    public bool IsFalling=>playing;
    public SpriteRenderer Stump=>stump;
    public Transform FallingPivot=>fallingPivot;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()=>prefab=null;
    public static bool IsTree(RObj item)
    {
        if(item==null) return false;
        string id=!string.IsNullOrEmpty(item.dbID)?item.dbID:item.dbObj?.ID;
        return string.Equals(id,"tree",StringComparison.OrdinalIgnoreCase) || (id!=null && id.StartsWith("tree_",StringComparison.OrdinalIgnoreCase));
    }
    public static bool TryPlay(RObj tree)
    {
        if(!IsTree(tree) || tree.main==null || !tree.visuals.TryGetValue("vis_main",out var root) || root==null) return false;
        var sources=root.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer primary=null; float largest=0;
        foreach(var source in sources) {
            if(!Usable(source)) continue;
            var bounds=VisualBounds(source); float score=bounds.size.x*bounds.size.y*source.color.a;
            if(score>largest) {largest=score;primary=source;}
        }
        if(primary==null) return false;
        if(prefab==null) prefab=Resources.Load<GameObject>("TreeDeath/TreeDeathEffect");
        if(prefab==null) {Debug.LogWarning("TreeDeathEffect prefab missing; using standard death fade.");return false;}
        var effect=Instantiate(prefab).GetComponent<TreeDeathEffect>();
        if(tree.main.scene.IsValid() && tree.main.scene.isLoaded) SceneManager.MoveGameObjectToScene(effect.gameObject,tree.main.scene);
        effect.Initialize(tree,primary,sources); return true;
    }
    private static bool Usable(SpriteRenderer source)=>source!=null && source.enabled && source.gameObject.activeInHierarchy
        && source.sprite!=null && source.color.a>.005f && source.name.IndexOf("shadow",StringComparison.OrdinalIgnoreCase)<0;
    private static Bounds VisualBounds(SpriteRenderer source)
    {
        var bounds=source.sprite.bounds;
        if(source.sharedMaterial!=null && source.sharedMaterial.shader.name=="AllForOne/Effects/WitheredTree") {
            var properties=new MaterialPropertyBlock();source.GetPropertyBlock(properties);
            var affine=properties.GetVector("_PlagueTransform");
            bounds.center=new Vector3(bounds.center.x*affine.x+affine.z,bounds.center.y*affine.y+affine.w,bounds.center.z);
            bounds.size=new Vector3(bounds.size.x*Mathf.Abs(affine.x),bounds.size.y*Mathf.Abs(affine.y),bounds.size.z);
        }
        if(source.flipX) bounds.center=new Vector3(-bounds.center.x,bounds.center.y,bounds.center.z);
        if(source.flipY) bounds.center=new Vector3(bounds.center.x,-bounds.center.y,bounds.center.z);
        return bounds;
    }
    private void Initialize(RObj tree,SpriteRenderer primary,SpriteRenderer[] sources)
    {
        var bounds=VisualBounds(primary);
        Vector3 foot=primary.transform.TransformPoint(new Vector3(bounds.center.x,bounds.min.y+bounds.size.y*footHeightFraction,0));
        transform.position=foot; axis=primary.transform.forward.normalized;
        direction=tree.lastDmgFrom!=null && tree.lastDmgFrom.main!=null
            ? (Vector3.Dot(foot-tree.lastDmgFrom.main.transform.position,primary.transform.right)>=0?-1:1)
            : (tree.RID==null || (tree.RID.GetHashCode()&1)==0?-1:1);
        var sorting=GetComponent<SortingGroup>();var original=primary.GetComponentInParent<SortingGroup>();
        sorting.sortingLayerID=original!=null?original.sortingLayerID:primary.sortingLayerID;
        sorting.sortingOrder=original!=null?original.sortingOrder:primary.sortingOrder;sorting.sortAtRoot=true;
        foreach(var source in sources) {
            if(!Usable(source)) continue;
            var go=new GameObject("Falling "+source.name,typeof(SpriteRenderer));go.layer=source.gameObject.layer;
            go.transform.SetParent(fallingPivot,false);go.transform.position=source.transform.position;
            go.transform.rotation=source.transform.rotation;go.transform.localScale=source.transform.lossyScale;
            var copy=go.GetComponent<SpriteRenderer>();copy.sprite=source.sprite;copy.sharedMaterial=source.sharedMaterial;copy.color=source.color;
            copy.flipX=source.flipX;copy.flipY=source.flipY;copy.drawMode=source.drawMode;copy.size=source.size;
            copy.sortingLayerID=source.sortingLayerID;copy.sortingOrder=source.sortingOrder;
            copy.maskInteraction=source.maskInteraction;copy.spriteSortPoint=source.spriteSortPoint;
            var properties=new MaterialPropertyBlock();source.GetPropertyBlock(properties);copy.SetPropertyBlock(properties);
            var local=VisualBounds(source);copy.localBounds=local;
            visuals.Add(new Visual{renderer=copy,bounds=local,color=source.color});source.enabled=false;
        }
        float width=primary.transform.TransformVector(Vector3.right*bounds.size.x).magnitude;
        float scale=Mathf.Clamp(width*.26f,.38f,1.05f)/Mathf.Max(.01f,stump.sprite.bounds.size.x);
        stump.transform.rotation=primary.transform.rotation;stump.transform.localScale=Vector3.one*scale;
        var sb=stump.sprite.bounds;
        stump.transform.position=foot-stump.transform.rotation*(new Vector3(sb.center.x,sb.min.y,0)*scale);
        stump.sortingLayerID=primary.sortingLayerID;stump.sortingOrder=primary.sortingOrder-1;stump.flipX=primary.flipX;
        stump.color=new Color(primary.color.r,primary.color.g,primary.color.b,.25f);
        playing=true;Animate(0);
    }
    private void Update() {if(!playing)return;elapsed+=Time.deltaTime;Animate(Mathf.Clamp01(elapsed/Mathf.Max(.1f,fallDuration)));}
    public void SetPreviewProgress(float t) {if(visuals.Count>0) Animate(Mathf.Clamp01(t));}
    private void Animate(float t)
    {
        fallingPivot.rotation=Quaternion.AngleAxis(direction*fallAngle*Mathf.Pow(t,1.7f),axis);
        float alpha=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.28f,1,t));
        foreach(var visual in visuals) {
            if(visual.renderer==null) continue;
            var color=visual.color;color.a*=alpha;visual.renderer.color=color;
            visual.renderer.bounds=WorldBounds(visual.bounds,visual.renderer.transform.localToWorldMatrix);
        }
        var sc=stump.color;sc.a=Mathf.Lerp(.25f,1,Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2)));stump.color=sc;
        if(t<1)return;
        playing=false;Destroy(fallingPivot.gameObject);visuals.Clear();gameObject.name="Tree stump";enabled=false;
    }
    private static Bounds WorldBounds(Bounds local,Matrix4x4 matrix)
    {
        var e=local.extents;var x=matrix.MultiplyVector(new Vector3(e.x,0,0));
        var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
        return new Bounds(matrix.MultiplyPoint3x4(local.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),
            Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
    }
}
