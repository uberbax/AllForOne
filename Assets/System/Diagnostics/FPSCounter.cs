using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class FPSCounter : MonoBehaviour
{
    [SerializeField, Range(.1f, 2f)] private float refreshInterval = .5f;
    [SerializeField, Min(12)] private float fontSize = 24f;
    private Canvas overlay;
    private RectTransform safeArea;
    private TextMeshProUGUI label;
    private Rect previousSafeArea;
    private Vector2Int previousScreen;
    private float elapsed;
    private int frames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateCounter()
    {
        if (Object.FindFirstObjectByType<FPSCounter>(FindObjectsInactive.Include) != null) return;
        var root = new GameObject("FPSCounter");
        root.AddComponent<FPSCounter>();
        DontDestroyOnLoad(root);
    }

    private void Awake()
    {
        // Separate overlay keeps the counter above windows without intercepting UI input.
        var canvasObject = new GameObject("FPS Overlay", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = short.MaxValue;
        var area = new GameObject("Safe Area", typeof(RectTransform));
        area.transform.SetParent(canvasObject.transform, false);
        safeArea = area.GetComponent<RectTransform>();
        var panel = new GameObject("Counter", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(safeArea, false);
        var background = panel.GetComponent<Image>();
        background.color = new Color(0, 0, 0, .65f);
        background.raycastTarget = false;
        var rect = background.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -12);
        rect.sizeDelta = new Vector2(240, 46);
        var text = new GameObject("FPS", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        text.transform.SetParent(rect, false);
        label = text.GetComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.text = "FPS: --";
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(10, 0);
        label.rectTransform.offsetMax = new Vector2(-10, 0);
        ApplySafeArea();
    }

    private void OnEnable()
    {
        elapsed = 0; frames = 0;
        if (overlay != null) overlay.enabled = true;
    }

    private void OnDisable()
    {
        if (overlay != null) overlay.enabled = false;
    }

    private void Update()
    {
        ApplySafeArea();
        float delta = Time.unscaledDeltaTime;
        if (delta <= 0) return;
        elapsed += delta;
        frames++;
        if (elapsed < refreshInterval) return;
        label.SetText("FPS: {0:0}  ({1:1} ms)", frames / elapsed, elapsed * 1000f / frames);
        elapsed = 0; frames = 0;
    }

    private void ApplySafeArea()
    {
        var screen = new Vector2Int(Screen.width, Screen.height);
        var area = Screen.safeArea;
        if (screen.x <= 0 || screen.y <= 0) return;
        if (previousScreen == screen && previousSafeArea == area) return;
        previousScreen = screen; previousSafeArea = area;
        safeArea.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
        safeArea.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
}
