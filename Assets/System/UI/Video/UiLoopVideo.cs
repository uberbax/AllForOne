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
    private float elapsed;
    private Color fadeColor;

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

    public void CrossFade(string anim)
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
            return;

        CancelPending();
        if (current != null && currentName == anim && (current.isPlaying || current.isLooping))
            return;

        StartClip(anim, next, anim == "idle" || anim == "walk");
    }

    private VideoClip FindClip(string anim)
    {
        foreach (var entry in animations)
            if (entry != null && entry.clip != null &&
                string.Equals(entry.anim, anim, StringComparison.OrdinalIgnoreCase))
                return entry.clip;

        return anim == "idle" ? clip : null;
    }

    private void StartClip(string anim, VideoClip next, bool loop)
    {
        if (current == primary && secondary == null)
            secondary = gameObject.AddComponent<VideoPlayer>();

        pending = current == primary ? secondary : primary;
        pendingName = anim;
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
        pending.isLooping = loop;
        pending.audioOutputMode = VideoAudioOutputMode.None;
        pending.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        pending.waitForFirstFrame = true;
        pending.sendFrameReadyEvents = true;
        pending.prepareCompleted += Prepared;
        pending.frameReady += FirstFrame;
        pending.errorReceived += Failed;
        pending.Prepare();
    }

    private void Prepared(VideoPlayer source)
    {
        if (source == pending)
            source.Play();
    }

    private void FirstFrame(VideoPlayer source, long frame)
    {
        if (source != pending)
            return;

        source.sendFrameReadyEvents = false;
        source.frameReady -= FirstFrame;
        if (current == null || crossFadeDuration <= 0)
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
        overlay.texture = source.targetTexture;
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
        player.sendFrameReadyEvents = false;
        player.Stop();
        var target = player.targetTexture;
        player.targetTexture = null;
        if (target != null)
        {
            target.Release();
            Destroy(target);
        }
    }

    private static Color WithAlpha(Color color, float factor)
    {
        color.a *= factor;
        return color;
    }
}
