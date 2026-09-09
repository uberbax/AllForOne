using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WhoHeroesUnitView : MonoBehaviour
{
    [Serializable]
    private sealed class UnitAnimationSet
    {
        [SerializeField] internal string unitId;
        [SerializeField] internal Sprite[] idleFrames;
        [SerializeField] internal Sprite[] walkFrames;
        [SerializeField] internal Sprite[] attackFrames;
        [SerializeField] internal Sprite[] deathFrames;
        [SerializeField, Min(0.01f)] internal float visualScale = 2f;
    }

    private const string SatyrId = "satyr";
    private const string SatyrCleaveSkillId = "whoheroes_satyr_cleave";

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private UnitAnimationSet[] unitAnimations;
    [SerializeField, Min(0.01f)] private float animationFrameSeconds = 0.12f;
    [SerializeField] private Sprite[] satyrAttackFrames;
    [SerializeField, Min(0.01f)] private float satyrAttackFrameSeconds = 0.1f;

    private RObj owner;
    private Sprite idleSprite;
    private SpriterAnim spriterAnimator;
    private UnoAnim normalAttackAnimation;
    private UnoAnim satyrAttackAnimation;
    private int attackAnimationIndex;
    private bool animationsConfigured;

    private void Awake()
    {
        ResolveReferences();
        TryConfigureAnimations();
    }

    private void OnEnable()
    {
        EventManager.SUB("skill_casted", OnSkillCasted);
    }

    private void OnDisable()
    {
        EventManager.UNSUB("skill_casted", OnSkillCasted);
        RestoreIdleSprite();
    }

    private void Start()
    {
        ResolveReferences();
        if (TryConfigureAnimations())
            return;

        var id = owner?.dbObj?.ID;
        if (spriteRenderer == null || string.IsNullOrEmpty(id) || ResourceHolder.instance == null ||
            !ResourceHolder.instance.avas.TryGetValue(id, out idleSprite))
            return;
        spriteRenderer.sprite = idleSprite;
    }

    private bool TryConfigureAnimations()
    {
        if (animationsConfigured)
            return true;

        var id = owner?.dbObj?.ID;
        if (spriteRenderer == null || string.IsNullOrEmpty(id) || unitAnimations == null)
            return false;

        UnitAnimationSet selected = null;
        foreach (var animationSet in unitAnimations)
            if (animationSet != null && string.Equals(animationSet.unitId, id, StringComparison.Ordinal))
            {
                selected = animationSet;
                break;
            }

        if (selected == null || !HasFrames(selected.idleFrames) || !HasFrames(selected.walkFrames))
            return false;

        idleSprite = FirstFrame(selected.idleFrames);
        spriteRenderer.sprite = idleSprite;
        spriterAnimator = GetComponent<SpriterAnim>();
        if (spriterAnimator == null)
            spriterAnimator = gameObject.AddComponent<SpriterAnim>();

        spriterAnimator.timeFrame = animationFrameSeconds;
        spriterAnimator.defaultAnim = "idle";
        spriterAnimator.anims = new List<UnoAnim>
        {
            CreateAnimation("idle", selected.idleFrames, true, selected.visualScale),
            CreateAnimation("walk", selected.walkFrames, true, selected.visualScale),
            CreateAnimation("attack", HasFrames(selected.attackFrames) ? selected.attackFrames : selected.idleFrames,
                false, selected.visualScale, "idle"),
            CreateAnimation("death", HasFrames(selected.deathFrames) ? selected.deathFrames : selected.idleFrames,
                false, selected.visualScale)
        };
        attackAnimationIndex = spriterAnimator.anims.FindIndex(animation => animation.nm == "attack");
        normalAttackAnimation = spriterAnimator.anims[attackAnimationIndex];
        if (id == SatyrId && HasFrames(satyrAttackFrames))
        {
            satyrAttackAnimation = CreateAnimation("attack", satyrAttackFrames, false, selected.visualScale, "idle");
            satyrAttackAnimation.speed = animationFrameSeconds / Mathf.Max(0.01f, satyrAttackFrameSeconds);
        }
        animationsConfigured = true;
        return true;
    }

    private static UnoAnim CreateAnimation(
        string name, Sprite[] frames, bool loop, float scale, string endAnimation = "")
    {
        var animation = new UnoAnim
        {
            nm = name,
            loop = loop,
            endAnim = endAnimation,
            scale = Mathf.Max(0.01f, scale),
            speed = 1f
        };
        foreach (var frame in frames)
            if (frame != null)
                animation.sprites.Add(frame);
        return animation;
    }

    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null)
            return false;
        foreach (var frame in frames)
            if (frame != null)
                return true;
        return false;
    }

    private static Sprite FirstFrame(Sprite[] frames)
    {
        foreach (var frame in frames)
            if (frame != null)
                return frame;
        return null;
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
        if (owner?.dbObj?.ID != SatyrId || args?.who != owner || !TryConfigureAnimations() ||
            satyrAttackAnimation == null)
            return;

        // SkillExecutor has already queued XDanimator's attack. Select its clip before it starts;
        // a second CrossFade here would restart the same attack when that queued call arrives.
        spriterAnimator.anims[attackAnimationIndex] = args.who2?.dbObj?.ID == SatyrCleaveSkillId
            ? satyrAttackAnimation
            : normalAttackAnimation;
    }

    private void RestoreIdleSprite()
    {
        if (spriteRenderer != null && idleSprite != null)
            spriteRenderer.sprite = idleSprite;
    }
}
