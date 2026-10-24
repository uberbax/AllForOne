using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Rarity marker attached only to world loot; child lifetime follows the dropped object.</summary>
public sealed class LootRarityBeam : MonoBehaviour
{
    [SerializeField] private SpriteRenderer column;
    [SerializeField] private MeshRenderer ground;
    [SerializeField] private Color tint = Color.white;
    [SerializeField, Min(.01f)] private float fadeInDuration = .22f;
    private MaterialPropertyBlock properties;
    private float elapsed;
    private float phase;
    private static readonly GameObject[] prefabs = new GameObject[6];
    private static readonly int Tint = Shader.PropertyToID("_BeamTint");
    private static readonly int Opacity = Shader.PropertyToID("_Opacity");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => System.Array.Clear(prefabs, 0, prefabs.Length);

    public static bool IsEligible(RObj item)
    {
        if (item == null || item.dbObj == null) return false;
        if (item.it != ItemType.item) return true;
        float subtype = item.GetPar("subtype");
        // In the config, stackable resources have no equipment slot and use currency/none.
        // Usable items, potions, adornments, skills and equipment still receive a beam.
        bool resource = item.GetPar("slot") < 0 &&
            (subtype == MainStates.subtypes["currency"] || subtype == MainStates.subtypes["none"]);
        return !resource;
    }
    public static LootRarityBeam Attach(RObj item)
    {
        if (!IsEligible(item) || item.main == null) return null;
        var existing = item.main.GetComponentInChildren<LootRarityBeam>(true);
        if (existing != null) return existing;
        int rarity = Mathf.Clamp(Mathf.RoundToInt(item.GetPar("rarity")), 0, prefabs.Length - 1);
        if (prefabs[rarity] == null) prefabs[rarity] = Resources.Load<GameObject>("LootBeams/LootBeam_" + rarity);
        if (prefabs[rarity] == null) { Debug.LogError("Loot beam prefab missing for rarity " + rarity); return null; }
        var instance = Instantiate(prefabs[rarity], item.main.transform, false).GetComponent<LootRarityBeam>();
        var holder = ResourceHolder.instance;
        if (holder != null && holder.rareColors.ContainsKey(rarity)) instance.tint = holder.rareColors[rarity];
        instance.phase = (item.RID == null ? rarity : item.RID.GetHashCode() & 1023) * .013f;
        bool xy = ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0;
        if (!xy)
        {
            instance.ground.transform.localRotation = Quaternion.Euler(90, 0, 0);
            instance.ground.transform.localPosition = new Vector3(0, .025f, 0);
            if (Camera.main != null) instance.column.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
        }
        instance.NormalizeScale();
        instance.Apply(0);
        return instance;
    }
    public static void Remove(RObj item)
    {
        if (item == null || item.main == null) return;
        var beam = item.main.GetComponentInChildren<LootRarityBeam>(true);
        if (beam == null) return;
        beam.gameObject.SetActive(false);
        Destroy(beam.gameObject);
    }
    private void Awake() { properties = new MaterialPropertyBlock(); Apply(0); }
    private void NormalizeScale()
    {
        if (transform.parent == null) return;
        Vector3 scale = transform.parent.lossyScale;
        transform.localScale = new Vector3(Inverse(scale.x), Inverse(scale.y), Inverse(scale.z));
    }
    private static float Inverse(float scale) => Mathf.Abs(scale) < .0001f ? 1 : 1f / scale;
    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        NormalizeScale();
        Apply(Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeInDuration))));
    }
    public void SetPreviewTime(float seconds) { elapsed = seconds; Apply(1); }
    private void Apply(float fade)
    {
        if (properties == null) properties = new MaterialPropertyBlock();
        properties.SetColor(Tint, tint);
        properties.SetFloat(Opacity, fade * (.86f + .14f * Mathf.Sin(elapsed * 2.1f + phase)));
        if (column != null) column.SetPropertyBlock(properties);
        if (ground != null) ground.SetPropertyBlock(properties);
    }
}
