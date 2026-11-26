using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public sealed class UiLoopVideo : MonoBehaviour
{
    [Serializable]
    private sealed class AnimationClipEntry
    {
        public string anim;
        public VideoClip clip;
        [Min(0)] public float startTime;
        [Tooltip("Absolute time in the clip at which scripted movement may begin (after anticipation).")]
        [Min(0)] public float movementStartTime;
        [Tooltip("Absolute landing time. When later than Movement Start Time, return movement follows this interval.")]
        [Min(0)] public float movementEndTime;
        [Min(0.01f)] public float visualScale = 1f;
        [Tooltip("Image offset as a fraction of the UI rectangle; does not move legs/head anchors.")]
        public Vector2 visualOffset;
    }

    [SerializeField] private VideoClip clip;
    [SerializeField] private Texture poster;
    [SerializeField] private List<AnimationClipEntry> animations = new List<AnimationClipEntry>();
    [SerializeField, Min(0)] private float crossFadeDuration = 0.15f;

    private RawImage view;
    private RawImage overlay;
    private UiVideoAspect viewAspect;
    private UiVideoAspect overlayAspect;
    private UiVideoHitReaction hitReaction;
    private VideoPlayer primary;
    private VideoPlayer secondary;
    private VideoPlayer current;
    private VideoPlayer pending;
    private VideoPlayer frameSyncedPlayer;
    private bool savedSkipOnDrop;
    private string currentName;
    private string pendingName;
    private bool fading;
    private bool pendingInstant;
    private float elapsed;
    private Color fadeColor;
    private long pendingStartFrame;
    private long currentStartFrame;
    private bool pendingLoop;
    private bool currentLoop;
    private bool currentFinished;
    private bool startDefaultIdle;
    private float? entranceAlpha;
    private bool pendingFinished;
    private int pendingFrameReadyAt = -1;
    private bool pendingPlaybackStarted;
    private bool pendingHoldFirstFrame;
    private bool currentHeld;
    private int visibleSinceFrame = -1;
    private bool waitingForLoopFrame;
    private RenderTexture loopHold;
    private int loopFrameReadyAt = -1;
    private readonly List<(RenderTexture Texture, int Frame)> retiredTextures =
        new List<(RenderTexture, int)>();

    private void OnEnable()
    {
        view = GetComponent<RawImage>();
        if (!TryGetComponent(out viewAspect)) viewAspect = gameObject.AddComponent<UiVideoAspect>();
        primary = GetComponent<VideoPlayer>();
        view.raycastTarget = false;
        ShowPoster();
        // Allow a spawner to request a starting clip before preparing the default idle.
        startDefaultIdle = true;
    }

    public void CrossFade(string anim) => ChangeAnimation(anim, false);

    public void Play(string anim) => ChangeAnimation(anim, true);

    public void PlayOnce(string anim) => ChangeAnimation(anim, true, loopOverride: false);

    public void HoldFirstFrame(string anim) => ChangeAnimation(anim, true, holdFirstFrame: true);

    public void PlayFrameSyncedAttack()
    {
        Play("attack");
        var player = pendingName == "attack" ? pending : currentName == "attack" ? current : null;
        if (player == null || !player.canSetSkipOnDrop || player == frameSyncedPlayer) return;
        RestoreFrameSkipping();
        frameSyncedPlayer = player;
        savedSkipOnDrop = player.skipOnDrop;
        player.skipOnDrop = false;
    }

    private void RestoreFrameSkipping()
    {
        if (frameSyncedPlayer != null) frameSyncedPlayer.skipOnDrop = savedSkipOnDrop;
        frameSyncedPlayer = null;
    }

    public bool IsReactionPlaying => hitReaction != null && hitReaction.Playing;

    public bool PlayHitReaction()
    {
        var hit = FindClip("hit");
        if (!isActiveAndEnabled || hit == null) return false;
        if (hitReaction == null)
            hitReaction = GetComponent<UiVideoHitReaction>() ?? gameObject.AddComponent<UiVideoHitReaction>();
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, "hit", StringComparison.OrdinalIgnoreCase))
            {
                hitReaction.Play(hit, entry.startTime, entry.visualScale, entry.visualOffset);
                return true;
            }
        return false;
    }

    internal void RetireTexture(RenderTexture texture)
    {
        if (texture != null) retiredTextures.Add((texture, Time.frameCount));
    }

    public bool HasAnimation(string anim) =>
        !string.IsNullOrWhiteSpace(anim) && FindClip(anim.Trim().ToLowerInvariant()) != null;

    public bool IsAnimationFinished(string anim) => current != null && currentFinished &&
        string.Equals(currentName, anim, StringComparison.OrdinalIgnoreCase);

    public float GetAnimationDuration(string anim)
    {
        var value = FindClip(anim);
        if (value == null) return 0;
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
                return (float)Math.Max(0, value.length - entry.startTime);
        return (float)value.length;
    }

    public bool IsAnimationVisible(string anim) =>
        current != null && string.Equals(currentName, anim, StringComparison.OrdinalIgnoreCase) &&
        view != null && view.enabled && !entranceAlpha.HasValue && view.texture == current.targetTexture &&
        !waitingForLoopFrame && Time.frameCount > visibleSinceFrame;

    public bool HasReachedAnimationTime(string anim, float seconds) =>
        IsAnimationVisible(anim) && current.frameRate > 0 &&
        current.frame >= Mathf.RoundToInt(seconds * current.frameRate);

    public bool IsAnimationMovementReady(string anim)
    {
        if (!IsAnimationVisible(anim)) return false;
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
                return current.time >= entry.movementStartTime;
        return true;
    }

    public bool TryGetAnimationMovementProgress(string anim, out float progress)
    {
        progress = 0;
        if (!IsAnimationVisible(anim)) return false;
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase) &&
                entry.movementEndTime > entry.movementStartTime)
            {
                progress = Mathf.InverseLerp(entry.movementStartTime, entry.movementEndTime, (float)current.time);
                return true;
            }
        return false;
    }

    private void ChangeAnimation(string anim, bool instant, bool holdFirstFrame = false, bool? loopOverride = null)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(anim))
            return;

        anim = anim.Trim().ToLowerInvariant();
        if (anim == "death" && hitReaction != null) hitReaction.Stop();
        var next = FindClip(anim);
        if (next == null)
        {
            Debug.LogWarning($"UI video animation '{anim}' has no clip.", this);
            return;
        }

        if (anim == "appear" && current == null && !entranceAlpha.HasValue)
        {
            entranceAlpha = view.color.a;
            view.color = WithAlpha(view.color, 0);
            view.enabled = false;
        }

        bool loop = loopOverride ?? (anim == "idle" || anim == "walk" || anim == "run");
        startDefaultIdle = false;
        if (pending != null && pendingName == anim && pendingLoop == loop)
        {
            pendingInstant = instant;
            if (!holdFirstFrame && pendingHoldFirstFrame)
            {
                pendingHoldFirstFrame = false;
                if (pendingFrameReadyAt >= 0) pending.Play();
            }
            if (instant && fading) FinishFade();
            return;
        }

        CancelPending();
        if (current != null && currentName == anim)
        {
            if (currentHeld)
            {
                if (!holdFirstFrame)
                {
                    currentHeld = false;
                    current.Play();
                }
                return;
            }

            if (!holdFirstFrame && currentLoop == loop && (current.isPlaying || currentLoop)) return;
        }

        StartClip(anim, next, loop, instant, holdFirstFrame);
    }

    private VideoClip FindClip(string anim)
    {
        foreach (var entry in animations)
            if (entry != null && entry.clip != null &&
                string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
                return entry.clip;

        return anim == "idle" ? clip : null;
    }

    private void ApplyFraming(UiVideoAspect aspect, string anim)
    {
        if (aspect == null) return;
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
            {
                aspect.SetFraming(entry.visualScale, entry.visualOffset);
                return;
            }
        aspect.SetFraming(1f, Vector2.zero);
    }

    private void StartClip(string anim, VideoClip next, bool loop, bool instant = false, bool holdFirstFrame = false)
    {
        if (current == primary && secondary == null)
            secondary = gameObject.AddComponent<VideoPlayer>();

        pending = current == primary ? secondary : primary;
        pendingName = anim;
        pendingInstant = instant;
        pendingLoop = loop;
        pendingFinished = false;
        pendingFrameReadyAt = -1;
        pendingPlaybackStarted = false;
        pendingHoldFirstFrame = holdFirstFrame;
        double startTime = 0;
        foreach (var entry in animations)
            if (entry != null && string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
            {
                startTime = Math.Min(entry.startTime, Math.Max(0, next.length - 1 / next.frameRate));
                break;
            }
        pendingStartFrame = (long)Math.Ceiling(startTime * next.frameRate);
        pending.playOnAwake = false;
        pending.source = VideoSource.VideoClip;
        pending.clip = next;
        pending.renderMode = VideoRenderMode.RenderTexture;
        pending.targetTexture = new RenderTexture((int)next.width, (int)next.height, 0, RenderTextureFormat.ARGB32)
        {
            name = $"{name} {anim}",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        pending.targetTexture.Create();
        pending.isLooping = loop && pendingStartFrame == 0;
        pending.audioOutputMode = VideoAudioOutputMode.None;
        pending.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        pending.waitForFirstFrame = true;
        pending.sendFrameReadyEvents = true;
        pending.prepareCompleted += Prepared;
        pending.frameReady += FirstFrame;
        pending.errorReceived += Failed;
        pending.loopPointReached += LoopPointReached;
        pending.Prepare();
    }

    private void Prepared(VideoPlayer source)
    {
        if (source != pending) return;
        if (pendingStartFrame > 0) source.frame = pendingStartFrame;
        pendingPlaybackStarted = true;
        source.Play();
    }

    private void FirstFrame(VideoPlayer source, long frame)
    {
        if (source != pending || !pendingPlaybackStarted || frame < pendingStartFrame) return;
        if (pendingHoldFirstFrame) source.Pause();
        source.sendFrameReadyEvents = false;
        source.frameReady -= FirstFrame;
        // frameReady can precede the decoder's copy into the RenderTexture.
        pendingFrameReadyAt = Time.frameCount;
    }

    private void LateUpdate()
    {
        if (startDefaultIdle)
        {
            startDefaultIdle = false;
            var idle = FindClip("idle");
            if (idle != null) StartClip("idle", idle, true);
        }

        if (pendingFrameReadyAt >= 0 && Time.frameCount > pendingFrameReadyAt && pending != null)
        {
            pendingFrameReadyAt = -1;
            BeginTransition();
        }

        if (waitingForLoopFrame && loopFrameReadyAt >= 0 && Time.frameCount > loopFrameReadyAt)
        {
            view.texture = current.targetTexture;
            RetireLoopHold();
            waitingForLoopFrame = false;
            visibleSinceFrame = Time.frameCount;
        }

        if (current != null && Time.frameCount > visibleSinceFrame) RestoreEntranceAlpha();
        ReleaseRetiredTextures(false);
    }

    private void BeginTransition()
    {
        if (current == null || pendingInstant || crossFadeDuration <= 0)
        {
            FinishFade();
            return;
        }

        if (overlay == null)
        {
            var layer = new GameObject("Video CrossFade", typeof(RectTransform), typeof(RawImage), typeof(UiVideoAspect));
            layer.transform.SetParent(transform, false);
            overlay = layer.GetComponent<RawImage>();
            overlayAspect = layer.GetComponent<UiVideoAspect>();
            overlay.rectTransform.anchorMin = Vector2.zero;
            overlay.rectTransform.anchorMax = Vector2.one;
            overlay.rectTransform.offsetMin = Vector2.zero;
            overlay.rectTransform.offsetMax = Vector2.zero;
            overlay.raycastTarget = false;
        }

        overlay.material = view.material;
        overlay.uvRect = view.uvRect;
        overlay.maskable = view.maskable;
        overlay.texture = pending.targetTexture;
        ApplyFraming(overlayAspect, pendingName);
        fadeColor = view.color;
        overlay.color = WithAlpha(fadeColor, 0);
        overlay.enabled = true;
        elapsed = 0;
        fading = true;
    }

    private void Update()
    {
        if (!fading)
            return;

        elapsed += Time.unscaledDeltaTime;
        float progress = crossFadeDuration > 0 ? Mathf.Clamp01(elapsed / crossFadeDuration) : 1;
        view.color = WithAlpha(fadeColor, 1 - progress);
        overlay.color = WithAlpha(fadeColor, progress);
        if (progress >= 1)
            FinishFade();
    }

    private void FinishFade()
    {
        ClearOverlay();
        var outgoing = current;
        current = pending;
        currentName = pendingName;
        currentStartFrame = pendingStartFrame;
        currentLoop = pendingLoop;
        currentFinished = pendingFinished;
        currentHeld = pendingHoldFirstFrame;
        RetireLoopHold();
        waitingForLoopFrame = false;
        visibleSinceFrame = Time.frameCount;
        pending = null;
        pendingName = null;
        view.texture = current.targetTexture;
        view.enabled = true;
        ApplyFraming(viewAspect, currentName);
        Release(outgoing);
    }

    private void CancelPending()
    {
        ClearOverlay();
        var cancelled = pending;
        pendingFrameReadyAt = -1;
        pending = null;
        pendingName = null;
        Release(cancelled);
    }

    private void ClearOverlay()
    {
        if (fading)
            view.color = fadeColor;
        fading = false;
        if (overlay != null)
        {
            overlay.enabled = false;
            overlay.texture = null;
        }
    }

    private void LoopPointReached(VideoPlayer source)
    {
        if (source == pending && !pendingLoop) pendingFinished = true;
        if (source == current && !currentLoop) currentFinished = true;
        if (source != current || !currentLoop || currentStartFrame <= 0 || waitingForLoopFrame) return;
        loopHold = new RenderTexture(source.targetTexture.descriptor) { name = name + " loop hold" };
        loopHold.Create();
        Graphics.Blit(source.targetTexture, loopHold);
        view.texture = loopHold;
        waitingForLoopFrame = true;
        loopFrameReadyAt = -1;
        source.sendFrameReadyEvents = true;
        source.frameReady += LoopFrame;
        source.frame = currentStartFrame;
        source.Play();
    }

    private void LoopFrame(VideoPlayer source, long frame)
    {
        if (source != current || frame < currentStartFrame) return;
        source.frameReady -= LoopFrame;
        source.sendFrameReadyEvents = false;
        loopFrameReadyAt = Time.frameCount;
    }

    private void Failed(VideoPlayer source, string message)
    {
        Debug.LogError("UI video: " + message, this);
        if (source == pending)
            CancelPending();
        else if (source == current)
        {
            if (fading)
            {
                FinishFade();
                return;
            }
            current = null;
            currentName = null;
            ShowPoster();
            RetireLoopHold();
            Release(source);
        }
    }

    private void OnDisable()
    {
        if (hitReaction != null) hitReaction.Stop();
        CancelPending();
        var outgoing = current;
        current = null;
        currentName = null;
        RestoreEntranceAlpha();
        ShowPoster();
        Release(outgoing);
        RetireLoopHold();
        ReleaseRetiredTextures(true);
    }

    private void RestoreEntranceAlpha()
    {
        if (!entranceAlpha.HasValue) return;
        var color = view.color;
        color.a = entranceAlpha.Value;
        view.color = color;
        entranceAlpha = null;
    }

    private void ShowPoster()
    {
        if (view == null)
            return;
        view.texture = poster;
        if (viewAspect != null) viewAspect.SetFraming(1f, Vector2.zero);
        view.enabled = poster != null && !entranceAlpha.HasValue;
    }

    private void Release(VideoPlayer player)
    {
        if (player == null)
            return;

        if (player == frameSyncedPlayer) RestoreFrameSkipping();

        player.prepareCompleted -= Prepared;
        player.frameReady -= FirstFrame;
        player.errorReceived -= Failed;
        player.loopPointReached -= LoopPointReached;
        player.frameReady -= LoopFrame;
        player.sendFrameReadyEvents = false;
        var target = player.targetTexture;
        player.targetTexture = null;
        player.Stop();
        if (target != null)
            retiredTextures.Add((target, Time.frameCount));
    }

    private void RetireLoopHold()
    {
        if (loopHold == null) return;
        retiredTextures.Add((loopHold, Time.frameCount));
        loopHold = null;
    }

    private void ReleaseRetiredTextures(bool all)
    {
        for (int i = retiredTextures.Count - 1; i >= 0; i--)
        {
            var retired = retiredTextures[i];
            if (!all && Time.frameCount <= retired.Frame + 1) continue;
            retired.Texture.Release();
            if (Application.isPlaying) Destroy(retired.Texture);
            else DestroyImmediate(retired.Texture);
            retiredTextures.RemoveAt(i);
        }
    }

    private static Color WithAlpha(Color color, float factor)
    {
        color.a *= factor;
        return color;
    }
}
