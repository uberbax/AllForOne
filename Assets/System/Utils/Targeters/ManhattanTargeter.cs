using System;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using UnityEngine;

public class ManhattanTargeter : Targeter
{
    private RObj empty;
    
    public LineRenderer range;
    public float size = 1;

    public LineRenderer secondRange;
    private void Start()
    {
        var go = new GameObject();
        empty = DatabaseAll.instance.CreateAny("empty", false, 1, go);
    }

    public override void MouseMoved(Vector3 pos)
    {
        base.MouseMoved(pos);

        var hh = PositionSetter.instance.GetClosestPos(pos);
        var ee = PositionSetter.instance.IsEmpty(hh.Item1, hh.Item2, out GameObject go);
        bool passable = false;
        if (go != null)
        {
            var go1 = go.GetComponent<ObjHolder>();
            if (go1 != null) passable = go1.obj.GetPar("passable") > 0;
        }
        
        target.transform.position = PositionSetter.instance.floors[hh.Item1, hh.Item2].transform.position;
        
        var hh0 = PositionSetter.instance.GetClosestPos(SkillExecutor.instance.lastWho.Position);

        if (ConfigLoader.GetMetaParamValue("mode_manhattan") > 0)
        {
            var g = MainStates.instance.GetDistance(hh.Item1, hh.Item2, hh0.Item1, hh0.Item2);
            if (g > SkillExecutor.instance.lastSkl.GetPar("range"))
            {
                target.GetComponent<SpriteRenderer>().color = Color.red;
                find = null;
                return;
            }
        }

        var ss = SkillExecutor.instance.lastSkl;
        if (ss.GetPar("empty_req") == 1)
        {
            if (ee || passable)
            {
                target.GetComponent<SpriteRenderer>().color = Color.green;
                empty.main.transform.position = target.transform.position;
                find = empty;
            }
            else
            {
                target.GetComponent<SpriteRenderer>().color = Color.red;
                find = null;
            }
        }
        else if (ss.GetPar("aoe") > 0 && go == null)
        {
            if (ee || passable)
            {
                target.GetComponent<SpriteRenderer>().color = Color.green;
                empty.main.transform.position = target.transform.position;
                find = empty;
            }
            else
            {
                target.GetComponent<SpriteRenderer>().color = Color.green;
                var oo = go.GetComponent<ObjHolder>().obj;
                find = oo;
            }
        }
        else
        {
            if (go == null || go.name.IndexOf("wall") >= 0 || go.GetComponent<ObjHolder>() == null)
            {
                target.GetComponent<SpriteRenderer>().color = Color.red;
                find = null;
                return;
            }
            
            var oo = go.GetComponent<ObjHolder>().obj;
            if (
                (SkillExecutor.instance.lastSkl.GetPar("target") == 0) ||
                (SkillExecutor.instance.lastSkl.GetPar("target") == 2)
                )
            {
                if (
                    (!MainStates.CompareTags(SkillExecutor.instance.lastWho, oo))||
                    (SkillExecutor.instance.lastSkl.GetPar("target") == 2)
                    )
                {
                    target.GetComponent<SpriteRenderer>().color = Color.green;
                    find = oo;
                }
                else
                {
                    target.GetComponent<SpriteRenderer>().color = Color.red;
                    find = null;
                }
            }
            else
            {
                if (!MainStates.CompareTags(SkillExecutor.instance.lastWho, oo))
                {
                    target.GetComponent<SpriteRenderer>().color = Color.red;
                    find = null;
                }
                else
                {
                    target.GetComponent<SpriteRenderer>().color = Color.green;
                    find = oo;

                }
            }
        }

    }

    private void Update()
    {
        //do range
        var skl = SkillExecutor.instance.lastSkl.GetPar("range");
        var sklAoe = SkillExecutor.instance.lastSkl.GetPar("aoe");
        
        var gv = PositionSetter.instance.GetClosestPos(SkillExecutor.instance.lastWho.Position);
        MainStates.instance.ShowRange(range, gv.Item3, skl, size);

        if (sklAoe > 0 && find != null)
        {
            secondRange.gameObject.SetActive(true);
            MainStates.instance.ShowRange(secondRange, find.Position, sklAoe, size);
        }
        else
        {
            secondRange.gameObject.SetActive(false);
        }
    }

    public override void Deactivate()
    {
        base.Deactivate();
    }
    
}
