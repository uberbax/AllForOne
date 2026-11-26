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
    private RawImage view;
    private Color homeColor;
    private bool fled;
    private Coroutine behavior;
    private int lastIdleAction = -1;
    private Vector3 homePosition;
    private Vector3 homeScale;
    private bool strollRight;

    private void Awake()
    {
        video = GetComponent<UiLoopVideo>();
        view = GetComponent<RawImage>();
        homeColor = view.color;
        homePosition = transform.localPosition;
        homeScale = transform.localScale;
        strollRight = homeScale.x < 0f;

        if (strollDistance <= 0f) return;
        if (roadMaterial != null) view.material = roadMaterial;
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

    private void OnEnable()
    {
        if (!fled) behavior = StartCoroutine(IdleRoutine());
    }

    private void OnDisable()
    {
        if (behavior != null) StopCoroutine(behavior);
        behavior = null;
        if (strollDistance > 0f || fled)
        {
            view.color = homeColor;
            transform.localPosition = homePosition;
            transform.localScale = homeScale;
            strollRight = homeScale.x < 0f;
        }
    }

    public bool ScareIfNear(float previousHeroX, float heroX, float radius)
    {
        if (fled || !isActiveAndEnabled || !video.HasAnimation("fly")) return false;

        float x = transform.position.x;
        float nearest = Mathf.Clamp(x, Mathf.Min(previousHeroX, heroX), Mathf.Max(previousHeroX, heroX));
        if (Mathf.Abs(x - nearest) > radius) return false;

        fled = true;
        if (behavior != null) StopCoroutine(behavior);
        behavior = StartCoroutine(Flee(x >= previousHeroX ? 1f : -1f));
        return true;
    }

    public void ResetBattleFlight()
    {
        if (!fled) return;

        fled = false;
        transform.localPosition = homePosition;
        transform.localScale = homeScale;
        view.color = homeColor;
        strollRight = homeScale.x < 0f;
        if (roadShadow != null) roadShadow.gameObject.SetActive(strollDistance > 0f);

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        else if (isActiveAndEnabled)
        {
            if (behavior != null) StopCoroutine(behavior);
            behavior = StartCoroutine(IdleRoutine());
        }
    }

    private IEnumerator Flee(float side)
    {
        video.PlayOnce("fly");
        float deadline = Time.unscaledTime + 2f;
        while (!video.IsAnimationVisible("fly") && Time.unscaledTime < deadline) yield return null;

        deadline = Time.unscaledTime + .45f;
        while (!video.HasReachedAnimationTime("fly", .25f) && Time.unscaledTime < deadline) yield return null;

        var scale = transform.localScale;
        scale.x = -side * Mathf.Abs(scale.x);
        transform.localScale = scale;
        if (roadShadow != null) roadShadow.gameObject.SetActive(false);

        const float duration = 1.2f;
        var start = transform.position;
        var displacement = new Vector3(side * 3.8f, 3.2f, 0f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            transform.position = start + displacement * (progress * (.5f + .5f * progress));
            var color = homeColor;
            color.a *= 1f - Mathf.InverseLerp(.75f, 1f, progress);
            view.color = color;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        gameObject.SetActive(false);
    }

    public void Fly()
    {
        if (!isActiveAndEnabled || fled) return;
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
            int choice = ChooseIdleAction();
            if (choice == IdleVariants.Length) yield return Stroll();
            else yield return PlayOnce(IdleVariants[choice]);
        }
    }

    private int ChooseIdleAction()
    {
        int count = IdleVariants.Length + (strollDistance > 0f ? 1 : 0);
        int choice = Random.Range(0, count - (lastIdleAction >= 0 ? 1 : 0));
        if (lastIdleAction >= 0 && choice >= lastIdleAction) choice++;
        lastIdleAction = choice;
        return choice;
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
