using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Compact pickup HUD: fixed-height rows, merged duplicates, bounded visible stack and a queue.</summary>
public sealed class PickupNotificationStack : MonoBehaviour
{
    [SerializeField] private RectTransform safeArea;
    [SerializeField] private RectTransform rows;
    [SerializeField] private RectTransform rowTemplate;
    [SerializeField, Range(1, 8)] private int maxVisible = 5;
    [SerializeField, Min(.1f)] private float holdDuration = 2.8f;
    [SerializeField, Min(.01f)] private float fadeDuration = .22f;
    private static PickupNotificationStack instance;
    private static GameObject prefab;
    private readonly List<Entry> active = new List<Entry>();
    private readonly List<Pending> pending = new List<Pending>();
    private readonly Stack<Entry> pool = new Stack<Entry>();
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private struct Pending { public string id; public string label; public float amount; public Sprite icon; }
    private sealed class Entry
    {
        public RectTransform rect;
        public CanvasGroup visibility;
        public Image icon;
        public TextMeshProUGUI label;
        public TextMeshProUGUI quantity;
        public string id;
        public float amount;
        public float age;
    }
    public int VisibleCount => active.Count;
    public int PendingCount => pending.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; prefab = null; }

    public static void Show(string itemId, string label, float amount, Sprite icon)
    {
        if (instance == null)
        {
            instance = FindFirstObjectByType<PickupNotificationStack>();
            if (instance == null)
            {
                if (prefab == null) prefab = Resources.Load<GameObject>("PickupNotifications");
                if (prefab == null) { Debug.LogError("PickupNotifications prefab is missing from Resources."); return; }
                instance = Instantiate(prefab).GetComponent<PickupNotificationStack>();
                instance.name = "PickupNotifications";
            }
        }
        instance.Enqueue(itemId, label, amount, icon);
    }
    private void Awake()
    {
        if (instance == null) instance = this;
        rowTemplate.gameObject.SetActive(false);
        ApplySafeArea();
    }
    public void Enqueue(string itemId, string label, float amount, Sprite icon)
    {
        string key = string.IsNullOrEmpty(itemId) ? label : itemId;
        foreach (var entry in active)
        {
            if (entry.id != key) continue;
            entry.amount += amount;
            // Keep a fully visible row visible; revive one that has started fading out.
            entry.age = Mathf.Min(entry.age, fadeDuration);
            entry.quantity.text = Amount(entry.amount);
            return;
        }
        for (int i = 0; i < pending.Count; i++)
        {
            var item = pending[i];
            if (item.id != key) continue;
            item.amount += amount; pending[i] = item; return;
        }
        pending.Add(new Pending { id = key, label = label, amount = amount, icon = icon });
        DrainQueue();
    }
    private static string Amount(float value) => "+" + value.ToString("0.##", CultureInfo.InvariantCulture);

    private Entry CreateRow()
    {
        var rect = Instantiate(rowTemplate, rows);
        rect.name = "Pickup row";
        return new Entry {
            rect = rect, visibility = rect.GetComponent<CanvasGroup>(),
            icon = rect.Find("Icon").GetComponent<Image>(),
            label = rect.Find("Name").GetComponent<TextMeshProUGUI>(),
            quantity = rect.Find("Amount").GetComponent<TextMeshProUGUI>()
        };
    }
    private void DrainQueue()
    {
        while (active.Count < Mathf.Clamp(maxVisible, 1, 8) && pending.Count > 0)
        {
            var item = pending[0]; pending.RemoveAt(0);
            var entry = pool.Count > 0 ? pool.Pop() : CreateRow();
            entry.id = item.id; entry.amount = item.amount; entry.age = 0;
            entry.label.text = item.label; entry.quantity.text = Amount(item.amount);
            entry.icon.sprite = item.icon; entry.icon.enabled = item.icon != null;
            entry.visibility.alpha = 0;
            entry.rect.SetAsLastSibling(); entry.rect.gameObject.SetActive(true);
            active.Add(entry);
        }
        LayoutRows();
    }
    private void LayoutRows()
    {
        // Fixed slots: never animate vertical positions through another row.
        float height = rowTemplate.sizeDelta.y;
        for (int i = 0; i < active.Count; i++)
            active[i].rect.anchoredPosition = new Vector2(0, -i * (height + 6f));
    }
    private void Update()
    {
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height || lastSafeArea != Screen.safeArea)
            ApplySafeArea();
        bool changed = false;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var entry = active[i]; entry.age += Time.unscaledDeltaTime;
            float fade = Mathf.Max(.01f, fadeDuration);
            if (entry.age >= holdDuration + fade * 2)
            {
                entry.rect.gameObject.SetActive(false);
                active.RemoveAt(i); pool.Push(entry); changed = true; continue;
            }
            float enter = Mathf.Clamp01(entry.age / fade);
            float exit = Mathf.Clamp01((entry.age - fade - holdDuration) / fade);
            entry.visibility.alpha = Mathf.SmoothStep(0, 1, enter) * (1 - Mathf.SmoothStep(0, 1, exit));
        }
        if (changed) DrainQueue();
    }
    private void ApplySafeArea()
    {
        if (safeArea == null || Screen.width <= 0 || Screen.height <= 0) return;
        lastScreen = new Vector2Int(Screen.width, Screen.height); lastSafeArea = Screen.safeArea;
        safeArea.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
    private void OnDisable()
    {
        foreach (var entry in active) { entry.rect.gameObject.SetActive(false); pool.Push(entry); }
        active.Clear(); pending.Clear();
    }
    private void OnDestroy() { if (instance == this) instance = null; }
}
