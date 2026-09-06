using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUITasksWindow : MonoBehaviour
{
    private const float QuestBottomMarginRatio = 0.3f;

    public Transform holder;
    public GameObject notify;
    private readonly List<GUITaskPrefab> tasks = new List<GUITaskPrefab>();
    private RectTransform windowRect;
    private RectTransform holderRect;
    private WindowSlider windowSlider;
    private float baseWindowHeight;
    private float baseHolderHeight;
    private Vector2 baseHolderPosition;

    private void Awake()
    {
        if (holder == null) return;
        windowRect = transform as RectTransform;
        holderRect = holder as RectTransform;
        windowSlider = GetComponent<WindowSlider>();
        for (var i = 0; i < holder.childCount; i++)
        {
            var value = holder.GetChild(i).GetComponent<GUITaskPrefab>();
            if (value != null) tasks.Add(value);
        }
        if (windowRect != null && holderRect != null)
        {
            baseWindowHeight = windowRect.rect.height;
            baseHolderHeight = MeasureFullTaskListHeight();
            baseHolderPosition = holderRect.anchoredPosition;
        }
    }

    private void Start()
    {
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        Fill();
    }

    private void OnRefresh(ArgPass _)
    {
        Fill();
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
    }

    public void Fill()
    {
        if (!GUILIB.CoreReady) return;
        var progress = MainStates.instance.playerData.playerTasks
            .Where(value => value.started != 0 && !value.taken &&
                            MainCycle_WhoHeroes.OnboardingTaskIds.Contains(value.id))
            .ToList();
        EnsureTaskRows(progress.Count);
        for (var i = 0; i < tasks.Count; i++)
        {
            var active = i < progress.Count && DatabaseAll.instance.allTasks.ContainsKey(progress[i].id);
            tasks[i].gameObject.SetActive(active);
            if (active) tasks[i].taskgui.Fill(new RObj(progress[i].id, ItemType.task));
        }
        notify?.SetActive(progress.Any(x => x.completed && !x.taken));
        ResizeToActiveTasks();
    }

    private void EnsureTaskRows(int count)
    {
        if (holder == null || tasks.Count == 0)
            return;
        while (tasks.Count < count)
        {
            var clone = Instantiate(tasks[0].gameObject, holder);
            var task = clone.GetComponent<GUITaskPrefab>();
            if (task == null)
            {
                Destroy(clone);
                return;
            }
            tasks.Add(task);
        }
    }

    private void ResizeToActiveTasks()
    {
        if (windowRect == null || holderRect == null || baseWindowHeight <= 0f)
            return;

        var holderHeight = MeasureActiveTaskListHeight();
        var targetHeight = Mathf.Max(0f, baseWindowHeight - baseHolderHeight + holderHeight);
        holderRect.anchoredPosition = baseHolderPosition +
                                      Vector2.up * ((baseHolderHeight - holderHeight) * 0.5f);
        windowRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        var taskHeight = tasks
            .Select(value => value.transform as RectTransform)
            .Where(value => value != null)
            .Select(value => value.rect.height)
            .FirstOrDefault();
        windowSlider?.SetVerticalWindowSizeDelta(
            targetHeight - baseWindowHeight + taskHeight * QuestBottomMarginRatio * 2f);
    }

    private float MeasureActiveTaskListHeight()
    {
        var layout = holder.GetComponent<VerticalLayoutGroup>();
        var height = layout == null ? 0f : layout.padding.top + layout.padding.bottom;
        var measured = 0;
        foreach (var task in tasks)
        {
            if (!task.gameObject.activeSelf || !(task.transform is RectTransform taskRect))
                continue;
            height += taskRect.rect.height;
            measured++;
        }
        if (layout != null && measured > 1)
            height += layout.spacing * (measured - 1);
        return height;
    }

    private float MeasureFullTaskListHeight()
    {
        var layout = holder.GetComponent<VerticalLayoutGroup>();
        var height = layout == null ? 0f : layout.padding.top + layout.padding.bottom;
        var measured = 0;
        foreach (var task in tasks)
        {
            if (!(task.transform is RectTransform taskRect))
                continue;
            height += taskRect.rect.height;
            measured++;
        }
        if (layout != null && measured > 1)
            height += layout.spacing * (measured - 1);
        return height;
    }
}
