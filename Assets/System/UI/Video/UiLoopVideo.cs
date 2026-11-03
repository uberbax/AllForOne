using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(RawImage), typeof(VideoPlayer))]
public sealed class UiLoopVideo : MonoBehaviour
{
    [SerializeField] private VideoClip clip;
    [SerializeField] private Texture poster;
    private RawImage view;
    private VideoPlayer player;
    private RenderTexture target;

    private void OnEnable()
    {
        view = GetComponent<RawImage>(); player = GetComponent<VideoPlayer>();
        if (clip == null) { Debug.LogError("UI video clip is missing", this); return; }
        target = new RenderTexture(Mathf.Max(2, (int)clip.width), Mathf.Max(2, (int)clip.height), 0, RenderTextureFormat.ARGB32)
        { name = name + " Video", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        target.Create();
        view.texture = target; view.enabled = false; view.raycastTarget = false;
        player.playOnAwake = false; player.source = VideoSource.VideoClip; player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture; player.targetTexture = target;
        player.isLooping = true; player.audioOutputMode = VideoAudioOutputMode.None;
        player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        player.waitForFirstFrame = true; player.sendFrameReadyEvents = true;
        player.prepareCompleted += Prepared; player.frameReady += FirstFrame; player.errorReceived += Failed;
        player.Prepare();
    }
    private void Prepared(VideoPlayer source) { if (isActiveAndEnabled) source.Play(); }
    private void FirstFrame(VideoPlayer source, long frame)
    {
        view.enabled = true; source.sendFrameReadyEvents = false;
    }
    private void Failed(VideoPlayer source, string message)
    {
        Debug.LogError("UI video: " + message, this); Release();
    }
    private void OnDisable() { Release(); }
    private void Release()
    {
        if (player != null)
        {
            player.prepareCompleted -= Prepared; player.frameReady -= FirstFrame; player.errorReceived -= Failed;
            player.Stop(); player.targetTexture = null;
        }
        if (view != null) { view.texture = poster; view.enabled = poster != null; }
        if (target != null) { target.Release(); Destroy(target); target = null; }
    }
}
