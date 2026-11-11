using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class AddedObject : MonoBehaviour
{
    public string uniqueID = "";
    
    public Buyable toInjectBuyable;

    public bool useDefault = false;
    public string overID = "";
    public string id = "";
    
    public bool isEnemy;
    public bool isNeutral;
    public int amount = 1;
    public int level = 1;
    [Min(0), Tooltip("Respawn delay in game seconds for a point that recreates its visual. Zero disables respawn.")]
    public float respawnDelaySeconds;
    
    public List<string> addedVis = new List<string>();
    //to do it as lootset
    public string addedInvLoot = "";
    public List<string> addedInv = new List<string>();

    public Action<RObj> onAdd = null;
    //----
    //add option as main
    public List<Bon> addedPars = new List<Bon>();
    public List<string> addedMeta = new List<string>();
    public List<Bon> reqPars = new List<Bon>();
    
    public bool asMainViz = false;
    public bool recreateViz = false;
    public bool markSkillsAsReq = false;
    public bool randomizeItemsParams = false;
    
    public List<GameObject> toActivate = new List<GameObject>();

    public GameObject visMain;
    
    //
    public List<Bon> extraMonsters = new List<Bon>();

    private bool inst = false;
    public RObj spawnedUnit;
    private float respawnAt = -1f;
    private Vector3 deathPosition;
    private GameObject corpse;
    private Coroutine respawnRoutine;
    private TextMeshPro respawnTimer;
    
    public void Inst() => Spawn();

    private void Start() => Spawn();

    private bool CanSpawn() =>
        ConfigLoader.parseEnded && MainStates.instance != null &&
        MainStates.instance.all.ContainsKey("main_player") && DatabaseAll.instance != null &&
        (!recreateViz || ResourceHolder.instance != null);

    private void Spawn()
    {
        if (inst || respawnAt >= 0f) return;
        
        if (!CanSpawn())
        {
            if (!IsInvoking(nameof(Spawn)))
                Invoke(nameof(Spawn), 0.1f);
            return;
        }

        GameObject nn = gameObject;
        if (recreateViz)
        {
            nn = Instantiate(ResourceHolder.instance.GetGameobject(id));
            nn.transform.position = transform.position;
            nn.transform.parent = gameObject.transform.parent;
        }
        if (visMain != null) nn =  visMain;
        
        var r = DatabaseAll.instance.CreateAny(id, isEnemy, amount, nn, overID, asMainViz ? gameObject : null, level:level);
        
        var hl = new List<string>(addedVis);
        if (MainSwitcher.instance != null)
            hl.AddRange(MainSwitcher.instance.extraAddOverrides);

        if (useDefault)
        {
            List<string> defo = new List<string> { "coll#val:0.5", "dmg_track","flash","death","drop","combat",
                "animator#pr:1","realcol#val:0.2","rand_move"};
            hl.AddRange(defo);
        }
        
        
        foreach (var v in hl)
        {
            r.AddViz(v);
        }

        if (addedInvLoot != "")
        {
            var a = ModelSet.GetMeItemsBon(addedInvLoot);
            for (int i = 0; i < a.Count; i++)
            {
                MainStates.instance.AddItem(r, a[i].Key, a[i].Value, randomizeItemsParams);
            }
        }

        foreach (var v in addedInv)
        {
            var hh = v.Split(',');
            int rar = 0;
            if (hh.Length > 2)
            {
                rar = int.Parse(hh[2]);
            }
            MainStates.instance.AddItem(r, hh[0], int.Parse(hh[1]), randomizeItemsParams, rar);
        }

        foreach (var v in addedPars)
        {
            r.SetPar(v.Key, v.Value);
        }

        r.addedPars = addedPars;
        
        


        foreach (var v in addedMeta)
        {
            r.AddMeta(v);
        }
        
        if (ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.mode_manhattan) > 0)
        {
            r.Position = r.main.transform.position;
            r.AdjustPosition();
        }

        if (markSkillsAsReq)
        {
            foreach (var v in r.actSkills)
            {
                v.SetPar("action_req", 1);
            }
        }

        if (r.HasVis("loot") && reqPars.Count > 0)
        {
            r.visuals["loot"].GetComponent<XDloot>().price = reqPars;
        }
        
        foreach (var v in toActivate)
            v.SetActive(true);
        
        //isNeutral checking
        if (isNeutral)
        {
            if (r.tags.Contains("player"))
                r.tags.Remove("player");
            if (r.tags.Contains("enemy"))
                r.tags.Remove("enemy");
            r.tags.Add("neutral");
        }
        
        MainStates.instance.ApplyMonsterExtraParams(r,r);

        var b = addedPars.Find(x => x.Key == "scale");
        if (b != null)
        {
            if (b.Value == -1)
                r.SetScale(true);
        }

        r.extraMonsters = extraMonsters;
        spawnedUnit = r;
        r.fromRef = this;
        inst = true;
        
        if (onAdd != null)
            onAdd(r);

        if (ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.use_navmesh) > 0 && ConfigLoader.GetMetaParamValue(ConfigLoader.MetaParam.coord_mode_xz) > 0)
        {
            var m = r.main.AddComponent<NavMeshAgent>();
            //m.acceleration = 0;
            m.angularSpeed = 1200;

        }
        
        if (MainStates.isCameraFace)
        {
            //thwbbb
            if (r.HasVisMain())
                r.visMain.transform.forward = Camera.main.transform.forward;
        }

        if (toInjectBuyable != null)
        {
            toInjectBuyable.rReward = r;
        }

        if (MainSwitcher.instance != null)
            MainSwitcher.instance.OnAdd(r);
    }


    public void UnitDead()
    {
        if (spawnedUnit == null || respawnAt >= 0f)
            return;

        var dead = spawnedUnit;
        spawnedUnit = null;
        if (respawnDelaySeconds <= 0f || !recreateViz || asMainViz || visMain != null)
            return;

        corpse = dead.main;
        deathPosition = corpse != null ? corpse.transform.position : dead.Position;
        respawnAt = Time.time + respawnDelaySeconds;
        if (isActiveAndEnabled)
            respawnRoutine = StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        var timer = new GameObject("Respawn timer", typeof(RectTransform));
        timer.layer = gameObject.layer;
        timer.transform.SetParent(transform, false);
        respawnTimer = timer.AddComponent<TextMeshPro>();
        respawnTimer.alignment = TextAlignmentOptions.Center;
        respawnTimer.fontSize = 3.5f;
        respawnTimer.rectTransform.sizeDelta = new Vector2(3f, 1f);
        int displayedSeconds = -1;

        // Wait at least one frame so the death handler can unregister the old unit.
        do
        {
            var camera = Camera.main;
            var up = camera != null ? camera.transform.up : Vector3.up;
            timer.transform.position = deathPosition + up * .65f;
            if (camera != null)
                timer.transform.rotation = camera.transform.rotation;

            int seconds = Mathf.Max(0, Mathf.CeilToInt(respawnAt - Time.time));
            if (seconds != displayedSeconds)
            {
                respawnTimer.text = $"{seconds / 60:00}:{seconds % 60:00}";
                displayedSeconds = seconds;
            }
            yield return null;
        } while (Time.time < respawnAt || corpse != null || !CanSpawn());

        RemoveRespawnTimer();
        respawnRoutine = null;
        corpse = null;
        respawnAt = -1f;
        inst = false;
        Spawn();
    }

    private void OnEnable()
    {
        if (respawnAt >= 0f && respawnRoutine == null)
            respawnRoutine = StartCoroutine(Respawn());
    }

    private void OnDisable()
    {
        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
            respawnRoutine = null;
        }
        RemoveRespawnTimer();
    }

    private void RemoveRespawnTimer()
    {
        if (respawnTimer != null)
            Destroy(respawnTimer.gameObject);
        respawnTimer = null;
    }
    
}
