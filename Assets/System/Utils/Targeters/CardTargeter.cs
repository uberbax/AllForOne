using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;

public class CardTargeter : Targeter
{
    public LineRenderer line;
    public List<Vector3> positions = new List<Vector3>();

    private Vector3 liftDown = new Vector3(0, -1, 0);
    //

    public Vector3 pos1;
    public Vector3 pos2;

    public Deck deck;
    
    public override void MouseMoved(Vector3 pos)
    {
        base.MouseMoved(pos);

        if (SkillExecutor.instance.lastSkl == null) return;
        
        if (SkillExecutor.instance.lastSkl.GetPar("target") == 1)
        {
            pos2 = MainStates.instance.mainPlayer.Position;
            find = MainStates.instance.mainPlayer;
            return;
        }
        
        var gg = MainStates.instance.GetClosestEnemy(MainStates.instance.mainPlayer, out var df, fromPos: MainStates.instance.lastCard.position);
        pos = gg.Position;
        
        line.enabled = true;
        positions = new List<Vector3>();

        pos1 = MainStates.instance.lastCard.transform.position;
        
        //its position in perspective state
        var tt = Camera.main.WorldToScreenPoint(new Vector3(gg.pos_x, gg.pos_y, gg.pos_z));
        var dx = -MainStates.instance.mainPlayer.pos_x + gg.Position.x;
        pos2 = gg.Position + new Vector3(0.8f * dx, 0, 0) + liftDown;
        
        int cnt = 10;
        for (int i = 0; i <= cnt; i++)
        {
            float t = ((float)i) / cnt;
            Vector3 p = pos1 + (pos2 - pos1) * t + new Vector3(0, t*(1-t) * 2  ,0);
            positions.Add(p);
        }
        
        line.positionCount = positions.Count;
        line.SetPositions(positions.ToArray());

        target.transform.position = pos2;
        target.transform.forward = -Camera.main.transform.forward;

        find = gg;

    }

    public override void HandleExec()
    {
        base.HandleExec();
        Vector3 pos = Vector3.zero;
        
        if (SkillExecutor.instance.lastSkl.GetPar("aoe") == 0)
        {
            var gg = find;
            pos = gg.Position;
            SkillExecutor.instance.lastSkl.exact = gg;
        }
        
                
        var tmpSkl = SkillExecutor.instance.lastSkl;
        //dont like it
        if (tmpSkl.main == null)
        {
            DatabaseAll.instance.CreateOnlyVizual(tmpSkl, SkillExecutor.instance.lastWho.Position);
            if (!tmpSkl.HasVis("coll")) tmpSkl.AddViz("coll");
        }
                
        
        MainStates.instance.mainPlayer.ChangePar("mana", -tmpSkl.GetPar("mana_req"));
                
        UtilsControl.Instance.MoveTo(tmpSkl.main.transform, tmpSkl.GetPar("speed"), pos2,
            () =>
            {
                tmpSkl.RemoveViz("coll");
                SkillExecutor.instance.Explode(tmpSkl, extraTarget:tmpSkl.exact);
                if (tmpSkl.effect != null)
                {
                    var go = Instantiate(tmpSkl.effect);
                    go.transform.position = tmpSkl.main.transform.position;
                    Destroy(go, 1);
                    tmpSkl.RemoveViz("vis_main");
                }
                
                
            }, null, travelType:(TravelType)tmpSkl.GetPar("travel"));
        SkillExecutor.instance.lastSkl.SetPar("cd", SkillExecutor.instance.lastSkl.GetPar("cooldown"));

        SkillExecutor.instance.lastSkl = null;
        
        //
        UtilsControl.Instance.MoveTo(MainStates.instance.lastCard, 20, pos2, null, null);
        var tmpCard = MainStates.instance.lastCard;
        UtilsControl.Instance.ApplyCurve(tmpCard, AnimationCurve.Linear(0, 1, 1, 0.5f), UtilsControl.CurveType.Scale,
            () =>
            {
                tmpCard.GetComponent<FillCard>().cardEffect.SetActive(true);
                tmpCard.GetComponent<FillCard>().Deactivate();
                
                SkillExecutor.instance.CancelAction();
                
                Destroy(tmpCard.gameObject, 1);
                FunctionTimer.Create(() =>
                {
                    deck.DoUpdate();
                }, 1.1f);
                
            }, 0.33f, 3, 1, 0, Color.white);    
        
    }

    public override void Deactivate()
    {
        base.Deactivate();
        line.enabled = false;
    }
}
