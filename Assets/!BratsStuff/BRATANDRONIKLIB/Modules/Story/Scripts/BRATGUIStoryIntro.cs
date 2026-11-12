using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Standalone illustrated prologue. Only controls explicitly assigned view objects.</summary>
[DisallowMultipleComponent]
public sealed class BRATGUIStoryIntro : MonoBehaviour
{
    [Serializable]
    public sealed class IllustrationLayer
    {
        public RectTransform image;
        [Tooltip("Небольшое независимое перемещение слоя за эпизод, в единицах Canvas.")]
        public Vector2 travel = new Vector2(10f, 2f);
        [Range(1f, 1.05f)] public float finalScale = 1f;
    }

    [Serializable]
    public sealed class Chapter
    {
        public string title;
        [TextArea(4, 8)] public string narration;
        [Tooltip("Separate illustration object belonging to this chapter.")]
        public RectTransform illustration;
        public IllustrationLayer[] layers = Array.Empty<IllustrationLayer>();
        [Tooltip("Total slow travel in canvas units. The painted pose stays static.")]
        public Vector2 travel = new Vector2(28f, 12f);
        [Range(1f, 1.1f)] public float finalScale = 1.035f;
        [Min(0f)] public float extraReadingTime = 0f;
    }

    [Header("Chapters in book order")]
    public Chapter[] chapters = Array.Empty<Chapter>();
    [Header("View references — all belong to this scene")]
    public RectTransform canvasRect;
    public RectTransform illustrationFrame;
    public RectTransform narrationFrame;
    public RectTransform background;
    public TMP_Text chapterLabel;
    public TMP_Text heading;
    public TMP_Text storyText;
    public Image blackFade;
    public Image[] chapterMarks = Array.Empty<Image>();
    public GameObject replayButton;
    public Button nextButton;
    public Button skipButton;
    [Header("Playback")]
    [Tooltip("Play() — начать сначала. Next() — перейти к следующей главе через затемнение. SkipStory() — завершить вступление через затемнение и вызвать On Completed. Stop() — вернуть первый кадр. Загрузка другой сцены здесь не выполняется.")]
    public bool playOnEnable = true;
    [Min(1f)] public float charactersPerSecond = 28f;
    [Min(0f)] public float punctuationPause = 0.15f;
    [Min(0.01f)] public float fadeDuration = 1.25f;
    [Min(0f)] public float blackHold = 0.3f;
    [Min(0f)] public float readingHold = 3.5f;
    [Tooltip("Called once after all six chapters. Assign the next action here when integrating the intro into the game.")]
    public UnityEvent onCompleted = new UnityEvent();

    public int CurrentChapter { get; private set; } = -1;
    public bool IsPlaying { get; private set; }

    private Coroutine _sequence;
    private Vector2[] _positions;
    private Vector3[] _scales;
    private Vector2[][] _layerPositions;
    private Vector3[][] _layerScales;
    private Vector2 _backgroundPosition;
    private Vector2 _lastCanvasSize;
    private float _shotTime;
    private float _shotDuration;
    private bool _typing;
    private bool _reveal;
    private bool _advance;
    private bool _layoutBusy;
    private bool _transitioning;
    private bool _completionSent;

    private void Awake()
    {
        CacheOrigins();
        ApplyLayout();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    private void OnDisable() => Stop();

    private void CacheOrigins()
    {
        bool cached = _positions != null && _positions.Length == chapters.Length &&
                      _layerPositions != null && _layerPositions.Length == chapters.Length;
        if (cached)
        {
            for (int i = 0; i < chapters.Length; i++)
                if (_layerPositions[i] == null || _layerPositions[i].Length != (chapters[i]?.layers?.Length ?? 0))
                {
                    cached = false;
                    break;
                }
        }
        if (cached) return;
        _positions = new Vector2[chapters.Length];
        _scales = new Vector3[chapters.Length];
        _layerPositions = new Vector2[chapters.Length][];
        _layerScales = new Vector3[chapters.Length][];
        for (int i = 0; i < chapters.Length; i++)
        {
            int count = chapters[i]?.layers?.Length ?? 0;
            _layerPositions[i] = new Vector2[count];
            _layerScales[i] = new Vector3[count];
            for (int n = 0; n < count; n++)
            {
                var layer = chapters[i].layers[n];
                if (layer == null || !layer.image) continue;
                _layerPositions[i][n] = layer.image.anchoredPosition;
                _layerScales[i][n] = layer.image.localScale;
            }
            if (chapters[i] == null || !chapters[i].illustration) continue;
            _positions[i] = chapters[i].illustration.anchoredPosition;
            _scales[i] = chapters[i].illustration.localScale;
        }
        if (background) _backgroundPosition = background.anchoredPosition;
    }

    [ContextMenu("Play prologue (Play Mode)")]
    public void Play()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        Stop();
        if (chapters.Length == 0 || !storyText || !heading || !blackFade)
        {
            Debug.LogWarning("Story Intro: assign chapters, heading, story text and black fade.", this);
            return;
        }
        IsPlaying = true;
        _completionSent = false;
        ShowNavigation(true);
        if (replayButton) replayButton.SetActive(false);
        _sequence = StartCoroutine(Sequence());
    }

    public void Next()
    {
        if (!IsPlaying || _transitioning) return;
        _reveal = true;
        _advance = true;
    }

    public void SkipStory()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !IsPlaying || _completionSent) return;
        if (_sequence != null) StopCoroutine(_sequence);
        _typing = false;
        _sequence = StartCoroutine(FinishSkipped());
    }

    private IEnumerator FinishSkipped()
    {
        ShowNavigation(false);
        yield return Fade(blackFade ? blackFade.color.a : 0f, 1f);
        Complete();
    }

    private void ShowNavigation(bool visible)
    {
        if (nextButton) nextButton.gameObject.SetActive(visible);
        if (skipButton) skipButton.gameObject.SetActive(visible);
    }

    private void Complete()
    {
        IsPlaying = false;
        _sequence = null;
        ShowNavigation(false);
        if (replayButton) replayButton.SetActive(true);
        if (_completionSent) return;
        _completionSent = true;
        onCompleted.Invoke();
    }

    public void Stop()
    {
        if (_sequence != null) StopCoroutine(_sequence);
        _sequence = null;
        IsPlaying = false;
        _typing = _reveal = _advance = false;
        _transitioning = false;
        CacheOrigins();
        for (int i = 0; i < chapters.Length; i++)
        {
            int count = chapters[i]?.layers?.Length ?? 0;
            for (int n = 0; n < count; n++)
            {
                var layer = chapters[i].layers[n];
                if (layer == null || !layer.image) continue;
                layer.image.anchoredPosition = _layerPositions[i][n];
                layer.image.localScale = _layerScales[i][n];
            }
            if (chapters[i] == null || !chapters[i].illustration) continue;
            chapters[i].illustration.anchoredPosition = _positions[i];
            chapters[i].illustration.localScale = _scales[i];
        }
        if (background) background.anchoredPosition = _backgroundPosition;
        if (chapters.Length > 0) SetChapter(0, true);
        SetBlack(0f);
        if (replayButton) replayButton.SetActive(false);
        ShowNavigation(true);
    }

    private void Update()
    {
        if (canvasRect && canvasRect.rect.size != _lastCanvasSize) ApplyLayout();
        if (!IsPlaying || CurrentChapter < 0) return;
        _shotTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_shotTime / Mathf.Max(1f, _shotDuration));
        float smooth = t * t * (3f - 2f * t);
        Chapter chapter = chapters[CurrentChapter];
        int count = chapter.layers?.Length ?? 0;
        for (int n = 0; n < count; n++)
        {
            var layer = chapter.layers[n];
            if (layer == null || !layer.image) continue;
            layer.image.anchoredPosition = _layerPositions[CurrentChapter][n] + layer.travel * (smooth - 0.5f);
            layer.image.localScale = _layerScales[CurrentChapter][n] * Mathf.Lerp(1f, layer.finalScale, smooth);
        }
        if (chapter.illustration)
        {
            chapter.illustration.anchoredPosition = _positions[CurrentChapter] + chapter.travel * (smooth - 0.5f);
            chapter.illustration.localScale = _scales[CurrentChapter] * Mathf.Lerp(1f, chapter.finalScale, smooth);
        }
        if (background) background.anchoredPosition = _backgroundPosition - chapter.travel * (smooth - 0.5f) * 0.45f;
    }

    private IEnumerator Sequence()
    {
        SetBlack(1f);
        for (int i = 0; i < chapters.Length; i++)
        {
            SetChapter(i, false);
            _advance = _reveal = false;
            yield return Fade(1f, 0f);
            _typing = true;
            int count = storyText.textInfo.characterCount;
            float due = 0f;
            for (int visible = 0; visible < count;)
            {
                if (_reveal) break;
                due -= Time.unscaledDeltaTime;
                while (due <= 0f && visible < count)
                {
                    char c = storyText.textInfo.characterInfo[visible++].character;
                    storyText.maxVisibleCharacters = visible;
                    due += 1f / Mathf.Max(1f, charactersPerSecond);
                    if (c == '.' || c == ',' || c == ';' || c == '—') due += punctuationPause;
                }
                yield return null;
            }
            storyText.maxVisibleCharacters = int.MaxValue;
            _typing = false;
            float hold = readingHold + chapters[i].extraReadingTime;
            for (float t = 0; t < hold && !_advance; t += Time.unscaledDeltaTime) yield return null;
            if (i == chapters.Length - 1)
            {
                if (_advance) yield return Fade(0f, 1f);
                break;
            }
            yield return Fade(0f, 1f);
            for (float t = 0; t < blackHold; t += Time.unscaledDeltaTime) yield return null;
        }
        Complete();
    }

    private void SetChapter(int index, bool reveal)
    {
        CurrentChapter = index;
        for (int i = 0; i < chapters.Length; i++)
            if (chapters[i] != null && chapters[i].illustration) chapters[i].illustration.gameObject.SetActive(i == index);
        Chapter chapter = chapters[index];
        if (chapter == null) return;
        if (chapterLabel) chapterLabel.text = "Odium  /  Chapter " + (index + 1).ToString("00");
        if (heading) heading.text = chapter.title;
        if (storyText)
        {
            storyText.text = chapter.narration;
            storyText.maxVisibleCharacters = reveal ? int.MaxValue : 0;
            storyText.ForceMeshUpdate();
        }
        for (int i = 0; i < chapterMarks.Length; i++)
            if (chapterMarks[i]) chapterMarks[i].color = i == index ? new Color(0.68f, 0.46f, 0.29f) : new Color(0.27f, 0.29f, 0.3f, 0.7f);
        _shotTime = 0f;
        int length = chapter.narration == null ? 0 : chapter.narration.Length;
        _shotDuration = length / Mathf.Max(1f, charactersPerSecond) + readingHold + chapter.extraReadingTime + fadeDuration * 2f + 2f;
    }

    private IEnumerator Fade(float from, float to)
    {
        _transitioning = true;
        if (nextButton) nextButton.interactable = false;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            SetBlack(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / Mathf.Max(0.01f, fadeDuration))));
            yield return null;
        }
        SetBlack(to);
        _transitioning = false;
        if (nextButton) nextButton.interactable = true;
    }

    private void SetBlack(float alpha)
    {
        if (!blackFade) return;
        blackFade.color = new Color(0f, 0f, 0f, alpha);
        blackFade.raycastTarget = alpha > 0.01f;
    }

    // Layout only changes when the canvas changes size; motion stays local to each illustration.
    public void ApplyLayout()
    {
        if (_layoutBusy || !canvasRect || !illustrationFrame || !narrationFrame) return;
        _layoutBusy = true;
        _lastCanvasSize = canvasRect.rect.size;
        bool portrait = _lastCanvasSize.x < _lastCanvasSize.y;
        illustrationFrame.anchorMin = portrait ? new Vector2(0.12f, 0.43f) : new Vector2(0.5f, 0.08f);
        illustrationFrame.anchorMax = portrait ? new Vector2(0.88f, 0.94f) : new Vector2(0.96f, 0.94f);
        narrationFrame.anchorMin = portrait ? new Vector2(0.1f, 0.11f) : new Vector2(0.075f, 0.22f);
        narrationFrame.anchorMax = portrait ? new Vector2(0.9f, 0.4f) : new Vector2(0.455f, 0.79f);
        illustrationFrame.offsetMin = illustrationFrame.offsetMax = Vector2.zero;
        narrationFrame.offsetMin = narrationFrame.offsetMax = Vector2.zero;
        if (heading) heading.fontSize = portrait ? 80f : 76f;
        if (storyText) storyText.fontSize = portrait ? 48f : 36f;
        if (chapterLabel) chapterLabel.fontSize = portrait ? 29f : 22f;
        _layoutBusy = false;
    }
}
