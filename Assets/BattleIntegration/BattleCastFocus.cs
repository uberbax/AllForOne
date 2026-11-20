using System;
using System.Collections.Generic;
using BRATANDRONIKLIB;
using UnityEngine;
using UnityEngine.UI;

// The test effect supplies its framing settings; gameplay supplies the live cast participants.
[DefaultExecutionOrder(-100)]
public sealed class BattleCastFocus : MonoBehaviour
{
    [SerializeField] private BRATEffectBattle settings;
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Transform environmentRoot;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool dimEnvironment = true;
    [Tooltip("Usable viewport above the battle panels, in normalized screen coordinates.")]
    [SerializeField] private Rect focusViewport = new Rect(.06f, .40f, .88f, .50f);

    private sealed class MaterialState
    {
        public Material Original, Working;
        public Color Tint;
    }
    private sealed class SpriteState
    {
        public SpriteRenderer Renderer;
        public Color Color;
        public MaterialPropertyBlock OriginalProperties, WorkingProperties;
    }
    private sealed class Participant
    {
        public Transform Transform;
        public Vector3 LocalFeet, LocalHead;
    }

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private readonly List<MaterialState> materials = new List<MaterialState>();
    private readonly List<SpriteState> sprites = new List<SpriteState>();
    private readonly List<GameObject> tintedRoots = new List<GameObject>();
    private readonly List<Participant> participants = new List<Participant>();
    private readonly List<(Canvas Canvas, int Layer, int Order)> raisedCanvases = new List<(Canvas, int, int)>();
    private readonly HashSet<RObj> foreground = new HashSet<RObj>();
    private readonly HashSet<RObj> seenUnits = new HashSet<RObj>();
    private readonly HashSet<SpriteRenderer> seenSprites = new HashSet<SpriteRenderer>();
    private IDisposable activity;
    private Camera cameraInUse;
    private RObj caster;
    private XDanimator animator;
    private UiLoopVideo video;
    private Vector3 cameraHome, lastCameraPosition;
    private float cameraSize, cameraFov, startedAt, minimumEnd, deadline;
    private float releaseAt = -1;
    private float releaseWeight;
    private bool active, naprig, naprigComplete;
    public bool IsFocusing => active;

    private void OnEnable()
    {
        Unsubscribe();
        EventManager.SUB("skill_casted", OnCast);
        EventManager.SUB("naprig_completed", OnNaprigCompleted);
    }

    private void Unsubscribe()
    {
        if (EventManager.dynActions.TryGetValue("skill_casted", out var casts))
            EventManager.dynActions["skill_casted"] = casts - OnCast;
        if (EventManager.dynActions.TryGetValue("naprig_completed", out var jumps))
            EventManager.dynActions["naprig_completed"] = jumps - OnNaprigCompleted;
    }

    private bool IsStageUnit(RObj unit) => unit?.main != null && stageRoot != null &&
        unit.it == ItemType.monster && unit.main.transform.IsChildOf(stageRoot);

    private void OnCast(ArgPass cast)
    {
        if (cast == null || cast.what1 == "secondary" || cast.who2 == null ||
            !IsStageUnit(cast.who) || !cast.who.main.activeInHierarchy ||
            MainStates.instance == null || !MainStates.instance.inBattle) return;
        StopFocus();
        cameraInUse = targetCamera != null && targetCamera.isActiveAndEnabled ? targetCamera : Camera.main;
        if (cameraInUse == null) return;
        caster = cast.who;
        var skill = cast.who2;
        foreground.Add(caster);
        if (cast.whats != null)
            foreach (var target in cast.whats)
                if (IsStageUnit(target)) foreground.Add(target);
        foreach (var unit in foreground) CacheParticipant(unit);

        foreach (var holder in stageRoot.GetComponentsInChildren<ObjHolder>(true))
        {
            var unit = holder.obj;
            if (!IsStageUnit(unit) || !seenUnits.Add(unit) || foreground.Contains(unit) || unit.visMain == null) continue;
            var root = unit.visMain;
            tintedRoots.Add(root);
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) TintGraphic(graphic);
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) CacheSprite(sprite);
        }
        RaiseParticipants();

        if (dimEnvironment && environmentRoot != null)
            foreach (var sprite in environmentRoot.GetComponentsInChildren<SpriteRenderer>(true))
                CacheSprite(sprite, usePropertyBlock: true);

        if (dimEnvironment && settings != null && settings.sceneSprites != null)
            foreach (var sprite in settings.sceneSprites)
            {
                if (sprite == null) continue;
                bool primary = false;
                foreach (var unit in foreground)
                    if (unit.main != null && sprite.transform.IsChildOf(unit.main.transform)) { primary = true; break; }
                if (!primary) CacheSprite(sprite);
            }

        animator = caster.visuals.TryGetValue("animator", out var view) && view != null
            ? view.GetComponentInChildren<XDanimator>() : null;
        video = caster.visMain != null ? caster.visMain.GetComponent<UiLoopVideo>() : null;
        naprig = ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.use_naprig) == 1 &&
            (skill.dbObj.ID == "basic_melee" || skill.GetPar("use_naprig") > 0);
        naprigComplete = false;
        cameraHome = lastCameraPosition = cameraInUse.transform.localPosition;
        cameraSize = cameraInUse.orthographicSize;
        cameraFov = cameraInUse.fieldOfView;
        startedAt = Time.unscaledTime;
        float distance = 0;
        foreach (var target in foreground)
            if (target != caster) distance = Mathf.Max(distance, Vector3.Distance(caster.Position, target.Position));
        var effect = ResourceHolder.instance.GetMeSkillEtc(caster.dbObj, skill.dbObj.ID);
        float delay = effect != null ? Mathf.Max(0, effect.projDelay) : 0;
        float projectiles = delay + Mathf.Max(0, skill.GetPar("proj_amount") - 1) * Mathf.Max(0, skill.GetPar("dt")) +
            distance / Mathf.Max(1, skill.GetPar("speed")) + (naprig ? 0 : SkillExecutor.timeWait);
        minimumEnd = startedAt + Mathf.Max(FocusTime + (settings != null ? settings.holdTime : .38f), projectiles);
        float animationLength = video != null ? video.GetAnimationDuration("attack") : .5f;
        deadline = startedAt + Mathf.Max(15, animationLength + distance * .4f + 6, projectiles + 2);
        activity = UnitActionTracker.Begin(caster);
        active = true;
    }

    private static Canvas FindUnitCanvas(RObj unit)
    {
        var canvas = unit.visMain != null ? unit.visMain.GetComponentInParent<Canvas>() : null;
        return canvas != null && (canvas.isRootCanvas || canvas.overrideSorting) &&
            canvas.transform.IsChildOf(unit.main.transform)
            ? canvas : null;
    }

    private void RaiseParticipants()
    {
        int topLayer = 0;
        int topOrder = int.MinValue;
        void Include(int layer, int order)
        {
            int layerValue = SortingLayer.GetLayerValueFromID(layer);
            int currentTopLayerValue = SortingLayer.GetLayerValueFromID(topLayer);
            if (topOrder != int.MinValue && (layerValue < currentTopLayerValue ||
                (layerValue == currentTopLayerValue && order <= topOrder))) return;
            topLayer = layer;
            topOrder = order;
        }

        foreach (var unit in seenUnits)
        {
            if (foreground.Contains(unit)) continue;
            var canvas = FindUnitCanvas(unit);
            if (canvas != null) Include(canvas.sortingLayerID, canvas.sortingOrder);
        }
        foreach (var state in sprites)
            Include(state.Renderer.sortingLayerID, state.Renderer.sortingOrder);
        foreach (var target in foreground)
        {
            if (target == caster) continue;
            var canvas = FindUnitCanvas(target);
            if (canvas == null) continue;
            RaiseAbove(canvas, topLayer, topOrder);
        }
        // Resolve the attacker's order after the targets, so their promoted canvases cannot cover the lunge.
        foreach (var target in foreground)
        {
            if (target == caster) continue;
            var canvas = FindUnitCanvas(target);
            if (canvas != null) Include(canvas.sortingLayerID, canvas.sortingOrder);
        }
        var attackerCanvas = FindUnitCanvas(caster);
        if (attackerCanvas != null) RaiseAbove(attackerCanvas, topLayer, topOrder);
    }

    private void RaiseAbove(Canvas canvas, int layer, int order)
    {
        if (order == int.MinValue) return;
        int currentLayerValue = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID);
        int layerValue = SortingLayer.GetLayerValueFromID(layer);
        if (currentLayerValue > layerValue || (currentLayerValue == layerValue && canvas.sortingOrder > order)) return;
        raisedCanvases.Add((canvas, canvas.sortingLayerID, canvas.sortingOrder));
        canvas.sortingLayerID = layer;
        canvas.sortingOrder = order + 1;
    }

    private void CacheParticipant(RObj unit)
    {
        var root = unit.visMain != null ? unit.visMain.transform : unit.main.transform;
        var legs = root.Find("legs");
        var head = root.Find("head");
        participants.Add(new Participant
        {
            Transform = root,
            LocalFeet = legs != null ? root.InverseTransformPoint(legs.position) : Vector3.zero,
            LocalHead = head != null ? root.InverseTransformPoint(head.position) : Vector3.up
        });
    }

    private void TintGraphic(Graphic graphic)
    {
        var original = graphic.material;
        if (original == null || !original.HasProperty(ColorId)) return;
        MaterialState state = null;
        foreach (var item in materials)
            if (item.Original == original) { state = item; break; }
        if (state == null)
        {
            state = new MaterialState
            {
                Original = original, Tint = original.GetColor(ColorId),
                Working = new Material(original) { name = original.name + " (Cast focus)", hideFlags = HideFlags.DontSave }
            };
            materials.Add(state);
        }
        graphic.material = state.Working;
    }

    private void CacheSprite(SpriteRenderer sprite, bool usePropertyBlock = false)
    {
        if (sprite == null || !seenSprites.Add(sprite)) return;
        var state = new SpriteState { Renderer = sprite, Color = sprite.color };
        var material = sprite.sharedMaterial;
        if (usePropertyBlock && material != null && material.HasProperty(ColorId))
        {
            // Cloud spawners copy the template renderer's color. Keep the temporary tint on this renderer.
            state.OriginalProperties = new MaterialPropertyBlock();
            state.WorkingProperties = new MaterialPropertyBlock();
            sprite.GetPropertyBlock(state.OriginalProperties);
            sprite.GetPropertyBlock(state.WorkingProperties);
            state.Color = state.OriginalProperties.HasColor(ColorId)
                ? state.OriginalProperties.GetColor(ColorId) : material.GetColor(ColorId);
        }
        sprites.Add(state);
    }

    private float FocusTime => Mathf.Max(.01f, settings != null ? settings.focusTime : .18f);
    private float ReturnTime => Mathf.Max(.01f, settings != null ? settings.returnTime : .32f);

    private void OnNaprigCompleted(ArgPass evt)
    {
        if (active && evt != null && ReferenceEquals(evt.who, caster)) naprigComplete = true;
    }

    private void LateUpdate()
    {
        if (!active) return;
        if (cameraInUse == null || caster?.main == null || !caster.main.activeInHierarchy ||
            caster.GetPar("health") <= 0 || MainStates.instance == null || !MainStates.instance.inBattle)
        {
            StopFocus();
            return;
        }
        // Keep deliberate external camera movement instead of snapping it back when focus ends.
        cameraHome += cameraInUse.transform.localPosition - lastCameraPosition;
        float now = Time.unscaledTime;
        float weight = Mathf.SmoothStep(0, 1, Mathf.Clamp01((now - startedAt) / FocusTime));
        bool animationActive = animator != null && animator.prevState == "attack" &&
            !(video != null && video.IsAnimationFinished("attack"));
        bool finished = naprig ? naprigComplete : !animationActive;
        if (releaseAt < 0 && ((now >= minimumEnd && finished) || now >= deadline))
        {
            releaseAt = now;
            releaseWeight = weight;
        }
        if (releaseAt >= 0)
        {
            float returning = Mathf.Clamp01((now - releaseAt) / ReturnTime);
            weight = releaseWeight * (1 - Mathf.SmoothStep(0, 1, returning));
            if (returning >= 1) { StopFocus(); return; }
        }
        ApplyCamera(weight);
        float brightness = Mathf.Lerp(1, settings != null ? settings.backgroundBrightness : .44f, weight);
        foreach (var state in materials)
            if (state.Working != null)
                state.Working.SetColor(ColorId, new Color(state.Tint.r * brightness, state.Tint.g * brightness,
                    state.Tint.b * brightness, state.Tint.a));
        foreach (var state in sprites)
        {
            if (state.Renderer == null) continue;
            var tint = new Color(state.Color.r * brightness, state.Color.g * brightness,
                state.Color.b * brightness, state.Color.a);
            if (state.WorkingProperties == null)
                state.Renderer.color = tint;
            else
            {
                state.WorkingProperties.SetColor(ColorId, tint);
                state.Renderer.SetPropertyBlock(state.WorkingProperties);
            }
        }
    }

    private void ApplyCamera(float weight)
    {
        var cameraTransform = cameraInUse.transform;
        Vector3 homeWorld = cameraTransform.parent != null ? cameraTransform.parent.TransformPoint(cameraHome) : cameraHome;
        Vector3 right = cameraTransform.right, up = cameraTransform.up, forward = cameraTransform.forward;
        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
        float nearestDepth = float.PositiveInfinity;
        foreach (var unit in participants)
            if (unit.Transform != null && unit.Transform.gameObject.activeInHierarchy)
            {
                Vector3 feet = unit.Transform.TransformPoint(unit.LocalFeet) - homeWorld;
                Vector3 head = unit.Transform.TransformPoint(unit.LocalHead) - homeWorld;
                float height = Mathf.Max(.01f, Vector3.Distance(feet, head));
                // Keep space below the soles and around the moving arms/weapon.
                float x = Vector3.Dot((feet + head) * .5f, right);
                minX = Mathf.Min(minX, x - height * .35f);
                maxX = Mathf.Max(maxX, x + height * .35f);
                minY = Mathf.Min(minY, Vector3.Dot(feet, up) - height * .08f);
                maxY = Mathf.Max(maxY, Vector3.Dot(head, up) + height * .08f);
                nearestDepth = Mathf.Min(nearestDepth, Vector3.Dot(feet, forward), Vector3.Dot(head, forward));
            }
        float desiredZoom = Mathf.Clamp(settings != null ? settings.zoom : .78f, .55f, 1);
        Vector3 focus = homeWorld;
        if (!float.IsPositiveInfinity(minX))
        {
            float viewportWidth = Mathf.Max(.1f, focusViewport.width);
            float viewportHeight = Mathf.Max(.1f, focusViewport.height);
            float baselineHalfHeight = cameraInUse.orthographic ? cameraSize :
                Mathf.Max(.01f, nearestDepth) * Mathf.Tan(cameraFov * Mathf.Deg2Rad * .5f);
            float requiredHalfHeight = Mathf.Max((maxY - minY) / (2 * viewportHeight),
                (maxX - minX) / (2 * viewportWidth * cameraInUse.aspect));
            desiredZoom = Mathf.Max(desiredZoom, requiredHalfHeight / baselineHalfHeight);
            float halfHeight = baselineHalfHeight * desiredZoom;
            // Frame participants above the lower HUD instead of at the full-screen center.
            focus += right * ((minX + maxX) * .5f - (focusViewport.center.x - .5f) * 2 * halfHeight * cameraInUse.aspect) +
                up * ((minY + maxY) * .5f - (focusViewport.center.y - .5f) * 2 * halfHeight);
        }
        cameraTransform.position = Vector3.Lerp(homeWorld, focus, weight);
        lastCameraPosition = cameraTransform.localPosition;
        float zoom = Mathf.Lerp(1, desiredZoom, weight);
        cameraInUse.orthographicSize = cameraSize * zoom;
        cameraInUse.fieldOfView = 2 * Mathf.Atan(Mathf.Tan(cameraFov * Mathf.Deg2Rad * .5f) * zoom) * Mathf.Rad2Deg;
    }

    public void StopFocus()
    {
        if (active && cameraInUse != null)
        {
            cameraInUse.transform.localPosition = cameraHome;
            cameraInUse.orthographicSize = cameraSize;
            cameraInUse.fieldOfView = cameraFov;
        }
        active = false;
        foreach (var state in raisedCanvases)
        {
            if (state.Canvas == null) continue;
            state.Canvas.sortingLayerID = state.Layer;
            state.Canvas.sortingOrder = state.Order;
        }
        raisedCanvases.Clear();
        activity?.Dispose();
        activity = null;
        foreach (var root in tintedRoots)
            if (root != null)
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                    foreach (var state in materials)
                        if (graphic.material == state.Working) { graphic.material = state.Original; break; }
        foreach (var state in sprites)
        {
            if (state.Renderer == null) continue;
            if (state.WorkingProperties == null) state.Renderer.color = state.Color;
            else state.Renderer.SetPropertyBlock(state.OriginalProperties.isEmpty ? null : state.OriginalProperties);
        }
        foreach (var state in materials)
            if (state.Working != null) Destroy(state.Working);
        materials.Clear();
        sprites.Clear();
        tintedRoots.Clear();
        participants.Clear();
        foreground.Clear();
        seenUnits.Clear();
        seenSprites.Clear();
        caster = null;
        animator = null;
        video = null;
        releaseAt = -1;
        cameraInUse = null;
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopFocus();
    }
}
