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
    }

    [SerializeField] private VideoClip clip;
    [SerializeField] private Texture poster;
    [SerializeField] private List<AnimationClipEntry> animations = new List<AnimationClipEntry>();
    [SerializeField, Min(0)] private float crossFadeDuration = 0.15f;

    private RawImage view;
    private RawImage overlay;
    private VideoPlayer primary;
    private VideoPlayer secondary;
    private VideoPlayer current;
    private VideoPlayer pending;
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
    private int pendingFrameReadyAt = -1;
    private int visibleSinceFrame = -1;
    private bool waitingForLoopFrame;
    private RenderTexture loopHold;
    private int loopFrameReadyAt = -1;
    private readonly List<(RenderTexture Texture, int Frame)> retiredTextures =
        new List<(RenderTexture, int)>();

    private void OnEnable()
    {
        view = GetComponent<RawImage>();
        primary = GetComponent<VideoPlayer>();
        view.raycastTarget = false;
        ShowPoster();
        var idle = FindClip("idle");
        if (idle != null)
            StartClip("idle", idle, true);
    }

    public void CrossFade(string anim) => ChangeAnimation(anim, false);

    public void Play(string anim) => ChangeAnimation(anim, true);

    public bool IsAnimationVisible(string anim) =>
        current != null && string.Equals(currentName, anim, StringComparison.OrdinalIgnoreCase) &&
        view != null && view.enabled && view.texture == current.targetTexture &&
        !waitingForLoopFrame && Time.frameCount > visibleSinceFrame;

    private void ChangeAnimation(string anim, bool instant)
    {
        if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(anim))
            return;

        anim = anim.Trim().ToLowerInvariant();
        var next = FindClip(anim);
        if (next == null)
        {
            Debug.LogWarning($"UI video animation '{anim}' has no clip.", this);
            return;
        }

        if (pending != null && pendingName == anim)
        {
            pendingInstant = instant;
            if (instant && fading) FinishFade();
            return;
        }

        CancelPending();
        if (current != null && currentName == anim && (current.isPlaying || currentLoop))
            return;

        StartClip(anim, next, anim == "idle" || anim == "walk", instant);
    }

    private VideoClip FindClip(string anim)
    {
        foreach (var entry in animations)
            if (entry != null && entry.clip != null &&
                string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
                return entry.clip;

        return anim == "idle" ? clip : null;
    }

    private void StartClip(string anim, VideoClip next, bool loop, bool instant = false)
    {
        if (current == primary && secondary == null)
            secondary = gameObject.AddComponent<VideoPlayer>();

        pending = current == primary ? secondary : primary;
        pendingName = anim;
        pendingInstant = instant;
        pendingLoop = loop;
        pendingFrameReadyAt = -1;
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
        source.Play();
    }

    private void FirstFrame(VideoPlayer source, long frame)
    {
        if (source != pending || frame < pendingStartFrame) return;
        source.sendFrameReadyEvents = false;
        source.frameReady -= FirstFrame;
        // frameReady can precede the decoder's copy into the RenderTexture.
        pendingFrameReadyAt = Time.frameCount;
    }

    private void LateUpdate()
    {
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
            var layer = new GameObject("Video CrossFade", typeof(RectTransform), typeof(RawImage));
            layer.transform.SetParent(transform, false);
            overlay = layer.GetComponent<RawImage>();
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
        RetireLoopHold();
        waitingForLoopFrame = false;
        visibleSinceFrame = Time.frameCount;
        pending = null;
        pendingName = null;
        view.texture = current.targetTexture;
        view.enabled = true;
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
        CancelPending();
        var outgoing = current;
        current = null;
        currentName = null;
        ShowPoster();
        Release(outgoing);
        RetireLoopHold();
        ReleaseRetiredTextures(true);
    }

    private void ShowPoster()
    {
        if (view == null)
            return;
        view.texture = poster;
        view.enabled = poster != null;
    }

    private void Release(VideoPlayer player)
    {
        if (player == null)
            return;

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
