using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GUITasksWindow : MonoBehaviour
{
    public Transform holder;
    public GameObject notify;
    private readonly List<GUITaskPrefab> tasks = new List<GUITaskPrefab>();

    private void Awake()
    {
        if (holder == null) return;
        for (var i = 0; i < holder.childCount; i++)
        {
            var value = holder.GetChild(i).GetComponent<GUITaskPrefab>();
            if (value != null) tasks.Add(value);
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
            .Where(value => value.started != 0 && MainCycle_WhoHeroes.OnboardingTaskIds.Contains(value.id))
            .ToList();
        for (var i = 0; i < tasks.Count; i++)
        {
            var active = i < progress.Count && DatabaseAll.instance.allTasks.ContainsKey(progress[i].id);
            tasks[i].gameObject.SetActive(active);
            if (active) tasks[i].taskgui.Fill(new RObj(progress[i].id, ItemType.task));
        }
        notify?.SetActive(progress.Any(x => x.completed && !x.taken));
    }
}
