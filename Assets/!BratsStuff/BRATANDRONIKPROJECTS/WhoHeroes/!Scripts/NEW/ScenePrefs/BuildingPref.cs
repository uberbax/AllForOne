using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPref : MonoBehaviour
{
    private const int RuntimeBindTimeoutFrames = 600;

    public WhoHeroesObjectRef build = new WhoHeroesObjectRef();
    public WhoHeroesObjectRepresent workers = new WhoHeroesObjectRepresent();
    public bool workRepresent;
    public bool portalType;
    public WhoHeroesObjectOptions portals = new WhoHeroesObjectOptions();
    public TempObj effectOnCall;
    public bool ignoreMove;

    private bool blockUpdate;
    private bool enemyPortal;
    private bool ignoreClick;
    private RObj runtime;

    private void Awake()
    {
        if (workRepresent)
            workers.SetUpGrades();
    }

    private void Start()
    {
        blockUpdate = !workRepresent && !portalType;
        enemyPortal = portalType && build != null && build.id.Contains("0");
        EventManager.SUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.SUB("new_day", OnNewDay);
        EventManager.SUB("new_night", OnNewNight);
        EventManager.SUB("block_movement", OnBlockMovement);
        StartCoroutine(BindWhenReady());
    }

    private void OnRefresh(ArgPass _)
    {
        Sync();
    }

    private void OnNewDay(ArgPass _)
    {
        if (enemyPortal)
            blockUpdate = false;
    }

    private void OnNewNight(ArgPass _)
    {
        if (enemyPortal)
            blockUpdate = true;
    }

    private void OnBlockMovement(ArgPass value)
    {
        ignoreClick = value != null && (value.num != 0 || value.what == "true");
    }

    private void OnDestroy()
    {
        EventManager.UNSUB(WhoHeroesEvents.Refresh, OnRefresh);
        EventManager.UNSUB("new_day", OnNewDay);
        EventManager.UNSUB("new_night", OnNewNight);
        EventManager.UNSUB("block_movement", OnBlockMovement);
    }

    private IEnumerator BindWhenReady()
    {
        while (!GUILIB.CoreReady)
            yield return null;
        ValidateConfiguration();

        for (var frame = 0; runtime == null && frame < RuntimeBindTimeoutFrames; frame++)
        {
            runtime = GUILIB.Resolve(build, gameObject);
            if (runtime == null)
                yield return null;
        }

        if (runtime == null)
            Debug.LogError(
                $"WhoHeroes scene state '{build?.id}' was not created by AddedObject within {RuntimeBindTimeoutFrames} frames.",
                this);
        Sync();
    }

    private void ValidateConfiguration()
    {
        var id = build?.id?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("WhoHeroes scene: BuildingPref id is empty.", this);
            return;
        }

        if (!DatabaseAll.instance.heroes.ContainsKey(id))
            Debug.LogError($"WhoHeroes config: scene object '{id}' is missing from Heroes.", this);

        var added = GetComponent<AddedObject>();
        if (added == null || added.id != id || added.overID != id || !added.asMainViz)
            Debug.LogError(
                $"WhoHeroes scene: '{id}' must be wired through AddedObject (id/overID='{id}', asMainViz=true).",
                this);
    }

    public void OnMouseDown()
    {
        if (ignoreClick || (MainStates.instance != null && MainStates.instance.isPaused))
            return;
        runtime ??= GUILIB.Resolve(build, gameObject);
        GUILIB.Emit(ignoreMove ? WhoHeroesEvents.ObserveBuilding : WhoHeroesEvents.ViewBuilding,
            runtime, build?.id ?? "");
    }

    private void Sync()
    {
        if (blockUpdate)
            return;
        runtime ??= GUILIB.Resolve(build, gameObject);
        var level = runtime == null ? build?.level ?? 0 : GUILIB.Level(runtime);
        if (workRepresent)
            workers.DrowGrades(level);
        if (portalType && IsEnterPortal())
            portals.ActivateSingleItem(level > 0 ? "in" : "closed");
    }

    private bool IsEnterPortal()
    {
        if (runtime?.dbObj != null && runtime.dbObj.pars.ContainsKey("enter"))
            return runtime.GetPar("enter") > 0;
        return build == null || !build.id.Contains("out");
    }

    public void SpaumPortal(bool start = true)
    {
        if (!IsEnterPortal())
            return;
        if (start)
            portals.ActivateSingleItem("enemy");
        else
            portals.ActivateSingleItem(GUILIB.Level(runtime, build?.level ?? 0) > 0 ? "in" : "closed");
    }

    public void OutPortal()
    {
        effectOnCall?.Activate();
    }

}

[Serializable]
public class WhoHeroesObjectRepresent
{
    public List<GameObject> activObj = new List<GameObject>();
    public Transform holderActive;
    [NonSerialized] public int curLevel = -1;

    public void SetUpGrades()
    {
        if (holderActive == null)
            return;
        activObj.Clear();
        for (var i = 0; i < holderActive.childCount; i++)
            activObj.Add(holderActive.GetChild(i).gameObject);
    }

    public void DrowGrades(int level = 0)
    {
        foreach (var value in activObj)
            if (value != null) value.SetActive(activObj.IndexOf(value) < level);
        curLevel = level;
    }
}

[Serializable]
public class WhoHeroesObjectOptions
{
    public List<WhoHeroesNamedObject> items = new List<WhoHeroesNamedObject>();

    public void ActivateSingleItem(string id)
    {
        foreach (var item in items)
            if (item.obj != null) item.obj.SetActive(item.id == id);
    }

    public void DisableAll()
    {
        foreach (var item in items)
            if (item.obj != null) item.obj.SetActive(false);
    }
}
