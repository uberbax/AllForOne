using System.Collections;
using UnityEngine;

public sealed class WhoHeroesUnitView : MonoBehaviour
{
    private const string SatyrId = "satyr";
    private const string SatyrCleaveSkillId = "whoheroes_satyr_cleave";

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] satyrAttackFrames;
    [SerializeField, Min(0.01f)] private float satyrAttackFrameSeconds = 0.1f;

    private RObj owner;
    private Sprite idleSprite;
    private Coroutine attackRoutine;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        EventManager.SUB("skill_casted", OnSkillCasted);
    }

    private void OnDisable()
    {
        EventManager.UNSUB("skill_casted", OnSkillCasted);
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        RestoreIdleSprite();
    }

    private void Start()
    {
        ResolveReferences();
        var id = owner?.dbObj?.ID;
        if (spriteRenderer == null || string.IsNullOrEmpty(id) || ResourceHolder.instance == null ||
            !ResourceHolder.instance.avas.TryGetValue(id, out idleSprite))
            return;
        spriteRenderer.sprite = idleSprite;
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (owner == null)
            owner = GetComponentInParent<ObjHolder>()?.obj;
    }

    private void OnSkillCasted(ArgPass args)
    {
        ResolveReferences();
        if (owner?.dbObj?.ID != SatyrId || args?.who != owner || args.who2?.dbObj?.ID != SatyrCleaveSkillId ||
            spriteRenderer == null || satyrAttackFrames == null || satyrAttackFrames.Length == 0)
            return;

        if (attackRoutine != null)
            StopCoroutine(attackRoutine);
        attackRoutine = StartCoroutine(PlaySatyrAttack());
    }

    private IEnumerator PlaySatyrAttack()
    {
        var wait = new WaitForSeconds(satyrAttackFrameSeconds);
        foreach (var frame in satyrAttackFrames)
        {
            if (frame != null)
                spriteRenderer.sprite = frame;
            yield return wait;
        }

        attackRoutine = null;
        RestoreIdleSprite();
    }

    private void RestoreIdleSprite()
    {
        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }
}
