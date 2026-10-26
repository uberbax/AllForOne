using UnityEngine;

public class FogActivator : MonoBehaviour
{
    public Transform lo;
    public Transform hi;
    public Transform cloudRoot;
    [SerializeField] private MeshRenderer cloudSurface;
    [SerializeField, Min(.1f)] private float disperseDuration = 2.4f;
    private MaterialPropertyBlock properties;
    private Vector2 revealOrigin = new Vector2(.5f, .5f);
    private float elapsed;
    private float progress;
    private bool dispersing;
    public float ClearProgress => progress;
    public bool IsCleared => progress >= 1f;
    public bool IsDispersing => dispersing;

    private void Start() => Apply();
    public void Activate()
    {
        if (IsCleared || dispersing) return;
        if (!ResolveSurface()) return;
        revealOrigin = new Vector2(.5f, .5f);
        var state = MainStates.instance;
        if (state != null && state.HasMain() && state.mainPlayer != null && state.mainPlayer.main != null && lo != null && hi != null)
        {
            Vector3 point = cloudRoot.InverseTransformPoint(state.mainPlayer.main.transform.position);
            Vector3 a = cloudRoot.InverseTransformPoint(lo.position), b = cloudRoot.InverseTransformPoint(hi.position);
            revealOrigin = new Vector2(Mathf.InverseLerp(Mathf.Min(a.x,b.x),Mathf.Max(a.x,b.x),point.x),
                Mathf.InverseLerp(Mathf.Min(a.y,b.y),Mathf.Max(a.y,b.y),point.y));
        }
        elapsed = 0;
        dispersing = true;
        Apply();
    }
    private void Update()
    {
        if (!dispersing) return;
        elapsed += Time.deltaTime;
        progress = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.1f, disperseDuration)));
        Apply();
        if (progress < 1) return;
        dispersing = false;
        if (cloudSurface != null) cloudSurface.enabled = false;
    }
    public void ResetFog()
    {
        elapsed = progress = 0;
        dispersing = false;
        revealOrigin = new Vector2(.5f,.5f);
        if (ResolveSurface()) cloudSurface.enabled = true;
        Apply();
    }
    private bool ResolveSurface()
    {
        if (cloudSurface != null) return true;
        if (cloudRoot == null) return false;
        var child = cloudRoot.Find("AnimatedClouds");
        if (child != null) cloudSurface = child.GetComponent<MeshRenderer>();
        return cloudSurface != null;
    }
    private void Apply()
    {
        if (!ResolveSurface()) return;
        if (properties == null) properties = new MaterialPropertyBlock();
        cloudSurface.GetPropertyBlock(properties);
        properties.SetFloat("_ClearProgress", progress);
        properties.SetVector("_RevealOrigin", new Vector4(revealOrigin.x,revealOrigin.y,0,0));
        properties.SetFloat("_PreviewTime", -1);
        cloudSurface.SetPropertyBlock(properties);
    }
}
