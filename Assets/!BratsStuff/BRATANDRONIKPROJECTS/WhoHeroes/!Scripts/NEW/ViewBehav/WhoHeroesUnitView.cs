using UnityEngine;

public sealed class WhoHeroesUnitView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        var holder = GetComponentInParent<ObjHolder>();
        var id = holder?.obj?.dbObj?.ID;
        if (spriteRenderer == null || string.IsNullOrEmpty(id) || ResourceHolder.instance == null ||
            !ResourceHolder.instance.avas.ContainsKey(id))
            return;
        spriteRenderer.sprite = ResourceHolder.instance.avas[id];
    }
}
