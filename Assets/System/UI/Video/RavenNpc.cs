using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(UiLoopVideo))]
public sealed class RavenNpc : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float minIdleDelay = 3f;
    [SerializeField, Min(0.1f)] private float maxIdleDelay = 7f;
    [SerializeField, Min(0f)] private float strollDistance;
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Canvas roadShadow;

    private static readonly string[] IdleVariants = { "idle_peck", "idle_clean", "idle_hohl" };
    private UiLoopVideo video;
    private Coroutine behavior;
    private int nextIdle;
    private Vector3 homePosition;
    private Vector3 homeScale;
    private bool strollRight;

    private void Awake()
    {
        video = GetComponent<UiLoopVideo>();
        homePosition = transform.localPosition;
        homeScale = transform.localScale;
        strollRight = homeScale.x < 0f;

        if (strollDistance <= 0f) return;
        if (roadMaterial != null) GetComponent<RawImage>().material = roadMaterial;
        if (roadShadow == null) return;

        roadShadow.gameObject.SetActive(true);
        var parentCanvas = GetComponentInParent<Canvas>(true);
        if (parentCanvas != null)
        {
            roadShadow.overrideSorting = true;
            roadShadow.sortingLayerID = parentCanvas.sortingLayerID;
            roadShadow.sortingOrder = parentCanvas.sortingOrder - 1;
        }
    }

    private void OnEnable() => behavior = StartCoroutine(IdleRoutine());

    private void OnDisable()
    {
        if (behavior != null) StopCoroutine(behavior);
        behavior = null;
        if (strollDistance > 0f)
        {
            transform.localPosition = homePosition;
            transform.localScale = homeScale;
            strollRight = homeScale.x < 0f;
        }
    }

    public void Fly()
    {
        if (!isActiveAndEnabled) return;
        if (behavior != null) StopCoroutine(behavior);
        behavior = StartCoroutine(IdleRoutine(playFlyFirst: true));
    }

    private IEnumerator IdleRoutine(bool playFlyFirst = false)
    {
        if (playFlyFirst) yield return PlayOnce("fly");

        while (true)
        {
            video.Play("idle");
            yield return new WaitForSecondsRealtime(Random.Range(minIdleDelay, Mathf.Max(minIdleDelay, maxIdleDelay)));
            if (strollDistance > 0f && nextIdle == 0) yield return Stroll();
            yield return PlayOnce(IdleVariants[nextIdle]);
            nextIdle = (nextIdle + 1) % IdleVariants.Length;
        }
    }

    private IEnumerator Stroll()
    {
        if (!video.HasAnimation("walk")) yield break;
        var side = strollRight ? 1f : -1f;
        var target = transform.parent.TransformPoint(homePosition).x + side * strollDistance;
        var position = transform.position;
        var startX = position.x;
        strollRight = !strollRight;
        video.PlayOnce("walk");

        // Finish translating on the planted frame while the last frames remain visible.
        float lastProgress = -1f;
        float deadline = Time.unscaledTime + video.GetAnimationDuration("walk") + 2f;
        while (!video.IsAnimationFinished("walk") && Time.unscaledTime < deadline)
        {
            if (video.TryGetAnimationMovementProgress("walk", out float progress) && progress > lastProgress)
            {
                var scale = transform.localScale;
                scale.x = -side * Mathf.Abs(scale.x);
                transform.localScale = scale;
                lastProgress = progress;
                position.x = Mathf.SmoothStep(startX, target, progress);
                transform.position = position;
                if (progress >= 1f) yield break;
            }
            yield return null;
        }
    }

    private IEnumerator PlayOnce(string clip)
    {
        if (!video.HasAnimation(clip)) yield break;
        // UiLoopVideo keeps the current frame until the new clip is ready to show.
        video.Play(clip);
        float deadline = Time.unscaledTime + video.GetAnimationDuration(clip) + 2f;
        do
        {
            yield return null;
        } while (!video.IsAnimationFinished(clip) && Time.unscaledTime < deadline);
    }
}
