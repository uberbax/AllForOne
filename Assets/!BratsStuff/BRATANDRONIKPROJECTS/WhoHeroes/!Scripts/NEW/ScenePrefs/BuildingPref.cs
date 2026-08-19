using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingPref : MonoBehaviour
{
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
        EventManager.SUB(WhoHeroesEvents.Refresh, _ => Sync());
        EventManager.SUB("new_day", _ => { if (enemyPortal) blockUpdate = false; });
        EventManager.SUB("new_night", _ => { if (enemyPortal) blockUpdate = true; });
        EventManager.SUB("block_movement", value => ignoreClick = value != null && (value.num != 0 || value.what == "true"));
        StartCoroutine(BindWhenReady());
    }

    private IEnumerator BindWhenReady()
    {
        while (!GUILIB.CoreReady)
            yield return null;
        runtime = GUILIB.Resolve(build, gameObject, true);
        Sync();
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
