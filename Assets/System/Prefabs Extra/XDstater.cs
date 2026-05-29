using Unity.VisualScripting;
using UnityEngine;

public class XDstater : ComponentBehavior
{
    public RObj mon;
    public string state = "idle";
    public Vector3 savedStart = Vector3.zero;
    public Vector3 lastCalmPoint = Vector3.zero;

    private float aggroNearby = 3;
    private float aggroRange = 5;
    private float pursueDistance = 10;
    public bool inAggro = false;

    private float idleRange = 1.5f;
    //QUESTION mark on Stop Pursuing
    //EXCLAMATION MARK on Start Pursuing

    private bool allowIdleWalk = false;
    private bool allowIdleGathering = true;

    public void MakeMeAggro()
    {
        if (inAggro) return;
        
        if (state == "idle")
        {
            lastCalmPoint = mon.Position;
        }
        
        //Show exclamation
        state = "pursue";
        inAggro = true;
        UtilsControl.Instance.FlyText3D(mon.Position, "!", Color.white, Vector3.zero);
        //make another aggro ?
        var g = MainStates.instance.GetAlliesInRange(mon, aggroNearby);
        foreach (var e in g)
        {
            if (e.HasVis("stater"))
            {
                if (e.visuals["stater"].GetComponent<XDstater>().inAggro) continue;
                e.visuals["stater"].GetComponent<XDstater>().MakeMeAggro();
            }
            else continue;
        }
    }

    private RObj empty;
    
    void Start()
    {
        mon = GetComponentInParent<ObjHolder>().obj;
        savedStart = transform.position;
        lastCalmPoint = savedStart;
        
        var go = new GameObject();
        go.name = "path_point";
        empty = DatabaseAll.instance.CreateAny("empty", false, 1, go);
    }

    public void Iteration(XDcombat from)
    {
        var c = MainStates.instance.GetClosestEnemy(mon, out float d);
        var d1 = MainStates.instance.GetLowestDistanceSkills(mon);

        //EXTRA TASKS !!!!!

        if ((state == "idle" || state == "idle_task") && allowIdleGathering && mon.attachables.Count == 0)
        {
            //find goldmine or find tree
            state = "idle_task";
            var a = MainStates.instance.FindClosestObj(mon, "goldmine", false, true);
            //move and attack there
            mon.visuals["combat"].GetComponent<XDcombat>().Iteration(ignoreState: true, overTarget:a);
        }
        
        if (state == "idle_task" && allowIdleGathering && mon.attachables.Count > 0)
        {
            //find barracks to drop
            var a = MainStates.instance.FindClosestObj(mon, "barracks", false, false);
            //move and drop there
            var dd = MainStates.instance.GetDistance(mon, a, out var st);
            if (dd <= 1 || st <= 1)
            {
                SkillExecutor.instance.ExecuteSkill(mon, "put", a);
            }
            
            MainStates.instance.MovePath(mon, a);
        }
        
        
        //EXTRA TASKS !!!!!        
        
        
        if ((state == "idle" || state == "return_to_point" || state == "idle_move") && d < aggroRange)
        {
            MakeMeAggro();
            return;
        }

        if (state == "pursue")
        {
            if (c == null || d > pursueDistance)
            {
                //trigger ???
                inAggro = false;
                state = "return_to_point";
                UtilsControl.Instance.FlyText3D(mon.Position, "?", Color.white, Vector3.zero);
                //return to latest save point
                var g = mon.main.GetOrAddComponent<PathfindingMovement>();
                
                empty.main.transform.position = lastCalmPoint;
                empty.Position = lastCalmPoint;
                empty.AdjustPosition();
                
                g.SetTarget(empty.main.transform);
            }
            else
            {
                mon.visuals["combat"].GetComponent<XDcombat>().Iteration(ignoreState: true);
            }
            return;
        }

        if (state == "return_to_point" || state == "idle_move")
        {
            var hh = MainStates.instance.GetDistance(lastCalmPoint, mon, out float dst);
            if (hh == 0 || dst < 1)
            {
                state = "idle";
                return;
            }
            
        }

        if (state == "idle" && allowIdleWalk)
        {
            //allow funzies
            lastCalmPoint = MainStates.instance.GetRndFree(savedStart, idleRange);
            var g = mon.main.GetOrAddComponent<PathfindingMovement>();

            empty.main.transform.position = lastCalmPoint;
            empty.Position = lastCalmPoint;
            empty.AdjustPosition();
            
            g.SetTarget(empty.main.transform);
            state = "idle_move";
            return;
        }


        if (state == "idle_move" || state == "return_to_point")
        {
            MainStates.instance.MovePath(mon, empty);
        }
        
    }

}
