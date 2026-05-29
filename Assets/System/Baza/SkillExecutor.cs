using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Diagnostics;
using Random = UnityEngine.Random;

public class SkillExecutor : MonoBehaviour
{
    public static SkillExecutor instance;

    public Transform summonPos1;
    public Transform summonPos2;

    public Transform targetersRoot;
    public Targeter curTargeter;
    
    public PathChecker movePath;
    private void Awake()
    {
        instance = this;
    }

    public static Dictionary<string, int> mapFilter = new Dictionary<string, int>
    {
        {"any", -1},
        {"lowest", 0},
        {"highest", 1},
    };

    //my_side wave
    public List<RObj> GetAllTargets(RObj who, RObj skl, string meta1 = "", string meta2 = "", List<string> metas = null, Vector3 overPos = default, List<RObj> except = null)
    {
        List<RObj> targets = new List<RObj>();
        foreach (var v in MainStates.instance.combats)
        {
            if (meta1 != "" && !v.META_TAGS.Contains(meta1)) continue;
            if (meta2 != "" && !v.META_TAGS.Contains(meta2)) continue;
            if (except != null && except.Contains(v)) continue;
            if (v.GetPar("immortal") > 0) continue;

            float d = 0;
            if (overPos == default)
                d = MainStates.instance.GetDistance(who, v, out float dd);
            else
                d = MainStates.instance.GetDistance(overPos, v, out float dd);
            
            if (d > skl.GetPar("range")) continue;
            
            targets.Add(v);
        }

        targets = GetTargets(who, targets, "filter_target", "x",(int)skl.GetPar("target"), overPos);
        targets = GetTargets(who, targets, "filter_self", "x",(int)skl.GetPar("filter_self"), overPos);
        targets = GetTargets(who, targets, "filter_val", "range",(int)skl.GetPar("filter_range"), overPos);
        targets = GetTargets(who, targets, "filter_ratio_val", "health",(int)skl.GetPar("filter_hp"), overPos);
        targets = GetTargets(who, targets, "filter_val", "attack",(int)skl.GetPar("filter_atk"), overPos);
        
        return targets;
    }
    
    public List<RObj> GetTargets(RObj who, List<RObj> targets, string filterName, string filterPar, int filterVal, Vector3 overPos = default)
    {
        if (filterVal == -1) return targets;
        
        if (filterName == "filter_target")
        {
            if (filterVal == 0)
            {
                return targets.FindAll(x => x.tags[0] != who.tags[0]);
            }
            else if (filterVal == 2)
            {
                return targets;
            }
            else 
            {
                return targets.FindAll(x => x.tags[0] == who.tags[0]);
            }
        }

        if (filterPar == "range")
        {
            if (filterVal == 0)
            {
                if (overPos == default)
                    targets = targets.OrderBy(x => MainStates.instance.GetDistance(who, x, out float dd)).ToList();
                else
                    targets = targets.OrderBy(x => MainStates.instance.GetDistance(overPos, x, out float dd)).ToList();
            }
            else
            {
                if (overPos == default)
                    targets = targets.OrderBy(x => 10000 - MainStates.instance.GetDistance(who, x, out float dd)).ToList();
                else
                    targets = targets.OrderBy(x => 10000 - MainStates.instance.GetDistance(overPos, x, out float dd)).ToList();
            }

            return targets;
        }

        if (filterName == "filter_self")
        {
            if (filterVal == 1)
            {
                targets.Remove(who);
            }

            return targets;
        }
        
        if (filterName == "filter_ratio_val")
        {
            if (filterVal == 1)
            {
                targets = targets.OrderBy(x => 1 - x.GetPar(filterPar) / x.GetPar("max_" + filterPar)).ToList();
            }
            else
            {
                targets = targets.OrderBy(x => x.GetPar(filterPar) / x.GetPar("max_" + filterPar)).ToList();
            }

            return targets;
        }
        
        if (filterName == "filter_val")
        {
            if (filterVal == 1)
            {
                targets = targets.OrderBy(x => 10000 - x.GetPar(filterPar)).ToList();
            }
            else
            {
                targets = targets.OrderBy(x => x.GetPar(filterPar)).ToList();
            }

            return targets;
        }
        
        return new List<RObj>();
    }

    public ExecReso ExecuteSkill(RObj who, string skl, RObj target)
    {
        var h0 = DatabaseAll.instance.CreateProjectile(who, skl, Vector3.zero, false, false); 
        return ExecuteSkill(who, h0, target);
    }
    
    public ExecReso ExecuteSkill(RObj who, RObj skl, RObj target = null, Vector3 overPos = default, bool overCd = false, List<RObj> except = null, Bon change = null, bool useSame = false)
    {
        float d = 1e+10f;
        //cur weapon skill ?
        skl = who.GetSKillReplace(skl);
        
        var targ = GetAllTargets(who, skl, "", "", null, overPos, except);
        if (targ.Count == 0) return ExecReso.NO_TARGETS;
        else
        {
            if (!targ.Contains(target))
                target = targ[0];
        }
        
        if (target == null)
        {
            target = MainStates.instance.GetClosestEnemy(who, out d);
        }
        else
        {
            var vec = target.Position - (overPos == default ? who.Position : overPos);
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0) vec.z = 0;
            if (overPos == default)
                d = MainStates.instance.GetDistance(who, target, out float dd);
            else
                d = MainStates.instance.GetDistance(overPos, target, out float dd);
        }
        
        if (d > skl.GetPar("range")) return ExecReso.NO_TARGETS;
        if (targ.Count == 0) return ExecReso.NO_TARGETS;
        if (who.GetPar("mana") < skl.GetPar("manacost") && !overCd) return ExecReso.NO_MANA;

        if (ConfigLoader.GetMetaParamValue("req_line_sight") > 0)
        {
            RaycastHit2D h;
            if (overPos == default)
                h =Physics2D.Raycast(who.Position, target.Position - who.Position, skl.GetPar("range"), 1 << LayerMask.NameToLayer("Nopass"));
            else
                h =Physics2D.Raycast(overPos, target.Position - overPos, skl.GetPar("range"), 1 << LayerMask.NameToLayer("Nopass"));
            
            if (h.collider != null)
            {
                return ExecReso.NO_SIGHT;
            }
        }

        var ll = ResourceHolder.instance.GetMeSkillEtc(who.dbObj, skl.dbObj.ID);
        if (ll != null && ll.effSelf != null)
        {
            var kk = Instantiate(ll.effSelf);
            kk.transform.position = who.Position;
            Destroy(kk, 1);
        }
        
        if (target != who && overPos == default)
            who.SetScale(target.Position.x > who.Position.x);
        
        //animation attack
        if (who.visuals.ContainsKey("animator") && overPos == default)
            who.visuals["animator"].GetComponentInChildren<XDanimator>().SetState("attack");
        
        if (overPos == default)
            SoundManager.instance.PlayAny(skl.dbObj.ID);
        
        int p = (int)skl.GetPar("proj_amount");
        float dt = skl.GetPar("dt");
        float ft = skl.GetPar("first");

        if (overPos == default)
            who.ChangePar("mana", -skl.GetPar("manacost"));
        
        if (skl.dbObj.spawn != "")
        {
            var gg = Instantiate(ResourceHolder.instance.skillsWorld[skl.dbObj.spawn], who.main.transform);
            gg.GetComponent<SkillBehavior>().SetTarget(targ[0]);
        }
        else
        {
            for (int i = 0; i < Mathf.Min(targ.Count, skl.GetPar("targets")); i++)
            {

                for (int j = 0; j < p; j++)
                {
                    RObj ro = null;
                    if (!useSame)
                    {
                        ro = DatabaseAll.instance.CreateProjectile(who, skl.dbObj.ID, overPos == default ? Vector3.zero : overPos - who.Position);
                        ro.AddViz("coll");
                    }
                    else
                    {
                        ro = skl;
                    }

                    if (change != null)
                        ro.ChangePar(change.Key, change.Value);

                    if (ConfigLoader.GetMetaParamValue("use_exact_target") == 1 && ft == 0)
                        ro.exact = target;
                    
                    if (ro.dbObj.ID == "put")
                    {
                        ro.main.transform.position = who.Position + new Vector3(0,0.5f,0);
                        ro.Position = ro.main.transform.position;
                        if (who.attachables.Count > 0)
                        {
                            for (int o = who.attachables.Count - 1; o >= 0; o--)
                            {
                                who.attachables[o].parent = ro.main.transform;
                                ro.attachables.Add(who.attachables[o]);
                            }
                            who.attachables.Clear();
                        }
                    }

                    FunctionTimer.Create(() =>
                    {
                        UtilsControl.Instance.MoveTo(ro.main.transform, skl.GetPar("speed"),
                            targ[i].Position + new Vector3(0, MainStates.instance.LIFT_PROJ, 0),
                            () =>
                            {
                                Debug.Log("NYAM");
                                ro.RemoveViz("coll");
                                if (ro.GetPar("aoe") > 0)
                                {
                                    Explode(ro);
                                }
                                else
                                {
                                    if (ro.exact == who)
                                    {
                                        MainStates.instance.DealDamage(who, ro);
                                    }

                                    Destroy(ro.main);
                                }

                            }, null);
                    }, dt * (float)j);


                }

            }
        }

        if (ConfigLoader.GetMetaParamValue("use_naprig") == 1 && skl.dbObj.ID == "basic_melee")
        {
            MainStates.instance.DoNaprig(who, target);
        }

        if (overPos == default)
            skl.SetPar("cd", skl.GetPar("cooldown"));
        return ExecReso.OK;
    }

    private bool reqAction = false;
    public RObj lastWho;
    public RObj lastSkl;
    
    public void CastSkill(RObj who, RObj skl, RObj target = null, string skill = "")
    {
        if (skill != "")
        {
            skl = DatabaseAll.instance.CreateProjectile(who, skill, Vector3.zero, false, false);
            skl.SetPar("cd", 0);
        }
        
        if (skl.GetPar("cd") > 0) return;
        if (who.GetPar("mana") < skl.GetPar("manacost")) return;
        
        lastSkl = skl;
        lastWho = who;
        var ss = skl.dbObj.extraPars;
        if (ss.Count > 0 && skl.GetPar("req2") < 1)
        {
            CastUniqueSkill(who, skl, target);
            return;
        }

        if (skl.GetPar("action_req") > 0)
        {
            //?
            //DatabaseAll.instance.CreateOnlyVizual(skl, who.main.transform.position);
            
            curTargeter = GetTargeter(skl.dbObj.ID);
            curTargeter.gameObject.SetActive(true);
            
            reqAction = true;
            return;
        }
        
    }

    public Targeter GetTargeter(string sklName)
    {
        for (int i = 0; i < targetersRoot.childCount; i++)
        {
            if (targetersRoot.GetChild(i).name == sklName)
            {
                return targetersRoot.GetChild(i).GetComponent<Targeter>();
            }
        }

        return targetersRoot.GetChild(0).GetComponent<Targeter>();
    }
    
    public void CastUniqueSkill(RObj who, RObj skl, RObj target = null)
    {
        if (skl.GetPar("cd") > 0) return;
        
        if (skl.dbObj.ID == "podkrep")
        {
            WaveSpawner.instance.DoSpawnAny(skl.dbObj.extraPars, "player", summonPos1, summonPos2, true, Vector3.zero, Vector3.zero);
            skl.SetPar("cd", skl.GetPar("cooldown"));
            SoundManager.instance.PlayAny(skl.dbObj.ID);
            return;
        }
        else if (skl.dbObj.ID.IndexOf("summon") >= 0)
        {
            RObj trg = who;
            if (skl.dbObj.second != "") trg = MainStates.instance.all[skl.dbObj.second]; 
            WaveSpawner.instance.DoSpawnAny(skl.dbObj.extraPars, "player", null, null, true, trg.Position - new Vector3(1,1,0), trg.Position + new Vector3(1,1,0), isSummon:true);
            skl.SetPar("cd", skl.GetPar("cooldown"));
            SoundManager.instance.PlayAny(skl.dbObj.ID);
            return;
        }
        
        
    }

    public void Explode(RObj skl, RObj exact = null, RObj extraTarget = null)
    {
        var dd = GetTargetsInRange(skl, skl.GetPar("aoe"), exact);
        
        if (extraTarget != null && !dd.Contains(extraTarget))
            dd.Add(extraTarget);
        
        foreach (var d in dd)
        {
            MainStates.instance.DealDamage(d, skl);
        }
        SoundManager.instance.PlayAny(skl.dbObj.ID + "_expl");
        Destroy(skl.main);
    }

    public List<RObj> GetTargetsInRange(RObj skl, float range, RObj other = null)
    {
        List<RObj> targets = new List<RObj>();
        var aoe = skl.GetPar("aoe");
        //combats or all ?
        foreach (var v1 in MainStates.instance.combats)
        {
            var v = v1;
            if (v.it != ItemType.monster) continue;
            var d = MainStates.instance.GetDistance(v, other == null ? skl : other, out float dd);
            if (d > aoe) continue;

            if (skl.GetPar("target") == 2)
            {
                targets.Add(v);    
            }
            else if (v.tags[0] == skl.owner.tags[0] && (skl.GetPar("target") == 1 || skl.dbObj.pars["target"] == 1))
            {
                targets.Add(v);
            }
            else if (v.tags[0] != skl.owner.tags[0] && (skl.GetPar("target") == 0 || skl.dbObj.pars["target"] == 0))
            {
                targets.Add(v);
            }
        }
        
        return targets;
    }

    public void CancelAction()
    {
        if (curTargeter != null) curTargeter.Deactivate();
        reqAction = false;
    }

    public void Update()
    {
        if (lastSkl != null && lastSkl.dbObj.ID == "move")
        {
            if (movePath) movePath.gameObject.SetActive(true);
        }
        else
        {
            if (movePath) movePath.gameObject.SetActive(false);
        }
        
        if (!reqAction) return;
        if (Input.GetMouseButtonDown(1))
        {
            if (reqAction)
            {
                curTargeter.Deactivate();
                reqAction = false;
                return;
            }
        }

        if (reqAction)
        {
            Vector3 pos = Vector3.zero;
            if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
            {
                pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                pos.z = -0.1f;
            }
            else
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var h = Physics.Raycast(ray, out RaycastHit hit, 100, 1<< LayerMask.NameToLayer("Earth"));
                pos = hit.point;
            }

            curTargeter.MouseMoved(pos);
        }
        
        if (Input.GetMouseButtonDown(0) && !curTargeter.handleExec)
        {
            if (UtilsControl.IsPointerOverUIElement()) return;
            if (curTargeter.reqFind && curTargeter.find == null && !MainStates.instance.freeTargeting) return;
            
            if (reqAction)
            {
                var pos = UtilsControl.GetMousePoint();
                curTargeter.Deactivate();
                reqAction = false;

                if (MainStates.instance.freeTargeting)
                {
                    //pos stays from mouse cast
                    if (ConfigLoader.GetMetaParamValue("coord_mode_xy") > 0)
                        pos.z = 0;
                    else
                        pos.y = 0.3f;
                }
                else if (lastSkl.GetPar("aoe") == 0)
                {
                    if (curTargeter.find == null)
                    {
                        var gg = MainStates.instance.GetClosestEnemy(lastWho, out var df, fromPos: pos);
                        pos = gg.Position;
                        lastSkl.exact = gg;
                    }
                    else
                    {
                        var gg = curTargeter.find;
                        pos = gg.Position;
                        lastSkl.exact = gg;
                    }
                }
                else if (curTargeter.find != null)
                {
                    lastSkl.exact = curTargeter.find;
                }
                
                int p = (int)lastSkl.GetPar("proj_amount");
                float dt = lastSkl.GetPar("dt");
                float ft = lastSkl.GetPar("first");
                float da = lastSkl.GetPar("angle")* Mathf.Deg2Rad;;
                
                string savedSkl = lastSkl.dbObj.ID;
                
                
                
                var tmpSkl = lastSkl;
                //if (!tmpSkl.HasVis("coll")) tmpSkl.AddViz("coll");       
                SoundManager.instance.PlayAny(lastSkl.dbObj.ID);
                
                var ll = ResourceHolder.instance.GetMeSkillEtc(lastWho.dbObj, lastSkl.dbObj.ID);
                if (ll != null && ll.effSelf != null)
                {
                    var kk = Instantiate(ll.effSelf);
                    kk.transform.position = lastWho.Position;
                    Destroy(kk, 1);
                }
                

                
                if (lastSkl.dbObj.spawn != "")
                {
                    var gg = Instantiate(ResourceHolder.instance.skillsWorld[lastSkl.dbObj.spawn], lastWho.main.transform);
                    if (MainStates.instance.freeTargeting)
                        gg.GetComponent<SkillBehavior>().SetTarget(pos);
                    else gg.GetComponent<SkillBehavior>().SetTarget(curTargeter.find);
                }
                else
                {
                    for (int j = 0; j < p; j++)
                    {
                        FunctionTimer.Create(() =>
                        {
                            var ro = DatabaseAll.instance.CreateProjectile(
                                MainStates.instance.lastAllySelected == null
                                    ? MainStates.instance.mainPlayer
                                    : MainStates.instance.lastAllySelected,
                                savedSkl, Vector3.zero);
                            ro.AddViz("coll#scale:0.2");

                            if (ro.dbObj.ID == "put")
                            {
                                ro.main.transform.position = lastWho.Position + new Vector3(0,0.5f,0);
                                ro.Position = ro.main.transform.position;
                                if (lastWho.attachables.Count > 0)
                                {
                                    for (int o = lastWho.attachables.Count - 1; o >= 0; o--)
                                    {
                                        lastWho.attachables[o].parent = ro.main.transform;
                                        ro.attachables.Add(lastWho.attachables[o]);
                                    }
                                    lastWho.attachables.Clear();
                                }
                            }
                            
                            if (lastSkl.exact != null)
                                ro.exact = lastSkl.exact;
                            ro.trg = pos;
                            Vector3 ep = pos + new Vector3(0, MainStates.instance.LIFT_PROJ, 0);
                            if (da > 0)
                            {
                                var vec = pos + new Vector3(0, MainStates.instance.LIFT_PROJ, 0) - ro.Position;
                                Vector3 rotatedVector = Quaternion.Euler(0, 0, Random.Range(-da, da)) * vec;
                                ep = ro.Position + rotatedVector;
                            }

                            UtilsControl.Instance.MoveTo(ro.main.transform, ro.GetPar("speed"),
                                ep,
                                () =>
                                {
                                    ro.RemoveViz("coll");
                                    Explode(ro);
                                    if (ro.effect != null)
                                    {
                                        var go = Instantiate(ro.effect);
                                        go.transform.position = ro.main.transform.position;
                                        Destroy(go, 1);
                                        ro.RemoveViz("vis_main");
                                    }

                                    //
                                    if (ro.dbObj.ID == "move")
                                    {
                                        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0 
                                            || ConfigLoader.GetMetaParamValue("mode_isometric") > 0
                                            ||  ConfigLoader.GetMetaParamValue("mode_hex") > 0)
                                        {
                                            var savedOwner = ro.owner;
                                            var savedPos = ro.main.transform.position;
                                            UtilsControl.Instance.MoveToMany(savedOwner.main.transform, 10, UtilsControl.ConvertPath(movePath.lastPath), 0,
                                                () =>
                                                {
                                                    savedOwner.Position = savedPos;
                                                    savedOwner.AdjustPosition();
                                                    if (savedOwner == MainStates.instance.mainPlayer)
                                                    {
                                                        EventManager.INV("main_move", new ArgPass { pos = savedPos });
                                                    }
                                                }, () =>
                                                {
                                                    savedOwner.Position = savedOwner.main.transform.position;
                                                    savedOwner.AdjustPosition();
                                                });
                                        }
                                        else
                                        {
                                            var savedOwner = ro.owner;
                                            var savedPos = ro.main.transform.position;
                                            var bn = PositionSetter.instance.GetClosestPos(savedPos);
                                            savedOwner.ref_pos_x = bn.Item1;
                                            savedOwner.ref_pos_y = bn.Item2;

                                            UtilsControl.Instance.MoveTo(ro.owner.main.transform,
                                                ConfigLoader.GetMetaParamValue("global_move"), ro.main.transform.position,
                                                () =>
                                                {
                                                    savedOwner.Position = savedPos;
                                                    savedOwner.AdjustPosition();
                                                    if (savedOwner == MainStates.instance.mainPlayer)
                                                    {
                                                        EventManager.INV("main_move", new ArgPass { pos = savedPos });
                                                    }

                                                }, null, useRight: false);                                            
                                        }

                                        
                                        
                                    }
                                    else if (ro.dbObj.ID == "teleport")
                                    {
                                        ro.owner.main.transform.position = ro.main.transform.position;
                                        ro.owner.Position = ro.main.transform.position;
                                        ro.owner.AdjustPosition();
                                        if (ro.owner == MainStates.instance.mainPlayer)
                                        {
                                            EventManager.INV("main_move", new ArgPass { pos = ro.owner.Position });
                                        }
                                    }
                                    else if (ro.dbObj.ID == "put")
                                    {
                                        foreach (var v in ro.attachables)
                                        {
                                            v.parent = null;
                                            v.GetComponentInChildren<XDpick>().picked = false;
                                            v.GetComponentInChildren<XDpick>().EnableCollider();
                                            
                                            var kk = v.GetComponent<ObjHolder>();
                                            kk.enabled = true;
                                            kk.obj.Position = v.position;
                                            kk.obj.AdjustPosition();
                                            var tt = v.GetComponent<MoveDir>();
                                            if (tt != null) tt.enabled = true;
                                        }
                                    }
                                    else if (ro.dbObj.ID.IndexOf("summon") >= 0)
                                    {
                                        var gg = WaveSpawner.instance.DoSpawnAny(ro.dbObj.extraPars, "player", null,
                                            null, false,
                                            ro.Position, ro.Position, true);
                                        foreach (var v in gg)
                                        {
                                            v.AdjustPosition();
                                        }
                                        //action_req ? or mark them as summon ?
                                    }

                                }, null, travelType: (TravelType)ro.GetPar("travel"));
                        }, dt * (float)j);



                    }
                }

                if (ConfigLoader.GetMetaParamValue("use_naprig") == 1 && lastSkl.dbObj.ID == "basic_melee")
                {
                    MainStates.instance.DoNaprig(lastWho, pos + new Vector3(0, MainStates.instance.LIFT_PROJ, 0));
                }
                
                lastSkl.SetPar("cd", lastSkl.GetPar("cooldown"));
                lastWho.ChangePar("mana", -lastSkl.GetPar("manacost")); 
                
                lastSkl = null;
                EventManager.INV("casted", new ArgPass{what = savedSkl});                
                
                
            }
        }
    }
}

public enum ExecReso
{
    NO_TARGETS = -1,
    NO_MANA = -2,
    OK = 0,
    NO_SIGHT = 3
}


