using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SpaumPoint : MonoBehaviour
{
    public bool onCall;
    [FormerlySerializedAs("spawnList")]
    public SpawnItemSceneList spaum = new SpawnItemSceneList();
    public SpawnPointSettings settings = new SpawnPointSettings();

    public int wavenum;
    public int objnum;
    public float spaumtimer;
    public string type = "";
    public bool circle;
    public bool complete;

    private bool active;
    private Transform holder;
    private Transform effectHolder;

    private void Awake()
    {
        active = settings.onStart || settings.active;
    }

    private void Start()
    {
        spaumtimer = 0;
        holder = spaum.parrent != null ? spaum.parrent : MainStates.instance?.root;
        effectHolder = MainStates.instance?.root;
    }

    private void Update()
    {
        if ((MainStates.instance != null && MainStates.instance.isPaused) || !active || onCall || settings.onCall)
            return;
        spaumtimer -= Time.deltaTime;
        if (spaumtimer > 0)
            return;
        spaumtimer = Mathf.Max(0.01f, spaum.delay);
        Spawn();
    }

    public void CallSpawn(int wave = -1)
    {
        Spawn(wave);
    }

    public void Activate(bool state)
    {
        active = state;
        settings.active = state;
    }

    private void Spawn(int wave = -1)
    {
        if (spaum.items.Count == 0)
            return;
        var index = wave < 0 ? Mathf.Clamp(wavenum, 0, spaum.items.Count - 1) : Mathf.Clamp(wave, 0, spaum.items.Count - 1);
        var item = spaum.items[index];
        if (item.pref != null && UnityEngine.Random.value <= settings.spawnChance)
        {
            var current = Instantiate(item.pref, holder);
            current.transform.position = RandomPoint(transform.position, settings.delta, settings.deltaOff);
            if (!string.IsNullOrEmpty(item.id) && GUILIB.CoreReady)
                GUILIB.Resolve(item.id, current, true);

            var effect = spaum.effect;
            if (effect == null && !string.IsNullOrEmpty(spaum.effectId) && ResourceHolder.instance != null &&
                ResourceHolder.instance.miscGO.ContainsKey(spaum.effectId))
                effect = ResourceHolder.instance.miscGO[spaum.effectId];
            if (effect != null)
            {
                var spawnedEffect = Instantiate(effect, effectHolder);
                spawnedEffect.transform.position = current.transform.position + spaum.effectShift;
            }
        }

        if (wave >= 0)
            return;
        objnum++;
        var targetCount = Mathf.Max(1, Mathf.RoundToInt(item.num * settings.countMultiplier));
        if (objnum >= targetCount)
            SwitchWave();
    }

    private static Vector3 RandomPoint(Vector3 center, float radius, float innerRadius)
    {
        var offset = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(innerRadius, Mathf.Max(innerRadius, radius));
        return center + new Vector3(offset.x, offset.y, 0);
    }

    private void SwitchWave()
    {
        objnum = 0;
        wavenum++;
        if (wavenum < spaum.items.Count)
            return;
        wavenum = 0;
        if (circle || settings.circle)
            spaumtimer += settings.circleDelay;
        else
        {
            active = false;
            complete = true;
        }
    }
}

[Serializable]
public class SpawnItemScene
{
    public string id = "";
    public GameObject pref;
    public int num = 1;
}

[Serializable]
public class SpawnItemSceneList
{
    [FormerlySerializedAs("who")]
    public List<SpawnItemScene> items = new List<SpawnItemScene>();
    public GameObject effect;
    public string effectId = "";
    public Vector3 effectShift = Vector3.zero;
    public Transform parrent;
    public float delay = 1;
    public int Count => items.Count;

    public void LoadLocationList(Transform source)
    {
        items.Clear();
        if (source == null)
            return;
        for (var i = 0; i < source.childCount; i++)
            items.Add(new SpawnItemScene { pref = source.GetChild(i).gameObject });
    }
}

[Serializable]
public class SpawnPointSettings
{
    public bool active = true;
    public bool onStart;
    public bool onCall;
    public bool circle;
    public float delta = 1;
    public float deltaOff;
    public float spawnChance = 1;
    public float circleDelay = 1;
    public float countMultiplier = 1;

    public void ChangeSettings(float countMult = 1, float luck = 1, float time = 1)
    {
        if (countMult >= 0) countMultiplier = countMult;
        if (luck >= 0) spawnChance = luck;
        if (time >= 0) circleDelay = time;
    }

    public void Activate(bool state)
    {
        active = state;
    }
}
