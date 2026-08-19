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
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Fill());
        Fill();
    }

    public void Fill()
    {
        if (!GUILIB.CoreReady) return;
        var progress = MainStates.instance.playerData.playerTasks.Where(x => x.started != 0).ToList();
        for (var i = 0; i < tasks.Count; i++)
        {
            var active = i < progress.Count && DatabaseAll.instance.allTasks.ContainsKey(progress[i].id);
            tasks[i].gameObject.SetActive(active);
            if (active) tasks[i].taskgui.Fill(new RObj(progress[i].id, ItemType.task));
        }
        notify?.SetActive(progress.Any(x => x.completed && !x.taken));
    }
}
