using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// A separate decoder lets the underlying attack/run keep its playback position.
[RequireComponent(typeof(UiLoopVideo), typeof(RawImage))]
[DefaultExecutionOrder(100)]
public sealed class UiVideoHitReaction : MonoBehaviour
{
    private UiLoopVideo owner;
    private RawImage mainView, fadeView, reactionView;
    private UiVideoAspect aspect;
    private VideoPlayer player;
    private bool active, showing, fadeHidden;
    private float mainAlpha, fadeAlpha;
    private int readyAt = -1, endedAt = -1;
    private long startFrame;
    private float deadline;
    public bool Playing => active;

    private void Awake()
    {
        owner = GetComponent<UiLoopVideo>();
        mainView = GetComponent<RawImage>();
        CacheFadeView();
    }

    private void OnTransformChildrenChanged() => CacheFadeView();

    private void CacheFadeView()
    {
        var layer = transform.Find("Video CrossFade");
        var next = layer != null ? layer.GetComponent<RawImage>() : null;
        if (next == fadeView) return;
        fadeView = next;
        fadeHidden = false;
    }

    public void Play(VideoClip clip, float startTime, float scale, Vector2 offset)
    {
        if (!isActiveAndEnabled || clip == null) return;
        if (active && player != null && player.clip == clip)
        {
            if (player.isPrepared)
            {
                endedAt = -1;
                deadline = Time.unscaledTime + (float)clip.length + 10f;
                player.frame = startFrame;
                player.Play();
            }
            return;
        }
        Stop();
        if (reactionView == null)
        {
            var layer = new GameObject("Video Hit Reaction", typeof(RectTransform), typeof(RawImage),
                typeof(UiVideoAspect), typeof(VideoPlayer));
            layer.layer = gameObject.layer;
            layer.transform.SetParent(transform, false);
            var rect = (RectTransform)layer.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            reactionView = layer.GetComponent<RawImage>();
            reactionView.raycastTarget = false;
            aspect = layer.GetComponent<UiVideoAspect>();
            player = layer.GetComponent<VideoPlayer>();
        }
        reactionView.transform.SetAsLastSibling();
        reactionView.material = mainView.material;
        reactionView.color = Color.white;
        reactionView.uvRect = mainView.uvRect;
        reactionView.maskable = mainView.maskable;
        aspect.SetFraming(scale, offset);
        reactionView.enabled = false;
        var texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32)
        {
            name = name + " hit", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
        };
        texture.Create();
        reactionView.texture = texture;
        player.playOnAwake = false;
        player.source = VideoSource.VideoClip;
        player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.isLooping = false;
        player.audioOutputMode = VideoAudioOutputMode.None;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.waitForFirstFrame = true;
        player.sendFrameReadyEvents = true;
        startFrame = (long)System.Math.Ceiling(System.Math.Min(startTime,
            System.Math.Max(0, clip.length - 1 / clip.frameRate)) * clip.frameRate);
        readyAt = endedAt = -1;
        active = true;
        deadline = Time.unscaledTime + (float)clip.length + 10f;
        player.prepareCompleted += Prepared;
        player.frameReady += FrameReady;
        player.loopPointReached += Ended;
        player.errorReceived += Failed;
        player.Prepare();
    }

    private void Prepared(VideoPlayer source)
    {
        if (!active || source != player) return;
        if (startFrame > 0) source.frame = startFrame;
        source.Play();
    }

    private void FrameReady(VideoPlayer source, long frame)
    {
        if (!active || source != player || frame < startFrame) return;
        source.frameReady -= FrameReady;
        source.sendFrameReadyEvents = false;
        readyAt = Time.frameCount;
    }

    private void Ended(VideoPlayer source) { if (source == player) endedAt = Time.frameCount; }
    private void Failed(VideoPlayer source, string message)
    {
        Debug.LogWarning("UI hit video: " + message, this);
        Stop();
    }

    private void LateUpdate()
    {
        if (!active) return;
        if (Time.unscaledTime >= deadline) { Stop(); return; }
        if (endedAt >= 0 && Time.frameCount > endedAt) { Stop(); return; }
        if (!showing && readyAt >= 0 && Time.frameCount > readyAt)
        {
            mainAlpha = mainView.canvasRenderer.GetAlpha();
            showing = true;
            reactionView.enabled = true;
        }
        if (!showing) return;
        mainView.canvasRenderer.SetAlpha(0);
        if (fadeView != null)
        {
            if (!fadeHidden) { fadeAlpha = fadeView.canvasRenderer.GetAlpha(); fadeHidden = true; }
            fadeView.canvasRenderer.SetAlpha(0);
        }
    }

    public void Stop()
    {
        if (showing && mainView != null) mainView.canvasRenderer.SetAlpha(mainAlpha);
        if (fadeHidden && fadeView != null) fadeView.canvasRenderer.SetAlpha(fadeAlpha);
        active = showing = fadeHidden = false;
        readyAt = endedAt = -1;
        if (reactionView != null) { reactionView.enabled = false; reactionView.texture = null; }
        if (player == null) return;
        player.prepareCompleted -= Prepared;
        player.frameReady -= FrameReady;
        player.loopPointReached -= Ended;
        player.errorReceived -= Failed;
        player.sendFrameReadyEvents = false;
        var texture = player.targetTexture;
        player.targetTexture = null;
        player.Stop();
        if (texture != null) owner.RetireTexture(texture);
    }

    private void OnDisable() => Stop();
}
