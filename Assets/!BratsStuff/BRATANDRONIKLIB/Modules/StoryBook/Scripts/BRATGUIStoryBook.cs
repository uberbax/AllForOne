using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent, RequireComponent(typeof(CanvasGroup))]
public sealed class BRATGUIStoryBook : MonoBehaviour
{
    [Header("Развороты и управление")]
    [Tooltip("Развороты по порядку: page1 → page6. Open() всегда начинает с первого.")]
    [SerializeField] private CanvasGroup[] pages = new CanvasGroup[0];
    [Tooltip("Вся кнопка Back с подложкой. Скрывается на первой странице.")]
    [SerializeField] private GameObject backRoot;
    [Tooltip("Вся кнопка Next с подложкой. Скрывается на последней странице.")]
    [SerializeField] private GameObject nextRoot;
    [SerializeField] private CanvasGroup controls;
    [SerializeField] private CanvasGroup bookSpread;

    [Header("Открытие и закрытие")]
    [Tooltip("Отдельный визуальный слой закрытия; исходная иерархия страниц не меняется.")]
    [SerializeField] private CanvasGroup closingVisual;
    [SerializeField] private RawImage leftHalf;
    [SerializeField] private RawImage rightHalf;
    [SerializeField] private Image closedCover;
    [Header("Перелистывание")]
    [SerializeField] private CanvasGroup turningVisual;
    [SerializeField] private RawImage stationaryPage;
    [SerializeField] private BRATGUIBookLeaf turningLeaf;
    [Min(0.05f), Tooltip("Время переворота листа с текстом и иллюстрацией; работает при Time.timeScale = 0.")]
    [SerializeField] private float pageDuration = 0.65f;
    [Min(0.05f), Tooltip("Длительность складывания переплёта перед скрытием окна.")]
    [SerializeField] private float closeDuration = 0.55f;
    [Min(0.05f)] [SerializeField] private float fadeDuration = 0.2f;

    private CanvasGroup _window;
    private Coroutine _routine;
    private int _index;
    private bool _busy;
    private bool _closing;
    private Vector2 _closingPosition;
    private bool _initialized;
    private Graphic[][] _pageGraphics;
    private Graphic _bookGraphic;
    private BRATGUIBookSnapshot _snapshot;
    private RenderTexture _oldSpread;
    private RenderTexture _newSpread;

    private void Awake() => Initialize();
    private void Initialize()
    {
        if (_initialized) return;
        _window = GetComponent<CanvasGroup>();
        _pageGraphics = new Graphic[pages == null ? 0 : pages.Length][];
        for (int i = 0; i < _pageGraphics.Length; i++)
            _pageGraphics[i] = pages[i] != null ? pages[i].GetComponentsInChildren<Graphic>(true) : new Graphic[0];
        if (bookSpread != null) _bookGraphic = bookSpread.GetComponent<Graphic>();
        if (closingVisual != null) _closingPosition = ((RectTransform)closingVisual.transform).anchoredPosition;
        _initialized = true;
    }

    private void OnEnable()
    {
        Initialize();
        ResetVisuals();
        ShowPage(0);
    }

    private void OnDisable()
    {
        StopTransition();
        if (_initialized) ResetVisuals();
    }

    private void OnDestroy()
    {
        _snapshot?.Dispose();
        if (_oldSpread != null) { _oldSpread.Release(); Destroy(_oldSpread); }
        if (_newSpread != null) { _newSpread.Release(); Destroy(_newSpread); }
    }

    /// <summary>Открыть окно на первом развороте, в том числе из неактивного состояния.</summary>
    public void Open()
    {
        if (!enabled) return;
        Initialize();
        StopTransition();
        gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy) return;
        ResetVisuals();
        ShowPage(0);
        _busy = true;
        _routine = StartCoroutine(OpenRoutine());
    }

    /// <summary>Анимированно закрыть книгу, затем выключить объект окна.</summary>
    public void Close()
    {
        if (!isActiveAndEnabled || _closing) return;
        StopTransition();
        ResetVisuals();
        ShowPage(_index);
        _closing = _busy = true;
        _routine = StartCoroutine(CloseRoutine());
    }

    /// <summary>Следующий разворот. На последнем и во время анимации вызов игнорируется.</summary>
    public void Next() => Turn(1);

    /// <summary>Предыдущий разворот. На первом и во время анимации вызов игнорируется.</summary>
    public void Back() => Turn(-1);

    private void Turn(int direction)
    {
        if (!isActiveAndEnabled || _busy || pages == null) return;
        int target = _index + direction;
        if (target < 0 || target >= pages.Length || pages[target] == null) return;
        _busy = true;
        _routine = StartCoroutine(TurnRoutine(target));
    }

    private void ShowPage(int index)
    {
        _index = pages == null || pages.Length == 0 ? 0 : Mathf.Clamp(index, 0, pages.Length - 1);
        if (pages != null)
            for (int i = 0; i < pages.Length; i++)
                if (pages[i] != null) { pages[i].alpha = 1; pages[i].gameObject.SetActive(i == _index); }
        if (backRoot != null) backRoot.SetActive(_index > 0);
        if (nextRoot != null) nextRoot.SetActive(pages != null && _index < pages.Length - 1);
    }

    private IEnumerator OpenRoutine()
    {
        _window.interactable = false;
        _window.alpha = 0;
        bool fold = closingVisual != null && leftHalf != null && rightHalf != null && closedCover != null && bookSpread != null;
        if (fold)
        {
            if (pages != null && pages.Length > 0 && pages[0] != null) pages[0].alpha = 0;
            if (controls != null) controls.alpha = 0;
            bookSpread.alpha = 0;
            closingVisual.gameObject.SetActive(true);
            leftHalf.rectTransform.localScale = new Vector3(0, 1, 1);
            closedCover.rectTransform.localScale = Vector3.one;
        }
        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        { _window.alpha = Smooth(t / fadeDuration); yield return null; }
        _window.alpha = 1;
        if (fold)
        {
            for (float t = 0; t < closeDuration; t += Time.unscaledDeltaTime)
            {
                float cosine = Mathf.Cos((1 - Smooth(t / closeDuration)) * Mathf.PI);
                leftHalf.rectTransform.localScale = new Vector3(Mathf.Max(0, cosine), 1, 1);
                closedCover.rectTransform.localScale = new Vector3(Mathf.Max(0, -cosine), 1, 1);
                float shade = Mathf.Lerp(0.65f, 1, Mathf.Abs(cosine));
                closedCover.color = new Color(shade, shade, shade, 1);
                yield return null;
            }
            closingVisual.gameObject.SetActive(false);
            bookSpread.alpha = 1;
            for (float t = 0; t < 0.12f; t += Time.unscaledDeltaTime)
            {
                float a = Smooth(t / 0.12f);
                if (pages != null && pages.Length > 0 && pages[0] != null) pages[0].alpha = a;
                if (controls != null) controls.alpha = a;
                yield return null;
            }
            ResetVisuals();
        }
        _window.interactable = true;
        _busy = false;
        _routine = null;
    }

    private IEnumerator TurnRoutine(int target)
    {
        if (turningVisual != null && stationaryPage != null && turningLeaf != null && _bookGraphic != null)
        {
            int original = _index;
            int direction = target > original ? 1 : -1;
            var book = (RectTransform)bookSpread.transform;
            EnsureSnapshots(book);
            Canvas.ForceUpdateCanvases();
            _snapshot.Capture(book, _bookGraphic, _pageGraphics[original], _oldSpread);
            // Build the new page before showing the visual overlay, in the same frame.
            ShowPage(target);
            Canvas.ForceUpdateCanvases();
            _snapshot.Capture(book, _bookGraphic, _pageGraphics[target], _newSpread);
            stationaryPage.texture = _oldSpread;
            RectTransform stationary = stationaryPage.rectTransform;
            float x0 = direction > 0 ? 0.065f : 0.5f;
            float x1 = direction > 0 ? 0.5f : 0.945f;
            stationary.anchorMin = new Vector2(x0, 0.075f);
            stationary.anchorMax = new Vector2(x1, 0.935f);
            stationary.offsetMin = stationary.offsetMax = Vector2.zero;
            stationaryPage.uvRect = new Rect(x0, 0.075f, x1 - x0, 0.86f);
            turningLeaf.SetSheet(_oldSpread, _newSpread, direction);
            turningVisual.gameObject.SetActive(true);
            if (controls != null) controls.alpha = 0;
            for (float t = 0; t < pageDuration; t += Time.unscaledDeltaTime)
            {
                turningLeaf.SetProgress(Smooth(t / pageDuration));
                yield return null;
            }
            turningVisual.gameObject.SetActive(false);
            if (controls != null) controls.alpha = 1;
            _busy = false;
            _routine = null;
            yield break;
        }
        CanvasGroup old = pages[_index];
        float half = Mathf.Max(0.025f, pageDuration * 0.5f);
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        { if (old != null) old.alpha = 1 - Smooth(t / half); yield return null; }
        ShowPage(target);
        CanvasGroup current = pages[_index];
        current.alpha = 0;
        for (float t = 0; t < half; t += Time.unscaledDeltaTime)
        { if (current != null) current.alpha = Smooth(t / half); yield return null; }
        if (current != null) current.alpha = 1;
        _busy = false;
        _routine = null;
    }

    private void EnsureSnapshots(RectTransform book)
    {
        if (_snapshot == null) _snapshot = new BRATGUIBookSnapshot();
        int width = Mathf.Clamp(Mathf.RoundToInt(book.rect.width), 1024, 2048);
        int height = Mathf.Clamp(Mathf.RoundToInt(width * book.rect.height / Mathf.Max(1, book.rect.width)), 256, 2048);
        if (_oldSpread != null && _oldSpread.width == width && _oldSpread.height == height) return;
        if (_oldSpread != null) { _oldSpread.Release(); Destroy(_oldSpread); }
        if (_newSpread != null) { _newSpread.Release(); Destroy(_newSpread); }
        _oldSpread = new RenderTexture(width, height, 24) { name = "Story old spread", hideFlags = HideFlags.HideAndDontSave };
        _newSpread = new RenderTexture(width, height, 24) { name = "Story new spread", hideFlags = HideFlags.HideAndDontSave };
        _oldSpread.Create(); _newSpread.Create();
    }

    private IEnumerator CloseRoutine()
    {
        _window.interactable = false;
        // Keep blocking raycasts until the modal is completely closed.
        CanvasGroup page = pages != null && _index < pages.Length ? pages[_index] : null;
        for (float t = 0; t < 0.12f; t += Time.unscaledDeltaTime)
        {
            float a = 1 - Smooth(t / 0.12f);
            if (page != null) page.alpha = a;
            if (controls != null) controls.alpha = a;
            yield return null;
        }
        if (page != null) page.alpha = 0;
        if (controls != null) controls.alpha = 0;
        bool fold = closingVisual != null && leftHalf != null && rightHalf != null && closedCover != null && bookSpread != null;
        if (fold)
        {
            closingVisual.gameObject.SetActive(true);
            bookSpread.alpha = 0;
            for (float t = 0; t < closeDuration; t += Time.unscaledDeltaTime)
            {
                float progress = Smooth(t / closeDuration);
                float cosine = Mathf.Cos(progress * Mathf.PI);
                leftHalf.rectTransform.localScale = new Vector3(Mathf.Max(0, cosine), 1, 1);
                closedCover.rectTransform.localScale = new Vector3(Mathf.Max(0, -cosine), 1, 1);
                float shade = Mathf.Lerp(0.65f, 1, Mathf.Abs(cosine));
                closedCover.color = new Color(shade, shade, shade, 1);
                yield return null;
            }
            leftHalf.rectTransform.localScale = new Vector3(0, 1, 1);
            closedCover.rectTransform.localScale = Vector3.one;
            closedCover.color = Color.white;
            rightHalf.color = new Color(1, 1, 1, 0);
        }
        for (float t = 0; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            float p = Smooth(t / fadeDuration);
            _window.alpha = 1 - p;
            if (closingVisual != null)
            {
                closingVisual.transform.localScale = Vector3.one * Mathf.Lerp(1, 0.93f, p);
                ((RectTransform)closingVisual.transform).anchoredPosition = _closingPosition + new Vector2(0, -22 * p);
            }
            yield return null;
        }
        _routine = null;
        gameObject.SetActive(false);
    }

    private void StopTransition()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        _busy = _closing = false;
    }

    private void ResetVisuals()
    {
        _window.alpha = 1;
        _window.interactable = _window.blocksRaycasts = true;
        if (controls != null) controls.alpha = 1;
        if (turningVisual != null) turningVisual.gameObject.SetActive(false);
        if (bookSpread != null) bookSpread.alpha = 1;
        if (pages != null) foreach (var page in pages) if (page != null) page.alpha = 1;
        if (closingVisual != null)
        {
            closingVisual.gameObject.SetActive(false);
            closingVisual.alpha = 1;
            closingVisual.transform.localScale = Vector3.one;
            ((RectTransform)closingVisual.transform).anchoredPosition = _closingPosition;
        }
        if (leftHalf != null) leftHalf.rectTransform.localScale = Vector3.one;
        if (rightHalf != null) rightHalf.color = Color.white;
        if (closedCover != null) { closedCover.rectTransform.localScale = new Vector3(0, 1, 1); closedCover.color = Color.white; }
    }

    private static float Smooth(float t) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
}
